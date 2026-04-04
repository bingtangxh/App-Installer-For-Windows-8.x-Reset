#pragma once
#include <vector>
#include <string>
#include <map>
#include <set>
#include "nstring.h"
#include "rctools.h"
#include "filepath.h"
#include "raii.h"
#include "resource.h"

// 允许忽视命令行前缀。如当命令行前缀为“-”"/"时，且开启此项，则输入“-args”与"/args"和"args" 是等效的
#define CMDARG_IGNOREPREFIXS 0b001
// 允许参数后跟着值，如“/args=file”，但要求必须配置后缀，且是有效的字符
#define CMDARG_ENABLEPARAMS 0b010
// 当 CMDARG_ENABLEPARAMS 开启时允许参数为空。如 “/args=file”和"/args"都是允许的
#define CMDARG_IGNOREPARAMS 0b100
// 命令行参数
struct cmdarg 
{
	std::vector <std::wstring> prefixs; // 允许无前缀（长度为 0）
	std::vector <std::wstring> commands; // 参数（长度不能为 0，不能为空）
	std::vector <std::wstring> postfixs; // 允许无后缀（长度为 0）
	std::wstring uniquelabel; // 唯一标识，参数名
	std::wstring description; // 描述，用于生成帮助文本
	DWORD flags; // 标志
};
#define CMDARG_PREFIXS_DEFAULT {L"/", L"-"}
#define CMDARG_POSTFIXS_DEFAULT {}
#define CMDARG_HELP {CMDARG_PREFIXS_DEFAULT, {L"?", L"help", L"h"}, CMDARG_PREFIXS_DEFAULT, L"help", GetRCStringSW (IDS_CMDPARAM_HELP), CMDARG_IGNOREPREFIXS}
std::vector <cmdarg> g_argslist = {
	CMDARG_HELP,
	{CMDARG_PREFIXS_DEFAULT, {L"silent", L"quiet", L"passive"}, CMDARG_POSTFIXS_DEFAULT, L"silent", GetRCStringSW (IDS_CMDPARAM_SILENT), CMDARG_IGNOREPREFIXS},
	{CMDARG_PREFIXS_DEFAULT, {L"verysilent", L"veryquiet"}, CMDARG_POSTFIXS_DEFAULT, L"verysilent", GetRCStringSW (IDS_CMDPARAM_VERYSILENT), CMDARG_IGNOREPREFIXS},
	{CMDARG_PREFIXS_DEFAULT, {L"multiple", L"filelist"}, {L"="}, L"multiple", GetRCStringSW (IDS_CMDPARAM_MULTIPLE), CMDARG_IGNOREPREFIXS | CMDARG_ENABLEPARAMS},
	{CMDARG_PREFIXS_DEFAULT, {L"language", L"lang"}, {L"="}, L"language", L"", CMDARG_IGNOREPREFIXS | CMDARG_ENABLEPARAMS}
};
bool IsFile (const std::wstring &path)
{
	return IsPathExists (path);
}
bool IsURI (const std::wstring &str)
{
	try
	{
		auto uristr = gcnew System::String (str.c_str ());
		auto uri = gcnew System::Uri (uristr);
		return uri != nullptr;
	}
	catch (...) { return false; }
}
std::wstring TrimQuotes (const std::wstring& str) {
	if (str.empty ()) return str;
	size_t len = str.size ();
	size_t start = (str.front () == L'"') ? 1 : 0; 
	size_t end = len; 
	if (str.back () == L'"') 
	{
		if (end > start) end = len - 1;
	}
	return str.substr (start, end - start);
}
enum class paramtype
{
	string,
	file,
	uri
};
struct cmdkey
{
	std::wnstring key; // 键
	paramtype type; // 键类型，当传入的参数是存在的文件路径时（如“C:\1.txt”）则键的类型为文件。同理也可设置为 uri
	operator std::wnstring () { return key; }
	operator LPCWSTR () { return key.c_str (); }
	cmdkey (const std::wstring &k): key (k)
	{
		if (IsFile (k)) type = paramtype::file;
		else if (IsURI (k)) type = paramtype::uri;
		else type = paramtype::string;
	}
	cmdkey (const std::wstring &k, paramtype pt): key (k), type (pt) {}
	bool operator == (const cmdkey &r) const { return key == r.key; }
	bool operator > (const cmdkey &r) const { return key > r.key; }
	bool operator < (const cmdkey &r) const { return key < r.key; }
};
struct cmdvalue
{
	std::wstring value; // 值
	paramtype type;
	bool isnull; // 当参数不支持跟着值时，或允许时值为空时设置为真。
};
// 对于从命令行获取到的 argv 和 argc，argc 最小为 1，argv 的第一个元素指向程序。所以 startpos 为 1。
void ParseCmdArgs (LPWSTR *argv, DWORD argc, std::map <cmdkey, cmdvalue> &parseresult, DWORD startpos = 1)
{
	for (size_t i = startpos; i < argc; i ++)
	{
		std::wnstring arg = argv [i];
		arg = arg.trim ();
		if (IsFile (arg)) parseresult [cmdkey (arg, paramtype::file)] = cmdvalue {L"", paramtype::file, true};
		else if (IsURI (arg)) parseresult [cmdkey (arg, paramtype::uri)] = cmdvalue {L"", paramtype::uri, true};
		else if (IsFile (TrimQuotes (arg))) parseresult [cmdkey (TrimQuotes (arg), paramtype::file)] = cmdvalue { L"", paramtype::file, true };
		else 
		{
			for (auto &it : g_argslist)
			{
				std::set <std::wnstring> prefixs;
				for (auto &it_s : it.prefixs) prefixs.insert (it_s);
				int cmdhead = -1;
				for (auto &it_s : prefixs)
				{
					auto plen = GetNormalizeStringLength (it_s);
					auto parg = GetStringLeft (arg, plen);
					if (it_s == parg) { cmdhead = plen; break; }
				}
				if (((it.flags & CMDARG_IGNOREPREFIXS) || !it.prefixs.size () || !prefixs.size ()) && cmdhead < 0)
				{
					if (!arg.length ()) continue;
					cmdhead = 0;
				}
				if (cmdhead < 0) continue;
				int postfixhead = -1;
				std::set <std::wnstring> commands;
				for (auto &it_s : it.commands) if (GetNormalizeStringLength (it_s)) commands.insert (it_s);
				if (commands.empty ()) continue;
				for (auto &it_s : commands)
				{
					auto clen = it_s.length ();
					auto carg = GetStringLeft (arg.substr (cmdhead), clen);
					if (it_s == carg) { postfixhead = cmdhead + clen; }
				}
				if (postfixhead < 0) continue;
				std::set <std::wnstring> postfixs;
				for (auto &it_s : it.postfixs) if (GetNormalizeStringLength (it_s)) postfixs.insert (it_s);
				if (!(it.flags & CMDARG_ENABLEPARAMS)) it.postfixs.clear ();
				if ((it.flags & CMDARG_ENABLEPARAMS) && postfixs.size ())
				{
					int valuehead = -1;
					auto rightpart = StringTrim (GetStringRight (arg, lstrlenW (arg.c_str ()) - postfixhead));
					if (it.flags & CMDARG_IGNOREPARAMS)
					{
						if (!rightpart.length ()) { parseresult [cmdkey (it.uniquelabel, paramtype::string)] = cmdvalue {L"", paramtype::string, true}; break; }
					}
					for (auto &it_s : postfixs)
					{
						auto plen = it_s.length ();
						auto parg = GetStringLeft (rightpart, plen);
						if (it_s == parg) { valuehead = plen; break; }
					}
					if (valuehead < 0) continue;
					else
					{
						auto value = rightpart.substr (valuehead);
						paramtype ptype = paramtype::string;
						if (IsFile (value)) ptype = paramtype::file;
						else if (IsFile (TrimQuotes (value)))
						{
							value = TrimQuotes (value);
							ptype = paramtype::file;
						}
						else if (IsURI (StringTrim (value))) ptype = paramtype::uri;
						parseresult [cmdkey (it.uniquelabel, paramtype::string)] = cmdvalue {value, ptype, false};
					}
					break;
				}
				else
				{
					parseresult [cmdkey (it.uniquelabel, paramtype::string)] = cmdvalue {L"", paramtype::string, true};
					break;
				}
			}
		}
	}
}
void ParseCmdLine (LPCWSTR lpCommandLine, std::map <cmdkey, cmdvalue> &parseresult)
{
	int argc = 0;
	LPWSTR *alpstr = CommandLineToArgvW (lpCommandLine, &argc);
	destruct relt ([&alpstr] () {
		if (alpstr) LocalFree (alpstr);
	});
	ParseCmdArgs (alpstr, argc, parseresult, 0);
}