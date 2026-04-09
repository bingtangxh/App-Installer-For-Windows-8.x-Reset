using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AppxPackage;
using AppxPackage.Info;
using Newtonsoft.Json;

namespace PkgCLI
{
	public static class Polyfill
	{
		static public bool NEquals (this string s1, string s2)
		{
			if (string.IsNullOrWhiteSpace (s1) && string.IsNullOrWhiteSpace (s2)) return true;
			else return s1?.Trim ()?.ToLowerInvariant () == s2?.Trim ()?.ToLowerInvariant ();
		}
		static public string NNormalize (this string s1)
		{
			return s1?.Trim ()?.ToLowerInvariant ();
		}
		static public string Join<T> (this IEnumerable<T> ie, string divide = ", ")
		{
			var ret = "";
			for (var i = 0; i < ie.Count (); i++)
			{
				if (i != 0) ret += divide;
				var item = ie.ElementAt (i);
				if (item == null) ret += "(null)";
				else ret += item.ToString ();
			}
			return ret;
		}
		/// <summary>
		/// 将对象通过指定的选择器转换为目标类型。
		/// 类似于 LINQ 的 Select，但适用于单一对象。
		/// </summary>
		/// <typeparam name="T">目标类型</typeparam>
		/// <param name="obj">源对象</param>
		/// <param name="selector">转换委托，输入源对象，输出目标对象</param>
		/// <returns>转换后的结果</returns>
		public static T ToOther<T> (this object obj, Func<object, T> selector)
		{
			if (selector == null)
				throw new ArgumentNullException (nameof (selector));
			return selector (obj);
		}
		/// <summary>
		/// 将字典格式化为对齐的文本，每个键值对一行，分隔符垂直对齐。
		/// </summary>
		/// <param name="dict">要格式化的字典</param>
		/// <param name="separator">分隔符（如 ":", "="），默认为 ":"</param>
		/// <param name="indent">每行前的缩进字符串，默认为空</param>
		/// <param name="sortKeys">是否按键名排序（不区分大小写），默认为 false</param>
		/// <returns>格式化后的字符串，例如：
		/// Name : Alice
		/// Age  : 30
		/// </returns>
		public static string FormatDictionaryAligned (
			this IDictionary<string, string> dict,
			string separator = ":",
			string indent = "",
			bool sortKeys = false)
		{
			if (dict == null || dict.Count == 0)
				return string.Empty;
			var keys = sortKeys
				? dict.Keys.OrderBy (k => k, StringComparer.OrdinalIgnoreCase).ToList ()
				: dict.Keys.ToList ();
			int maxKeyLength = keys.Max (k => k.Length);
			var sb = new StringBuilder ();
			foreach (string key in keys)
			{
				string value = dict [key] ?? string.Empty;
				sb.AppendLine ($"{indent}{key.PadRight (maxKeyLength)} {separator} {value}");
			}
			return sb.ToString ().TrimEnd (Environment.NewLine.ToCharArray ());
		}
		public static string Format (this string format, params object [] args)
		{
			return String.Format (format, args);
		}
		public static string Format (this string format, object args)
		{
			return String.Format (format, args);
		}
	}
	public static class PackageReaderExt
	{
		static public object GetJsonObjectForCli (this PackageReader pr)
		{
			var id = pr.Identity;
			var prop = pr.Properties;
			var pre = pr.Prerequisites;
			var apps = pr.Applications;
			var caps = pr.Capabilities;
			var deps = pr.Dependencies;
			dynamic obj = new {
				valid = pr.IsValid,
				type = pr.Type.ToOther (e => {
					switch ((AppxPackage.Info.PackageType)e)
					{
						case AppxPackage.Info.PackageType.Appx: return "appx";
						case AppxPackage.Info.PackageType.Bundle: return "bundle";
						default:
						case AppxPackage.Info.PackageType.Unknown: return "unknown";
					}
				}),
				role = pr.Role.ToOther (o => {
					switch ((AppxPackage.Info.PackageRole)o)
					{
						case AppxPackage.Info.PackageRole.Application: return "application";
						case AppxPackage.Info.PackageRole.Framework: return "framework";
						case AppxPackage.Info.PackageRole.Resource: return "resource";
						default:
						case AppxPackage.Info.PackageRole.Unknown: return "unknown";
					}
				}),
				identity = new {
					name = id.Name,
					publisher = id.Publisher,
					version = id.Version.Expression,
					realVersion = id.RealVersion.Expression,
					architecture = id.ProcessArchitecture.Select (e => {
						switch (e)
						{
							case AppxPackage.Info.Architecture.ARM: return "arm";
							case AppxPackage.Info.Architecture.ARM64: return "arm64";
							case AppxPackage.Info.Architecture.Neutral: return "neutral";
							default:
							case AppxPackage.Info.Architecture.Unknown: return "unknown";
							case AppxPackage.Info.Architecture.x64: return "x64";
							case AppxPackage.Info.Architecture.x86: return "x86";
						}
					}).ToList (),
					familyName = id.FamilyName,
					fullName = id.FullName,
					resourceId = id.ResourceId
				},
				properties = new {
					displayName = prop.DisplayName,
					publisherDisplayName = prop.Publisher,
					description = prop.Description,
					logo = prop.Logo,
					framework = prop.Framework,
					resourcePackage = prop.ResourcePackage
				},
				prerequisite = new {
					osMinVersion = pre.OSMinVersion.ToString (),
					osMaxVersionTested = pre.OSMaxVersionTested.ToString (),
					_osMinVersion = pre.OSMinVersionDescription,
					_osMaxVersionTested = pre.OSMaxVersionDescription
				},
				applications = apps.Select (e => {
					var dict = new Dictionary<string, string> ();
					foreach (var kv in e)
					{
						if (kv.Key.IndexOf ("Base64") >= 0) continue;
						var value = e.NewAt (kv.Key, pr.EnablePri);
						if (string.IsNullOrWhiteSpace (value))
							value = e.At (kv.Key);
						dict [kv.Key] = value;
					}
					return dict;
				}).ToList (),
				capabilities = new {
					capabilities = caps.Capabilities,
					deviceCapabilities = caps.DeviceCapabilities
				},
				dependencies = deps.Select (e => new {
					name = e.Name,
					publisher = e.Publisher,
					minVersion = e.Version.ToString ()
				})
			};
			return obj;
		}
		/// <summary>
		/// 从 PackageReader 中获取指定路径的值。
		/// 支持格式：路径中可使用 '.' 或 ':' 分隔，支持索引器 '[index]'，支持 '.length'。
		/// 大小写不敏感，自动去除首尾空白。
		/// 示例：
		///   "Identity"                      -> 返回 Identity 字典
		///   "Identity.Name"                 -> 返回名称
		///   "Properties.Publisher"          -> 返回发布者显示名称（别名）
		///   "applications[0]"               -> 返回第一个应用字典
		///   "applications.length"           -> 返回应用个数
		///   "Applications[0].LogoBase64"    -> 返回第一个应用的 LogoBase64
		/// </summary>
		public static object GetItem (this PackageReader pr, string item)
		{
			if (pr == null) throw new ArgumentNullException ("pr");
			if (string.IsNullOrWhiteSpace (item)) return null;

