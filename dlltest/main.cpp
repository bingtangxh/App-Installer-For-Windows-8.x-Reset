#include <Windows.h>
#include <iostream>
#include <string>
#include <algorithm>
#include <thread>
#include <iomanip>
#include "..\priformatcli\priformatcli.h"
#include "..\pkgread\pkgread.h"
#include "..\pkgmgr\pkgmgr.h"

void read_package (const std::wstring &filepath)
{
	package_reader pr (filepath);
	pr.enable_pri_convert (true);
	pr.use_pri (true);
	std::wcout << L"Is Valid: " << (pr.valid () ? L"true" : L"false") << std::endl;
	std::wcout << L"Package Type: ";
	switch (pr.package_type ())
	{
		case PKGTYPE_APPX: std::wcout << L"Appx"; break;
		case PKGTYPE_BUNDLE: std::wcout << L"AppxBundle"; break;
	}
	std::wcout << std::endl;
	std::wcout << L"Package Role: ";
	switch (pr.package_role ())
	{
		case PKGROLE_APPLICATION: std::wcout << L"Application"; break;
		case PKGROLE_FRAMEWORK: std::wcout << L"Framework"; break;
		case PKGROLE_RESOURCE: std::wcout << L"Resource"; break;
		case PKGROLE_UNKNOWN: std::wcout << L"Unknown"; break;
	}
	std::wcout << std::endl;
	auto id = pr.get_identity ();
	std::wcout << L"Identity" << std::endl;
	std::wcout << L"\tName: " << id.name () << std::endl;
	std::wcout << L"\tPublisher: " << id.publisher () << std::endl;
	auto ver = id.version ();
	std::wcout << L"\tVersion: " << ver.major << L"." << ver.minor << L"." << ver.build << L"." << ver.revision << std::endl;
	std::wcout << L"\tPackage Family Name: " << id.package_family_name () << std::endl;
	std::wcout << L"\tPackage Full Name: " << id.package_full_name () << std::endl;
	std::wcout << L"\tResource Id: " << id.resource_id () << std::endl;
	std::wcout << L"\tArchitecture: ";
	DWORD archi = id.architecture ();
	for (size_t i = 0xF; i > 0; i /= 2)
	{
		if (archi & i)
		{
			switch (i)
			{
				case PKG_ARCHITECTURE_ARM: std::wcout << "Arm"; break;
				case PKG_ARCHITECTURE_ARM64: std::wcout << "Arm64"; break;
				case PKG_ARCHITECTURE_NEUTRAL: std::wcout << "Neutral"; break;
				case PKG_ARCHITECTURE_X64: std::wcout << "x64"; break;
				case PKG_ARCHITECTURE_X86: std::wcout << "x86"; break;
				default: continue;
			}
			std::wcout << L", ";
		}
	}
	std::wcout << std::endl;
	ver = id.version (true);
	std::wcout << L"\tVersion (Appx): " << ver.major << L"." << ver.minor << L"." << ver.build << L"." << ver.revision << std::endl;
	std::wcout << L"Properties" << std::endl;
	auto prop = pr.get_properties ();
	std::wcout << L"\tDisplay Name: " << prop.display_name () << std::endl;
	std::wcout << L"\tDescription: " << prop.description () << std::endl;
	std::wcout << L"\tPublisher Display Name: " << prop.publisher_display_name () << std::endl;
	std::wcout << L"\tLogo: " << prop.logo () << std::endl;
	std::wcout << L"\tLogo Base64: " << prop.logo_base64 () << std::endl;
	std::wcout << L"\tFramework: " << (prop.framework () ? L"true" : L"false") << std::endl;
	std::wcout << L"\tResource Package: " << (prop.resource_package () ? L"true" : L"false") << std::endl;
	auto preq = pr.get_prerequisites ();
	std::wcout << L"Prerequisites" << std::endl;
	ver = preq.os_min_version ();
	std::wcout << L"\tOS Min Version: " << ver.major << L"." << ver.minor << L"." << ver.build << L"." << ver.revision << std::endl;
	ver = preq.os_max_version_tested ();
	std::wcout << L"\tOS Max Version Tested: " << ver.major << L"." << ver.minor << L"." << ver.build << L"." << ver.revision << std::endl;
	std::wcout << L"Resources" << std::endl;
	auto res = pr.get_resources ();
	{
		std::vector <std::wstring> langs;
		std::vector <UINT32> scales;
		std::vector <DWORD> dx_levels;
		res.languages (langs);
		res.scales (scales);
		res.dx_feature_level (dx_levels);
		std::wcout << L"\tSupported Languages: ";
		for (auto &it : langs)
		{
			std::wcout << it << L", ";
		}
		std::wcout << std::endl;
		std::wcout << L"\tSupported Scales: ";
		for (auto &it : scales)
		{
			std::wcout << it << L", ";
		}
		std::wcout << std::endl;
		std::wcout << L"\tSupported Languages: ";
		for (auto &it : dx_levels)
		{
			switch (it)
			{
				case PKG_RESOURCES_DXFEATURE_LEVEL9: std::wcout << L"9, "; break;
				case PKG_RESOURCES_DXFEATURE_LEVEL10: std::wcout << L"10, "; break;
				case PKG_RESOURCES_DXFEATURE_LEVEL11: std::wcout << L"11, "; break;
				case PKG_RESOURCES_DXFEATURE_LEVEL12: std::wcout << L"12, "; break;
			}
		}
		std::wcout << std::endl;
	}
	std::wcout << L"Capabilities" << std::endl;
	auto cap = pr.get_capabilities ();
	{
		std::vector <std::wstring> caps;
		std::vector <std::wstring> devicecaps;
		cap.capabilities_name (caps);
		cap.device_capabilities (devicecaps);
		std::wcout << L"\tCapabilities: ";
		for (auto &it : caps)
		{
			std::wcout << it << L", ";
		}
		std::wcout << std::endl;
		std::wcout << L"\tDevice Capabilities: ";
		for (auto &it : devicecaps)
		{
			std::wcout << it << L", ";
		}
		std::wcout << std::endl;
	}
	std::wcout << L"Applications" << std::endl;
	auto apps = pr.get_applications ();
	{
		std::vector <package_reader::application> appmaps;
		apps.get (appmaps);
		size_t cnt = 1;
		for (auto &it : appmaps)
		{
			std::wcout << L"\tApplication" << cnt ++ << std::endl;
			for (auto &it_s : it)
			{
				std::wcout << L"\t\t" << it_s.first << L": " << it.newat (it_s.first) << std::endl;
				std::wstring base64 = it.newat_base64 (it_s.first);
				if (!base64.empty ()) std::wcout << L"\t\t" << it_s.first << L" (Base64): " << base64 << std::endl;
			}
		}
	}
	auto deps = pr.get_dependencies ();
	std::wcout << L"Dependencies" << std::endl;
	{
		std::vector <package_reader::dependency> deparr;
		deps.get (deparr);
		size_t cnt = 1;
		for (auto &it : deparr)
		{
			std::wcout << L"\tDependency" << cnt ++ << std::endl;
			std::wcout << L"\t\tName: " << it.name << std::endl;
			std::wcout << L"\t\tPublisher: " << it.publisher << std::endl;
			ver = it.vermin;
			std::wcout << L"\t\tMin Version: " << ver.major << L"." << ver.minor << L"." << ver.build << L"." << ver.revision << std::endl;
		}
	}
}
void read_manifest (const std::wstring &filepath)
{
	manifest_reader pr (filepath);
	pr.enable_pri_convert (true);
	pr.use_pri (true);
	std::wcout << L"Is Valid: " << (pr.valid () ? L"true" : L"false") << std::endl;
	std::wcout << L"Package Type: ";
	switch (pr.package_type ())
	{
		case PKGTYPE_APPX: std::wcout << L"Appx"; break;
		case PKGTYPE_BUNDLE: std::wcout << L"AppxBundle"; break;
	}
	std::wcout << std::endl;
	std::wcout << L"Package Role: ";
	switch (pr.package_role ())
	{
		case PKGROLE_APPLICATION: std::wcout << L"Application"; break;
		case PKGROLE_FRAMEWORK: std::wcout << L"Framework"; break;
		case PKGROLE_RESOURCE: std::wcout << L"Resource"; break;
		case PKGROLE_UNKNOWN: std::wcout << L"Unknown"; break;
	}
	std::wcout << std::endl;
	auto id = pr.get_identity ();
	std::wcout << L"Identity" << std::endl;
	std::wcout << L"\tName: " << id.name () << std::endl;
	std::wcout << L"\tPublisher: " << id.publisher () << std::endl;
	auto ver = id.version ();
	std::wcout << L"\tVersion: " << ver.major << L"." << ver.minor << L"." << ver.build << L"." << ver.revision << std::endl;
	std::wcout << L"\tPackage Family Name: " << id.package_family_name () << std::endl;
	std::wcout << L"\tPackage Full Name: " << id.package_full_name () << std::endl;
	std::wcout << L"\tResource Id: " << id.resource_id () << std::endl;
	std::wcout << L"\tArchitecture: ";
	DWORD archi = id.architecture ();
	for (size_t i = 0xF; i > 0; i /= 2)
	{
		if (archi & i)
		{
			switch (i)
			{
				case PKG_ARCHITECTURE_ARM: std::wcout << "Arm"; break;
				case PKG_ARCHITECTURE_ARM64: std::wcout << "Arm64"; break;
				case PKG_ARCHITECTURE_NEUTRAL: std::wcout << "Neutral"; break;
				case PKG_ARCHITECTURE_X64: std::wcout << "x64"; break;
				case PKG_ARCHITECTURE_X86: std::wcout << "x86"; break;
				default: continue;
			}
			std::wcout << L", ";
		}
	}
	std::wcout << std::endl;
	ver = id.version (true);
	std::wcout << L"\tVersion (Appx): " << ver.major << L"." << ver.minor << L"." << ver.build << L"." << ver.revision << std::endl;
	std::wcout << L"Properties" << std::endl;
	auto prop = pr.get_properties ();
	std::wcout << L"\tDisplay Name: " << prop.display_name () << std::endl;
	std::wcout << L"\tDescription: " << prop.description () << std::endl;
	std::wcout << L"\tPublisher Display Name: " << prop.publisher_display_name () << std::endl;
	std::wcout << L"\tLogo: " << prop.logo () << std::endl;
	std::wcout << L"\tLogo Base64: " << prop.logo_base64 () << std::endl;
	std::wcout << L"\tFramework: " << (prop.framework () ? L"true" : L"false") << std::endl;
	std::wcout << L"\tResource Package: " << (prop.resource_package () ? L"true" : L"false") << std::endl;
	auto preq = pr.get_prerequisites ();
	std::wcout << L"Prerequisites" << std::endl;
	ver = preq.os_min_version ();
	std::wcout << L"\tOS Min Version: " << ver.major << L"." << ver.minor << L"." << ver.build << L"." << ver.revision << std::endl;
	ver = preq.os_max_version_tested ();
	std::wcout << L"\tOS Max Version Tested: " << ver.major << L"." << ver.minor << L"." << ver.build << L"." << ver.revision << std::endl;
	std::wcout << L"Resources" << std::endl;
	auto res = pr.get_resources ();
	{
		std::vector <std::wstring> langs;
		std::vector <UINT32> scales;
		std::vector <DWORD> dx_levels;
		res.languages (langs);
		res.scales (scales);
		res.dx_feature_level (dx_levels);
		std::wcout << L"\tSupported Languages: ";
		for (auto &it : langs)
		{
			std::wcout << it << L", ";
		}
		std::wcout << std::endl;
		std::wcout << L"\tSupported Scales: ";
		for (auto &it : scales)
		{
			std::wcout << it << L", ";
		}
		std::wcout << std::endl;
		std::wcout << L"\tSupported Languages: ";
		for (auto &it : dx_levels)
		{
			switch (it)
			{
				case PKG_RESOURCES_DXFEATURE_LEVEL9: std::wcout << L"9, "; break;
				case PKG_RESOURCES_DXFEATURE_LEVEL10: std::wcout << L"10, "; break;
				case PKG_RESOURCES_DXFEATURE_LEVEL11: std::wcout << L"11, "; break;
				case PKG_RESOURCES_DXFEATURE_LEVEL12: std::wcout << L"12, "; break;
			}
		}
		std::wcout << std::endl;
	}
	std::wcout << L"Capabilities" << std::endl;
	auto cap = pr.get_capabilities ();
	{
		std::vector <std::wstring> caps;
		std::vector <std::wstring> devicecaps;
		cap.capabilities_name (caps);
		cap.device_capabilities (devicecaps);
		std::wcout << L"\tCapabilities: ";
		for (auto &it : caps)
		{
			std::wcout << it << L", ";
		}
		std::wcout << std::endl;
		std::wcout << L"\tDevice Capabilities: ";
		for (auto &it : devicecaps)
		{
			std::wcout << it << L", ";
		}
		std::wcout << std::endl;
	}
	std::wcout << L"Applications" << std::endl;
	auto apps = pr.get_applications ();
	{
		std::vector <manifest_reader::application> appmaps;
		apps.get (appmaps);
		size_t cnt = 1;
		for (auto &it : appmaps)
		{
			std::wcout << L"\tApplication" << cnt ++ << std::endl;
			for (auto &it_s : it)
			{
				std::wcout << L"\t\t" << it_s.first << L": " << it.newat (it_s.first) << std::endl;
				std::wstring base64 = it.newat_base64 (it_s.first);
				if (!base64.empty ()) std::wcout << L"\t\t" << it_s.first << L" (Base64): " << base64 << std::endl;
			}
		}
	}
	auto deps = pr.get_dependencies ();
	std::wcout << L"Dependencies" << std::endl;
	{
		std::vector <manifest_reader::dependency> deparr;
		deps.get (deparr);
		size_t cnt = 1;
		for (auto &it : deparr)
		{
			std::wcout << L"\tDependency" << cnt ++ << std::endl;
			std::wcout << L"\t\tName: " << it.name << std::endl;
			std::wcout << L"\t\tPublisher: " << it.publisher << std::endl;
			ver = it.vermin;
			std::wcout << L"\t\tMin Version: " << ver.major << L"." << ver.minor << L"." << ver.build << L"." << ver.revision << std::endl;
		}
	}
}
using cbfunc = std::function <void (int progress)>;
void ProgressCallback (DWORD dwProgress, void *pCustom)
{
	if (auto func = reinterpret_cast <cbfunc *> (pCustom)) (*func)(dwProgress);
}
HRESULT AddAppxPackage (const std::wstring path, cbfunc callback, std::wstring &errorcode, std::wstring &detailmsg)
{
	LPWSTR ec = nullptr, dm = nullptr;
	struct reltask
	{
		using endfunc = std::function <void ()>;
		endfunc endtask = nullptr;
		reltask (endfunc et): endtask (et) {}
		~reltask () { if (endtask) endtask (); }
	} relt ([=] () {
		if (ec) free (ec);
		if (dm) free (dm);
	});
	HRESULT hr = AddAppxPackageFromPath (path.c_str (), nullptr, DEPOLYOPTION_NONE, ProgressCallback, &callback, &ec, &dm);
	errorcode.clear ();
	detailmsg.clear ();
	if (ec) errorcode = ec;
	if (dm) detailmsg = dm;
	return hr;
}
int main (int argc, char *argv [])
{
	setlocale (LC_ALL, "");
	std::wcout.imbue (std::locale ("", LC_CTYPE));
	CoInitializeEx (nullptr, COINIT_MULTITHREADED);
	std::wcout << L"Please enter the file path: " << std::endl;
	std::wcout << L"\\> ";
	std::wstring pkgPathStr = L"E:\\Profiles\\Bruce\\Desktop\\Discourse.appx";
	//pkgPathStr = L"F:\\BaiduNetdiskDownload\\Collection4\\Microsoft.BingFinance_2015.709.2014.2069_neutral_~_8wekyb3d8bbwe\\FinanceApp_3.0.4.336_x86.appx";
	//pkgPathStr = L"F:\\BaiduNetdiskDownload\\Collection4\\Microsoft.BingFinance_2015.709.2014.2069_neutral_~_8wekyb3d8bbwe.appxbundle";
	//pkgPathStr = L"E:\\Profiles\\Bruce\\Desktop\\新建文件夹\\Microsoft.MSIXPackagingTool_2023.1212.538.0_neutral_~_8wekyb3d8bbwe.Msixbundle";
	pkgPathStr = L"C:\\Program Files\\WindowsApps\\Microsoft.3DBuilder_20.0.4.0_x64__8wekyb3d8bbwe\\AppxManifest.xml";
	if (pkgPathStr.empty ()) std::getline (std::wcin, pkgPathStr);
	pkgPathStr.erase (
		std::remove (pkgPathStr.begin (), pkgPathStr.end (), L'\"'),
		pkgPathStr.end ()
	);
	//read_package (pkgPathStr);
	read_manifest (pkgPathStr);
	system ("pause");
	return 0;
}