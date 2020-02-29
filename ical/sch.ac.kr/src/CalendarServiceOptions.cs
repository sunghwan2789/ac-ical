using System;
using Ical.Net.CalendarComponents;

namespace sch.ac.kr
{
    public class CalendarServiceOptions
    {
        /// <summary>
        /// Maximum value of elapsed time since <see cref="CalendarEvent.DtStart"/>
        /// to <see cref="DateTime.Today"/>.
        /// </summary>
        public TimeSpan MaximumElapsedTimeSinceDtStartToToday { get; set; }

        /// <summary>
        /// Minimum value of <see cref="CalendarEvent.DtStart"/>.
        /// </summary>
        /// <remarks>
        /// Overrides <see cref="MaximumElapsedTimeSinceDtStartToToday"/>.
        /// </remarks>
        public DateTime? MinimumDtStart { get; set; }
    }
}