			string path = item.Trim ().Replace (':', '.');
			object result = ResolvePath (pr, path);
			return SerializeObject (result, pr, false);
		}

		private static object ResolvePath (object target, string path)
		{
			if (target == null) return null;
			if (string.IsNullOrEmpty (path)) return target;

			string [] segments = path.Split ('.');
			object current = target;

			foreach (string seg in segments)
			{
				if (current == null) return null;

				if (seg.Contains ("[") && seg.Contains ("]"))
				{
					int bracketStart = seg.IndexOf ('[');
					string propName = seg.Substring (0, bracketStart);
					string indexStr = seg.Substring (bracketStart + 1, seg.Length - bracketStart - 2);
					int index;
					if (!int.TryParse (indexStr, out index))
						return null;

					object collection = GetPropertyValue (current, propName);
					if (collection == null) return null;
					current = GetIndexedValue (collection, index);
				}
				else if (string.Equals (seg, "length", StringComparison.OrdinalIgnoreCase) ||
						 string.Equals (seg, "count", StringComparison.OrdinalIgnoreCase))
				{
					current = GetLength (current);
				}
				else
				{
					current = GetPropertyValue (current, seg);
				}
			}
			return current;
		}

		private static object GetPropertyValue (object target, string propName)
		{
			if (target == null) return null;
			Type type = target.GetType ();

