#include <iostream>
#include <string>
#include <Windows.h>
#include <locale>
#include "../notice/notice.h"

int wmain (int argc, wchar_t **argv)
{
	setlocale (LC_ALL, "");
	std::wcout.imbue (std::locale ("", LC_CTYPE));
	if (argc < 4 && argc > 1)
	{
		std::wcout << L"Error: invalid args." << std::endl;
		return 1;
	}
	else if (argc <= 1)
	{
		std::wcout << L"Usage: " << std::endl <<
			L"\tshortcut.exe <lnk_file_path> <target_file_path> <app_id>" << std::endl;
		return 0;
	}
	auto res = CreateShortcutWithAppIdW (argv [1], argv [2], argv [3]);
	if (SUCCEEDED (res.hr))
	{
		std::wcout << L"Create successfully!" << std::endl;
		return 0;
	}
	else
	{
		std::wcout << L"Create failed. Reason: " << res.message << std::endl;
		return 1;
	}
	return 0;
}