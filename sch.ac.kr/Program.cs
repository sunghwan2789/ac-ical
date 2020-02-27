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

        static async Task<T> RunWithRetries<T>(Func<T> func)
        {
            var delays = new[] { 1000, 5000, 10000, 30000, 60000, };
            var exceptions = new List<Exception>();
            foreach (var delay in delays)
            {
                await Task.Delay(delay);
                try
                {
                    return func();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            throw new AggregateException(exceptions);
        }

        static async IAsyncEnumerable<AcademicEvent> GetAcademicSchedules()
        {
            var seedUrl = @"https://homepage.sch.ac.kr/sch/05/05010001.jsp?mode=list&board_no=20110224223754285127";
            var baseUri = new Uri(seedUrl);
            for (; ; )
            {
                Console.Error.WriteLine(seedUrl);
                var doc = client.Load(seedUrl);

                foreach (var scheduleAnchor in doc.DocumentNode.SelectNodes("//table//a"))
                {
                    var scheduleUri = new Uri(baseUri, scheduleAnchor.Attributes["href"].DeEntitizeValue);

                    var acevent = await RunWithRetries(() =>
                    {
                        Console.Error.WriteLine(scheduleUri);
                        var sdoc = client.Load(scheduleUri);
                        var contents = sdoc.DocumentNode.SelectNodes("//table//td");
                        return new AcademicEvent
                        {
                            Url = scheduleUri.ToString(),
                            Title = contents.ElementAt(0).InnerText,
                            Begin = DateTime.Parse(contents.ElementAt(1).InnerText),
                            Content = WebUtility.HtmlDecode(contents.ElementAt(2).InnerText),
                        };
                    });

                    // Ignore events that would not be changed.
                    if (acevent.Begin < DateTime.Now.AddMonths(-2))
                    {
                        yield break;
                    }

                    yield return acevent;
                }

                // Reached the end page.
                var next = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'box_pager')]/span/following-sibling::a");
                if (next == null)
                {
                    yield break;
                }

                seedUrl = new Uri(baseUri, next.Attributes["href"].DeEntitizeValue).ToString();
            }
        }

        static async Task Main(string[] args)
        {
            var calendar = new Calendar();
            var dest = args.FirstOrDefault();

            // First, grab online calendar events.
            try
            {
                await GetAcademicSchedules().Reverse()
                    .ForEachAsync(i => calendar.Events.Add(i.ToCalendarEvent()));
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
                Func<CalendarEvent, CalendarEvent, bool> hasSameUid = (a, b) => a.Uid == b.Uid;

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
