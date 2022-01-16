using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.Utils
{
    public static class TimespanExtensions
    {
        public static string ToHumanReadableString(this TimeSpan t)
        {
            if (t.TotalMinutes <= 1)
            {
                return $@"{t:%s} sec";
            }
            if (t.TotalHours <= 1)
            {
                return $@"{t:%m} min";
            }
            if (t.TotalDays <= 1)
            {
                return $@"{t:%h} hours";
            }

            return $@"{t:%d} days";
        }

        public static TimeSpan sec(this int t)
        {
            return TimeSpan.FromSeconds(t);
        }

        public static TimeSpan min(this int t)
        {
            return TimeSpan.FromMinutes(t);
        }

        public static TimeSpan msec(this int t)
        {
            return TimeSpan.FromMilliseconds(t);
        }
    }
}
