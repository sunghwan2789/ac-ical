using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.Serialization;

namespace sch_academic_calendar
{
    class CalendarManager
    {
        public CalendarManager(CalendarManagerOptions options)
        {
            Options = options;
        }

        private CalendarManagerOptions Options { get; }

        public Calendar? Calendar { get; private set; }

        public bool IsClean() =>
            !Options.LoadInput
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
                throw new NullReferenceException(nameof(Calendar));
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
                throw new NullReferenceException(nameof(Calendar));
            }

            // Filter old events that may need update.
            var lowerBound = incoming.Events.FirstOrDefault(i => Calendar.Events[i.Uid] != null);
            if (lowerBound == null)
            {
                Console.Error.WriteLine("Lost the synchronization point event. Skipping fork...");
                return;
            }
            var updatingEvents = Calendar.Events.SkipWhile(i => i.Uid != lowerBound.Uid);

            // Remove removed events in old events.
            updatingEvents.Except(incoming.Events, HaveSameUid)
                .ToList()
                .ForEach(i => Calendar.Events.Remove(i));

            // Update old events and increase edit count.
            updatingEvents.Intersect(incoming.Events, HaveSameUid)
                .Select(i => (i, incoming.Events[i.Uid]))
                .ToList()
                .ForEach(t =>
                {
                    var (oldEvent, newEvent) = t;
                    if (!oldEvent.Equals(newEvent))
                    {
                        oldEvent.Summary = newEvent.Summary;
                        oldEvent.DtStart = newEvent.DtStart;
                        oldEvent.DtEnd = newEvent.DtEnd;
                        oldEvent.Description = newEvent.Description;
                        oldEvent.DtStamp = newEvent.DtStamp;
                        oldEvent.Sequence++;
                    }
                });

            // Add new events.
            incoming.Events.Except(updatingEvents, HaveSameUid)
                .ToList()
                .ForEach(i => Calendar.Events.Add(i));

            bool HaveSameUid(CalendarEvent a, CalendarEvent b) => a.Uid == b.Uid;
        }
    }
}
