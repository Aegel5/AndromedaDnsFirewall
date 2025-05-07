using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall;

public static class TimespanExtensions
{
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
