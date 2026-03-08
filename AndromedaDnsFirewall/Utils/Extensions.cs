using System;

namespace AndromedaDnsFirewall;
static class Ext_Double {
	public static int Round_to_int(this double a) => (int)Math.Round(a);

}

static class Ext_DateTime {
	public static DateTime ToLocalQuick(this DateTime dt) {
		if (dt.Kind != DateTimeKind.Utc) throw new Exception("need utc");
		return new DateTime(dt.Ticks + delt.Ticks, DateTimeKind.Local);
	}
	static TimeSpan delt;
	static Ext_DateTime() {
		delt = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
	}

}
