#ifdef _M_ARM
#define _ARM_WINAPI_PARTITION_DESKTOP_SDK_AVAILABLE 1
#endif

#include <Windows.h>
#include <set>
#include <msclr/marshal_cppstd.h>
#include <ShObjIdl.h>
#include <ShlObj.h>       // KNOWNFOLDERID, SHGetKnownFolderPath
#include <commdlg.h>      // OPENFILENAME
#include <comdef.h>       // _com_error
#include <winhttp.h>      // WinHTTP
#include <MsHTML.h>
#include <ExDisp.h>
#include <atlbase.h>
#include <fstream>
#include <comdef.h>
#include <vcclr.h>
#include <map>
#include <commdlg.h>
#include <rapidjson\document.h>
#include <rapidjson\writer.h>
#include <rapidjson\stringbuffer.h>
#include <tlhelp32.h>
#include <Psapi.h>
#include <cliext/vector>
#include <cliext/utility>
#include "module.h"
#include "themeinfo.h"
#include "mpstr.h"
#include "initfile.h"
#include "vemani.h"
#include "ieshell.h"
#include "localeex.h"
#include "download.h"
#include "bridge.h"
#include "rctools.h"
#include "nstring.h"
#include "resmap.h"
#include "nstring.h"
#include "raii.h"

using namespace System;
using namespace System::Runtime::InteropServices;
using namespace System::IO::Compression;
using namespace System::Text;

#ifdef _DEBUG
#define DEBUGMODE true
#else
#define DEBUGMODE false
#endif
#define JS_SAFE [MarshalAs (UnmanagedType::SafeArray, SafeArraySubType = VarEnum::VT_VARIANT)]
struct iconhandle
{
	HICON hIcon = nullptr;
	iconhandle (HICON hIcon = nullptr): hIcon (hIcon) {}
	~iconhandle () { try { if (hIcon) DestroyIcon (hIcon); hIcon = nullptr; } catch (...) {} }
};

LPCWSTR g_lpAppId = L"WindowsModern.PracticalToolsProject!Settings";
LPCWSTR g_idInVe = L"Settings";
LPCWSTR g_wndclass = L"Win32_WebUI_WindowsModern_Settings";
iconhandle g_hIconMain (LoadRCIcon (IDI_ICON_MAIN));
initfile g_initfile (CombinePath (GetProgramRootDirectoryW (), L"config.ini"));
vemanifest g_vemani (
	IsFileExists (CombinePath (GetProgramRootDirectoryW (), L"VisualElementsManifest.xml")) ?
	CombinePath (GetProgramRootDirectoryW (), L"VisualElementsManifest.xml") :
	CombinePath (GetProgramRootDirectoryW (), L"Settings.VisualElementsManifest.xml")
);
resxmldoc g_scaleres (
	IsFileExists (CombinePath (GetProgramRootDirectoryW (), L"VisualElements\\scale.xml")) ?
	CombinePath (GetProgramRootDirectoryW (), L"VisualElements\\scale.xml") :
	CombinePath (GetProgramRootDirectoryW (), L"VisualElementsManifest.xml")
);
ref class MainHtmlWnd;
msclr::gcroot <MainHtmlWnd ^> g_mainwnd;
std::wstring g_lastfile;
inline std::wstring ToStdWString (const std::wstring &str) { return str; }
std::string GetSuitableLanguageValue (const std::map <std::nstring, std::string> &map, const std::nstring &localename)
{
	for (auto &it : map) if (it.first == localename) return it.second;
	for (auto &it : map) if (LocaleNameCompare (pugi::as_wide (it.first), pugi::as_wide (localename))) return it.second;
	for (auto &it : map) if (IsNormalizeStringEquals (GetLocaleRestrictedCodeA (it.first), GetLocaleRestrictedCodeA (localename))) return it.second;
	for (auto &it : map) if (LocaleNameCompare (pugi::as_wide (GetLocaleRestrictedCodeA (it.first)), pugi::as_wide (GetLocaleRestrictedCodeA (localename)))) return it.second;
	return "";
}
std::string GetSuitableLanguageValue (const std::map <std::nstring, std::string> &map)
{
	if (map.empty ()) return "";
	std::string ret = GetSuitableLanguageValue (map, pugi::as_utf8 (GetComputerLocaleCodeW ()));
	if (ret.empty ()) ret = GetSuitableLanguageValue (map, "en-US");
	if (ret.empty ()) ret = map.begin ()->second;
	return ret;
}
struct xmlstrres
{
	pugi::xml_document doc;
	bool isvalid = false;
	void destroy ()
	{
		if (isvalid) doc.reset ();
		isvalid = false;
	}
	bool create (const std::wstring &filepath)
	{
		destroy ();
		auto res = doc.load_file (filepath.c_str ());
		return isvalid = res;
	}
	xmlstrres (const std::wstring &filepath) { create (filepath); }
	~xmlstrres () { destroy (); }
	std::string get (const std::string &id) const
	{
		auto root = doc.first_child ();
		auto nodes = root.children ();
		for (auto &it : nodes)
		{
			if (IsNormalizeStringEquals (std::string (it.attribute ("id").as_string ()), id))
			{
				auto strings = it.children ();
				std::map <std::nstring, std::string> lang_value;
				for (auto &sub : strings)
				{
					std::nstring lang = sub.attribute ("name").as_string ();
					if (!lang.empty ()) lang_value [lang] = sub.text ().get ();
				}
				return GetSuitableLanguageValue (lang_value);
			}
		}
		return "";
	}
	std::wstring get (const std::wstring &id) const { return pugi::as_wide (get (pugi::as_utf8 (id))); }
	std::wstring operator [] (const std::wstring &id) const { return get (id); }
	std::wstring operator [] (const std::wstring &id) { return get (id); }
	std::string operator [] (const std::string &id) const { return get (id); }
	std::string operator [] (const std::string &id) { return get (id); }
};
xmlstrres g_winjspri (CombinePath (GetProgramRootDirectoryW (), L"locale\\resources.xml"));
std::vector <std::wstring> g_cmdargs;

