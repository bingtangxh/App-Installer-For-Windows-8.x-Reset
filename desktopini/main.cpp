#define _CRT_SECURE_NO_WARNINGS
#define HMODULE_MODE_EXE
#include <iostream>
#include <iomanip>
#include <string>
#include <Windows.h>
#include "module.h"
#include "filepath.h"
#include "initfile.h"
#include <fstream>

std::wstring GetWin32ErrorMessage (DWORD err)
{
	if (err == 0) return L"";
	LPWSTR buffer = nullptr;
	FormatMessageW (
		FORMAT_MESSAGE_ALLOCATE_BUFFER |
		FORMAT_MESSAGE_FROM_SYSTEM |
		FORMAT_MESSAGE_IGNORE_INSERTS,
		nullptr,
		err,
		MAKELANGID (LANG_NEUTRAL, SUBLANG_DEFAULT),
		(LPWSTR)&buffer,
		0,
		nullptr
	);
	std::wstring msg = buffer ? buffer : L"Unknown error";
	if (buffer) LocalFree (buffer);
	return msg;
}

int wmain (int argc, wchar_t** argv)
{
	if (argc <= 1)
	{
		std::cout <<
			"Usage:\n"
			"  desktopini.exe <filepath | dirpath> <section> <key> <value>\n\n"
			"<filepath>: The tool does not modify the file itself. It writes\n"
			"information to the desktop.ini file located in the same directory.\n";
		return 0;
	}
	else if (argc < 5)
	{
		std::cout << "ERROR: Invalid arguments.\n";
		return 1;
	}

	std::wstring desktopini;
	if (IsFileExists (argv [1]))
	{
		desktopini = CombinePath (GetFileDirectoryW (argv [1]), L"desktop.ini");
		std::wcout << L"INFO: Input is a file. Using directory desktop.ini:\n  "
			<< desktopini << L"\n";
	}
	else
	{
		desktopini = CombinePath (argv [1], L"desktop.ini");
		std::wcout << L"INFO: Input is a directory. Using desktop.ini:\n  "
			<< desktopini << L"\n";
	}

	std::wstring parent = GetFileDirectoryW (desktopini);

	// Write INI
	if (!IsFileExists (desktopini))
	{
		std::wfstream wfs (desktopini, std::ios::out);
		wfs << L"";
		wfs.close ();
	}
	initfile dini (desktopini);
	auto res = dini [argv [2]] [argv [3]].write (argv [4]);
	if (res)
	{
		std::wcout << L"OK: Wrote INI entry ["
			<< argv [2] << L"] "
			<< argv [3] << L" = "
			<< argv [4] << L"\n";
	}
	else
	{
		std::wcout << L"ERROR: Failed to write to desktop.ini.\n";
		return 2;
	}

// desktop.ini attributes
	DWORD iniattr = GetFileAttributesW (desktopini.c_str ());
	if (iniattr == INVALID_FILE_ATTRIBUTES)
	{
		DWORD err = GetLastError ();
		std::wcout << L"WARNING: Cannot query desktop.ini attributes.\n"
			<< L"         Win32 Error "
			<< err << L": "
			<< GetWin32ErrorMessage (err) << L"\n";
		iniattr = FILE_ATTRIBUTE_ARCHIVE;
	}

	if (!(iniattr & (FILE_ATTRIBUTE_HIDDEN | FILE_ATTRIBUTE_SYSTEM)))
	{
		if (SetFileAttributesW (
			desktopini.c_str (),
			iniattr | FILE_ATTRIBUTE_HIDDEN | FILE_ATTRIBUTE_SYSTEM))
		{
			std::wcout << L"OK: desktop.ini attributes set to HIDDEN | SYSTEM.\n";
		}
		else
		{
			DWORD err = GetLastError ();
			std::wcout << L"ERROR: Failed to set attributes on desktop.ini.\n"
				<< L"       Path: " << desktopini << L"\n"
				<< L"       Win32 Error "
				<< err << L": "
				<< GetWin32ErrorMessage (err) << L"\n";
			return 3;
		}
	}
	else
	{
		std::wcout << L"INFO: desktop.ini already has required attributes.\n";
	}

	// parent directory attributes
	DWORD parentattr = GetFileAttributesW (parent.c_str ());
	if (parentattr == INVALID_FILE_ATTRIBUTES)
	{
		DWORD err = GetLastError ();
		std::wcout << L"ERROR: Cannot query parent directory attributes.\n"
			<< L"       Path: " << parent << L"\n"
			<< L"       Win32 Error "
			<< err << L": "
			<< GetWin32ErrorMessage (err) << L"\n";
		return 4;
	}
	if (!(parentattr & FILE_ATTRIBUTE_SYSTEM))
	{
		if (SetFileAttributesW (parent.c_str (), parentattr | FILE_ATTRIBUTE_SYSTEM))
		{
			std::wcout << L"OK: Parent directory marked as SYSTEM.\n";
		}
		else
		{
			DWORD err = GetLastError ();
			std::wcout << L"ERROR: Failed to set SYSTEM attribute on directory.\n"
				<< L"       Path: " << parent << L"\n"
				<< L"       Win32 Error "
				<< err << L": "
				<< GetWin32ErrorMessage (err) << L"\n";
			return 5;
		}
	}
	else
	{
		std::wcout << L"INFO: Parent directory is already SYSTEM.\n";
	}
	std::wcout << L"\nSUCCESS: desktop.ini configuration completed successfully.\n";
	return 0;
}
