using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;

namespace DataUtils
{
	public enum ManifestTextColor
	{
		Dark = 0x000000,
		Light = 0xFFFFFF
	}
	[ComVisible (true)]
	public class VisualElementManifest: IDisposable
	{
		private XmlDocument _doc;
		private bool _available;

		public VisualElementManifest ()
		{
			_available = false;
		}

		public VisualElementManifest (string filename)
		{
			Create (filename);
		}

		public VisualElementManifest (Stream stream)
		{
			Create (stream);
		}

		public bool Create (string filename)
		{
			Destroy ();
			if (string.IsNullOrEmpty (filename) || !File.Exists (filename))
			{
				_available = false;
				return false;
			}

			try
			{
				_doc = new XmlDocument ();
				// Load using UTF-8/UTF-16 auto-detection
				_doc.Load (filename);
				_available = true;
			}
			catch
			{
				_available = false;
			}
			return _available;
		}

		public bool Create (Stream stream)
		{
			Destroy ();
			if (stream == null) { _available = false; return false; }
			try
			{
				_doc = new XmlDocument ();
				_doc.Load (stream);
				_available = true;
			}
			catch
			{
				_available = false;
			}
			return _available;
		}

		public void Destroy ()
		{
			if (!_available) return;
			_doc = null;
			_available = false;
		}

		public bool Valid { get { return _available; } }

		// Helper: find VisualElements node for an app id (if root is <Applications> iterate <Application> children)
		private XmlNode VisualElementNode (string id)
		{
			if (!_available || _doc == null) return null;
			XmlElement root = _doc.DocumentElement;
			if (root == null) return null;

			string rootName = root.Name;
			if (string.Equals (rootName, "Applications", StringComparison.OrdinalIgnoreCase))
			{
				foreach (XmlNode app in root.SelectNodes ("Application"))
				{
					var attr = app.Attributes? ["Id"];
					if (attr != null && string.Equals (attr.Value, id, StringComparison.OrdinalIgnoreCase))
					{
						return app.SelectSingleNode ("VisualElements");
					}
				}
				return null;
			}
			else if (string.Equals (rootName, "Application", StringComparison.OrdinalIgnoreCase))
			{
				return root.SelectSingleNode ("VisualElements");
			}
			return null;
		}

		// Utility to get attribute string safely
		private static string Attr (XmlNode node, string attrName)
		{
			if (node == null || node.Attributes == null) return string.Empty;
			var a = node.Attributes [attrName];
			return a != null ? a.Value : string.Empty;
		}

		public string DisplayName (string id = "App")
		{
			var visual = VisualElementNode (id);
			return visual != null ? Attr (visual, "DisplayName") : string.Empty;
		}

		public string Logo (string id = "App")
		{
			var visual = VisualElementNode (id);
			if (visual == null) return string.Empty;
			string logo = Attr (visual, "Logo");
			if (!string.IsNullOrEmpty (logo)) return logo;
			return Attr (visual, "Square150x150Logo") ?? string.Empty;
		}

		public string SmallLogo (string id = "App")
		{
			var visual = VisualElementNode (id);
			if (visual == null) return string.Empty;
			string small = Attr (visual, "SmallLogo");
			if (!string.IsNullOrEmpty (small)) return small;
			return Attr (visual, "Square70x70Logo") ?? string.Empty;
		}

		public ManifestTextColor ForegroundText (string id = "App")
		{
			var visual = VisualElementNode (id);
			if (visual == null) return ManifestTextColor.Dark;
			string fg = Attr (visual, "ForegroundText");
			return string.Equals (fg, "light", StringComparison.OrdinalIgnoreCase) ? ManifestTextColor.Light : ManifestTextColor.Dark;
		}

		public string Lnk32x32Logo (string id = "App")
		{
			var visual = VisualElementNode (id);
			return visual != null ? Attr (visual, "Lnk32x32Logo") : string.Empty;
		}

