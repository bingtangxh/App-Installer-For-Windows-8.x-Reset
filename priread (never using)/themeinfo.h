#pragma once
#include <Windows.h>

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
