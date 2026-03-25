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
/// <summary>
/// 获取指定 LCID 的区域设置信息（ANSI 版本）。
/// </summary>
/// <param name="code">区域标识符（LCID）。</param>
/// <param name="type">要获取的信息类型（LCTYPE）。</param>
/// <returns>以 ANSI 字符串形式返回的区域设置信息。</returns>
std::string GetLocaleInfoA (LCID code, LCTYPE type)
{
	char buf [LOCALE_NAME_MAX_LENGTH] = {0};
	GetLocaleInfoA (code, type, buf, LOCALE_NAME_MAX_LENGTH);
	return buf;
}
/// <summary>
/// 获取指定 LCID 的区域设置信息（Unicode 版本）。
/// </summary>
/// <param name="code">区域标识符（LCID）。</param>
/// <param name="type">要获取的信息类型（LCTYPE）。</param>
/// <returns>以宽字符串形式返回的区域设置信息。</returns>
std::wstring GetLocaleInfoW (LCID code, LCTYPE type)
{
	WCHAR buf [LOCALE_NAME_MAX_LENGTH] = {0};
	GetLocaleInfoW (code, type, buf, LOCALE_NAME_MAX_LENGTH);
	return buf;
}
/// <summary>
/// 获取指定 LCID 的区域设置信息（宽字符串输出）。
/// </summary>
/// <param name="code">区域标识符（LCID）。</param>
/// <param name="type">要获取的信息类型（LCTYPE）。</param>
/// <param name="output">接收信息的宽字符串引用。</param>
void GetLocaleInfo (LCID code, LCTYPE type, std::wstring &output)
{
	output = GetLocaleInfoW (code, type);
}
/// <summary>
/// 获取指定 LCID 的区域设置信息（ANSI 字符串输出）。
/// </summary>
/// <param name="code">区域标识符（LCID）。</param>
/// <param name="type">要获取的信息类型（LCTYPE）。</param>
/// <param name="output">接收信息的 ANSI 字符串引用。</param>
void GetLocaleInfo (LCID code, LCTYPE type, std::string &output)
{
	output = GetLocaleInfoA (code, type);
}
/// <summary>
/// 获取指定区域名称的区域设置信息（扩展版本，支持 Windows Vista+）。
/// </summary>
/// <param name="lpLocaleName">区域名称（如 "en-US"）。</param>
/// <param name="type">要获取的信息类型（LCTYPE）。</param>
/// <param name="output">接收信息的宽字符串引用。</param>
/// <returns>成功返回非零值，失败返回 0；如果 lpLocaleName 无效，则函数会返回 0。</returns>
int GetLocaleInfoEx (std::wstring lpLocaleName, LCTYPE type, std::wstring &output)
{
	WCHAR buf [LOCALE_NAME_MAX_LENGTH] = {0};
	int res = GetLocaleInfoEx (lpLocaleName.c_str (), type, buf, LOCALE_NAME_MAX_LENGTH);
	if (&output) output = std::wstring (buf);
	return res;
}

#undef SetLocaleInfo
/// <summary>
/// 设置指定 LCID 的区域设置信息（ANSI 版本）。
/// </summary>
/// <param name="code">区域标识符（LCID）。</param>
/// <param name="type">要设置的信息类型（LCTYPE）。</param>
/// <param name="lcData">要设置的数据（ANSI 字符串）。</param>
/// <returns>成功返回非零值，失败返回 0。</returns>
BOOL SetLocaleInfoA (LCID code, LCTYPE type, const std::string &lcData)
{
	return SetLocaleInfoA (code, type, lcData.c_str ());
}
/// <summary>
/// 设置指定 LCID 的区域设置信息（Unicode 版本）。
/// </summary>
/// <param name="code">区域标识符（LCID）。</param>
/// <param name="type">要设置的信息类型（LCTYPE）。</param>
/// <param name="lcData">要设置的数据（宽字符串）。</param>
/// <returns>成功返回非零值，失败返回 0。</returns>
BOOL SetLocaleInfoW (LCID code, LCTYPE type, const std::wstring &lcData)
{
	return SetLocaleInfoW (code, type, lcData.c_str ());
}
/// <summary>
/// 设置指定 LCID 的区域设置信息（宽字符串重载）。
/// </summary>
/// <param name="code">区域标识符（LCID）。</param>
/// <param name="type">要设置的信息类型（LCTYPE）。</param>
/// <param name="lcData">要设置的数据（宽字符串）。</param>
/// <returns>成功返回非零值，失败返回 0。</returns>
BOOL SetLocaleInfo (LCID code, LCTYPE type, const std::wstring &lcData)
{
	return SetLocaleInfoW (code, type, lcData);
}
/// <summary>
/// 设置指定 LCID 的区域设置信息（ANSI 字符串重载）。
/// </summary>
/// <param name="code">区域标识符（LCID）。</param>
/// <param name="type">要设置的信息类型（LCTYPE）。</param>
/// <param name="lcData">要设置的数据（ANSI 字符串）。</param>
/// <returns>成功返回非零值，失败返回 0。</returns>
BOOL SetLocaleInfo (LCID code, LCTYPE type, const std::string &lcData)
{
	return SetLocaleInfoA (code, type, lcData);
}