size_t ExploreFile (HWND hParent, std::vector <std::wstring> &results, LPWSTR lpFilter = L"Windows Store App Package (*.appx; *.appxbundle)\0*.appx;*.appxbundle", DWORD dwFlags = OFN_EXPLORER | OFN_ALLOWMULTISELECT | OFN_PATHMUSTEXIST, const std::wstring &swWndTitle = std::wstring (L"Please select the file(-s): "), const std::wstring &swInitDir = GetFileDirectoryW (g_lastfile))
{
	results.clear ();
	const DWORD BUFFER_SIZE = 65536; // 64KB
	std::vector <WCHAR> buffer (BUFFER_SIZE, 0);
	OPENFILENAME ofn;
	ZeroMemory (&ofn, sizeof (ofn));
	ofn.hwndOwner = hParent;
	ofn.lpstrFile = (LPWSTR)buffer.data ();
	ofn.nMaxFile = BUFFER_SIZE;
	ofn.lpstrFilter = lpFilter;
	ofn.nFilterIndex = 1;
	ofn.lpstrTitle = swWndTitle.c_str ();
	ofn.Flags = dwFlags;
	ofn.lpstrInitialDir = swInitDir.c_str ();
	ofn.lStructSize = sizeof (ofn);
	if (GetOpenFileNameW (&ofn))
	{
		LPCWSTR p = buffer.data ();
		std::wstring dir = p;
		p += dir.length () + 1;
		if (*p == 0) results.push_back (dir);
		else
		{
			while (*p)
			{
				std::wstring fullPath = dir + L"\\" + p;
				results.push_back (fullPath);
				p += wcslen (p) + 1;
			}
		}
		if (!results.empty ()) g_lastfile = results.back ();
	}
	return results.size ();
}
HRESULT GetWebBrowser2Interface (System::Windows::Forms::WebBrowser ^fwb, IWebBrowser2 **output)
{
	if (fwb == nullptr || output == nullptr) return E_INVALIDARG;
	*output = nullptr;
	Object ^activeX = fwb->ActiveXInstance;
	if (activeX == nullptr) return E_FAIL;
	IntPtr pUnk = Marshal::GetIUnknownForObject (activeX);
	if (pUnk == IntPtr::Zero) return E_FAIL;
	HRESULT hr = ((IUnknown *)pUnk.ToPointer ())->QueryInterface (IID_IWebBrowser2, (void **)output);
	Marshal::Release (pUnk);
	return hr;
}
bool IsWindows10 ()
{
#pragma warning(push)
#pragma warning(disable:4996)
	OSVERSIONINFOEX osvi = {0};
	osvi.dwOSVersionInfoSize = sizeof (OSVERSIONINFOEX);
	osvi.dwMajorVersion = 10;
	DWORDLONG conditionMask = 0;
	VER_SET_CONDITION (conditionMask, VER_MAJORVERSION, VER_GREATER_EQUAL);
	if (VerifyVersionInfoW (&osvi, VER_MAJORVERSION, conditionMask)) return TRUE;
	DWORD error = GetLastError ();
	return (error == ERROR_OLD_WIN_VERSION) ? FALSE : FALSE;
#pragma warning(pop)
}
[ComVisible (true)]
public ref class _I_InitConfig
{
	public:
	Win32::InitConfig ^Create (String ^filepath) { return gcnew Win32::InitConfig (filepath); }
	Win32::InitConfig ^GetConfig () { return Create (CStringToMPString (g_initfile.filepath)); }
};
[ComVisible (true)]
public ref class _I_VisualElement
{
	protected:
	String ^appid;
	public:
	_I_VisualElement (String ^appid): appid (appid) {}
	_I_VisualElement (): appid (String::Empty) {}
	property String ^Id { String ^get () { return appid; } void set (String ^value) { appid = value; } }
#define PROPERTY_VELEMENT(_PropertyName_, _MethodName_) \
	property String ^_PropertyName_ { \
		String ^get() { \
			return CStringToMPString(g_vemani._MethodName_(MPStringToStdW(appid))); \
		} \
	}
	PROPERTY_VELEMENT (DisplayName, display_name)
		PROPERTY_VELEMENT (Logo, logo)
		PROPERTY_VELEMENT (SmallLogo, small_logo)
		property String ^ForegroundText { String ^get () { return g_vemani.foreground_text (MPStringToStdW (appid)) == vemanifest::TextColor::light ? "light" : "dark"; }}
		PROPERTY_VELEMENT (Lnk32x32Logo, lnk_32x32_logo)
		PROPERTY_VELEMENT (ItemDisplayLogo, item_display_logo)
		property bool ShowNameOnTile { bool get () { return g_vemani.show_name_on_tile (MPStringToStdW (appid)); }}
		PROPERTY_VELEMENT (BackgroundColor, background_color)
		PROPERTY_VELEMENT (SplashScreenImage, splash_screen_image)
		PROPERTY_VELEMENT (SplashScreenBackgroundColor, splash_screen_backgroundcolor)
		PROPERTY_VELEMENT (SplashScreenBackgroundColorDarkMode, splash_screen_backgroundcolor_darkmode)
		#ifdef PROPERTY_VELEMENT
		#undef PROPERTY_VELEMENT
		#endif
		Object ^Get (String ^propertyName)
		{
			String ^str = propertyName->ToLower ()->Trim ();
			if (str == "displayname") return DisplayName;
			else if (str == "logo") return Logo;
			else if (str == "smalllogo") return SmallLogo;
			else if (str == "foregroundtext") return ForegroundText;
			else if (str == "lnk32x32logo") return Lnk32x32Logo;
			else if (str == "shownameontile") return ShowNameOnTile;
			else if (str == "backgroundcolor") return BackgroundColor;
			else if (str == "splashscreenimage") return SplashScreenImage;
			else if (str == "splashscreenbackgroundcolor") return SplashScreenBackgroundColor;
			else if (str == "splashscreenbackgroundcolordarkmode") return SplashScreenBackgroundColorDarkMode;
			return String::Empty;
		}
		Object ^operator [] (String ^propertyName) { return Get (propertyName); }
};
[ComVisible (true)]
public ref class _I_VisualElements
{
	public:
	array <String ^> ^GetIds ()
	{
		std::vector <std::wstring> res;
		g_vemani.app_ids (res);
		auto ret = gcnew array <String ^> (res.size ());
		for (size_t i = 0; i < res.size (); i ++) ret [i] = CStringToMPString (res [i]);
		return ret;
	}
	String ^GetIdsToJson () { return StringArrayToJson (GetIds ()); }
	_I_VisualElement ^Get (String ^id) { return gcnew _I_VisualElement (id); }
	_I_VisualElement ^operator [] (String ^id) { return Get (id); }
#define ATTRIBUTE_METHODS(_FunctionName_, _MethodName_) \
String^ _FunctionName_(String^ appid) { \
    return CStringToMPString(g_vemani._MethodName_(MPStringToStdW(appid))); \
}
	ATTRIBUTE_METHODS (DisplayName, display_name)
	ATTRIBUTE_METHODS (Logo, logo)
	ATTRIBUTE_METHODS (SmallLogo, small_logo)
		String ^ForegroundText (String ^appid)
	{
		return g_vemani.foreground_text (MPStringToStdW (appid)) == vemanifest::TextColor::light ? "light" : "dark";
	}
	ATTRIBUTE_METHODS (Lnk32x32Logo, lnk_32x32_logo)
	ATTRIBUTE_METHODS (ItemDisplayLogo, item_display_logo)
		bool ShowNameOnTile (String ^appid) { return g_vemani.show_name_on_tile (MPStringToStdW (appid)); }
	ATTRIBUTE_METHODS (BackgroundColor, background_color)
	ATTRIBUTE_METHODS (SplashScreenImage, splash_screen_image)
	ATTRIBUTE_METHODS (SplashScreenBackgroundColor, splash_screen_backgroundcolor)
	ATTRIBUTE_METHODS (SplashScreenBackgroundColorDarkMode, splash_screen_backgroundcolor_darkmode)
	#ifdef ATTRIBUTE_METHODS
	#undef ATTRIBUTE_METHODS
	#endif
		Object ^GetValue (String ^appid, String ^attributeName)
	{
		auto attr = attributeName->ToLower ()->Trim ();
		if (attr == "displayname") return DisplayName (appid);
		else if (attr == "logo") return Logo (appid);
		else if (attr == "smalllogo") return SmallLogo (appid);
		else if (attr == "foregroundtext") return ForegroundText (appid);
		else if (attr == "lnk32x32logo") return Lnk32x32Logo (appid);
		else if (attr == "itemdisplaylogo") return ItemDisplayLogo (appid);
		else if (attr == "shownameontile") return ShowNameOnTile (appid);
		else if (attr == "backgroundcolor") return BackgroundColor (appid);
		else if (attr == "splashscreenimage") return SplashScreenImage (appid);
		else if (attr == "splashscreenbackgroundcolor") return SplashScreenBackgroundColor (appid);
		else if (attr == "splashscreenbackgroundcolordarkmode") return SplashScreenBackgroundColorDarkMode (appid);
		else return String::Empty;
	}
};
[ComVisible (true)]
public ref class _I_Bridge_Base
{
	protected:
	_I_String ^str = gcnew _I_String ();
	_I_InitConfig ^initconfig = gcnew _I_InitConfig ();
	_I_Storage ^storage = gcnew _I_Storage ();
	public:
	property _I_String ^String { _I_String ^get () { return str; }}
	property _I_InitConfig ^Config { _I_InitConfig ^get () { return initconfig; }}
	property _I_Storage ^Storage { _I_Storage ^get () { return storage; }}
};
[ComVisible (true)]
public interface class IScriptBridge
{
	public:
	virtual Object ^CallEvent (String ^funcName, Object ^e) = 0;
};
[ComVisible (true)]
public ref class _I_Window
{
	private:
	IScriptBridge ^wndinst = nullptr;
	public:
	_I_Window (IScriptBridge ^wnd): wndinst (wnd) {}
	Object ^CallEvent (String ^name, ... array <Object ^> ^args) { return wndinst->CallEvent (name, args [0]); }
};
[ComVisible (true)]
public ref class _I_UI
{
	private:
	System::Windows::Forms::Form ^wndinst = nullptr;
	public:
	ref struct _I_UI_Size
	{
		private:
		int m_width = 0;
		int m_height = 0;
		public:
		property int width { int get () { return m_width; } }
		property int height { int get () { return m_height; }}
		property int Width { int get () { return m_width; } }
		property int Height { int get () { return m_height; }}
		int getWidth () { return m_width; }
		int getHeight () { return m_height; }
		_I_UI_Size (int w, int h): m_width (w), m_height (h) {}
	};
	_I_UI (System::Windows::Forms::Form ^wnd): wndinst (wnd) {}
	property int DPIPercent { int get () { return GetDPI (); }}
	property double DPI { double get () { return DPIPercent * 0.01; }}
	property _I_UI_Size ^WndSize { _I_UI_Size ^get () { return gcnew _I_UI_Size (wndinst->Width, wndinst->Height); } }
	property _I_UI_Size ^ClientSize { _I_UI_Size ^get () { auto cs = wndinst->ClientSize; return gcnew _I_UI_Size (cs.Width, cs.Height); } }
	property String ^ThemeColor { String ^get () { return ColorToHtml (GetDwmThemeColor ()); } }
	property bool DarkMode { bool get () { return IsAppInDarkMode (); }}
	property String ^HighContrast
	{
		String ^get ()
		{
			auto highc = GetHighContrastTheme ();
			switch (highc)
			{
				case HighContrastTheme::None: return "none";
					break;
				case HighContrastTheme::Black: return "black";
					break;
				case HighContrastTheme::White: return "white";
					break;
				case HighContrastTheme::Other: return "high";
					break;
				default: return "none";
					break;
			}
			return "none";
		}
	}
};
[ComVisible (true)]
public ref class _I_Locale
{
	public:
	property String ^CurrentLocale { String ^get () { return CStringToMPString (GetComputerLocaleCodeW ()); } }
	property LCID CurrentLCID { LCID get () { return LocaleCodeToLcid (GetComputerLocaleCodeW ()); } }
	String ^ToLocaleName (LCID lcid) { return CStringToMPString (LcidToLocaleCodeW (lcid)); }
	LCID ToLCID (String ^localename) { return LocaleCodeToLcidW (MPStringToStdW (localename)); }
	Object ^LocaleInfo (LCID lcid, LCTYPE lctype) { return CStringToMPString (GetLocaleInfoW (lcid, lctype)); }
	Object ^LocaleInfoEx (String ^localeName, LCTYPE lctype)
	{
		std::wstring output = L"";
		int ret = GetLocaleInfoEx (MPStringToStdW (localeName), lctype, output);
		if (output.empty ()) return ret;
		else return CStringToMPString (output);
	}
};
[ComVisible (true)]
public ref class _I_System
{
	private:
	_I_Resources ^ires = gcnew _I_Resources ();
	_I_Locale ^ilocale = gcnew _I_Locale ();
	public:
	property _I_Resources ^Resources { _I_Resources ^get () { return ires; } }
	property _I_Locale ^Locale { _I_Locale ^get () { return ilocale; } }
	property bool IsWindows10
	{
		bool get ()
		{
			OSVERSIONINFOEX osvi = {0};
			osvi.dwOSVersionInfoSize = sizeof (OSVERSIONINFOEX);
			osvi.dwMajorVersion = 10;
			DWORDLONG conditionMask = 0;
			VER_SET_CONDITION (conditionMask, VER_MAJORVERSION, VER_GREATER_EQUAL);
			if (VerifyVersionInfoW (&osvi, VER_MAJORVERSION, conditionMask)) return TRUE;
			DWORD error = GetLastError ();
			return (error == ERROR_OLD_WIN_VERSION) ? FALSE : FALSE;
		}
	}
};
public ref class _I_System2: public _I_System
{
	protected:
	_I_UI ^ui;
	public:
	_I_System2 (System::Windows::Forms::Form ^wnd)
	{
		ui = gcnew _I_UI (wnd);
	}
	property _I_UI ^UI { _I_UI ^get () { return ui; } }
};
[ComVisible (true)]
public ref class _I_Bridge_Base2: public _I_Bridge_Base
{
	protected:
	_I_Window ^window;
	public:
	_I_Bridge_Base2 (IScriptBridge ^iscr)
	{
		window = gcnew _I_Window (iscr);
	}
	property _I_Window ^Window { _I_Window ^get () { return window; }}
};
[ComVisible (true)]
public ref class _I_Bridge_Base3: public _I_Bridge_Base2
{
	protected:
	_I_System2 ^system;
	public:
	_I_Bridge_Base3 (IScriptBridge ^iscr, System::Windows::Forms::Form ^form): _I_Bridge_Base2 (iscr)
	{
		system = gcnew _I_System2 (form);
	}
	property _I_System2 ^System { _I_System2 ^get () { return system; }}
};
[ComVisible (true)]
public ref class _I_IEFrame_Base
{
	public:
	property int Version { int get () { return GetInternetExplorerVersionMajor (); }}
	String ^ParseHtmlColor (String ^color)
	{
		auto dcolor = Drawing::ColorTranslator::FromHtml (color);
		{
			rapidjson::Document doc;
			doc.SetObject ();
			auto &alloc = doc.GetAllocator ();
			doc.AddMember ("r", (uint16_t)dcolor.R, alloc);
			doc.AddMember ("g", (uint16_t)dcolor.G, alloc);
			doc.AddMember ("b", (uint16_t)dcolor.B, alloc);
			doc.AddMember ("a", (uint16_t)dcolor.A, alloc);
			rapidjson::StringBuffer buffer;
			rapidjson::Writer <rapidjson::StringBuffer> writer (buffer);
			doc.Accept (writer);
			std::string utf8 = buffer.GetString ();
			std::wstring_convert <std::codecvt_utf8 <wchar_t>> conv;
			return CStringToMPString (conv.from_bytes (utf8));
		}
		return "{}";
	}
};
int ExecuteProgram (
	const std::wstring &cmdline,
	const std::wstring &file,
	int wndshowmode,
	bool wait,
	const std::wstring &execdir = L"")
{
	STARTUPINFOW si;
	PROCESS_INFORMATION pi;
	ZeroMemory (&si, sizeof (si));
	ZeroMemory (&pi, sizeof (pi));
	si.cb = sizeof (si);
	si.dwFlags = STARTF_USESHOWWINDOW;
	si.wShowWindow = static_cast <WORD> (wndshowmode);
	std::vector <WCHAR> buf (cmdline.capacity () + 1);
	wcscpy (buf.data (), cmdline.c_str ());
	LPCWSTR workdir = IsNormalizeStringEmpty (execdir) ? NULL : execdir.c_str ();
	BOOL ok = CreateProcessW (
		IsNormalizeStringEmpty (file) ? NULL : file.c_str (),   // 应用程序路径
		IsNormalizeStringEmpty (cmdline) ? NULL : buf.data (),         // 命令行必须可写
		NULL,                                 // 进程安全属性
		NULL,                                 // 线程安全属性
		FALSE,                                // 不继承句柄
		0,                                    // 创建标志
		NULL,                                 // 使用父进程环境变量
		workdir,                              // 工作目录
		&si,
		&pi
	);
	if (!ok) return static_cast <int> (GetLastError ());
	if (wait) WaitForSingleObject (pi.hProcess, INFINITE);
	CloseHandle (pi.hThread);
	CloseHandle (pi.hProcess);
	return 0;
}
bool KillProcessByFilePath (
	const std::wstring &filepath,
	bool multiple = false,
	bool isonlyname = false
)
{
	if (filepath.empty ()) return false;
	std::wstring targetPath = filepath;
	std::wstring targetName;
	if (isonlyname)
	{
		size_t pos = filepath.find_last_of (L"\\/");
		if (pos != std::wstring::npos) targetName = filepath.substr (pos + 1);
		else targetName = filepath;  // 直接是文件名
	}
	HANDLE hSnap = CreateToolhelp32Snapshot (TH32CS_SNAPPROCESS, 0);
	if (hSnap == INVALID_HANDLE_VALUE) return false;
	PROCESSENTRY32W pe;
	pe.dwSize = sizeof (pe);
	bool killed = false;
	if (Process32FirstW (hSnap, &pe))
	{
		do
		{
			bool match = false;
			if (isonlyname)
			{
				if (PathEquals (pe.szExeFile, targetName.c_str ())) match = true;
			}
			else
			{
				// 比较完整路径，需要 QueryFullProcessImageNameW
				HANDLE hProc = OpenProcess (PROCESS_QUERY_INFORMATION | PROCESS_TERMINATE, FALSE, pe.th32ProcessID);
				if (hProc)
				{
					wchar_t exePath [MAX_PATH] = {0};
					DWORD sz = MAX_PATH;

					if (QueryFullProcessImageNameW (hProc, 0, exePath, &sz))
					{
						if (_wcsicmp (exePath, targetPath.c_str ()) == 0)
							match = true;
					}
					CloseHandle (hProc);
				}
			}
			if (match)
			{
				HANDLE hProc = OpenProcess (PROCESS_TERMINATE, FALSE, pe.th32ProcessID);
				if (hProc)
				{
					TerminateProcess (hProc, 1);
					CloseHandle (hProc);
					killed = true;
				}
				if (!multiple) break;
			}

		} while (Process32NextW (hSnap, &pe));
	}
	CloseHandle (hSnap);
	return killed;
}
static std::wstring QueryVersionString (const BYTE *data, const std::wstring &langCode,
	const wchar_t *key)
{
	wchar_t query [256] = {0};
	wsprintfW (query, L"\\StringFileInfo\\%s\\%s", langCode.c_str (), key);
	LPWSTR value = nullptr;
	UINT len = 0;
	if (VerQueryValueW (data, query, (LPVOID *)&value, &len) && value) 
		return std::wstring (value ? value : L"", len);
	return L"";
}
rapidjson::Document GetFileVersionAsJson (const std::wstring &filePath)
{
	rapidjson::Document doc;
	doc.SetObject ();
	auto &alloc = doc.GetAllocator ();
	DWORD dummy = 0;
	DWORD size = GetFileVersionInfoSizeW (filePath.c_str (), &dummy);
	if (size == 0)
	{
		doc.AddMember ("error", "No version info", alloc);
		return doc;
	}
	std::vector <BYTE> data (size);
	if (!GetFileVersionInfoW (filePath.c_str (), 0, size, data.data ()))
	{
		doc.AddMember ("error", "GetFileVersionInfoW failed", alloc);
		return doc;
	}
	struct LANGANDCODEPAGE
	{
		WORD wLanguage;
		WORD wCodePage;
	};
	LANGANDCODEPAGE *lpTranslate = nullptr;
	UINT cbTranslate = 0;
	if (!VerQueryValueW (data.data (),
		L"\\VarFileInfo\\Translation",
		(LPVOID *)&lpTranslate,
		&cbTranslate))
	{
		doc.AddMember ("error", "No Translation", alloc);
		return doc;
	}
	wchar_t langCode [20] = {};
	wsprintfW (langCode, L"%04x%04x",
		lpTranslate [0].wLanguage,
		lpTranslate [0].wCodePage);
	std::wstring lc = langCode;
	const wchar_t *keys [] =
	{
		L"CompanyName",
		L"FileDescription",
		L"FileVersion",
		L"InternalName",
		L"OriginalFilename",
		L"ProductName",
		L"ProductVersion",
		L"LegalCopyright"
	};
	for (auto key : keys)
	{
		std::wstring val = QueryVersionString (data.data (), lc, key);
		if (!val.empty ())
		{
			rapidjson::Value k;
			k.SetString (WStringToString (key, CP_UTF8).c_str (), alloc);
			rapidjson::Value v;
			v.SetString (WStringToString (val, CP_UTF8).c_str (), alloc);
			doc.AddMember (k, v, alloc);
		}
	}
	VS_FIXEDFILEINFO *ffi = nullptr;
	UINT ffiLen = 0;
	if (VerQueryValueW (data.data (), L"\\", (LPVOID *)&ffi, &ffiLen))
	{
		if (ffi && ffiLen)
		{
			wchar_t ver [64];
			wsprintfW (ver, L"%u.%u.%u.%u",
				HIWORD (ffi->dwFileVersionMS),
				LOWORD (ffi->dwFileVersionMS),
				HIWORD (ffi->dwFileVersionLS),
				LOWORD (ffi->dwFileVersionLS));
			doc.AddMember ("FileVersionRaw",
				rapidjson::Value ().SetString (WStringToString (ver, CP_UTF8).c_str (), alloc),
				alloc);
		}
	}
	return doc;
}
size_t ExploreSaveFile (
	HWND hParent,
	std::vector<std::wstring> &results,
	LPWSTR lpFilter = L"Windows Store App Package (*.appx; *.appxbundle)\0*.appx;*.appxbundle",
	const std::wstring &lpDefExt = L"appx",
	DWORD dwFlags = OFN_OVERWRITEPROMPT | OFN_PATHMUSTEXIST | OFN_HIDEREADONLY,
	const std::wstring &swWndTitle = std::wstring (L"Please select the file to save: "),
	const std::wstring &swInitDir = GetFileDirectoryW (g_lastfile))
{
	results.clear ();
	const DWORD BUFFER_SIZE = 65536; // 64KB
	std::vector <WCHAR> buffer (BUFFER_SIZE, 0);
	OPENFILENAME ofn;
	ZeroMemory (&ofn, sizeof (ofn));
	ofn.hwndOwner = hParent;
	ofn.lpstrFile = buffer.data ();
	ofn.nMaxFile = BUFFER_SIZE;
	ofn.lpstrFilter = lpFilter;
	ofn.nFilterIndex = 1;
	ofn.lpstrTitle = swWndTitle.c_str ();
	ofn.lpstrDefExt = lpDefExt.c_str ();
	ofn.Flags = dwFlags;
	ofn.lpstrInitialDir = swInitDir.c_str ();
	ofn.lStructSize = sizeof (ofn);
	if (GetSaveFileNameW (&ofn))
	{
		std::wstring filePath = buffer.data ();
		results.push_back (filePath);
		g_lastfile = filePath;
	}
	return results.size ();
}

