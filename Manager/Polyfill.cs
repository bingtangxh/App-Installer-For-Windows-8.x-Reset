using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Manager
{
	public static class Polyfill
	{
		public static T ParseTo <T> (this string src, T dflt = default (T))
		{
			if (string.IsNullOrWhiteSpace (src)) return dflt;
			try
			{
				Type targetType = typeof (T);
				Type underlying = Nullable.GetUnderlyingType (targetType);
				if (underlying != null)
				{
					object v = Convert.ChangeType (src, underlying, CultureInfo.InvariantCulture);
					return (T)v;
				}
				if (targetType.IsEnum)
				{
					object enumValue = Enum.Parse (targetType, src, true);
					return (T)enumValue;
				}
				TypeConverter converter = TypeDescriptor.GetConverter (targetType);
				if (converter != null && converter.CanConvertFrom (typeof (string)))
				{
					object v = converter.ConvertFrom (null, CultureInfo.InvariantCulture, src);
					return (T)v;
				}
			}
			catch { }
			return dflt;
		}
	}
}