			// 根对象 PackageReader 的属性名映射（不区分大小写）
			if (type == typeof (PackageReader))
			{
				string mapped = null;
				string lower = propName.ToLowerInvariant ();
				switch (lower)
				{
					case "pkgid":
					case "id":
					case "identity": mapped = "Identity"; break;
					case "prop":
					case "properties": mapped = "Properties"; break;
					case "prerequistes":
					case "prerequisite":
					case "prerequisites": mapped = "Prerequisites"; break;
					case "res":
					case "resources": mapped = "Resources"; break;
					case "apps":
					case "applications": mapped = "Applications"; break;
					case "caps":
					case "capabilities": mapped = "Capabilities"; break;
					case "deps":
					case "dependencies": mapped = "Dependencies"; break;
					case "type": mapped = "Type"; break;
					case "role": mapped = "Role"; break;
					case "valid":
					case "isvalid": mapped = "IsValid"; break;
					case "filepath": mapped = "FilePath"; break;
				}
				if (mapped != null)
					propName = mapped;
			}

			// PRProperties 别名：Publisher / PublisherDisplayName 都映射到 Publisher 属性
			if (type == typeof (PRProperties))
			{
				if (string.Equals (propName, "Publisher", StringComparison.OrdinalIgnoreCase) ||
					string.Equals (propName, "PublisherDisplayName", StringComparison.OrdinalIgnoreCase))
				{
					propName = "Publisher";
				}
			}

			// PRApplication 中以 Base64 结尾的属性 -> 调用 NewAtBase64
			if (type == typeof (PRApplication) && propName.EndsWith ("Base64", StringComparison.OrdinalIgnoreCase))
			{
				string baseKey = propName.Substring (0, propName.Length - 6);
				PRApplication app = (PRApplication)target;
				MethodInfo method = typeof (PRApplication).GetMethod ("NewAtBase64", BindingFlags.Public | BindingFlags.Instance);
				if (method != null)
				{
					return method.Invoke (app, new object [] { baseKey });
				}
				return null;
			}

			// PRApplication 普通属性访问（字典键，大小写不敏感）
			if (type == typeof (PRApplication))
			{
				PRApplication app = (PRApplication)target;
				// 直接使用索引器，内部已处理资源解析和大小写不敏感
				return app [propName];
			}

			// MRApplication 同理
			if (type == typeof (MRApplication))
			{
				MRApplication app = (MRApplication)target;
				return app [propName];
			}

