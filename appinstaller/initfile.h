#pragma once
#ifndef _CRT_NON_CONFORMING_SWPRINTFS
#define _CRT_NON_CONFORMING_SWPRINTFS
#endif
#include <Windows.h>
#include <string>
#include <cstdlib>
#include <cstdio>
#include <vector>
#include <functional>
#include <cstdint>
#include <cwchar>
#include <type_traits>
#include "strcode.h"
#include "nstring.h"
#include "typestrans.h"
#ifdef _cplusplus_cli
#include "mpstr.h"
#endif

template <typename T> std::wstring TypeToString (T value, const std::wstring &reserve = L"") { return std::to_wstring (value); }
template <typename T> std::string TypeToString (T value, const std::string &reserve = "") { return std::to_string (value); }

std::string GetPrivateProfileStringA (const std::string &filePath, const std::string &section, const std::string &key, LPCSTR defaultValue = "")
{
	char buf [32768] = {0};
	GetPrivateProfileStringA (section.c_str (), key.c_str (), defaultValue, buf, 32767, filePath.c_str ());
	return buf;
}
std::wstring GetPrivateProfileStringW (const std::wstring &filePath, const std::wstring &section, const std::wstring &key, LPCWSTR defaultValue = L"")
{
	WCHAR buf [32768] = {0};
	GetPrivateProfileStringW (section.c_str (), key.c_str (), defaultValue, buf, 32767, filePath.c_str ());
	return buf;
}
UINT GetPrivateProfileIntA (const std::string &filePath, const std::string &section, const std::string &key, INT defaultValue = 0)
{
	return GetPrivateProfileIntA (section.c_str (), key.c_str (), defaultValue, filePath.c_str ());
}
UINT GetPrivateProfileIntW (const std::wstring &filePath, const std::wstring &section, const std::wstring &key, INT defaultValue = 0)
{
	return GetPrivateProfileIntW (section.c_str (), key.c_str (), defaultValue, filePath.c_str ());
}
BOOL WritePrivateProfileStringA (const std::string &filePath, const std::string &section, const std::string &key, const std::string &value)
{
	return WritePrivateProfileStringA (section.c_str (), key.c_str (), value.c_str (), filePath.c_str ());
}
BOOL WritePrivateProfileStringW (const std::wstring &filePath, const std::wstring &section, const std::wstring &key, const std::wstring &value)
{
	return WritePrivateProfileStringW (section.c_str (), key.c_str (), value.c_str (), filePath.c_str ());
}
size_t GetPrivateProfileSectionA (const std::string &filePath, const std::string &section, std::vector <std::string> &output)
{
	char buf [32768] = {0};
	DWORD len = GetPrivateProfileSectionA (section.c_str (), buf, sizeof (buf), filePath.c_str ());
	output.clear ();
	if (len == 0) return 0;
	char *ptr = buf;
	while (*ptr)
	{
		output.emplace_back (ptr);
		ptr += strlen (ptr) + 1;
	}
	return output.size ();
}
size_t GetPrivateProfileSectionW (const std::wstring &filePath, const std::wstring &section, std::vector <std::wstring> &output)
{
	WCHAR buf [32768] = {0};
	DWORD len = GetPrivateProfileSectionW (section.c_str (), buf, sizeof (buf) / sizeof (WCHAR), filePath.c_str ());
	output.clear ();
	if (len == 0) return 0;
	WCHAR *ptr = buf;
	while (*ptr)
	{
		output.emplace_back (ptr);
		ptr += wcslen (ptr) + 1;
	}
	return output.size ();
}
size_t GetPrivateProfileSectionNamesA (const std::string &filePath, std::vector <std::string> &output)
{
	char buf [32768] = {0};
	DWORD len = GetPrivateProfileSectionNamesA (buf, sizeof (buf), filePath.c_str ());
	output.clear ();
	if (len == 0) return 0;
	char *ptr = buf;
	while (*ptr)
	{
		output.emplace_back (ptr);
		ptr += strlen (ptr) + 1;
	}
	return output.size ();
}
size_t GetPrivateProfileSectionNamesW (const std::wstring &filePath, std::vector <std::wstring> &output)
{
	WCHAR buf [32768] = {0};
	DWORD len = GetPrivateProfileSectionNamesW (buf, sizeof (buf) / sizeof (WCHAR), filePath.c_str ());
	output.clear ();
	if (len == 0) return 0;
	WCHAR *ptr = buf;
	while (*ptr)
	{
		output.emplace_back (ptr);
		ptr += wcslen (ptr) + 1;
	}
	return output.size ();
}
bool WritePrivateProfileSectionA (const std::string &filePath, const std::string &section, const std::vector <std::string> &lines)
{
	std::string buf;
	for (const auto &line : lines) buf.append (line).push_back ('\0');
	buf.push_back ('\0');
	return WritePrivateProfileSectionA (section.c_str (), buf.c_str (), filePath.c_str ()) != 0;
}
bool WritePrivateProfileSectionW (const std::wstring &filePath, const std::wstring &section, const std::vector <std::wstring> &lines)
{
	std::wstring buf;
	for (const auto &line : lines) buf.append (line).push_back (L'\0');
	buf.push_back (L'\0'); // 双 \0 结尾
	return WritePrivateProfileSectionW (section.c_str (), buf.c_str (), filePath.c_str ()) != 0;
}
bool GetPrivateProfileStructA (const std::string &filePath, const std::string &section, const std::string &key, void *output, UINT size)
{
	return GetPrivateProfileStructA (section.c_str (), key.c_str (), output, size, filePath.c_str ()) != 0;
}
bool WritePrivateProfileStructA (const std::string &filePath, const std::string &section, const std::string &key, void *data, UINT size)
{
	return WritePrivateProfileStructA (section.c_str (), key.c_str (), data, size, filePath.c_str ()) != 0;
}
bool GetPrivateProfileStructW (const std::wstring &filePath, const std::wstring &section, const std::wstring &key, void *output, UINT size)
{
	return GetPrivateProfileStructW (section.c_str (), key.c_str (), output, size, filePath.c_str ()) != 0;
}
bool WritePrivateProfileStructW (const std::wstring &filePath, const std::wstring &section, const std::wstring &key, void *data, UINT size)
{
	return WritePrivateProfileStructW (section.c_str (), key.c_str (), data, size, filePath.c_str ()) != 0;
}
size_t GetPrivateProfileKeysW (const std::wstring &filePath, const std::wstring &section, std::vector <std::wstring> &keys)
{
	WCHAR buf [32768] = {0};
	DWORD len = GetPrivateProfileSectionW (section.c_str (), buf, sizeof (buf) / sizeof (WCHAR), filePath.c_str ());
	keys.clear ();
	if (len == 0) return 0;
	WCHAR* ptr = buf;
	while (*ptr)
	{
		std::wstring line = ptr;
		size_t pos = line.find (L'=');
		if (pos != std::wstring::npos) keys.push_back (line.substr (0, pos));
		ptr += wcslen (ptr) + 1;
	}
	return keys.size ();
}
size_t GetPrivateProfileKeysA (const std::string &filePath, const std::string &section, std::vector <std::string> &keys)
{
	char buf [32768] = {0};
	DWORD len = GetPrivateProfileSectionA (section.c_str (), buf, sizeof (buf), filePath.c_str ());
	keys.clear ();
	if (len == 0)
		return 0;
	char *ptr = buf;
	while (*ptr)
	{
		std::string line = ptr;
		size_t pos = line.find ('=');
		if (pos != std::string::npos) keys.push_back (line.substr (0, pos));
		ptr += strlen (ptr) + 1;
	}
	return keys.size ();
}
bool DeletePrivateProfileKeyA (const std::string &filePath, const std::string &section, const std::string &key)
{
	return WritePrivateProfileStringA (section.c_str (), key.c_str (), NULL, filePath.c_str ()) != FALSE;
}
bool DeletePrivateProfileKeyW (const std::wstring &filePath, const std::wstring &section, const std::wstring &key)
{
	return WritePrivateProfileStringW (section.c_str (), key.c_str (), NULL, filePath.c_str ()) != FALSE;
}
bool DeletePrivateProfileSectionA (const std::string &filePath, const std::string &section)
{
	return WritePrivateProfileStringA (section.c_str (), NULL, NULL, filePath.c_str ()) != FALSE;
}
bool DeletePrivateProfileSectionW (const std::wstring &filePath, const std::wstring &section)
{
	return WritePrivateProfileStringW (section.c_str (), NULL, NULL, filePath.c_str ()) != FALSE;
}