/// <summary>
/// 从 LCID 获取受限区域代码（如 "en"），ANSI 版本。
/// </summary>
/// <param name="lcid">区域标识符。</param>
/// <returns>受限区域代码的 ANSI 字符串。</returns>
std::string GetLocaleRestrictedCodeFromLcidA (LCID lcid)
{
	return GetLocaleInfoA (lcid, 89);
}
/// <summary>
/// 从 LCID 获取受限区域代码（如 "en"），Unicode 版本。
/// </summary>
/// <param name="lcid">区域标识符。</param>
/// <returns>受限区域代码的宽字符串。</returns>
std::wstring GetLocaleRestrictedCodeFromLcidW (LCID lcid)
{
	return GetLocaleInfoW (lcid, 89);
}
/// <summary>
/// 从 LCID 获取受限区域代码（ANSI 输出）。
/// </summary>
/// <param name="lcid">区域标识符。</param>
/// <param name="ret">输出 ANSI 字符串。</param>
void GetLocaleRestrictedCodeFromLcid (LCID lcid, std::string &ret)
{
	ret = GetLocaleRestrictedCodeFromLcidA (lcid);
}
/// <summary>
/// 从 LCID 获取受限区域代码（宽字符串输出）。
/// </summary>
/// <param name="lcid">区域标识符。</param>
/// <param name="ret">输出宽字符串。</param>
void GetLocaleRestrictedCodeFromLcid (LCID lcid, std::wstring &ret)
{
	ret = GetLocaleRestrictedCodeFromLcidW (lcid);
}

/// <summary>
/// 从 LCID 获取详细区域代码（如 "US"），ANSI 版本。
/// </summary>
/// <param name="lcid">区域标识符。</param>
/// <returns>详细区域代码的 ANSI 字符串。</returns>
std::string GetLocaleElaboratedCodeFromLcidA (LCID lcid)
{
	return GetLocaleInfoA (lcid, 90);
}
/// <summary>
/// 从 LCID 获取详细区域代码（如 "US"），Unicode 版本。
/// </summary>
/// <param name="lcid">区域标识符。</param>
/// <returns>详细区域代码的宽字符串。</returns>
std::wstring GetLocaleElaboratedCodeFromLcidW (LCID lcid)
{
	return GetLocaleInfoW (lcid, 90);
}
/// <summary>
/// 从 LCID 获取详细区域代码（宽字符串输出）。
/// </summary>
/// <param name="lcid">区域标识符。</param>
/// <param name="ret">输出宽字符串。</param>
void  GetLocaleElaboratedCodeFromLcid (LCID lcid, std::wstring &ret)
{
	ret = GetLocaleElaboratedCodeFromLcidW (lcid);
}
/// <summary>
/// 从 LCID 获取详细区域代码（ANSI 输出）。
/// </summary>
/// <param name="lcid">区域标识符。</param>
/// <param name="ret">输出 ANSI 字符串。</param>
void  GetLocaleElaboratedCodeFromLcid (LCID lcid, std::string &ret)
{
	ret = GetLocaleElaboratedCodeFromLcidA (lcid);
}

