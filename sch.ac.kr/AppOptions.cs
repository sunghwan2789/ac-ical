using System;
using Ical.Net.CalendarComponents;

namespace sch_academic_calendar
{
    class AppOptions
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
        public string? FileName { get; set; }

        /// <summary>
        /// Flag to load the original iCalendar or not.
        /// </summary>
        public bool LoadInput { get; set; } = true;
    }
}