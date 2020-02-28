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
            var app = new App(new AppOptions
            {
                FileName = args.FirstOrDefault(),
            }, new Bot(new BotOptions()));

            // First, get an online calendar.
            Calendar incoming;
            try
            {
                incoming = await app.GetOnlineCalendarAsync();
            }
            // An online calendar is required. Abort.
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception thrown while getting online calendar: {ex.Message}\n{ex}");
                Environment.ExitCode = 1;
                return;
            }

            // If we don't need update, save it and exit.
            if (!app.ShouldUpdate())
            {
                await app.SaveCalendarAsync(incoming);
                return;
            }

            // Second, get a local calendar.
            Calendar calendar;
            try
            {
                calendar = await app.GetLocalCalendarAsync();
            }
            // Corrupted, save the online calendar and exit.
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception thrown while reading old calendar: {ex.Message}\n{ex}");
                await app.SaveCalendarAsync(incoming);
                return;
            }

            // Third, merge the online calendar into the local calendar.
            app.Merge(incoming, calendar);
            await app.SaveCalendarAsync(calendar);
        }
    }
}
