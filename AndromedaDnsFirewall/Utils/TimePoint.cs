using System;
using System.Diagnostics;
using System.Threading;

namespace AndromedaDnsFirewall;

// используем наш специальный класс вместо DateTime
// из-за того что DateTime может резко изменяться после перевода системного времени
// + DateTime - довольно тяжелый класс сам по себе.
internal record struct TimePoint : IComparable<TimePoint> {
	private long _ticks;
	public long Ticks { get => _ticks; } // alwasy pos
	public TimePoint() { }
	public TimePoint(long ticks) {
		_ticks = ticks;
	}
	static Stopwatch timer = Stopwatch.StartNew();
	static readonly long safe_delt_ticks = TimeSpan.FromDays(3000).Ticks;// нам нужен запас значений от 0 иначем некторые условия могут не срабатывать на старте работы
	public static TimePoint Now => new(timer.ElapsedTicks + safe_delt_ticks);

	public static readonly TimePoint MinValue = default;
	public static readonly TimePoint MaxValue = new(DateTime.MaxValue.Ticks);
	public int CompareTo(TimePoint other) => Ticks.CompareTo(other.Ticks);
	public TimeSpan Minus(TimePoint other) => TimeSpan.FromTicks(Math.Abs(Ticks - other.Ticks));

	public TimeSpan DeltToNow => Minus(Now);
	public TimePoint Add(TimeSpan ts) => new(Math.Max(Ticks + ts.Ticks, 0));

	public static bool operator <(TimePoint a, TimePoint b) => a.Ticks < b.Ticks;
	public static bool operator >(TimePoint a, TimePoint b) => a.Ticks > b.Ticks;
	public static bool operator >=(TimePoint a, TimePoint b) => a.Ticks >= b.Ticks;
	public static bool operator <=(TimePoint a, TimePoint b) => a.Ticks <= b.Ticks;

	public void AssignSafe(TimePoint other) {
		Interlocked.Exchange(ref _ticks, other._ticks);
	}
}

