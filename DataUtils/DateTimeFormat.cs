using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace DataUtils
{
	public enum YearFormat
	{
		None = 0,
		Default = 1,
		Abbreviated = 2,
		Full = 3,
	}

	public enum MonthFormat
	{
		None = 0,
		Default = 1,
		Abbreviated = 2,
		Full = 3,
		Numeric = 4,
	}

	public enum DayFormat
	{
		None = 0,
		Default = 1,
	}

	public enum DayOfWeekFormat
	{
		None = 0,
		Default = 1,
		Abbreviated = 2,
		Full = 3,
	}

	public enum HourFormat
	{
		None = 0,
		Default = 1,
	}

	public enum MinuteFormat
	{
		None = 0,
		Default = 1,
	}

	public enum SecondFormat
	{
		None = 0,
		Default = 1,
	}

	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_DateTimeFormatter
	{
		// ---------- 私有字段 ----------
		private string _formatTemplate;           // 原始模板字符串
		private string _formatPattern;            // 实际用于 .NET 格式化的模式
		private List<string> _languages;          // 语言优先级列表
		private string _geographicRegion;         // 地理区域
		private string _calendar = "GregorianCalendar";
		private string _clock = "24HourClock";
		private string _numeralSystem = "Latn";
		private CultureInfo _culture;              // 解析后的 CultureInfo
		private string _resolvedLanguage;          // 实际使用的语言
		private string _resolvedGeographicRegion;  // 实际使用的区域

		// 存储构造时传入的枚举值（用于 IncludeXXX 属性）
		private YearFormat _yearFormat = YearFormat.None;
		private MonthFormat _monthFormat = MonthFormat.None;
		private DayFormat _dayFormat = DayFormat.None;
		private DayOfWeekFormat _dayOfWeekFormat = DayOfWeekFormat.None;
		private HourFormat _hourFormat = HourFormat.None;
		private MinuteFormat _minuteFormat = MinuteFormat.None;
		private SecondFormat _secondFormat = SecondFormat.None;

		// ---------- 辅助方法 ----------
		private static string MapTemplateToPattern (string template, CultureInfo culture, string clock)
		{
			if (string.IsNullOrEmpty (template))
				return string.Empty;

			switch (template.ToLowerInvariant ())
			{
				case "longdate":
					return culture.DateTimeFormat.LongDatePattern;
				case "shortdate":
					return culture.DateTimeFormat.ShortDatePattern;
				case "longtime":
					return culture.DateTimeFormat.LongTimePattern;
				case "shorttime":
					return culture.DateTimeFormat.ShortTimePattern;
				case "dayofweek":
					return culture.DateTimeFormat.ShortestDayNames? [0] ?? "dddd"; // 近似
				case "dayofweek.full":
					return "dddd";
				case "dayofweek.abbreviated":
					return "ddd";
				case "day":
					return "dd";
				case "month":
					return "MMMM";
				case "month.full":
					return "MMMM";
				case "month.abbreviated":
					return "MMM";
				case "month.numeric":
					return "MM";
				case "year":
					return "yyyy";
				case "year.full":
					return "yyyy";
				case "year.abbreviated":
					return "yy";
				case "hour":
					return clock == "24HourClock" ? "HH" : "hh";
				case "minute":
					return "mm";
				case "second":
					return "ss";
				case "timezone":
					return "zzz";
				default:
					// 如果不是预定义模板，则当作自定义模式直接返回
					return template;
			}
		}

		// 根据枚举组合构建格式模式
		private static string BuildPatternFromEnums (YearFormat year, MonthFormat month, DayFormat day, DayOfWeekFormat dayOfWeek,
													 HourFormat hour, MinuteFormat minute, SecondFormat second,
													 string clock, CultureInfo culture)
		{
			var parts = new List<string> ();
			// 日期部分
			if (dayOfWeek != DayOfWeekFormat.None)
			{
				if (dayOfWeek == DayOfWeekFormat.Abbreviated)
					parts.Add ("ddd");
				else // Full or Default
					parts.Add ("dddd");
			}
			if (year != YearFormat.None)
			{
				if (year == YearFormat.Abbreviated)
					parts.Add ("yy");
				else
					parts.Add ("yyyy");
			}
			if (month != MonthFormat.None)
			{
				switch (month)
				{
					case MonthFormat.Numeric:
						parts.Add ("MM");
						break;
					case MonthFormat.Abbreviated:
						parts.Add ("MMM");
						break;
					case MonthFormat.Full:
						parts.Add ("MMMM");
						break;
					default:
						parts.Add ("MM");
						break;
				}
			}
			if (day != DayFormat.None)
			{
				parts.Add ("dd");
			}
			string datePart = string.Join (" ", parts);
			// 时间部分
			var timeParts = new List<string> ();
			if (hour != HourFormat.None)
			{
				if (clock == "24HourClock")
					timeParts.Add ("HH");
				else
					timeParts.Add ("hh");
			}
			if (minute != MinuteFormat.None)
			{
				timeParts.Add ("mm");
			}
			if (second != SecondFormat.None)
			{
				timeParts.Add ("ss");
			}
			string timePart = timeParts.Count > 0 ? string.Join (":", timeParts) : "";
			if (!string.IsNullOrEmpty (datePart) && !string.IsNullOrEmpty (timePart))
				return datePart + " " + timePart;
			if (!string.IsNullOrEmpty (datePart))
				return datePart;
			return timePart;
		}

		// 将 JS Date 对象转换为 DateTime
		private DateTime ConvertJsDateToDateTime (object jsDate)
		{
			if (jsDate == null)
				throw new ArgumentNullException (nameof (jsDate));

			Type type = jsDate.GetType ();
			// 调用 getTime() 获取毫秒数 (double)
			double milliseconds = (double)type.InvokeMember (
				"getTime",
				BindingFlags.InvokeMethod,
				null,
				jsDate,
				null);

			// 手动计算 Unix 纪元转换（兼容低版本 .NET）
			DateTime epoch = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			DateTime utcDateTime = epoch.AddMilliseconds (milliseconds);
			// 返回本地时间（可根据需求调整）
			return utcDateTime.ToLocalTime ();
		}

		// 初始化文化信息
		private void InitializeCulture ()
		{
			string lang = (_languages != null && _languages.Count > 0) ? _languages [0] : CultureInfo.CurrentCulture.Name;
			try
			{
				_culture = new CultureInfo (lang);
				_resolvedLanguage = _culture.Name;
			}
			catch
			{
				_culture = CultureInfo.CurrentCulture;
				_resolvedLanguage = _culture.Name;
			}
			_resolvedGeographicRegion = _geographicRegion ?? _culture.Name;
			// 根据区域和语言更新格式模式（如果使用模板）
			if (!string.IsNullOrEmpty (_formatTemplate))
			{
				_formatPattern = MapTemplateToPattern (_formatTemplate, _culture, _clock);
			}
			else if (_yearFormat != YearFormat.None || _monthFormat != MonthFormat.None ||
					 _dayFormat != DayFormat.None || _dayOfWeekFormat != DayOfWeekFormat.None ||
					 _hourFormat != HourFormat.None || _minuteFormat != MinuteFormat.None ||
					 _secondFormat != SecondFormat.None)
			{
				_formatPattern = BuildPatternFromEnums (_yearFormat, _monthFormat, _dayFormat, _dayOfWeekFormat,
													   _hourFormat, _minuteFormat, _secondFormat, _clock, _culture);
			}
		}

		// ---------- 构造函数 ----------
		public _I_DateTimeFormatter (string formatTemplate)
			: this (formatTemplate, null, null, null, null)
		{
		}

		public _I_DateTimeFormatter (string formatTemplate, IEnumerable<string> languages)
			: this (formatTemplate, languages, null, null, null)
		{
		}

		public _I_DateTimeFormatter (string formatTemplate, IEnumerable<string> languages, string geographicRegion, string calendar, string clock)
		{
			_formatTemplate = formatTemplate;
			_languages = languages == null ? new List<string> () : new List<string> (languages);
			_geographicRegion = geographicRegion;
			if (!string.IsNullOrEmpty (calendar))
				_calendar = calendar;
			if (!string.IsNullOrEmpty (clock))
				_clock = clock;
			InitializeCulture ();
		}

		public _I_DateTimeFormatter (YearFormat year, MonthFormat month, DayFormat day, DayOfWeekFormat dayOfWeek)
			: this (year, month, day, dayOfWeek, HourFormat.None, MinuteFormat.None, SecondFormat.None, null, null, null, null)
		{
		}

		public _I_DateTimeFormatter (HourFormat hour, MinuteFormat minute, SecondFormat second)
			: this (YearFormat.None, MonthFormat.None, DayFormat.None, DayOfWeekFormat.None, hour, minute, second, null, null, null, null)
		{
		}

		public _I_DateTimeFormatter (YearFormat year, MonthFormat month, DayFormat day, DayOfWeekFormat dayOfWeek,
									HourFormat hour, MinuteFormat minute, SecondFormat second,
									IEnumerable<string> languages)
			: this (year, month, day, dayOfWeek, hour, minute, second, languages, null, null, null)
		{
		}

		public _I_DateTimeFormatter (YearFormat year, MonthFormat month, DayFormat day, DayOfWeekFormat dayOfWeek,
									HourFormat hour, MinuteFormat minute, SecondFormat second,
									IEnumerable<string> languages, string geographicRegion, string calendar, string clock)
		{
			_yearFormat = year;
			_monthFormat = month;
			_dayFormat = day;
			_dayOfWeekFormat = dayOfWeek;
			_hourFormat = hour;
			_minuteFormat = minute;
			_secondFormat = second;
			_languages = languages == null ? new List<string> () : new List<string> (languages);
			_geographicRegion = geographicRegion;
			if (!string.IsNullOrEmpty (calendar))
				_calendar = calendar;
			if (!string.IsNullOrEmpty (clock))
				_clock = clock;
			InitializeCulture ();
		}

		// ---------- 属性 ----------
		public string Calendar
		{
			get { return _calendar; }
			set
			{
				_calendar = value;
				// 日历更改可能需要重新初始化模式，这里简化处理
			}
		}

		public string Clock
		{
			get { return _clock; }
			set
			{
				_clock = value;
				InitializeCulture (); // 重新生成模式（因为小时格式可能变化）
			}
		}

		public string GeographicRegion
		{
			get { return _geographicRegion; }
			set
			{
				_geographicRegion = value;
				InitializeCulture ();
			}
		}

		public int IncludeDay
		{
			get { return (int)_dayFormat; }
		}

		public int IncludeDayOfWeek
		{
			get { return (int)_dayOfWeekFormat; }
		}

		public int IncludeHour
		{
			get { return (int)_hourFormat; }
		}

		public int IncludeMinute
		{
			get { return (int)_minuteFormat; }
		}

		public int IncludeMonth
		{
			get { return (int)_monthFormat; }
		}

		public int IncludeSecond
		{
			get { return (int)_secondFormat; }
		}

		public int IncludeYear
		{
			get { return (int)_yearFormat; }
		}

		public _I_List Languages
		{
			get { return new _I_List (_languages.AsReadOnly ().Select (e => (object)e), true); }
		}

		public static _I_DateTimeFormatter LongDate
		{
			get
			{
				return new _I_DateTimeFormatter ("longdate");
			}
		}

		public static _I_DateTimeFormatter LongTime
		{
			get
			{
				return new _I_DateTimeFormatter ("longtime");
			}
		}

		public string NumeralSystem
		{
			get { return _numeralSystem; }
			set { _numeralSystem = value; }
		}

		public _I_List Patterns
		{
			get
			{
				return new _I_List (new List<string> { _formatPattern }.AsReadOnly ().Select (e => (object)e), true);
			}
		}

		public string ResolvedGeographicRegion
		{
			get { return _resolvedGeographicRegion; }
		}

		public string ResolvedLanguage
		{
			get { return _resolvedLanguage; }
		}

		public static _I_DateTimeFormatter ShortDate
		{
			get
			{
				return new _I_DateTimeFormatter ("shortdate");
			}
		}

		public static _I_DateTimeFormatter ShortTime
		{
			get
			{
				return new _I_DateTimeFormatter ("shorttime");
			}
		}

		public string Template
		{
			get
			{
				if (!string.IsNullOrEmpty (_formatTemplate))
					return _formatTemplate;
				// 从枚举组合生成模板描述（简化）
				return _formatPattern;
			}
		}

		// ---------- 方法 ----------
		public string FormatC (DateTime dateTime)
		{
			return dateTime.ToString (_formatPattern, _culture);
		}

		public string FormatC (DateTime dateTime, string timeZoneId)
		{
			if (!string.IsNullOrEmpty (timeZoneId))
			{
				try
				{
					TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById (timeZoneId);
					DateTime targetTime = TimeZoneInfo.ConvertTime (dateTime, tzi);
					return targetTime.ToString (_formatPattern, _culture);
				}
				catch
				{
					// 时区无效，回退到原始时间
					return dateTime.ToString (_formatPattern, _culture);
				}
			}
			return dateTime.ToString (_formatPattern, _culture);
		}

		// 为方便 JS 调用，提供接受 object 的重载（自动识别 JS Date）
		public string Format (object jsDate)
		{
			DateTime dt = ConvertJsDateToDateTime (jsDate);
			return FormatC (dt);
		}

		public string FormatWithTimeZone (object jsDate, string timeZoneId)
		{
			DateTime dt = ConvertJsDateToDateTime (jsDate);
			return FormatC (dt, timeZoneId);
		}
	}
}
