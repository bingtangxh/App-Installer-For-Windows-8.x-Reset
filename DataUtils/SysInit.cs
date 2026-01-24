using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DataUtils
{
	public static class VisualElementsStore
	{
		// Publicly accessible instances for internal use
		public static readonly VisualElementManifest Vemanifest;
		public static readonly FileResXmlDoc ScaleResources;

		static VisualElementsStore ()
		{
			try
			{
				string programRoot = GetProgramRootDirectory ();

				// manifest path: VisualElementsManifest.xml in program root
				string manifestPath = Path.Combine (programRoot, "VisualElementsManifest.xml");

				// scale xml candidate: VisualElements\scale.xml
				string scaleCandidate = Path.Combine (programRoot, "VisualElements", "scale.xml");

				// If scale.xml exists use it, otherwise fall back to VisualElementsManifest.xml
				string scalePath = File.Exists (scaleCandidate) ? scaleCandidate : manifestPath;

				// Initialize (constructors will attempt to load)
				Vemanifest = new VisualElementManifest ();
				if (File.Exists (manifestPath))
				{
					// Use Create to ensure we reflect true load status
					Vemanifest.Create (manifestPath);
				}

				FileResXmlDoc tmp = null;
				if (File.Exists (scalePath))
				{
					tmp = new FileResXmlDoc (scalePath);
				}
				else
				{
					// if both missing, try manifest as last resort (ResXmlDoc handles manifest-style layout)
					if (File.Exists (manifestPath))
						tmp = new FileResXmlDoc (manifestPath);
				}
				ScaleResources = tmp;
			}
			catch
			{
				// swallow exceptions; leave fields null if initialization fails
				Vemanifest = new VisualElementManifest ();
				ScaleResources = null;
			}
		}

		private static string GetProgramRootDirectory ()
		{
			try
			{
				// Prefer the directory of the executing assembly
				string codeBase = Assembly.GetExecutingAssembly ().Location;
				if (!string.IsNullOrEmpty (codeBase))
				{
					string dir = Path.GetDirectoryName (codeBase);
					if (!string.IsNullOrEmpty (dir)) return dir;
				}
			}
			catch { }

			try
			{
				return AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
			}
			catch { }

			return Environment.CurrentDirectory;
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_VisualElement
	{
		private string _appid;

		public _I_VisualElement ()
		{
			_appid = "App";
		}

		public _I_VisualElement (string appid)
		{
			_appid = string.IsNullOrEmpty (appid) ? "App" : appid;
		}

		public string Id
		{
			get { return _appid; }
			set { _appid = string.IsNullOrEmpty (value) ? "App" : value; }
		}

		public string DisplayName
		{
			get { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.DisplayName (_appid) : string.Empty; }
		}

		public string Logo
		{
			get { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.Logo (_appid) : string.Empty; }
		}

		public string SmallLogo
		{
			get { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.SmallLogo (_appid) : string.Empty; }
		}

		public string ForegroundText
		{
			get
			{
				if (VisualElementsStore.Vemanifest == null) return "dark";
				var t = VisualElementsStore.Vemanifest.ForegroundText (_appid);
				return t == ManifestTextColor.Light ? "light" : "dark";
			}
		}

		public string Lnk32x32Logo
		{
			get { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.Lnk32x32Logo (_appid) : string.Empty; }
		}

		public string ItemDisplayLogo
		{
			get { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.ItemDisplayLogo (_appid) : string.Empty; }
		}

		public bool ShowNameOnTile
		{
			get { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.ShowNameOnTile (_appid) : false; }
		}

		public string BackgroundColor
		{
			get { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.BackgroundColor (_appid) : string.Empty; }
		}

		public string SplashScreenImage
		{
			get { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.SplashScreenImage (_appid) : string.Empty; }
		}

		public string SplashScreenBackgroundColor
		{
			get { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.SplashScreenBackgroundColor (_appid) : string.Empty; }
		}

		public string SplashScreenBackgroundColorDarkMode
		{
			get { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.SplashScreenBackgroundColorDarkMode (_appid) : string.Empty; }
		}

		// Indexer for script-friendly access: v["displayname"]
		public object this [string propertyName]
		{
			get { return Get (propertyName); }
		}

		// Generic getter by property name (case-insensitive)
		public object Get (string propertyName)
		{
			if (string.IsNullOrEmpty (propertyName)) return string.Empty;
			string key = propertyName.Trim ().ToLowerInvariant ();

			switch (key)
			{
				case "displayname": return DisplayName;
				case "logo": return Logo;
				case "smalllogo": return SmallLogo;
				case "foregroundtext": return ForegroundText;
				case "lnk32x32logo": return Lnk32x32Logo;
				case "itemdisplaylogo": return ItemDisplayLogo;
				case "shownameontile": return ShowNameOnTile;
				case "backgroundcolor": return BackgroundColor;
				case "splashscreenimage": return SplashScreenImage;
				case "splashscreenbackgroundcolor": return SplashScreenBackgroundColor;
				case "splashscreenbackgroundcolordarkmode": return SplashScreenBackgroundColorDarkMode;
				default: return string.Empty;
			}
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_VisualElements
	{
		public string [] GetIds ()
		{
			try
			{
				var list = VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.AppIds () : new System.Collections.Generic.List<string> () { "App" };
				return list.ToArray ();
			}
			catch
			{
				return new string [] { "App" };
			}
		}

		public string GetIdsToJson ()
		{
			var ids = GetIds ();
			// Use Utilities.StringArrayToJson (which uses Newtonsoft.Json if available)
			try
			{
				return Utilities.StringArrayToJson (ids);
			}
			catch
			{
				// fallback
				return Newtonsoft.Json.JsonConvert.SerializeObject (ids);
			}
		}

		public _I_VisualElement Get (string id)
		{
			return new _I_VisualElement (id);
		}

		public _I_VisualElement this [string id]
		{
			get { return Get (id); }
		}

		// Attribute-style methods
		public string DisplayName (string appid) { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.DisplayName (string.IsNullOrEmpty (appid) ? "App" : appid) : string.Empty; }
		public string Logo (string appid) { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.Logo (string.IsNullOrEmpty (appid) ? "App" : appid) : string.Empty; }
		public string SmallLogo (string appid) { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.SmallLogo (string.IsNullOrEmpty (appid) ? "App" : appid) : string.Empty; }
		public string ForegroundText (string appid)
		{
			if (VisualElementsStore.Vemanifest == null) return "dark";
			var t = VisualElementsStore.Vemanifest.ForegroundText (string.IsNullOrEmpty (appid) ? "App" : appid);
			return t == ManifestTextColor.Light ? "light" : "dark";
		}
		public string Lnk32x32Logo (string appid) { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.Lnk32x32Logo (string.IsNullOrEmpty (appid) ? "App" : appid) : string.Empty; }
		public string ItemDisplayLogo (string appid) { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.ItemDisplayLogo (string.IsNullOrEmpty (appid) ? "App" : appid) : string.Empty; }
		public bool ShowNameOnTile (string appid) { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.ShowNameOnTile (string.IsNullOrEmpty (appid) ? "App" : appid) : false; }
		public string BackgroundColor (string appid) { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.BackgroundColor (string.IsNullOrEmpty (appid) ? "App" : appid) : string.Empty; }
		public string SplashScreenImage (string appid) { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.SplashScreenImage (string.IsNullOrEmpty (appid) ? "App" : appid) : string.Empty; }
		public string SplashScreenBackgroundColor (string appid) { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.SplashScreenBackgroundColor (string.IsNullOrEmpty (appid) ? "App" : appid) : string.Empty; }
		public string SplashScreenBackgroundColorDarkMode (string appid) { return VisualElementsStore.Vemanifest != null ? VisualElementsStore.Vemanifest.SplashScreenBackgroundColorDarkMode (string.IsNullOrEmpty (appid) ? "App" : appid) : string.Empty; }

		// Generic getter by attribute name
		public object GetValue (string appid, string attributeName)
		{
			if (string.IsNullOrEmpty (attributeName)) return string.Empty;
			string key = attributeName.Trim ().ToLowerInvariant ();
			switch (key)
			{
				case "displayname": return DisplayName (appid);
				case "logo": return Logo (appid);
				case "smalllogo": return SmallLogo (appid);
				case "foregroundtext": return ForegroundText (appid);
				case "lnk32x32logo": return Lnk32x32Logo (appid);
				case "itemdisplaylogo": return ItemDisplayLogo (appid);
				case "shownameontile": return ShowNameOnTile (appid);
				case "backgroundcolor": return BackgroundColor (appid);
				case "splashscreenimage": return SplashScreenImage (appid);
				case "splashscreenbackgroundcolor": return SplashScreenBackgroundColor (appid);
				case "splashscreenbackgroundcolordarkmode": return SplashScreenBackgroundColorDarkMode (appid);
				default: return string.Empty;
			}
		}
	}
}
