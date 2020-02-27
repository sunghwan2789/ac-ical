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
        public App(AppOptions options)
        {
            Options = options;
        }

        private AppOptions Options { get; }
    }
}
