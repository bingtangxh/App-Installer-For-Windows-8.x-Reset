using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Update
{
	static class Program
	{
		static string EscapeArgument (string arg)
		{
			if (arg.Length == 0) return "\"\"";
			bool needQuotes = arg.Any (char.IsWhiteSpace) || arg.Contains ('"');
			if (!needQuotes) return arg;
			var sb = new StringBuilder ();
			sb.Append ('"');
			int backslashes = 0;
			foreach (char c in arg)
			{
				if (c == '\\')
				{
					backslashes++;
				}
				else if (c == '"')
				{
					sb.Append ('\\', backslashes * 2 + 1);
					sb.Append ('"');
					backslashes = 0;
				}
				else
				{
					sb.Append ('\\', backslashes);
					sb.Append (c);
					backslashes = 0;
				}
			}
			sb.Append ('\\', backslashes * 2);
			sb.Append ('"');
			return sb.ToString ();
		}
		/// <summary>
		/// 应用程序的主入口点。
		/// </summary>
		[STAThread]
		static void Main (string [] args)
		{
			Process p = new Process ();
			p.StartInfo.FileName = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "settings.exe");
			var arguments = new List<string>
			{
				"appinstaller",
				"update"
			};
			arguments.AddRange (args);
			p.StartInfo.Arguments = string.Join (" ", arguments.Select (EscapeArgument));
			p.Start (); 
			p.WaitForExit (); 
			int exitCode = p.ExitCode; 
		}
	}
}
