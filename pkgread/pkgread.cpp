// pkgread.cpp : 定义 DLL 应用程序的导出函数。
//

#include "stdafx.h"
#include "readobj.h"
#include "pkgread.h"
#include "localeex.h"
#include "themeinfo.h"

#define ToHandleRead(_cpp_ptr_) reinterpret_cast <HPKGREAD> (_cpp_ptr_) 
#define ToPtrPackage(_cpp_ptr_) reinterpret_cast <package *> (_cpp_ptr_)
HPKGREAD CreatePackageReader () { return ToHandleRead (new package ()); }
BOOL LoadPackageFromFile (_In_ HPKGREAD hReader, _In_ LPCWSTR lpFilePath)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return false;
	return ptr->create (lpFilePath);
}
void DestroyPackageReader (_In_ HPKGREAD hReader)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return;
	return delete ptr;
}
WORD GetPackageType (_In_ HPKGREAD hReader)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return PKGTYPE_UNKNOWN;
 	switch (ptr->type ())
	{
		case PackageType::unknown: return PKGTYPE_UNKNOWN;
		case PackageType::single: return PKGTYPE_APPX;
		case PackageType::bundle: return PKGTYPE_BUNDLE;
	}
	return PKGTYPE_UNKNOWN;
}
BOOL IsPackageValid (_In_ HPKGREAD hReader)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return false;
	return ptr->valid ();
}
WORD GetPackageRole (_In_ HPKGREAD hReader)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return PKGROLE_UNKNOWN;
	switch (ptr->type ())
	{
		case PackageType::unknown: return PKGROLE_UNKNOWN;
		case PackageType::bundle: return PKGROLE_APPLICATION;
		case PackageType::single: {
			auto ar = ptr->appx_reader ();
			switch (ar.package_role ())
			{
				case PackageRole::unknown: return PKGROLE_UNKNOWN;
				case PackageRole::application: return PKGROLE_APPLICATION;
				case PackageRole::framework: return PKGROLE_FRAMEWORK;
				case PackageRole::resource: return PKGROLE_RESOURCE;
			}
		} break;
	}
	return PKGROLE_UNKNOWN;
}

// Identity
VERSION VersionClassToStruct (const version &v)
{
	VERSION sv;
	sv.major = v.major;
	sv.minor = v.minor;
	sv.build = v.build;
	sv.revision = v.revision;
	return sv;
}
LPWSTR GetPackageIdentityStringValue (_In_ HPKGREAD hReader, _In_ DWORD dwName)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return nullptr;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			auto id = reader.identity ();
			switch (LOWORD (dwName))
			{
				case PKG_IDENTITY_NAME: return _wcsdup (id.name ().c_str ());
				case PKG_IDENTITY_PUBLISHER: return _wcsdup (id.publisher ().c_str ());
				case PKG_IDENTITY_PACKAGEFAMILYNAME: return _wcsdup (id.package_family_name ().c_str ());
				case PKG_IDENTITY_PACKAGEFULLNAME: return _wcsdup (id.package_full_name ().c_str ());
				case PKG_IDENTITY_RESOURCEID: return _wcsdup (id.resource_id ().c_str ());
			}
		} break;
		case PackageType::bundle: {
			auto reader = ptr->bundle_reader ();
			auto id = reader.identity ();
			if (HIWORD (dwName) == PKGTYPE_BUNDLE)
			{
				CComPtr <IAppxPackageReader> apkg;
				HRESULT hr = reader.random_application_package (&apkg);
				if (SUCCEEDED (hr))
				{
					appxreader aread (apkg.p);
					auto aid = aread.identity ();
					switch (LOWORD (dwName))
					{
						case PKG_IDENTITY_NAME: return _wcsdup (aid.name ().c_str ());
						case PKG_IDENTITY_PUBLISHER: return _wcsdup (aid.publisher ().c_str ());
						case PKG_IDENTITY_PACKAGEFAMILYNAME: return _wcsdup (aid.package_family_name ().c_str ());
						case PKG_IDENTITY_PACKAGEFULLNAME: return _wcsdup (aid.package_full_name ().c_str ());
						case PKG_IDENTITY_RESOURCEID: return _wcsdup (aid.resource_id ().c_str ());
					}
				}
			}
			else
			{
				switch (LOWORD (dwName))
				{
					case PKG_IDENTITY_NAME: return _wcsdup (id.name ().c_str ());
					case PKG_IDENTITY_PUBLISHER: return _wcsdup (id.publisher ().c_str ());
					case PKG_IDENTITY_PACKAGEFAMILYNAME: return _wcsdup (id.package_family_name ().c_str ());
					case PKG_IDENTITY_PACKAGEFULLNAME: return _wcsdup (id.package_full_name ().c_str ());
					case PKG_IDENTITY_RESOURCEID: return _wcsdup (id.resource_id ().c_str ());
				}
			}
		} break;
	}
	return nullptr;
}
BOOL GetPackageIdentityVersion (_In_ HPKGREAD hReader, _Out_ VERSION *pVersion, _In_ BOOL bGetSubPkgVer)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return FALSE;
	if (!pVersion) return FALSE;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			auto id = reader.identity ();
			auto ver = id.version ();
			*pVersion = VersionClassToStruct (ver);
			return !ver.empty ();
		} break;
		case PackageType::bundle: {
			auto reader = ptr->bundle_reader ();
			auto id = reader.identity ();
			if (bGetSubPkgVer)
			{
				CComPtr <IAppxPackageReader> apkg;
				HRESULT hr = reader.random_application_package (&apkg);
				if (SUCCEEDED (hr))
				{
					appxreader aread (apkg.p);
					auto aid = aread.identity ();
					auto ver = aid.version ();
					*pVersion = VersionClassToStruct (ver);
					return !ver.empty ();
				}
			}
			else
			{
				auto ver = id.version ();
				*pVersion = VersionClassToStruct (ver);
				return !ver.empty ();
			}
		} break;
	}
	return FALSE;
}
BOOL GetPackageIdentityArchitecture (_In_ HPKGREAD hReader, _Out_ DWORD *pdwArchi)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return FALSE;
	if (!pdwArchi) return FALSE;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			auto id = reader.identity ();
			auto archi = id.architecture ();
			DWORD ret = 0;
			switch (archi)
			{
				case APPX_PACKAGE_ARCHITECTURE::APPX_PACKAGE_ARCHITECTURE_X86: 
					ret = PKG_ARCHITECTURE_X86; break;
				case APPX_PACKAGE_ARCHITECTURE::APPX_PACKAGE_ARCHITECTURE_ARM:
					ret = PKG_ARCHITECTURE_ARM; break;
				case APPX_PACKAGE_ARCHITECTURE::APPX_PACKAGE_ARCHITECTURE_X64:
					ret = PKG_ARCHITECTURE_X64; break;
				case APPX_PACKAGE_ARCHITECTURE::APPX_PACKAGE_ARCHITECTURE_NEUTRAL:
					ret = PKG_ARCHITECTURE_NEUTRAL; break;
				case (APPX_PACKAGE_ARCHITECTURE)12: // ARM64
					ret = PKG_ARCHITECTURE_ARM64; break;
			}
			*pdwArchi = ret;
			return ret != PKG_ARCHITECTURE_UNKNOWN;
		} break;
		case PackageType::bundle: {
			auto reader = ptr->bundle_reader ();
			DWORD ret = 0;
			size_t size = reader.application_packages ([&ret] (IAppxPackageReader *p) {
				appxreader aread (p);
				auto id = aread.identity ();
				auto archi = id.architecture ();
				switch (archi)
				{
					case APPX_PACKAGE_ARCHITECTURE::APPX_PACKAGE_ARCHITECTURE_X86:
						ret |= PKG_ARCHITECTURE_X86; break;
					case APPX_PACKAGE_ARCHITECTURE::APPX_PACKAGE_ARCHITECTURE_ARM:
						ret |= PKG_ARCHITECTURE_ARM; break;
					case APPX_PACKAGE_ARCHITECTURE::APPX_PACKAGE_ARCHITECTURE_X64:
						ret |= PKG_ARCHITECTURE_X64; break;
					case APPX_PACKAGE_ARCHITECTURE::APPX_PACKAGE_ARCHITECTURE_NEUTRAL:
						ret |= PKG_ARCHITECTURE_NEUTRAL; break;
					case (APPX_PACKAGE_ARCHITECTURE)12: // ARM64
						ret |= PKG_ARCHITECTURE_ARM64; break;
				}
			});
			*pdwArchi = ret;
			return ret != PKG_ARCHITECTURE_UNKNOWN;
		} break;
	}
	return FALSE;
}

