using System;
using System.Collections.Generic;

namespace AndromedaDnsFirewall;

public static class IListExt {
	public static void RemoveLast<T>(this IList<T> list) {
		list.RemoveAt(list.Count - 1);
	}
	public static int RemoveAll<T>(this IList<T> list, Predicate<T> match) {
		int count = 0;

		for (int i = list.Count - 1; i >= 0; i--) {
			if (match(list[i])) {
				++count;
				list.RemoveAt(i);
			}
		}

		return count;
	}
}
