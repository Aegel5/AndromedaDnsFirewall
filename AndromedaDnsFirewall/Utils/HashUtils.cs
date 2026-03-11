global using QuickHashType = System.UInt64;
using System;
using System.IO.Hashing;
using System.Runtime.InteropServices;


namespace AndromedaDnsFirewall.Utils;

static internal class HashUtils {

	public static int UltraFastSearch(QuickHashType[] sortedHashes, QuickHashType key) {

		int n = sortedHashes.Length;

		if (n == 0)
			return -1;

		// 1. Математический прогноз (0 обращений к памяти)
		double fraction = (double)key / (double)QuickHashType.MaxValue;
		int pos = (int)(fraction * (n - 1));

		// 2. Устанавливаем локальное окно (например, 512 элементов)
		// Это окно гарантированно попадет в L2/L3 кэш после первого обращения
		const int window = 4096;
		int low = Math.Max(0, pos - window / 2);
		int high = Math.Min(n - 1, pos + window / 2);

		int low_orig = low;
		int high_orig = high;

		// 3. Сразу запускаем бинарный поиск в этом окне
		while (low <= high) {
			int mid = (low + high) >> 1;
			var midVal = sortedHashes[mid];

			if (midVal == key)
				return mid;

			if (midVal < key) {
				low = mid + 1;
			} else {
				high = mid - 1;
			}
		}

		// Не нашли
		if ((low == low_orig || high == high_orig) && low != 0 && high != n - 1) {
			// Окно не схлопнулось, но
			// если данные — честные хэши, этот блок почти никогда не выполнится (вероятность = 10^-8 для 500000 элементов)
			return Array.BinarySearch(sortedHashes, key);
		}

		return -1;
	}

	private static readonly long _seed = new Random().NextInt64();

	public static QuickHashType QuickHash(ReadOnlySpan<byte> data) {
		Span<byte> buf = stackalloc byte[sizeof(QuickHashType)];
		if (sizeof(QuickHashType) == 8) XxHash64.Hash(data, buf, _seed);
		else if (sizeof(QuickHashType) == 16) XxHash128.Hash(data, buf, _seed);
		else throw new NotSupportedException();
		return MemoryMarshal.Read<QuickHashType>(buf);
	}
}
