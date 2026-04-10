using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace System
{
	namespace Text
	{
		public static class Escape
		{
			/// <summary>按 JSON 规范对字符串进行转义。</summary>
			/// <param name="input">原始字符串。</param>
			/// <returns>转义后的 JSON 字符串字面量内容（不含外围双引号）。</returns>
			public static string ToEscape (string input)
			{
				if (input == null) throw new ArgumentNullException (nameof (input));
				if (input.Length == 0) return string.Empty;

				StringBuilder sb = new StringBuilder (input.Length);
				foreach (char c in input)
				{
					switch (c)
					{
						case '"': sb.Append ("\\\""); break;
						case '\\': sb.Append ("\\\\"); break;
						case '\b': sb.Append ("\\b"); break;
						case '\f': sb.Append ("\\f"); break;
						case '\n': sb.Append ("\\n"); break;
						case '\r': sb.Append ("\\r"); break;
						case '\t': sb.Append ("\\t"); break;
						default:
							// 控制字符 (U+0000 - U+001F) 需转义为 \uXXXX
							if (c <= 0x1F)
							{
								sb.Append ("\\u");
								sb.Append (((int)c).ToString ("X4"));
							}
							else
							{
								sb.Append (c);
							}
							break;
					}
				}
				return sb.ToString ();
			}
			/// <summary>按 JSON 规范反转义字符串。</summary>
			/// <param name="input">转义后的 JSON 字符串内容（不含外围双引号）。</param>
			/// <returns>原始字符串。</returns>
			/// <exception cref="FormatException">遇到非法转义序列时抛出。</exception>
			public static string Unescape (string input)
			{
				if (input == null) throw new ArgumentNullException (nameof (input));
				if (input.Length == 0) return string.Empty;

				StringBuilder sb = new StringBuilder (input.Length);
				int i = 0;
				while (i < input.Length)
				{
					char c = input [i];
					if (c == '\\')
					{
						i++;
						if (i >= input.Length)
							throw new FormatException ("字符串末尾包含不完整的转义序列。");

						char next = input [i];
						switch (next)
						{
							case '"': sb.Append ('"'); break;
							case '\\': sb.Append ('\\'); break;
							case '/': sb.Append ('/'); break;      // 允许转义斜杠
							case 'b': sb.Append ('\b'); break;
							case 'f': sb.Append ('\f'); break;
							case 'n': sb.Append ('\n'); break;
							case 'r': sb.Append ('\r'); break;
							case 't': sb.Append ('\t'); break;
							case 'u':
								i++;
								if (i + 4 > input.Length)
									throw new FormatException ("\\u 转义后必须跟随 4 位十六进制数字。");

								string hex = input.Substring (i, 4);
								int codePoint; // 先声明变量，兼容 C# 5/6
								if (!int.TryParse (hex,
										NumberStyles.HexNumber,
										CultureInfo.InvariantCulture,
										out codePoint))
								{
									throw new FormatException (string.Format ("无效的 Unicode 转义序列: \\u{0}", hex));
								}

								sb.Append ((char)codePoint);
								i += 3; // 循环末尾会再加1，因此这里只增加3
								break;
							default:
								throw new FormatException (string.Format ("未识别的转义序列: \\{0}", next));
						}
					}
					else
					{
						sb.Append (c);
					}
					i++;
				}
				return sb.ToString ();
			}
		}
	}
}