/// <summary>
/// 将宽字符串形式的区域名称转换为 LCID。
/// </summary>
/// <param name="localeCode">区域名称（如 L"en-US"）。</param>
/// <returns>对应的 LCID；若失败返回 0。</returns>
LCID LocaleCodeToLcidW (const std::wstring &localeCode)
{
#if defined(_WIN32_WINNT) && (_WIN32_WINNT >= 0x0600)
	try
	{
		BYTE buf [LOCALE_NAME_MAX_LENGTH * sizeof (WCHAR)] = {0};
		int res = GetLocaleInfoEx (localeCode.c_str (), LOCALE_RETURN_NUMBER | LOCALE_ILANGUAGE, (LPWSTR)buf, LOCALE_NAME_MAX_LENGTH);
		LCID lcid = *((LCID *)buf);
		return lcid;
	}
	catch (const std::exception &e) {}
	return LocaleNameToLCID (localeCode.c_str (), 0);
#else
	return LocaleNameToLCID (localeCode.c_str (), 0);
#endif
}
/// <summary>
/// 将 ANSI 字符串形式的区域名称转换为 LCID。
/// </summary>
/// <param name="localeCode">区域名称（如 "en-US"）。</param>
/// <returns>对应的 LCID；若失败返回 0。</returns>
LCID LocaleCodeToLcidA (const std::string &localeCode)
{
	std::wstring lcWide = StringToWString (std::string (localeCode));
	return LocaleCodeToLcidW (lcWide.c_str ());
}
/// <summary>
/// 将宽字符串区域名称转换为 LCID（重载）。
/// </summary>
/// <param name="loccode">区域名称（宽字符串）。</param>
/// <returns>对应的 LCID。</returns>
LCID LocaleCodeToLcid (const std::wstring &loccode)
{
	return LocaleCodeToLcidW (loccode.c_str ());
}
/// <summary>
/// 将 ANSI 字符串区域名称转换为 LCID（重载）。
/// </summary>
/// <param name="loccode">区域名称（ANSI 字符串）。</param>
/// <returns>对应的 LCID。</returns>
LCID LocaleCodeToLcid (const std::string &loccode)
{
	return LocaleCodeToLcidA (loccode.c_str ());
}

/// <summary>
/// 根据区域名称获取受限区域代码（ANSI 版本，接受 C 字符串）。
/// </summary>
/// <param name="lc">区域名称（如 "en-US"）。</param>
/// <returns>受限区域代码的 ANSI 字符串。</returns>
std::string GetLocaleRestrictedCodeA (LPCSTR lc)
{
	return GetLocaleInfoA (LocaleCodeToLcidA (lc), 89);
}
/// <summary>
/// 根据区域名称获取受限区域代码（ANSI 版本，接受 std::string）。
/// </summary>
/// <param name="lc">区域名称（std::string）。</param>
/// <returns>受限区域代码的 ANSI 字符串。</returns>
std::string GetLocaleRestrictedCodeA (const std::string &lc)
{
	return GetLocaleInfoA (LocaleCodeToLcidA (lc.c_str ()), 89);
}
/// <summary>
/// 根据区域名称获取受限区域代码（Unicode 版本，接受 C 宽字符串）。
/// </summary>
/// <param name="lc">区域名称（如 L"en-US"）。</param>
/// <returns>受限区域代码的宽字符串。</returns>
std::wstring GetLocaleRestrictedCodeW (LPCWSTR lc)
{
	return GetLocaleInfoW (LocaleCodeToLcidW (lc), 89);
}
/// <summary>
/// 根据区域名称获取受限区域代码（Unicode 版本，接受 std::wstring）。
/// </summary>
/// <param name="lc">区域名称（std::wstring）。</param>
/// <returns>受限区域代码的宽字符串。</returns>
std::wstring GetLocaleRestrictedCodeW (const std::wstring &lc)
{
	return GetLocaleInfoW (LocaleCodeToLcidW (lc.c_str ()), 89);
}
/// <summary>
/// 根据区域名称获取受限区域代码（宽字符串重载）。
/// </summary>
/// <param name="lc">区域名称（std::wstring）。</param>
/// <returns>受限区域代码的宽字符串。</returns>
std::wstring GetLocaleRestrictedCode (const std::wstring &lc) { return GetLocaleRestrictedCodeW (lc); }
/// <summary>
/// 根据区域名称获取受限区域代码（ANSI 字符串重载）。
/// </summary>
/// <param name="lc">区域名称（std::string）。</param>
/// <returns>受限区域代码的 ANSI 字符串。</returns>
std::string GetLocaleRestrictedCode (const std::string &lc) { return GetLocaleRestrictedCodeA (lc); }

