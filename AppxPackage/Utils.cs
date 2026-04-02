using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

internal static class Utils
{
	public static string PascalToCamel (string pascalCase)
	{
		if (string.IsNullOrEmpty (pascalCase))
			return pascalCase;

		// 已经是小驼峰或首字母小写，直接返回
		if (char.IsLower (pascalCase [0]))
			return pascalCase;

		// 按大写字母边界拆分单词（处理连续大写字母作为一个单词）
		// 正则解释：
		//   (?<=[a-z])(?=[A-Z])         小写后跟大写 -> 分割
		//   |                           或
		//   (?<=[A-Z])(?=[A-Z][a-z])    大写后跟大写+小写（如 XMLR 中的 X 和 MLR）-> 分割
		string [] words = Regex.Split (pascalCase, @"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])");

		StringBuilder result = new StringBuilder ();

		for (int i = 0; i < words.Length; i++)
		{
			string word = words [i];
			if (string.IsNullOrEmpty (word))
				continue;

			if (i == 0)
			{
				// 第一个单词：全部转为小写
				result.Append (word.ToLowerInvariant ());
			}
			else
			{
				// 后续单词：首字母大写，其余小写
				result.Append (char.ToUpperInvariant (word [0]));
				if (word.Length > 1)
					result.Append (word.Substring (1).ToLowerInvariant ());
			}
		}

		return result.ToString ();
	}
	/// <summary>
	/// 表示应用清单中用于定义文件/资源路径的属性名称
	/// </summary>
	public static readonly List<string> AppFileProperties = new List<string>
	{
		"Executable",
		"LockScreenLogo",
		"Logo",
		"SmallLogo",
		"Square150x150Logo",
		"Square30x30Logo",
		"Square310x310Logo",
		"Square44x44Logo",
		"Square70x70Logo",
		"Square71x71Logo",
		"StartPage",
		"Tall150x310Logo",
		"WideLogo",
		"Wide310x150Logo"
	};
}