// Properties
LPWSTR GetPackagePropertiesStringValue (_In_ HPKGREAD hReader, _In_ LPCWSTR lpName)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return nullptr;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			auto prop = reader.properties ();
			return _wcsdup (prop.string_value (lpName ? lpName : L"").c_str ());
		} break;
		case PackageType::bundle: {
			auto reader = ptr->bundle_reader ();
			CComPtr <IAppxPackageReader> apppkg;
			if (SUCCEEDED (reader.random_application_package (&apppkg)))
			{
				appxreader apkg (apppkg.p);
				auto prop = apkg.properties ();
				return _wcsdup (prop.string_value (lpName ? lpName : L"").c_str ());
			}
		} break;
	}
	return nullptr;
}
HRESULT GetPackagePropertiesBoolValue (_In_ HPKGREAD hReader, _In_ LPCWSTR lpName, _Outptr_ BOOL *pRet)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return E_INVALIDARG;
	if (!pRet) return E_INVALIDARG;
	*pRet = FALSE;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			CComPtr <IAppxManifestReader> m;
			HRESULT hr = reader.manifest (&m);
			if (FAILED (hr)) return hr;
			CComPtr <IAppxManifestProperties> p;
			hr = m->GetProperties (&p);
			if (FAILED (hr)) return hr;
			return p->GetBoolValue (lpName, pRet);
		} break;
		case PackageType::bundle: {
			auto reader = ptr->bundle_reader ();
			CComPtr <IAppxPackageReader> apppkg;
			HRESULT hr = S_OK;
			if (SUCCEEDED (hr = reader.random_application_package (&apppkg)))
			{
				appxreader apkg (apppkg.p);
				CComPtr <IAppxManifestReader> m;
				hr = apkg.manifest (&m);
				if (FAILED (hr)) return hr;
				CComPtr <IAppxManifestProperties> p;
				hr = m->GetProperties (&p);
				if (FAILED (hr)) return hr;
				return p->GetBoolValue (lpName, pRet);
			}
			else return hr;
		} break;
	}
	return E_FAIL;
}

// Applications
BOOL AddPackageApplicationItemGetName (_In_ LPCWSTR lpName)
{
	if (std::wnstring (lpName ? lpName : L"").empty ()) return FALSE;
	return PushApplicationAttributeItem (lpName);
}
BOOL RemovePackageApplicationItemGetName (_In_ LPCWSTR lpName)
{
	if (std::wnstring (lpName ? lpName : L"").empty ()) return FALSE;
	return RemoveApplicationAttributeItem (lpName);
}
HLIST_PVOID GetAllApplicationItemsName ()
{
	size_t len = sizeof (LIST_PVOID) + sizeof (LPCWSTR) * appitems.size ();
	HLIST_PVOID hList = (HLIST_PVOID)malloc (len);
	ZeroMemory (hList, len);
	hList->dwSize = 0;
	try
	{
		for (auto &it : appitems)
		{
			((LPCWSTR *)hList->alpVoid) [hList->dwSize ++] = (LPCWSTR)it.c_str ();
		}
	}
	catch (const std::exception &e) {}
	return hList;
}
void DestroyApplicationItemsName (_In_ HLIST_PVOID hList)
{
	if (!hList) return;
	free (hList);
}
#define ToHandleAppEnumerator(_cpp_ptr_) reinterpret_cast <HAPPENUMERATOR> (_cpp_ptr_)
typedef std::vector <app_info> app_enumerator;
#define ToPtrAppxApps(_cpp_ptr_) reinterpret_cast <app_enumerator *> (_cpp_ptr_)
HAPPENUMERATOR GetPackageApplications (_In_ HPKGREAD hReader)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return nullptr;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			auto app = reader.applications ();
			auto appvec = new app_enumerator ();
			app.applications (*appvec);
			return ToHandleAppEnumerator (appvec);
		} break;
		case PackageType::bundle: {
			auto br = ptr->bundle_reader ();
			CComPtr <IAppxPackageReader> pr;
			HRESULT hr = br.random_application_package (&pr);
			if (SUCCEEDED (hr))
			{
				appxreader reader (pr.p);
				auto app = reader.applications ();
				auto appvec = new app_enumerator ();
				app.applications (*appvec);
				return ToHandleAppEnumerator (appvec);
			}
		} break;
	}
	return nullptr;
}
HLIST_PVOID ApplicationsToMap (_In_ HAPPENUMERATOR hEnumerator)
{
	auto ptr = ToPtrAppxApps (hEnumerator);
	if (!ptr || ptr->empty ()) return nullptr;
	size_t count = ptr->size ();
	size_t len = sizeof (LIST_PVOID) + sizeof (LPVOID) * count;
	auto list = (HLIST_PVOID)malloc (len);
	ZeroMemory (list, len);
	if (!list) return nullptr;
	list->dwSize = 0;
	bool outer_throw = false;
	raii list_guard ([&] () {
		if (outer_throw)
		{
			for (DWORD i = 0; i < list->dwSize; ++i)
			{
				auto map = (HLIST_PVOID)list->alpVoid [i];
				if (map)
				{
					for (DWORD j = 0; j < map->dwSize; ++j)
					{
						auto pair = (HPAIR_PVOID)map->alpVoid [j];
						if (pair)
						{
							free (pair);
							map->alpVoid [j] = nullptr;
						}
					}
					free (map);
					list->alpVoid [i] = nullptr;
				}
			}
			free (list);
			list = nullptr;
		}
	});
	try
	{
		for (auto &app : *ptr)
		{
			size_t map_size = app.size ();
			size_t mlen = sizeof (LIST_PVOID) + sizeof (LPVOID) * map_size;
			auto map = (HLIST_PVOID)malloc (mlen);
			ZeroMemory (map, mlen);
			if (!map)
			{
				outer_throw = true;
				throw std::bad_alloc ();
			}
			map->dwSize = 0;
			bool inner_throw = false;
			raii map_guard ([&] () {
				if (inner_throw)
				{
					for (DWORD j = 0; j < map->dwSize; ++j)
					{
						auto pair = (HPAIR_PVOID)map->alpVoid [j];
						if (pair)
						{
							free (pair);
							map->alpVoid [j] = nullptr;
						}
					}
					free (map);
					map = nullptr;
				}
			});
			try
			{
				for (auto &kv : app)
				{
					auto pair = (HPAIR_PVOID)malloc (sizeof (PAIR_PVOID));
					ZeroMemory (pair, sizeof (PAIR_PVOID));
					if (!pair)
					{
						inner_throw = true;
						throw std::bad_alloc ();
					}
					pair->lpKey = (LPVOID)kv.first.c_str ();
					pair->lpValue = (LPVOID)kv.second.c_str ();
					map->alpVoid [map->dwSize++] = pair;
				}
			}
			catch (const std::exception &)
			{
				inner_throw = true;
				throw;
			}
			list->alpVoid [list->dwSize ++] = map;
		}
	}
	catch (const std::exception &) { outer_throw = true; }
	if (outer_throw) return nullptr;
	return list;
}
void DestroyApplicationsMap (_In_ HLIST_PVOID hEnumerator)
{
	if (!hEnumerator) return;
	HLIST_PVOID list = hEnumerator;
	for (DWORD i = 0; i < list->dwSize; ++ i)
	{
		HLIST_PVOID map = (HLIST_PVOID)list->alpVoid [i];
		if (!map) continue;
		for (DWORD j = 0; j < map->dwSize; ++ j)
		{
			HPAIR_PVOID pair = (HPAIR_PVOID)map->alpVoid [j];
			if (pair)
			{
				free (pair);
				map->alpVoid [j] = nullptr;
			}
		}
		free (map);
		list->alpVoid [i] = nullptr;
	}
	free (list);
}
void DestroyPackageApplications (_In_ HAPPENUMERATOR hEnumerator)
{
	auto ptr = ToPtrAppxApps (hEnumerator);
	if (ptr) delete ptr;
}

