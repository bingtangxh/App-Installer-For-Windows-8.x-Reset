#pragma once
#include <WinNls.h>
#include <string>
static std::wstring StringToWString (const std::string &str, UINT codePage = CP_ACP)
{
	if (str.empty ()) return std::wstring ();
	int len = MultiByteToWideChar (codePage, 0, str.c_str (), -1, nullptr, 0);
	if (len == 0) return std::wstring ();
	std::wstring wstr (len - 1, L'\0');
	MultiByteToWideChar (codePage, 0, str.c_str (), -1, &wstr [0], len);
	return wstr;
}

#undef GetLocaleInfo
std::string GetLocaleInfoA (LCID code, LCTYPE type)
{
	char buf [LOCALE_NAME_MAX_LENGTH] = {0};
	GetLocaleInfoA (code, type, buf, LOCALE_NAME_MAX_LENGTH);
	return buf;
}
std::wstring GetLocaleInfoW (LCID code, LCTYPE type)
{
	WCHAR buf [LOCALE_NAME_MAX_LENGTH] = {0};
	GetLocaleInfoW (code, type, buf, LOCALE_NAME_MAX_LENGTH);
	return buf;
}
void GetLocaleInfo (LCID code, LCTYPE type, std::wstring &output)
{
	output = GetLocaleInfoW (code, type);
}
void GetLocaleInfo (LCID code, LCTYPE type, std::string &output)
{
	output = GetLocaleInfoA (code, type);
}
int GetLocaleInfoEx (std::wstring lpLocaleName, LCTYPE type, std::wstring &output)
{
	WCHAR buf [LOCALE_NAME_MAX_LENGTH] = {0};
	int res = GetLocaleInfoEx (lpLocaleName.c_str (), type, buf, LOCALE_NAME_MAX_LENGTH);
	if (&output) output = std::wstring (buf);
	return res;
}

#undef SetLocaleInfo
BOOL SetLocaleInfoA (LCID code, LCTYPE type, const std::string &lcData)
{
	return SetLocaleInfoA (code, type, lcData.c_str ());
}
BOOL SetLocaleInfoW (LCID code, LCTYPE type, const std::wstring &lcData)
{
	return SetLocaleInfoW (code, type, lcData.c_str ());
}
BOOL SetLocaleInfo (LCID code, LCTYPE type, const std::wstring &lcData)
{
	return SetLocaleInfoW (code, type, lcData);
}
BOOL SetLocaleInfo (LCID code, LCTYPE type, const std::string &lcData)
{
	return SetLocaleInfoA (code, type, lcData);
}

std::string GetLocaleRestrictedCodeFromLcidA (LCID lcid)
{
	return GetLocaleInfoA (lcid, 89);
}
std::wstring GetLocaleRestrictedCodeFromLcidW (LCID lcid)
{
	return GetLocaleInfoW (lcid, 89);
}
void GetLocaleRestrictedCodeFromLcid (LCID lcid, std::string &ret)
{
	ret = GetLocaleRestrictedCodeFromLcidA (lcid);
}
void GetLocaleRestrictedCodeFromLcid (LCID lcid, std::wstring &ret)
{
	ret = GetLocaleRestrictedCodeFromLcidW (lcid);
}

std::string GetLocaleElaboratedCodeFromLcidA (LCID lcid)
{
	return GetLocaleInfoA (lcid, 90);
}
std::wstring GetLocaleElaboratedCodeFromLcidW (LCID lcid)
{
	return GetLocaleInfoW (lcid, 90);
}
void  GetLocaleElaboratedCodeFromLcid (LCID lcid, std::wstring &ret)
{
	ret = GetLocaleElaboratedCodeFromLcidW (lcid);
}
void  GetLocaleElaboratedCodeFromLcid (LCID lcid, std::string &ret)
{
	ret = GetLocaleElaboratedCodeFromLcidA (lcid);
}

LCID LocaleCodeToLcidW (LPCWSTR localeCode)
{
	BYTE buf [LOCALE_NAME_MAX_LENGTH * sizeof (WCHAR)] = {0};
	int res = GetLocaleInfoEx (localeCode, LOCALE_RETURN_NUMBER | LOCALE_ILANGUAGE, (LPWSTR)buf, LOCALE_NAME_MAX_LENGTH);
	LCID lcid = *((LCID *)buf);
	return lcid;
}
LCID LocaleCodeToLcidA (LPCSTR localeCode)
{
	std::wstring lcWide = StringToWString (std::string (localeCode));
	return LocaleCodeToLcidW (lcWide.c_str ());
}
LCID LocaleCodeToLcid (const std::wstring &loccode)
{
	return LocaleCodeToLcidW (loccode.c_str ());
}
LCID LocaleCodeToLcid (const std::string &loccode)
{
	return LocaleCodeToLcidA (loccode.c_str ());
}

std::string GetLocaleRestrictedCodeA (LPCSTR lc)
{
	return GetLocaleInfoA (LocaleCodeToLcidA (lc), 89);
}
std::string GetLocaleRestrictedCodeA (const std::string &lc)
{
	return GetLocaleInfoA (LocaleCodeToLcidA (lc.c_str ()), 89);
}
std::wstring GetLocaleRestrictedCodeW (LPCWSTR lc)
{
	return GetLocaleInfoW (LocaleCodeToLcidW (lc), 89);
}
std::wstring GetLocaleRestrictedCodeW (const std::wstring &lc)
{
	return GetLocaleInfoW (LocaleCodeToLcidW (lc.c_str ()), 89);
}
std::wstring GetLocaleRestrictedCode (const std::wstring &lc) { return GetLocaleRestrictedCodeW (lc); }
std::string GetLocaleRestrictedCode (const std::string &lc) { return GetLocaleRestrictedCodeA (lc); }

std::string GetLocaleElaboratedCodeA (LPCSTR lc)
{
	return GetLocaleInfoA (LocaleCodeToLcidA (lc), 90);
}
std::string GetLocaleElaboratedCodeA (const std::string &lc)
{
	return GetLocaleInfoA (LocaleCodeToLcidA (lc.c_str ()), 90);
}
std::wstring GetLocaleElaboratedCodeW (LPCWSTR lc)
{
	return GetLocaleInfoW (LocaleCodeToLcidW (lc), 90);
}
std::wstring GetLocaleElaboratedCodeW (const std::wstring &lc)
{
	return GetLocaleInfoW (LocaleCodeToLcidW (lc.c_str ()), 90);
}
std::wstring GetLocaleElaboratedCode (const std::wstring &lc) { return GetLocaleElaboratedCodeW (lc); }
std::string GetLocaleElaboratedCode (const std::string &lc) { return GetLocaleElaboratedCodeA (lc); }

std::string LcidToLocaleCodeA (LCID lcid, char divide = '-')
{
	return GetLocaleRestrictedCodeFromLcidA (lcid) + divide + GetLocaleElaboratedCodeFromLcidA (lcid);
}
std::wstring LcidToLocaleCodeW (LCID lcid, WCHAR divide = L'-')
{
	return GetLocaleRestrictedCodeFromLcidW (lcid) + divide + GetLocaleElaboratedCodeFromLcidW (lcid);
}
std::wstring LcidToLocaleCode (LCID lcid, WCHAR divide = L'-') { return LcidToLocaleCodeW (lcid, divide); }
std::string LcidToLocaleCode (LCID lcid, char divide = '-') { return LcidToLocaleCodeA (lcid, divide); }