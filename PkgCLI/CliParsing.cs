using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PkgCLI
{
	public class NormalizeStringComparer: IEqualityComparer<string>
	{
		public bool Equals (string x, string y) => Polyfill.NEquals (x, y);
		public int GetHashCode (string obj) => obj.NNormalize ().GetHashCode ();
	}
	public class NormalizeCharacterComparer: IEqualityComparer<char>
	{
		public bool Equals (char x, char y) => char.ToLowerInvariant (x) == char.ToLowerInvariant (y);
		public int GetHashCode (char obj) => char.ToLowerInvariant (obj).GetHashCode ();
	}
	public class StringNSet: HashSet<string>
	{
		public StringNSet () : base (new NormalizeStringComparer ()) { }
		public StringNSet (IEnumerable<string> list) : base (list, new NormalizeStringComparer ()) { }
	}
	public class CharNSet: HashSet<char>
	{
		public CharNSet () : base (new NormalizeCharacterComparer ()) { }
		public CharNSet (IEnumerable<char> list) : base (list, new NormalizeCharacterComparer ()) { }
	}
	public static class CliParsingConst
	{
		public static readonly char [] defaultPrefixs = new char [] { '/', '-' };
		public static readonly char [] defaultPostfixs = new char [] { '=', ':' };
		public static readonly string [] emptyStringArray = new string [] { };
	}
	public class CmdParamName: IEquatable<CmdParamName>, IComparable<CmdParamName>, IDisposable
	{
		private string _id = "";
		private string _name = "";
		/// <summary>
		/// 命令唯一标识，但不作为命令名，仅用于内部识别。命令 ID 可以作为命令名或别名。
		/// </summary>
		public string Id { get { return _id.NNormalize (); } set { _id = value; } }
		/// <summary>
		/// 命令名，唯一。命令名不能与别名重复。首字符不能为前缀中的字符，尾字符不能为后缀中的字符。
		/// </summary>
		public string Name { get { return _name.NNormalize (); } set { _name = value; } }
		/// <summary>
		/// 命令别名，唯一。不能与命令名重复。首字符不能为前缀中的字符，尾字符不能为后缀中的字符。
		/// </summary>
		/// </summary>
		public StringNSet Aliases { get; private set; } = new StringNSet ();
		/// <summary>
		/// 命令前缀，为一个字符，标点符号。不能为命令名或别名的首字符。
		/// </summary>
		public CharNSet Prefixs { get; set; } = new CharNSet (CliParsingConst.defaultPrefixs);
		/// <summary>
		/// 命令后缀，为一个字符，标点符号。不能为命令名或别名的尾字符。
		/// </summary>
		public CharNSet Postfixs { get; set; } = new CharNSet (CliParsingConst.defaultPostfixs);
		public bool Equals (CmdParamName other)
		{
			if (other == null) return false;
			if (ReferenceEquals (this, other)) return true;
			return string.Equals (this.Id, other.Id, StringComparison.Ordinal);
		}
		public int CompareTo (CmdParamName other)
		{
			if (other == null) return 1;
			if (ReferenceEquals (this, other)) return 0;
			return string.Compare (this.Id, other.Id, StringComparison.Ordinal);
		}
		public override bool Equals (object obj) => Equals (obj as CmdParamName);
		public override int GetHashCode () => this.Id?.GetHashCode () ?? 0;
		public bool ParamContains (string param)
		{
			var ret = Name.NEquals (param);
			if (!ret)
			{
				foreach (var alias in Aliases)
				{
					if (alias.NEquals (param)) return true;
				}
			}
			return ret;
		}
		public void Dispose ()
		{
			Aliases?.Clear ();
			Prefixs?.Clear ();
		}
		public CmdParamName (string id, string name, IEnumerable<string> aliases, IEnumerable<char> prefixs, IEnumerable<char> postfixs)
		{
			Id = id;
			Name = name;
			Aliases = new StringNSet (aliases);
			Prefixs = new CharNSet (prefixs);
			Postfixs = new CharNSet (postfixs);
		}
		public CmdParamName (string name, IEnumerable<string> aliases) : this (name, name, aliases, CliParsingConst.defaultPrefixs, CliParsingConst.defaultPostfixs) { }
		public CmdParamName (string name) : this (name, CliParsingConst.emptyStringArray) { }
		public CmdParamName () { }
	}
	public class CommandParam: IEquatable<string>
	{
		private string _id = "";
		public string Id { get { return _id.NNormalize (); } set { _id = value; } }
		public string Value = "";
		public bool Equals (string other)
		{
			return (_id ?? "").NEquals (other);
		}
		public override bool Equals (object obj)
		{
			if (obj is string) return Equals (obj as string);
			else if (obj is CommandParam) return Equals ((obj as CommandParam).Id);
			else return base.Equals (obj);
		}
		public override int GetHashCode ()
		{
			return Id.GetHashCode ();
		}
	}
	public class CliParsing: IDisposable
	{
		public HashSet<CmdParamName> Params { get; set; } = new HashSet<CmdParamName> ();
		public void Dispose ()
		{
			Params = null;
		}
		public List<CommandParam> Parse (string [] args)
		{
			var ret = new List<CommandParam> ();
			CommandParam last = new CommandParam ();
			for (long i = 0; i < args.LongLength; i++)
			{
				var arg = args [i]?.Trim () ?? "";
				var item = args [i]?.NNormalize () ?? "";
				if (string.IsNullOrWhiteSpace (item)) continue;
				var first = item [0];
				bool isfind = false;
				foreach (var param in Params)
				{
					if (param.Prefixs.Contains (first))
					{
						var minser = param.Postfixs.Select (e => {
							var index = item.IndexOf (e);
							return index == -1 ? int.MaxValue : index;
						}).Min ();
						string paramPart, postfixPart;
						if (minser == int.MaxValue)
						{
							paramPart = arg.Substring (1);
							postfixPart = "";
						}
						else
						{
							paramPart = arg.Substring (1, minser - 1);
							postfixPart = arg.Substring (minser + 1);
						}
						if (param.ParamContains (paramPart))
						{
							isfind = true;
							var cmdParam = new CommandParam ();
							cmdParam.Id = param.Id;
							if (!string.IsNullOrEmpty (postfixPart))
								cmdParam.Value = postfixPart;
							last = cmdParam;
							ret.Add (cmdParam);
							break;
						}
					}
				}
				if (!isfind)
				{
					var valueparam = new CommandParam ();
					valueparam.Value = arg;
					ret.Add (valueparam);
				}
			}
			return ret;
		}
	}
	public static class CliPasingUtils
	{
		public static bool ParamContains (this List<CommandParam> cpl, string id)
		{
			foreach (var i in cpl)
			{
				if (i.Id.NEquals (id)) return true;
			}
			return false;
		}
		public static bool ParamContains (this List<CommandParam> cpl, CmdParamName param)
		{
			foreach (var i in cpl)
			{
				if (i.Id.NEquals (param.Id)) return true;
			}
			return false;
		}
		public static bool ParamsContainsOr (this List<CommandParam> cpl, params string [] ids)
		{
			foreach (var i in cpl)
			{
				foreach (var j in ids)
				{
					if (i.Id.NEquals (j)) return true;
				}
			}
			return false;
		}
		public static bool ParamsContainsAnd (this List<CommandParam> cpl, params string [] ids)
		{
			if (ids == null || ids.Length == 0) return true;
			foreach (var id in ids)
			{
				if (!ParamContains (cpl, id)) return false;
			}
			return true;
		}
		public static CommandParam GetFromId (this List <CommandParam> cpl, string id)
		{
			foreach (var c in cpl)
			{
				if (c.Id.NEquals (id)) return c;
			}
			return null;
		}
	}
}