// Resources
HLIST_PVOID GetResourcesLanguages (_In_ HPKGREAD hReader)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return nullptr;
	std::vector <std::wnstring> langs;
	switch (ptr->type ()) 
	{
		case PackageType::single:
		{
			auto reader = ptr->appx_reader ();
			auto res = reader.resources ();
			res.languages (langs);
			break;
		}
		case PackageType::bundle:
		{
			auto br = ptr->bundle_reader ();
			auto res = br.package_id_items ();
			res.enumerate ([&langs] (IAppxBundleManifestPackageInfo *inf) {
				auto item = appx_info::appx_iditem (inf);
				std::vector <std::wnstring> l;
				auto qr = item.qualified_resources ();
				qr.languages (l);
				for (auto &it : l) push_unique <std::wnstring> (langs, it);
			});
			break;
		}
		default: return nullptr;
	}
	if (langs.empty ()) return nullptr;
	size_t count = langs.size ();
	size_t bytes = sizeof (LIST_PVOID) + sizeof (LPWSTR) * (count - 1);
	auto list = (HLIST_PVOID)malloc (bytes);
	ZeroMemory (list, bytes);
	if (!list) return nullptr;
	list->dwSize = 0;
	for (auto &it : langs) list->alpVoid [list->dwSize ++] = _wcsdup (it.c_str ());
	return list;
}
HLIST_LCID GetResourcesLanguagesToLcid (_In_ HPKGREAD hReader)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return nullptr;
	std::vector <std::wnstring> langs;
	switch (ptr->type ())
	{
		case PackageType::single:
		{
			auto reader = ptr->appx_reader ();
			auto res = reader.resources ();
			res.languages (langs);
			break;
		}
		case PackageType::bundle:
		{
			auto br = ptr->bundle_reader ();
			auto res = br.package_id_items ();
			res.enumerate ([&langs] (IAppxBundleManifestPackageInfo *inf) {
				auto item = appx_info::appx_iditem (inf);
				std::vector <std::wnstring> l;
				auto qr = item.qualified_resources ();
				qr.languages (l);
				for (auto &it : l) push_unique <std::wnstring> (langs, it);
			});
			break;
		}
		default: return nullptr;
	}
	if (langs.empty ()) return nullptr;
	size_t len = sizeof (LIST_LCID) + sizeof (LCID) * langs.size ();
	HLIST_LCID hList = (HLIST_LCID)malloc (len);
	ZeroMemory (hList, len);
	hList->dwSize = 0;
	for (auto &it : langs)
	{
		LCID lcid = LocaleCodeToLcid (it);
		if (lcid)
		{
			hList->aLcid [hList->dwSize ++] = lcid;
		}
	}
	return hList;
}
HLIST_UINT32 GetResourcesScales (_In_ HPKGREAD hReader)

{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return nullptr;
	std::vector <UINT32> scales;
	switch (ptr->type ())
	{
		case PackageType::single:
		{
			auto reader = ptr->appx_reader ();
			auto res = reader.resources ();
			res.scales (scales);
			break;
		}
		case PackageType::bundle:
		{
			auto br = ptr->bundle_reader ();
			auto res = br.package_id_items ();
			res.enumerate ([&scales] (IAppxBundleManifestPackageInfo *inf) {
				auto item = appx_info::appx_iditem (inf);
				std::vector <UINT32> s;
				auto qr = item.qualified_resources ();
				qr.scales (s);
				for (auto &it : s) push_unique (scales, it);
			});
			break;
		}
		default: return nullptr;
	}
	if (scales.empty ()) return nullptr;
	size_t len = sizeof (LIST_UINT32) + sizeof (UINT32) * scales.size ();
	HLIST_UINT32 hList = (HLIST_UINT32)malloc (len);
	ZeroMemory (hList, len);
	hList->dwSize = 0;
	for (auto &it : scales)
	{
		if (!it) continue;
		hList->aUI32 [hList->dwSize ++] = it;
	}
	return hList;
}
DWORD GetResourcesDxFeatureLevels (_In_ HPKGREAD hReader)

{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return 0;
	DWORD dwFlags = 0;
	switch (ptr->type ())
	{
		case PackageType::single:
		{
			auto reader = ptr->appx_reader ();
			auto res = reader.resources ();
			std::vector <DX_FEATURE_LEVEL> dxlevels;
			res.dx_feature_level (dxlevels);
			for (auto &it : dxlevels)
			{
				switch (it)
				{
					case DX_FEATURE_LEVEL::DX_FEATURE_LEVEL_9:
						dwFlags |= PKG_RESOURCES_DXFEATURE_LEVEL9; break;
					case DX_FEATURE_LEVEL::DX_FEATURE_LEVEL_10:
						dwFlags |= PKG_RESOURCES_DXFEATURE_LEVEL10; break;
					case DX_FEATURE_LEVEL::DX_FEATURE_LEVEL_11:
						dwFlags |= PKG_RESOURCES_DXFEATURE_LEVEL11; break;
					case (DX_FEATURE_LEVEL)4:
						dwFlags |= PKG_RESOURCES_DXFEATURE_LEVEL12; break;
				}
			}
			break;
		}
		case PackageType::bundle:
		{
			auto br = ptr->bundle_reader ();
			auto res = br.package_id_items ();
			res.enumerate ([&dwFlags] (IAppxBundleManifestPackageInfo *inf) {
				auto item = appx_info::appx_iditem (inf);
				std::vector <DX_FEATURE_LEVEL> dxlevels;
				auto qr = item.qualified_resources ();
				qr.dx_feature_level (dxlevels);
				for (auto &it : dxlevels)
				{
					switch (it)
					{
						case DX_FEATURE_LEVEL::DX_FEATURE_LEVEL_9:
							dwFlags |= PKG_RESOURCES_DXFEATURE_LEVEL9; break;
						case DX_FEATURE_LEVEL::DX_FEATURE_LEVEL_10:
							dwFlags |= PKG_RESOURCES_DXFEATURE_LEVEL10; break;
						case DX_FEATURE_LEVEL::DX_FEATURE_LEVEL_11:
							dwFlags |= PKG_RESOURCES_DXFEATURE_LEVEL11; break;
						case (DX_FEATURE_LEVEL)4:
							dwFlags |= PKG_RESOURCES_DXFEATURE_LEVEL12; break;
					}
				}
			});
			break;
		}
		default: return 0;
	}
	return dwFlags;
}
void DestroyResourcesLanguagesList (_In_ HLIST_PVOID hList)
{
	if (!hList) return;
	for (DWORD i = 0; i < hList->dwSize; i++)
	{
		if (hList->alpVoid [i])
		{
			free (hList->alpVoid [i]);
			hList->alpVoid [i] = nullptr;
		}
	}
	free (hList);
}
void DestroyResourcesLanguagesLcidList (_In_ HLIST_LCID hList)
{
	if (!hList) return;
	free (hList);
}
void DestroyUInt32List (_In_ HLIST_UINT32 hList)
{
	if (!hList) return;
	free (hList); 
}

