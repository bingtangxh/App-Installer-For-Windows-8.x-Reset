#pragma once
#ifndef _CRT_SECURE_NO_WARNINGS
#define _CRT_SECURE_NO_WARNINGS
#endif
#include <Windows.h>
#ifdef __cplusplus
#include <cstdio>
#include <cstdlib>
#include <cstdbool>
#include <cstring>
#else
#include <stdio.h>
#include <stdlib.h>
#include <stdbool.h>
#include <string.h>
#endif
unsigned _wtou (const wchar_t *str)
{
	unsigned value = 0;
	if (str)
	{
		swscanf (str, L"%u", &value);
	}
	return value;
}
unsigned long _wtoul (const wchar_t *str)
{
	unsigned value = 0;
	if (str)
	{
		swscanf (str, L"%lu", &value);
	}
	return value;
}
unsigned long long _wtou64 (const wchar_t *str)
{
	unsigned long long value = 0;
	if (str)
	{
		swscanf (str, L"%llu", &value);
	}
	return value;
}
double _wtod (const wchar_t *str)
{
	if (!str || !*str) return 0.0; // ±‹√‚ø’÷∏’ÎªÚø’◊÷∑˚¥Æ
	double value = 0.0;
	if (swscanf (str, L"%lg", &value) == 1)
	{
		return value;
	}
	return 0.0; // Ω‚Œˆ ß∞‹ ±∑µªÿ 0.0
}
unsigned atou (const char *str)
{
	unsigned value = 0;
	if (str)
	{
		sscanf (str, "%u", &value);
	}
	return value;
}
unsigned long atoul (const char *str)
{
	unsigned value = 0;
	if (str)
	{
		sscanf (str, "%lu", &value);
	}
	return value;
}
unsigned long long atou64 (const char *str)
{
	unsigned long long value = 0;
	if (str)
	{
		sscanf (str, "%llu", &value);
	}
	return value;
}
double atod (const char *str)
{
	if (!str || !*str) return 0.0; // ±‹√‚ø’÷∏’ÎªÚø’◊÷∑˚¥Æ
	double value = 0.0;
	if (sscanf (str, "%lg", &value) == 1)
	{
		return value;
	}
	return 0.0; // Ω‚Œˆ ß∞‹ ±∑µªÿ 0.0
}
int8_t atoi8 (const char *str)
{
	int value = 0;
	if (str) sscanf (str, "%d", &value);
	return (int8_t)value;
}
int16_t atoi16 (const char *str)
{
	int value = 0;
	if (str) sscanf (str, "%d", &value);
	return (int16_t)value;
}
int32_t atoi32 (const char *str)
{
	int32_t value = 0;
	if (str) sscanf (str, "%d", &value);
	return value;
}
uint8_t atoui8 (const char *str)
{
	unsigned value = 0;
	if (str) sscanf (str, "%u", &value);
	return (uint8_t)value;
}
uint16_t atoui16 (const char *str)
{
	unsigned value = 0;
	if (str) sscanf (str, "%u", &value);
	return (uint16_t)value;
}
uint32_t atoui32 (const char *str)
{
	uint32_t value = 0;
	if (str) sscanf (str, "%u", &value);
	return value;
}
int8_t _wtoi8 (const wchar_t *str)
{
	int value = 0;
	if (str) swscanf (str, L"%d", &value);
	return (int8_t)value;
}
int16_t _wtoi16 (const wchar_t *str)
{
	int value = 0;
	if (str) swscanf (str, L"%d", &value);
	return (int16_t)value;
}
int32_t _wtoi32 (const wchar_t *str)
{
	int32_t value = 0;
	if (str) swscanf (str, L"%d", &value);
	return value;
}
uint8_t _wtoui8 (const wchar_t *str)
{
	unsigned value = 0;
	if (str) swscanf (str, L"%u", &value);
	return (uint8_t)value;
}
uint16_t _wtoui16 (const wchar_t *str)
{
	unsigned value = 0;
	if (str) swscanf (str, L"%u", &value);
	return (uint16_t)value;
}
uint32_t _wtoui32 (const wchar_t *str)
{
	uint32_t value = 0;
	if (str) swscanf (str, L"%u", &value);
	return value;
}
int64_t atoi64 (const char *str)
{
	int64_t value = 0;
	if (str) sscanf (str, "%lld", &value);
	return value;
}