[ComVisible (true)]
public ref class SplashForm: public System::Windows::Forms::Form
{
	public:
	using PictureBox = System::Windows::Forms::PictureBox;
	using Timer = System::Windows::Forms::Timer;
	private:
	PictureBox ^picbox;
	Timer ^timer;
	System::Drawing::Image ^splashimg = nullptr;
	System::Drawing::Color background = System::Drawing::Color::Transparent;
	// System::Windows::Forms ^parent = nullptr;
	double opastep = 0.05;
	void InitForm ()
	{
		this->DoubleBuffered = true;
		InitializeComponent ();
		this->FormBorderStyle = System::Windows::Forms::FormBorderStyle::None;
		this->StartPosition = System::Windows::Forms::FormStartPosition::Manual;
		this->ShowInTaskbar = false;
		this->AllowTransparency = true;
		this->Opacity = 1.0;
	}
	void InitializeComponent ()
	{
		double dDpi = GetDPI () * 0.01;
		this->picbox = gcnew System::Windows::Forms::PictureBox ();
		this->picbox->Size = System::Drawing::Size (620 * dDpi, 300 * dDpi);
		this->picbox->BackColor = System::Drawing::Color::Transparent;
		picbox->Anchor = System::Windows::Forms::AnchorStyles::None;
		picbox->SizeMode = System::Windows::Forms::PictureBoxSizeMode::Zoom;
	}
	void OnFadeTimer (Object ^sender, EventArgs ^e)
	{
		auto fadeTimer = timer;
		auto opacityStep = opastep;
		if (this->Opacity > 0)
		{
			this->Opacity -= opacityStep;
		}
		else
		{
			fadeTimer->Stop ();
			this->Close ();
		}
	}
	void OnLoad (Object ^sender, EventArgs ^e)
	{
		this->ChangePosAndSize ();
		this->Visible = true;
	}
	void OnResize (Object ^sender, EventArgs ^e)
	{
		if (IsHandleCreated && picbox->IsHandleCreated)
		{
			Drawing::Size sz = this->ClientSize;
			this->picbox->Location = Drawing::Point (
				(sz.Width - picbox->Width) * 0.5,
				(sz.Height - picbox->Height) * 0.5
			);
		}
	}
	void OnResizeOwner (Object ^sender, EventArgs ^e) { this->ChangePosAndSize (); }
	void OnLocationChangedOwner (Object ^sender, EventArgs ^e) { this->ChangePosAndSize (); }
	protected:
	virtual void OnHandleCreated (EventArgs^ e) override
	{
		Form::OnHandleCreated (e);
		if (Environment::OSVersion->Version->Major >= 6)
		{
			INT mr = 0;
			MARGINS margins = {mr, mr, mr, mr};
			HRESULT hr = DwmExtendFrameIntoClientArea ((HWND)this->Handle.ToPointer (), &margins);
		}
	}
	public:
	SplashForm (System::String ^imgpath, System::Drawing::Color backcolor, System::Windows::Forms::Form ^owner)
	{
		if (owner != nullptr) this->Owner = owner;
		InitForm ();
		std::wstring filefullpath = MPStringToStdW (imgpath);
		if (filefullpath.find (L'%') != filefullpath.npos) filefullpath = ProcessEnvVars (filefullpath);
		filefullpath = GetFullPathName (imgpath ? MPStringToStdW (imgpath) : L"");
		try
		{
			auto img = System::Drawing::Image::FromFile (gcnew System::String (filefullpath.c_str ()));
			if (img != nullptr)
			{
				splashimg = img;
				picbox->Image = img;
			}
		}
		catch (...) {}
		if (splashimg) picbox->Image = splashimg;
		try
		{
			if (backcolor != Drawing::Color::Transparent)
			{
				background = backcolor;
				picbox->BackColor = backcolor;
				this->BackColor = backcolor;
			}
			else
			{
				picbox->BackColor = background;
				this->BackColor = background;
			}
		}
		catch (...) {}
		if (this->Owner != nullptr)
		{
			this->Owner->Resize += gcnew System::EventHandler (this, &SplashForm::OnResizeOwner);
			this->Owner->LocationChanged += gcnew System::EventHandler (this, &SplashForm::OnLocationChangedOwner);
		}
		this->Controls->Add (picbox);
		this->Resize += gcnew EventHandler (this, &SplashForm::OnResize);
		timer = gcnew System::Windows::Forms::Timer ();
		timer->Interval = 15;
		timer->Tick += gcnew System::EventHandler (this, &SplashForm::OnFadeTimer);
		this->Load += gcnew EventHandler (this, &SplashForm::OnLoad);
	}
	void ReInit ()
	{
		InitForm ();
		picbox = gcnew System::Windows::Forms::PictureBox ();
		picbox->BackColor = background;
		if (splashimg) picbox->Image = splashimg;
		picbox->SizeMode = System::Windows::Forms::PictureBoxSizeMode::Zoom;
		picbox->Anchor = System::Windows::Forms::AnchorStyles::None;
		double dDpi = GetDPI () * 0.01;
		picbox->Size = Drawing::Size (620 * dDpi, 300 * dDpi);
		this->BackColor = background;
		this->Controls->Clear ();
		this->Controls->Add (picbox);
		timer = gcnew System::Windows::Forms::Timer ();
		timer->Interval = 15;
		timer->Tick += gcnew EventHandler (this, &SplashForm::OnFadeTimer);
		this->Resize += gcnew EventHandler (this, &SplashForm::OnResize);
		this->Load += gcnew EventHandler (this, &SplashForm::OnLoad);
		ChangePosAndSize ();
		this->Opacity = 1.0;
	}
	void ChangePosAndSize ()
	{
		if (this->Owner && this->Owner->IsHandleCreated)
		{
			this->Owner->Update ();
			System::Drawing::Point pt = this->Owner->PointToScreen (this->Owner->ClientRectangle.Location);
			this->Location = pt;
			this->Size = this->Owner->ClientSize;
		}
		else if (this->Parent && this->Parent->IsHandleCreated)
		{
			this->Parent->Update ();
			System::Drawing::Point pt = this->Parent->PointToScreen (this->Parent->ClientRectangle.Location);
			this->Location = pt;
			this->Size = this->Parent->ClientSize;
		}
		if (IsHandleCreated && picbox->IsHandleCreated)
		{
			Drawing::Size sz = this->ClientSize;
			this->picbox->Location = Drawing::Point (
				(sz.Width - picbox->Width) * 0.5,
				(sz.Height - picbox->Height) * 0.5
			);
		}
	}
	void SetSplashImage (System::Drawing::Image ^img) { if (picbox && picbox->IsHandleCreated) { splashimg = img; picbox->Image = splashimg; } else splashimg = img; }
	void SetSplashImage (System::String ^imgpath) { try { SetSplashImage (System::Drawing::Image::FromFile (imgpath)); } catch (...) {} }
	void SetSplashImage (const std::wstring &imgpath) { SetSplashImage (CStringToMPString (imgpath)); }
	void SetSplashBackgroundColor (System::Drawing::Color color) { background = color;  picbox->BackColor = color; this->BackColor = color; }
	// 渐变消失
	void FadeOut () { timer->Start (); }
	// 立即消失
	void FadeAway () { timer->Start (); }
	~SplashForm ()
	{
		if (this->Owner != nullptr)
		{
			this->Owner->Resize -= gcnew System::EventHandler (this, &SplashForm::OnResizeOwner);
			this->Owner->LocationChanged -= gcnew System::EventHandler (this, &SplashForm::OnLocationChangedOwner);
		}
	}
};
uint64_t GetFileSize (String ^filepath)
{
	FileInfo ^fi = gcnew FileInfo (filepath);
	if (!fi->Exists) return -1;
	return fi->Length; 
}
bool CopyFileWithDialog (const std::wstring &src, const std::wstring &dst)
{
	HRESULT hr = CoInitializeEx (NULL, COINIT_APARTMENTTHREADED);
	if (FAILED (hr)) return false;
	CComPtr <IFileOperation> pfo;
	hr = pfo.CoCreateInstance (CLSID_FileOperation);
	if (FAILED (hr)) return false;
	pfo->SetOperationFlags (
		FOF_NOCONFIRMMKDIR |
		FOF_ALLOWUNDO
	);
	CComPtr <IShellItem> psiSrc;
	CComPtr <IShellItem> psiDst;
	hr = SHCreateItemFromParsingName (
		src.c_str (),
		NULL,
		IID_PPV_ARGS (&psiSrc)
	);
	if (FAILED (hr)) return false;
	std::wstring dstDir = GetFileDirectoryW (dst);
	hr = SHCreateItemFromParsingName (
		dstDir.c_str (),
		NULL,
		IID_PPV_ARGS (&psiDst)
	);
	if (FAILED (hr)) return false;
	hr = pfo->CopyItem (psiSrc, psiDst, NULL, NULL);
	if (FAILED (hr)) return false;
	hr = pfo->PerformOperations ();
	bool ok = SUCCEEDED (hr);
	return ok;
}
// pkginfo 中的内容以 UTF8 保存为 themeinfo.json 文件，并位于压缩包根目录。
// 该文件仅在压缩包中存在，是为了方便读取主题文件信息的文件。当压缩或解压完毕后该文件会在磁盘中删除。
void CompressToThemePackage (String ^srcfolder, String ^destfile, String ^pkginfo, String ^id)
{
	if (!Directory::Exists (srcfolder))
		throw gcnew DirectoryNotFoundException (srcfolder);

	if (File::Exists (destfile))
		File::Delete (destfile);

	FileStream^ fs = gcnew FileStream (destfile, FileMode::CreateNew);
	ZipArchive^ zip = gcnew ZipArchive (fs, ZipArchiveMode::Create);

	try
	{
		array<String^>^ files = Directory::GetFiles (srcfolder, "*", SearchOption::AllDirectories);
		for each (String^ file in files)
		{
			String^ relativePath = file->Substring (srcfolder->Length);

			// 去掉开头的分隔符
			if (relativePath->StartsWith (Path::DirectorySeparatorChar.ToString ()) ||
				relativePath->StartsWith (Path::AltDirectorySeparatorChar.ToString ()))
			{
				relativePath = relativePath->Substring (1);
			}

			// 如果是 custom.css 就改名为 {id}.css
			if (relativePath->Equals ("custom.css", StringComparison::OrdinalIgnoreCase))
			{
				relativePath = id->Trim () + ".css";
			}

			ZipArchiveEntry^ entry = zip->CreateEntry (relativePath, CompressionLevel::Optimal);
			Stream^ entryStream = entry->Open ();
			FileStream^ fileStream = gcnew FileStream (file, FileMode::Open, FileAccess::Read);

			array<Byte>^ buffer = gcnew array<Byte> (4096);
			int bytesRead;
			while ((bytesRead = fileStream->Read (buffer, 0, buffer->Length)) > 0)
			{
				entryStream->Write (buffer, 0, bytesRead);
			}

			fileStream->Close ();
			entryStream->Close ();
		}

		// 添加 themeinfo.json
		ZipArchiveEntry^ infoEntry = zip->CreateEntry ("themeinfo.json", CompressionLevel::Optimal);
		Stream^ infoStream = infoEntry->Open ();
		array<Byte>^ infoBytes = Encoding::UTF8->GetBytes (pkginfo);
		infoStream->Write (infoBytes, 0, infoBytes->Length);
		infoStream->Close ();
	}
	finally
	{
		delete zip;
		delete fs;
	}
}
String ^GetThemePackageInfo (String ^pkgfile)
{
	if (!File::Exists (pkgfile)) throw gcnew FileNotFoundException ("Theme package not found.", pkgfile);
	FileStream^ fs = gcnew FileStream (pkgfile, FileMode::Open, FileAccess::Read);
	ZipArchive^ zip = gcnew ZipArchive (fs, ZipArchiveMode::Read);
	try
	{
		ZipArchiveEntry^ infoEntry = zip->GetEntry ("themeinfo.json");
		if (infoEntry == nullptr)
			throw gcnew InvalidDataException ("themeinfo.json not found in theme package.");
		Stream^ stream = infoEntry->Open ();
		StreamReader^ reader = gcnew StreamReader (stream, Encoding::UTF8);
		String^ json = reader->ReadToEnd ();
		reader->Close ();
		stream->Close ();
		return json;
	}
	finally
	{
		delete zip;
		delete fs;
	}
}
void ExtractThemePackage (String ^pkgfile, String ^destfolder)
{
	if (!File::Exists (pkgfile)) throw gcnew FileNotFoundException ("Theme package not found.", pkgfile);
	if (Directory::Exists (destfolder)) Directory::Delete (destfolder);
	if (!Directory::Exists (destfolder)) Directory::CreateDirectory (destfolder);
	FileStream^ fs = gcnew FileStream (pkgfile, FileMode::Open, FileAccess::Read);
	ZipArchive^ zip = gcnew ZipArchive (fs, ZipArchiveMode::Read);
	try
	{
		for each (ZipArchiveEntry^ entry in zip->Entries)
		{
			String^ outPath = Path::Combine (destfolder, entry->FullName);

			// 目录项
			if (String::IsNullOrEmpty (entry->Name))
			{
				if (!Directory::Exists (outPath))
					Directory::CreateDirectory (outPath);
				continue;
			}

			// 确保目录存在
			String^ dir = Path::GetDirectoryName (outPath);
			if (!String::IsNullOrEmpty (dir) && !Directory::Exists (dir))
				Directory::CreateDirectory (dir);

			// 写文件
			Stream^ inStream = entry->Open ();
			FileStream^ outStream = gcnew FileStream (
				outPath,
				FileMode::Create,
				FileAccess::Write
			);

			array<Byte>^ buffer = gcnew array<Byte> (4096);
			int bytesRead;
			while ((bytesRead = inStream->Read (buffer, 0, buffer->Length)) > 0)
			{
				outStream->Write (buffer, 0, bytesRead);
			}

			outStream->Close ();
			inStream->Close ();
		}
	}
	finally
	{
		delete zip;
		delete fs;
	}
}
[ComVisible (true)]
public ref class MainHtmlWnd: public System::Windows::Forms::Form, public IScriptBridge
{
	public:
	using WebBrowser = System::Windows::Forms::WebBrowser;
	private:
	WebBrowser ^webui;
	SplashForm ^splash;
	ITaskbarList3 *taskbar = nullptr;
	public:
	[ComVisible (true)]
	ref class _I_UI2: public _I_UI
	{
		private:
		MainHtmlWnd ^wndinst = nullptr;
		public:
		_I_UI2 (MainHtmlWnd ^wnd): wndinst (wnd), _I_UI (wnd) {}
		property String ^SplashImage
		{
			String ^get ()
			{
				auto uri = gcnew Uri (CStringToMPString (wndinst->GetSuitSplashImage ()));
				return uri->AbsoluteUri;
			}
		}
		property String ^SplashBackgroundColor
		{
			String ^get ()
			{
				std::wnstring ret = L"";
				auto personal = g_initfile [L"Personalization"];
				auto thememode = personal [L"AppInstaller:ThemeMode"];
				auto custommode = personal [L"AppInstaller:CustomThemeMode"];
				bool nowdark =
					IsNormalizeStringEquals (thememode.read_wstring ().c_str (), L"dark") ||
					IsNormalizeStringEquals (thememode.read_wstring ().c_str (), L"auto") && IsAppInDarkMode () ||
					IsNormalizeStringEquals (thememode.read_wstring ().c_str (), L"custom") && IsNormalizeStringEquals (custommode.read_wstring ().c_str (), L"dark") ||
					IsNormalizeStringEquals (thememode.read_wstring ().c_str (), L"custom") && IsNormalizeStringEquals (custommode.read_wstring ().c_str (), L"auto") && IsAppInDarkMode ();
				if (nowdark) ret = g_vemani.splash_screen_backgroundcolor_darkmode (g_idInVe);
				else ret = g_vemani.splash_screen_backgroundcolor (g_idInVe);
				if (ret.empty ()) ret = g_vemani.splash_screen_backgroundcolor (g_idInVe);
				if (ret.empty ()) ret = g_vemani.background_color (g_idInVe);
				return CStringToMPString (ret);
			}
		}
		void ShowSplash () { if (wndinst->SplashScreen->IsHandleCreated) wndinst->SplashScreen->Show (); else wndinst->SplashScreen->ReInit (); }
		void FadeAwaySplash () { wndinst->SplashScreen->FadeAway (); }
		void FadeOutSplash () { wndinst->SplashScreen->FadeOut (); }
	};
	[ComVisible (true)]
	ref class _I_VisualElements2: public _I_VisualElements
	{
		public:
		property _I_VisualElement ^Current {_I_VisualElement ^get () { return Get (CStringToMPString (g_idInVe)); }}
	};
	[ComVisible (true)]
	ref class IBridge: public _I_Bridge_Base2
	{
		private:
		MainHtmlWnd ^wndinst = nullptr;
		bool hasjump1 = false,
			hasjump2 = false,
			hasexec = false;
		public:
		using String = System::String;
		[ComVisible (true)]
		ref class _I_IEFrame: public _I_IEFrame_Base
		{
			private:
			MainHtmlWnd ^wndinst = nullptr;
			public:
			_I_IEFrame (MainHtmlWnd ^wnd): wndinst (wnd) {}
			property int Scale
			{
				int get () { return wndinst->PageScale; }
				void set (int value) { return wndinst->PageScale = value; }
			}
		};
		[ComVisible (true)]
		ref class _I_System3: public _I_System
		{
			protected:
			_I_UI2 ^ui2;
			public:
			_I_System3 (MainHtmlWnd ^wnd)
			{
				ui2 = gcnew _I_UI2 (wnd);
			}
			property _I_UI2 ^UI { _I_UI2 ^get () { return ui2; } }
		};
		[ComVisible (true)]
		ref class _I_Process
		{
			public:
			using String = System::String;
			ref class ProcessWorker
			{
				_I_Process ^parent;
				public:
				ProcessWorker (_I_Process ^parent): parent (parent) {}
				ProcessWorker (): parent (gcnew _I_Process ()) {}
				String ^cmdline = String::Empty;
				String ^filepath = String::Empty;
				int wndtype = SW_NORMAL;
				String ^runpath = String::Empty;
				Object ^callback = nullptr;
				void Work ()
				{
					int ret = parent->Run (cmdline, filepath, wndtype, true, runpath);
					if (callback)
					{
						try
						{
							callback->GetType ()->InvokeMember (
								"call",
								BindingFlags::InvokeMethod,
								nullptr,
								callback,
								gcnew array<Object^>{ 1, ret }
							);
						}
						catch (...) {}
					}
				}
			};
			public:
			int Run (String ^cmdline, String ^filepath, int wndtype, bool wait, String ^runpath)
			{
				return ExecuteProgram (
					MPStringToStdW (cmdline),
					MPStringToStdW (filepath),
					wndtype,
					wait,
					MPStringToStdW (runpath)
				);
			}
			void RunAsync (String ^cmdline, String ^filepath, int wndtype, String ^runpath, Object ^callback)
			{
				auto worker = gcnew ProcessWorker (this);
				worker->cmdline = cmdline;
				worker->filepath = filepath;
				worker->wndtype = wndtype;
				worker->runpath = runpath;
				worker->callback = callback;
				Thread^ th = gcnew Thread (gcnew ThreadStart (worker, &ProcessWorker::Work));
				th->IsBackground = true;
				th->Start ();
			}
			bool Kill (String ^filename, bool allproc, bool onlyname) { return KillProcessByFilePath (MPStringToStdW (filename), allproc, onlyname); }
		};
		[ComVisible (true)]
		ref class _I_ResourcePri
		{
			public:
			using String = System::String;
			String ^GetString (String ^uri) 
			{ 
				auto ret = g_winjspri.get (MPStringToStdW (uri));
				auto retstr = CStringToMPString (ret);
				return retstr;
			}
		};
		private:
		_I_IEFrame ^ieframe;
		_I_System3 ^sys;
		_I_VisualElements2 ^ve;
		_I_Download ^download;
		_I_Process ^proc;
		_I_ResourcePri ^winjs_res;
		public:
		IBridge (MainHtmlWnd ^wnd): wndinst (wnd), _I_Bridge_Base2 (wnd)
		{
			ieframe = gcnew _I_IEFrame (wnd);
			sys = gcnew _I_System3 (wnd);
			storage = gcnew _I_Storage ();
			ve = gcnew _I_VisualElements2 ();
			download = gcnew _I_Download ();
			proc = gcnew _I_Process ();
			winjs_res = gcnew _I_ResourcePri ();
		}
		property _I_IEFrame ^IEFrame { _I_IEFrame ^get () { return ieframe; }}
		property _I_System3 ^System { _I_System3 ^get () { return sys; }}
		property _I_VisualElements2 ^VisualElements { _I_VisualElements2 ^get () { return ve; } }
		property _I_Download ^Download { _I_Download ^get () { return download; }}
		property _I_Process ^Process { _I_Process ^get () { return proc; }}
		property _I_ResourcePri ^WinJsStringRes { _I_ResourcePri ^get () { return winjs_res; }}
		property _I_ResourcePri ^StringResources { _I_ResourcePri ^get () { return winjs_res; }}
		String ^FormatDateTime (String ^fmt, String ^jsDate) { return FormatString (fmt, Convert::ToDateTime (jsDate)); }
		property String ^CmdArgs { String ^get () { return CStringToMPString (StringArrayToJson (g_cmdargs)); }}
		property bool Jump1 { bool get () { return hasjump1; } void set (bool value) { hasjump1 = value; } }
		property bool Jump2 { bool get () { return hasjump2; } void set (bool value) { hasjump2 = value; } }
		property bool Exec1 { bool get () { return hasexec; } void set (bool value) { hasexec = value; } }
		property double TBProgress { void set (double value) { wndinst->TProgressPercent = value; }}
		property DWORD TBState { void set (DWORD value) { wndinst->TProgressState = (TBPFLAG)value; }}
		String ^GetVersionInfoToJSON (String ^filepath)
		{
			auto doc = GetFileVersionAsJson (MPStringToStdW (filepath));
			rapidjson::StringBuffer buffer;
			rapidjson::Writer <rapidjson::StringBuffer> writer (buffer);
			doc.Accept (writer);
			return CStringToMPString (StringToWString (buffer.GetString (), CP_UTF8));
		}
		String ^SelectFilesToJSON (String ^filter, DWORD flags, String ^wndtitle, String ^initdir)
		{
			std::vector <std::wstring> ret;
			std::wstring filterbuf = MPStringToStdW (filter) + L'|||';
			for (auto &it : filterbuf) if (it == L'|') it = L'\0';
			ExploreFile (wndinst->InvokeGetHWND (), ret, (LPWSTR)filterbuf.c_str (), flags, MPStringToStdW (wndtitle), MPStringToStdW (initdir));
			return CStringToMPString (StringArrayToJson (ret));
		}
		uint64_t GetFileSize (String ^filepath) { return ::GetFileSize (filepath); }
		bool ShellCopyFile (String ^src, String ^desc) { return CopyFileWithDialog (MPStringToStdW (src), MPStringToStdW (desc)); }
		String ^GetSavePath (String ^filter, String ^defext, DWORD flags, String ^wndtitle, String ^initdir)
		{
			std::vector <std::wstring> ret;
			std::wstring filterbuf = MPStringToStdW (filter) + L'|||';
			for (auto &it : filterbuf) if (it == L'|') it = L'\0';
			ExploreSaveFile (wndinst->InvokeGetHWND (), ret, (LPWSTR)filterbuf.data (), MPStringToStdW (defext), flags, MPStringToStdW (wndtitle), MPStringToStdW (initdir));
			for (auto &it : ret) if (!IsNormalizeStringEmpty (it)) return CStringToMPString (it);
			return "";
		}
		void CompressTheme (String ^srcfolder, String ^destfile, String ^pkginfo, String ^id) { CompressToThemePackage (srcfolder, destfile, pkginfo, id); }
		String ^GetThemePkgInfo (String ^themepkg) { return GetThemePackageInfo (themepkg); }
		void ExtraceThemePkg (String ^src, String ^desc) { ExtractThemePackage (src, desc); }
		bool CopyFile (String^ srcFile, String^ destFile, bool cover)
		{
			try
			{
				if (String::IsNullOrEmpty (srcFile) || String::IsNullOrEmpty (destFile))
					return false;

				if (!File::Exists (srcFile))
					throw gcnew FileNotFoundException ("Source file not found.", srcFile);

				// 确保目标目录存在
				String^ destDir = Path::GetDirectoryName (destFile);
				if (!String::IsNullOrEmpty (destDir) && !Directory::Exists (destDir))
				{
					Directory::CreateDirectory (destDir);
				}

				// overwrite = true
				File::Copy (srcFile, destFile, cover);
				return true;
			}
			catch (Exception^)
			{
				return false;
			}
		}
		void CloseWindow ()
		{
			if (wndinst && wndinst->IsHandleCreated) wndinst->Close ();
		}
	};
	protected:
	property WebBrowser ^WebUI { WebBrowser ^get () { return this->webui; } }
	property SplashForm ^SplashScreen { SplashForm ^get () { return this->splash; } }
	property int DPIPercent { int get () { return GetDPI (); }}
	property double DPI { double get () { return DPIPercent * 0.01; }}
	virtual void OnHandleCreated (EventArgs ^e) override
	{
		::SetClassLongPtrW ((HWND)this->Handle.ToPointer (), GCLP_HBRBACKGROUND, (LONG_PTR)g_wndclass);
		Form::OnHandleCreated (e);
	}
	void InitSize ()
	{
		unsigned ww = 0, wh = 0;
		auto &ini = g_initfile;
		auto setsect = ini ["Settings"];
		auto savepos = setsect [L"Settings:SavePosAndSizeBeforeCancel"];
		auto lastw = setsect [L"Settings:LastWidth"];
		auto lasth = setsect [L"Settings:LastHeight"];
		auto defw = setsect [L"Settings:DefaultWidth"];
		auto defh = setsect [L"Settings:DefaultHeight"];
		auto minw = setsect [L"Settings:MinimumWidth"];
		auto minh = setsect [L"Settings:MinimumHeight"];
		auto lasts = setsect [L"Settings:LastWndState"];
		if (savepos.read_bool ())
		{
			ww = lastw.read_uint (defw.read_uint (rcInt (IDS_DEFAULTWIDTH)));
			wh = lasth.read_uint (defh.read_uint (rcInt (IDS_DEFAULTHEIGHT)));
		}
		else
		{
			ww = defw.read_uint (rcInt (IDS_DEFAULTWIDTH));
			wh = defh.read_uint (rcInt (IDS_DEFAULTHEIGHT));
		}
		this->ClientSize = System::Drawing::Size (ww * DPI, wh * DPI);
		int hborder = this->Size.Width - this->ClientSize.Width,
			vborder = this->Size.Height - this->ClientSize.Height;
		this->MinimumSize = System::Drawing::Size (
			minw.read_uint (rcInt (IDS_MINWIDTH)) * DPI + hborder,
			minh.read_uint (rcInt (IDS_MINHIEHGT)) * DPI + vborder
		);
		this->WindowState = (System::Windows::Forms::FormWindowState)lasts.read_int ((int)System::Windows::Forms::FormWindowState::Normal);
	}
	void Init ()
	{
		this->Visible = false;
		this->webui = gcnew System::Windows::Forms::WebBrowser ();
		this->SuspendLayout ();
		this->webui->Dock = System::Windows::Forms::DockStyle::Fill;
		this->webui->IsWebBrowserContextMenuEnabled = DEBUGMODE;
		this->webui->AllowWebBrowserDrop = false;
		this->Controls->Add (this->webui);
		if (g_hIconMain.hIcon)
		{
			try { this->Icon = System::Drawing::Icon::FromHandle (IntPtr (g_hIconMain.hIcon)); }
			catch (...) {}
		}
		this->Text = GetRCStringCli (IDS_WINTITLE);
		this->ResumeLayout (false);
		webui->ObjectForScripting = gcnew IBridge (this);
		this->webui->DocumentCompleted += gcnew System::Windows::Forms::WebBrowserDocumentCompletedEventHandler (this, &MainHtmlWnd::OnDocumentCompleted);
		this->webui->PreviewKeyDown += gcnew System::Windows::Forms::PreviewKeyDownEventHandler (this, &MainHtmlWnd::OnPreviewKeyDown_WebBrowser);
		this->Resize += gcnew System::EventHandler (this, &MainHtmlWnd::OnResize);
		this->Load += gcnew EventHandler (this, &MainHtmlWnd::OnCreate);
		this->ResizeEnd += gcnew EventHandler (this, &MainHtmlWnd::OnResizeEnd);
	}
	void OnDocumentCompleted (Object ^sender, System::Windows::Forms::WebBrowserDocumentCompletedEventArgs ^e)
	{
		static bool issetdpi = false;
		if (!issetdpi)
		{
			issetdpi = true;
			ExecScript ("Bridge.Frame.scale = Bridge.Frame.scale * Bridge.UI.dpi");
		}
		ExecScript ("Windows.UI.DPI.mode = 1");
		if (e->Url->ToString () == webui->Url->ToString ())
		{
			auto &ini = g_initfile;
			splash->FadeAway ();
		}
	}
	void OnCreate (System::Object ^sender, System::EventArgs ^e)
	{
		splash->Owner = this;
		splash->ChangePosAndSize ();
		splash->Show ();
		splash->Update ();
		splash->SetSplashImage (GetSuitSplashImage ());
		System::Windows::Forms::Application::DoEvents ();
		auto htmlpath = CombinePath (GetProgramRootDirectoryW (), L"html\\settings.html");
		webui->Navigate (CStringToMPString (htmlpath));
		if (!IsFileExists (htmlpath))
		{
			std::wstring msg = L"Error: cannot find file \"" + htmlpath + L"\".";
			MessageBoxW (InvokeGetHWND (), msg.c_str (), GetRCStringSW (IDS_WINTITLE).c_str (), MB_ICONERROR);
			this->Close ();
			return;
		}
	}
	void OnPreviewKeyDown_WebBrowser (System::Object ^sender, System::Windows::Forms::PreviewKeyDownEventArgs ^e)
	{
		if (e->KeyCode == System::Windows::Forms::Keys::F5 || (e->KeyCode == System::Windows::Forms::Keys::R && e->Control))
			e->IsInputKey = true;
	}
	void ResponseSplashChange ()
	{
		splash->SetSplashImage (GetSuitSplashImage ());
		//ExecScript (
		//	"(function () {"
		//	"var splash = Page.splash;"
		//	"if (!splash) return null;"
		//	"splash.imagesrc = Bridge.UI.Splash.imageurl;"
		//	"splash.background = Bridge.UI.Splash.backcolor;"
		//	"var progress = splash.content.querySelector (\"progress\");"
		//	"if (Bridge.Frame.WindowSize.height / Bridge.UI.dpi < 500) {"
		//	"if (progress.classList.contains(\"win-ring\")) progress.classList.remove(\"win-ring\");}"
		//	"else { if (!progress.classList.contains(\"win-ring\")) progress.classList.add(\"win-ring\"); }"
		//	"}) ();"
		//);
	}
	void OnResize (Object ^sender, EventArgs ^e) { ResizeEvent (); }
	void OnResizeEnd (Object ^sender, EventArgs ^e) {}
	std::wstring GetSuitSplashImage ()
	{
		int limitw = 800 * DPI;
		int limith = 600 * DPI;
		std::wstring tag = L"settings-splash";
		int noww = this->Width;
		int nowh = this->Height;
		if (noww >= limitw && nowh >= limith)
			tag = L"settings-splashlarge";
		std::wstring path = g_scaleres [tag];
		if (IsNormalizeStringEmpty (path))
			path = g_scaleres [L"settings-splash"];
		if (IsNormalizeStringEmpty (path))
			path = g_vemani.splash_screen_image (g_idInVe);
		return path;
	}
	void ResizeEvent ()
	{
		auto &ini = g_initfile;
		auto setsect = ini ["Settings"];
		auto lasts = setsect [L"Settings:LastWndState"];
		auto savepos = setsect [L"Settings:SavePosAndSizeBeforeCancel"];
		auto lastw = setsect [L"Settings:LastWidth"];
		auto lasth = setsect [L"Settings:LastHeight"];
		switch (this->WindowState)
		{
			case System::Windows::Forms::FormWindowState::Normal:
			case System::Windows::Forms::FormWindowState::Maximized:
				lasts = (int)this->WindowState;
		}
		if (this->WindowState == System::Windows::Forms::FormWindowState::Normal && savepos)
		{
			lastw = (int)(this->ClientSize.Width / DPI);
			lasth = (int)(this->ClientSize.Height / DPI);
		}
		ResponseSplashChange ();
	}
	void InvokeClose ()
	{
		if (this->InvokeRequired) this->Invoke (gcnew Action (this, &MainHtmlWnd::Close));
		else this->Close ();
	}
	IntPtr GetHWnd () { return this->Handle; }
	delegate IntPtr GetHwndDelegate ();
	HWND InvokeGetHWND ()
	{
		if (this->InvokeRequired)
		{
			GetHwndDelegate ^del = gcnew GetHwndDelegate (this, &MainHtmlWnd::GetHWnd);
			IntPtr result = safe_cast <IntPtr> (this->Invoke (del));
			return static_cast <HWND> (result.ToPointer ());
		}
		else return static_cast <HWND> (this->Handle.ToPointer ());
	}
	bool BackPage () { return this->webui->GoBack (); }
	bool InvokeBackPage ()
	{
		if (this->InvokeRequired) return (bool)this->Invoke (gcnew Func <bool> (this, &MainHtmlWnd::BackPage));
		else return BackPage ();   // 当前线程就是 UI 线程
	}
	public:
	MainHtmlWnd ()
	{
		InitSize ();
		System::Windows::Forms::Application::DoEvents ();
		splash = gcnew SplashForm (
			CStringToMPString (GetSuitSplashImage ()),
			StringToColor (CStringToMPString (g_vemani.splash_screen_backgroundcolor (g_idInVe))),
			this
		);
		System::Windows::Forms::Application::DoEvents ();
		Init ();
		ITaskbarList3 *ptr = nullptr;
		HRESULT hr = CoCreateInstance (CLSID_TaskbarList, nullptr, CLSCTX_INPROC_SERVER, IID_ITaskbarList3, (void **)&ptr);
		if (SUCCEEDED (hr))
		{
			taskbar = ptr;
			taskbar->HrInit ();
		}
		else
		{
			taskbar = nullptr;
			if (ptr) ptr->Release ();
		}
	}
	Object ^CallScriptFunction (String ^lpFuncName, ... array <Object ^> ^alpParams)
	{
		try { return this->webui->Document->InvokeScript (lpFuncName, alpParams); }
		catch (Exception ^e)
		{
			try
			{
				this->webui->Document->InvokeScript ("messageBoxAsync", gcnew array <Object ^> {
					e->Message,
						e->Source,
						0,
						CStringToMPString (g_vemani.background_color (g_idInVe))
				});
			}
			catch (Exception ^ex)
			{
				MessageBoxW (InvokeGetHWND (), MPStringToStdW (e->Message).c_str (), MPStringToStdW (e->Source).c_str (), 0);
			}
		}
		return nullptr;
	}
	Object ^CallScriptFunction (String ^lpScriptName)
	{
		try { return this->webui->Document->InvokeScript (lpScriptName); }
		catch (Exception ^ex) { System::Windows::Forms::MessageBox::Show ("Error calling JavaScript function: " + ex->Message); }
		return nullptr;
	}
	Object ^InvokeCallScriptFunction (String ^lpFuncName, ... array <Object ^> ^alpParams)
	{
		try
		{
			if (this->InvokeRequired) return (Object ^)this->Invoke (gcnew Func <String ^, array <Object ^> ^, Object ^> (this, &MainHtmlWnd::CallScriptFunction), lpFuncName, alpParams);
			else return CallScriptFunction (lpFuncName, alpParams);
		}
		catch (Exception ^e) {}
		return nullptr;
	}
	Object ^InvokeCallScriptFunction (String ^lpScriptName)
	{
		try
		{
			if (this->InvokeRequired) return (Object ^)this->Invoke (gcnew Func <String ^, Object ^> (this, &MainHtmlWnd::CallScriptFunction), lpScriptName);
			else return CallScriptFunction (lpScriptName);
		}
		catch (Exception ^e) {}
		return nullptr;
	}
	Object ^ExecScript (... array <Object ^> ^alpScript) { return InvokeCallScriptFunction ("eval", alpScript); }
	Object ^CallEvent (String ^funcName, Object ^e) override
	{
		std::wstring fname = MPStringToStdW (funcName);
		if (fname == L"InvokeBackPage") InvokeBackPage ();
		return nullptr;
	}
	property int PageScale
	{
		int get ()
		{
			CComPtr <IWebBrowser2> web2;
			HRESULT hr = GetWebBrowser2Interface (webui, &web2);
			if (FAILED (hr)) return 0;
			VARIANT v;
			VariantInit (&v);
			hr = web2->ExecWB (OLECMDID_OPTICAL_ZOOM, OLECMDEXECOPT_DODEFAULT, nullptr, &v);
			if (FAILED (hr) || v.vt != VT_I4) return 0;
			int val = v.lVal;
			VariantClear (&v);
			return val;
		}
		void set (int value)
		{
			CComPtr <IWebBrowser2> web2;
			HRESULT hr = GetWebBrowser2Interface (webui, &web2);
			if (FAILED (hr)) return;
			VARIANT v;
			VariantInit (&v);
			v.vt = VT_I4;
			v.lVal = value;
			web2->ExecWB (OLECMDID_OPTICAL_ZOOM, OLECMDEXECOPT_DONTPROMPTUSER, &v, nullptr);
		}
	}
	// 只能设置。0 - 100
	property double TProgressPercent { void set (double progressPercent) { if (taskbar) taskbar->SetProgressValue (InvokeGetHWND (), progressPercent * 100, 100 * 100); }}
	property TBPFLAG TProgressState { void set (TBPFLAG state) { if (taskbar) taskbar->SetProgressState (InvokeGetHWND (), state); }}
	~MainHtmlWnd ()
	{
		if (taskbar) taskbar->Release ();
		taskbar = nullptr;
	}
};
using MainWnd = MainHtmlWnd;