/// <summary>
/// 根据区域名称获取详细区域代码（ANSI 版本，接受 C 字符串）。
/// </summary>
/// <param name="lc">区域名称（如 "en-US"）。</param>
/// <returns>详细区域代码的 ANSI 字符串。</returns>
std::string GetLocaleElaboratedCodeA (LPCSTR lc)
{
	return GetLocaleInfoA (LocaleCodeToLcidA (lc), 90);
}
/// <summary>
/// 根据区域名称获取详细区域代码（ANSI 版本，接受 std::string）。
/// </summary>
/// <param name="lc">区域名称（std::string）。</param>
/// <returns>详细区域代码的 ANSI 字符串。</returns>
std::string GetLocaleElaboratedCodeA (const std::string &lc)
{
	return GetLocaleInfoA (LocaleCodeToLcidA (lc.c_str ()), 90);
}
/// <summary>
/// 根据区域名称获取详细区域代码（Unicode 版本，接受 C 宽字符串）。
/// </summary>
/// <param name="lc">区域名称（如 L"en-US"）。</param>
/// <returns>详细区域代码的宽字符串。</returns>
std::wstring GetLocaleElaboratedCodeW (LPCWSTR lc)
{
	return GetLocaleInfoW (LocaleCodeToLcidW (lc), 90);
}
/// <summary>
/// 根据区域名称获取详细区域代码（Unicode 版本，接受 std::wstring）。
/// </summary>
/// <param name="lc">区域名称（std::wstring）。</param>
/// <returns>详细区域代码的宽字符串。</returns>
std::wstring GetLocaleElaboratedCodeW (const std::wstring &lc)
{
	return GetLocaleInfoW (LocaleCodeToLcidW (lc.c_str ()), 90);
}
/// <summary>
/// 根据区域名称获取详细区域代码（宽字符串重载）。
/// </summary>
/// <param name="lc">区域名称（std::wstring）。</param>
/// <returns>详细区域代码的宽字符串。</returns>
std::wstring GetLocaleElaboratedCode (const std::wstring &lc) { return GetLocaleElaboratedCodeW (lc); }
/// <summary>
/// 根据区域名称获取详细区域代码（ANSI 字符串重载）。
/// </summary>
/// <param name="lc">区域名称（std::string）。</param>
/// <returns>详细区域代码的 ANSI 字符串。</returns>
std::string GetLocaleElaboratedCode (const std::string &lc) { return GetLocaleElaboratedCodeA (lc); }

/// <summary>
/// 将 LCID 转换为区域名称（ANSI 格式，如 "en-US"）。
/// </summary>
/// <param name="lcid">区域标识符。</param>
/// <param name="divide">分隔符，默认为 '-'。</param>
/// <returns>区域名称的 ANSI 字符串。</returns>
std::string LcidToLocaleCodeA (LCID lcid, char divide = '-')
{
	return GetLocaleRestrictedCodeFromLcidA (lcid) + divide + GetLocaleElaboratedCodeFromLcidA (lcid);
}
/// <summary>
/// 将 LCID 转换为区域名称（Unicode 格式，如 L"en-US"）。
/// </summary>
/// <param name="lcid">区域标识符。</param>
/// <param name="divide">分隔符，默认为 L'-'。</param>
/// <returns>区域名称的宽字符串。</returns>
std::wstring LcidToLocaleCodeW (LCID lcid, WCHAR divide = L'-')
{
#if defined(_WIN32_WINNT) && (_WIN32_WINNT >= 0x0600)
	try
	{
		WCHAR buf [LOCALE_NAME_MAX_LENGTH] = {0};
		LCIDToLocaleName (lcid, buf, LOCALE_NAME_MAX_LENGTH, 0);
		return buf;
	}
	catch (const std::exception &e) {}
	return GetLocaleRestrictedCodeFromLcidW (lcid) + divide + GetLocaleElaboratedCodeFromLcidW (lcid);
#else
	return GetLocaleRestrictedCodeFromLcidW (lcid) + divide + GetLocaleElaboratedCodeFromLcidW (lcid);
#endif
}
/// <summary>
/// 将 LCID 转换为区域名称（宽字符串重载）。
/// </summary>
/// <param name="lcid">区域标识符。</param>
/// <param name="divide">分隔符，默认为 L'-'。</param>
/// <returns>区域名称的宽字符串。</returns>
std::wstring LcidToLocaleCode (LCID lcid, WCHAR divide = L'-') { return LcidToLocaleCodeW (lcid, divide); }
/// <summary>
/// 将 LCID 转换为区域名称（ANSI 字符串重载）。
/// </summary>
/// <param name="lcid">区域标识符。</param>
/// <param name="divide">分隔符，默认为 '-'。</param>
/// <returns>区域名称的 ANSI 字符串。</returns>
std::string LcidToLocaleCode (LCID lcid, char divide = '-') { return LcidToLocaleCodeA (lcid, divide); }

