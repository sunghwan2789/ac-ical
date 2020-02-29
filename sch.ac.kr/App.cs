using System;
using System.Threading.Tasks;
using Ical.Net;

namespace sch_academic_calendar
{
    class App
    {
        public App(CalendarManager manager, CalendarService service)
        {
            Manager = manager;
            Service = service;
        }

        private CalendarManager Manager { get; }
        private CalendarService Service { get; }

        public async Task RunAsync()
        {
            // First, get an online calendar.
            Calendar incoming;
            try
            {
                incoming = await Service.GetCalendarAsync();
            }
            // An online calendar is required. Abort.
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception thrown while getting online calendar: {ex.Message}\n{ex}");
                throw;
            }

            // If a local calendar is clean, save the online calendar and exit.
            if (Manager.IsClean())
            {
                await Manager.SaveAsync(incoming);
                return;
            }

            // Second, get a local calendar.
            try
            {
                await Manager.LoadAsync();
            }
            // Corrupted, save the online calendar and exit.
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception thrown while reading old calendar: {ex.Message}\n{ex}");
                await Manager.SaveAsync(incoming);
                return;
            }

            // Third, merge the online calendar into the local calendar.
            Manager.Merge(incoming);
            await Manager.SaveAsync();
        }
    }
}