			// 反射属性（不区分大小写）
			PropertyInfo prop = type.GetProperty (propName,
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			if (prop != null)
			{
				return prop.GetValue (target, null);
			}

			// 反射字段
			FieldInfo field = type.GetField (propName,
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			if (field != null)
			{
				return field.GetValue (target);
			}

			return null;
		}

		private static object GetIndexedValue (object collection, int index)
		{
			if (collection == null) return null;

			// PRApplications
			PRApplications apps = collection as PRApplications;
			if (apps != null)
			{
				if (index >= 0 && index < apps.Applications.Count)
					return apps.Applications [index];
				return null;
			}

			// IList
			IList list = collection as IList;
			if (list != null && index >= 0 && index < list.Count)
				return list [index];

			// 索引器属性
			PropertyInfo indexer = collection.GetType ().GetProperty ("Item", new [] { typeof (int) });
			if (indexer != null)
				return indexer.GetValue (collection, new object [] { index });

			return null;
		}

		private static int? GetLength (object obj)
		{
			if (obj == null) return null;
			PRApplications apps = obj as PRApplications;
			if (apps != null) return apps.Applications.Count;
			PRDependencies deps = obj as PRDependencies;
			if (deps != null) return deps.Dependencies.Count;
			IList list = obj as IList;
			if (list != null) return list.Count;
			ICollection coll = obj as ICollection;
			if (coll != null) return coll.Count;
			return null;
		}

		private static object SerializeObject (object obj, PackageReader pr, bool filterBase64)
		{
			if (obj == null) return null;

			Type type = obj.GetType ();
			if (type.IsPrimitive || obj is string || obj is decimal || obj is Enum)
				return obj;

			// PRIdentity
			if (type == typeof (PRIdentity))
			{
				PRIdentity id = (PRIdentity)obj;
				return new {
					name = id.Name,
					publisher = id.Publisher,
					version = id.Version.Expression,
					realVersion = id.RealVersion.Expression,
					architecture = id.ProcessArchitecture.Select (e => {
						switch (e)
						{
							case Architecture.ARM: return "arm";
							case Architecture.ARM64: return "arm64";
							case Architecture.Neutral: return "neutral";
							case Architecture.x64: return "x64";
							case Architecture.x86: return "x86";
							default: return "unknown";
						}
					}).ToList (),
					familyName = id.FamilyName,
					fullName = id.FullName,
					resourceId = id.ResourceId
				};
			}

			// PRProperties
			if (type == typeof (PRProperties))
			{
				PRProperties prop = (PRProperties)obj;
				return new {
					displayName = prop.DisplayName,
					publisherDisplayName = prop.Publisher,
					description = prop.Description,
					logo = prop.Logo,
					framework = prop.Framework,
					resourcePackage = prop.ResourcePackage
				};
			}

			// PRPrerequisites
			if (type == typeof (PRPrerequisites))
			{
				PRPrerequisites pre = (PRPrerequisites)obj;
				return new {
					osMinVersion = pre.OSMinVersion,
					osMaxVersionTested = pre.OSMaxVersionTested,
					_osMinVersion = pre.OSMinVersionDescription,
					_osMaxVersionTested = pre.OSMaxVersionDescription
				};
			}

			// PRCapabilities
			if (type == typeof (PRCapabilities))
			{
				PRCapabilities caps = (PRCapabilities)obj;
				return new {
					capabilities = caps.Capabilities,
					deviceCapabilities = caps.DeviceCapabilities
				};
			}

			// PRDependencies
			if (type == typeof (PRDependencies))
			{
				PRDependencies deps = (PRDependencies)obj;
				return deps.Select (e => new {
					name = e.Name,
					publisher = e.Publisher,
					minVersion = e.Version.ToString ()
				}).ToList ();
			}

			// PRApplications (集合)
			if (type == typeof (PRApplications))
			{
				PRApplications apps = (PRApplications)obj;
				return apps.Select (e => SerializeObject (e, pr, filterBase64)).ToList ();
			}

			// PRApplication (单个应用)
			if (type == typeof (PRApplication))
			{
				PRApplication app = (PRApplication)obj;
				var dict = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
				foreach (var kv in app)
				{
					if (kv.Key.IndexOf ("Base64", StringComparison.OrdinalIgnoreCase) >= 0)
						continue;
					string value = app.NewAt (kv.Key, pr.EnablePri);
					if (string.IsNullOrWhiteSpace (value))
						value = app.At (kv.Key);
					dict [kv.Key] = value;
				}
				return dict;
			}

			// IDictionary
			IDictionary dictObj = obj as IDictionary;
			if (dictObj != null)
			{
				var result = new Dictionary<string, object> (StringComparer.OrdinalIgnoreCase);
				foreach (DictionaryEntry entry in dictObj)
				{
					string key = entry.Key?.ToString ();
					if (string.IsNullOrEmpty (key)) continue;
					if (filterBase64 && key.EndsWith ("_Base64", StringComparison.OrdinalIgnoreCase))
						continue;
					result [key] = SerializeObject (entry.Value, pr, filterBase64);
				}
				return result;
			}

			// IEnumerable (非字符串)
			if (!(obj is string))
			{
				IEnumerable enumerable = obj as IEnumerable;
				if (enumerable != null)
				{
					var list = new List<object> ();
					foreach (var item in enumerable)
						list.Add (SerializeObject (item, pr, filterBase64));
					return list;
				}
			}

			// 后备：反射属性
			var props = type.GetProperties (BindingFlags.Public | BindingFlags.Instance);
			var propDict = new Dictionary<string, object> (StringComparer.OrdinalIgnoreCase);
			foreach (var prop in props)
			{
				if (prop.GetIndexParameters ().Length > 0) continue;
				string propName = prop.Name;
				if (filterBase64 && propName.EndsWith ("Base64", StringComparison.OrdinalIgnoreCase))
					continue;
				object val = prop.GetValue (obj, null);
				propDict [propName] = SerializeObject (val, pr, filterBase64);
			}
			return propDict;
		}
	}
	public static class ManifestReaderExt
	{
		/// <summary>
		/// 获取用于 CLI 输出的 JSON 对象（与 PackageReaderExt.GetJsonObjectForCli 结构一致）。
		/// </summary>
		public static object GetJsonObjectForCli (this ManifestReader mr)
		{
			var id = mr.Identity;
			var prop = mr.Properties;
			var pre = mr.Prerequisites;
			var apps = mr.Applications;
			var caps = mr.Capabilities;
			var deps = mr.Dependencies;
			var res = mr.Resources;

