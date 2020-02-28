using System;
using Ical.Net.CalendarComponents;

namespace sch_academic_calendar
{
    class CalendarServiceOptions
    {
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