// Dependencies
HLIST_DEPINFO GetDependencesInfoList (_In_ HPKGREAD hReader)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return nullptr;
	std::vector <dep_info> vec;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			auto deps = reader.dependencies ();
			deps.dependencies (vec);
		} break;
		case PackageType::bundle: {
			auto br = ptr->bundle_reader ();
			CComPtr <IAppxPackageReader> pr;
			HRESULT hr = br.random_application_package (&pr);
			if (FAILED (hr)) return nullptr;
			appxreader reader (pr.p);
			auto deps = reader.dependencies ();
			deps.dependencies (vec);
		}
	} 
	size_t len = sizeof (LIST_DEPINFO) + sizeof (DEPENDENCY_INFO) * vec.size ();
	HLIST_DEPINFO hList = (HLIST_DEPINFO)malloc (len);
	ZeroMemory (hList, len);
	hList->dwSize = 0;
	for (auto &it : vec)
	{
		auto &dep = hList->aDepInfo [hList->dwSize ++];
		dep.lpName = _wcsdup (it.name.c_str ());
		dep.lpPublisher = _wcsdup (it.publisher.c_str ());
		dep.verMin = VersionClassToStruct (it.minversion);
	}
	return hList;
}
void DestroyDependencesInfoList (_In_ HLIST_DEPINFO hList)
{
	if (!hList) return;
	for (DWORD i = 0; i < hList->dwSize; i ++)
	{
		auto &dep = hList->aDepInfo [i];
		if (dep.lpName) { free (dep.lpName); dep.lpName = nullptr; }
		if (dep.lpPublisher) { free (dep.lpPublisher); dep.lpPublisher = nullptr; }
	}
	free (hList);
}

// Capabilities
HLIST_PVOID GetCapabilitiesList (_In_ HPKGREAD hReader)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return nullptr;
	std::vector <std::wnstring> caps;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			auto cap = reader.capabilities ();
			std::vector <std::wstring> vec;
			cap.capabilities_names (vec);
			for (auto &it : vec)
			{
				auto cname = std::wnstring (it.c_str ());
				if (cname.empty ()) continue;
				push_unique (caps, cname);
			}
		} break;
		case PackageType::bundle: {
			auto br = ptr->bundle_reader ();
			CComPtr <IAppxPackageReader> ar;
			if (FAILED (br.random_application_package (&ar))) return nullptr;
			appxreader reader (ar.p);
			auto cap = reader.capabilities ();
			std::vector <std::wstring> vec;
			cap.capabilities_names (vec);
			for (auto &it : vec)
			{
				auto cname = std::wnstring (it.c_str ());
				if (cname.empty ()) continue;
				push_unique (caps, cname);
			}
		} break;
	}
	size_t len = sizeof (LIST_PVOID) + sizeof (LPWSTR) * caps.size ();
	HLIST_PVOID hList = (HLIST_PVOID)malloc (len);
	ZeroMemory (hList, len);
	hList->dwSize = 0;
	for (auto &it : caps)
	{
		hList->alpVoid [hList->dwSize ++] = (LPVOID)_wcsdup (it.c_str ());
	}
	return hList;
}
HLIST_PVOID GetDeviceCapabilitiesList (_In_ HPKGREAD hReader)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return nullptr;
	std::vector <std::wnstring> caps;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			auto cap = reader.capabilities ();
			std::vector <std::wstring> vec;
			cap.device_capabilities (vec);
			for (auto &it : vec)
			{
				auto cname = std::wnstring (it.c_str ());
				if (cname.empty ()) continue;
				push_unique (caps, cname);
			}
		} break;
		case PackageType::bundle: {
			auto br = ptr->bundle_reader ();
			CComPtr <IAppxPackageReader> ar;
			if (FAILED (br.random_application_package (&ar))) return nullptr;
			appxreader reader (ar.p);
			auto cap = reader.capabilities ();
			std::vector <std::wstring> vec;
			cap.device_capabilities (vec);
			for (auto &it : vec)
			{
				auto cname = std::wnstring (it.c_str ());
				if (cname.empty ()) continue;
				push_unique (caps, cname);
			}
		} break;
	}
	size_t len = sizeof (LIST_PVOID) + sizeof (LPWSTR) * caps.size ();
	HLIST_PVOID hList = (HLIST_PVOID)malloc (len);
	ZeroMemory (hList, len);
	hList->dwSize = 0;
	for (auto &it : caps)
	{
		hList->alpVoid [hList->dwSize ++] = (LPVOID)_wcsdup (it.c_str ());
	}
	return hList;
}
void DestroyWStringList (_In_ HLIST_PVOID hList)
{
	if (!hList) return;
	for (size_t cnt = 0; cnt < hList->dwSize; cnt ++)
	{
		free (hList->alpVoid [cnt]);
		hList->alpVoid [cnt] = nullptr;
	}
	free (hList);
}

// Prerequisite
BOOL GetPackagePrerequisite (_In_ HPKGREAD hReader, _In_ LPCWSTR lpName, _Outptr_ VERSION *pVerRet)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return FALSE;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			auto pre = reader.prerequisites ();
			auto ver = pre.get_version (lpName ? lpName : L"");
			*pVerRet = VersionClassToStruct (ver);
			return !ver.empty ();
		} break;
		case PackageType::bundle: {
			auto br = ptr->bundle_reader ();
			CComPtr <IAppxPackageReader> ar;
			HRESULT hr = br.random_application_package (&ar);
			if (FAILED (hr)) return false;
			auto reader = appxreader (ar.p);
			auto pre = reader.prerequisites ();
			auto ver = pre.get_version (lpName ? lpName : L"");
			*pVerRet = VersionClassToStruct (ver);
			return !ver.empty ();
		} break;
	}
	return FALSE;
}

// Selector
std::wnstring SelectLanguageSuitPackageNameByLocaleCode (std::map <std::wnstring, appx_info::resource> &in, const std::wstring &langcode,  std::wnstring &output)
{
	output.clear ();
	for (auto &it : in)
	{
		if (it.second.restype != appx_info::ResourceType::language) continue;
		for (auto &it_s : it.second.resvalue.languages) if (LocaleNameCompare (it_s, langcode)) return output = it.first;
	}
	return output = L"";
}
std::wnstring SelectLanguageSuitPackageName (std::map <std::wnstring, appx_info::resource> &in, std::wnstring &output)
{
	output.clear ();
	output = SelectLanguageSuitPackageNameByLocaleCode (in, GetComputerLocaleCodeW (), output);
	if (output.empty ()) output = SelectLanguageSuitPackageNameByLocaleCode (in, L"en-US", output);
	if (output.empty ())
	{
		for (auto &it : in)
		{
			if (it.second.pkgtype == APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE_APPLICATION)
			{
				output = it.first;
				break;
			}
		}
	}
	if (output.empty ())
	{
		try { output = in.begin ()->first; }
		catch (const std::exception &e) { output = L""; }
	}
	return output;
}
std::wnstring SelectScaleSuitPackageName (std::map <std::wnstring, appx_info::resource> &in, std::wnstring &output)
{
	output.clear ();
	struct res_key_value
	{
		std::wnstring filename = L"";
		uint32_t scale = 0;
		res_key_value (const std::wstring &fpath = L"", uint32_t s = 0):
			filename (fpath), scale (s) {}
	};
	std::vector <res_key_value> rkv;
	for (auto &it : in) if ((WORD)it.second.restype & (WORD)appx_info::ResourceType::scale)
	{
		for (auto &it_s : it.second.resvalue.scales)
		{
			if (!it_s) continue;
			rkv.push_back (res_key_value (it.first, it_s));
		}
	}
	std::sort (rkv.begin (), rkv.end (), [] (const res_key_value &a, const res_key_value &b) {
		return a.scale < b.scale;
	});
	auto dpi = GetDPI ();
	for (auto &it : rkv) if (it.scale > dpi) return output = it.filename;
	if (output.empty ()) { for (auto it = rkv.rbegin (); it != rkv.rend (); ++ it) if (it->scale < dpi) return output = it->filename; }
	if (output.empty ()) { for (auto &it : in) if (it.second.pkgtype == APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE_APPLICATION) return output = it.first; }
	try { output = in.begin ()->first; }
	catch (const std::exception &e) { output = L""; }
	return output;
}