			dynamic obj = new {
				valid = mr.IsValid,
				type = mr.Type.ToOther (e => {
					switch ((PackageType)e)
					{
						case PackageType.Appx: return "appx";
						case PackageType.Bundle: return "bundle";
						default:
						case PackageType.Unknown: return "unknown";
					}
				}),
				role = mr.Role.ToOther (o => {
					switch ((PackageRole)o)
					{
						case PackageRole.Application: return "application";
						case PackageRole.Framework: return "framework";
						case PackageRole.Resource: return "resource";
						default:
						case PackageRole.Unknown: return "unknown";
					}
				}),
				identity = new {
					name = id.Name,
					publisher = id.Publisher,
					version = id.Version.Expression,
					architecture = id.ProcessArchitecture.Select (e => {
						switch (e)
						{
							case Architecture.ARM: return "arm";
							case Architecture.ARM64: return "arm64";
							case Architecture.Neutral: return "neutral";
							case Architecture.x64: return "x64";
							case Architecture.x86: return "x86";
							default: return "unknown";
						}
					}).ToList (),
					familyName = id.FamilyName,
					fullName = id.FullName,
					resourceId = id.ResourceId
				},
				properties = new {
					displayName = prop.DisplayName,
					publisherDisplayName = prop.Publisher,
					description = prop.Description,
					logo = prop.Logo,
					framework = prop.Framework,
					resourcePackage = prop.ResourcePackage
				},
				prerequisite = new {
					osMinVersion = pre.OSMinVersion.ToString (),
					osMaxVersionTested = pre.OSMaxVersionTested.ToString (),
					_osMinVersion = pre.OSMinVersionDescription,
					_osMaxVersionTested = pre.OSMaxVersionDescription
				},
				resources = new {
					languages = res.Languages,
					scales = res.Scales,
					dxFeatureLevels = res.DXFeatures.Select (d => {
						switch (d)
						{
							case DXFeatureLevel.Level9: return 9;
							case DXFeatureLevel.Level10: return 10;
							case DXFeatureLevel.Level11: return 11;
							case DXFeatureLevel.Level12: return 12;
							default: return -1;
						}
					}).ToList ()
				},
				applications = apps.Select (e => {
					var dict = new Dictionary<string, string> ();
					foreach (var kv in e)
					{
						if (kv.Key.IndexOf ("Base64", StringComparison.OrdinalIgnoreCase) >= 0)
							continue;
						string value = e.NewAt (kv.Key, mr.EnablePri);
						if (string.IsNullOrWhiteSpace (value))
							value = e.At (kv.Key);
						dict [kv.Key] = value;
					}
					return dict;
				}).ToList (),
				capabilities = new {
					capabilities = caps.Capabilities,
					deviceCapabilities = caps.DeviceCapabilities
				},
				dependencies = deps.Select (e => new {
					name = e.Name,
					publisher = e.Publisher,
					minVersion = e.Version.ToString ()
				})
			};
			return obj;
		}

