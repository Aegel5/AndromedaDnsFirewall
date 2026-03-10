using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace AndromedaDnsFirewall.Utils;

internal sealed class HashSetUtf8 {

	private readonly FrozenSet<byte[]> _set;
	private readonly FrozenSet<byte[]>.AlternateLookup<ReadOnlySpan<char>> _charLookup;

	public int Count => _set.Count;

	public HashSetUtf8(IEnumerable<byte[]> items) {
		_set = items.ToFrozenSet(Utf8Comparer.Instance);
		_charLookup = _set.GetAlternateLookup<ReadOnlySpan<char>>();
	}
	public HashSetUtf8() : this(Enumerable.Empty<byte[]>()) { }

	public bool ContainsAsci(ReadOnlySpan<char> chars) => _charLookup.Contains(chars);

	private sealed class Utf8Comparer
		: IEqualityComparer<byte[]>,
		IAlternateEqualityComparer<ReadOnlySpan<char>, byte[]> {
		public static readonly Utf8Comparer Instance = new();

		public bool Equals(byte[]? x, byte[]? y) => x != null && y != null && x.AsSpan().SequenceEqual(y);

		public int GetHashCode(byte[] obj) {
			//var hash = new HashCode();
			//hash.AddBytes(obj.AsSpan());
			//var res1 = hash.ToHashCode();

			var hash2 = new HashCode();
			foreach (var c in obj) hash2.Add(c);
			var res2 = hash2.ToHashCode();

			return res2;
		}

		public bool Equals(ReadOnlySpan<char> alternate, byte[] obj) {
			if (alternate.Length != obj.Length) return false;
			// Быстрое сравнение ASCII: char == byte
			for (int i = 0; i < alternate.Length; i++)
				if (alternate[i] != (char)obj[i]) return false;
			return true;
		}
		public int GetHashCode(ReadOnlySpan<char> alternate) {
			var hash = new HashCode();
			foreach (var c in alternate) hash.Add((byte)c);
			return hash.ToHashCode();
		}

		public byte[] Create(ReadOnlySpan<char> alternate) => throw new NotImplementedException();
	}
}