// File Stream
HANDLE GetAppxFileFromAppxPackage (_In_ HPKGREAD hReader, _In_ LPCWSTR lpFileName)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return nullptr;
	if (!lpFileName) return nullptr;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			CComPtr <IAppxFile> afile;
			if (FAILED (reader.payload_stream (lpFileName, &afile))) return nullptr;
			IStream *istream = nullptr;
			if (FAILED (afile->GetStream (&istream))) return nullptr;
			else return istream;
		} break;
		case PackageType::bundle: {
			auto bread = ptr->bundle_reader ();
			CComPtr <IAppxPackageReader> appfile;
			if (FAILED (bread.random_application_package (&appfile))) return nullptr;
			appxreader reader (appfile.p);
			CComPtr <IAppxFile> afile;
			if (FAILED (reader.payload_stream (lpFileName, &afile))) return nullptr;
			IStream *istream = nullptr;
			if (FAILED (afile->GetStream (&istream))) return nullptr;
			else return istream;
		} break;
	}
	return nullptr;
}
HANDLE GetAppxBundlePayloadPackageFile (_In_ HPKGREAD hReader, _In_ LPCWSTR lpFileName)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return nullptr;
	if (!lpFileName) return nullptr;
	if (ptr->type () == PackageType::bundle)
	{
		auto reader = ptr->bundle_reader ();
		CComPtr <IAppxFile> iafile;
		if (FAILED (reader.get_payload_package (lpFileName, &iafile))) return nullptr;
		IStream *istream = nullptr;
		if (FAILED (iafile->GetStream (&istream))) return nullptr;
		else return istream;
	}
	else return nullptr;
}
HANDLE GetAppxPriFileStream (_In_ HPKGREAD hReader) { return GetAppxFileFromAppxPackage (hReader, L"resources.pri"); }
HANDLE GetFileFromPayloadPackage (_In_ HANDLE hPackageStream, _In_ LPCWSTR lpFileName)
{
	if (!hPackageStream || !lpFileName) return nullptr;
	IStream *ifs = (IStream *)hPackageStream;
	CComPtr <IAppxPackageReader> iappx;
	if (FAILED (GetAppxPackageReader (ifs, &iappx))) return nullptr;
	appxreader reader (iappx.p);
	CComPtr <IAppxFile> iaf;
	if (FAILED (reader.payload_stream (lpFileName, &iaf))) return nullptr;
	IStream *istream = nullptr;
	if (FAILED (iaf->GetStream (&istream))) return nullptr;
	else return istream;
}
HANDLE GetPriFileFromPayloadPackage (_In_ HANDLE hPackageStream) { return GetFileFromPayloadPackage (hPackageStream, L"resources.pri"); }
BOOL GetSuitablePackageFromBundle (_In_ HPKGREAD hReader, _Outptr_ HANDLE *pStreamForLang, _Outptr_ HANDLE *pStreamForScale)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr)  return FALSE;
	if (ptr->type () != PackageType::bundle) return FALSE;
	if (pStreamForLang) *pStreamForLang = nullptr;
	if (pStreamForScale) *pStreamForScale = nullptr;
	auto bread = ptr->bundle_reader ();
	auto pkgsinfo = bread.package_id_items ();
	std::map <std::wnstring, appx_info::resource> mapfr;
	pkgsinfo.resource_info (mapfr);
	std::wnstring lf = L"", sf = L"";
	SelectLanguageSuitPackageName (mapfr, lf);
	SelectScaleSuitPackageName (mapfr, sf);
	if (lf == sf)
	{
		WORD flag = (bool)pStreamForLang << 1 | (bool)pStreamForScale;
		switch (flag)
		{
			case 0b01:
			case 0b10:
			case 0b11: {
				IStream *file = nullptr;
				CComPtr <IAppxFile> pread;
				if (FAILED (bread.get_payload_package (lf, &pread))) return false;
				if (FAILED (pread->GetStream (&file))) return false;
				if (pStreamForLang) *pStreamForLang = file;
				if (pStreamForScale) *pStreamForScale = file;
				return true;
			} break;
			default:
			case 0b00: {
				CComPtr <IAppxFile> pread;
				if (FAILED (bread.get_payload_package (lf, &pread))) return false;
				CComPtr <IStream> file = nullptr;
				if (FAILED (pread->GetStream (&file))) return false;
				else return true;
			} break;
		}
	}
	else
	{
		{
			CComPtr <IAppxFile> reslangpkg;
			if (FAILED (bread.get_payload_package (lf, &reslangpkg))) return false;
			if (pStreamForLang)
			{
				IStream *file = nullptr;
				if (FAILED (reslangpkg->GetStream (&file))) return false;
				*pStreamForLang = file;
			}
			else
			{
				CComPtr <IStream> file;
				if (FAILED (reslangpkg->GetStream (&file))) return false;
			}
		}
		{
			CComPtr <IAppxFile> resscalepkg;
			if (FAILED (bread.get_payload_package (sf, &resscalepkg))) return false;
			if (pStreamForScale)
			{
				IStream *file = nullptr;
				if (FAILED (resscalepkg->GetStream (&file))) return false;
				*pStreamForScale = file;
			}
			else
			{
				CComPtr <IStream> file;
				if (FAILED (resscalepkg->GetStream (&file))) return false;
			}
		}
	}
	return true;
}
ULONG DestroyAppxFileStream (_In_ HANDLE hFileStream)
{
	if (!hFileStream) return 0;
	IStream *ptr = reinterpret_cast <IStream *> (hFileStream);
	if (!ptr) return 0;
	ULONG ret = ptr->Release ();
	return ret;
}
HANDLE GetAppxBundleApplicationPackageFile (_In_ HPKGREAD hReader)
{
	if (!hReader) return nullptr;
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return nullptr;
	switch (ptr->type ())
	{
		case PackageType::single: {
			return nullptr;
		} break;
		case PackageType::bundle: {
			auto bread = ptr->bundle_reader ();
			IStream *ipf = nullptr;
			if (FAILED (bread.random_application_package (&ipf))) return nullptr;
			else return ipf;
		} break;
	}
	return nullptr;
}

std::wstring GetMimeTypeFromStream (IStream *filestream)
{
	if (!filestream) return L"";
	LARGE_INTEGER liZero = {0};
	filestream->Seek (liZero, STREAM_SEEK_SET, nullptr);
	BYTE buffer [256] = {0};
	ULONG bytesRead = 0;
	HRESULT hr = filestream->Read (buffer, sizeof (buffer), &bytesRead);
	filestream->Seek (liZero, STREAM_SEEK_SET, nullptr);
	if (FAILED (hr) || bytesRead == 0) return L"";
	LPWSTR lpMime = nullptr;
	raii relt ([&lpMime] () {
		if (lpMime) CoTaskMemFree (lpMime);
		lpMime = nullptr;
	});
	std::wstring mime;
	hr = FindMimeFromData (
		nullptr,            // pBC
		nullptr,            // URL (unknown)
		buffer,              // data buffer
		bytesRead,           // data size
		nullptr,             // proposed MIME
		FMFD_RETURNUPDATEDIMGMIMES |
		FMFD_IGNOREMIMETEXTPLAIN |
		FMFD_URLASFILENAME,
		&lpMime,             // result
		0                    // reserved
	);
	if (SUCCEEDED (hr) && lpMime) mime = lpMime;
	if (mime.empty ())
	{
		if (bytesRead >= 8 && memcmp (buffer, "\x89PNG\r\n\x1A\n", 8) == 0) mime = L"image/png";
		else if (bytesRead >= 3 && buffer [0] == 0xFF && buffer [1] == 0xD8) mime = L"image/jpeg";
		else if (bytesRead >= 6 && memcmp (buffer, "GIF89a", 6) == 0) mime = L"image/gif";
		else if (bytesRead >= 2 && buffer [0] == 'B' && buffer [1] == 'M') mime = L"image/bmp";
		else if (bytesRead >= 12 && memcmp (buffer, "RIFF", 4) == 0 && memcmp (buffer + 8, "WEBP", 4) == 0) mime = L"image/webp";
		else if (bytesRead >= 4 && memcmp (buffer, "\x00\x00\x01\x00", 4) == 0) mime = L"image/x-icon";
		else mime = L"application/octet-stream";
	}
	return mime;
}
std::wstring GetBase64StringFromStreamW (IStream *ifile)
{
	if (!ifile) return L"";
	IStream *&pStream = ifile;
	LARGE_INTEGER liZero = {};
	pStream->Seek (liZero, STREAM_SEEK_SET, nullptr);
	STATSTG statstg;
	pStream->Stat (&statstg, STATFLAG_NONAME);
	ULARGE_INTEGER uliSize = statstg.cbSize;
	std::vector <BYTE> buffer (uliSize.QuadPart);
	ULONG bytesRead;
	pStream->Read (buffer.data (), static_cast <ULONG> (buffer.size ()), &bytesRead);
	DWORD base64Size = 0;
	if (!CryptBinaryToStringW (buffer.data (), bytesRead, CRYPT_STRING_BASE64 | CRYPT_STRING_NOCRLF, nullptr, &base64Size)) return nullptr;
	std::vector <WCHAR> base64Buffer (base64Size + 1);
	if (!CryptBinaryToStringW (buffer.data (), bytesRead, CRYPT_STRING_BASE64 | CRYPT_STRING_NOCRLF, base64Buffer.data (), &base64Size)) return nullptr;
	pStream->Seek (liZero, STREAM_SEEK_SET, nullptr);
	return std::wstring (base64Buffer.data ());
}
LPWSTR StreamToBase64W (_In_ HANDLE hFileStream, _Out_writes_ (dwCharCount) LPWSTR lpMimeBuf, _In_ DWORD dwCharCount, _Outptr_ LPWSTR *lpBase64Head)
{
	IStream *ifs = (IStream *)hFileStream;
	if (!ifs) return nullptr;
	LPWSTR retptr = nullptr;
	std::wstring ret = L"";
	std::wstring mime = GetMimeTypeFromStream (ifs);
	auto &dwBufSize = dwCharCount;
	if (lpMimeBuf)
	{ 
		ZeroMemory (lpMimeBuf, sizeof (WCHAR) * dwBufSize); 
		wcsncpy_s (lpMimeBuf, dwBufSize, mime.c_str (), _TRUNCATE);
	}
	ret += L"data:" + mime + L";base64,";
	size_t head = ret.length ();
	ret += GetBase64StringFromStreamW (ifs);
	retptr = _wcsdup (ret.c_str ());
	if (lpBase64Head) *lpBase64Head = retptr + head;
	return retptr;
}

