using System;
using System.Threading.Tasks;

namespace sch.ac.kr
{
    public class App
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
            var incoming = await Service.GetCalendarAsync();

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