EXTERN_C int StringToIntA (const char *str) { return atoi (str); }
EXTERN_C int StringToIntW (const WCHAR *str) { return _wtoi (str); }
EXTERN_C unsigned StringToUnsignedA (const char *str) { return atou (str); }
EXTERN_C unsigned StringToUnsignedW (const WCHAR *str) { return _wtou (str); }
EXTERN_C bool StringToBoolA (const char *str)
{
	char buf [32] = {0};
	strcpy (buf, str);
	for (int cnt = 0; buf [cnt]; cnt ++) buf [cnt] = tolower (buf [cnt]);
	return !strcmp (buf, "true") ||
		!strcmp (buf, "yes") ||
		!strcmp (buf, "ok") ||
		!strcmp (buf, "sure") ||
		!strcmp (buf, "okay") ||
		!strcmp (buf, "zhen") ||
		!strcmp (buf, "’Ê");
}
EXTERN_C bool StringToBoolW (const WCHAR *str)
{
	WCHAR buf [32] = {0};
	lstrcpyW (buf, str);
	for (int cnt = 0; buf [cnt]; cnt ++) buf [cnt] = tolower (buf [cnt]);
	return !lstrcmpW (buf, L"true") ||
		!lstrcmpW (buf, L"yes") ||
		!lstrcmpW (buf, L"ok") ||
		!lstrcmpW (buf, L"sure") ||
		!lstrcmpW (buf, L"okay") ||
		!lstrcmpW (buf, L"zhen") ||
		!lstrcmpW (buf, L"’Ê");
}
EXTERN_C long StringToLongA (const char *str) { return atol (str); }
EXTERN_C long StringToLongW (const WCHAR *str) { return _wtol (str); }
EXTERN_C unsigned long StringToULongA (const char *str) { return atoul (str); }
EXTERN_C unsigned long StringToULongW (const WCHAR *str) { return _wtoul (str); }
EXTERN_C long long StringToLongLongA (const char *str) { return atoll (str); }
EXTERN_C long long StringToLongLongW (const WCHAR *str) { return _wtoll (str); }
EXTERN_C unsigned long long StringToULongLongA (const char *str) { return atou64 (str); }
EXTERN_C unsigned long long StringToULongLongW (const WCHAR *str) { return _wtou64 (str); }
EXTERN_C float StringToFloatA (const char *str) { return atof (str); }
EXTERN_C float StringToFloatW (const WCHAR *str) { return _wtof (str); }
EXTERN_C double StringToDoubleA (const char *str) { return atod (str); }
EXTERN_C double StringToDoubleW (const WCHAR *str) { return _wtod (str); }

#ifdef __cplusplus
int StringToInt (LPCSTR str) { return StringToIntA (str); }
int StringToInt (LPCWSTR str) { return StringToIntW (str); }
unsigned StringToUnsigned (LPCSTR str) { return StringToUnsignedA (str); }
unsigned StringToUnsigned (LPCWSTR str) { return StringToUnsignedW (str); }
bool StringToBool (LPCSTR str) { return StringToBoolA (str); }
bool StringToBool (LPCWSTR str) { return StringToBoolW (str); }
long StringToLong (LPCSTR str) { return StringToLongA (str); }
long StringToLong (LPCWSTR str) { return StringToLongW (str); }
unsigned long StringToULong (LPCSTR str) { return StringToULongA (str); }
unsigned long StringToULong (LPCWSTR str) { return StringToULongW (str); }
long long StringToLongLong (LPCSTR str) { return StringToLongLongA (str); }
long long StringToLongLong (LPCWSTR str) { return StringToLongLongW (str); }
unsigned long long StringToULongLong (LPCSTR str) { return StringToULongLongA (str); }
unsigned long long StringToULongLong (LPCWSTR str) { return StringToULongLongW (str); }
float StringToFloat (LPCSTR str) { return StringToFloatA (str); }
float StringToFloat (LPCWSTR str) { return StringToFloatW (str); }
double StringToDouble (LPCSTR str) { return StringToDoubleA (str); }
double StringToDouble (LPCWSTR str) { return StringToDoubleW (str); }
#endif

#if defined (__cplusplus) && defined (__cplusplus_cli)
using namespace System;
#define toInt(_String_Managed_Object_) Int32::Parse (_String_Managed_Object_)
#define objToInt(_Object_Managed_) Convert::ToInt32 (_Object_Managed_)
#define toDouble(_String_Managed_Object_) Double::Parse (_String_Managed_Object_)
#define objToDouble(_Object_Managed_) Convert::ToDouble (_Object_Managed_)
#define toBool(_String_Managed_Object_) Boolean::Parse (_String_Managed_Object_)
bool objToBool (Object ^result)
{
	if (!result) return false;
	try
	{
		String ^strValue = safe_cast <String ^> (result);
		return (strValue->ToLower () == "on"); 
	}
	catch (InvalidCastException ^)
	{
		try
		{
			return Convert::ToBoolean (result);
		}
		catch (InvalidCastException ^)
		{
			return false;
		}
	}
	return false;
}
#define toDateTime(_String_Managed_Object_) DateTime::Parse (_String_Managed_Object_)
#define toDateTimeObj(_Object_Managed_) Convert::ToDateTime (_Object_Managed_)
#define objectToType(_Object_Managed_, _Type_Name_) Convert::To##_Type_Name_ (_Object_Managed_)
#endif