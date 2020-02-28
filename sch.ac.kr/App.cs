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
    class App
    {
        public App(AppOptions options, Bot bot)
        {
            Options = options;
            Bot = bot;
        }

        private AppOptions Options { get; }
        private Bot Bot { get; }

        public async Task<Calendar> GetOnlineCalendarAsync()
        {
            var calendar = new Calendar();
            try
            {
                await Bot.GetCalendarEventsAsync().ForEachAsync(calendar.Events.Add);
            }
            // If exception occurs, just use some events we got before the exception.
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

        public Task SaveCalendarAsync(Calendar calendar)
        {
            var filename = Options.FileName ?? Options.OutputFileName;
            using var stream = filename == null
                ? Console.OpenStandardOutput()
                : File.OpenWrite(filename);
            using var writer = new StreamWriter(stream);
            return writer.WriteAsync(new CalendarSerializer(calendar).SerializeToString());
        }

        public async Task<Calendar> GetOfflineCalendarAsync()
        {
            var filename = Options.FileName ?? Options.InputFileName;
            return Calendar.Load(await File.ReadAllTextAsync(filename));
        }
    }
}