LPWSTR GetPackagePrerequistieSystemVersionName (_In_ HPKGREAD hReader, _In_ LPCWSTR lpName)
{
	auto ptr = ToPtrPackage (hReader);
	if (!ptr) return nullptr;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto read = ptr->appx_reader ();
			auto pre = read.prerequisites ();
			auto ver = pre.get_version (lpName ? lpName : L"");
			auto str = GetPrerequistOSVersionDescription (ver);
			return _wcsdup (str.c_str ());
		} break;
		case PackageType::bundle: {
			auto br = ptr->bundle_reader ();
			CComPtr <IAppxFile> iaf;
			if (FAILED (br.random_application_package (&iaf))) return nullptr;
			CComPtr <IStream> ist;
			if (FAILED (iaf->GetStream (&ist))) return nullptr;
			CComPtr <IAppxPackageReader> iar;
			if (FAILED (GetAppxPackageReader (ist, &iar))) return nullptr;
			appxreader read (iar.p);
			auto pre = read.prerequisites ();
			auto ver = pre.get_version (lpName ? lpName : L"");
			auto str = GetPrerequistOSVersionDescription (ver);
			return _wcsdup (str.c_str ());
		}
		default:
			break;
	}
	return nullptr;
}

LPWSTR GetPackageCapabilityDisplayName (LPCWSTR lpCapabilityName)
{
	if (!lpCapabilityName) return nullptr;
	std::wnstring capname = (lpCapabilityName ? lpCapabilityName : L"");
	if (capname.empty ()) return nullptr;
	std::wstring ret = GetCapabilityDisplayName (capname);
	if (IsNormalizeStringEmpty (ret)) return nullptr;
	else return _wcsdup (ret.c_str ());
}

void PackageReaderFreeString (LPWSTR lpStrFromThisDll)
{
	if (!lpStrFromThisDll) return;
	free (lpStrFromThisDll);
}