		public string ItemDisplayLogo (string id = "App")
		{
			var visual = VisualElementNode (id);
			if (visual == null) return string.Empty;
			string item = Attr (visual, "ItemDisplayLogo");
			if (!string.IsNullOrEmpty (item)) return item;
			item = Attr (visual, "Lnk32x32Logo");
			if (!string.IsNullOrEmpty (item)) return item;
			return Attr (visual, "Square44x44Logo") ?? string.Empty;
		}

		public bool ShowNameOnTile (string id = "App")
		{
			var visual = VisualElementNode (id);
			if (visual == null) return false;
			string val = Attr (visual, "ShowNameOnSquare150x150Logo");
			return string.Equals (val, "on", StringComparison.OrdinalIgnoreCase);
		}

		public string BackgroundColor (string id = "App")
		{
			var visual = VisualElementNode (id);
			return visual != null ? Attr (visual, "BackgroundColor") : string.Empty;
		}

		public string SplashScreenImage (string id = "App")
		{
			var visual = VisualElementNode (id);
			if (visual == null) return string.Empty;
			var splash = visual.SelectSingleNode ("SplashScreen");
			return splash != null ? Attr (splash, "Image") : string.Empty;
		}

		public string SplashScreenBackgroundColor (string id = "App")
		{
			var visual = VisualElementNode (id);
			if (visual == null) return string.Empty;
			var splash = visual.SelectSingleNode ("SplashScreen");
			string bg = splash != null ? Attr (splash, "BackgroundColor") : string.Empty;
			if (!string.IsNullOrEmpty (bg)) return bg;
			return Attr (visual, "BackgroundColor") ?? string.Empty;
		}

		public string SplashScreenBackgroundColorDarkMode (string id = "App")
		{
			var visual = VisualElementNode (id);
			if (visual == null) return string.Empty;
			var splash = visual.SelectSingleNode ("SplashScreen");
			string bg = splash != null ? Attr (splash, "DarkModeBackgroundColor") : string.Empty;
			if (!string.IsNullOrEmpty (bg)) return bg;
			return Attr (visual, "DarkModeBackgroundColor") ?? string.Empty;
		}

		// Check if an app id exists in document
		public bool IsAppIdExists (string id)
		{
			if (!_available || _doc == null) return false;
			XmlElement root = _doc.DocumentElement;
			if (root == null) return false;
			if (string.Equals (root.Name, "Applications", StringComparison.OrdinalIgnoreCase))
			{
				foreach (XmlNode app in root.SelectNodes ("Application"))
				{
					var attr = app.Attributes? ["Id"];
					if (attr != null && string.Equals (attr.Value, id, StringComparison.OrdinalIgnoreCase))
						return true;
				}
				return false;
			}
			else if (string.Equals (root.Name, "Application", StringComparison.OrdinalIgnoreCase))
			{
				var attr = root.Attributes? ["Id"];
				if (attr != null && string.Equals (attr.Value, id, StringComparison.OrdinalIgnoreCase)) return true;
			}
			return false;
		}

		// Get all application ids as a list
		public List<string> AppIds ()
		{
			var output = new List<string> ();
			if (!_available || _doc == null)
			{
				output.Add ("App");
				return output;
			}

			XmlElement root = _doc.DocumentElement;
			if (root == null)
			{
				output.Add ("App");
				return output;
			}

			if (string.Equals (root.Name, "Applications", StringComparison.OrdinalIgnoreCase))
			{
				foreach (XmlNode app in root.SelectNodes ("Application"))
				{
					var attr = app.Attributes? ["Id"];
					if (attr != null && !string.IsNullOrEmpty (attr.Value))
					{
						if (!output.Contains (attr.Value)) output.Add (attr.Value);
					}
				}
			}
			else if (string.Equals (root.Name, "Application", StringComparison.OrdinalIgnoreCase))
			{
				var attr = root.Attributes? ["Id"];
				if (attr != null && !string.IsNullOrEmpty (attr.Value))
				{
					if (!output.Contains (attr.Value)) output.Add (attr.Value);
				}
			}

			if (output.Count == 0) output.Add ("App");
			return output;
		}

		public void Dispose ()
		{
			Destroy ();
		}
	}
}
