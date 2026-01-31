using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Launch
{
	static class Program
	{

		/// <summary>
		/// 从 args[startIndex..] 生成安全的命令行字符串
		/// </summary>
		private static string BuildCommandLine (string [] args, int startIndex)
		{
			if (args.Length <= startIndex) return null;
			var sb = new StringBuilder ();
			for (int i = startIndex; i < args.Length; i++)
			{
				if (i > startIndex) sb.Append (' ');
				sb.Append (EscapeArgument (args [i]));
			}
			return sb.ToString ();
		}
		/// <summary>
		/// 按 Win32 命令行规则转义单个参数
		/// </summary>
		private static string EscapeArgument (string arg)
		{
			if (string.IsNullOrEmpty (arg)) return "\"\"";
			bool needQuotes = false;
			foreach (char c in arg)
			{
				if (char.IsWhiteSpace (c) || c == '"')
				{
					needQuotes = true;
					break;
				}
			}
			if (!needQuotes) return arg;
			var sb = new StringBuilder ();
			sb.Append ('"');
			foreach (char c in arg)
			{
				if (c == '"') sb.Append ("\\\"");
				else sb.Append (c);
			}
			sb.Append ('"');
			return sb.ToString ();
		}
		/// <summary>
		/// 应用程序的主入口点。
		/// </summary>
		[STAThread]
		static void Main (string [] args)
		{
			if (args == null || args.Length == 0)
			{
				MessageBox.Show ("Missing AppUserModelId.", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			string appUserModelId = args [0];
			string argumentLine = BuildCommandLine (args, 1);
			AppxPackage.PackageManager.ActiveApp (appUserModelId, string.IsNullOrEmpty (argumentLine) ? null : argumentLine);
		}
	}
}
