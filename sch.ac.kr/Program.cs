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
            }, new Bot(new BotOptions()));

            // First, grab online calendar events.
            try
            {
                calendar = await app.GetOnlineCalendarAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception thrown while getting online calendar: {ex.Message}\n{ex}");
                Environment.ExitCode = 1;
                return;
            }

            // If no dest specified, dump iCalendar data to standard output and exit.
            if (dest == null)
            {
                goto DUMP;
            }

            // Second, fork old calendar and update it using new calendar.
            if (!File.Exists(dest))
            {
                Console.Error.WriteLine("iCalendar file is clean. Skipping fork...");
                goto DUMP;
            }
            try
            {
                var oldCalendar = await app.GetOfflineCalendarAsync();

                app.Sync(oldCalendar, calendar);

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
            await app.SaveCalendarAsync(calendar);
        }
    }
}
