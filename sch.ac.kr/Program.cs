using System;
using System.Linq;
using System.Threading.Tasks;
using Ical.Net;

namespace sch_academic_calendar
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var dest = args.FirstOrDefault();

            var app = new App(new AppOptions
            {
                FileName = dest,
            }, new Bot(new BotOptions()));

            // First, grab online calendar events.
            Calendar calendar;
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

            if (!app.ShouldSync())
            {
                goto DUMP;
            }

            // Second, fork old calendar and update it using new calendar.
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
