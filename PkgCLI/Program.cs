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
		static bool IsHelpParam (string arg) => helpArgs.Contains (arg.Normalize ());
		static void PrintVersion ()
		{
			var verFilePath = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "version");
			var verFileInst = new _I_File (verFilePath);
			var verstr = verFileInst.Content?.Trim () ?? "0.0.0.1";
			Console.WriteLine (
				$"Package Manager CLI [Version {verstr}]\n(C) Windows Modern. All rights reserved."
			);
		}
		static void PrintTotalHelp ()
		{
			Console.WriteLine (@"
Usage:
    pkgcli <command> [arguments]

Commands:
    /install        Install a package.
    /update         Update a package (previous version must be installed).
    /register       Register an app manifest file.
    /stage          Stage a package.
    /remove         Remove the package.
    /read           Read package information.
    /get            List all installed apps.
    /find           Find installed apps.
    /active         Launch an app.
    /config         Configure settings (omit to show current config).
    /?              Show this help.
");
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
			try
			{
				Console.OutputEncoding = Encoding.UTF8;
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
					new CmdParamName ("show")
				});
				RefreshConfig ();
				var cmds = parser.Parse (args);
				if (CliPasingUtils.ParamsContainsOr (cmds, "install", "register", "update", "stage"))
				{
					#region help text: install register, update, stage
					PrintVersion ();
					if (CliPasingUtils.ParamContains (cmds, "help"))
					{
						Console.WriteLine (@"
Usage:
    pkgcli <command> <file_path...> [/developmode] [/force] [/allres]

Commands:
    /install        Install a package. Supports .appx, .appxbundle, .msix, .msixbundle.
    /register       Register a package. Supports AppxManifest.xml or *.appxmanifest.
    /update         Update an installed app. Supports same formats as /install.
    /stage          Stage a package (pre-deploy). Supports same formats as /install.

Options (DeploymentOptions flags):
    /developmode    Install/register in development mode. Do not use with bundle packages.
    /force          Force application shutdown to allow registration when the package or its dependencies are in use.
    /allres         Skip resource applicability checks; stage/register all resource packages in a bundle.

Arguments:
    <file_path...>  One or more package or manifest file paths. Can be:
                    - Direct path to .appx, .msix, .appxbundle, .msixbundle, or .xml manifest.
                    - A .txt file containing a list of such paths (one per line).
                    - If no command is repeated, the command applies to all listed files.

Examples:
    pkgcli /install MyApp.appx
    pkgcli /register /manifest:AppxManifest.xml /developmode
    pkgcli /update MyApp.msixbundle /force
    pkgcli /stage MyApp.appx /allres
");
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
						Console.Write ($"\r({i + 1}/{totallist.Count}) Operation in progress...");
						var hr = new _I_HResult (0);
						var tempope = ope;
						if (file.Item1 == 1)
						{
							if (cmds.ParamContains ("register")) tempope = RegisterPackageByFullName;
						}
						hr = tempope (file.Item2, null, options, prog => {
							var str = $"\r({i + 1}/{filelist.Count}) Operation in progress... {prog}% of {(int)((i + prog * 0.01) / filelist.Count * 100)}%";
							Console.Write (str);
						});
						if (hr.Failed)
						{
							Console.ForegroundColor = ConsoleColor.Red;
							Console.Write ($"\nPackage {i + 1} \"{file.Item2}\" Operation Error ({"0x" + hr.HResult.ToString ("X8")}): \n{hr.Message}");
							Console.ResetColor ();
						}
					}
					Console.WriteLine ("\nAll Done.");
					return;
				}
				else if (cmds.ParamsContainsOr ("remove"))
				{
					#region help text: remove
					if (CliPasingUtils.ParamContains (cmds, "help"))
					{
						PrintVersion ();
						Console.WriteLine (@"
Usage:
    pkgcli /remove <package_full_name> [<package_full_name>...]

Operation:
    Remove (uninstall) one or more packages.

Arguments:
    <package_full_name>  The full name of the installed package.
                         Format: <identity_name>_<version>_<architecture>_<resource_id>_<publisher_id>
                         Example: Microsoft.WinJS.1.0_1.0.9200.20789_neutral__8wekyb3d8bbwe

You can specify multiple full names separated by spaces.

Examples:
    pkgcli /remove MyPackage_1.0.0.0_x64__abcd1234
    pkgcli /remove PackageA_1.0.0.0_neutral__abcd1234 PackageB_2.0.0.0_x86__efgh5678
");
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
					for (int i = 0; i < list.Count; i++)
					{
						Console.WriteLine ();
						var file = list [i];
						Console.Write ($"\r({i + 1}/{list.Count}) Operation in progress...");
						var hr = RemovePackage (file, prog => {
							var str = $"\r({i + 1}/{list.Count}) Operation in progress... {prog}% of {(int)((i + prog * 0.01) / list.Count * 100)}%";
							Console.Write (str);
						});
						if (hr.Failed)
						{
							Console.ForegroundColor = ConsoleColor.Red;
							Console.Write ($"\nPackage {i + 1} \"{file}\" Operation Error ({"0x" + hr.HResult.ToString ("X8")}): \n{hr.Message}");
							Console.ResetColor ();
						}
					}
					Console.WriteLine ("\nAll Done.");
					return;
				}
				else if (cmds.ParamsContainsOr ("get"))
				{
					#region help text: get
					if (CliPasingUtils.ParamContains (cmds, "help"))
					{
						PrintVersion ();
						Console.WriteLine (@"
Usage:
    pkgcli /get [/filter:<filter1,filter2,...>]

Operation:
    List all installed packages with their properties.

Options:
    /filter:<filters>   Show only the specified properties.
                        Filters are case-insensitive and support '*' wildcard.
                        Multiple filters separated by comma ',' or semicolon ';'.
                        Examples:
                            /filter:Identity:Name,Identity:Version
                            /filter:Identity:*                 (all Identity properties)
                            /filter:Properties:DisplayName

Output:
    Each package is printed as a section [FullName] followed by key = value lines.
    Redirect output to a file to save as INI-like format.

Examples:
    pkgcli /get
    pkgcli /get /filter:Identity:FullName,Properties:DisplayName
");
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
						Console.WriteLine ($"Operation Error ({"0x" + hr.Item1.HResult.ToString ("X8")}): \n{hr.Item1.Message}");
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
Console.WriteLine(@"
Usage:
    pkgcli /find <package_full_name|package_family_name> [/filter:<filters>]
    pkgcli /find <identity_name> <identity_publisher> [/filter:<filters>]

Operation:
    Find installed packages matching the given identifier.

Arguments:
    <package_full_name>       Full name of a package.
    <package_family_name>     Family name of a package.
    <identity_name>           Name part of the package identity (e.g., ""Microsoft.WindowsStore"").
    <identity_publisher>      Publisher part (e.g., ""CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US"").

Options:
    /filter:<filters>         Same as in /get command.

Examples:
    pkgcli /find Microsoft.WindowsStore_8wekyb3d8bbwe!App
    pkgcli /find Microsoft.WindowsStore
    pkgcli /find Microsoft.WindowsStore ""CN=Microsoft Corporation, ...""
");
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
						Console.WriteLine (@"
Usage:
    pkgcli /active <app_user_model_id> [arguments]

Operation:
    Launch (activate) a Universal Windows Platform (UWP) app.

Arguments:
    <app_user_model_id>       The Application User Model ID (AUMID).
                              Format: <package_family_name>!<app_id>
                              Example: Microsoft.WindowsStore_8wekyb3d8bbwe!App

    [arguments]               Optional command-line arguments passed to the app.

Examples:
    pkgcli /active Microsoft.WindowsStore_8wekyb3d8bbwe!App
    pkgcli /active MyAppFamily!App --fullscreen --debug
");
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
							if (hr.Succeeded) Console.WriteLine ("Done.");
							else
							{
								Console.ForegroundColor = ConsoleColor.Red;
								Console.Write ($"Operation Error ({"0x" + hr.HResult.ToString ("X8")}): \n{hr.Message}");
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
						Console.WriteLine (@"
Usage:
    pkgcli /read [options] [<file>]

Description:
    Read an Appx/MSIX package (.appx, .appxbundle, .msix, .msixbundle) or its manifest file (.xml, .appxpackage, .msixpackage).
    Extracts package/manifest information and outputs as JSON to the console, or saves as a ZIP archive containing either info.json or info.xml.

Options:
    /manifest:<file>        Specify a manifest file to read.
    /package:<file>         Specify a package file to read.
    <file>                  If no /manifest or /package is provided, the first unnamed parameter is used as the input file.
                            File type is auto-detected by extension:
                              .xml, .appxpackage, .msixpackage  → ManifestReader
                              .appx, .appxbundle, .msix, .msixbundle → PackageReader

    /item:<path>            Instead of outputting the whole JSON object, output only the value at the given path.
                            Path syntax: use '.' or ':' as separators, case-insensitive.
                            Supports indexers (e.g., [0]), .length for collections, and automatic Base64 access (e.g., LogoBase64).
                            Examples:
                                /item:Identity.Name
                                /item:Properties.Publisher
                                /item:applications[0]
                                /item:applications.length
                                /item:applications[0].LogoBase64

    /usepri                 Enable PRI resource resolution (localization and scaled images). Required for proper resource string and image resolution.

    /savexml:<output>       Save extracted data as an XML file wrapped in a ZIP archive.
                            The archive will contain an info.xml file with the structured data.

    /savejson:<output>      Save extracted data as a JSON file wrapped in a ZIP archive.
                            The archive will contain an info.json file with the structured data.

    /help                   Display this help text.

Output Behavior:
    - If neither /savexml nor /savejson is given, the information is printed directly to the console as JSON.
    - If /item is used, only the value at the specified path is printed (as a string, version, or formatted JSON).
    - If /savexml or /savejson is used, the data is saved to the specified file (ZIP archive) and a success/failure message is shown.

Examples:
    pkgcli /read MyApp.appx
    pkgcli /read /manifest:AppxManifest.xml /usepri
    pkgcli /read /package:MyApp.msixbundle /item:Identity.Name
    pkgcli /read /package:MyApp.appx /savejson:output.zip
    pkgcli /read MyApp.appx /item:applications[0].DisplayName
");
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
						Console.WriteLine (@"
Usage:
    pkgcli /config                      Show current configuration (all keys).
    pkgcli /config /show:<key>          Show value of a specific configuration key.
    pkgcli /config /set:<key> <value>   Set configuration key to the given value.
    pkgcli /config /refresh             Reload configuration from config.ini.

Configuration keys:
    AppMetadataItems    Comma-separated list of application metadata fields to read from manifests.
                        Default: ""Id,BackgroundColor,DisplayName,ForegroundText,ShortName,SmallLogo,Square44x44Logo""

Configuration file:
    config.ini in the same directory as pkgcli.exe.

Examples:
    pkgcli /config
    pkgcli /config /show:AppMetadataItems
    pkgcli /config /set:AppMetadataItems ""Id,DisplayName,Logo""
    pkgcli /config /refresh
");
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
						if (string.IsNullOrWhiteSpace (key)) throw new InvalidOperationException ($"key is empty");
						var isfind = false;
						foreach (var i in configItems)
						{
							if (i.NEquals (key))
							{
								isfind = true;
								break;
							}
						}
						if (!isfind) throw new KeyNotFoundException ($"Error: cannot find the key \"{key}\"");
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
						if (!isfind) throw new KeyNotFoundException ($"Error: cannot find the key \"{key}\"");
						var value = sSettings.GetKey ($"PkgCLI:{key.Trim ()}").ReadString ("(use default)");
						Console.WriteLine (value);
					}
					return;
				}
				else if (cmds.ParamsContainsOr ("version"))
				{
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
				Console.WriteLine ($"Exception {ex.GetType ()}, \nMessage: \n    {ex.Message}\nStack: \n    {ex.StackTrace}");
			}
			finally
			{
				Console.ResetColor ();
			}
		}
	}
}
