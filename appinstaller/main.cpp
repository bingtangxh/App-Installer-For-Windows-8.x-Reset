#ifdef _M_ARM
#define _ARM_WINAPI_PARTITION_DESKTOP_SDK_AVAILABLE 1
#endif

#include <Windows.h>
#include <msclr/marshal_cppstd.h>
#include <ShObjIdl.h>
#include <ShlObj.h>       // KNOWNFOLDERID, SHGetKnownFolderPath
#include <commdlg.h>      // OPENFILENAME
#include <comdef.h>       // _com_error
#include <MsHTML.h>
#include <ExDisp.h>
#include <atlbase.h>
#include <fstream>
#include <comdef.h>
#include <vcclr.h>
#include "cmdargs.h"
#include "themeinfo.h"
#include "mpstr.h"
#include "initfile.h"
#include "resource.h"
#include "vemani.h"
#include "ieshell.h"
#include "resmap.h"
#include "appxinfo.h"
#include "localeex.h"
#include "pkgmgr.h"
#include "notice.h"
#include "certmgr.h"
#include "bridge.h"
#include "filepath.h"

using namespace System;
using namespace System::Runtime::InteropServices;

#ifdef _DEBUG
#define DEBUGMODE true
#else
#define DEBUGMODE false
#endif
#define JS_SAFE [MarshalAs (UnmanagedType::SafeArray, SafeArraySubType = VarEnum::VT_VARIANT)]
enum class CMDPARAM: DWORD
{
	NONE = 0b000,
	SILENT = 0b001,
	VERYSILENT = 0b011,
	MULTIPLE = 0b100
};

