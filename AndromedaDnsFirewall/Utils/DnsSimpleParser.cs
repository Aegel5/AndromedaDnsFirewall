using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace AndromedaDnsFirewall.Utils; 
internal static class DnsSimpleParser {
	public class Quest {
		public string domain;
		public int type;
	}
	public static ushort QuestCount(ReadOnlySpan<byte> bytes) {
		// 32 бита = 4 байта. Чтобы прочитать еще 2 байта, нужно минимум 6.
		if (bytes.Length < 6) return 0;

		// Пропускаем 4 байта (32 бита) и читаем ushort (2 байта)
		return BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(4));
	}
	public static Quest? ReadFirstQuest(ReadOnlySpan<byte> bytes) {
		if (QuestCount(bytes) == 0) return null;
		return null;
	}

	public static ushort ReadId(ReadOnlySpan<byte> bytes) {
		if (bytes.Length < 2) throw new Exception("bytes too small");
		return BinaryPrimitives.ReadUInt16LittleEndian(bytes);
	}
	public static void WriteId(Span<byte> bytes, ushort id) {
		if (bytes.Length < 2) throw new Exception("bytes too small");
		BinaryPrimitives.WriteUInt32LittleEndian(bytes, id);
	}

}
