using System;

namespace sch_academic_calendar
{
    class AppOptions
    {
        /// <summary>
        /// Filename of the old iCalendar.
        /// </summary>
        /// <remarks>
        /// If this is <c>null</c>, <see cref="App"/> will not load the old iCalendar.
        /// </remarks>
        public string? InputFileName { get; set; }

        /// <summary>
        /// Filename to save the new iCalendar.
        /// </summary>
        /// <remarks>
        /// If this is <c>null</c>, <see cref="App"/> will write the new iCalendar to stdout.
        /// </remarks>
        public string? OutputFileName { get; set; }

        /// <summary>
        /// Filename of the iCalendar to be synchronized.
        /// </summary>
        /// <remarks>
        /// Overrides <see cref="InputFileName"/> and <see cref="OutputFileName"/>.
        /// </remarks>
        public string? FileName { get; set; }

        /// <summary>
        /// Flag to load the old iCalendar or not.
        /// </summary>
        /// <remarks>
        /// If this is <c>false</c>, <see cref="InputFileName"/> will be ignored.
        /// </remarks>
        public bool LoadInput { get; set; } = true;
    }
}