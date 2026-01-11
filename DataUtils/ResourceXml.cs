using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace DataUtils
{
	public class FileResXmlDoc: IDisposable
	{
		private string _filepath;
		private XmlDocument _doc;
		private bool _available;

		public FileResXmlDoc (string xmlpath)
		{
			Create (xmlpath);
		}

		public bool Create (string xmlpath)
		{
			Destroy ();
			if (string.IsNullOrEmpty (xmlpath) || !File.Exists (xmlpath))
			{
				_available = false;
				return false;
			}

			try
			{
				_doc = new XmlDocument ();
				_doc.Load (xmlpath);
				_filepath = xmlpath;
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
			_filepath = null;
		}

		public void Dispose ()
		{
			Destroy ();
		}

		// Returns the chosen absolute (or resolved) path string or empty if none found
		public string Get (string id)
		{
			if (!_available || _doc == null) return string.Empty;
			if (string.IsNullOrEmpty (id)) return string.Empty;

			XmlNode root = _doc.DocumentElement;
			if (root == null) return string.Empty;

			foreach (XmlNode node in root.ChildNodes)
			{
				var attr = node.Attributes? ["id"];
				if (attr == null) continue;
				if (!string.Equals (attr.Value, id, StringComparison.OrdinalIgnoreCase)) continue;

				// Build dpi -> path map
				var map = new Dictionary<int, string> ();
				foreach (XmlNode child in node.ChildNodes)
				{
					var dpiAttr = child.Attributes? ["dpi"];
					string dpiStr = dpiAttr != null ? dpiAttr.Value : "default";
					string text = child.InnerText ?? string.Empty;
					int dpiKey = 0;
					if (string.Equals (dpiStr, "default", StringComparison.OrdinalIgnoreCase))
					{
						dpiKey = 0;
					}
					else
					{
						int parsed;
						if (int.TryParse (dpiStr, out parsed)) dpiKey = parsed;
						else dpiKey = 0;
					}
					// Resolve relative path relative to xml file directory
					string candidate = text;
					if (!Path.IsPathRooted (candidate) && !string.IsNullOrEmpty (_filepath))
					{
						string dir = Path.GetDirectoryName (_filepath);
						if (!string.IsNullOrEmpty (dir))
							candidate = Path.Combine (dir, candidate);
					}
					// Normalize
					try { candidate = Path.GetFullPath (candidate); } catch { /* ignore */ }

					// Insert/overwrite by key (keep last if duplicate)
					map [dpiKey] = candidate;
				}

				// Convert map to list and sort by string value (to mimic original)
				var list = map.Select (kv => new KeyValuePair<int, string> (kv.Key, kv.Value)).ToList ();
				list.Sort ((a, b) => string.CompareOrdinal (a.Value, b.Value));

				// Keep only those whose file exists
				var existList = new List<KeyValuePair<int, string>> ();
				foreach (var kv in list)
				{
					if (!string.IsNullOrEmpty (kv.Value) && File.Exists (kv.Value))
						existList.Add (kv);
				}

				int dpiPercent = UITheme.GetDPI (); // uses earlier helper

				// Find first with dpi >= dpiPercent
				foreach (var kv in existList)
				{
					if (kv.Key >= dpiPercent) return kv.Value;
				}

				// otherwise return first existing candidate
				if (existList.Count > 0) return existList [0].Value;

				return string.Empty;
			}

			return string.Empty;
		}

		// Overloads for convenience
		//public string Get (System.String idAsString) { return Get (idAsString); }
	}
	public class StringResXmlDoc: IDisposable
	{
		private XmlDocument doc;
		private bool isValid;

		public bool IsValid
		{
			get { return isValid; }
		}

		public StringResXmlDoc () { }

		public StringResXmlDoc (string filePath)
		{
			Create (filePath);
		}

		public bool Create (string filePath)
		{
			Destroy ();

			if (string.IsNullOrEmpty (filePath) || !File.Exists (filePath))
				return false;

			try
			{
				doc = new XmlDocument ();
				doc.Load (filePath);
				isValid = true;
				return true;
			}
			catch
			{
				Destroy ();
				return false;
			}
		}

		public void Destroy ()
		{
			doc = null;
			isValid = false;
		}

		public void Dispose ()
		{
			Destroy ();
			GC.SuppressFinalize (this);
		}

		public string Get (string id)
		{
			if (!isValid || doc == null || string.IsNullOrEmpty (id))
				return string.Empty;

			XmlElement root = doc.DocumentElement;
			if (root == null)
				return string.Empty;

			foreach (XmlNode node in root.ChildNodes)
			{
				if (node.Attributes == null)
					continue;

				XmlAttribute idAttr = node.Attributes ["id"];
				if (idAttr == null)
					continue;

				if (!StringEqualsNormalized (idAttr.Value, id))
					continue;

				Dictionary<string, string> langValues =
					new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);

				foreach (XmlNode sub in node.ChildNodes)
				{
					if (sub.Attributes == null)
						continue;

					XmlAttribute langAttr = sub.Attributes ["name"];
					if (langAttr == null)
						continue;

					string lang = langAttr.Value;
					if (!string.IsNullOrEmpty (lang))
					{
						langValues [lang] = sub.InnerText;
					}
				}

				return GetSuitableLanguageValue (langValues);
			}

			return string.Empty;
		}

		public string this [string id]
		{
			get { return Get (id); }
		}

		private static bool StringEqualsNormalized (string a, string b)
		{
			return string.Equals (
				a != null ? a.Trim () : null,
				b != null ? b.Trim () : null,
				StringComparison.OrdinalIgnoreCase
			);
		}

		private static string GetSuitableLanguageValue (
			Dictionary<string, string> values)
		{
			if (values == null || values.Count == 0)
				return string.Empty;

			CultureInfo culture = CultureInfo.CurrentUICulture;
			string val;

			if (values.TryGetValue (culture.Name, out val))
				return val;

			if (values.TryGetValue (culture.TwoLetterISOLanguageName, out val))
				return val;

			if (values.TryGetValue ("en", out val))
				return val;

			foreach (string v in values.Values)
				return v;

			return string.Empty;
		}
	}
}
