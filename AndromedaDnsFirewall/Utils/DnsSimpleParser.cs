using System;
using System.Buffers.Binary;
using System.Text;

namespace AndromedaDnsFirewall.Utils;

internal static class DnsSimpleParser {
	public class Quest {
		public string domain;
		public int type;
	}
	public static ushort QuestCount(ReadOnlySpan<byte> bytes) {
		if (bytes.Length < 6) return 0;
		return read16(bytes.Slice(4));
	}
	public static (string, int) ReadString(ReadOnlySpan<byte> bytes) {

		StringBuilder sb = new();

		(bool, int) read(ReadOnlySpan<byte> bytes, int pos, int recursion_count = 0) {

			if (recursion_count >= 10) throw new Exception("recursion limit");

			var len = bytes[pos++];

			if ((len & 0xC0) == 0xC0) {
				throw new NotImplementedException();
				var cpointer = (len ^ 0xC0) << 8 | bytes[pos++];
				if (cpointer >= pos) throw new Exception("only backward pointers allowed");
				read(bytes, cpointer, recursion_count + 1);
			} else {
				if (len == 0) return (false, pos);
				for (int i = 0; i < len; i++) {
					var c = (char)bytes[pos++];
					sb.Append(char.ToLower(c));
				}
				sb.Append('.');
			}
			return (true, pos);
		}

		int pos = 0;
		while (true) {
			var (ok, newPos) = read(bytes, pos);
			pos = newPos;
			if (!ok) break;
		}
		if (sb.Length > 0) {
			sb.Remove(sb.Length - 1, 1);
		}
		return (sb.ToString(), pos);
	}
	public static Quest ReadFirstQuest(ReadOnlySpan<byte> bytes) {
		bytes = bytes.Slice(12);
		var res = new Quest();
		var (str, pos) = ReadString(bytes);
		res.domain = str;
		bytes = bytes.Slice(pos);
		res.type = read16(bytes);
		return res;
	}

	static ushort read16(ReadOnlySpan<byte> bytes) => BinaryPrimitives.ReadUInt16BigEndian(bytes);
	static void write16(Span<byte> bytes, ushort v) => BinaryPrimitives.WriteUInt16BigEndian(bytes, v);

	public static ushort ReadId(ReadOnlySpan<byte> bytes) {
		if (bytes.Length < 2) throw new Exception("bytes too small");
		return read16(bytes);
	}
	public static void WriteId(Span<byte> bytes, ushort id) {
		if (bytes.Length < 2) throw new Exception("bytes too small");
		write16(bytes, id);
	}

	public static void SetFlag(Span<byte> bytes, ushort flag) {
		var b = bytes.Slice(2);
		write16(b, (ushort)(flag | read16(b)));
	}

	public static ushort ReadFlag(ReadOnlySpan<byte> bytes) {
		return read16(bytes.Slice(2));
	}

	public static byte[] GenerateEmptyResponse(ushort id, byte status = 2) {
		var res = new byte[12];
		WriteId(res, id);
		SetFlag(res, 0x8000); // response
		SetFlag(res, status); // status
		return res;
	}

	public static bool IsQuery(ReadOnlySpan<byte> bytes) {
		return (ReadFlag(bytes) & 0x8000) == 0;
	}

	public static byte OpCode(ReadOnlySpan<byte> bytes) {
		return (byte)((ReadFlag(bytes) & 0x7800) >> 11);
	}

}