class initkey
{
	public:
	using pstring = std::string &;
	using pwstring = std::wstring &;
	using pcstring = const std::string &;
	using pcwstring = const std::wstring &;
	private:
	pcwstring filepath;
	pcwstring section;
	template <typename T, typename Trans = T, typename Func> T read_t (T defaultvalue, Func process) const
	{
		auto res = read_wstring (std::to_wstring ((Trans)defaultvalue));
		if (IsNormalizeStringEmpty (res)) return defaultvalue;
		return (T)process (res.c_str ());
	}
	template <typename T> bool write_t (T value) { return write_string (std::to_wstring (value)); }
	public:
	std::wstring key;
	initkey (pcwstring path, pcwstring sect, pcwstring k): filepath (path), section (sect), key (k) {}
	std::wstring read_wstring (pcwstring defaultvalue = L"") const { return GetPrivateProfileStringW (filepath, section, key, defaultvalue.c_str ()); }
	std::string read_string (pcstring defaultvalue = "") const { return WStringToString (read_wstring (StringToWString (defaultvalue))); }
	short read_short (short defaultvalue = 0) const { return read_t (defaultvalue, _wtoi16); }
	unsigned short read_ushort (unsigned short defaultvalue = 0) const { return read_t (defaultvalue, _wtoui16); }
	int read_int (int defaultvalue = 0) const { return read_t (defaultvalue, _wtoi); }
	unsigned int read_uint (unsigned int defaultvalue = 0) const { return read_t (defaultvalue, _wtou); }
	long read_long (long defaultvalue = 0) const { return read_t (defaultvalue, _wtol); }
	unsigned long read_ulong (unsigned long defaultvalue = 0) const { return read_t (defaultvalue, _wtoul); }
	long long read_llong (long long defaultvalue = 0) const { return read_t (defaultvalue, _wtoll); }
	unsigned long long read_ullong (unsigned long long defaultvalue = 0) const { return read_t (defaultvalue, _wtou64); }
	float read_float (float defaultvalue = 0) const { return read_t (defaultvalue, _wtof); }
	double read_double (double defaultvalue = 0) const { return read_t (defaultvalue, _wtod); }
	bool read_bool (bool defaultvalue = false) const
	{
		std::wnstring res = read_wstring (defaultvalue ? L"true" : L"false");
		if (res.empty ()) return defaultvalue;
		if (res.equals (L"true") || res.equals (L"yes") || res.equals (L"zhen") || res.equals (L"真") || res.equals (L"1") || _wtoi (res.c_str ()) != 0) return true;
		else if (res.equals (L"false") || res.equals (L"no") || res.equals (L"jia") || res.equals (L"假") || res.equals (L"0")) return false;
		else return defaultvalue;
	}
	int8_t read_i8 (int8_t defaultvalue = 0) const { return read_t <int8_t, int16_t> (defaultvalue, _wtoi8); }
	uint8_t read_u8 (uint8_t defaultvalue = 0) const { return read_t <uint8_t, uint16_t> (defaultvalue, _wtoui8); }
	int16_t read_i16 (int16_t defaultvalue = 0) const { return read_t (defaultvalue, _wtoi16); }
	uint16_t read_u16 (uint16_t defaultvalue = 0) const { return read_t (defaultvalue, _wtoui16); }
	int32_t read_i32 (int32_t defaultvalue = 0) const { return read_t (defaultvalue, _wtoi32); }
	uint32_t read_u32 (uint32_t defaultvalue = 0) const { return read_t (defaultvalue, _wtoui32); }
	int64_t read_i64 (int64_t defaultvalue = 0) const { return read_t (defaultvalue, _wtoi64); }
	uint64_t read_u64 (uint64_t defaultvalue = 0) const { return read_t (defaultvalue, _wtou64); }
	bool read_struct (void *output, size_t size) const { return GetPrivateProfileStructW (filepath, section, key, output, size); }
	template <typename T> bool read_struct (T &structinst) const { return read_struct (&structinst, sizeof (structinst)); }
	bool write_string (pcwstring value) { return write (value); }
	bool write_string (pcstring value) { return write (value); }
	bool write (pcwstring value) { return WritePrivateProfileStringW (filepath, section, key, value); }
	bool write (pcstring value) { return write (StringToWString (value)); }
	bool write (LPCWSTR value) { return write_string (std::wstring (value ? value : L"")); }
	bool write (LPCSTR value) { return write_string (value ? value : ""); }
	bool write (int value) { return write_t (value); }
	bool write (unsigned int value) { return write_t (value); }
	bool write (short value) { return write_t (value); }
	bool write (unsigned short value) { return write_t (value); }
	bool write (long value) { return write_t (value); }
	bool write (unsigned long value) { return write_t (value); }
	bool write (long long value) { return write_t (value); }
	bool write (unsigned long long value) { return write_t (value); }
	bool write (int8_t value) { return write_t ((int16_t)value); }
	bool write (uint8_t value) { return write_t ((uint16_t)value); }
	bool write (float value) { return write_t (value); }
	bool write (double value) { return write_t (value); }
	bool write (bool value) { return write (value ? L"true" : L"false"); }
	bool write (void *buf, size_t bufsize) { return WritePrivateProfileStructW (filepath, section, key, buf, bufsize); }
	operator std::wstring () { return read_wstring (); }
	operator std::string () { return read_string (); }
	template <typename T> initkey &operator = (T value) { write (value); return *this; }
	initkey &operator = (const initkey &) = delete;
	// 删除项
	bool clear () { return DeletePrivateProfileKeyW (filepath, section, key); }
	bool empty () const { return read_wstring ().empty (); }
#define OPERATOR_TYPE_READ(_type_, _method_) \
operator _type_ () { return _method_ (); }
	OPERATOR_TYPE_READ (int, read_int)
		OPERATOR_TYPE_READ (unsigned int, read_uint)
		OPERATOR_TYPE_READ (long, read_long)
		OPERATOR_TYPE_READ (unsigned long, read_ulong)
		OPERATOR_TYPE_READ (long long, read_llong)
		OPERATOR_TYPE_READ (unsigned long long, read_ullong)
		OPERATOR_TYPE_READ (short, read_short)
		OPERATOR_TYPE_READ (unsigned short, read_ushort)
		OPERATOR_TYPE_READ (float, read_float)
		OPERATOR_TYPE_READ (double, read_double)
		OPERATOR_TYPE_READ (bool, read_bool)
	#ifdef OPERATOR_TYPE_READ
	#undef OPERATOR_TYPE_READ
	#endif
};
class initsection
{
	private:
	const std::wstring &filepath;
	template <typename T, typename Trans = T, typename CT, typename Func> T read_t (const std::basic_string <CT> &key, T defaultvalue, Func process) const
	{
		std::basic_string <CT> temp;
		auto res = read_string (key, TypeToString ((Trans)defaultvalue, temp));
		if (IsNormalizeStringEmpty (res)) return defaultvalue;
		return (T)process (res.c_str ());
	}
	template <typename T, typename CT> bool write_t (const std::basic_string <CT> &key, T value)
	{
		std::basic_string <CT> temp;
		return write_string (key, TypeToString (value, temp));
	}
	public:
	using pcstring = const std::string &;
	using pcwstring = const std::wstring &;
	std::wstring section;
	initsection (const std::wstring &path, const std::wstring &sect): filepath (path), section (sect) {}
	size_t keys (std::vector <std::wstring> &output) const { return GetPrivateProfileKeysW (filepath, section, output); }
	size_t keys (std::vector <std::string> &output) const { return GetPrivateProfileKeysA (WStringToString (filepath), WStringToString (section), output); }
	bool key_values (const std::vector <std::wstring> &lines) { return WritePrivateProfileSectionW (filepath, section, lines); }
	bool key_values (const std::vector <std::string> &lines) { return WritePrivateProfileSectionA (WStringToString (filepath), WStringToString (section), lines); }
	size_t key_values (std::vector <std::wstring> &output) const { return GetPrivateProfileSectionW (filepath, section, output); }
	size_t key_values (std::vector <std::string> &output) const { return GetPrivateProfileSectionA (WStringToString (filepath), WStringToString (section), output); }
	std::wstring read_string (const std::wstring &key, const std::wstring &defaultvalue = L"") const { return GetPrivateProfileStringW (filepath, section, key, defaultvalue.c_str ()); }
	std::string read_string (const std::string &key, const std::string &defaultvalue = "") const { return WStringToString (read_string (StringToWString (key), StringToWString (defaultvalue))); }
	std::wstring read_wstring (const std::wstring &key, const std::wstring &defaultvalue = L"") const { return read_string (key, defaultvalue); }
	int read_int (const std::wstring &key, int defaultvalue = 0) const { return read_t (key, defaultvalue, _wtoi); }
	int read_int (const std::string &key, int defaultvalue = 0) const { return read_t (key, defaultvalue, atoi); }
	unsigned int read_uint (const std::wstring &key, unsigned int defaultvalue = 0) const { return read_t (key, defaultvalue, _wtou); }
	unsigned int read_uint (const std::string &key, unsigned int defaultvalue = 0) const { return read_t (key, defaultvalue, atou); }
	long read_long (const std::wstring &key, long defaultvalue = 0) const { return read_t (key, defaultvalue, _wtol); }
	long read_long (const std::string &key, long defaultvalue = 0) const { return read_t (key, defaultvalue, atol); }
	unsigned long read_ulong (const std::wstring &key, unsigned long defaultvalue = 0) const { return read_t (key, defaultvalue, _wtoul); }
	unsigned long read_ulong (const std::string &key, unsigned long defaultvalue = 0) const { return read_t (key, defaultvalue, atoul); }
	long long read_llong (const std::wstring &key, long long defaultvalue = 0) const { return read_t (key, defaultvalue, _wtoll); }
	long long read_llong (const std::string &key, long long defaultvalue = 0) const { return read_t (key, defaultvalue, atoll); }
	unsigned long long read_ullong (const std::wstring &key, unsigned long long defaultvalue = 0) const { return read_t (key, defaultvalue, _wtou64); }
	unsigned long long read_ullong (const std::string &key, unsigned long long defaultvalue = 0) const { return read_t (key, defaultvalue, atou64); }
	int8_t read_i8 (const std::wstring &key, int8_t defaultvalue = 0) const { return read_t <int8_t, int16_t> (key, defaultvalue, _wtoi8); }
	int8_t read_i8 (const std::string &key, int8_t defaultvalue = 0) const { return read_t <int8_t, int16_t> (key, defaultvalue, atoi8); }
	uint8_t read_u8 (const std::wstring &key, uint8_t defaultvalue = 0) const { return read_t <uint8_t, uint16_t> (key, defaultvalue, _wtoui8); }
	uint8_t read_u8 (const std::string &key, uint8_t defaultvalue = 0) const { return read_t <uint8_t, uint16_t> (key, defaultvalue, atoui8); }
	int16_t read_i16 (const std::wstring &key, int16_t defaultvalue = 0) const { return read_t (key, defaultvalue, _wtoi16); }
	int16_t read_i16 (const std::string &key, int16_t defaultvalue = 0) const { return read_t (key, defaultvalue, atoi16); }
	short read_short (pcwstring key, short defaultvalue = 0) const { return read_i16 (key, defaultvalue); }
	short read_short (pcstring key, short defaultvalue = 0) const { return read_i16 (key, defaultvalue); }
	unsigned short read_ushort (pcwstring key, unsigned short defaultvalue = 0) const { return read_u16 (key, defaultvalue); }
	unsigned short read_ushort (pcstring key, unsigned short defaultvalue = 0) const { return read_u16 (key, defaultvalue); }
	uint16_t read_u16 (const std::wstring &key, uint16_t defaultvalue = 0) const { return read_t (key, defaultvalue, _wtoui16); }
	uint16_t read_u16 (const std::string &key, uint16_t defaultvalue = 0) const { return read_t (key, defaultvalue, atoui16); }
	int32_t read_i32 (const std::wstring &key, int32_t defaultvalue = 0) const { return read_t (key, defaultvalue, _wtoi32); }
	int32_t read_i32 (const std::string &key, int32_t defaultvalue = 0) const { return read_t (key, defaultvalue, atoi32); }
	uint32_t read_u32 (const std::wstring &key, uint32_t defaultvalue = 0) const { return read_t (key, defaultvalue, _wtoui32); }
	uint32_t read_u32 (const std::string &key, uint32_t defaultvalue = 0) const { return read_t (key, defaultvalue, atoui32); }
	int64_t read_i64 (const std::wstring &key, int64_t defaultvalue = 0) const { return read_t (key, defaultvalue, _wtoi64); }
	int64_t read_i64 (const std::string &key, int64_t defaultvalue = 0) const { return read_t (key, defaultvalue, atoi64); }
	uint64_t read_u64 (const std::wstring &key, uint64_t defaultvalue = 0) const { return read_ullong (key, defaultvalue); }
	uint64_t read_u64 (const std::string &key, uint64_t defaultvalue = 0) const { return read_ullong (key, defaultvalue); }
	float read_float (const std::wstring &key, float defaultvalue = 0) const { return read_t (key, defaultvalue, _wtof); }
	double read_double (const std::wstring &key, double defaultvalue = 0) const { return read_t (key, defaultvalue, _wtod); }
	bool read_bool (const std::wstring &key, bool defaultvalue = false) const
	{
		std::wnstring res = read_string (key, defaultvalue ? L"true" : L"false");
		if (res.empty ()) return defaultvalue;
		if (res.equals (L"true") || res.equals (L"yes") || res.equals (L"zhen") || res.equals (L"真") || res.equals (L"1") || _wtoi (res.c_str ()) != 0) return true;
		else if (res.equals (L"false") || res.equals (L"no") || res.equals (L"jia") || res.equals (L"假") || res.equals (L"0")) return false;
		else return defaultvalue;
	}
	bool read_bool (const std::string &key, bool defaultvalue = false) const { return read_bool (StringToWString (key), defaultvalue); }
	bool read_struct (const std::wstring &key, void *output, size_t size) const { return GetPrivateProfileStructW (filepath, section, key, output, size); }
	template <typename T> bool read_struct (const std::wstring &key, T &structinst) const { return read_struct (key, &structinst, sizeof (structinst)); }
	bool write_string (const std::wstring &key, const std::wstring &value) { return WritePrivateProfileStringW (filepath, section, key, value); }
	bool write_string (const std::string &key, const std::string &value) { return write_string (StringToWString (key), StringToWString (value)); }
	bool write (pcwstring key, LPCWSTR value) { return write_string (key, value ? value : L""); }
	bool write (pcstring key, LPCSTR value) { return write_string (key, value ? value : ""); }
	bool write (const std::wstring &key, const std::wstring &value) { return write_string (key, value); }
	bool write (const std::string &key, const std::string &value) { return write_string (key, value); }
	bool write (pcwstring key, short value) { return write_t (key, value); }
	bool write (pcwstring key, unsigned short value) { return write_t (key, value); }
	bool write (pcwstring key, int value) { return write_t (key, value); }
	bool write (pcwstring key, unsigned int value) { return write_t (key, value); }
	bool write (pcwstring key, long value) { return write_t (key, value); }
	bool write (pcwstring key, unsigned long value) { return write_t (key, value); }
	bool write (pcwstring key, long long value) { return write_t (key, value); }
	bool write (pcwstring key, unsigned long long value) { return write_t (key, value); }
	bool write (pcwstring key, int8_t value) { return write_t (key, (int16_t)value); }
	bool write (pcwstring key, uint8_t value) { return write_t (key, (uint16_t)value); }
	bool write (pcwstring key, bool value) { return write (key, value ? L"true" : L"false"); }
	bool write (pcstring key, short value) { return write_t (key, value); }
	bool write (pcstring key, unsigned short value) { return write_t (key, value); }
	bool write (pcstring key, int value) { return write_t (key, value); }
	bool write (pcstring key, unsigned int value) { return write_t (key, value); }
	bool write (pcstring key, long value) { return write_t (key, value); }
	bool write (pcstring key, unsigned long value) { return write_t (key, value); }
	bool write (pcstring key, long long value) { return write_t (key, value); }
	bool write (pcstring key, unsigned long long value) { return write_t (key, value); }
	bool write (pcstring key, int8_t value) { return write_t (key, (int16_t)value); }
	bool write (pcstring key, uint8_t value) { return write_t (key, (uint16_t)value); }
	bool write (pcstring key, bool value) { return write (StringToWString (key), value ? L"true" : L"false"); }
	bool write (pcwstring key, void *buf, size_t bufsize) { return WritePrivateProfileStructW (filepath, section, key, buf, bufsize); }
	bool write (pcstring key, void *buf, size_t bufsize) { return write (StringToWString (key), buf, bufsize); }
	initkey operator [] (pcwstring key) { return initkey (filepath, section, key); }
	initkey operator [] (pcstring key) { return initkey (filepath, section, StringToWString (key)); }
	std::wstring operator [] (pcwstring key) const { return read_string (key); }
	std::wstring operator [] (pcstring key) const { return read_string (StringToWString (key)); }
	bool delete_key (pcwstring key) { return DeletePrivateProfileKeyW (filepath, section, key); }
	bool delete_key (pcstring key) { return delete_key (StringToWString (key)); }
	bool clear () { return DeletePrivateProfileSectionW (filepath, section); }
	initkey get_key (pcwstring key) const { return initkey (filepath, section, key); }
	initkey get_key (pcwstring key) { return initkey (filepath, section, key); }
	initkey get_key (pcstring key) const { return initkey (filepath, section, StringToWString (key)); }
	initkey get_key (pcstring key) { return initkey (filepath, section, StringToWString (key)); }
	initsection &operator = (const initsection &) = delete;
};
class initfile
{
	public:
	using pstring = std::string &;
	using pwstring = std::wstring &;
	using pcstring = const std::string &;
	using pcwstring = const std::wstring &;
	std::wstring filepath;
	private:
	template <typename T, typename TRANS = T, typename FN> T read_t (pcwstring section, pcwstring key, T defaultvalue, FN process) const
	{
		auto res = read_wstring (section, key, std::to_wstring ((TRANS)defaultvalue));
		if (IsNormalizeStringEmpty (res)) return defaultvalue;
		return (T)process (res.c_str ());
	}
	template <typename T> bool write_t (pcwstring section, pcwstring key, T value) { return write (section, key, std::to_wstring (value)); }
	public:
	initfile (const std::wstring &initpath): filepath (initpath) {}
	size_t sections (std::vector <std::wstring> &sect) const { return GetPrivateProfileSectionNamesW (filepath, sect); }
	size_t sections (std::vector <std::string> &sect) const { return GetPrivateProfileSectionNamesA (WStringToString (filepath), sect); }
	bool delete_section (pcwstring section) { return DeletePrivateProfileSectionW (filepath, section); }
	bool delete_section (pcstring section) { return delete_section (StringToWString (section)); }
	size_t key_values (pcwstring section, std::vector <std::wstring> &keyvalues) const { return GetPrivateProfileSectionW (filepath, section, keyvalues); }
	size_t key_values (pcstring section, std::vector <std::string> &keyvalues) const { return GetPrivateProfileSectionA (WStringToString (filepath), section, keyvalues); }
	size_t keys (pcwstring section, std::vector <std::wstring> &keyvalues) const { return GetPrivateProfileKeysW (filepath, section, keyvalues); }
	size_t keys (pcstring section, std::vector <std::string> &keyvalues) const { return GetPrivateProfileKeysA (WStringToString (filepath), section, keyvalues); }
	initsection get_section (pcwstring section) { return initsection (filepath, section); }
	initsection get_section (pcwstring section) const { return initsection (filepath, section); }
	std::wstring read_wstring (pcwstring section, pcwstring key, pcwstring dflt = L"") const { return GetPrivateProfileStringW (filepath, section, key, dflt.c_str ()); }
	std::string read_string (pcwstring section, pcwstring key, pcstring dflt = "") const { return WStringToString (read_wstring (section, key, StringToWString (dflt))); }
#define INIT_READ_WARGS(_type_, _dfltvalue_) pcwstring section, pcwstring key, _type_ dflt = _dfltvalue_
#define METHOD_INIT_READ(_type_, _typename_, _dfltvalue_, _process_) \
_type_ read_##_typename_ (INIT_READ_WARGS (_type_, _dfltvalue_)) const { return read_t (section, key, dflt, _process_); }
	METHOD_INIT_READ (int, int, 0, _wtoi)
		METHOD_INIT_READ (unsigned int, uint, 0, _wtou)
		METHOD_INIT_READ (long, long, 0, _wtol)
		METHOD_INIT_READ (unsigned long, ulong, 0, _wtoul)
		METHOD_INIT_READ (long long, llong, 0, _wtoll)
		METHOD_INIT_READ (unsigned long, ullong, 0, _wtou64)
		METHOD_INIT_READ (short, short, 0, _wtoi16)
		METHOD_INIT_READ (unsigned short, ushort, 0, _wtoui16)
		METHOD_INIT_READ (int16_t, i16, 0, _wtoi16)
		METHOD_INIT_READ (uint16_t, u16, 0, _wtoui16)
		METHOD_INIT_READ (int32_t, i32, 0, _wtoi32)
		METHOD_INIT_READ (uint32_t, u32, 0, _wtoui32)
		METHOD_INIT_READ (int64_t, i64, 0, _wtoi64)
		METHOD_INIT_READ (uint64_t, u64, 0, _wtou64)
		METHOD_INIT_READ (float, float, 0, _wtof)
		METHOD_INIT_READ (double, double, 0, _wtod)
		int8_t read_i8 (INIT_READ_WARGS (int8_t, 0)) const { return read_t <int8_t, int16_t> (section, key, dflt, _wtoi8); }
	uint8_t read_u8 (INIT_READ_WARGS (uint8_t, 0)) const { return read_t <uint8_t, uint16_t> (section, key, dflt, _wtoui8); }
	bool read_bool (INIT_READ_WARGS (bool, false)) const
	{
		std::wnstring res = read_wstring (section, key, dflt ? L"true" : L"false");
		if (res.empty ()) return dflt;
		if (res.equals (L"true") || res.equals (L"yes") || res.equals (L"zhen") || res.equals (L"真") || res.equals (L"1") || _wtoi (res.c_str ()) != 0) return true;
		else if (res.equals (L"false") || res.equals (L"no") || res.equals (L"jia") || res.equals (L"假") || res.equals (L"0")) return false;
		else return dflt;
	}
	bool read_struct (pcwstring section, pcwstring key, void *output, size_t size) const { return GetPrivateProfileStructW (filepath, section, key, output, size); }
	template <typename T> bool read_struct (pcwstring section, pcwstring key, T &structinst) const { return read_struct (key, &structinst, sizeof (structinst)); }
#ifdef INIT_READ_WARGS
#undef INIT_READ_WARGS
#endif
#ifdef METHOD_INIT_READ
#undef METHOD_INIT_READ
#endif
#define INIT_WRITE_WARGS(_type_) pcwstring section, pcwstring key, _type_ value
	bool write (INIT_WRITE_WARGS (pcwstring)) { return WritePrivateProfileStringW (filepath, section, key, value); }
#define METHOD_INIT_WRITE(_type_) \
bool write (INIT_WRITE_WARGS (_type_)) { return write_t (section, key, value); }
	METHOD_INIT_WRITE (short)
		METHOD_INIT_WRITE (unsigned short)
		METHOD_INIT_WRITE (int)
		METHOD_INIT_WRITE (unsigned int)
		METHOD_INIT_WRITE (long)
		METHOD_INIT_WRITE (unsigned long)
		METHOD_INIT_WRITE (long long)
		METHOD_INIT_WRITE (unsigned long long)
		METHOD_INIT_WRITE (float)
		METHOD_INIT_WRITE (double)
		bool write (INIT_WRITE_WARGS (bool)) { return write (section, key, value ? L"true" : L"false"); }
	bool write (INIT_WRITE_WARGS (int8_t)) { return write_t (section, key, (int16_t)value); }
	bool write (INIT_WRITE_WARGS (uint8_t)) { return write_t (section, key, (uint16_t)value); }
	bool write (pcwstring section, pcwstring key, LPCWSTR value) { return write (section, key, std::wstring (value ? value : L"")); }
	bool write (pcstring section, pcstring key, LPCSTR value) { return write (StringToWString (section), StringToWString (key), StringToWString (value)); }
	bool write (pcwstring section, pcwstring key, void *buf, size_t bufsize) { return WritePrivateProfileStructW (filepath, section, key, buf, bufsize); }
	initsection operator [] (pcwstring section) { return initsection (filepath, section); }
	initsection operator [] (pcstring section) { return initsection (filepath, StringToWString (section)); }
	initsection operator [] (pcwstring section) const { return initsection (filepath, section); }
	initsection operator [] (pcstring section) const { return initsection (filepath, StringToWString (section)); }
#ifdef METHOD_INIT_WRITE
#undef METHOD_INIT_WRITE
#endif
#ifdef INIT_WRITE_WARGS
#undef INIT_WRITE_WARGS
#endif
};

