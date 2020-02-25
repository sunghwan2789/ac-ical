using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Ical.Net;
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
            do
            {
                Console.WriteLine(seedUrl);
                var doc = client.Load(seedUrl);

                foreach (var scheduleAnchor in doc.DocumentNode.SelectNodes("//table//a"))
                {
                    var scheduleUri = new Uri(baseUri, scheduleAnchor.Attributes["href"].DeEntitizeValue);

                    var acevent = await RunWithRetries(() =>
                    {
                        Console.WriteLine(scheduleUri);
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

                    // Ignore events that begin past year
                    if (acevent.Begin.Year < DateTime.Now.Year)
                    {
                        yield break;
                    }

                    yield return acevent;
                }

                var next = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'box_pager')]/span/following-sibling::a");
                seedUrl = next != null ? new Uri(baseUri, next.Attributes["href"].DeEntitizeValue).ToString() : null;
            }
            while (!string.IsNullOrEmpty(seedUrl));
        }

        static async Task Main(string[] args)
        {
            var calendar = new Calendar();
            var dest = args.FirstOrDefault();

            try
            {
                await foreach (var schedule in GetAcademicSchedules())
                {
                    calendar.Events.Add(schedule.ToCalendarEvent());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            var serializer = new CalendarSerializer(calendar);
            if (dest != null)
            {
                using var fs = File.OpenWrite(dest);
                using var sw = new StreamWriter(fs);
                sw.Write(serializer.SerializeToString());
            }
            else
            {
                Console.WriteLine(serializer.SerializeToString());
            }
        }
    }
}
