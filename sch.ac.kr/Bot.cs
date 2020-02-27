using System.Collections.Generic;
using System;
using System.Linq;
using HtmlAgilityPack;
using Ical.Net.CalendarComponents;
using System.Threading.Tasks;
using System.Net;

namespace sch_academic_calendar
{
    class Bot
    {
        public Bot(BotOptions options)
        {
            Options = options;
        }

        private BotOptions Options { get; }
        private HtmlWeb Client { get; } = new HtmlWeb();

        public IAsyncEnumerable<CalendarEvent> GetCalendarEventsAsync() =>
            GetCalenderEventsExAsync().Reverse();

        private async IAsyncEnumerable<CalendarEvent> GetCalenderEventsExAsync()
        {
            var seedUrl = "https://homepage.sch.ac.kr/sch/05/05010001.jsp?mode=list&board_no=20110224223754285127";
            var baseUri = new Uri(seedUrl);
            for (; ; )
            {
                Console.Error.WriteLine(seedUrl);
                var doc = Client.Load(seedUrl);

                foreach (var scheduleAnchor in doc.DocumentNode.SelectNodes("//table//a"))
                {
                    var scheduleUri = new Uri(baseUri, scheduleAnchor.Attributes["href"].DeEntitizeValue);

                    var acevent = await RunWithRetriesAsync(() =>
                    {
                        Console.Error.WriteLine(scheduleUri);
                        var sdoc = Client.Load(scheduleUri);
                        var contents = sdoc.DocumentNode.SelectNodes("//table//td");
                        return new AcademicEvent
                        {
                            Url = scheduleUri.ToString(),
                            Title = contents.ElementAt(0).InnerText,
                            Begin = DateTime.Parse(contents.ElementAt(1).InnerText),
                            Content = WebUtility.HtmlDecode(contents.ElementAt(2).InnerText),
                        }.ToCalendarEvent();
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

        static async Task<T> RunWithRetriesAsync<T>(Func<T> func)
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