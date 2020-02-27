using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.Serialization;

namespace sch_academic_calendar
{
    class Program
    {
        static readonly HtmlWeb client = new HtmlWeb();

        static async Task Main(string[] args)
        {
            var calendar = new Calendar();
            var dest = args.FirstOrDefault();

            var app = new App(new AppOptions
            {
                FileName = dest,
            });

            var bot = new Bot(new BotOptions());

            // First, grab online calendar events.
            try
            {
                await bot.GetCalendarEventsAsync()
                    .ForEachAsync(i => calendar.Events.Add(i));
            }
            // If exception occurs, let me handle grabbed events just before the exception.
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception thrown while getting events: {ex.Message}\n{ex}");
            }
            // Something went wrong!
            if (calendar.Events.Count == 0)
            {
                Console.Error.WriteLine("There are no event. Something went wrong!");
                Environment.ExitCode = 1;
                return;
            }

            // If no dest specified, dump iCalendar data to standard output and exit.
            if (dest == null)
            {
                Console.WriteLine(new CalendarSerializer(calendar).SerializeToString());
                return;
            }

            // Second, fork old calendar and update it using new calendar.
            if (!File.Exists(dest))
            {
                Console.Error.WriteLine("iCalendar file is clean. Skipping fork...");
                goto DUMP;
            }
            try
            {
                var oldCalendar = Calendar.Load(await File.ReadAllTextAsync(dest));

                // Filter old events that may need update.
                var lowerBound = calendar.Events.FirstOrDefault(i => oldCalendar.Events[i.Uid] != default);
                if (lowerBound == null)
                {
                    Console.Error.WriteLine("Lost the synchronization point event. Skipping fork...");
                    goto DUMP;
                }
                var updatingEvents = oldCalendar.Events.SkipWhile(i => i.Uid != lowerBound.Uid);

                // Remove removed events in old events.
                updatingEvents.Except(calendar.Events, hasSameUid)
                    .ToList()
                    .ForEach(i => oldCalendar.Events.Remove(i));

                // Update old events and increase edit count.
                updatingEvents.Intersect(calendar.Events, hasSameUid)
                    .Select(i => (i, calendar.Events[i.Uid]))
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
                calendar.Events.Except(updatingEvents, hasSameUid)
                    .ToList()
                    .ForEach(i => oldCalendar.Events.Add(i));

                // Swap.
                calendar = oldCalendar;

                bool hasSameUid(CalendarEvent a, CalendarEvent b) => a.Uid == b.Uid;
            }
            // If the old calendar does not exists or it is corrupted, use new one as a result.
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception thrown while reading old calendar: {ex.Message}\n{ex}");
            }

        DUMP:
            // Dump iCalendar data to dest and exit.
            await File.WriteAllTextAsync(dest, new CalendarSerializer(calendar).SerializeToString());
        }
    }
}