// ========== 以下是对清单文件的读取 ==========
#define ToHandleMRead(_cpp_ptr_) reinterpret_cast <HPKGMANIFESTREAD> (_cpp_ptr_) 
#define ToPtrManifest(_cpp_ptr_) reinterpret_cast <manifest *> (_cpp_ptr_)
HPKGMANIFESTREAD CreateManifestReader () { return ToHandleMRead (new manifest ()); }
BOOL LoadManifestFromFile (_In_ HPKGMANIFESTREAD hReader, _In_ LPCWSTR lpFilePath)
{
	auto ptr = ToPtrManifest (hReader);
	if (!ptr) return false;
	return ptr->create (lpFilePath);
}
void DestroyManifestReader (_In_ HPKGMANIFESTREAD hReader)
{
	auto ptr = ToPtrManifest (hReader);
	if (!ptr) return;
	return delete ptr;
}
WORD GetManifestType (_In_ HPKGMANIFESTREAD hReader)
{
	auto ptr = ToPtrManifest (hReader);
	if (!ptr) return PKGTYPE_UNKNOWN;
	switch (ptr->type ())
	{
		case PackageType::unknown: return PKGTYPE_UNKNOWN;
		case PackageType::single: return PKGTYPE_APPX;
		case PackageType::bundle: return PKGTYPE_BUNDLE;
	}
	return PKGTYPE_UNKNOWN;
}
BOOL IsManifestValid (_In_ HPKGMANIFESTREAD hReader)
{
	auto ptr = ToPtrManifest (hReader);
	if (!ptr) return false;
	return ptr->valid ();
}
WORD GetManifestRole (_In_ HPKGMANIFESTREAD hReader)
{
	auto ptr = ToPtrManifest (hReader);
	if (!ptr) return PKGROLE_UNKNOWN;
	switch (ptr->type ())
	{
		case PackageType::unknown: return PKGROLE_UNKNOWN;
		case PackageType::bundle: return PKGROLE_APPLICATION;
		case PackageType::single: {
			auto ar = ptr->appx_reader ();
			switch (ar.package_role ())
			{
				case PackageRole::unknown: return PKGROLE_UNKNOWN;
				case PackageRole::application: return PKGROLE_APPLICATION;
				case PackageRole::framework: return PKGROLE_FRAMEWORK;
				case PackageRole::resource: return PKGROLE_RESOURCE;
			}
		} break;
	}
	return PKGROLE_UNKNOWN;
}
// Identity
LPWSTR GetManifestIdentityStringValue (_In_ HPKGMANIFESTREAD hReader, _In_ DWORD dwName)
{
	auto ptr = ToPtrManifest (hReader);
	if (!ptr) return nullptr;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			auto id = reader.identity ();
			switch (LOWORD (dwName))
			{
				case PKG_IDENTITY_NAME: return _wcsdup (id.name ().c_str ());
				case PKG_IDENTITY_PUBLISHER: return _wcsdup (id.publisher ().c_str ());
				case PKG_IDENTITY_PACKAGEFAMILYNAME: return _wcsdup (id.package_family_name ().c_str ());
				case PKG_IDENTITY_PACKAGEFULLNAME: return _wcsdup (id.package_full_name ().c_str ());
				case PKG_IDENTITY_RESOURCEID: return _wcsdup (id.resource_id ().c_str ());
			}
		} break;
		case PackageType::bundle: {
			auto reader = ptr->bundle_reader ();
			auto id = reader.identity ();
			{
				switch (LOWORD (dwName))
				{
					case PKG_IDENTITY_NAME: return _wcsdup (id.name ().c_str ());
					case PKG_IDENTITY_PUBLISHER: return _wcsdup (id.publisher ().c_str ());
					case PKG_IDENTITY_PACKAGEFAMILYNAME: return _wcsdup (id.package_family_name ().c_str ());
					case PKG_IDENTITY_PACKAGEFULLNAME: return _wcsdup (id.package_full_name ().c_str ());
					case PKG_IDENTITY_RESOURCEID: return _wcsdup (id.resource_id ().c_str ());
				}
			}
		} break;
	}
	return nullptr;
}
BOOL GetManifestIdentityVersion (_In_ HPKGMANIFESTREAD hReader, _Out_ VERSION *pVersion)
{
	auto ptr = ToPtrManifest (hReader);
	if (!ptr) return FALSE;
	if (!pVersion) return FALSE;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			auto id = reader.identity ();
			auto ver = id.version ();
			*pVersion = VersionClassToStruct (ver);
			return !ver.empty ();
		} break;
		case PackageType::bundle: {
			auto reader = ptr->bundle_reader ();
			auto id = reader.identity ();
			{
				auto ver = id.version ();
				*pVersion = VersionClassToStruct (ver);
				return !ver.empty ();
			}
		} break;
	}
	return FALSE;
}
BOOL GetManifestIdentityArchitecture (_In_ HPKGMANIFESTREAD hReader, _Out_ DWORD *pdwArchi)
{
	auto ptr = ToPtrManifest (hReader);
	if (!ptr) return FALSE;
	if (!pdwArchi) return FALSE;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			auto id = reader.identity ();
			auto archi = id.architecture ();
			DWORD ret = 0;
			switch (archi)
			{
				case APPX_PACKAGE_ARCHITECTURE::APPX_PACKAGE_ARCHITECTURE_X86:
					ret = PKG_ARCHITECTURE_X86; break;
				case APPX_PACKAGE_ARCHITECTURE::APPX_PACKAGE_ARCHITECTURE_ARM:
					ret = PKG_ARCHITECTURE_ARM; break;
				case APPX_PACKAGE_ARCHITECTURE::APPX_PACKAGE_ARCHITECTURE_X64:
					ret = PKG_ARCHITECTURE_X64; break;
				case APPX_PACKAGE_ARCHITECTURE::APPX_PACKAGE_ARCHITECTURE_NEUTRAL:
					ret = PKG_ARCHITECTURE_NEUTRAL; break;
				case (APPX_PACKAGE_ARCHITECTURE)12: // ARM64
					ret = PKG_ARCHITECTURE_ARM64; break;
			}
			*pdwArchi = ret;
			return ret != PKG_ARCHITECTURE_UNKNOWN;
		} break;
		case PackageType::bundle: {
			auto reader = ptr->bundle_reader ();
			auto ids = reader.package_id_items ();
			std::vector <appx_info::appx_iditem> apps;
			ids.application_packages (apps);
			DWORD ret = 0;
			for (auto &it : apps)
			{
				auto id = it.identity ();
				auto archi = id.architecture ();
				switch (archi)
				{
					case APPX_PACKAGE_ARCHITECTURE::APPX_PACKAGE_ARCHITECTURE_X86:
						ret |= PKG_ARCHITECTURE_X86; break;
					case APPX_PACKAGE_ARCHITECTURE::APPX_PACKAGE_ARCHITECTURE_ARM:
						ret |= PKG_ARCHITECTURE_ARM; break;
					case APPX_PACKAGE_ARCHITECTURE::APPX_PACKAGE_ARCHITECTURE_X64:
						ret |= PKG_ARCHITECTURE_X64; break;
					case APPX_PACKAGE_ARCHITECTURE::APPX_PACKAGE_ARCHITECTURE_NEUTRAL:
						ret |= PKG_ARCHITECTURE_NEUTRAL; break;
					case (APPX_PACKAGE_ARCHITECTURE)12: // ARM64
						ret |= PKG_ARCHITECTURE_ARM64; break;
				}
			}
			*pdwArchi = ret;
			return ret != PKG_ARCHITECTURE_UNKNOWN;
		} break;
	}
	return FALSE;
}
// Properties
LPWSTR GetManifestPropertiesStringValue (_In_ HPKGMANIFESTREAD hReader, _In_ LPCWSTR lpName)
{
	auto ptr = ToPtrManifest (hReader);
	if (!ptr) return nullptr;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			auto prop = reader.properties ();
			return _wcsdup (prop.string_value (lpName ? lpName : L"").c_str ());
		} break;
	}
	return nullptr;
}
HRESULT GetManifestPropertiesBoolValue (_In_ HPKGMANIFESTREAD hReader, _In_ LPCWSTR lpName, _Outptr_ BOOL *pRet)
{
	auto ptr = ToPtrManifest (hReader);
	if (!ptr) return E_INVALIDARG;
	if (!pRet) return E_INVALIDARG;
	*pRet = FALSE;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			IAppxManifestReader *m = reader.manifest ();
			if (!m) return E_FAIL;
			CComPtr <IAppxManifestProperties> p;
			HRESULT hr = m->GetProperties (&p);
			if (FAILED (hr)) return hr;
			return p->GetBoolValue (lpName, pRet);
		} break;
	}
	return E_FAIL;
}
// Applications
BOOL AddManifestApplicationItemGetName (_In_ LPCWSTR lpName)
{
	if (std::wnstring (lpName ? lpName : L"").empty ()) return FALSE;
	return PushApplicationAttributeItem (lpName);
}
BOOL RemoveManifestApplicationItemGetName (_In_ LPCWSTR lpName)
{
	if (std::wnstring (lpName ? lpName : L"").empty ()) return FALSE;
	return RemoveApplicationAttributeItem (lpName);
}
HAPPENUMERATOR GetManifestApplications (_In_ HPKGMANIFESTREAD hReader)
{
	auto ptr = ToPtrManifest (hReader);
	if (!ptr) return nullptr;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			auto app = reader.applications ();
			auto appvec = new app_enumerator ();
			app.applications (*appvec);
			return ToHandleAppEnumerator (appvec);
		} break;
	}
	return nullptr;
}
void DestroyManifestApplications (_In_ HAPPENUMERATOR hEnumerator)
{
	auto ptr = ToPtrAppxApps (hEnumerator);
	if (ptr) delete ptr;
}
// Resources
HLIST_PVOID GetManifestResourcesLanguages (_In_ HPKGMANIFESTREAD hReader)
{
	auto ptr = ToPtrManifest (hReader);
	if (!ptr) return nullptr;
	std::vector <std::wnstring> langs;
	switch (ptr->type ())
	{
		case PackageType::single:
		{
			auto reader = ptr->appx_reader ();
			auto res = reader.resources ();
			res.languages (langs);
			break;
		}
		case PackageType::bundle:
		{
			auto br = ptr->bundle_reader ();
			auto res = br.package_id_items ();
			res.enumerate ([&langs] (IAppxBundleManifestPackageInfo *inf) {
				auto item = appx_info::appx_iditem (inf);
				std::vector <std::wnstring> l;
				auto qr = item.qualified_resources ();
				qr.languages (l);
				for (auto &it : l) push_unique <std::wnstring> (langs, it);
			});
			break;
		}
		default: return nullptr;
	}
	if (langs.empty ()) return nullptr;
	size_t count = langs.size ();
	size_t bytes = sizeof (LIST_PVOID) + sizeof (LPWSTR) * (count - 1);
	auto list = (HLIST_PVOID)malloc (bytes);
	ZeroMemory (list, bytes);
	if (!list) return nullptr;
	list->dwSize = 0;
	for (auto &it : langs) list->alpVoid [list->dwSize ++] = _wcsdup (it.c_str ());
	return list;
}
HLIST_LCID GetManifestResourcesLanguagesToLcid (_In_ HPKGMANIFESTREAD hReader)
{
	auto ptr = ToPtrManifest (hReader);
	if (!ptr) return nullptr;
	std::vector <std::wnstring> langs;
	switch (ptr->type ())
	{
		case PackageType::single:
		{
			auto reader = ptr->appx_reader ();
			auto res = reader.resources ();
			res.languages (langs);
			break;
		}
		case PackageType::bundle:
		{
			auto br = ptr->bundle_reader ();
			auto res = br.package_id_items ();
			res.enumerate ([&langs] (IAppxBundleManifestPackageInfo *inf) {
				auto item = appx_info::appx_iditem (inf);
				std::vector <std::wnstring> l;
				auto qr = item.qualified_resources ();
				qr.languages (l);
				for (auto &it : l) push_unique <std::wnstring> (langs, it);
			});
			break;
		}
		default: return nullptr;
	}
	if (langs.empty ()) return nullptr;
	size_t len = sizeof (LIST_LCID) + sizeof (LCID) * langs.size ();
	HLIST_LCID hList = (HLIST_LCID)malloc (len);
	ZeroMemory (hList, len);
	hList->dwSize = 0;
	for (auto &it : langs)
	{
		LCID lcid = LocaleCodeToLcid (it);
		if (lcid)
		{
			hList->aLcid [hList->dwSize ++] = lcid;
		}
	}
	return hList;
}
HLIST_UINT32 GetManifestResourcesScales (_In_ HPKGMANIFESTREAD hReader)

