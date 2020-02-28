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
            var manager = new CalendarManager(new CalendarManagerOptions
            {
                FileName = args.FirstOrDefault(),
            });
            var service = new CalendarService(new CalendarServiceOptions());

            // First, get an online calendar.
            Calendar incoming;
            try
            {
                incoming = await service.GetCalendarAsync();
            }
            // An online calendar is required. Abort.
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception thrown while getting online calendar: {ex.Message}\n{ex}");
                Environment.ExitCode = 1;
                return;
            }

            // If a local calendar is clean, save the online calendar and exit.
            if (manager.IsClean())
            {
                await manager.SaveAsync(incoming);
                return;
            }

            // Second, get a local calendar.
            try
            {
                await manager.LoadAsync();
            }
            // Corrupted, save the online calendar and exit.
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception thrown while reading old calendar: {ex.Message}\n{ex}");
                await manager.SaveAsync(incoming);
                return;
            }

            // Third, merge the online calendar into the local calendar.
            manager.Merge(incoming);
            await manager.SaveAsync();
        }
    }
}
