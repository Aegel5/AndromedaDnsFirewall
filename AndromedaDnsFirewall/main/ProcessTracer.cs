using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AndromedaDnsFirewall.main; 
internal static class ProcessTracer {

	static readonly List<ProcessLogItem> currentList = new();

	static Dictionary<int, ProcessInfo> PidsToInfo = new();

	static ProcessInfo GetProcessInfo(int pid) {
		if (!PidsToInfo.TryGetValue(pid, out var info)) {
			// внештатная ситуация не знаем про такой pid
			info = new ProcessInfo() { Name = $"<Error> pid:{pid}" };
			PidsToInfo.Add(pid, info);
			info.lastUpdate = TimePoint.Now;
		}
		return info;
	}

	static public void Start() {

		if (!ProgramUtils.IsElevated) 
			return;

		bool IsLoopBack(IPAddress sender, IPAddress daddr) {
			if (IPAddress.IsLoopback(daddr)) return true;
			//if (sender.Equals(daddr)) return true;
			return false;
		}

		Task.Run(() => {
			// Чтобы не пулить миллион объектов в очередь (UDP), будем собирать наш лог прямо в этом потоке
			// Будем использовать два листа - текущий и последний который мы отдаем на чтение.
			do {
				try {
					using var session = new TraceEventSession("AndromedaNetMonitor");
					// Включаем ядро для отслеживания сети
					session.EnableKernelProvider(
						KernelTraceEventParser.Keywords.NetworkTCPIP
						//| KernelTraceEventParser.Keywords.Process
						| KernelTraceEventParser.Keywords.ImageLoad
						);


					session.Source.Kernel.ProcessStart += (data) => {
						//data.ImageFileName
						//PidsToInfo[data.ProcessID] = new() { name = data.ProcessName };
					};
					//session.Source.Kernel.ProcessDCStart += (data) => {
					//	PidsToInfo[data.ProcessID] = new() { name = data.ProcessName };
					//};
					session.Source.Kernel.ImageDCStart += (data) => {
						if (data.FileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) {
							GetProcessInfo(data.ProcessID).fullPath = data.FileName;
						}
					};

					//session.Source.Kernel.ProcessStop += (data) => {
					//	PidsToInfo.Remove(data.ProcessID);
					//};

					//session.Source.Kernel.ProcessDCStop += (data) => { };

					// --- TCP: Ловим попытки установки соединений ---
					session.Source.Kernel.TcpIpConnect += (data) => {
						if (IsLoopBack(data.saddr, data.daddr))
							return;
						var cur = GetProcessInfo(data.ProcessID);
						cur.AddType(ProcessTraceType.TCPv4);
					};
					session.Source.Kernel.TcpIpConnectIPV6 += (data) => {
						bool isLoopback = data.saddr.Equals(data.daddr);
						if (isLoopback) return;
						var cur = GetProcessInfo(data.ProcessID);
						cur.AddType(ProcessTraceType.TcpV6);
					};
					// --- UDP: Ловим отправку пакетов (т.к. у UDP нет 'Connect') ---
					session.Source.Kernel.UdpIpSend += (data) => {
						bool isLoopback = data.saddr.Equals(data.daddr);
						if (isLoopback) return;
						var cur = GetProcessInfo(data.ProcessID);
						cur.AddType(ProcessTraceType.UdpV4);
					};
					session.Source.Kernel.UdpIpSendIPV6 += (data) => {
						bool isLoopback = data.saddr.Equals(data.daddr);
						if (isLoopback) return;
						var cur = GetProcessInfo(data.ProcessID);
						cur.AddType(ProcessTraceType.UdpV6);
					};

					// Запуск прослушивания
					session.Source.Process();
				} catch (Exception ex) {
					GuiTools.ShowMessageNoWait(ex.Message);
					Log.Err(ex);
				}
				//Thread.Sleep(1.min());
			} while (false);
		});
	}
}