{
	auto ptr = ToPtrManifest (hReader);
	if (!ptr) return nullptr;
	std::vector <UINT32> scales;
	switch (ptr->type ())
	{
		case PackageType::single:
		{
			auto reader = ptr->appx_reader ();
			auto res = reader.resources ();
			res.scales (scales);
			break;
		}
		case PackageType::bundle:
		{
			auto br = ptr->bundle_reader ();
			auto res = br.package_id_items ();
			res.enumerate ([&scales] (IAppxBundleManifestPackageInfo *inf) {
				auto item = appx_info::appx_iditem (inf);
				std::vector <UINT32> s;
				auto qr = item.qualified_resources ();
				qr.scales (s);
				for (auto &it : s) push_unique (scales, it);
			});
			break;
		}
		default: return nullptr;
	}
	if (scales.empty ()) return nullptr;
	size_t len = sizeof (LIST_UINT32) + sizeof (UINT32) * scales.size ();
	HLIST_UINT32 hList = (HLIST_UINT32)malloc (len);
	ZeroMemory (hList, len);
	hList->dwSize = 0;
	for (auto &it : scales)
	{
		if (!it) continue;
		hList->aUI32 [hList->dwSize ++] = it;
	}
	return hList;
}
DWORD GetManifestResourcesDxFeatureLevels (_In_ HPKGMANIFESTREAD hReader)

{
	auto ptr = ToPtrManifest (hReader);
	if (!ptr) return 0;
	DWORD dwFlags = 0;
	switch (ptr->type ())
	{
		case PackageType::single:
		{
			auto reader = ptr->appx_reader ();
			auto res = reader.resources ();
			std::vector <DX_FEATURE_LEVEL> dxlevels;
			res.dx_feature_level (dxlevels);
			for (auto &it : dxlevels)
			{
				switch (it)
				{
					case DX_FEATURE_LEVEL::DX_FEATURE_LEVEL_9:
						dwFlags |= PKG_RESOURCES_DXFEATURE_LEVEL9; break;
					case DX_FEATURE_LEVEL::DX_FEATURE_LEVEL_10:
						dwFlags |= PKG_RESOURCES_DXFEATURE_LEVEL10; break;
					case DX_FEATURE_LEVEL::DX_FEATURE_LEVEL_11:
						dwFlags |= PKG_RESOURCES_DXFEATURE_LEVEL11; break;
					case (DX_FEATURE_LEVEL)4:
						dwFlags |= PKG_RESOURCES_DXFEATURE_LEVEL12; break;
				}
			}
			break;
		}
		case PackageType::bundle:
		{
			auto br = ptr->bundle_reader ();
			auto res = br.package_id_items ();
			res.enumerate ([&dwFlags] (IAppxBundleManifestPackageInfo *inf) {
				auto item = appx_info::appx_iditem (inf);
				std::vector <DX_FEATURE_LEVEL> dxlevels;
				auto qr = item.qualified_resources ();
				qr.dx_feature_level (dxlevels);
				for (auto &it : dxlevels)
				{
					switch (it)
					{
						case DX_FEATURE_LEVEL::DX_FEATURE_LEVEL_9:
							dwFlags |= PKG_RESOURCES_DXFEATURE_LEVEL9; break;
						case DX_FEATURE_LEVEL::DX_FEATURE_LEVEL_10:
							dwFlags |= PKG_RESOURCES_DXFEATURE_LEVEL10; break;
						case DX_FEATURE_LEVEL::DX_FEATURE_LEVEL_11:
							dwFlags |= PKG_RESOURCES_DXFEATURE_LEVEL11; break;
						case (DX_FEATURE_LEVEL)4:
							dwFlags |= PKG_RESOURCES_DXFEATURE_LEVEL12; break;
					}
				}
			});
			break;
		}
		default: return 0;
	}
	return dwFlags;
}
// Dependencies
HLIST_DEPINFO GetManifestDependencesInfoList (_In_ HPKGMANIFESTREAD hReader)
{
	auto ptr = ToPtrManifest (hReader);
	if (!ptr) return nullptr;
	std::vector <dep_info> vec;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			auto deps = reader.dependencies ();
			deps.dependencies (vec);
		} break;
	}
	size_t len = sizeof (LIST_DEPINFO) + sizeof (DEPENDENCY_INFO) * vec.size ();
	HLIST_DEPINFO hList = (HLIST_DEPINFO)malloc (len);
	ZeroMemory (hList, len);
	hList->dwSize = 0;
	for (auto &it : vec)
	{
		auto &dep = hList->aDepInfo [hList->dwSize ++];
		dep.lpName = _wcsdup (it.name.c_str ());
		dep.lpPublisher = _wcsdup (it.publisher.c_str ());
		dep.verMin = VersionClassToStruct (it.minversion);
	}
	return hList;
}
// Capabilities
HLIST_PVOID GetManifestCapabilitiesList (_In_ HPKGMANIFESTREAD hReader)
{
	auto ptr = ToPtrManifest (hReader);
	if (!ptr) return nullptr;
	std::vector <std::wnstring> caps;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			auto cap = reader.capabilities ();
			std::vector <std::wstring> vec;
			cap.capabilities_names (vec);
			for (auto &it : vec)
			{
				auto cname = std::wnstring (it.c_str ());
				if (cname.empty ()) continue;
				push_unique (caps, cname);
			}
		} break;
	}
	size_t len = sizeof (LIST_PVOID) + sizeof (LPWSTR) * caps.size ();
	HLIST_PVOID hList = (HLIST_PVOID)malloc (len);
	ZeroMemory (hList, len);
	hList->dwSize = 0;
	for (auto &it : caps)
	{
		hList->alpVoid [hList->dwSize ++] = (LPVOID)_wcsdup (it.c_str ());
	}
	return hList;
}
HLIST_PVOID GetManifestDeviceCapabilitiesList (_In_ HPKGMANIFESTREAD hReader)
{
	auto ptr = ToPtrManifest (hReader);
	if (!ptr) return nullptr;
	std::vector <std::wnstring> caps;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			auto cap = reader.capabilities ();
			std::vector <std::wstring> vec;
			cap.device_capabilities (vec);
			for (auto &it : vec)
			{
				auto cname = std::wnstring (it.c_str ());
				if (cname.empty ()) continue;
				push_unique (caps, cname);
			}
		} break;
	}
	size_t len = sizeof (LIST_PVOID) + sizeof (LPWSTR) * caps.size ();
	HLIST_PVOID hList = (HLIST_PVOID)malloc (len);
	ZeroMemory (hList, len);
	hList->dwSize = 0;
	for (auto &it : caps)
	{
		hList->alpVoid [hList->dwSize ++] = (LPVOID)_wcsdup (it.c_str ());
	}
	return hList;
}
// Prerequisite
BOOL GetManifestPrerequisite (_In_ HPKGMANIFESTREAD hReader, _In_ LPCWSTR lpName, _Outptr_ VERSION *pVerRet)
{
	auto ptr = ToPtrManifest (hReader);
	if (!ptr) return FALSE;
	switch (ptr->type ())
	{
		case PackageType::single: {
			auto reader = ptr->appx_reader ();
			auto pre = reader.prerequisites ();
			auto ver = pre.get_version (lpName ? lpName : L"");
			*pVerRet = VersionClassToStruct (ver);
			return !ver.empty ();
		} break;
	}
	return FALSE;
}
