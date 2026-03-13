using AndromedaDnsFirewall.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace AndromedaDnsFirewall.main; 
internal class ProcessListModel {

	static readonly ObservableDeque<ProcessInfo> deque = new();

	// выходная дорожка из класса
	public static IEnumerable<ProcessInfo> ModelBinding => deque;

	// входная и единственная дорожка в класс
	static public void NotifyChanged(ProcessInfo info) {
		// Алогоритм пока простой: ищем запись в первых 20 записях. Нашли - обновляем, нет - вставляем в начало (могут быть дубликаты это норм
		for (int i = 0; i < Math.Min(20, deque.Count); i++) {
			if (deque[i].Id == info.Id) {
				// нашли, просто обновляем
				deque.NotifyUpdated(i);
				return;
			}
		}
		// станартная вставка
		deque.PushFrontNotify(info);
		while (deque.Count > 100) {
			deque.PopBackNofity();
		}
	}
}
