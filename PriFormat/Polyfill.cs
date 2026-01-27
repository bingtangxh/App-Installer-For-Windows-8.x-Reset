using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PriFormat
{
	public static class Polyfill
	{
		public static string ReadString (this BinaryReader reader, Encoding encoding, int length)
		{
			//byte [] data = reader.ReadBytes (length * encoding.GetByteCount ("a"));
			//return encoding.GetString (data, 0, data.Length);
			// ==========
			if (length <= 0) return string.Empty;
			int maxBytes = encoding.GetMaxByteCount (length);
			byte [] buffer = reader.ReadBytes (maxBytes);
			if (buffer.Length == 0) return string.Empty;
			string decoded = encoding.GetString (buffer, 0, buffer.Length);
			if (decoded.Length > length) decoded = decoded.Substring (0, length);
			return decoded;
		}
		public static string ReadNullTerminatedString (this BinaryReader reader, Encoding encoding)
		{
			MemoryStream ms = new MemoryStream ();
			while (true)
			{
				byte b1 = reader.ReadByte ();
				byte b2 = reader.ReadByte ();

				if (b1 == 0 && b2 == 0)
					break;

				ms.WriteByte (b1);
				ms.WriteByte (b2);
			}
			return encoding.GetString (ms.ToArray ());
			// ==========
			List<byte> bytes = new List<byte> ();
			byte b;
			while ((b = reader.ReadByte ()) != 0) bytes.Add (b);
			return encoding.GetString (bytes.ToArray ());
		}
		public static void ExpectByte (this BinaryReader reader, byte expectedValue)
		{
			if (reader.ReadByte () != expectedValue) throw new InvalidDataException ("Unexpected value read.");
		}
		public static void ExpectUInt16 (this BinaryReader reader, ushort expectedValue)
		{
			if (reader.ReadUInt16 () != expectedValue) throw new InvalidDataException ("Unexpected value read.");
		}
		public static void ExpectUInt32 (this BinaryReader reader, uint expectedValue)
		{
			if (reader.ReadUInt32 () != expectedValue) throw new InvalidDataException ("Unexpected value read.");
		}
		public static void ExpectString (this BinaryReader reader, string s)
		{
			if (new string (reader.ReadChars (s.Length)) != s) throw new InvalidDataException ("Unexpected value read.");
		}

	}
}
