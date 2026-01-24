using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace DataUtils
{
	public enum HighContrastTheme
	{
		None,
		Black,
		White,
		Other
	}

	public static class UITheme
	{
		// --- P/Invoke & constants ---
		private const int SPI_GETHIGHCONTRAST = 0x0042;
		private const uint HCF_HIGHCONTRASTON = 0x00000001;

		private const int COLOR_WINDOW = 5;
		private const int COLOR_WINDOWTEXT = 8;

		private const int LOGPIXELSX = 88; // GetDeviceCaps index for DPI X
										   // private const int HORZRES = 8; // not used now

		private const int SM_CXSCREEN = 0;
		private const int SM_CYSCREEN = 1;

		[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct HIGHCONTRAST
		{
			public int cbSize;
			public uint dwFlags;
			public IntPtr lpszDefaultScheme;
		}

		[DllImport ("user32.dll", SetLastError = true)]
		private static extern bool SystemParametersInfo (int uiAction, int uiParam, ref HIGHCONTRAST pvParam, int fWinIni);

		[DllImport ("user32.dll")]
		private static extern int GetSysColor (int nIndex);

		[DllImport ("user32.dll")]
		private static extern int GetSystemMetrics (int nIndex);

		[DllImport ("user32.dll")]
		private static extern IntPtr GetDC (IntPtr hWnd);

		[DllImport ("user32.dll")]
		private static extern int ReleaseDC (IntPtr hWnd, IntPtr hDC);

		[DllImport ("gdi32.dll")]
		private static extern int GetDeviceCaps (IntPtr hdc, int nIndex);

		// DwmGetColorizationColor
		[DllImport ("dwmapi.dll", EntryPoint = "DwmGetColorizationColor", SetLastError = false)]
		private static extern int DwmGetColorizationColor (out uint pcrColorization, out bool pfOpaqueBlend);

		// --- Methods ---

		public static bool IsHighContrastEnabled ()
		{
			try
			{
				HIGHCONTRAST hc = new HIGHCONTRAST ();
				hc.cbSize = Marshal.SizeOf (typeof (HIGHCONTRAST));
				if (SystemParametersInfo (SPI_GETHIGHCONTRAST, hc.cbSize, ref hc, 0))
				{
					return (hc.dwFlags & HCF_HIGHCONTRASTON) != 0;
				}
			}
			catch
			{
				// ignore errors
			}
			return false;
		}
		public static bool IsHighContrast => IsHighContrastEnabled ();
		public static HighContrastTheme GetHighContrastTheme ()
		{
			try
			{
				HIGHCONTRAST hc = new HIGHCONTRAST ();
				hc.cbSize = Marshal.SizeOf (typeof (HIGHCONTRAST));
				if (!SystemParametersInfo (SPI_GETHIGHCONTRAST, hc.cbSize, ref hc, 0))
					return HighContrastTheme.None;

				if ((hc.dwFlags & HCF_HIGHCONTRASTON) == 0)
					return HighContrastTheme.None;

				int bgColorRef = GetSysColor (COLOR_WINDOW);
				int textColorRef = GetSysColor (COLOR_WINDOWTEXT);

				int bgR = (bgColorRef & 0x0000FF);
				int bgG = (bgColorRef & 0x00FF00) >> 8;
				int bgB = (bgColorRef & 0xFF0000) >> 16;

				int txtR = (textColorRef & 0x0000FF);
				int txtG = (textColorRef & 0x00FF00) >> 8;
				int txtB = (textColorRef & 0xFF0000) >> 16;

				int brightnessBg = (bgR + bgG + bgB) / 3;
				int brightnessText = (txtR + txtG + txtB) / 3;

				if (brightnessBg < brightnessText) return HighContrastTheme.Black;
				else if (brightnessBg > brightnessText) return HighContrastTheme.White;
				else return HighContrastTheme.Other;
			}
			catch
			{
				return HighContrastTheme.None;
			}
		}
		public static HighContrastTheme HighContrast => GetHighContrastTheme ();
		// Returns DPI as percent (100 = normal 96 DPI)
		public static int GetDPI ()
		{
			IntPtr hdc = IntPtr.Zero;
			try
			{
				hdc = GetDC (IntPtr.Zero);
				if (hdc == IntPtr.Zero) return 0;
				int dpiX = GetDeviceCaps (hdc, LOGPIXELSX);
				if (dpiX <= 0) return 0;
				// convert to percentage of 96 DPI baseline
				int percent = (int)Math.Round ((dpiX / 96.0) * 100.0);
				return percent;
			}
			catch
			{
				return 0;
			}
			finally
			{
				if (hdc != IntPtr.Zero) ReleaseDC (IntPtr.Zero, hdc);
			}
		}
		public static int DPI => GetDPI ();
		public static double DPIDouble => GetDPI () * 0.01;
		public static int GetScreenWidth ()
		{
			try { return GetSystemMetrics (SM_CXSCREEN); }
			catch { return 0; }
		}
		public static int ScreenWidth => GetScreenWidth ();
		public static int GetScreenHeight ()
		{
			try { return GetSystemMetrics (SM_CYSCREEN); }
			catch { return 0; }
		}
		public static int ScreenHeight => GetScreenHeight ();
		public static Color GetDwmThemeColor ()
		{
			try
			{
				uint color;
				bool opaque;
				int hr = DwmGetColorizationColor (out color, out opaque);
				if (hr == 0) // S_OK
				{
					byte r = (byte)((color & 0x00FF0000) >> 16);
					byte g = (byte)((color & 0x0000FF00) >> 8);
					byte b = (byte)(color & 0x000000FF);
					return Color.FromArgb (r, g, b);
				}
			}
			catch
			{
				// ignored
			}
			// fallback default (matches original C++ fallback)
			return Color.FromArgb (0, 120, 215);
		}
		public static Color ThemeColor => GetDwmThemeColor ();
		public static string ColorToHtml (Color color)
		{
			// Return #RRGGBB
			return string.Format ("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
		}

		public static Color StringToColor (string colorStr)
		{
			if (string.IsNullOrWhiteSpace (colorStr)) return Color.Transparent;
			string s = colorStr.Trim ();

			// Hex: #RGB, #RRGGBB, #AARRGGBB
			if (s.StartsWith ("#"))
			{
				string hex = s.Substring (1);
				// Expand short forms (#RGB or #RGBA)
				if (hex.Length == 3 || hex.Length == 4)
				{
					string expanded = "";
					for (int i = 0; i < hex.Length; i++)
					{
						expanded += hex [i].ToString () + hex [i].ToString ();
					}
					hex = expanded;
				}

				uint argb;
				try
				{
					argb = Convert.ToUInt32 (hex, 16);
				}
				catch
				{
					return Color.Transparent;
				}

				if (hex.Length == 6)
				{
					int r = (int)((argb >> 16) & 0xFF);
					int g = (int)((argb >> 8) & 0xFF);
					int b = (int)(argb & 0xFF);
					return Color.FromArgb (r, g, b);
				}
				else if (hex.Length == 8)
				{
					int a = (int)((argb >> 24) & 0xFF);
					int r = (int)((argb >> 16) & 0xFF);
					int g = (int)((argb >> 8) & 0xFF);
					int b = (int)(argb & 0xFF);
					return Color.FromArgb (a, r, g, b);
				}
				else
				{
					return Color.Transparent;
				}
			}

			// rgb()/rgba() functional notation
			// Accept forms like: rgb(255,0,0) or rgba(255,0,0,0.5) or rgb(100%,0%,0%)
			var m = Regex.Match (s, @"^(rgba?)\s*\(\s*([^\)]+)\s*\)$", RegexOptions.IgnoreCase);
			if (m.Success)
			{
				string func = m.Groups [1].Value.ToLowerInvariant ();
				string inside = m.Groups [2].Value;
				string [] parts = inside.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length >= 3)
				{
					try
					{
						Func<string, int> comp = (string v) => {
							v = v.Trim ();
							if (v.EndsWith ("%"))
							{
								// percentage
								string num = v.TrimEnd ('%');
								float p;
								if (float.TryParse (num, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out p))
								{
									return (int)Math.Round (Math.Max (0, Math.Min (100, p)) * 255.0 / 100.0);
								}
								return 0;
							}
							else
							{
								int iv;
								if (int.TryParse (v, out iv))
								{
									return Math.Max (0, Math.Min (255, iv));
								}
								// fallback parse float
								float fv;
								if (float.TryParse (v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fv))
								{
									return Math.Max (0, Math.Min (255, (int)Math.Round (fv)));
								}
								return 0;
							}
						};

						int r = comp (parts [0]);
						int g = comp (parts [1]);
						int b = comp (parts [2]);
						int a = 255;
						if (func == "rgba" && parts.Length >= 4)
						{
							string av = parts [3].Trim ();
							if (av.EndsWith ("%"))
							{
								string num = av.TrimEnd ('%');
								float p;
								if (float.TryParse (num, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out p))
								{
									a = (int)Math.Round (Math.Max (0, Math.Min (100, p)) * 255.0 / 100.0);
								}
							}
							else
							{
								// alpha may be 0..1 or 0..255
								float af;
								if (float.TryParse (av, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out af))
								{
									if (af <= 1.0f) a = (int)Math.Round (Math.Max (0.0f, Math.Min (1.0f, af)) * 255.0f);
									else a = (int)Math.Round (Math.Max (0.0f, Math.Min (255.0f, af)));
								}
							}
						}
						return Color.FromArgb (a, r, g, b);
					}
					catch
					{
						return Color.Transparent;
					}
				}
			}

			// Named color
			try
			{
				Color byName = Color.FromName (s);
				if (byName.IsKnownColor || byName.IsNamedColor)
				{
					return byName;
				}
			}
			catch { /* ignore */ }

			// fallback: try parse as known color again (case-insensitive)
			try
			{
				Color named = Color.FromName (s);
				if (named.IsKnownColor || named.IsNamedColor) return named;
			}
			catch { }

			return Color.Transparent;
		}

		public static bool IsAppInDarkMode ()
		{
			try
			{
				// HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme
				object val = Registry.GetValue (
					@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
					"AppsUseLightTheme",
					null);

				if (val == null) return false; // default to light? original returned false
				int intVal;
				if (val is int) intVal = (int)val;
				else
				{
					if (!int.TryParse (val.ToString (), out intVal)) return false;
				}
				// 0 => dark, 1 => light
				return intVal == 0;
			}
			catch
			{
				return false;
			}
		}
		public static bool AppDarkMode => IsAppInDarkMode ();
	}
}
