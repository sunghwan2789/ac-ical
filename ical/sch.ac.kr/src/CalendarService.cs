using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Options;
using HtmlAgilityPack;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace sch.ac.kr
{
    public class CalendarService
    {
        public CalendarService(IOptions<CalendarServiceOptions> options)
        {
            Options = options.Value;
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
                var document = await RunWithRetriesAsync(async () =>
                {
                    Console.Error.WriteLine(seedUrl);
                    return await Client.LoadFromWebAsync(seedUrl);
                });

                foreach (var eventAnchor in document.DocumentNode.SelectNodes("//table//a"))
                {
                    var eventUrl = new Uri(baseUri, eventAnchor.Attributes["href"].DeEntitizeValue).ToString();

                    var evnt = await RunWithRetriesAsync(async () =>
                    {
                        Console.Error.WriteLine(eventUrl);
                        return await GetEventAsync(eventUrl);
                    });

                    // Ignore events that would not be changed.
                    if ((evnt.DtStart.Value < Options.MinimumDtStart)
                        || (DateTime.Today.Subtract(evnt.DtStart.Value) > Options.MaximumElapsedTimeSinceDtStartToToday))
                    {
                        yield break;
                    }

                    yield return evnt;
                }

                // Reached the end page.
                var nextAnchor = document.DocumentNode.SelectSingleNode("//div[contains(@class, 'box_pager')]/span/following-sibling::a");
                if (nextAnchor == null)
                {
                    yield break;
                }

                seedUrl = new Uri(baseUri, nextAnchor.Attributes["href"].DeEntitizeValue).ToString();
            }
        }

        public async Task<CalendarEvent> GetEventAsync(string url)
        {
            var document = await Client.LoadFromWebAsync(url);
            var tds = document.DocumentNode.SelectNodes("//table//td");

            var id = HttpUtility.ParseQueryString(new Uri(url).Query)["article_no"];
            var summary = tds.ElementAt(0).InnerText;
            var startDate = DateTime.Parse(tds.ElementAt(1).InnerText);
            var description = WebUtility.HtmlDecode(tds.ElementAt(2).InnerText);

            return new CalendarEvent
            {
                Uid = id,
                Summary = summary,
                DtStart = new CalDateTime(startDate),
                DtEnd = new CalDateTime(ParseEndDate().AddDays(1)),
                Description = description,
            };

            DateTime ParseEndDate()
            {
                var match = Regex.Match(description, @"^\s*(?:\d+\.\d+|\d+\/\d+|\d+)[^\d]+(\d+\.\d+|\d+\/\d+|\d+)");
                if (!match.Success)
                {
                    return startDate;
                }

                var monthDay = match.Groups[1].Value.Split('.', '/');
                var month = monthDay.Length > 1 ? int.Parse(monthDay[0]) : startDate.Month;
                var day = int.Parse(monthDay[monthDay.Length > 1 ? 1 : 0]);

                try
                {
                    var endDate = new DateTime(startDate.Year, month, day);
                    if (month < startDate.Month)
                    {
                        endDate = endDate.AddYears(1);
                    }
                    if (day < endDate.Day)
                    {
                        endDate = endDate.AddMonths(1);
                    }

                    if (endDate < startDate)
                    {
                        return startDate;
                    }

                    return endDate;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return startDate;
                }
            }
        }

        private static async Task<T> RunWithRetriesAsync<T>(Func<Task<T>> func)
        {
            var delays = new[] { 1000, 5000, 10000, 30000, 60000, };
            var exceptions = new List<Exception>();
            foreach (var delay in delays)
            {
                await Task.Delay(delay);
                try
                {
                    return await func();
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