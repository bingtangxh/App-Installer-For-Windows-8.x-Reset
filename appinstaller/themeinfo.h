#pragma once
#include <Windows.h>
#include <dwmapi.h>

bool IsHighContrastEnabled ()
{
	HIGHCONTRAST hc = {sizeof (HIGHCONTRAST)};
	if (SystemParametersInfo (SPI_GETHIGHCONTRAST, sizeof (hc), &hc, 0)) return (hc.dwFlags & HCF_HIGHCONTRASTON) != 0;
	return false;
}
enum class HighContrastTheme
{
	None,
	Black,
	White,
	Other
};
HighContrastTheme GetHighContrastTheme ()
{
	HIGHCONTRAST hc = {sizeof (HIGHCONTRAST)};
	if (!SystemParametersInfo (SPI_GETHIGHCONTRAST, sizeof (hc), &hc, 0)) return HighContrastTheme::None;
	if (!(hc.dwFlags & HCF_HIGHCONTRASTON)) return HighContrastTheme::None;
	COLORREF bgColor = GetSysColor (COLOR_WINDOW);
	COLORREF textColor = GetSysColor (COLOR_WINDOWTEXT);
	int brightnessBg = (GetRValue (bgColor) + GetGValue (bgColor) + GetBValue (bgColor)) / 3;
	int brightnessText = (GetRValue (textColor) + GetGValue (textColor) + GetBValue (textColor)) / 3;
	if (brightnessBg < brightnessText) return HighContrastTheme::Black;
	else if (brightnessBg > brightnessText) return HighContrastTheme::White;
	else return HighContrastTheme::Other;
}
int GetDPI ()
{
	HDC hDC = GetDC (NULL);
	int DPI_A = (int)(((double)GetDeviceCaps (hDC, 118) / (double)GetDeviceCaps (hDC, 8)) * 100);
	int DPI_B = (int)(((double)GetDeviceCaps (hDC, 88) / 96) * 100);
	ReleaseDC (NULL, hDC);
	if (DPI_A == 100) return DPI_B;
	else if (DPI_B == 100) return DPI_A;
	else if (DPI_A == DPI_B) return DPI_A;
	else return 0;
}
int GetScreenWidth () { return GetSystemMetrics (SM_CXSCREEN); }
int GetScreenHeight () { return GetSystemMetrics (SM_CYSCREEN); }
System::Drawing::Color GetDwmThemeColor ()
{
	DWORD color = 0;
	BOOL opaqueBlend = FALSE;
	// 调用 DwmGetColorizationColor 获取 Aero 颜色
	HRESULT hr = DwmGetColorizationColor (&color, &opaqueBlend);
	if (SUCCEEDED (hr)) 
	{
		BYTE r = (BYTE)((color & 0x00FF0000) >> 16);
		BYTE g = (BYTE)((color & 0x0000FF00) >> 8);
		BYTE b = (BYTE)(color & 0x000000FF);
		return System::Drawing::Color::FromArgb (r, g, b);
	}
	else return System::Drawing::Color::FromArgb (0, 120, 215); // 如果获取失败，返回默认颜色
}
System::String ^ColorToHtml (System::Drawing::Color color)
{
	return System::String::Format ("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
}
System::Drawing::Color StringToColor (System::String ^colorStr)
{
	using Color = System::Drawing::Color;
	using Regex = System::Text::RegularExpressions::Regex;
	auto Clamp = [] (int value, int min, int max)
	{
		return (value < min) ? min : (value > max) ? max : value;
	};
	System::String ^normalized = colorStr->Trim ()->ToLower ();
	if (normalized == "transparent") return Color::Transparent;
	if (Color::FromName (normalized).IsKnownColor)
	{
		return Color::FromName (normalized);
	}
	if (normalized->StartsWith ("#"))
	{
		System::String^ hex = normalized->Substring (1);
		if (hex->Length == 3 || hex->Length == 4)
		{
			hex = System::String::Concat (
				hex [0].ToString () + hex [0],
				hex [1].ToString () + hex [1],
				hex [2].ToString () + hex [2],
				(hex->Length == 4) ? (hex [3].ToString () + hex [3]) : ""
			);
		}
		uint32_t argb = System::Convert::ToUInt32 (hex, 16);
		switch (hex->Length)
		{
			case 6: return Color::FromArgb (
				0xFF,
				(argb >> 16) & 0xFF,
				(argb >> 8) & 0xFF,
				argb & 0xFF
			);
			case 8: return Color::FromArgb (
				(argb >> 24) & 0xFF,
				(argb >> 16) & 0xFF,
				(argb >> 8) & 0xFF,
				argb & 0xFF
			);
			default: throw gcnew System::ArgumentException ("Invalid hex color format");
		}
	}
	System::Text::RegularExpressions::Match ^match = Regex::Match (normalized,
		"^(rgba?)\\s*\\(\\s*(\\d+%?)\\s*,\\s*(\\d+%?)\\s*,\\s*(\\d+%?)\\s*,?\\s*([\\d.]+%?)?\\s*\\)$");
	if (match->Success)
	{
		auto GetComponent = [&] (System::String^ val) -> int
		{
			if (val->EndsWith ("%"))
			{
				float percent = float::Parse (val->TrimEnd ('%')) / 100.0f;
				return Clamp ((int)System::Math::Round (percent * 255), 0, 255);
			}
			return Clamp (int::Parse (val), 0, 255);
		};
		int r = GetComponent (match->Groups [2]->Value);
		int g = GetComponent (match->Groups [3]->Value);
		int b = GetComponent (match->Groups [4]->Value);
		if (match->Groups [1]->Value == "rgba")
		{
			System::String^ alphaVal = match->Groups [5]->Value;
			int a = 255;
			if (alphaVal->EndsWith ("%"))
			{
				float percent = float::Parse (alphaVal->TrimEnd ('%')) / 100.0f;
				a = Clamp ((int)System::Math::Round (percent * 255), 0, 255);
			}
			else if (!System::String::IsNullOrEmpty (alphaVal))
			{
				a = Clamp ((int)System::Math::Round (float::Parse (alphaVal) * 255), 0, 255);
			}
			return Color::FromArgb (a, r, g, b);
		}
		return Color::FromArgb (r, g, b);
	}
	//throw gcnew ArgumentException ("Unsupported color format: " + colorStr);
	return Color::Transparent;
}
bool IsAppInDarkMode ()
{
	HKEY hKey;
	DWORD dwValue;
	if (RegOpenKeyEx (HKEY_CURRENT_USER, L"Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", 0, KEY_READ, &hKey) == ERROR_SUCCESS)
	{
		DWORD dwSize = sizeof (dwValue);
		if (RegQueryValueEx (hKey, L"AppsUseLightTheme", NULL, NULL, (LPBYTE)&dwValue, &dwSize) == ERROR_SUCCESS)
		{
			RegCloseKey (hKey);
			return dwValue == 0;
		}
		RegCloseKey (hKey);
	}
	return false;
}