HRESULT SetCurrentAppUserModelID (PCWSTR appID)
{
	typedef HRESULT (WINAPI *SetAppUserModelIDFunc)(PCWSTR);
	HMODULE shell32 = LoadLibraryW (L"shell32.dll");
	destruct freelib ([&] () {
		if (shell32) FreeLibrary (shell32);
	});
	try
	{
		if (!shell32) return E_FAIL;
		auto SetAppUserModelID = (SetAppUserModelIDFunc)GetProcAddress (shell32, "SetCurrentProcessExplicitAppUserModelID");
		if (!SetAppUserModelID)
		{
			FreeLibrary (shell32);
			return E_FAIL;
		}
		return SetAppUserModelID (appID);
	}
	catch (...) { return E_FAIL; }
	return E_FAIL;
}

void SetProgramSingleInstance (
	const std::wstring &mutexName,
	std::function <void ()> repeatCallback = nullptr,
	bool focusMain = true)
{
	HANDLE hMutex = CreateMutexW (NULL, TRUE, mutexName.c_str ());
	if (hMutex == NULL) return;
	destruct _mutexFree ([&] () { CloseHandle (hMutex); });
	if (GetLastError () != ERROR_ALREADY_EXISTS) return;
	if (repeatCallback) repeatCallback ();
	wchar_t pathBuf [MAX_PATH] = {0};
	GetModuleFileNameW (NULL, pathBuf, MAX_PATH);
	std::wstring exeName = pathBuf;
	exeName = exeName.substr (exeName.find_last_of (L"\\/") + 1);
	DWORD existingPid = 0;
	HANDLE snap = CreateToolhelp32Snapshot (TH32CS_SNAPPROCESS, 0);
	if (snap != INVALID_HANDLE_VALUE)
	{
		PROCESSENTRY32W pe = {sizeof (pe)};
		if (Process32FirstW (snap, &pe))
		{
			do {
				if (_wcsicmp (pe.szExeFile, exeName.c_str ()) == 0 &&
					pe.th32ProcessID != GetCurrentProcessId ())
				{
					existingPid = pe.th32ProcessID;
					break;
				}
			} while (Process32NextW (snap, &pe));
		}
		CloseHandle (snap);
	}
	HWND targetHwnd = NULL;
	if (existingPid)
	{
		EnumWindows ([] (HWND hwnd, LPARAM lParam) -> BOOL {
			DWORD pid = 0;
			GetWindowThreadProcessId (hwnd, &pid);
			if (pid == (DWORD)lParam && IsWindowVisible (hwnd))
			{
				*((HWND *)(&lParam)) = hwnd;
				return FALSE;
			}
			return TRUE;
		}, (LPARAM)&targetHwnd);
	}
	if (focusMain && targetHwnd)
	{
		if (IsIconic (targetHwnd)) ShowWindow (targetHwnd, SW_RESTORE);
		DWORD thisThread = GetCurrentThreadId ();
		DWORD wndThread = GetWindowThreadProcessId (targetHwnd, NULL);
		AttachThreadInput (thisThread, wndThread, TRUE);
		SetActiveWindow (targetHwnd);
		SetForegroundWindow (targetHwnd);
		AttachThreadInput (thisThread, wndThread, FALSE);
		BringWindowToTop (targetHwnd);
	}
	ExitProcess (0);
}
[STAThread]
int APIENTRY wWinMain (HINSTANCE hInstance, HINSTANCE hPrevInstance, LPWSTR lpCmdLine, int nCmdShow)
{
	SetCurrentProcessExplicitAppUserModelID (g_lpAppId);
	SetProgramSingleInstance (g_lpAppId);
	SetProcessDPIAware ();
	{
		// 设置当前目录为程序所在目录
		std::wnstring currdir = GetCurrentDirectoryW ();
		std::wnstring rootdir = GetProgramRootDirectoryW ();
		if (!PathEquals (currdir, rootdir)) SetCurrentDirectoryW (rootdir.c_str ());
	}
	CoInitializeEx (NULL, COINIT_MULTITHREADED | COINIT_APARTMENTTHREADED);
	SetupInstanceEnvironment ();
	destruct relco ([] () {
		CoUninitialize ();
	});
	{
		auto cmdline = GetCommandLineW ();
		int argc = 0;
		auto argv = CommandLineToArgvW (cmdline, &argc);
		destruct relt ([&argv] () {
			if (argv) LocalFree (argv);
		});
		for (size_t i = 1; i < argc; i ++) g_cmdargs.push_back (argv [i]);
	}
	SetWebBrowserEmulation ();
	System::Windows::Forms::Application::EnableVisualStyles ();
	System::Windows::Forms::Application::SetCompatibleTextRenderingDefault (false);
	auto mwnd = gcnew MainHtmlWnd ();
	g_mainwnd = mwnd;
	System::Windows::Forms::Application::Run (mwnd);
	return 0;
}