#ifdef __cplusplus_cli
namespace Win32
{
	using namespace System;
	using namespace System::Runtime::InteropServices;
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType::AutoDual)]
	public ref class Key
	{
		private:
		String ^filepath = "";
		String ^section = "";
		String ^key = "";
		public:
		property String ^FilePath { String ^get () { return filepath; }}
		property String ^Section { String ^get () { return section; }}
		property String ^KeyName { String ^get () { return key; }}
		Key (String ^file, String ^sect, String ^k): filepath (file), section (sect), key (k) {}
		Object ^Get (Object ^dflt)
		{
			auto res = GetPrivateProfileStringW (
				MPStringToStdW (filepath),
				MPStringToStdW (section),
				MPStringToStdW (key),
				dflt ? MPStringToStdW (dflt->ToString ()).c_str () : L""
			);
			return CStringToMPString (res);
		}
		Object ^GetWithDefault (Object ^dflt) { return Get (dflt); }
		Object ^Get ()
		{
			auto res = GetPrivateProfileStringW (
				MPStringToStdW (filepath),
				MPStringToStdW (section),
				MPStringToStdW (key)
			);
			return CStringToMPString (res);
		}
		bool Set (Object ^value)
		{
			return WritePrivateProfileStringW (
				MPStringToStdW (filepath),
				MPStringToStdW (section),
				MPStringToStdW (key),
				MPStringToStdW (value ? value->ToString () : L"")
			);
		}
		property Object ^Value { Object ^get () { return Get (); } void set (Object ^value) { Set (value); } }
		Key %operator = (Object ^value) { Value = value; return *this; }
		operator String ^ () { return Value->ToString (); }
		explicit operator bool ()
		{
			auto boolstr = Value->ToString ()->Trim ()->ToLower ();
			if (boolstr == "true" || boolstr == "zhen" || boolstr == "yes" || boolstr == "真") return true;
			else if (boolstr == "false" || boolstr == "jia" || boolstr == "no" || boolstr == "假") return false;
			else return false;
		}
	#define OPERATOR_TRANSITION_DEFINE(type, transfunc, defaultret) \
operator type () { try { transfunc (Value->ToString ()); } catch (...) { return defaultret; }}
		OPERATOR_TRANSITION_DEFINE (int8_t, Convert::ToSByte, 0)
			OPERATOR_TRANSITION_DEFINE (uint8_t, Convert::ToByte, 0)
			OPERATOR_TRANSITION_DEFINE (int16_t, Convert::ToInt16, 0)
			OPERATOR_TRANSITION_DEFINE (uint16_t, Convert::ToUInt16, 0)
			OPERATOR_TRANSITION_DEFINE (int32_t, Convert::ToInt32, 0)
			OPERATOR_TRANSITION_DEFINE (uint32_t, Convert::ToUInt32, 0)
			OPERATOR_TRANSITION_DEFINE (int64_t, Convert::ToInt64, 0)
			OPERATOR_TRANSITION_DEFINE (uint64_t, Convert::ToUInt64, 0)
			OPERATOR_TRANSITION_DEFINE (float, Convert::ToSingle, 0)
			OPERATOR_TRANSITION_DEFINE (double, Convert::ToDouble, 0)
			OPERATOR_TRANSITION_DEFINE (System::Decimal, Convert::ToDecimal, 0)
			OPERATOR_TRANSITION_DEFINE (System::DateTime, Convert::ToDateTime, Convert::ToDateTime (0))
		#ifdef OPERATOR_TRANSITION_DEFINE
		#undef OPERATOR_TRANSITION_DEFINE
		#endif
	};
	[ComVisible (true)]
	public ref class Section
	{
		private:
		String ^filepath = "";
		String ^section = "";
		public:
		property String ^FilePath { String ^get () { return filepath; } }
		property String ^SectionName { String ^get () { return section; } }
		Section (String ^file, String ^sect): filepath (file), section (sect) {}
		Key ^GetKey (String ^key) { return gcnew Key (filepath, section, key); }
		Object ^Get (String ^key, Object ^dflt) { return GetKey (key)->Get (dflt); }
		Object ^Get (String ^key) { return GetKey (key)->Get (); }
		bool Set (String ^key, Object ^value) { return GetKey (key)->Set (value); }
		Key ^operator [] (String ^key) { return GetKey (key); }
	};
	[ComVisible (true)]
	public ref class InitConfig
	{
		private:
		String ^filepath = "";
		public:
		property String ^FilePath { String ^get () { return filepath; } void set (String ^path) { filepath = path; } }
		InitConfig (String ^path): filepath (path) {}
		InitConfig () {}
		Section ^GetSection (String ^section) { return gcnew Section (filepath, section); }
		Key ^GetKey (String ^section, String ^key) { return gcnew Key (filepath, section, key); }
		Object ^Get (String ^section, String ^key, String ^dflt) { return GetKey (section, key)->Get (dflt); }
		Object ^Get (String ^section, String ^key) { return GetKey (section, key)->Get (); }
		Section ^Get (String ^section) { return GetSection (section); }
		bool Set (String ^section, String ^key, String ^value) { return GetKey (section, key)->Set (value); }
		Section ^operator [] (String ^section) { return GetSection (section); }
	};
}

#endif