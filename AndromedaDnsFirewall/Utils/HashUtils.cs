using System;
using System.IO.Hashing;
using System.Runtime.InteropServices;

namespace AndromedaDnsFirewall.Utils;

static internal class HashUtils {

	public static int UltraFastSearch(UInt128[] sortedHashes, UInt128 key) {

		int n = sortedHashes.Length;

		if (n == 0) 
			return -1;

		// 1. Математический прогноз (0 обращений к памяти)
		double fraction = (double)key / (double)UInt128.MaxValue;
		int pos = (int)(fraction * (n - 1));

		// 2. Устанавливаем локальное окно (например, 512 элементов)
		// Это окно гарантированно попадет в L2/L3 кэш после первого обращения
		const int window = 4096;
		int low = Math.Max(0, pos - window/2);
		int high = Math.Min(n - 1, pos + window/2);

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
		if ((low == low_orig || high == high_orig) && low != 0 && high != n-1)  {
			// Окно не схлопнулось, но
			// если данные — честные хэши, этот блок почти никогда не выполнится (вероятность = 10^-8 для 500000 элементов)
			return Array.BinarySearch(sortedHashes, key);
		}

		return -1;
	}

	public static UInt128 QuickHash(ReadOnlySpan<byte> data) {
		Span<byte> buf = stackalloc byte[16];
		XxHash128.Hash(data, buf);
		return MemoryMarshal.Read<UInt128>(buf);
	}
	//public static UInt128 Murmur3(ReadOnlySpan<byte> data) {
	//	// Используем стандартные константы Murmur3
	//	ulong h1 = 0x9e3779b97f4a7c15UL;
	//	ulong h2 = 0x312e8fb04ad2ed3dUL;

	//	const ulong c1 = 0x87c37b91114253d5UL;
	//	const ulong c2 = 0x4cf5ad432745937fUL;

	//	int length = data.Length;
	//	ReadOnlySpan<ulong> blocks = MemoryMarshal.Cast<byte, ulong>(data);

	//	// Основной цикл по 16 байт (2 ulong)
	//	for (int i = 0; i < blocks.Length / 2; i++) {
	//		ulong k1 = blocks[i * 2];
	//		ulong k2 = blocks[i * 2 + 1];

	//		k1 *= c1; k1 = BitOperations.RotateLeft(k1, 31); k1 *= c2; h1 ^= k1;
	//		h1 = BitOperations.RotateLeft(h1, 27); h1 += h2; h1 = h1 * 5 + 0x52dce729;

	//		k2 *= c2; k2 = BitOperations.RotateLeft(k2, 33); k2 *= c1; h2 ^= k2;
	//		h2 = BitOperations.RotateLeft(h2, 31); h2 += h1; h2 = h2 * 5 + 0x38495ab5;
	//	}

	//	// Обработка "хвоста" (остаток < 16 байт)
	//	ReadOnlySpan<byte> tail = data.Slice((blocks.Length / 2) * 16);
	//	ulong t1 = 0, t2 = 0;

	//	switch (tail.Length) {
	//		case 15: t2 ^= (ulong)tail[14] << 48; goto case 14;
	//		case 14: t2 ^= (ulong)tail[13] << 40; goto case 13;
	//		case 13: t2 ^= (ulong)tail[12] << 32; goto case 12;
	//		case 12: t2 ^= (ulong)tail[11] << 24; goto case 11;
	//		case 11: t2 ^= (ulong)tail[10] << 16; goto case 10;
	//		case 10: t2 ^= (ulong)tail[9] << 8; goto case 9;
	//		case 9: t2 ^= (ulong)tail[8]; t2 *= c2; t2 = BitOperations.RotateLeft(t2, 33); t2 *= c1; h2 ^= t2; goto case 8;
	//		case 8: t1 ^= (ulong)tail[7] << 56; goto case 7;
	//		case 7: t1 ^= (ulong)tail[6] << 48; goto case 6;
	//		case 6: t1 ^= (ulong)tail[5] << 40; goto case 5;
	//		case 5: t1 ^= (ulong)tail[4] << 32; goto case 4;
	//		case 4: t1 ^= (ulong)tail[3] << 24; goto case 3;
	//		case 3: t1 ^= (ulong)tail[2] << 16; goto case 2;
	//		case 2: t1 ^= (ulong)tail[1] << 8; goto case 1;
	//		case 1: t1 ^= (ulong)tail[0]; t1 *= c1; t1 = BitOperations.RotateLeft(t1, 31); t1 *= c2; h1 ^= t1; break;
	//	}

	//	// Финализация
	//	h1 ^= (ulong)length; h2 ^= (ulong)length;
	//	h1 += h2; h2 += h1;

	//	h1 = Mix(h1); h2 = Mix(h2);
	//	h1 += h2; h2 += h1;

	//	return new UInt128(h2, h1);
	//}

	//private static ulong Mix(ulong k) {
	//	k ^= k >> 33;
	//	k *= 0xff51afd7ed558ccdUL;
	//	k ^= k >> 33;
	//	k *= 0xc4ceb9fe1a85ec53UL;
	//	k ^= k >> 33;
	//	return k;
	//}
}