		/// <summary>
		/// 从 ManifestReader 中获取指定路径的值。
		/// 支持格式：路径中可使用 '.' 或 ':' 分隔，支持索引器 '[index]'，支持 '.length'。
		/// 大小写不敏感，自动去除首尾空白。
		/// 示例：
		///   "Identity"                      -> 返回 Identity 字典
		///   "Identity.Name"                 -> 返回名称
		///   "Properties.Publisher"          -> 返回发布者显示名称（别名）
		///   "applications[0]"               -> 返回第一个应用字典
		///   "applications.length"           -> 返回应用个数
		///   "Applications[0].LogoBase64"    -> 返回第一个应用的 LogoBase64
		/// </summary>
		public static object GetItem (this ManifestReader mr, string item)
		{
			if (mr == null) throw new ArgumentNullException ("mr");
			if (string.IsNullOrWhiteSpace (item)) return null;

			string path = item.Trim ().Replace (':', '.');
			object result = ResolvePath (mr, path);
			return SerializeObject (result, mr, false);
		}

		private static object ResolvePath (object target, string path)
		{
			if (target == null) return null;
			if (string.IsNullOrEmpty (path)) return target;

			string [] segments = path.Split ('.');
			object current = target;

			foreach (string seg in segments)
			{
				if (current == null) return null;

				if (seg.Contains ("[") && seg.Contains ("]"))
				{
					int bracketStart = seg.IndexOf ('[');
					string propName = seg.Substring (0, bracketStart);
					string indexStr = seg.Substring (bracketStart + 1, seg.Length - bracketStart - 2);
					int index;
					if (!int.TryParse (indexStr, out index))
						return null;

					object collection = GetPropertyValue (current, propName);
					if (collection == null) return null;
					current = GetIndexedValue (collection, index);
				}
				else if (string.Equals (seg, "length", StringComparison.OrdinalIgnoreCase) ||
						 string.Equals (seg, "count", StringComparison.OrdinalIgnoreCase))
				{
					current = GetLength (current);
				}
				else
				{
					current = GetPropertyValue (current, seg);
				}
			}
			return current;
		}

		private static object GetPropertyValue (object target, string propName)
		{
			if (target == null) return null;
			Type type = target.GetType ();

			// 根对象 ManifestReader 的属性名映射（不区分大小写）
			if (type == typeof (ManifestReader))
			{
				string mapped = null;
				string lower = propName.ToLowerInvariant ();
				switch (lower)
				{
					case "pkgid":
					case "id":
					case "identity": mapped = "Identity"; break;
					case "prop":
					case "properties": mapped = "Properties"; break;
					case "prerequistes":
					case "prerequisite":
					case "prerequisites": mapped = "Prerequisites"; break;
					case "res":
					case "resources": mapped = "Resources"; break;
					case "apps":
					case "applications": mapped = "Applications"; break;
					case "caps":
					case "capabilities": mapped = "Capabilities"; break;
					case "deps":
					case "dependencies": mapped = "Dependencies"; break;
					case "type": mapped = "Type"; break;
					case "role": mapped = "Role"; break;
					case "valid":
					case "isvalid": mapped = "IsValid"; break;
					case "file":
					case "filepath": mapped = "FilePath"; break;
					case "fileroot": mapped = "FileRoot"; break;
				}
				if (mapped != null)
					propName = mapped;
			}

