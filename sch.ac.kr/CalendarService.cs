using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Ical.Net;
using Ical.Net.CalendarComponents;

namespace sch_academic_calendar
{
    class CalendarService
    {
        public CalendarService(CalendarServiceOptions options)
        {
            Options = options;
        }

        private CalendarServiceOptions Options { get; }
        private HtmlWeb Client { get; } = new HtmlWeb();

        public async Task<Calendar> GetCalendarAsync()
        {
            var calendar = new Calendar();
            try
            {
                await GetEventsAsync().ForEachAsync(calendar.Events.Add);
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

        public IAsyncEnumerable<CalendarEvent> GetEventsAsync() =>
            GetEventsExAsync().Reverse();

        private async IAsyncEnumerable<CalendarEvent> GetEventsExAsync()
        {
            var seedUrl = "https://homepage.sch.ac.kr/sch/05/05010001.jsp?mode=list&board_no=20110224223754285127";
            var baseUri = new Uri(seedUrl);
            for (; ; )
            {
                Console.Error.WriteLine(seedUrl);
                var doc = await Client.LoadFromWebAsync(seedUrl);

                foreach (var scheduleAnchor in doc.DocumentNode.SelectNodes("//table//a"))
                {
                    var eventUrl = new Uri(baseUri, scheduleAnchor.Attributes["href"].DeEntitizeValue).ToString();

                    var acevent = await RunWithRetriesAsync(() =>
                    {
                        Console.Error.WriteLine(eventUrl);
                        return GetEvent(eventUrl);
                    });

                    // Ignore events that would not be changed.
                    if ((acevent.DtStart.Value < Options.MinimumDtStart)
                        || (DateTime.Today.Subtract(acevent.DtStart.Value) >= Options.MinimumElapsedTimeSinceDtStartToToday))
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

        public CalendarEvent GetEvent(string url)
        {
            var sdoc = Client.Load(url);
            var contents = sdoc.DocumentNode.SelectNodes("//table//td");
            return new AcademicEvent
            {
                Url = url,
                Title = contents.ElementAt(0).InnerText,
                Begin = DateTime.Parse(contents.ElementAt(1).InnerText),
                Content = WebUtility.HtmlDecode(contents.ElementAt(2).InnerText),
            }.ToCalendarEvent();
        }

        private static async Task<T> RunWithRetriesAsync<T>(Func<T> func)
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
    }
}