/// <summary>
/// 获取当前用户的默认区域名称（Unicode）。
/// </summary>
/// <returns>用户默认区域名称的宽字符串。</returns>
std::wstring GetUserDefaultLocaleName ()
{
#if defined(_WIN32_WINNT) && (_WIN32_WINNT >= 0x0600)
	try
	{
		WCHAR buf [LOCALE_NAME_MAX_LENGTH] = {0};
		GetUserDefaultLocaleName (buf, LOCALE_NAME_MAX_LENGTH);
		return buf;
	}
	catch (const std::exception &e) {}
	return LcidToLocaleCodeW (GetUserDefaultLCID ());
#else
	return LcidToLocaleCodeW (GetUserDefaultLCID ());
#endif
}
/// <summary>
/// 获取系统默认的区域名称（Unicode）。
/// </summary>
/// <returns>系统默认区域名称的宽字符串。</returns>
std::wstring GetSystemDefaultLocaleName ()
{
#if defined(_WIN32_WINNT) && (_WIN32_WINNT >= 0x0600)
	try
	{
		WCHAR buf [LOCALE_NAME_MAX_LENGTH] = {0};
		GetSystemDefaultLocaleName (buf, LOCALE_NAME_MAX_LENGTH);
		return buf;
	}
	catch (const std::exception &e) {}
	return LcidToLocaleCodeW (GetSystemDefaultLCID ());
#else
	return LcidToLocaleCodeW (GetSystemDefaultLCID ());
#endif
}

/// <summary>
/// 获取当前计算机的区域设置名称（优先使用线程区域，然后用户默认，最后系统默认）。
/// </summary>
/// <returns>计算机区域名称的宽字符串。</returns>
std::wstring GetComputerLocaleCodeW ()
{
#if defined(_WIN32_WINNT) && (_WIN32_WINNT >= 0x0600)
	{
		try
		{
			{
				LCID lcid = GetThreadLocale ();
				std::wstring tmp = LcidToLocaleCodeW (lcid);
				if (lcid && tmp.length () > 1) return tmp;
			}
			{
				WCHAR buf [LOCALE_NAME_MAX_LENGTH] = {0};
				GetUserDefaultLocaleName (buf, LOCALE_NAME_MAX_LENGTH);
				if (lstrlenW (buf)) return buf;
			}
			{
				WCHAR buf [LOCALE_NAME_MAX_LENGTH] = {0};
				GetSystemDefaultLocaleName (buf, LOCALE_NAME_MAX_LENGTH);
				return buf;
			}
		}
		catch (const std::exception &e) {}
		LCID lcid = GetThreadLocale ();
		if (!lcid) lcid = GetUserDefaultLCID ();
		if (!lcid) lcid = GetSystemDefaultLCID ();
		return LcidToLocaleCodeW (lcid);
	}
#else
	{
		LCID lcid = GetThreadLocale ();
		if (!lcid) lcid = GetUserDefaultLCID ();
		if (!lcid) lcid = GetSystemDefaultLCID ();
		return LcidToLocaleCodeW (lcid);
	}
#endif
}
/// <summary>
/// 比较两个区域名称是否相等（支持字符串直接比较或通过 LCID 比较）。
/// </summary>
/// <param name="left">左区域名称。</param>
/// <param name="right">右区域名称。</param>
/// <returns>如果区域名称相同（字符串相等或 LCID 相等）则返回 true，否则 false。</returns>
/// <remarks>注意：此处使用的 std::wnstring::equals 非标准，可能为自定义函数，实际应替换为 wstring 比较。</remarks>
bool LocaleNameCompare (const std::wstring &left, const std::wstring &right)
{
	return std::wnstring::equals (left, right) || LocaleCodeToLcidW (left) == LocaleCodeToLcidW (right);
}
