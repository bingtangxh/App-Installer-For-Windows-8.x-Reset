using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
namespace AppxPackage
{
	[Serializable]
	public sealed class Ref<T>
	{
		private T _value;
		public Ref ()
		{
			_value = default (T);
		}
		public Ref (T value)
		{
			_value = value;
		}
		/// <summary>
		/// 模拟 & 引用访问
		/// </summary>
		public T Value
		{
			get { return _value; }
			set { _value = value; }
		}
		/// <summary>
		/// 直接赋值（像 *ref = value）
		/// </summary>
		public void Set (T value)
		{
			_value = value;
		}
		/// <summary>
		/// 取值（像 value = *ref）
		/// </summary>
		public T Get ()
		{
			return _value;
		}
		public override string ToString ()
		{
			return _value == null ? string.Empty : _value.ToString ();
		}
		public override int GetHashCode ()
		{
			return _value == null ? 0 : _value.GetHashCode ();
		}
		public override bool Equals (object obj)
		{
			if (ReferenceEquals (this, obj)) return true;
			if (obj is Ref<T>) return Equals (_value, ((Ref<T>)obj)._value);
			return Equals (_value, obj);
		}
		public static implicit operator T (Ref<T> r)
		{
			return r == null ? default (T) : r._value;
		}
		public static implicit operator Ref<T>(T value)
		{
			return new Ref<T> (value);
		}
	}
}
namespace AppxPackage.Info
{
	public enum Architecture
	{
		x86 = 0,
		ARM = 5,
		x64 = 9,
		Neutral = 11,
		ARM64 = 12,
		Unknown = ushort.MaxValue
	};
	public enum PackageType
	{
		Unknown = 0,
		Appx = 1,
		Bundle = 2
	};
	public enum PackageRole
	{
		Unknown = 0,
		Application = 1,
		Framework = 2,
		Resource = 3
	};
	public interface IIdentity
	{
		string Name { get; }
		string Publisher { get; }
		string FamilyName { get; }
		string FullName { get; }
		string ResourceId { get; }
		DataUtils.Version Version { get; }
		List<Architecture> ProcessArchitecture { get; }
	}
	public interface IProperties
	{
		string DisplayName { get; }
		string Description { get; }
		string Publisher { get; }
		string Logo { get; }
		string LogoBase64 { get; }
		bool Framework { get; }
		bool ResourcePackage { get; }
	}
	public interface ICapabilities
	{
		List<string> Capabilities { get; }
		List<string> DeviceCapabilities { get; }
	}
	public class DependencyInfo
	{
		public string Name { get; private set; } = "";
		public string Publisher { get; private set; } = "";
		public DataUtils.Version Version { get; private set; } = new DataUtils.Version ();
		public DependencyInfo (string name, string publisher, DataUtils.Version ver)
		{
			Name = name;
			Publisher = publisher;
			Version = ver;
		}
		public DependencyInfo (string name, DataUtils.Version ver): this (name, "", ver) { }
		public DependencyInfo () { }
	}
	public enum DXFeatureLevel
	{
		Unspecified = 0,
		Level9 = 0x1,
		Level10 = 0x2,
		Level11 = 0x4,
		Level12 = 0x8
	}
	public interface IResources
	{
		List <string> Languages { get; }
		List <int> Languages_LCID { get; }
		List <int> Scales { get; }
		List <DXFeatureLevel> DXFeatures { get; }
	}
	public interface IPrerequisites
	{
		DataUtils.Version OSMinVersion { get; }
		DataUtils.Version OSMaxVersionTested { get; }
		string OSMinVersionDescription { get; }
		string OSMaxVersionDescription { get; }
	}
	[Serializable]
	[StructLayout (LayoutKind.Sequential)]
	public struct HRESULT: IEquatable<HRESULT>
	{
		private readonly int _value;
		public HRESULT (int value)
		{
			_value = value;
		}
		public int Value
		{
			get { return _value; }
		}
		public bool Succeeded
		{
			get { return _value >= 0; }
		}
		public bool Failed
		{
			get { return _value < 0; }
		}
		public void ThrowIfFailed ()
		{
			if (Failed)
				Marshal.ThrowExceptionForHR (_value);
		}
		public Exception GetException ()
		{
			return Failed ? Marshal.GetExceptionForHR (_value) : null;
		}
		public override string ToString ()
		{
			return string.Format ("HRESULT 0x{0:X8}", _value);
		}
		public override int GetHashCode ()
		{
			return _value;
		}
		public override bool Equals (object obj)
		{
			if (obj is HRESULT) return Equals ((HRESULT)obj);
			return false;
		}
		public bool Equals (HRESULT other)
		{
			return _value == other._value;
		}
		public static implicit operator int (HRESULT hr)
		{
			return hr._value;
		}
		public static implicit operator HRESULT (int value)
		{
			return new HRESULT (value);
		}
		public static bool operator == (HRESULT a, HRESULT b)
		{
			return a._value == b._value;
		}
		public static bool operator != (HRESULT a, HRESULT b)
		{
			return a._value != b._value;
		}
		public static bool operator >= (HRESULT a, int value)
		{
			return a._value >= value;
		}
		public static bool operator <= (HRESULT a, int value)
		{
			return a._value <= value;
		}
		public static bool operator > (HRESULT a, int value)
		{
			return a._value > value;
		}
		public static bool operator < (HRESULT a, int value)
		{
			return a._value < value;
		}
	}
}
