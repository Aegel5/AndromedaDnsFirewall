using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace AndromedaDnsFirewall.main;

enum ProcessTraceType  {
	TCPv4 = 0, 
	TcpV6 = 1, 
	UdpV4 = 2, 
	UdpV6 = 3
}

// Мульти-поточный класс - наш мостик между трейсером и остальной программой
// Пока после создания вообще никогда не удаляется
// Может читается и меняться параллельно.
class ProcessInfo {
	[InlineArray(4)] public struct TCounts { private int _firstElement; }

	public string Name = "";
	public string fullPath = "";
	public int LastPid {
		get => Volatile.Read(ref field);
		set => Interlocked.Exchange(ref field, value);
	}
	TCounts counts;
	static long nextId = 15;
	public long Id = nextId++;
	public TimePoint lastUpdate;

	public void SetNowUpdated() {
		lastUpdate.AssignSafe(TimePoint.Now);
	}

	public void AddType(ProcessTraceType type) {
		Interlocked.Increment(ref counts[(int)type]);
	}

	public override string ToString() {
		// пока используем строку
		string build(ProcessTraceType t) {
			var cnt = counts[(int)ProcessTraceType.TCPv4];
			return cnt == 0 ? "" : $" {t}({cnt})";
		}
		return $"{Name}, {fullPath}{build(ProcessTraceType.TCPv4)}{build(ProcessTraceType.TcpV6)}{build(ProcessTraceType.UdpV4)}{build(ProcessTraceType.UdpV6)}";
	}
}
