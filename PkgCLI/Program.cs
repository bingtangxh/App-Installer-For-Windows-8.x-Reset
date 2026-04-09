using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataUtils;
using AppxPackage;
using System.IO;
using static AppxPackage.PackageManager;
using Newtonsoft.Json;
using Win32;

namespace PkgCLI
{
	class Program
	{
		public delegate _I_HResult PackageOperation (string filePath, IEnumerable<string> depUris, DeploymentOptions options, PackageProgressCallback progress = null);
		static readonly string [] helpArgs = new string [] {
			"/?",
			"-?",
			"help",
			"/help",
			"-help",
			"/h",
			"-h"
		};
		public static StringResXmlDoc res = new StringResXmlDoc (Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "locale\\pkgcli.xml"));
		static bool IsHelpParam (string arg) => helpArgs.Contains (arg.Normalize ());
		static void PrintVersion ()
		{
			var verFilePath = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "version");
			var verFileInst = new _I_File (verFilePath);
			var verstr = verFileInst.Content?.Trim () ?? "0.0.0.1";
			Console.WriteLine (String.Format (res.Get ("PKGCLI_VERSION"), verstr));
		}
		static void PrintTotalHelp ()
		{
			Console.WriteLine (res.Get ("PKGCLI_TOTALHELP"));
		}
		public static bool IsFilePathInList (List<string> filelist, string file)
		{
			foreach (var f in filelist)
			{
				if (f.NEquals (file)) return true;
			}
			return false;
		}
		public static void AddFiles (List<string> filelist, string file)
		{
			if (string.IsNullOrWhiteSpace (file)) return;
			if (!File.Exists (file)) file = Path.Combine (Environment.CurrentDirectory, file);
			if (!File.Exists (file)) return;
			var ext = Path.GetExtension (file);
			if (ext.NEquals (".txt"))
			{
				var lines = File.ReadAllLines (file);
				foreach (var l in lines)
				{
					if (string.IsNullOrWhiteSpace (l)) continue;
					var line = l.Trim ();
					if (!File.Exists (l)) line = Path.Combine (Path.GetDirectoryName (file), l);
					if (!File.Exists (line)) continue;
					if (!IsFilePathInList (filelist, line))
						filelist.Add (line);
				}
			}
			else if (!IsFilePathInList (filelist, file)) filelist.Add (file);
		}
		public static void AddNStringItems (List<string> strlist, string str)
		{
			var find = false;
			foreach (var l in strlist)
				if (l.NEquals (str))
				{
					find = true;
					break;
				}
			if (!find) strlist.Add (str);
		}
		public static void ToFormatString (PMPackageInfo pkg, IEnumerable<string> filter)
		{
			if (pkg == null) return;
			var labels = new List<string> ();
			var values = new List<string> ();

			var id = pkg.Identity;
			labels.Add ("Identity:Name"); values.Add (id.Name);
			labels.Add ("Identity:Publisher"); values.Add (id.Publisher);
			labels.Add ("Identity:FamilyName"); values.Add (id.FamilyName);
			labels.Add ("Identity:FullName"); values.Add (id.FullName);
			labels.Add ("Identity:PublisherId"); values.Add (id.PublisherId);
			labels.Add ("Identity:ResourceId"); values.Add (id.ResourceId);
			labels.Add ("Identity:Version"); values.Add (id.Version?.ToString () ?? "");
			labels.Add ("Identity:ProcessArchitecture"); values.Add (string.Join (", ", id.ProcessArchitecture));

			var prop = pkg.Properties;
			labels.Add ("Properties:DisplayName"); values.Add (prop.DisplayName);
			labels.Add ("Properties:Description"); values.Add (prop.Description);
			labels.Add ("Properties:Publisher"); values.Add (prop.Publisher);
			labels.Add ("Properties:Framework"); values.Add (prop.Framework.ToString ());
			labels.Add ("Properties:ResourcePackage"); values.Add (prop.ResourcePackage.ToString ());
			labels.Add ("Properties:Logo"); values.Add (prop.Logo);

			labels.Add ("IsBundle"); values.Add (pkg.IsBundle.ToString ());
			labels.Add ("DevelopmentMode"); values.Add (pkg.DevelopmentMode.ToString ());
			labels.Add ("InstallLocation"); values.Add (pkg.InstallLocation);
			labels.Add ("Users"); values.Add (string.Join ("; ", pkg.Users));

			Console.WriteLine ($"[{pkg.Identity.FullName}]");
			var indicesToOutput = new List<int> ();
			bool outputAll = (filter == null || !filter.Any ());

			for (int i = 0; i < labels.Count; i++)
			{
				if (outputAll)
				{
					indicesToOutput.Add (i);
				}
				else
				{
					string label = labels [i];
					foreach (string patternRaw in filter)
					{
						if (MatchPattern (label, patternRaw))
						{
							indicesToOutput.Add (i);
							break;
						}
					}
				}
			}

			if (indicesToOutput.Count == 0) return;
			int maxLabelLen = 0;
			foreach (int i in indicesToOutput)
				if (labels [i].Length > maxLabelLen) maxLabelLen = labels [i].Length;
			string format = "{0,-" + maxLabelLen + "} = {1}";
			foreach (int i in indicesToOutput)
				Console.WriteLine (format, labels [i], values [i]);
		}
		/// <summary>
		/// 匹配模式（忽略大小写，首尾空格，支持通配符 *）
		/// </summary>
		private static bool MatchPattern (string text, string pattern)
		{
			if (string.IsNullOrEmpty (pattern)) return true;
			pattern = pattern.Trim ();
			text = text ?? "";

			if (pattern == "*") return true;
			// 忽略大小写
			var comparison = StringComparison.OrdinalIgnoreCase;

			// 检查通配符位置
			bool startsWithStar = pattern.StartsWith ("*");
			bool endsWithStar = pattern.EndsWith ("*");

			if (startsWithStar && endsWithStar)
			{
				// *middle*  包含子串
				string middle = pattern.Substring (1, pattern.Length - 2);
				return text.IndexOf (middle, comparison) >= 0;
			}
			else if (startsWithStar)
			{
				// *suffix   以 suffix 结尾
				string suffix = pattern.Substring (1);
				return text.EndsWith (suffix, comparison);
			}
			else if (endsWithStar)
			{
				// prefix*   以 prefix 开头
				string prefix = pattern.Substring (0, pattern.Length - 1);
				return text.StartsWith (prefix, comparison);
			}
			else
			{
				// 精确匹配（忽略大小写）
				return string.Equals (text, pattern, comparison);
			}
		}
		/// <summary>
		/// 判断字符串是否为有效的包全名 (Package Full Name)
		/// 格式: <IdentityName>_<Version>_<ProcessorArchitecture>_<ResourceId>_<PublisherId>
		/// 其中 ResourceId 可以为空（表现为连续两个下划线）
		/// </summary>
		public static bool IsPackageFullName (string fullName)
		{
			if (string.IsNullOrWhiteSpace (fullName)) return false;
			string [] parts = fullName.Split (new char [] { '_' }, StringSplitOptions.None);
			if (parts.Length != 5)
				return false;
			if (string.IsNullOrEmpty (parts [0]))   // IdentityName
				return false;
			if (string.IsNullOrEmpty (parts [1]))   // Version
				return false;
			if (string.IsNullOrEmpty (parts [2]))   // ProcessorArchitecture
				return false;
			if (string.IsNullOrEmpty (parts [4]))   // PublisherId
				return false;
			if (!parts [1].Contains ('.')) return false;
			return true;
		}
		/// <summary>
		/// 判断字符串是否为有效的包系列名 (Package Family Name)
		/// 格式: <IdentityName>_<PublisherId>
		/// </summary>
		public static bool IsPackageFamilyName (string familyName)
		{
			if (string.IsNullOrWhiteSpace (familyName)) return false;
			string [] parts = familyName.Split ('_');
			if (parts.Length != 2) return false;
			if (string.IsNullOrEmpty (parts [0]) || string.IsNullOrEmpty (parts [1]))
				return false;
			return true;
		}
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
		public static readonly string [] configItems = new string [] {
			"AppMetadataItems"
		};
		public static void RefreshConfig ()
		{
			var conf = new InitConfig (Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "config.ini"));
			var sSettings = conf.GetSection ("Settings");
			var kAppMetadatas = sSettings.GetKey ("PkgCLI:AppMetadataItems");
			var appMd = kAppMetadatas.ReadString ("Id,BackgroundColor,DisplayName,ForegroundText,ShortName,SmallLogo,Square44x44Logo");
			var appMdList = (appMd ?? "").Split (',', ';', '|');
			for (var i = 0; i < appMdList.Length; i ++)
			{
				appMdList [i] = appMdList [i].Trim ();
			}
			PackageReader.UpdateApplicationItems (appMdList);
		}
		static void Main (string [] args)
		{
			var currencoding = Console.OutputEncoding;
			try
			{
				//Console.OutputEncoding = Encoding.UTF8;
				if (args.Length <= 0 || args.Length >= 1 && IsHelpParam (args [0]))
				{
					PrintVersion ();
					PrintTotalHelp ();
					return;
				}
				var parser = new CliParsing ();
				parser.Params = new HashSet<CmdParamName> (new CmdParamName [] {
					new CmdParamName ("install", new string [] { "add" }),
					new CmdParamName ("register", new string [] { "reg" }),
					new CmdParamName ("update", new string [] { "up" }),
					new CmdParamName ("stage"),
					new CmdParamName ("remove", new string [] { "uninstall" }),
					new CmdParamName ("read"),
					new CmdParamName ("get"),
					new CmdParamName ("find"),
					new CmdParamName ("active", new string [] { "launch", "start", "activate" }),
					new CmdParamName ("config", new string [] { "conf" }),
					new CmdParamName ("version", new string [] { "ver" }),
					new CmdParamName ("help", new string [] { "?", "h" }),
					new CmdParamName ("developmode", new string [] { "develop" }),
					new CmdParamName ("forceappshutdown", new string [] { "forceshutdown" , "appshutdown", "forcesd", "force"}),
					new CmdParamName ("installallresources", new string [] {"allresources", "allres"}),
					new CmdParamName ("savexml"),
					new CmdParamName ("savejson"),
					new CmdParamName ("fullname"),
					new CmdParamName ("filter"),
					new CmdParamName ("package", new string [] { "pkg" }),
					new CmdParamName ("manifest", new string [] { "mani" }),
					new CmdParamName ("usepri", new string [] { "pri" }),
					new CmdParamName ("item"),
					new CmdParamName ("set"),
					new CmdParamName ("refresh"),
					new CmdParamName ("show"),
					new CmdParamName ("encoding", new string [] { "en", "charset", "encode" }),
					new CmdParamName ("yes", new string [] {"y", "agree"})
				});
				RefreshConfig ();
				var cmds = parser.Parse (args);
				if (cmds.ParamContains ("encoding"))
				{
					#region help text: encoding
					if (CliPasingUtils.ParamContains (cmds, "help"))
					{
						PrintVersion ();
						Console.WriteLine (res.Get ("PKGCLI_HELP_ENCODING"));
						return;
					}
					#endregion
					try
					{
						var c = cmds.GetFromId ("encoding");
						var i = 0;
						var isint = false;
						Encoding en = null;
						try { i = Convert.ToInt32 (c.Value); isint = true; } catch { isint = false; }
						if (isint) en = Encoding.GetEncoding (i);
						else en = Encoding.GetEncoding (c.Value);
						Console.OutputEncoding = en;
					}
					catch (Exception ex)
					{
						Console.ForegroundColor = ConsoleColor.Yellow;
						Console.WriteLine (String.Format (res.Get ("PKGCLI_WARNING_ENCODING"), ex.GetType (), ex.Message));
						Console.ResetColor ();
					}
				}
				if (CliPasingUtils.ParamsContainsOr (cmds, "install", "register", "update", "stage"))
				{
					#region help text: install register, update, stage
					PrintVersion ();
					if (CliPasingUtils.ParamContains (cmds, "help"))
					{
						Console.WriteLine (res.Get ("PKGCLI_HELP_IRUS"));
						return;
					}
					#endregion
					var options = DeploymentOptions.None;
					if (CliPasingUtils.ParamContains (cmds, "developmode")) options |= DeploymentOptions.DevelopmentMode;
					if (CliPasingUtils.ParamContains (cmds, "forceappshutdown")) options |= DeploymentOptions.ForceAppShutdown;
					if (CliPasingUtils.ParamContains (cmds, "installallresources")) options |= DeploymentOptions.InstallAllResources;
					var totallist = new List<Tuple<int, string>> ();
					var filelist = new List<string> ();
					foreach (var f in cmds)
					{
						if (string.IsNullOrWhiteSpace (f.Id))
						{
							var file = f.Value;
							AddFiles (filelist, file);
						}
						else
						{
							switch (f.Id)
							{
								case "install":
								case "register":
								case "update":
								case "stage":
									AddFiles (filelist, f.Value);
									break;
							}
						}
					}
					foreach (var f in filelist) totallist.Add (new Tuple<int, string> (0, f));
					if (cmds.ParamContains ("register") && cmds.ParamContains ("fullname"))
					{
						foreach (var f in cmds)
						{
							if (f.Id.NEquals ("fullname"))
							{
								if (string.IsNullOrWhiteSpace (f.Value))
								{
									totallist.Add (new Tuple<int, string> (1, f.Value));
								}
							}
						}

					}
					PackageOperation ope = null;
					if (cmds.ParamContains ("install")) ope = AddPackage;
					else if (cmds.ParamContains ("register")) ope = RegisterPackage;
					else if (cmds.ParamContains ("update")) ope = UpdatePackage;
					else if (cmds.ParamContains ("stage")) ope = StagePackage;
					for (int i = 0; i < totallist.Count; i++)
					{
						Console.WriteLine ();
						var file = totallist [i];
						Console.Write ("\r" + String.Format (res.Get ("PKGCLI_PROGRESS_OPERATION"), i + 1, totallist.Count));
						var hr = new _I_HResult (0);
						var tempope = ope;
						if (file.Item1 == 1)
						{
							if (cmds.ParamContains ("register")) tempope = RegisterPackageByFullName;
						}
						hr = tempope (file.Item2, null, options, prog => {
							var str = "\r" + String.Format (res.Get ("PKGCLI_PROGRESS_OPERATION_WITHPROGRESS"), i + 1, totallist.Count, prog, (int)((i + prog * 0.01) / totallist.Count * 100));
							Console.Write (str);
						});
						if (hr.Failed)
						{
							Console.ForegroundColor = ConsoleColor.Red;
							Console.Write ("\n" + String.Format (res.Get ("PKGCLI_ERROR_IRUS_EXCEPTION"), i + 1, file.Item2, "0x" + hr.HResult.ToString ("X8"), hr.Message));
							Console.ResetColor ();
						}
					}
					Console.WriteLine ("\n" + res.Get ("PKGCLI_COMPLETE_OPERATION"));
					return;
				}
				else if (cmds.ParamsContainsOr ("remove"))
				{ 
					#region help text: remove
					if (CliPasingUtils.ParamContains (cmds, "help"))
					{
						PrintVersion ();
						Console.WriteLine (res.Get ("PKGCLI_HELP_REMOVE"));
						return;
					}
					#endregion
					var list = new List<string> ();
					foreach (var c in cmds)
					{
						if (string.IsNullOrWhiteSpace (c.Id) || c.Id.NEquals ("remove"))
						{
							if (!string.IsNullOrWhiteSpace (c.Value)) list.Add (c.Value);
						}
					}
					var agreecmd = cmds.GetFromId ("yes");
					bool? agree = null;
					if (agreecmd != null) agree = true;
					if (agree == null)
					{
						if (list.Count <= 0) agree = true;
						else
						{
							Console.Write (String.Format (res.Get ("PKGCLI_ASK_REMOVE"), list.Count));
							var userinput = Console.ReadLine ();
							if (userinput.NEquals ("y") || userinput.NEquals ("yes")) agree = true;
							else agree = false;
						}
					}
					if (agree == false) throw new OperationCanceledException (res.Get ("PKGCLI_ERROR_USERABORT"));
					for (int i = 0; i < list.Count; i++)
					{
						Console.WriteLine ();
						var file = list [i];
						Console.Write ("\r" + String.Format (res.Get ("PKGCLI_PROGRESS_OPERATION"), i + 1, list.Count));
						var hr = RemovePackage (file, prog => {
							var str = "\r" + String.Format (res.Get ("PKGCLI_PROGRESS_OPERATION_WITHPROGRESS"), i + 1, list.Count, prog, (int)((i + prog * 0.01) / list.Count * 100));
							Console.Write (str);
						});
						if (hr.Failed)
						{
							Console.ForegroundColor = ConsoleColor.Red;
							Console.Write ("\n" + String.Format (res.Get ("PKGCLI_ERROR_IRUS_EXCEPTION"), i + 1, file, "0x" + hr.HResult.ToString ("X8"), hr.Message));
							Console.ResetColor ();
						}
					}
					Console.WriteLine ("\n" + res.Get ("PKGCLI_COMPLETE_OPERATION"));
					return;
				}
				else if (cmds.ParamsContainsOr ("get"))
				{
					#region help text: get
					if (CliPasingUtils.ParamContains (cmds, "help"))
					{
						PrintVersion ();
						Console.WriteLine (res.Get ("PKGCLI_HELP_GET"));
						return;
					}
					#endregion
					var filter = new HashSet<string> (new NormalizeStringComparer ());
					if (cmds.ParamContains ("filter"))
					{
						foreach (var c in cmds)
						{
							if (c.Id.NEmpty () || c.Id.NEquals ("filter"))
							{
								var items = c.Value.Split (',', ';');
								foreach (var i in items) filter.Add (i);
							}
						}
					}
					var hr = GetPackages ();
					if (hr.Item1.Failed)
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine (String.Format (res.Get ("PKGCLI_ERROR_EXCEPTION"), "0x" + hr.Item1.HResult.ToString ("X8"), hr.Item1.Message));
						Console.ResetColor ();
						return;
					}
					foreach (var i in hr.Item2)
					{
						ToFormatString (i, filter);
						Console.WriteLine ();
					}
					Console.WriteLine ("[Statistic]");
					Console.WriteLine ($"Length = {hr.Item2.Count}");
					return;
				}
				else if (cmds.ParamsContainsOr ("find"))
				{
					#region help text: find
					if (CliPasingUtils.ParamContains (cmds, "help"))
					{
						PrintVersion ();
						Console.WriteLine (res.Get ("PKGCLI_HELP_FIND"));
						return;
					}
					#endregion
					var names = new List<string> ();
					foreach (var c in cmds)
					{
						if (c.Id.NEmpty ())
						{
							if (c.Value.NEmpty ()) continue;
							names.Add (c.Value);
						}
					}
					var filter = new HashSet<string> (new NormalizeStringComparer ());
					if (cmds.ParamContains ("filter"))
					{
						foreach (var c in cmds)
						{
							if (c.Id.NEquals ("filter"))
							{
								var items = c.Value.Split (',', ';');
								foreach (var i in items) filter.Add (i);
							}
						}
					}
					var result = new List<Tuple<string, Tuple<_I_HResult, List<PMPackageInfo>>>> ();
					if (names.Count == 2)
					{
						result.Add (new Tuple<string, Tuple<_I_HResult, List<PMPackageInfo>>> (names [0], FindPackage (names [0], names [1])));
					}
					else
					{
						foreach (var n in names)
						{
							if (IsPackageFullName (n))
							{
								result.Add (new Tuple<string, Tuple<_I_HResult, List<PMPackageInfo>>> (n, FindPackageByFullName (n)));
							}
							else
							{
								result.Add (new Tuple<string, Tuple<_I_HResult, List<PMPackageInfo>>> (n, FindPackage (n)));
							}
						}
					}
					foreach (var r in result)
					{
						foreach (var l in r.Item2.Item2)
						{
							ToFormatString (l, filter);
							Console.WriteLine ();
						}
						Console.WriteLine ($"[Statistic:{r.Item1}]");
						Console.WriteLine ($"HResult = {"0x" + r.Item2.Item1.HResult.ToString ("X8")}");
						Console.WriteLine ($"Message = {Escape.ToEscape (r.Item2.Item1.Message)}");
						Console.WriteLine ($"Length = {r.Item2.Item2.Count}");
					}
					return;
				}
				else if (cmds.ParamsContainsOr ("active"))
				{
					#region help text: active
					if (CliPasingUtils.ParamContains (cmds, "help"))
					{
						PrintVersion ();
						Console.WriteLine (res.Get ("PKGCLI_HELP_ACTIVATE"));
						return;
					}
					#endregion
					foreach (var c in cmds)
					{
						if (string.IsNullOrWhiteSpace (c.Id) || c.Id.NEquals ("active"))
						{
							if (c.Value.NEmpty ()) continue;
							var i = 0;
							for (i = 0; i < args.Length; i++)
							{
								if (args [i].NNormalize ().IndexOf (c.Value.NNormalize ()) >= 0) break;
							}
							var hr = ActiveApp (c.Value, BuildCommandLine (args, i + 1));
							if (hr.Succeeded) Console.WriteLine (res.Get ("PKGCLI_COMPLETE_DONE"));
							else
							{
								Console.ForegroundColor = ConsoleColor.Red;
								Console.Write (String.Format (res.Get ("PKGCLI_ERROR_EXCEPTION"), "0x" + hr.HResult.ToString ("X8"), hr.Message));
								Console.ResetColor ();
							}
							return;
						}
					}
					return;
				}
				else if (cmds.ParamsContainsOr ("read"))
				{
					#region help text: read
					if (CliPasingUtils.ParamContains (cmds, "help"))
					{
						PrintVersion ();
						Console.WriteLine (res.Get ("PKGCLI_HELP_READ"));
						return;
					}
					#endregion
					var filename = "";
					var readtype = "";
					var savename = "";
					var savetype = "default";
					CommandParam cmd = null;
					cmd = cmds.GetFromId ("manifest");
					if (cmd != null)
					{
						filename = cmd.Value;
						readtype = "manifest";
					}
					cmd = cmds.GetFromId ("package");
					if (cmd != null)
					{
						filename = cmd.Value;
						readtype = "package";
					}
					if (string.IsNullOrWhiteSpace (filename) || !File.Exists (filename))
					{
						cmd = cmds.GetFromId ("");
						if (cmd != null)
						{
							if (File.Exists (cmd.Value))
							{
								filename = cmd.Value;
								if (string.IsNullOrWhiteSpace (readtype))
								{
									var ext = Path.GetExtension (cmd.Value);
									if (ext.NEquals (".xml") || ext.NEquals (".appxpackage") || ext.NEquals (".msixpackage"))
										readtype = "manifest";
									else readtype = "package";
								}
							}
						}
					}
					if (string.IsNullOrWhiteSpace (filename) || !File.Exists (filename))
					{
						cmd = cmds.GetFromId ("read");
						if (cmd != null)
						{
							if (File.Exists (cmd.Value))
							{
								filename = cmd.Value;
								if (string.IsNullOrWhiteSpace (readtype))
								{
									var ext = Path.GetExtension (cmd.Value);
									if (ext.NEquals (".xml") || ext.NEquals (".appxpackage") || ext.NEquals (".msixpackage"))
										readtype = "manifest";
									else readtype = "package";
								}
							}
						}
					}
					if (string.IsNullOrWhiteSpace (filename) || !File.Exists (filename)) throw new FileNotFoundException ();
					cmd = cmds.GetFromId ("savexml");
					if (cmd != null)
					{
						savename = cmd.Value;
						savetype = "xml";
					}
					if (string.IsNullOrWhiteSpace (savename))
					{
						cmd = cmds.GetFromId ("savejson");
						if (cmd != null)
						{
							savename = cmd.Value;
							savetype = "json";
						}
					}
					if (string.IsNullOrWhiteSpace (savename))
					{
						savetype = "ini";
					}
					switch (readtype)
					{
						default:
						case "package":
							{
								var pr = new PackageReader (filename);
								pr.UsePri = cmds.ParamContains ("usepri");
								pr.EnablePri = true;
								switch (savetype)
								{
									default:
									case "default":
										{
											if (cmds.ParamContains ("item"))
											{
												object value = pr.GetItem (cmds.GetFromId ("item").Value);
												if (value is string) Console.WriteLine (value as string);
												else if (value is DataUtils.Version) Console.WriteLine (value.ToString ());
												else Console.WriteLine (JsonConvert.SerializeObject (value, Formatting.Indented));
											}
											else Console.WriteLine (JsonConvert.SerializeObject (pr.GetJsonObjectForCli (), Formatting.Indented));
										}
										break;
									case "json":
										{
											var res = pr.SaveJsonFileCS (savename);
											if (res) Console.WriteLine ("Succeeded!");
											else Console.WriteLine ("Failed.");
										}
										break;
									case "xml":
										{
											var res = pr.SaveXmlFileCS (savename);
											if (res) Console.WriteLine ("Succeeded!");
											else Console.WriteLine ("Failed.");
										}
										break;
								}
							}
							break;
						case "manifest":
							{
								var mr = new ManifestReader (filename);
								mr.UsePri = cmds.ParamContains ("usepri");
								mr.EnablePri = true;
								if (cmds.ParamContains ("item"))
								{
									object value = mr.GetItem (cmds.GetFromId ("item").Value);
									if (value is string) Console.WriteLine (value as string);
									else if (value is DataUtils.Version) Console.WriteLine (value.ToString ());
									else Console.WriteLine (JsonConvert.SerializeObject (value, Formatting.Indented));
								}
								else Console.WriteLine (JsonConvert.SerializeObject (mr.GetJsonObjectForCli (), Formatting.Indented));
								//Console.WriteLine (mr.BuildJsonText ());
							}
							break;
					}
					return;
				}
				else if (cmds.ParamsContainsOr ("config"))
				{
					#region help text: config
					if (CliPasingUtils.ParamContains (cmds, "help"))
					{
						PrintVersion ();
						Console.WriteLine (res.Get ("PKGCLI_HELP_CONFIG"));
						return;
					}
					#endregion
					var conf = new InitConfig (Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "config.ini"));
					var sSettings = conf.GetSection ("Settings");
					if (cmds.ParamContains ("refresh")) RefreshConfig ();
					else if (cmds.ParamContains ("set"))
					{
						var cmd = cmds.GetFromId ("set");
						var key = cmd.Value;
						if (string.IsNullOrWhiteSpace (key)) throw new InvalidOperationException (Program.res.Get ("PKGCLI_ERROR_KEYSTRINGEMPTY"));
						var isfind = false;
						foreach (var i in configItems)
						{
							if (i.NEquals (key))
							{
								isfind = true;
								break;
							}
						}
						if (!isfind) throw new KeyNotFoundException (String.Format (Program.res.Get("PKGCLI_ERROR_CANNOTFINDKEY"), key));
						var valuelist = new List<string> ();
						foreach (var c in cmds)
						{
							if (c.Id.NEmpty ())
							{
								if (string.IsNullOrWhiteSpace (c.Value)) continue;
								valuelist.Add (c.Value);
							}
						}
						var res = sSettings.GetKey ($"PkgCLI:{key.Trim ()}").Set (valuelist.Join (","));
						if (res) Console.WriteLine ("Succeeded!");
						else Console.WriteLine ("Failed.");
					}
					else if (cmds.ParamContains ("show"))
					{
						var cmd = cmds.GetFromId ("show");
						var key = cmd.Value;
						if (string.IsNullOrWhiteSpace (key))
						{
							var dict = new Dictionary<string, string> ();
							foreach (var k in configItems)
							{
								var cKey = sSettings.GetKey ($"PkgCLI:{k}");
								dict [k] = cKey.ReadString ("(use default)");
							}
							Console.WriteLine (dict.FormatDictionaryAligned ("="));
							return;
						}
						var isfind = false;
						foreach (var i in configItems)
						{
							if (i.NEquals (key))
							{
								isfind = true;
								break;
							}
						}
						if (!isfind) throw new KeyNotFoundException (String.Format (Program.res.Get ("PKGCLI_ERROR_CANNOTFINDKEY"), key));
						var value = sSettings.GetKey ($"PkgCLI:{key.Trim ()}").ReadString ("(use default)");
						Console.WriteLine (value);
					}
					return;
				}
				else if (cmds.ParamsContainsOr ("version"))
				{
					#region help text: version
					if (CliPasingUtils.ParamContains (cmds, "help"))
					{
						PrintVersion ();
						Console.WriteLine (res.Get ("PKGCLI_HELP_VERSION"));
						return;
					}
					#endregion
					PrintVersion ();
					return;
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine ("Invalid args. Please use \"/help\" to get help.");
					Console.ResetColor ();
				}
			}
			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine (String.Format (res.Get ("PKGCLI_ERROR_FINALEXCEPTION"), ex.GetType (), ex.Message, ex.StackTrace));
			}
			finally
			{
				Console.ResetColor ();
				Console.OutputEncoding = currencoding;
			}
		}
	}
}
