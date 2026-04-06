using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Linq;
using System.Reflection;

namespace DataUtils
{
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Calendar
	{
		private DateTime _utcDateTime;           // 存储 UTC 时间（所有算术的基础）
		private Calendar _calendar;               // 当前日历系统
		private TimeZoneInfo _timeZone;           // 当前时区
		private bool _is24HourClock;              // true = 24小时制，false = 12小时制
		private List<string> _languages;          // 语言优先级列表
		private string _numeralSystem = "Latn";   // 数字系统（简化实现，仅存储）
		private const long TicksPerNanosecond = 100;  // 1 tick = 100 ns

		// 缓存的本地时间（根据时区从 _utcDateTime 转换得到）
		private DateTime _localDateTime;

		// 更新本地时间（当 UTC 时间或时区改变时调用）
		private void UpdateLocalDateTime ()
		{
			_localDateTime = TimeZoneInfo.ConvertTimeFromUtc (_utcDateTime, _timeZone);
		}

		// 当本地时间被修改后，同步回 UTC 时间
		private void UpdateUtcDateTime ()
		{
			_utcDateTime = TimeZoneInfo.ConvertTimeToUtc (_localDateTime, _timeZone);
		}

		// 辅助：日历字段的读取
		private int GetCalendarField (Func<Calendar, DateTime, int> fieldGetter)
		{
			return fieldGetter (_calendar, _localDateTime);
		}

		// 辅助：日历字段的设置（返回新的本地时间）
		private DateTime SetCalendarField (DateTime currentLocal, Func<Calendar, DateTime, int, DateTime> fieldSetter, int value)
		{
			return fieldSetter (_calendar, currentLocal, value);
		}

		// 根据日历标识符创建日历实例
		private static Calendar CreateCalendar (string calendarId)
		{
			switch (calendarId)
			{
				case "GregorianCalendar":
					return new GregorianCalendar ();
				case "HebrewCalendar":
					return new HebrewCalendar ();
				case "HijriCalendar":
					return new HijriCalendar ();
				case "JapaneseCalendar":
					return new JapaneseCalendar ();
				case "KoreanCalendar":
					return new KoreanCalendar ();
				case "TaiwanCalendar":
					return new TaiwanCalendar ();
				case "ThaiBuddhistCalendar":
					return new ThaiBuddhistCalendar ();
				case "UmAlQuraCalendar":
					return new UmAlQuraCalendar ();
				// 可根据需要增加更多日历
				default:
					return new GregorianCalendar ();
			}
		}

		private _I_Language GetFormatCulture ()
		{
			if (_languages == null || _languages.Count == 0)
				return new _I_Language (CultureInfo.CurrentCulture.Name);
			try
			{
				return new _I_Language (_languages [0]);
			}
			catch
			{
				return new _I_Language (CultureInfo.CurrentCulture.Name);
			}
		}

		private List<object> JsArrayToList (object jsArray)
		{
			var result = new List<object> ();

			if (jsArray == null)
				return result;

			// JS Array 有 length 属性和数字索引器
			var type = jsArray.GetType ();

			int length = (int)type.InvokeMember (
				"length",
				BindingFlags.GetProperty,
				null,
				jsArray,
				null);

			for (int i = 0; i < length; i++)
			{
				object value = type.InvokeMember (
					i.ToString (),
					BindingFlags.GetProperty,
					null,
					jsArray,
					null);

				result.Add (value);
			}

			return result;
		}

		public _I_Calendar ()
			: this (new List<string> { CultureInfo.CurrentCulture.Name },
				   "GregorianCalendar", "24HourClock", TimeZoneInfo.Local.Id)
		{
		}

		public _I_Calendar (object languages)
			: this (languages, "GregorianCalendar", "24HourClock", TimeZoneInfo.Local.Id)
		{
		}

		public _I_Calendar (object languages, string calendar, string clock)
			: this (languages, calendar, clock, TimeZoneInfo.Local.Id)
		{
		}

		public _I_Calendar (object languages, string calendar, string clock, string timeZoneId)
		{
			_languages = languages == null ? new List<string> () : new List<string> (JsArrayToList (languages).Select (e => e as string));
			if (_languages.Count == 0)
				_languages.Add (CultureInfo.CurrentCulture.Name);

			_calendar = CreateCalendar (calendar);
			_is24HourClock = (clock == "24HourClock");
			_timeZone = TimeZoneInfo.FindSystemTimeZoneById (timeZoneId);
			_utcDateTime = DateTime.UtcNow;
			UpdateLocalDateTime ();
		}

		public int Day
		{
			get
			{
				return _calendar.GetDayOfMonth (_localDateTime);
			}
			set
			{
				int currentDay = _calendar.GetDayOfMonth (_localDateTime);
				if (value == currentDay) return;
				// 通过加减天数来设置日期
				DateTime newLocal = _calendar.AddDays (_localDateTime, value - currentDay);
				_localDateTime = newLocal;
				UpdateUtcDateTime ();
			}
		}

		public int DayOfWeek
		{
			get
			{
				// 返回 0-6，0 表示星期日
				return (int)_calendar.GetDayOfWeek (_localDateTime);
			}
		}

		public int Era
		{
			get
			{
				return _calendar.GetEra (_localDateTime);
			}
			set
			{
				// 更改纪元较复杂，简化：仅当值不同时不做任何操作（可根据需求实现）
				// 这里忽略设置，或者可以抛出 NotSupportedException
				if (value != Era)
					throw new NotSupportedException ("Setting Era directly is not supported in this implementation.");
			}
		}

		public int FirstDayInThisMonth
		{
			get { return 1; }
		}

		public int FirstEra
		{
			get { return _calendar.GetEra (_calendar.MinSupportedDateTime); }
		}

		public int FirstHourInThisPeriod
		{
			get
			{
				if (_is24HourClock)
					return 0;
				else
					return (Period == 0) ? 1 : 13;
			}
		}

		public int FirstMinuteInThisHour
		{
			get { return 0; }
		}

		public int FirstMonthInThisYear
		{
			get { return 1; }
		}

		public int FirstPeriodInThisDay
		{
			get { return 0; } // 0 = AM
		}

		public int FirstSecondInThisMinute
		{
			get { return 0; }
		}

		public int FirstYearInThisEra
		{
			get
			{
				DateTime eraStart = _calendar.MinSupportedDateTime;
				int targetEra = Era;
				while (_calendar.GetEra (eraStart) != targetEra && eraStart < DateTime.MaxValue)
				{
					eraStart = _calendar.AddYears (eraStart, 1);
				}
				return _calendar.GetYear (eraStart);
			}
		}

		public int Hour
		{
			get
			{
				int hour24 = _calendar.GetHour (_localDateTime);
				if (_is24HourClock)
					return hour24;
				int hour12 = hour24 % 12;
				return (hour12 == 0) ? 12 : hour12;
			}
			set
			{
				if (_is24HourClock)
				{
					if (value < 0 || value > 23)
						throw new ArgumentOutOfRangeException (nameof (value), "Hour must be between 0 and 23 for 24-hour clock.");
					DateTime newLocal = _localDateTime.Date.AddHours (value).AddMinutes (Minute).AddSeconds (Second);
					_localDateTime = newLocal;
				}
				else
				{
					if (value < 1 || value > 12)
						throw new ArgumentOutOfRangeException (nameof (value), "Hour must be between 1 and 12 for 12-hour clock.");
					int hour24 = value % 12;
					if (Period == 1) hour24 += 12;
					DateTime newLocal = _localDateTime.Date.AddHours (hour24).AddMinutes (Minute).AddSeconds (Second);
					_localDateTime = newLocal;
				}
				UpdateUtcDateTime ();
			}
		}

		public bool IsDaylightSavingTime
		{
			get { return _timeZone.IsDaylightSavingTime (_localDateTime); }
		}

		public _I_List Languages
		{
			get { return new _I_List (_languages.AsReadOnly ().Select (e => (object)e), true) ; }
		}

		public int LastDayInThisMonth
		{
			get { return _calendar.GetDaysInMonth (_calendar.GetYear (_localDateTime), _calendar.GetMonth (_localDateTime)); }
		}

		public int LastEra
		{
			get { return _calendar.GetEra (_calendar.MaxSupportedDateTime); }
		}

		public int LastHourInThisPeriod
		{
			get
			{
				if (_is24HourClock)
					return 23;
				else
					return (Period == 0) ? 11 : 23;
			}
		}

		public int LastMinuteInThisHour
		{
			get { return 59; }
		}

		public int LastMonthInThisYear
		{
			get { return _calendar.GetMonthsInYear (_calendar.GetYear (_localDateTime)); }
		}

		public int LastPeriodInThisDay
		{
			get { return 1; } // 1 = PM
		}

		public int LastSecondInThisMinute
		{
			get { return 59; }
		}

		public int LastYearInThisEra
		{
			get
			{
				DateTime eraEnd = _calendar.MaxSupportedDateTime;
				int targetEra = Era;
				while (_calendar.GetEra (eraEnd) != targetEra && eraEnd > DateTime.MinValue)
				{
					eraEnd = _calendar.AddYears (eraEnd, -1);
				}
				return _calendar.GetYear (eraEnd);
			}
		}

		public int Minute
		{
			get { return _calendar.GetMinute (_localDateTime); }
			set
			{
				if (value < 0 || value > 59)
					throw new ArgumentOutOfRangeException (nameof (value), "Minute must be between 0 and 59.");
				DateTime newLocal = _localDateTime.AddMinutes (value - _calendar.GetMinute (_localDateTime));
				_localDateTime = newLocal;
				UpdateUtcDateTime ();
			}
		}

		public int Month
		{
			get { return _calendar.GetMonth (_localDateTime); }
			set
			{
				if (value < 1 || value > _calendar.GetMonthsInYear (_calendar.GetYear (_localDateTime)))
					throw new ArgumentOutOfRangeException (nameof (value), "Month is out of range for current year.");
				DateTime newLocal = SetCalendarField (_localDateTime, (cal, dt, val) => cal.AddMonths (dt, val - cal.GetMonth (dt)), value);
				_localDateTime = newLocal;
				UpdateUtcDateTime ();
			}
		}

		public int Nanosecond
		{
			get
			{
				long ticksInSecond = _localDateTime.Ticks % TimeSpan.TicksPerSecond;
				return (int)(ticksInSecond * 100); // 1 tick = 100 ns
			}
			set
			{
				if (value < 0 || value > 999999999)
					throw new ArgumentOutOfRangeException (nameof (value), "Nanosecond must be between 0 and 999,999,999.");
				long ticks = (_localDateTime.Ticks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond + (value / 100);
				_localDateTime = new DateTime (ticks, _localDateTime.Kind);
				UpdateUtcDateTime ();
			}
		}

		public int NumberOfDaysInThisMonth
		{
			get { return LastDayInThisMonth; }
		}

		public int NumberOfEras
		{
			get { return _calendar.Eras.Length; }
		}

		public int NumberOfHoursInThisPeriod
		{
			get { return _is24HourClock ? 24 : 12; }
		}

		public int NumberOfMinutesInThisHour
		{
			get { return 60; }
		}

		public int NumberOfMonthsInThisYear
		{
			get { return _calendar.GetMonthsInYear (_calendar.GetYear (_localDateTime)); }
		}

		public int NumberOfPeriodsInThisDay
		{
			get { return 2; }
		}

		public int NumberOfSecondsInThisMinute
		{
			get { return 60; }
		}

		public int NumberOfYearsInThisEra
		{
			get { return LastYearInThisEra - FirstYearInThisEra + 1; }
		}

		public string NumeralSystem
		{
			get { return _numeralSystem; }
			set { _numeralSystem = value; }   // 简化：未实现实际数字转换
		}

		public int Period
		{
			get
			{
				int hour24 = _calendar.GetHour (_localDateTime);
				return (hour24 >= 12) ? 1 : 0;
			}
			set
			{
				if (value != 0 && value != 1)
					throw new ArgumentOutOfRangeException (nameof (value), "Period must be 0 (AM) or 1 (PM).");
				int currentPeriod = Period;
				if (currentPeriod == value) return;
				// 切换 AM/PM：加减 12 小时
				DateTime newLocal = _localDateTime.AddHours (value == 0 ? -12 : 12);
				_localDateTime = newLocal;
				UpdateUtcDateTime ();
			}
		}

		public string ResolvedLanguage
		{
			get
			{
				if (_languages != null && _languages.Count > 0)
					return _languages [0];
				return CultureInfo.CurrentCulture.Name;
			}
		}

		public int Second
		{
			get { return _calendar.GetSecond (_localDateTime); }
			set
			{
				if (value < 0 || value > 59)
					throw new ArgumentOutOfRangeException (nameof (value), "Second must be between 0 and 59.");
				DateTime newLocal = _localDateTime.AddSeconds (value - _calendar.GetSecond (_localDateTime));
				_localDateTime = newLocal;
				UpdateUtcDateTime ();
			}
		}

		public int Year
		{
			get { return _calendar.GetYear (_localDateTime); }
			set
			{
				if (value < _calendar.GetYear (_calendar.MinSupportedDateTime) || value > _calendar.GetYear (_calendar.MaxSupportedDateTime))
					throw new ArgumentOutOfRangeException (nameof (value), "Year is out of range for this calendar.");
				DateTime newLocal = SetCalendarField (_localDateTime, (cal, dt, val) => cal.AddYears (dt, val - cal.GetYear (dt)), value);
				_localDateTime = newLocal;
				UpdateUtcDateTime ();
			}
		}

		public void AddDays (int days)
		{
			_localDateTime = _calendar.AddDays (_localDateTime, days);
			UpdateUtcDateTime ();
		}

		public void AddEras (int eras)
		{
			// 简化：每个纪元按 1000 年估算（实际应基于日历的纪元范围）
			// 大多数情况下不应频繁使用此方法
			_localDateTime = _calendar.AddYears (_localDateTime, eras * 1000);
			UpdateUtcDateTime ();
		}

		public void AddHours (int hours)
		{
			_localDateTime = _localDateTime.AddHours (hours);
			UpdateUtcDateTime ();
		}

		public void AddMinutes (int minutes)
		{
			_localDateTime = _localDateTime.AddMinutes (minutes);
			UpdateUtcDateTime ();
		}

		public void AddMonths (int months)
		{
			_localDateTime = _calendar.AddMonths (_localDateTime, months);
			UpdateUtcDateTime ();
		}

		public void AddNanoseconds (int nanoseconds)
		{
			long ticksToAdd = nanoseconds / 100;
			_localDateTime = _localDateTime.AddTicks (ticksToAdd);
			UpdateUtcDateTime ();
		}

		public void AddPeriods (int periods)
		{
			_localDateTime = _localDateTime.AddHours (periods * 12);
			UpdateUtcDateTime ();
		}

		public void AddSeconds (int seconds)
		{
			_localDateTime = _localDateTime.AddSeconds (seconds);
			UpdateUtcDateTime ();
		}

		public void AddWeeks (int weeks)
		{
			AddDays (weeks * 7);
		}

		public void AddYears (int years)
		{
			_localDateTime = _calendar.AddYears (_localDateTime, years);
			UpdateUtcDateTime ();
		}

		public void ChangeCalendarSystem (string calendarId)
		{
			Calendar newCalendar = CreateCalendar (calendarId);
			// 注意：日历改变不会改变绝对时间，但会影响组件（年、月、日等）
			// 只需替换日历实例，_localDateTime 保持不变（但解释方式变了）
			_calendar = newCalendar;
		}

		public void ChangeClock (string clock)
		{
			_is24HourClock = (clock == "24HourClock");
		}

		public void ChangeTimeZone (string timeZoneId)
		{
			_timeZone = TimeZoneInfo.FindSystemTimeZoneById (timeZoneId);
			UpdateLocalDateTime ();   // 重新计算本地时间（UTC 不变）
		}

		public _I_Calendar Clone ()
		{
			var clone = new _I_Calendar (_languages, GetCalendarSystem (), GetClock (), GetTimeZone ());
			clone.SetDateTime (GetDateTime ());
			return clone;
		}

		public int Compare (_I_Calendar other)
		{
			if (other == null) return 1;
			return DateTime.Compare (this.GetDateTime (), other.GetDateTime ());
		}

		public static DateTime JsDateToDateTime (object jsDate)
		{
			if (jsDate == null)
				throw new ArgumentNullException (nameof (jsDate));
			Type type = jsDate.GetType ();
			double milliseconds = (double)type.InvokeMember (
				"getTime",
				BindingFlags.InvokeMethod,
				null,
				jsDate,
				null);
			DateTimeOffset epoch = new DateTimeOffset (1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
			DateTimeOffset dateTimeOffset = epoch.AddMilliseconds (milliseconds);
			return dateTimeOffset.UtcDateTime;
		}
		public int CompareDateTime (object other)
		{
			return DateTime.Compare (this.GetDateTime (), JsDateToDateTime (other));
		}

		public void CopyTo (_I_Calendar other)
		{
			if (other == null) throw new ArgumentNullException (nameof (other));
			other._utcDateTime = this._utcDateTime;
			other._localDateTime = this._localDateTime;
			other._calendar = this._calendar;
			other._timeZone = this._timeZone;
			other._is24HourClock = this._is24HourClock;
			other._languages = new List<string> (this._languages);
			other._numeralSystem = this._numeralSystem;
		}

		public string DayAsPaddedString (int minDigits)
		{
			return Day.ToString ().PadLeft (minDigits, '0');
		}

		public string DayAsString ()
		{
			return Day.ToString ();
		}

		public string DayOfWeekAsSoloString ()
		{
			return DayOfWeekAsString ();
		}

		public string DayOfWeekAsSoloString (int idealLength)
		{
			// 简化：忽略 idealLength
			return DayOfWeekAsString ();
		}

		public string DayOfWeekAsString ()
		{
			return _localDateTime.ToString ("dddd", GetFormatCulture ());
		}

		public string DayOfWeekAsString (int idealLength)
		{
			return DayOfWeekAsString ();
		}

		public string EraAsString ()
		{
			// 简化：返回纪元索引的字符串
			return Era.ToString ();
		}

		public string EraAsString (int idealLength)
		{
			return EraAsString ();
		}

		public string GetCalendarSystem ()
		{
			if (_calendar is GregorianCalendar) return "GregorianCalendar";
			if (_calendar is HebrewCalendar) return "HebrewCalendar";
			if (_calendar is HijriCalendar) return "HijriCalendar";
			if (_calendar is JapaneseCalendar) return "JapaneseCalendar";
			if (_calendar is KoreanCalendar) return "KoreanCalendar";
			if (_calendar is TaiwanCalendar) return "TaiwanCalendar";
			if (_calendar is ThaiBuddhistCalendar) return "ThaiBuddhistCalendar";
			if (_calendar is UmAlQuraCalendar) return "UmAlQuraCalendar";
			return "GregorianCalendar";
		}

		public string GetClock ()
		{
			return _is24HourClock ? "24HourClock" : "12HourClock";
		}

		public DateTime GetDateTime ()
		{
			// 返回 UTC 时间
			return _utcDateTime;
		}

		public string GetTimeZone ()
		{
			return _timeZone.Id;
		}

		public string HourAsPaddedString (int minDigits)
		{
			return Hour.ToString ().PadLeft (minDigits, '0');
		}

		public string HourAsString ()
		{
			return Hour.ToString ();
		}

		public string MinuteAsPaddedString (int minDigits)
		{
			return Minute.ToString ().PadLeft (minDigits, '0');
		}

		public string MinuteAsString ()
		{
			return Minute.ToString ();
		}

		public string MonthAsNumericString ()
		{
			return Month.ToString ();
		}

		public string MonthAsPaddedNumericString (int minDigits)
		{
			return Month.ToString ().PadLeft (minDigits, '0');
		}

		public string MonthAsSoloString ()
		{
			return _localDateTime.ToString ("MMMM", GetFormatCulture ());
		}

		public string MonthAsSoloString (int idealLength)
		{
			return MonthAsSoloString ();
		}

		public string MonthAsString ()
		{
			return _localDateTime.ToString ("MMM", GetFormatCulture ());
		}

		public string MonthAsString (int idealLength)
		{
			return MonthAsString ();
		}

		public string NanosecondAsPaddedString (int minDigits)
		{
			return Nanosecond.ToString ().PadLeft (minDigits, '0');
		}

		public string NanosecondAsString ()
		{
			return Nanosecond.ToString ();
		}

		public string PeriodAsString ()
		{
			return PeriodAsString (0);
		}

		public string PeriodAsString (int idealLength)
		{
			return _localDateTime.ToString ("tt", GetFormatCulture ());
		}

		public string SecondAsPaddedString (int minDigits)
		{
			return Second.ToString ().PadLeft (minDigits, '0');
		}

		public string SecondAsString ()
		{
			return Second.ToString ();
		}

		public void SetDateTime (DateTime value)
		{
			_utcDateTime = value.ToUniversalTime ();
			UpdateLocalDateTime ();
		}

		public void SetToMax ()
		{
			SetDateTime (DateTime.MaxValue);
		}

		public void SetToMin ()
		{
			SetDateTime (DateTime.MinValue);
		}

		public void SetToNow ()
		{
			SetDateTime (DateTime.UtcNow);
		}

		public string TimeZoneAsString ()
		{
			return TimeZoneAsString (0);
		}

		public string TimeZoneAsString (int idealLength)
		{
			// 简化：返回标准显示名称
			return _timeZone.DisplayName;
		}

		public string YearAsPaddedString (int minDigits)
		{
			return Year.ToString ().PadLeft (minDigits, '0');
		}

		public string YearAsString ()
		{
			return Year.ToString ();
		}

		public string YearAsTruncatedString (int remainingDigits)
		{
			string yearStr = Year.ToString ();
			if (yearStr.Length <= remainingDigits)
				return yearStr;
			return yearStr.Substring (yearStr.Length - remainingDigits);
		}
	}
}