using System;
using Ical.Net.CalendarComponents;

namespace sch_academic_calendar
{
    class BotOptions
    {
        /// <summary>
        /// Filename of the original iCalendar.
        /// </summary>
        /// <remarks>
        /// If this is <c>null</c>, <see cref="Bot"/> will not load the original iCalendar.
        /// </remarks>
        public string? InputFileName { get; set; }

        /// <summary>
        /// Filename to save the new iCalendar.
        /// </summary>
        /// <remarks>
        /// If this is <c>null</c>, <see cref="Bot"/> will write the new iCalendar to stdout.
        /// </remarks>
        public string? OutputFileName { get; set; }

        /// <summary>
        /// Filename of the iCalendar to be synchronized.
        /// </summary>
        /// <remarks>
        /// Overrides <see cref="InputFilename"/> and <see cref="OutputFilename"/>.
        /// </remarks>
        public string? Filename { get; set; }

        /// <summary>
        /// Flag to load the original iCalendar or not.
        /// </summary>
        public bool LoadInput { get; set; } = true;

        /// <summary>
        /// Minimum value of elapsed time since <see cref="CalendarEvent.DtStart"/>
        /// to <see cref="DateTime.Today"/>.
        /// </summary>
        public TimeSpan MinimumElapsedTimeSinceDtStartToToday { get; set; } = TimeSpan.FromDays(60);

        /// <summary>
        /// Minimum value of <see cref="CalendarEvent.DtStart"/>.
        /// </summary>
        /// <remarks>
        /// Overrides <see cref="LowerBoundOfTimeSpanFromTodayToDtStart"/>.
        /// </remarks>
        public DateTime? MinimumDtStart { get; set; }
    }
}