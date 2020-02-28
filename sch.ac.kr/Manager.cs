using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.Serialization;

namespace sch_academic_calendar
{
    class Manager
    {
        public Manager(ManagerOptions options, Bot bot)
        {
            Options = options;
            Bot = bot;
        }

        private ManagerOptions Options { get; }
        private Bot Bot { get; }
        public Calendar? WorkingCalendar { get; private set; }

        public async Task<Calendar> GetOnlineCalendarAsync()
        {
            var calendar = new Calendar();
            try
            {
                await Bot.GetCalendarEventsAsync().ForEachAsync(calendar.Events.Add);
            }
            // If it fails, just warn the reason and use some events we got before the exception.
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception thrown while getting events: {ex.Message}\n{ex}");
            }
            // Having no events is weired.
            if (!calendar.Events.Any())
            {
                throw new Exception("There are no events. Something went wrong?");
            }
            return calendar;
        }

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
            if (WorkingCalendar == null)
            {
                throw new NullReferenceException(nameof(WorkingCalendar));
            }
            return SaveAsync(WorkingCalendar);
        }

        public async Task LoadAsync()
        {
            WorkingCalendar = await GetLocalCalendarAsync();
        }

        public async Task<Calendar> GetLocalCalendarAsync()
        {
            var filename = Options.FileName ?? Options.InputFileName;
            return Calendar.Load(await File.ReadAllTextAsync(filename));
        }

        public void Merge(Calendar incoming)
        {
            if (WorkingCalendar == null)
            {
                throw new NullReferenceException(nameof(WorkingCalendar));
            }

            // Filter old events that may need update.
            var lowerBound = incoming.Events.FirstOrDefault(i => WorkingCalendar.Events[i.Uid] != null);
            if (lowerBound == null)
            {
                Console.Error.WriteLine("Lost the synchronization point event. Skipping fork...");
                return;
            }
            var updatingEvents = WorkingCalendar.Events.SkipWhile(i => i.Uid != lowerBound.Uid);

            // Remove removed events in old events.
            updatingEvents.Except(incoming.Events, HaveSameUid)
                .ToList()
                .ForEach(i => WorkingCalendar.Events.Remove(i));

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
                .ForEach(i => WorkingCalendar.Events.Add(i));

            bool HaveSameUid(CalendarEvent a, CalendarEvent b) => a.Uid == b.Uid;
        }
    }
}