			// MRProperties 别名：Publisher / PublisherDisplayName 都映射到 Publisher 属性
			if (type == typeof (MRProperties))
			{
				if (string.Equals (propName, "Publisher", StringComparison.OrdinalIgnoreCase) ||
					string.Equals (propName, "PublisherDisplayName", StringComparison.OrdinalIgnoreCase))
				{
					propName = "Publisher";
				}
			}

			// MRApplication 中以 Base64 结尾的属性 -> 调用 NewAtBase64
			if (type == typeof (MRApplication) && propName.EndsWith ("Base64", StringComparison.OrdinalIgnoreCase))
			{
				string baseKey = propName.Substring (0, propName.Length - 6);
				MRApplication app = (MRApplication)target;
				MethodInfo method = typeof (MRApplication).GetMethod ("NewAtBase64", BindingFlags.Public | BindingFlags.Instance);
				if (method != null)
				{
					return method.Invoke (app, new object [] { baseKey });
				}
				return null;
			}

			// MRApplication 普通属性访问（字典键，大小写不敏感）
			if (type == typeof (MRApplication))
			{
				MRApplication app = (MRApplication)target;
				return app [propName];
			}

			// 反射属性（不区分大小写）
			PropertyInfo prop = type.GetProperty (propName,
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			if (prop != null)
			{
				return prop.GetValue (target, null);
			}

			// 反射字段
			FieldInfo field = type.GetField (propName,
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			if (field != null)
			{
				return field.GetValue (target);
			}

			return null;
		}

		private static object GetIndexedValue (object collection, int index)
		{
			if (collection == null) return null;

			MRApplications apps = collection as MRApplications;
			if (apps != null)
			{
				if (index >= 0 && index < apps.Applications.Count)
					return apps.Applications [index];
				return null;
			}

			IList list = collection as IList;
			if (list != null && index >= 0 && index < list.Count)
				return list [index];

			PropertyInfo indexer = collection.GetType ().GetProperty ("Item", new [] { typeof (int) });
			if (indexer != null)
				return indexer.GetValue (collection, new object [] { index });

			return null;
		}

		private static int? GetLength (object obj)
		{
			if (obj == null) return null;
			MRApplications apps = obj as MRApplications;
			if (apps != null) return apps.Applications.Count;
			MRDependencies deps = obj as MRDependencies;
			if (deps != null) return deps.Dependencies.Count;
			IList list = obj as IList;
			if (list != null) return list.Count;
			ICollection coll = obj as ICollection;
			if (coll != null) return coll.Count;
			return null;
		}

		private static object SerializeObject (object obj, ManifestReader mr, bool filterBase64)
		{
			if (obj == null) return null;

			Type type = obj.GetType ();
			if (type.IsPrimitive || obj is string || obj is decimal || obj is Enum)
				return obj;

			// MRIdentity
			if (type == typeof (MRIdentity))
			{
				MRIdentity id = (MRIdentity)obj;
				return new {
					name = id.Name,
					publisher = id.Publisher,
					version = id.Version.Expression,
					architecture = id.ProcessArchitecture.Select (e => {
						switch (e)
						{
							case Architecture.ARM: return "arm";
							case Architecture.ARM64: return "arm64";
							case Architecture.Neutral: return "neutral";
							case Architecture.x64: return "x64";
							case Architecture.x86: return "x86";
							default: return "unknown";
						}
					}).ToList (),
					familyName = id.FamilyName,
					fullName = id.FullName,
					resourceId = id.ResourceId
				};
			}

			// MRProperties
			if (type == typeof (MRProperties))
			{
				MRProperties prop = (MRProperties)obj;
				return new {
					displayName = prop.DisplayName,
					publisherDisplayName = prop.Publisher,
					description = prop.Description,
					logo = prop.Logo,
					framework = prop.Framework,
					resourcePackage = prop.ResourcePackage
				};
			}

			// MRPrerequisites
			if (type == typeof (MRPrerequisites))
			{
				MRPrerequisites pre = (MRPrerequisites)obj;
				return new {
					osMinVersion = pre.OSMinVersion,
					osMaxVersionTested = pre.OSMaxVersionTested,
					_osMinVersion = pre.OSMinVersionDescription,
					_osMaxVersionTested = pre.OSMaxVersionDescription
				};
			}

			// MRResources
			if (type == typeof (MRResources))
			{
				MRResources res = (MRResources)obj;
				return new {
					languages = res.Languages,
					scales = res.Scales,
					dxFeatureLevels = res.DXFeatures.Select (d => {
						switch (d)
						{
							case DXFeatureLevel.Level9: return 9;
							case DXFeatureLevel.Level10: return 10;
							case DXFeatureLevel.Level11: return 11;
							case DXFeatureLevel.Level12: return 12;
							default: return -1;
						}
					}).ToList ()
				};
			}

			// MRCapabilities
			if (type == typeof (MRCapabilities))
			{
				MRCapabilities caps = (MRCapabilities)obj;
				return new {
					capabilities = caps.Capabilities,
					deviceCapabilities = caps.DeviceCapabilities
				};
			}

			// MRDependencies
			if (type == typeof (MRDependencies))
			{
				MRDependencies deps = (MRDependencies)obj;
				return deps.Select (e => new {
					name = e.Name,
					publisher = e.Publisher,
					minVersion = e.Version.ToString ()
				}).ToList ();
			}

			// MRApplications (集合)
			if (type == typeof (MRApplications))
			{
				MRApplications apps = (MRApplications)obj;
				return apps.Select (e => SerializeObject (e, mr, filterBase64)).ToList ();
			}

			// MRApplication (单个应用)
			if (type == typeof (MRApplication))
			{
				MRApplication app = (MRApplication)obj;
				var dict = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
				foreach (var kv in app)
				{
					if (kv.Key.IndexOf ("Base64", StringComparison.OrdinalIgnoreCase) >= 0)
						continue;
					string value = app.NewAt (kv.Key, mr.EnablePri);
					if (string.IsNullOrWhiteSpace (value))
						value = app.At (kv.Key);
					dict [kv.Key] = value;
				}
				return dict;
			}

			// IDictionary
			IDictionary dictObj = obj as IDictionary;
			if (dictObj != null)
			{
				var result = new Dictionary<string, object> (StringComparer.OrdinalIgnoreCase);
				foreach (DictionaryEntry entry in dictObj)
				{
					string key = entry.Key?.ToString ();
					if (string.IsNullOrEmpty (key)) continue;
					if (filterBase64 && key.EndsWith ("_Base64", StringComparison.OrdinalIgnoreCase))
						continue;
					result [key] = SerializeObject (entry.Value, mr, filterBase64);
				}
				return result;
			}

			// IEnumerable (非字符串)
			if (!(obj is string))
			{
				IEnumerable enumerable = obj as IEnumerable;
				if (enumerable != null)
				{
					var list = new List<object> ();
					foreach (var item in enumerable)
						list.Add (SerializeObject (item, mr, filterBase64));
					return list;
				}
			}

			// 后备：反射属性
			var props = type.GetProperties (BindingFlags.Public | BindingFlags.Instance);
			var propDict = new Dictionary<string, object> (StringComparer.OrdinalIgnoreCase);
			foreach (var prop in props)
			{
				if (prop.GetIndexParameters ().Length > 0) continue;
				string propName = prop.Name;
				if (filterBase64 && propName.EndsWith ("Base64", StringComparison.OrdinalIgnoreCase))
					continue;
				object val = prop.GetValue (obj, null);
				propDict [propName] = SerializeObject (val, mr, filterBase64);
			}
			return propDict;
		}
	}
}
