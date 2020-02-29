using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.Serialization;

namespace sch_academic_calendar
{
    class CalendarManager
    {
        public CalendarManager(IOptions<CalendarManagerOptions> options)
        {
            Options = options.Value;
        }

        private CalendarManagerOptions Options { get; }

        public Calendar? Calendar { get; private set; }

        public bool IsClean() =>
            Options.NullInput
            || !File.Exists(Options.FileName ?? Options.InputFileName);

        public async Task SaveAsync(Calendar calendar)
        {
            var filename = Options.FileName ?? Options.OutputFileName;
            using var stream = filename == null
                ? Console.OpenStandardOutput()
                : File.OpenWrite(filename);
            using var writer = new StreamWriter(stream);
            await writer.WriteAsync(new CalendarSerializer(calendar).SerializeToString());
        }

        public Task SaveAsync()
        {
            if (Calendar == null)
            {
                throw new InvalidOperationException(nameof(Calendar));
            }

            return SaveAsync(Calendar);
        }

        public async Task LoadAsync()
        {
            Calendar = await GetLocalCalendarAsync();
        }

        public async Task<Calendar> GetLocalCalendarAsync()
        {
            var filename = Options.FileName ?? Options.InputFileName;
            return Calendar.Load(await File.ReadAllTextAsync(filename));
        }

        public void Merge(Calendar incoming)
        {
            if (Calendar == null)
            {
                throw new InvalidOperationException(nameof(Calendar));
            }

            // TODO: Check updatingEvents include a newer event than incoming's and remove it.
            var lowerBound = incoming.Events.First(i => Calendar.Events[i.Uid] != default);
            // Filter old events that may need update.
            var updatingEvents = Calendar.Events.SkipWhile(i => i.Uid != lowerBound.Uid);

            // Remove removed events in old events.
            updatingEvents.Except(incoming.Events, HaveSameUid)
                .ToList()
                .ForEach(i => Calendar.Events.Remove(i));

            // Update old events and increase edit count.
            updatingEvents.Intersect(incoming.Events, HaveSameUid)
                .Where(i => !i.Equals(incoming.Events[i.Uid]))
                .ToList()
                .ForEach(i =>
                {
                    var incomingEvent = incoming.Events[i.Uid];
                    i.Summary = incomingEvent.Summary;
                    i.DtStart = incomingEvent.DtStart;
                    i.DtEnd = incomingEvent.DtEnd;
                    i.Description = incomingEvent.Description;
                    i.DtStamp = incomingEvent.DtStamp;
                    i.Sequence++;
                });

            // Add new events.
            incoming.Events.Except(updatingEvents, HaveSameUid)
                .ToList()
                .ForEach(i => Calendar.Events.Add(i));

            bool HaveSameUid(CalendarEvent a, CalendarEvent b) => a.Uid == b.Uid;
        }
    }
}