LPCWSTR g_lpAppId = L"Microsoft.DesktopAppInstaller!App";
auto &g_identity = g_lpAppId;
auto &m_idenName = g_lpAppId;
struct iconhandle
{
	HICON hIcon = nullptr;
	iconhandle (HICON hIcon = nullptr): hIcon (hIcon) {}
	~iconhandle () { try { if (hIcon) DestroyIcon (hIcon); hIcon = nullptr; } catch (...) {} }
};
iconhandle g_hIconMain (LoadRCIcon (IDI_ICON_MAIN));
initfile g_initfile (CombinePath (GetProgramRootDirectoryW (), L"config.ini"));
ref class MainHtmlWnd;
msclr::gcroot <MainHtmlWnd ^> g_mainwnd;
vemanifest g_vemani (
	IsFileExists (CombinePath (GetProgramRootDirectoryW (), L"VisualElementsManifest.xml")) ?
	CombinePath (GetProgramRootDirectoryW (), L"VisualElementsManifest.xml") :
	CombinePath (GetProgramRootDirectoryW (), L"AppInstaller.VisualElementsManifest.xml")
);
resxmldoc g_scaleres (
	IsFileExists (CombinePath (GetProgramRootDirectoryW (), L"VisualElements\\scale.xml")) ?
	CombinePath (GetProgramRootDirectoryW (), L"VisualElements\\scale.xml") :
	CombinePath (GetProgramRootDirectoryW (), L"VisualElementsManifest.xml")
);
WORD g_wcmdflags = 0;
std::vector <std::wnstring> g_pkgfiles;
std::vector <pkginfo> g_pkginfo;
std::wstring g_lastfile;
struct package_installresult
{
	HRESULT result = S_OK;
	std::wstring error;
	std::wstring reason;
	bool succeeded () const { return SUCCEEDED (result); }
	bool failed () const { return FAILED (result); }
	package_installresult (HRESULT result, const std::wstring &err, const std::wstring &msg): result (result), reason (msg), error (err) {}
	package_installresult (HRESULT result, const std::wstring &msg): result (result), reason (msg) {}
	package_installresult () = default;
};
std::map <std::wnstring, package_installresult> g_pkgresult;

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
[ComVisible (true)]
public ref class _I_InitConfig
{
	public:
	Win32::InitConfig ^Create (String ^filepath) { return gcnew Win32::InitConfig (filepath); }
	Win32::InitConfig ^GetConfig () { return Create (CStringToMPString (g_initfile.filepath)); }
};
[ComVisible (true)]
public ref class _I_Package
{
	public:
	ref class _I_Package_Manager
	{
		public:
		_I_Package_Manager () {}
	};
	private:
	_I_Package_Manager ^mgr = gcnew _I_Package_Manager ();
	public:
	String ^GetPackagesToJson ()
	{
		rapidjson::Document doc;
		doc.SetArray ();
		auto &alloc = doc.GetAllocator ();
		for (auto &it : g_pkginfo)
		{
			rapidjson::Value member (rapidjson::kStringType);
			member.SetString (ws2utf8 (it.filepath).c_str (), alloc);
			doc.PushBack (member, alloc);
		}
		rapidjson::StringBuffer buffer;
		rapidjson::Writer <rapidjson::StringBuffer> writer (buffer);
		doc.Accept (writer);
		std::string utf8 = buffer.GetString ();
		std::wstring_convert <std::codecvt_utf8 <wchar_t>> conv;
		return CStringToMPString (conv.from_bytes (utf8));
	}
	String ^GetPackageInfoToJson (String ^filepath)
	{
		std::wstring fpath = MPStringToStdW (filepath);
		for (auto &it : g_pkginfo)
		{
			if (PathEquals (it.filepath, fpath))
			{
				return CStringToMPString (it.parseJson ());
			}
		}
		return "{}";
	}
	property _I_Package_Manager ^Manager { _I_Package_Manager ^get () { return mgr; }}
	String ^GetCapabilityDisplayName (String ^capabilityName)
	{
		return CStringToMPString (GetPackageCapabilityDisplayName (MPStringToStdW (capabilityName)));
	}
	_I_HResult ^GetPackageInstallResult (String ^filepath)
	{
		std::wstring path = MPStringToStdW (filepath);
		if (g_pkgresult.find (path) == g_pkgresult.end ()) return nullptr;
		auto &pres = g_pkgresult.at (path);
		return gcnew _I_HResult (
			pres.result,
			CStringToMPString (pres.error),
			CStringToMPString (pres.reason)
		);
	}
	_I_HResult ^Activate (String ^appid)
	{
		DWORD dwProgressId = 0;
		HRESULT hr = ActivateAppxApplication (MPStringToStdW (appid ? appid : "").c_str (), &dwProgressId);
		return gcnew _I_HResult (hr);
	}
};
[ComVisible (true)]
public ref class _I_Bridge_Base
{
	protected:
	_I_String ^str = gcnew _I_String ();
	_I_InitConfig ^initconfig = gcnew _I_InitConfig ();
	_I_Storage ^storage = gcnew _I_Storage ();
	_I_Package ^pkg = gcnew _I_Package ();
	public:
	property _I_String ^String { _I_String ^get () { return str; }}
	property _I_InitConfig ^Config { _I_InitConfig ^get () { return initconfig; }}
	property _I_Storage ^Storage { _I_Storage ^get () { return storage; }}
	property _I_Package ^Package { _I_Package ^get () { return pkg; }}
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
	property int Archievement
	{
		int get ()
		{
			SYSTEM_INFO si;
			ZeroMemory (&si, sizeof (si));
			typedef VOID (WINAPI *LPFN_GetNativeSystemInfo)(LPSYSTEM_INFO);
			LPFN_GetNativeSystemInfo fnGetNativeSystemInfo =
				(LPFN_GetNativeSystemInfo)GetProcAddress (
					GetModuleHandleW (L"kernel32.dll"),
					"GetNativeSystemInfo");
			if (fnGetNativeSystemInfo) fnGetNativeSystemInfo (&si);
			else GetSystemInfo (&si);
			return (int)si.wProcessorArchitecture;
		}
	}
	property _I_Version ^Version
	{
		_I_Version ^get ()
		{
			auto ver = Environment::OSVersion->Version;
			try
			{
				return gcnew _I_Version (ver->Major, ver->Minor, ver->Build, ver->Revision);
			}
			catch (...)
			{
				return gcnew _I_Version (ver->Major, ver->Minor, ver->Build, 0);
			}
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

enum class InstallType
{
	normal,
	update,
	reinstall
};
inline std::wstring ToStdWString (const std::wstring &str) { return str; }
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
public delegate void InstallProgressCallbackDelegate (DWORD progress);
#pragma managed(push, off)
void NativeProgressCallback (DWORD progress, void* context)
{
	(void)progress;
	(void)context;
}
#pragma managed(pop)
static void ManagedThunk (DWORD progress, void *context)
{
	if (context == nullptr) return;
	GCHandle handle = GCHandle::FromIntPtr (IntPtr (context));
	auto cb = (InstallProgressCallbackDelegate ^)handle.Target;
	if (cb != nullptr) cb (progress);
}
HRESULT AddAppxPackageFromPath (
	const std::wstring &pkgpath,
	const std::vector <std::wstring> &deplist,
	DWORD deployoption,
	InstallProgressCallbackDelegate ^callback,
	std::wstring &errorcode,
	std::wstring &detailmsg
)
{
	std::vector <BYTE> bytes (sizeof (REGISTER_PACKAGE_DEFENDENCIES) + sizeof (LPWSTR) * deplist.size ());
	auto lpdeplist = (PREGISTER_PACKAGE_DEFENDENCIES)bytes.data ();
	lpdeplist->dwSize = (DWORD)deplist.size ();
	for (size_t i = 0; i < deplist.size (); ++i)
		lpdeplist->alpDepUris [i] = (LPWSTR)deplist [i].c_str ();
	GCHandle handle;
	void *ctx = nullptr;
	PKGMRR_PROGRESSCALLBACK pfnCallback = nullptr;
	if (callback != nullptr)
	{
		handle = GCHandle::Alloc (callback);
		ctx = (void *)GCHandle::ToIntPtr (handle).ToPointer ();
		pfnCallback = (PKGMRR_PROGRESSCALLBACK)&ManagedThunk; // 传托管 thunk 给 native
	}
	LPWSTR lperr = nullptr, lpmsg = nullptr;
	destruct relt ([&] () {
		if (lperr) PackageManagerFreeString (lperr);
		if (lpmsg) PackageManagerFreeString (lpmsg);
		lperr = nullptr;
		lpmsg = nullptr;
	});
	HRESULT hr = AddAppxPackageFromPath (
		pkgpath.c_str (),
		lpdeplist,
		deployoption,
		pfnCallback,
		ctx,
		&lperr,
		&lpmsg
	);
	if (callback != nullptr && handle.IsAllocated) handle.Free ();
	errorcode = lperr ? lperr : L"";
	detailmsg = lpmsg ? lpmsg : L"";
	return hr;
}
HRESULT UpdateAppxPackageFromPath (
	const std::wstring &pkgpath,
	const std::vector <std::wstring> &deplist,
	DWORD deployoption,
	InstallProgressCallbackDelegate ^callback,
	std::wstring &errorcode,
	std::wstring &detailmsg
)
{
	std::vector <BYTE> bytes (sizeof (REGISTER_PACKAGE_DEFENDENCIES) + sizeof (LPWSTR) * deplist.size ());
	auto lpdeplist = (PREGISTER_PACKAGE_DEFENDENCIES)bytes.data ();
	lpdeplist->dwSize = (DWORD)deplist.size ();
	for (size_t i = 0; i < deplist.size (); ++i)
		lpdeplist->alpDepUris [i] = (LPWSTR)deplist [i].c_str ();
	GCHandle handle;
	void *ctx = nullptr;
	PKGMRR_PROGRESSCALLBACK pfnCallback = nullptr;
	if (callback != nullptr)
	{
		handle = GCHandle::Alloc (callback);
		ctx = (void *)GCHandle::ToIntPtr (handle).ToPointer ();
		pfnCallback = (PKGMRR_PROGRESSCALLBACK)&ManagedThunk; // 传托管 thunk 给 native
	}
	LPWSTR lperr = nullptr, lpmsg = nullptr;
	destruct relt ([&] () {
		if (lperr) PackageManagerFreeString (lperr);
		if (lpmsg) PackageManagerFreeString (lpmsg);
		lperr = nullptr;
		lpmsg = nullptr;
	});
	HRESULT hr = UpdateAppxPackageFromPath (
		pkgpath.c_str (),
		lpdeplist,
		deployoption,
		pfnCallback,
		ctx,
		&lperr,
		&lpmsg
	);
	if (callback != nullptr && handle.IsAllocated) handle.Free ();
	errorcode = lperr ? lperr : L"";
	detailmsg = lpmsg ? lpmsg : L"";
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
void ActivateApp (Object ^appid)
{
	auto res = ActivateAppxApplication (MPStringToStdW (appid->ToString ()));
}
[ComVisible (true)]
public ref class AppListWnd: public System::Windows::Forms::Form, public IScriptBridge
{
	public:
	using WebBrowser = System::Windows::Forms::WebBrowser;
	using Timer = System::Windows::Forms::Timer;
	[ComVisible (true)]
	ref class IBridge: public _I_Bridge_Base3
	{
		private:
		AppListWnd ^wndinst = nullptr;
		public:
		IBridge (AppListWnd ^wnd): wndinst (wnd), _I_Bridge_Base3 (wnd, wnd) {}
		ref class _I_IEFrame: public _I_IEFrame_Base
		{
			private:
			AppListWnd ^wndinst = nullptr;
			public:
			_I_IEFrame (AppListWnd ^wnd): wndinst (wnd) {}
			property int Scale
			{
				int get () { return wndinst->PageScale; }
				void set (int value) { return wndinst->PageScale = value; }
			}
		};
		private:
		_I_IEFrame ^ieframe = gcnew _I_IEFrame (wndinst);
		public:
		property _I_IEFrame ^IEFrame { _I_IEFrame ^get () { return ieframe; }}
	};
	private:
	WebBrowser ^webui = nullptr;
	Timer ^showtimer = gcnew Timer ();
	Timer ^hidetimer = gcnew Timer ();
	int aminedelay = 150;
	int framedelay = 25;
	double movelen = 10 * DPI;
	int frametotal = aminedelay / framedelay;
	double movestep = movelen / (double)frametotal;
	int framenow = 0;
	int finaltop = 0;
	public:
	property WebBrowser ^WebUI { WebBrowser ^get () { return this->webui; } }
	property int DPIPercent { int get () { return GetDPI (); }}
	property double DPI { double get () { return DPIPercent * 0.01; }}
	AppListWnd ()
	{
		this->Visible = false;
		this->DoubleBuffered = true;
		this->FormBorderStyle = System::Windows::Forms::FormBorderStyle::None;
		this->ShowInTaskbar = false;
		this->TopMost = true;
		this->Size = System::Drawing::Size (392 * DPI, 494 * DPI);
		if (IsWindows10 ()) this->MinimumSize = System::Drawing::Size (392 * DPI, 226 * DPI);
		else this->MinimumSize = System::Drawing::Size (392 * DPI, 129 * DPI);
		if (IsWindows10 ()) this->MaximumSize = System::Drawing::Size (392 * DPI, 522 * DPI);
		else this->MaximumSize = System::Drawing::Size (368 * DPI, 394 * DPI);
		this->Text = GetRCStringCli (IDS_APPLIST_WINTITLE);
		webui = gcnew System::Windows::Forms::WebBrowser ();
		webui->Dock = System::Windows::Forms::DockStyle::Fill;
		this->Controls->Add (webui);
		webui->Visible = false;
		webui->ObjectForScripting = gcnew IBridge (this);
		this->webui->DocumentCompleted += gcnew System::Windows::Forms::WebBrowserDocumentCompletedEventHandler (this, &AppListWnd::OnDocumentCompleted);
		this->Load += gcnew EventHandler (this, &AppListWnd::OnCreate);
		this->StartPosition = System::Windows::Forms::FormStartPosition::CenterScreen;
		this->Deactivate += gcnew EventHandler (this, &AppListWnd::OnDeactivate);
		webui->Navigate (CStringToMPString (CombinePath (GetProgramRootDirectoryW (), L"html\\applist.html")));
		showtimer->Interval = framedelay;
		hidetimer->Interval = framedelay;
		showtimer->Tick += gcnew EventHandler (this, &AppListWnd::OnTick_ShowTimer);
		hidetimer->Tick += gcnew EventHandler (this, &AppListWnd::OnTick_HideTimer);
		this->Opacity = 0;
	}
	protected:
	void OnCreate (System::Object ^sender, System::EventArgs ^e) {}
	void OnDocumentCompleted (Object ^sender, System::Windows::Forms::WebBrowserDocumentCompletedEventArgs ^e)
	{
		if (e->Url->ToString () == webui->Url->ToString ())
		{

			ExecScript ("Windows.UI.DPI.mode = 1");
			ExecScript ("Bridge.Frame.scale = Bridge.Frame.scale * Bridge.UI.dpi");
			InvokeCallScriptFunction ("setWindows10Style", IsWindows10 ());
			size_t cnt = 0;
			for (auto &it : g_pkginfo)
			{
				for (auto &app : it.applications)
				{
					std::wstring launchid = it.identity.package_family_name + L'!' + app [L"Id"];
					std::wnstring color = app [L"BackgroundColor"];
					if (color.empty () || color.equals (L"transparent")) color = MPStringToStdW (ColorToHtml (GetDwmThemeColor ()));
					std::wnstring displayName = app [L"DisplayName"];
					if (displayName.empty ()) displayName = app [L"ShortName"];
					auto logo = app [L"Square44x44Logo"];
					if (std::wnstring::empty (logo)) logo = app [L"SmallLogo"];
					InvokeCallScriptFunction (
						"addAppToList",
						CStringToMPString (displayName),
						CStringToMPString (logo),
						CStringToMPString (launchid),
						CStringToMPString (color)
					);
					cnt ++;
				}
			}
			if (cnt == 0) this->Close ();
			{
				bool isWin10 = IsWindows10 ();
				size_t height = ((cnt) * (isWin10 ? 50 : 60) * DPI) + (isWin10 ? 206 : 120) * DPI;
				if (height < (isWin10 ? 522 : 394) * DPI) this->Height = height;
				else this->Height = 522 * DPI;
				this->Left = (GetScreenWidth () - this->Width) / 2;
				this->Top = (GetScreenHeight () - this->Height) / 2;
			}
			finaltop = this->Top;
			this->Top -= movelen;
			webui->Visible = true;
			this->Visible = true;
		}
	}
	void OnPress_Cancel ()
	{
		if (!this->IsHandleCreated) return;
		if (InvokeRequired) this->Invoke (gcnew Action (this, &AppListWnd::HideAmine));
		else this->HideAmine ();
		return;
	}
	void OnPress_AppItem ()
	{
		// OnPress_Cancel ();
		if (!this->IsHandleCreated) return;
		if (InvokeRequired) this->Invoke (gcnew Action (this, &AppListWnd::Close));
		else this->Close ();
		return;
	}
	void OnDeactivate (Object ^sender, EventArgs ^e)
	{
		OnPress_Cancel ();
	}
	void OnTick_ShowTimer (Object ^sender, EventArgs ^e)
	{
		framenow ++;
		if (framenow > frametotal)
		{
			showtimer->Stop ();
			this->Top = finaltop;
			this->Opacity = 1;
		}
		else
		{
			this->Top = finaltop + movestep * (frametotal - framenow);
			this->Opacity = framenow / (double)frametotal;
		}
	}
	void OnTick_HideTimer (Object ^sender, EventArgs ^e)
	{
		framenow ++;
		if (framenow > frametotal)
		{
			showtimer->Stop ();
			this->Opacity = 0;
			this->Close ();
		}
		else
		{
			this->Opacity = 1.0 - framenow / (double)frametotal;
		}
	}
	public:
	static void DisplayWindow ()
	{
		auto wnd = gcnew AppListWnd ();
		wnd->ShowAmine ();
	}
	Object ^CallScriptFunction (String ^lpFuncName, ... array <Object ^> ^alpParams)
	{
		try { return this->webui->Document->InvokeScript (lpFuncName, alpParams); }
		catch (Exception ^e) {}
		return nullptr;
	}
	Object ^CallScriptFunction (String ^lpScriptName)
	{
		try { return this->webui->Document->InvokeScript (lpScriptName); }
		catch (Exception ^e) {}
		return nullptr;
	}
	Object ^InvokeCallScriptFunction (String ^lpFuncName, ... array <Object ^> ^alpParams)
	{
		try
		{
			if (this->InvokeRequired) return (Object ^)this->Invoke (gcnew Func <String ^, array <Object ^> ^, Object ^> (this, &AppListWnd::CallScriptFunction), lpFuncName, alpParams);
			else return CallScriptFunction (lpFuncName, alpParams);
		}
		catch (Exception ^e) {}
		return nullptr;
	}
	Object ^InvokeCallScriptFunction (String ^lpScriptName)
	{
		try
		{
			if (this->InvokeRequired) return (Object ^)this->Invoke (gcnew Func <String ^, Object ^> (this, &AppListWnd::CallScriptFunction), lpScriptName);
			else return CallScriptFunction (lpScriptName);
		}
		catch (Exception ^e) {}
		return nullptr;
	}
	Object ^ExecScript (... array <Object ^> ^alpScript) { return InvokeCallScriptFunction ("eval", alpScript); }
	Object ^CallEvent (String ^funcName, Object ^e) override
	{
		std::wstring fname = MPStringToStdW (funcName);
		if (IsNormalizeStringEquals (fname.c_str (), L"OnPress_CancelButton")) OnPress_Cancel ();
		else if (IsNormalizeStringEquals (fname.c_str (), L"OnPress_AppItem")) OnPress_AppItem ();
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
	void ShowAmine ()
	{
		this->Show ();
		hidetimer->Stop ();
		showtimer->Start ();
	}
	void HideAmine ()
	{
		framenow = 0;
		showtimer->Stop ();
		hidetimer->Start ();
	}
};
[ComVisible (true)]
public ref class MainHtmlWnd: public System::Windows::Forms::Form, public IScriptBridge
{
	public:
	using WebBrowser = System::Windows::Forms::WebBrowser;
	private:
	WebBrowser ^webui;
	SplashForm ^splash;
	String ^pagetag = "splash";
	InstallType insmode = InstallType::normal;
	size_t nowinstall = 0;
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
				if (nowdark) ret = g_vemani.splash_screen_backgroundcolor_darkmode (L"App");
				else ret = g_vemani.splash_screen_backgroundcolor (L"App");
				if (ret.empty ()) ret = g_vemani.splash_screen_backgroundcolor (L"App");
				if (ret.empty ()) ret = g_vemani.background_color (L"App");
				return CStringToMPString (ret);
			}
		}
		void ShowSplash () { if (wndinst->SplashScreen->IsHandleCreated) wndinst->SplashScreen->Show (); else wndinst->SplashScreen->ReInit (); }
		void FadeAwaySplash () { wndinst->SplashScreen->FadeAway (); }
		void FadeOutSplash () { wndinst->SplashScreen->FadeOut (); }
	};
	[ComVisible (true)]
	ref class IBridge: public _I_Bridge_Base2
	{
		private:
		MainHtmlWnd ^wndinst = nullptr;
		public:
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
		private:
		_I_IEFrame ^ieframe;
		_I_System3 ^sys;
		public:
		IBridge (MainHtmlWnd ^wnd): wndinst (wnd), _I_Bridge_Base2 (wnd)
		{
			ieframe = gcnew _I_IEFrame (wnd);
			sys = gcnew _I_System3 (wnd);
		}
		property _I_IEFrame ^IEFrame { _I_IEFrame ^get () { return ieframe; }}
		property _I_System3 ^System { _I_System3 ^get () { return sys; }}
	};
	protected:
	property WebBrowser ^WebUI { WebBrowser ^get () { return this->webui; } }
	property SplashForm ^SplashScreen { SplashForm ^get () { return this->splash; } }
	property int DPIPercent { int get () { return GetDPI (); }}
	property double DPI { double get () { return DPIPercent * 0.01; }}
	property String ^PageTag { String ^get () { return GetPage (); } void set (String ^tag) { SetPage (tag); }}
	void InitSize ()
	{
		unsigned ww = 0, wh = 0;
		auto &ini = g_initfile;
		auto setsect = ini ["Settings"];
		auto savepos = setsect [L"AppInstaller:SavePosAndSizeBeforeCancel"];
		auto lastw = setsect [L"AppInstaller:LastWidth"];
		auto lasth = setsect [L"AppInstaller:LastHeight"];
		auto defw = setsect [L"AppInstaller:DefaultWidth"];
		auto defh = setsect [L"AppInstaller:DefaultHeight"];
		auto minw = setsect [L"AppInstaller:MinimumWidth"];
		auto minh = setsect [L"AppInstaller:MinimumHeight"];
		auto lasts = setsect [L"AppInstaller:LastWndState"];
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
		if (e->Url->ToString () == webui->Url->ToString ())
		{

			ExecScript ("Windows.UI.DPI.mode = 1");
			ExecScript ("Bridge.Frame.scale = Bridge.Frame.scale * Bridge.UI.dpi");
			auto &ini = g_initfile;
			auto setsect = ini ["Settings"];
			auto lwr = setsect [L"AppInstaller:LaunchWhenReady"];
			bool launchwhenready = lwr.read_bool (true);
			if (g_wcmdflags & (DWORD)CMDPARAM::SILENT) launchwhenready = false;
			InvokeCallScriptFunction ("setLaunchWhenReady", launchwhenready, g_wcmdflags & (DWORD)CMDPARAM::SILENT);
			SetPage ("splash");
			splash->FadeAway ();
			ThreadPackageLoadTask ();
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
		auto htmlpath = CombinePath (GetProgramRootDirectoryW (), L"html\\install.html");
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
		ExecScript (
			"(function () {"
			"var splash = Page.splash;"
			"if (!splash) return null;"
			"splash.imagesrc = Bridge.UI.Splash.imageurl;"
			"splash.background = Bridge.UI.Splash.backcolor;"
			"var progress = splash.content.querySelector (\"progress\");"
			"if (Bridge.Frame.WindowSize.height / Bridge.UI.dpi < 500) {"
			"if (progress.classList.contains(\"win-ring\")) progress.classList.remove(\"win-ring\");}"
			"else { if (!progress.classList.contains(\"win-ring\")) progress.classList.add(\"win-ring\"); }"
			"}) ();"
		);
	}
	void OnResize (Object ^sender, EventArgs ^e) { ResizeEvent (); }
	void OnResizeEnd (Object ^sender, EventArgs ^e) {}
	std::wstring GetSuitSplashImage ()
	{
		int limitw = 800 * DPI;
		int limith = 600 * DPI;
		std::wstring tag = L"splash";
		int noww = this->Width;
		int nowh = this->Height;
		if (noww >= limitw && nowh >= limith)
			tag = L"splashlarge";
		std::wstring path = g_scaleres [tag];
		if (IsNormalizeStringEmpty (path))
			path = g_scaleres [L"splash"];
		if (IsNormalizeStringEmpty (path))
			path = g_vemani.splash_screen_image (L"App");
		return path;
	}
	void ResizeEvent ()
	{
		auto &ini = g_initfile;
		auto setsect = ini ["Settings"];
		auto lasts = setsect [L"AppInstaller:LastWndState"];
		auto savepos = setsect [L"AppInstaller:SavePosAndSizeBeforeCancel"];
		auto lastw = setsect [L"AppInstaller:LastWidth"];
		auto lasth = setsect [L"AppInstaller:LastHeight"];
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
	System::Threading::Thread ^ThreadPackageLoadTask ()
	{
		auto thread = gcnew Threading::Thread (gcnew Threading::ThreadStart (this, &MainHtmlWnd::PackageLoadTask));
		thread->IsBackground = true;
		thread->Start ();
		return thread;
	}
	void PackageLoadTask ()
	{
		bool res = ReadPackagesTask (gcnew Action <String ^> (this, &MainHtmlWnd::ReadPackageCallback));
		InvokeCallScriptFunction ("setSplashPageStatusText", "");
		if (res)
		{
			String ^fmt = GetRCStringCli (IDS_PREINSTALL_TITLE);
			String ^btn1 = GetRCStringCli (IDS_PREINSTALL_TINSTALL);
			String ^btn2 = GetRCStringCli (IDS_PREINSTALL_CANCEL);
			if (g_pkginfo.size () == 1)
			{
				try
				{
					const auto &pi = g_pkginfo.at (0);
					const std::wstring
						&name = pi.identity.name,
						&publisher = pi.identity.publisher,
						&family = pi.identity.package_family_name,
						&fullname = pi.identity.package_full_name;
					std::vector <find_pkginfo> fpkgs;
					std::wstring err, msg;
					HRESULT hr = S_OK;
					if (g_initfile [L"Settings"] [L"AppInstaller:CheckPackageIsIntalled"].read_bool ())
						hr = GetAppxPackages (family, fpkgs, err, msg);
					bool isfind = false;
					find_pkginfo findpkg;
					if (fpkgs.size () > 0)
					{
						for (auto &it : fpkgs)
						{
							if (it.identity.name != pi.identity.name) continue;
							auto archs = pi.get_architectures ();
							for (auto &arch : archs)
							{
								if (arch == it.identity.architecture)
								{
									isfind = true;
									findpkg = it;
									break;
								}
								else if (arch == 11 || it.identity.architecture == 11)
								{
									isfind = true;
									findpkg = it;
									break;
								}
							}
							if (isfind) break;
							if (it.properties.resource_package && pi.properties.resource_package && it.identity.resource_id == pi.identity.resource_id)
							{
								isfind = true;
								findpkg = it;
								break;
							}
							if (archs.size () == 0 && it.identity.architecture == (WORD)-1)
							{
								isfind = true;
								findpkg = it;
								break;
							}
						}
					}
					if (isfind)
					{
						version fver = findpkg.identity.version,
							pver (pi.identity.realver.major, pi.identity.realver.minor, pi.identity.realver.build, pi.identity.realver.revision);
						if (pver > fver) // 更新模式 
						{
							insmode = InstallType::update;
							fmt = GetRCStringCli (IDS_PREINSTALL_TUPDATE);
							btn1 = GetRCStringCli (IDS_PREINSTALL_CUPDATE);
							btn2 = GetRCStringCli (IDS_PREINSTALL_CANCEL);
						}
						else if (pver == fver)
						{
							insmode = InstallType::reinstall;
							fmt = GetRCStringCli (IDS_PREINSTALL_TREINSTALL);
							btn1 = GetRCStringCli (IDS_PREINSTALL_CREINSTALL);
							btn2 = GetRCStringCli (IDS_SUCCESS_LAUNCH);
						}
						else
						{
							insmode = InstallType::reinstall;
							fmt = GetRCStringCli (IDS_PREINSTALL_HASINSTALLED);
							btn1 = GetRCStringCli (IDS_PREINSTALL_CREINSTALL);
							btn2 = GetRCStringCli (IDS_SUCCESS_LAUNCH);
						}
					}
				}
				catch (...) {}
			}
			InvokeCallScriptFunction ("noticeLoadPreinstallPage", g_pkginfo.size () > 1);
			InvokeCallScriptFunction ("setPreinstallPagePkgTitleFormatSingle", fmt);
			InvokeCallScriptFunction ("setControlButtonState", 1, btn1, true, false);
			InvokeCallScriptFunction ("setControlButtonState", 2, btn2, true, false);
			pagetag = "preinstall";
			if (g_wcmdflags & (DWORD)CMDPARAM::SILENT) ThreadPackageInstallTask ();
		}
		else
		{
			InvokeCallScriptFunction ("noticeLoadSelectPage", g_pkginfo.size () > 1);
			pagetag = "select";
		}
	}
	void ReadPackageCallback (String ^lpPath)
	{
		size_t pkgfilelen = g_pkgfiles.size ();
		if (pkgfilelen > 1)
		{
			InvokeCallScriptFunction (
				"setSplashPageStatusText",
				System::String::Format (GetRCStringCli (IDS_SPLASH_MLOAD), lpPath)
			);
		}
	}
	bool ReadPackagesTask (System::Action <String ^> ^callback)
	{
		std::vector <std::wstring> noread;
		std::vector <std::wstring> hasread;
		for (auto &it : g_pkgfiles)
		{
			bool isfind = false;
			for (auto &rit : g_pkginfo)
			{
				if (rit.filepath == it)
				{
					isfind == true;
					push_unique <std::wstring> (hasread, it);
					break;
				}
			}
			if (!isfind) push_unique <std::wstring> (noread, it);
		}
		for (auto &it : noread)
		{
			try { if (callback) callback (gcnew String (it.c_str ())); }
			catch (Exception ^e) { if (DEBUGMODE) OutputDebugStringW (MPStringToPtrW (e->Message)); }
			auto pi = pkginfo::parse (it);
			if (pi.valid)
			{
				push_unique (hasread, pi.filepath);
				g_pkginfo.push_back (pi);
			}
		}
		g_pkgfiles.clear ();
		for (auto &it : hasread) push_unique <std::wnstring> (g_pkgfiles, it);
		return hasread.size ();
	}
	void InstallProgressCallback (DWORD dwProgress)
	{
		InvokeCallScriptFunction ("setInstallingProgress", dwProgress);
		InvokeCallScriptFunction ("setInstallingStatus", String::Format (GetRCStringCli (IDS_INSTALLING_SINSTALLING_PROGRESS), dwProgress));
		taskbar->SetProgressValue (InvokeGetHWND (), dwProgress, 100);
	}
	void InstallProgressCallbackMultiple (DWORD dwProgress)
	{
		double progress = (dwProgress * 0.01 + nowinstall) / (double)g_pkginfo.size () * 100;
		InvokeCallScriptFunction ("setInstallingProgress", progress);
		InvokeCallScriptFunction ("setInstallingStatus", String::Format (GetRCStringCli (IDS_INSTALLING_MSINSTALLING_PROGRESS), dwProgress, nowinstall + 1, g_pkginfo.size ()));
		taskbar->SetProgressValue (InvokeGetHWND (), progress * g_pkginfo.size (), 100 * g_pkginfo.size ());
	}
	System::Threading::Thread ^ThreadPackageInstallTask ()
	{
		auto thread = gcnew Threading::Thread (gcnew Threading::ThreadStart (this, &MainHtmlWnd::PackageInstallTask));
		thread->IsBackground = true;
		thread->Start ();
		return thread;
	}
	void PackageInstallTask ()
	{
		InvokeCallScriptFunction ("noticeLoadInstallingPage", g_pkginfo.size () > 1);
		std::vector <std::wstring> blankdeplist;
		if (g_pkginfo.size () == 1)
		{
			auto &pi = *g_pkginfo.begin ();
			InvokeCallScriptFunction ("setInstallingStatus", GetRCStringCli (IDS_INSTALLING_SLOADCER));
			LoadCertFromSignedFile (pi.filepath.c_str ());
			InvokeCallScriptFunction ("setInstallingStatus", GetRCStringCli (IDS_INSTALLING_SINSTALLING));
			InvokeCallScriptFunction ("setInstallingProgress", 0);
			package_installresult pir;
			if (insmode == InstallType::update)
			{
				pir.result = UpdateAppxPackageFromPath (pi.filepath, blankdeplist, DEPOLYOPTION_NONE, gcnew InstallProgressCallbackDelegate (this, &MainHtmlWnd::InstallProgressCallback), pir.error, pir.reason);
				if (FAILED (pir.result))
					pir.result = AddAppxPackageFromPath (pi.filepath, blankdeplist, DEPOLYOPTION_NONE, gcnew InstallProgressCallbackDelegate (this, &MainHtmlWnd::InstallProgressCallback), pir.error, pir.reason);
			}
			else pir.result = AddAppxPackageFromPath (pi.filepath, blankdeplist, DEPOLYOPTION_NONE, gcnew InstallProgressCallbackDelegate (this, &MainHtmlWnd::InstallProgressCallback), pir.error, pir.reason);
			g_pkgresult [pi.filepath] = pir;
			taskbar->SetProgressState (InvokeGetHWND (), TBPF_NOPROGRESS);
			if (pir.succeeded ())
			{
				InvokeCallScriptFunction ("noticeLoadInstallSuccessPage", false);
				pagetag = "installsuccess";
				InvokeLaunchAppForLaunchWhenReady ();
				CreateToastNoticeWithImgBase64 (
					g_identity,
					MPStringToStdW (
						System::String::Format (
							GetRCStringCli (IDS_SUCCESS_TITLE),
							CStringToMPString (pi.properties.display_name)
						)
					),
					pi.properties.logo_base64
				);
				ThreadPackageSuccessInstallCountTask ();
			}
			else
			{
				InvokeCallScriptFunction ("noticeLoadInstallFailedPage", false);
				pagetag = "installfailed";
				CreateToastNotice2WithImgBase64 (
					g_identity,
					MPStringToStdW (
						System::String::Format (
							GetRCStringCli (IDS_FAILED_STITLE),
							CStringToMPString (pi.properties.display_name)
						)
					),
					pir.reason,
					pi.properties.logo_base64
				);
				if (g_wcmdflags & (DWORD)CMDPARAM::SILENT) ThreadPackageSuccessInstallCountTask ();
			}
		}
		else
		{
			for (nowinstall = 0; nowinstall < g_pkginfo.size (); nowinstall ++)
			{
				auto &it = g_pkginfo.at (nowinstall);
				InvokeCallScriptFunction ("setInstallingPackageInfoMultiple", CStringToMPString (it.filepath));
				InvokeCallScriptFunction (
					"setInstallingStatus",
					String::Format (
						GetRCStringCli (IDS_INSTALLING_MLOADCER),
						nowinstall + 1,
						g_pkginfo.size ()
					)
				);
				LoadCertFromSignedFile (it.filepath.c_str ());
				InvokeCallScriptFunction (
					"setInstallingStatus",
					String::Format (
						GetRCStringCli (IDS_INSTALLING_MPKGNAME),
						nowinstall + 1,
						g_pkginfo.size ()
					)
				);
				package_installresult pir;
				pir.result = AddAppxPackageFromPath (
					it.filepath,
					blankdeplist,
					DEPOLYOPTION_NONE,
					gcnew InstallProgressCallbackDelegate (this, &MainHtmlWnd::InstallProgressCallbackMultiple),
					pir.error,
					pir.reason
				);
				if (pir.failed ()) taskbar->SetProgressState (InvokeGetHWND (), TBPF_ERROR);
				g_pkgresult [it.filepath] = pir;
			}
			taskbar->SetProgressState (InvokeGetHWND (), TBPF_NOPROGRESS);
			bool allsuccess = true;
			for (auto &it : g_pkgresult)
			{
				allsuccess = allsuccess && it.second.succeeded ();
				if (!allsuccess) break;
			}
			if (allsuccess)
			{
				InvokeCallScriptFunction ("noticeLoadInstallSuccessPage", true);
				InvokeLaunchAppForLaunchWhenReady ();
				pagetag = "installsuccess";
				CreateToastNotice (g_identity, GetRCStringSW (IDS_SUCCESS_MTITLE), L"");
				ThreadPackageSuccessInstallCountTask ();
			}
			else
			{
				InvokeCallScriptFunction ("noticeLoadInstallFailedPage", true);
				CreateToastNotice (g_identity, GetRCStringSW (IDS_FAILED_MTITLE), L"");
				pagetag = "installfailed";
				if (g_wcmdflags & (DWORD)CMDPARAM::SILENT) ThreadPackageSuccessInstallCountTask ();
			}
		}
	}
	System::Threading::Thread ^ThreadPackageSuccessInstallCountTask ()
	{
		auto thread = gcnew Threading::Thread (gcnew Threading::ThreadStart (this, &MainHtmlWnd::PackageSuccessInstallCountTask));
		thread->IsBackground = true;
		thread->Start ();
		return thread;
	}
	void PackageSuccessInstallCountTask ()
	{
		size_t cnt = 0;
		for (auto &it : g_pkginfo)
			for (auto &it_s : it.applications)
				if (!it_s [L"Id"].empty ()) cnt ++;
		if (cnt <= 0) cnt = 1;
		if (cnt > 3) cnt = 3;
		if (g_wcmdflags & (DWORD)CMDPARAM::SILENT && cnt > 1) cnt = 2;
		System::Threading::Thread::Sleep (System::TimeSpan (0, 0, 5 * cnt));
		this->InvokeClose ();
	}
	void InvokeClose ()
	{
		if (this->InvokeRequired) this->Invoke (gcnew Action (this, &MainHtmlWnd::Close));
		else this->Close ();
	}
	String ^GetPage () { return pagetag; }
	String ^SetPage (String ^tag)
	{
		InvokeCallScriptFunction ("setPage", tag, g_pkginfo.size () > 1);
		return pagetag = tag;
	}
#define nequals(_str1_, _str2_) IsNormalizeStringEquals (ToStdWString (_str1_), ToStdWString (_str2_))
	void OnPress_Button1 ()
	{
		std::wstring current = MPStringToStdW (pagetag);
		if (nequals (current, L"select"))
		{
			std::vector <std::wstring> files;
			std::wstring item1 = GetRCStringSW (IDS_SELECT_DLGAPPX),
				item1type = L"*.appx;*.appxbundle",
				item2 = GetRCStringSW (IDS_SELECT_DLGALL),
				item2type = L"*.*";
			std::vector <WCHAR> buf (item1.capacity () + item1type.capacity () + item2.capacity () + item2type.capacity () + 5);
			strcpynull (buf.data (), item1.c_str (), buf.size ());
			strcpynull (buf.data (), item1type.c_str (), buf.size ());
			strcpynull (buf.data (), item2.c_str (), buf.size ());
			strcpynull (buf.data (), item2type.c_str (), buf.size ());
			ExploreFile (
				this->InvokeGetHWND (),
				files,
				buf.data (),
				OFN_EXPLORER | OFN_ALLOWMULTISELECT | OFN_PATHMUSTEXIST,
				GetRCStringSW (IDS_SELECT_DLGTITLE)
			);
			if (files.empty ()) return;
			for (auto &it : files) g_pkgfiles.push_back (it);
			this->PageTag = "loading";
			ThreadPackageLoadTask ();
			return;
		}
		else if (nequals (current, L"preinstall"))
		{
			if (g_pkginfo.size () == 1)
			{
				switch (insmode)
				{
					case InstallType::reinstall:
						ThreadPackageInstallTask ();
						break;
					default:
					case InstallType::normal:
					case InstallType::update:
						ThreadPackageInstallTask ();
						break;
				}
			}
			else ThreadPackageInstallTask ();
			return;
		}
		else if (nequals (current, L"installsuccess"))
		{
			std::vector <std::wnstring> appids;
			for (auto &it : g_pkginfo)
				for (auto &it_s : it.applications)
					if (!it_s [L"Id"].empty ())
						appids.emplace_back (it.identity.package_family_name + L'!' + it_s [L"Id"]);
			if (appids.size () == 1) ActivateAppxApplication (appids.at (0));
			else if (appids.size () > 1) AppListWnd::DisplayWindow ();
			else this->Close ();
		}
		else if (nequals (current, L"installfailed")) this->Close ();
		return;
		System::Windows::Forms::MessageBox::Show ("Button1 按下事件");
	}
	void OnPress_Button2 ()
	{
		std::wstring current = MPStringToStdW (pagetag);
		if (nequals (current, L"select"))
		{
			this->Close ();
			return;
		}
		else if (nequals (current, L"preinstall"))
		{
			if (g_pkginfo.size () == 1)
			{
				switch (insmode)
				{
					case InstallType::reinstall: {
						auto &pi = *g_pkginfo.begin ();
						if (pi.applications.size () == 1) ActivateAppxApplication (pi.identity.package_family_name + L"!" + pi.applications.at (0).at (L"Id"));
						else AppListWnd::DisplayWindow ();
					} break;
					default:
					case InstallType::normal:
					case InstallType::update:
						this->Close ();
						break;
				}
			}
			else this->Close ();
			return;
		}
		else if (nequals (current, L"installfailed")) this->Close ();
		return;
		System::Windows::Forms::MessageBox::Show ("Button2 按下事件");
	}
	void LaunchAppForLaunchWhenReady ()
	{
		if (!(g_wcmdflags & (DWORD)CMDPARAM::SILENT))
		{
			auto launch_when_ready = (bool)InvokeCallScriptFunction ("getLaunchWhenReady");
			if (!launch_when_ready) return;
			std::vector <std::wnstring> appids;
			for (auto &it : g_pkginfo)
				for (auto &it_s : it.applications)
					if (!it_s [L"Id"].empty ())
						appids.emplace_back (it.identity.package_family_name + L'!' + it_s [L"Id"]);
			if (appids.size () == 1) ActivateAppxApplication (appids.at (0));
			else if (appids.size () > 1) AppListWnd::DisplayWindow ();
		}
	}
	void InvokeLaunchAppForLaunchWhenReady ()
	{
		if (this->InvokeRequired) this->Invoke (gcnew Action (this, &MainHtmlWnd::LaunchAppForLaunchWhenReady));
		else LaunchAppForLaunchWhenReady ();
	}
#ifdef nequals
#undef nequals
#endif
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
	public:
	MainHtmlWnd ()
	{
		InitSize ();
		System::Windows::Forms::Application::DoEvents ();
		splash = gcnew SplashForm (
			CStringToMPString (GetSuitSplashImage ()),
			StringToColor (CStringToMPString (g_vemani.splash_screen_backgroundcolor (L"App"))),
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
						CStringToMPString (g_vemani.background_color (L"App"))
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
		if (IsNormalizeStringEquals (fname.c_str (), L"OnPress_Button1")) OnPress_Button1 ();
		else if (IsNormalizeStringEquals (fname.c_str (), L"OnPress_Button2")) OnPress_Button2 ();
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
	~MainHtmlWnd ()
	{
		if (taskbar) taskbar->Release ();
		taskbar = nullptr;
	}
};
using MainWnd = MainHtmlWnd;
std::vector <std::wstring> LoadFileListW (const std::wstring &filePath)
{
	std::vector <std::wstring> result;
	std::wifstream file (filePath);
	if (!file.is_open ()) return result;
	file.imbue (std::locale (file.getloc (), new std::codecvt_utf8_utf16 <wchar_t>));
	std::wstring line;
	while (std::getline (file, line))
	{
		if (!line.empty () && line.back () == L'\r') line.pop_back ();
		if (!line.empty () && !std::wnstring::empty (line) && IsFileExists (line)) result.push_back (line);
	}
	return result;
}

std::wstring GenerateCmdHelper ()
{
	std::wstring ret = GetRCStringSW (IDS_CMDTIP_PRETEXT) + L"\r\n";
	for (auto &it : g_argslist)
	{
		if (it.description.empty ()) continue;
		ret += L"\r\n";
		ret += L"\t" + (it.prefixs.size () ? it.prefixs.at (0) : L"") + it.commands.at (0) + L"\r\n";
		ret += L"\t" + it.description + L"\r\n";
	}
	return ret;
}
DWORD CmdMapsToFlags (std::map <cmdkey, cmdvalue> cmdpairs, std::vector <std::wnstring> &files = std::vector <std::wnstring> (), std::vector <std::wnstring> &uris = std::vector <std::wnstring> ())
{
	DWORD dwret = 0;
	for (auto &it : cmdpairs)
	{
		switch (it.first.type)
		{
			case paramtype::file: {
				if (IsFileExists (it.first.key)) push_unique (files, it.first.key);
			} break;
			case paramtype::uri: {
				push_unique (uris, it.first.key);
			} break;
			default:
			case paramtype::string: {
				auto &key = it.first;
				auto &value = it.second;
				if (key.key.equals (L"silent")) dwret |= (DWORD)CMDPARAM::SILENT;
				else if (key.key.equals (L"verysilent")) dwret |= (DWORD)CMDPARAM::SILENT;
				else if (key.key.equals (L"multiple"))
				{
					if (value.type == paramtype::file)
					{
						auto strlist = LoadFileListW (value.value);
						for (auto &it_s : strlist)
						{
							if (std::wnstring::empty (it_s)) continue;
							std::wnstring filepath = it_s;
							std::wstring listdir = GetFileDirectoryW (value.value);
							if (!IsFileExists (filepath)) filepath = ProcessEnvVars (filepath);
							if (!IsFileExists (filepath)) filepath = CombinePath (listdir, filepath);
							if (!IsFileExists (filepath)) filepath = CombinePath (listdir, it_s);
							if (!IsFileExists (filepath)) continue;
							else push_unique (files, filepath);
						}
					}
				}
				else if (key.key.equals (L"language")) SetThreadUILanguage (LocaleCodeToLcid (value.value));
			} break;
		}
	}
	if (files.size () > 1) dwret |= (DWORD)CMDPARAM::MULTIPLE;
	return dwret;
}

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
void InstallPackageTaskVerySilent ()
{
	for (auto &it : g_pkgfiles)
	{
		AddAppxPackageFromPath (it.c_str ());
	}
}
[STAThread]
int APIENTRY wWinMain (HINSTANCE hInstance, HINSTANCE hPrevInstance, LPWSTR lpCmdLine, int nCmdShow)
{
	SetCurrentProcessExplicitAppUserModelID (m_idenName);
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
		std::map <cmdkey, cmdvalue> pair_cmdkv;
		ParseCmdLine (lpCmdLine, pair_cmdkv);
		for (auto pair : pair_cmdkv)
		{
			if (pair.first.key == std::wnstring (L"help"))
			{
				MessageBoxW (nullptr, GenerateCmdHelper ().c_str (), GetRCStringSW (IDS_WINTITLE).c_str (), 0);
				return 0;
			}
		}
		g_wcmdflags = CmdMapsToFlags (pair_cmdkv, g_pkgfiles);
		if (g_wcmdflags & (DWORD)CMDPARAM::VERYSILENT)
		{
			try
			{
				InstallPackageTaskVerySilent ();
				return 0;
			}
			catch (...) { return 1; }
		}
	}
	SetWebBrowserEmulation ();
	System::Windows::Forms::Application::EnableVisualStyles ();
	System::Windows::Forms::Application::SetCompatibleTextRenderingDefault (false);
	auto mwnd = gcnew MainHtmlWnd ();
	g_mainwnd = mwnd;
	System::Windows::Forms::Application::Run (mwnd);
	return 0;
}