using System;
using System.Text.RegularExpressions;
using System.Web;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace sch_academic_calendar
{
    class AcademicEvent
    {
        /// <summary>
        /// 참조 주소
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 제목
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 예정일
        /// </summary>
        public DateTime Begin { get; set; }

        /// <summary>
        /// 종료일
        /// </summary>
        /// <remarks>
        /// 기본 제공하지 않아서 <see cref="Content"/>에서 파싱합니다.
        /// </remarks>
        public DateTime? End
        {
            get
            {
                var match = Regex.Match(Content, @"^\s*(?:\d+\.\d+|\d+\/\d+|\d+)[^\d]+(\d+\.\d+|\d+\/\d+|\d+)");
                if (!match.Success)
                {
                    return null;
                }

                var monthDay = match.Groups[1].Value.Split('.', '/');
                var month = monthDay.Length > 1 ? int.Parse(monthDay[0]) : Begin.Month;
                var day = int.Parse(monthDay[monthDay.Length > 1 ? 1 : 0]);

                try
                {
                    var ret = new DateTime(Begin.Year, month, day);
                    if (month < Begin.Month)
                    {
                        ret = ret.AddYears(1);
                    }
                    if (day < ret.Day)
                    {
                        ret = ret.AddMonths(1);
                    }

                    if (ret < Begin)
                    {
                        return null;
                    }

                    return ret;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 내용
        /// </summary>
        public string Content { get; set; }

        public string Id => HttpUtility.ParseQueryString(new Uri(Url).Query)["article_no"];

        public CalendarEvent ToCalendarEvent()
        {
            return new CalendarEvent
            {
                Summary = Title,
                DtStart = new CalDateTime(Begin),
                DtEnd = new CalDateTime((End ?? Begin).AddDays(1)),
                Description = Content,
                Uid = Id,
            };
        }
    }
}
