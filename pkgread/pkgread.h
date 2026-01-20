// 下列 ifdef 块是创建使从 DLL 导出更简单的
// 宏的标准方法。此 DLL 中的所有文件都是用命令行上定义的 PKGREAD_EXPORTS
// 符号编译的。在使用此 DLL 的
// 任何其他项目上不应定义此符号。这样，源文件中包含此文件的任何其他项目都会将
// PKGREAD_API 函数视为是从 DLL 导入的，而此 DLL 则将用此宏定义的
// 符号视为是被导出的。
#ifndef _APPX_PACKAGE_READER_H_
#define _APPX_PACKAGE_READER_H_
#ifdef PKGREAD_EXPORTS
#define PKGREAD_API __declspec(dllexport)
#else
#define PKGREAD_API __declspec(dllimport)
#endif

#ifdef __cplusplus
#define _DEFAULT_INIT_VALUE_(_init_value_) = _init_value_
#ifndef PKGREAD_EXPORTS
#define _DEFAULT_INIT_VALUE_FORFUNC_(_init_value_) = _init_value_
#else 
#define _DEFAULT_INIT_VALUE_FORFUNC_(_init_value_)
#endif
#else
#define _DEFAULT_INIT_VALUE_(_init_value_)
#define _DEFAULT_INIT_VALUE_FORFUNC_(_init_value_)
#endif

#if defined (__cplusplus) && defined (PKGREAD_EXPORTS)
#include <iostream>
#endif

#ifndef _TYPE_STRUCT_VERSION_
#define _TYPE_STRUCT_VERSION_
typedef struct _VERSION
{
	UINT16 major _DEFAULT_INIT_VALUE_ (0),
		minor _DEFAULT_INIT_VALUE_ (0),
		build _DEFAULT_INIT_VALUE_ (0),
		revision _DEFAULT_INIT_VALUE_ (0);
#if defined (__cplusplus) && defined (PKGREAD_EXPORTS)
	_VERSION (UINT16 major = 0, UINT16 minor = 0, UINT16 build = 0, UINT16 revision = 0):
		major (major), minor (minor), build (build), revision (revision) {}
	friend std::ostream &operator << (std::ostream &o, const _VERSION &v)
	{
		return o << v.major << "." << v.minor << "." << v.build << "." << v.revision;
	}
	friend std::wostream &operator << (std::wostream &o, const _VERSION &v)
	{
		return o << v.major << L"." << v.minor << L"." << v.build << L"." << v.revision;
	}
#endif
} VERSION;
#endif

#ifdef __cplusplus
extern "C"
{
#endif

#ifndef _TYPE_STRUCT_LIST_PVOID_
#define _TYPE_STRUCT_LIST_PVOID_
	typedef struct _LIST_PVOID
	{
		DWORD dwSize _DEFAULT_INIT_VALUE_ (0);
		LPVOID alpVoid [1] _DEFAULT_INIT_VALUE_ ({NULL});
	} LIST_PVOID, *HLIST_PVOID;
#endif

#ifndef _TYPE_STRUCT_PAIR_PVOID_
#define _TYPE_STRUCT_PAIR_PVOID_
	typedef struct _PAIR_PVOID_
	{
		LPVOID lpKey _DEFAULT_INIT_VALUE_ (NULL);
		LPVOID lpValue _DEFAULT_INIT_VALUE_ (NULL);
	} PAIR_PVOID, *HPAIR_PVOID;
#endif

#define TEMPLATE_STRUCT(_typename_) typedef struct _typename_##__ _typename_
	TEMPLATE_STRUCT (PKGREADER);
	typedef PKGREADER *HPKGREAD;
	TEMPLATE_STRUCT (APPENUMERATOR);
	typedef APPENUMERATOR *HAPPENUMERATOR;

#define PKGTYPE_UNKNOWN   0
#define PKGTYPE_APPX      1
#define PKGTYPE_BUNDLE    2
#define PKGROLE_UNKNOWN       0
#define PKGROLE_APPLICATION   1
#define PKGROLE_FRAMEWORK     2
#define PKGROLE_RESOURCE      3
// 创建包读取器
	PKGREAD_API HPKGREAD CreatePackageReader ();
	// 通过包读取器打开包
	PKGREAD_API BOOL LoadPackageFromFile (_In_ HPKGREAD hReader, _In_ LPCWSTR lpFilePath);
	// 销毁包读取器（必须）
	PKGREAD_API void DestroyPackageReader (_In_ HPKGREAD hReader);
	// 获取包的类型：Appx 包还是 AppxBundle 包。返回 PKGTYPE_* 宏 
	PKGREAD_API WORD GetPackageType (_In_ HPKGREAD hReader);
	// 返回包是否有效。无效的包会返回假。
	PKGREAD_API BOOL IsPackageValid (_In_ HPKGREAD hReader);
	// 获取包的类型：是应用包、框架包还是资源包。
	// 对于 AppxBundle 包永远返回是资源包（因为框架包无法打包成 AppxBundle，资源包必须与应用包一同打包进 AppxBundle）。
	// 返回 PKGROLE_* 宏。
	PKGREAD_API WORD GetPackageRole (_In_ HPKGREAD hReader);

	// Identity
	// 对于 AppxBundle 包来说，AppxBundle 包与储存的 Appx 的包的身份信息并不一致（不一致在版本号和处理器架构上）
#define PKG_IDENTITY_BUNDLE_GETAPPXINFO ((PKGTYPE_BUNDLE && 0xFFFF) << 16)
#define PKG_IDENTITY_NAME 0
#define PKG_IDENTITY_PUBLISHER 1
#define PKG_IDENTITY_PACKAGEFAMILYNAME 2
#define PKG_IDENTITY_PACKAGEFULLNAME 3
#define PKG_IDENTITY_RESOURCEID 4
#define PKG_ARCHITECTURE_UNKNOWN 0x0
#define PKG_ARCHITECTURE_X86 0x1
#define PKG_ARCHITECTURE_X64 0x2
#define PKG_ARCHITECTURE_ARM 0x4
#define PKG_ARCHITECTURE_ARM64 0x8
#define PKG_ARCHITECTURE_NEUTRAL 0xF
	// 获取身份信息能获取文本值的项。支持 PKG_IDENTITY_* 前缀的宏 NAME, PUBLISHER, PACKAGEFAMILYNAME, PACKAGEFULLNAME, RESOURCEID。
	// 当与 PKG_IDENTITY_BUNDLE_GETAPPXINFO 通过位或结合时，则获取 AppxBundle 中某一个应用包。
	PKGREAD_API LPWSTR GetPackageIdentityStringValue (_In_ HPKGREAD hReader, _In_ DWORD dwName);
#define GetPackageIdentityName(_In_hReader_) GetPackageIdentityStringValue (_In_hReader_, PKG_IDENTITY_NAME)
#define GetPackageIdentityPublisher(_In_hReader_) GetPackageIdentityStringValue (_In_hReader_, PKG_IDENTITY_PUBLISHER)
#define GetPackageIdentityPackageFamilyName(_In_hReader_) GetPackageIdentityStringValue (_In_hReader_, PKG_IDENTITY_PACKAGEFAMILYNAME)
#define GetPackageIdentityPackageFullName(_In_hReader_) GetPackageIdentityStringValue (_In_hReader_, PKG_IDENTITY_PACKAGEFULLNAME)
#define GetPackageIdentityResourceId(_In_hReader_) GetPackageIdentityStringValue (_In_hReader_, PKG_IDENTITY_RESOURCEID)
	// 获取身份信息。当 bGetSubPkgVer 为真，且打开的包为 AppxBundle 包，返回 AppxBundle 包中的某个应用包的版本号。
	PKGREAD_API BOOL GetPackageIdentityVersion (_In_ HPKGREAD hReader, _Out_ VERSION *pVersion, _In_ BOOL bGetSubPkgVer _DEFAULT_INIT_VALUE_FORFUNC_ (0));
	// 对于 AppxBundle 包，当传入的值有 PKG_IDENTITY_BUNDLE_GETAPPXINFO，则返回一个按位值，如 0b0011，则意味着支持 x86 和 x64
	PKGREAD_API BOOL GetPackageIdentityArchitecture (_In_ HPKGREAD hReader, _Out_ DWORD *pdwArchi);

	// Properties
	// 获取字符串值
#define PKG_PROPERTIES_DISPLAYNAME L"DisplayName"
#define PKG_PROPERTIES_DESCRIPTION L"Description"
#define PKG_PROPERTIES_LOGO L"Logo"
#define PKG_PROPERTIES_PUBLISHER L"PublisherDisplayName"
// 获取逻辑型值
#define PKG_PROPERTIES_FRAMEWORD L"Framework"
#define PKG_PROPERTIES_IS_RESOURCE L"ResourcePackage"
	// 传入参数参考：https://learn.microsoft.com/en-us/windows/win32/api/appxpackaging/nf-appxpackaging-iappxmanifestproperties-getstringvalue
	// 可以使用宏 PKG_PROPERTIES_* 前缀的 DISPLAYNAME, DESCRIPTION, LOGO, PUBLISHER
	PKGREAD_API LPWSTR GetPackagePropertiesStringValue (_In_ HPKGREAD hReader, _In_ LPCWSTR lpName);
#define GetPackagePropertiesDisplayName(_In_hReader_) GetPackagePropertiesStringValue (_In_hReader_, PKG_PROPERTIES_DISPLAYNAME)
#define GetPackagePropertiesDescription(_In_hReader_) GetPackagePropertiesStringValue (_In_hReader_, PKG_PROPERTIES_DESCRIPTION)
#define GetPackagePropertiesLogo(_In_hReader_) GetPackagePropertiesStringValue (_In_hReader_, PKG_PROPERTIES_LOGO)
#define GetPackagePropertiesPublisher(_In_hReader_) GetPackagePropertiesStringValue (_In_hReader_, PKG_PROPERTIES_PUBLISHER)
	// 传入参数参考：https://learn.microsoft.com/zh-cn/windows/win32/api/appxpackaging/nf-appxpackaging-iappxmanifestproperties-getboolvalue
	// 可以使用宏 PKG_PROPERTIES_* 前缀的 FRAMEWORD, IS_RESOURCE (Windows 8.1 支持)
	PKGREAD_API HRESULT GetPackagePropertiesBoolValue (_In_ HPKGREAD hReader, _In_ LPCWSTR lpName, _Outptr_ BOOL *pRet);
#define GetPackagePropertiesIsFramework(_In_hReader_, _Outptr_pRetValue_) GetPackagePropertiesBoolValue (_In_hReader_, PKG_PROPERTIES_FRAMEWORD, _Outptr_pRetValue_)
#define GetPackagePropertiesIsResourcePackage(_In_hReader_, _Outptr_pRetValue_) GetPackagePropertiesBoolValue (_In_hReader_, PKG_PROPERTIES_IS_RESOURCE, _Outptr_pRetValue_)
//  Applications
	// 注意：由于读取 Application 在官方 API 中是枚举类型，只能枚举一次。
	// 所以请预先安排好需要的信息，并在枚举时进行缓存。
	// 添加和删除的请参考这些：https://learn.microsoft.com/en-us/windows/win32/api/appxpackaging/nf-appxpackaging-iappxmanifestapplication-getstringvalue
	// DLL 本身支持：Id, DisplayName, BackgroundColor, ForegroundText, ShortName, Square44x44Logo
	// 注：一定会保留项：AppUserModelID。这是必须项。
	PKGREAD_API BOOL AddPackageApplicationItemGetName (_In_ LPCWSTR lpName);
	// 移除不需要的信息项名。
	PKGREAD_API BOOL RemovePackageApplicationItemGetName (_In_ LPCWSTR lpName);
	// 获取现在有的信息项名。读取时将指针数组的成员转换为 LPCWSTR 读取，且不能修改。
	// 注意：返回的 HLIST_PVOID 要经过 DestroyApplicationItemsName 销毁。
	PKGREAD_API HLIST_PVOID GetAllApplicationItemsName ();
	// 通过 GetAllApplicationItemsName 获取的 HLIST_PVOID 由 DestroyApplicationItemsName 销毁。
	PKGREAD_API void DestroyApplicationItemsName (_In_ HLIST_PVOID hList);
	// 获取到 Application 信息枚举器。在自己缓存完信息前不要销毁，直到再也不需要后才销毁。
	// 通过 GetPackageApplications 获取的 HAPPENUMERATOR 由 DestroyApplicationsMap 销毁
	PKGREAD_API HAPPENUMERATOR GetPackageApplications (_In_ HPKGREAD hReader);
	// 从 Application 枚举器转换成 HLIST_PVOID { HLIST_PVOID {HPAIR_PVOID} } （类似于多个映射组成的数组）。
	// 读取最深层的指针成员转换为 LPCWSTR 读取。其 LPCWSTR 周期随着 HAPPENUMERATOR 的周期而决定。
	PKGREAD_API HLIST_PVOID ApplicationsToMap (_In_ HAPPENUMERATOR hEnumerator);
	// 通过 ApplicationsToMap 获取的 HLIST_PVOID 由 DestroyApplicationsMap 销毁
	PKGREAD_API void DestroyApplicationsMap (_In_ HLIST_PVOID hEnumerator);
	// 通过 GetPackageApplications 获取的 HAPPENUMERATOR 由 DestroyApplicationsMap 销毁
	PKGREAD_API void DestroyPackageApplications (_In_ HAPPENUMERATOR hEnumerator);

	// Resources
#ifndef _TYPE_STRUCT_LIST_LCID_
#define _TYPE_STRUCT_LIST_LCID_
	typedef struct _LIST_LCID
	{
		DWORD dwSize _DEFAULT_INIT_VALUE_ (0);
		LCID aLcid [1] _DEFAULT_INIT_VALUE_ ({0});
	} LIST_LCID, *HLIST_LCID;
#endif
#ifndef _TYPE_STRUCT_LIST_UINT32_
#define _TYPE_STRUCT_LIST_UINT32_
	typedef struct _LIST_UINT32
	{
		DWORD dwSize _DEFAULT_INIT_VALUE_ (0);
		LCID aUI32 [1] _DEFAULT_INIT_VALUE_ ({0});
	} LIST_UINT32, *HLIST_UINT32;
#endif
	// 返回区域代码 (Locale Code) 数组。以 LPWSTR 读取。注：一定要通过 DestroyResourcesLanguagesList 释放
	PKGREAD_API HLIST_PVOID GetResourcesLanguages (_In_ HPKGREAD hReader);
	// 返回将区域代码转换成 LCID（如 en-US 为 1033，zh-CN 为 2052） 后的数组。以 UINT32 读取。
	// 注：一定要通过 DestroyResourcesLanguagesLcidList 释放
	PKGREAD_API HLIST_LCID GetResourcesLanguagesToLcid (_In_ HPKGREAD hReader);
	// 返回 DPI 支持（如 100，125） 的数组。
	// 注：一定要通过 DestroyResourcesScalesList 宏（或 DestroyUInt32List 函数）释放
	PKGREAD_API HLIST_UINT32 GetResourcesScales (_In_ HPKGREAD hReader);
#define PKG_RESOURCES_DXFEATURE_LEVEL9		0x1
#define PKG_RESOURCES_DXFEATURE_LEVEL10		0x2
#define PKG_RESOURCES_DXFEATURE_LEVEL11		0x4
#define PKG_RESOURCES_DXFEATURE_LEVEL12		0x8
#define PKG_RESOURCES_DXFEATURE_UNSPECIFIED	0
	// 返回一个 DWORD 值。通过按位与来获取支持的 DirectX 功能支持。如果等于 0 则意味着都不支持。该值不必释放。
	PKGREAD_API DWORD GetResourcesDxFeatureLevels (_In_ HPKGREAD hReader);
	// 销毁 GetResourcesLanguages 获取的数组
	PKGREAD_API void DestroyResourcesLanguagesList (_In_ HLIST_PVOID hList);
	// 销毁 GetResourcesLanguagesToLcid 获取的数组
	PKGREAD_API void DestroyResourcesLanguagesLcidList (_In_ HLIST_LCID hList);
	PKGREAD_API void DestroyUInt32List (_In_ HLIST_UINT32 hList);
	// 销毁 GetResourcesScales 获取的数组
#define DestroyResourcesScalesList DestroyUInt32List

	// Dependencies
#ifndef _TYPE_STRUCT_DEPENDENCY_
#define _TYPE_STRUCT_DEPENDENCY_
	typedef struct _DEPENDENCY_INFO
	{
		VERSION verMin;
		LPWSTR lpName _DEFAULT_INIT_VALUE_ (NULL);
		LPWSTR lpPublisher _DEFAULT_INIT_VALUE_ (NULL);
	} DEPENDENCY_INFO;
#endif
#ifndef _TYPE_STRUCT_DEPENDENCY_LIST_
#define _TYPE_STRUCT_DEPENDENCY_LIST_
	typedef struct _LIST_DEPINFO
	{
		DWORD dwSize _DEFAULT_INIT_VALUE_ (0);
		DEPENDENCY_INFO aDepInfo [1];
	} LIST_DEPINFO, *HLIST_DEPINFO;
#endif
	// 获取依赖项信息数组，返回 HLIST_DEPINFO。必须由 DestroyDependencesInfoList 销毁
	PKGREAD_API HLIST_DEPINFO GetDependencesInfoList (_In_ HPKGREAD hReader);
	// 通过 GetDependencesInfoList 返回的指针由此销毁
	PKGREAD_API void DestroyDependencesInfoList (_In_ HLIST_DEPINFO hList);

	// Capabilities
	PKGREAD_API HLIST_PVOID GetCapabilitiesList (_In_ HPKGREAD hReader);
	PKGREAD_API HLIST_PVOID GetDeviceCapabilitiesList (_In_ HPKGREAD hReader);
	PKGREAD_API void DestroyWStringList (_In_ HLIST_PVOID hList);
#define DestroyCapabilitiesList DestroyWStringList
#define DestroyDeviceCapabilitiesList DestroyWStringList

	// Prerequisite
#define PKG_PREREQUISITE_OS_MIN_VERSION L"OSMinVersion"
#define PKG_PREREQUISITE_OS_MAX_VERSION_TESTED L"OSMaxVersionTested"
	PKGREAD_API BOOL GetPackagePrerequisite (_In_ HPKGREAD hReader, _In_ LPCWSTR lpName, _Outptr_ VERSION *pVerRet);
#define GetPackagePrerequisiteOsMinVersion(_In_hReader_, _Outptr_pVerRet_) GetPackagePrerequisite (_In_hReader_, PKG_PREREQUISITE_OS_MIN_VERSION, _Outptr_pVerRet_)
#define GetPackagePrerequisiteOsMaxVersionTested(_In_hReader_, _Outptr_pVerRet_) GetPackagePrerequisite (_In_hReader_, PKG_PREREQUISITE_OS_MAX_VERSION_TESTED, _Outptr_pVerRet_)
	PKGREAD_API LPWSTR GetPackagePrerequistieSystemVersionName (_In_ HPKGREAD hReader, _In_ LPCWSTR lpName);

	// File Stream
	// 从 Appx 包中获取 Appx 中的文件的文件流。
	// 注：只提取 Payloads 文件。不可提供 Footprint 文件。
	// 对于 AppxBundle 包，则随机从一个应用包中提取文件。
	PKGREAD_API HANDLE GetAppxFileFromAppxPackage (_In_ HPKGREAD hReader, _In_ LPCWSTR lpFileName);
	// 从 AppxBundle 包中获取 Appx 子包的文件流。
	PKGREAD_API HANDLE GetAppxBundlePayloadPackageFile (_In_ HPKGREAD hReader, _In_ LPCWSTR lpFileName);
	// 从 Appx 包中获取 Appx 的 Pri 文件流
	// 对于 AppxBundle 包，则随机从一个应用包中提取 Pri 文件。
	PKGREAD_API HANDLE GetAppxPriFileStream (_In_ HPKGREAD hReader);
	// 从 AppxBundle 包中获取的子包的文件流中提取包中的文件
	// 注：只提取 Payloads 文件。不可提供 Footprint 文件。
	PKGREAD_API HANDLE GetFileFromPayloadPackage (_In_ HANDLE hPackageStream, _In_ LPCWSTR lpFileName);
	// 从 AppxBundle 包中获取的子包的文件流中提取 Pri 文件流
	PKGREAD_API HANDLE GetPriFileFromPayloadPackage (_In_ HANDLE hPackageStream);
	// 从 AppxBundle 包中获取合适的包，合适指的是资源合适。
	// 第一个用于返回的参数指的是返回的语言合适的选项，第二个用于返回的参数是返回的缩放资源。
	// 如果指向的都是同一个包，那么两个参数返回的都是同一个指针。
	// 对于 Appx 文件直接返回假。
	PKGREAD_API BOOL GetSuitablePackageFromBundle (_In_ HPKGREAD hReader, _Outptr_ HANDLE *pStreamForLang, _Outptr_ HANDLE *pStreamForScale);
	// 文件流必须通过此来释放。
	PKGREAD_API ULONG DestroyAppxFileStream (_In_ HANDLE hFileStream);
	// 转换：文件流转换到 Data URL。这样转换后浏览器可以使用图片。
	// 注意：dwCharCount 指的是 lpMimeBuf 的可容纳字符数，如 WCHAR lpMimeBuf [dwCharCount]，不是实际可容纳字节数。
	// 返回的非空指针需要通过 free 释放。lpBase64Head 返回的是 Base64 编码后的数据，为返回指针指向的部分。
	// 如字符串为 data:text/plain;base64,SGVsbG8sIFdvcmxkIQ%3D%3D
	// 那么：						    ↑ lpBase64Head 指向于“base64,”后的第一个字符。
	PKGREAD_API LPWSTR StreamToBase64W (_In_ HANDLE hFileStream, _Out_writes_ (dwCharCount) LPWSTR lpMimeBuf, _In_ DWORD dwCharCount, _Outptr_ LPWSTR *lpBase64Head);
	// 获取 AppxBundle 包中的应用包文件流。最后通过 DestroyAppxFileStream 销毁。
	PKGREAD_API HANDLE GetAppxBundleApplicationPackageFile (_In_ HPKGREAD hReader);
	// 获取功能名的显示名，如 internetClient 对应“访问您的 Internet 连接”。返回的是适应于系统区域语言的文本。
	// 注意：返回的字符串一定要通过 free 释放。
	PKGREAD_API LPWSTR GetPackageCapabilityDisplayName (LPCWSTR lpCapabilityName);
#ifdef _DEFAULT_INIT_VALUE_
#undef _DEFAULT_INIT_VALUE_
#endif
#ifdef _DEFAULT_INIT_VALUE_FORFUNC_
#undef _DEFAULT_INIT_VALUE_FORFUNC_
#endif
#ifdef __cplusplus
}
#endif

#if defined (__cplusplus)
#include <cstdlib>
#include <string>
#include <map>
#include <vector>
#include <functional>
#include <cstring>
#include <Shlwapi.h>
#include <algorithm>
//#include "..\priformatcli\priformatcli.h"
const std::vector <std::wstring> g_filepathitems =
{
	L"LockScreenLogo",
	L"Logo",
	L"SmallLogo",
	L"Square150x150Logo",
	L"Square30x30Logo",
	L"Square310x310Logo",
	L"Square44x44Logo",
	L"Square70x70Logo",
	L"Square71x71Logo",
	L"StartPage",
	L"Tall150x310Logo",
	L"VisualGroup",
	L"WideLogo",
	L"Wide310x150Logo",
	L"Executable"
};
std::map <std::wstring, std::wstring> g_capnamemap;
std::wstring GetPackageCapabilityDisplayName (const std::wstring &capname)
{
	try
	{
		if (g_capnamemap.find (capname) != g_capnamemap.end () && !g_capnamemap.at (capname).empty ()) return g_capnamemap.at (capname);
		else
		{
			LPWSTR lpstr = GetPackageCapabilityDisplayName (capname.c_str ());
			std::wstring ret = L"";
			ret += lpstr ? lpstr : L"";
			if (lpstr) free (lpstr);
			lpstr = nullptr;
			g_capnamemap [capname] = ret;
			return ret;
		}
	}
	catch (...)
	{
		LPWSTR lpstr = GetPackageCapabilityDisplayName (capname.c_str ());
		std::wstring ret = L"";
		ret += lpstr ? lpstr : L"";
		if (lpstr) free (lpstr);
		lpstr = nullptr;
		g_capnamemap [capname] = ret;
		return ret;
	}
	return L"";
}
class package_reader
{
	private:
	HPKGREAD hReader = nullptr;
	std::wstring filepath = L"";
	bool usepri = false;
	bool resswitch = false;
#ifdef _PRI_READER_CLI_HEADER_
	prifilebundle pribundlereader;
	std::vector <IStream *> prifilestreams;
#endif

	void initpri ()
	{
	#ifdef _PRI_READER_CLI_HEADER_
		pribundlereader.destroy ();
		for (auto &it : prifilestreams)
		{
			if (it != nullptr)
			{
				DestroyAppxFileStream (it);
				it = nullptr;
			}
		}
		prifilestreams.clear ();
		switch (this->package_type ())
		{
			case PKGTYPE_APPX: {
				IStream *pristream = (IStream *)GetAppxPriFileStream (hReader);
				if (pristream)
				{
					prifilestreams.push_back (pristream);
					pribundlereader.set (3, pristream);
				}
			} break;
			case PKGTYPE_BUNDLE: {
				HANDLE hls = nullptr, hss = nullptr;
				destruct rel1 ([&hls, &hss] () {
					if (hls == hss) hss = nullptr;
					if (hls) DestroyAppxFileStream (hls);
					if (hss) DestroyAppxFileStream (hss);
				});
				GetSuitablePackageFromBundle (hReader, &hls, &hss);
				HANDLE hlpri = GetPriFileFromPayloadPackage (hls),
					hspri = GetPriFileFromPayloadPackage (hss);
				IStream *ls = (IStream *)hls, *ss = (IStream *)hss;
				switch ((bool)ls << 1 | (bool)ss)
				{
					case 0b01:
					case 0b10: {
						if (hlpri) pribundlereader.set (1, (IStream *)hlpri);
						if (hspri) pribundlereader.set (2, (IStream *)hspri);
						if (hlpri) prifilestreams.push_back ((IStream *)hlpri);
						if (hspri) prifilestreams.push_back ((IStream *)hspri);
						HANDLE hd = GetAppxBundleApplicationPackageFile (hReader);
						destruct relthd ([&hd] () {
							if (hd) DestroyAppxFileStream (hd);
						});
						HANDLE hdpri = GetPriFileFromPayloadPackage (hd);
						if (hd) pribundlereader.set (3, (IStream *)hd);
						if (hd) prifilestreams.push_back ((IStream *)hdpri);
					} break;
					case 0b11: {
						if (ls) pribundlereader.set (1, (IStream *)hlpri);
						if (ss) pribundlereader.set (2, (IStream *)hspri);
						if (ls && hlpri) prifilestreams.push_back ((IStream *)hlpri);
						if (ss && hspri) prifilestreams.push_back ((IStream *)hspri);
					} break;
					default:
					case 0b00: {
						IStream *pkgstream = (IStream *)GetAppxBundleApplicationPackageFile (hReader);
						destruct relthd ([&pkgstream] () {
							if (pkgstream) DestroyAppxFileStream (pkgstream);
						});
						IStream *pristream = (IStream *)GetPriFileFromPayloadPackage (pkgstream);
						if (pristream)
						{
							prifilestreams.push_back (pristream);
							pribundlereader.set (3, pristream);
						}
					} break;
				}
			} break;
		}
		std::sort (prifilestreams.begin (), prifilestreams.end ());
		auto last = std::unique (prifilestreams.begin (), prifilestreams.end ());
		prifilestreams.erase (last, prifilestreams.end ());
		try
		{
			std::vector <std::wstring> resnames;
			{
				auto prop = get_properties ();
				std::wstring temp = prop.description ();
				if (IsMsResourcePrefix (temp.c_str ())) resnames.push_back (temp);
				temp = prop.display_name ();
				if (IsMsResourcePrefix (temp.c_str ())) resnames.push_back (temp);
				temp = prop.publisher_display_name ();
				if (IsMsResourcePrefix (temp.c_str ())) resnames.push_back (temp);
				resnames.push_back (prop.logo ());
			}
			{
				auto app = get_applications ();
				std::vector <application> apps;
				app.get (apps);
				for (auto &it_map : apps)
				{
					for (auto &it_item : it_map)
					{
						if (std::find (g_filepathitems.begin (), g_filepathitems.end (), it_item.first) != g_filepathitems.end () && !it_item.second.empty ())
							resnames.push_back (it_item.second);
						else if (IsMsResourcePrefix (it_item.second.c_str ())) resnames.push_back (it_item.second);
					}
				}
			}
			pribundlereader.add_search (resnames);
		}
		catch (const std::exception &e) {}
	#endif
	}
	typedef struct deconstr
	{
		std::function <void ()> endtask = nullptr;
		deconstr (std::function <void ()> pf): endtask (pf) {}
		~deconstr () { if (endtask) endtask (); }
	} destruct;
	public:
	class base_subitems
	{
		protected:
		HPKGREAD &hReader;
		public:
		base_subitems (HPKGREAD &hReader): hReader (hReader) {}
	};
	class identity: public base_subitems
	{
		using base = base_subitems;
		public:
		using base::base;
		std::wstring string_value (DWORD dwName) const
		{
			LPWSTR lpstr = nullptr;
			deconstr rel ([&lpstr] () {
				if (lpstr) free (lpstr);
				lpstr = nullptr;
			});
			lpstr = GetPackageIdentityStringValue (hReader, dwName);
			return lpstr ? lpstr : L"";
		}
		std::wstring name () const { return string_value (PKG_IDENTITY_NAME); }
		std::wstring publisher () const { return string_value (PKG_IDENTITY_PUBLISHER); }
		std::wstring package_family_name () const { return string_value (PKG_IDENTITY_PACKAGEFAMILYNAME); }
		std::wstring package_full_name () const { return string_value (PKG_IDENTITY_PACKAGEFULLNAME); }
		std::wstring resource_id () const { return string_value (PKG_IDENTITY_RESOURCEID); }
		VERSION version (bool read_subpkg_ver = false) const { VERSION ver; GetPackageIdentityVersion (hReader, &ver, read_subpkg_ver); return ver; }
		DWORD architecture () const { DWORD dw = 0; GetPackageIdentityArchitecture (hReader, &dw); return dw; }
	};
	class properties: public base_subitems
	{
		using base = base_subitems;
		public:
		using base::base;
		std::wstring string_value (const std::wstring &swName) const
		{
			LPWSTR lpstr = nullptr;
			deconstr rel ([&lpstr] () {
				if (lpstr) free (lpstr);
				lpstr = nullptr;
			});
			lpstr = GetPackagePropertiesStringValue (hReader, swName.c_str ());
			return lpstr ? lpstr : L"";
		}
		bool bool_value (const std::wstring &swName, bool bRetWhenFailed = false) const
		{
			BOOL ret = FALSE;
			HRESULT hr = GetPackagePropertiesBoolValue (hReader, swName.c_str (), &ret);
			if (FAILED (hr)) return bRetWhenFailed;
			else return ret != FALSE;
		}
		std::wstring display_name (bool toprires = true) 
		{ 
			std::wstring ret = string_value (PKG_PROPERTIES_DISPLAYNAME);
			if (!toprires) return ret;
			if (!enable_pri ()) return ret;
		#ifdef _PRI_READER_CLI_HEADER_
			else
			{
				if (!IsMsResourcePrefix (ret.c_str ())) return ret;
				std::wstring privalue = pri_get_res (ret);
				if (privalue.empty ()) return ret;
				return privalue;
			}
		#endif
			return ret;
		}
		std::wstring publisher_display_name (bool toprires = true) 
		{ 
			std::wstring ret = string_value (PKG_PROPERTIES_PUBLISHER);
			if (!toprires) return ret;
			if (!enable_pri ()) return ret;
		#ifdef _PRI_READER_CLI_HEADER_
			else
			{
				if (!IsMsResourcePrefix (ret.c_str ())) return ret;
				std::wstring privalue = pri_get_res (ret);
				if (privalue.empty ()) return ret;
				return privalue;
			}
		#endif
			return ret;
		}
		std::wstring description (bool toprires = true) 
		{ 
			std::wstring ret = string_value (PKG_PROPERTIES_DESCRIPTION);
			if (!toprires) return ret;
			if (!enable_pri ()) return ret;
		#ifdef _PRI_READER_CLI_HEADER_
			else
			{
				if (!IsMsResourcePrefix (ret.c_str ())) return ret;
				std::wstring privalue = pri_get_res (ret);
				if (privalue.empty ()) return ret;
				return privalue;
			}
		#endif
			return ret;
		}
		std::wstring logo (bool toprires = true)
		{ 
			std::wstring ret = string_value (PKG_PROPERTIES_LOGO);
			if (!toprires) return ret;
			if (!enable_pri ()) return ret;
		#ifdef _PRI_READER_CLI_HEADER_
			else
			{
				std::wstring privalue = pri_get_res (ret);
				if (privalue.empty ()) return ret;
				return privalue;
			}
		#endif
			return ret;
		}
		std::wstring logo_base64 ()
		{
			switch (GetPackageType (hReader))
			{
				case PKGTYPE_APPX: {
					HANDLE pic = GetAppxFileFromAppxPackage (hReader, logo ().c_str ());
					destruct relp ([&pic] () {
						if (pic) DestroyAppxFileStream (pic);
						pic = nullptr;
					});
					LPWSTR lpstr = nullptr;
					destruct rel ([&lpstr] () {
						if (lpstr) free (lpstr);
						lpstr = nullptr;
					});
					lpstr = StreamToBase64W (pic, nullptr, 0, nullptr);
					return lpstr ? lpstr : L"";
				} break;
				case PKGTYPE_BUNDLE: {
					HANDLE pkg = nullptr, pic = nullptr;
					destruct relp ([&pic, &pkg] () {
						if (pic == pkg) pkg = nullptr; // 虽然来说这个代码永远无法达到，但是保险起见
						if (pic) DestroyAppxFileStream (pic);
						if (pkg) DestroyAppxFileStream (pkg);
						pkg = nullptr;
						pic = nullptr;
					});
					GetSuitablePackageFromBundle (hReader, nullptr, &pkg);
					pic = GetFileFromPayloadPackage (pkg, logo ().c_str ());
					LPWSTR lpstr = nullptr;
					destruct rel ([&lpstr] () {
						if (lpstr) free (lpstr);
						lpstr = nullptr;
					});
					lpstr = StreamToBase64W (pic, nullptr, 0, nullptr);
					if (!(lpstr && *lpstr))
					{
						if (lpstr) free (lpstr);
						HANDLE pkg1 = nullptr, pic1 = nullptr;
						destruct relp1 ([&pic1, &pkg1] () {
							if (pic1 == pkg1) pkg1 = nullptr; // 虽然来说这个代码永远无法达到，但是保险起见
							if (pic1) DestroyAppxFileStream (pic1);
							if (pkg1) DestroyAppxFileStream (pkg1);
							pkg1 = nullptr;
							pic1 = nullptr;
						});
						pkg1 = GetAppxBundleApplicationPackageFile (hReader);
						if (pkg1 == pkg) pkg = nullptr;
						if (pkg1)
						{
							pic1 = GetFileFromPayloadPackage (pkg1, logo ().c_str ());
							lpstr = StreamToBase64W (pic1, nullptr, 0, nullptr);
						}
					}
					return lpstr ? lpstr : L"";
				} break;
			}
			return L"";
		}
		bool framework () const { return bool_value (PKG_PROPERTIES_FRAMEWORD); }
		bool resource_package () const { return bool_value (PKG_PROPERTIES_IS_RESOURCE); }
	#ifdef _PRI_READER_CLI_HEADER_
		prifilebundle *pbreader = nullptr;
		bool *usepri = nullptr;
		bool *resconvert = nullptr;
	#endif
		bool enable_pri () const
		{
		#ifdef _PRI_READER_CLI_HEADER_
			if (!pbreader) return false;
			if (!usepri || !*usepri) return false;
			if (!resconvert) return false;
			return *resconvert;
		#else
			return false;
		#endif
		}
		std::wstring pri_get_res (const std::wstring &resname)
		{
		#ifdef _PRI_READER_CLI_HEADER_
			if (resname.empty ()) return L"";
			if (!pbreader) return L"";
			return pbreader->resource (resname);
		#else
			return L"";
		#endif
		}
	#ifdef _PRI_READER_CLI_HEADER_
		properties (HPKGREAD &hReader, prifilebundle *pri, bool *up, bool *resc):
			base (hReader), pbreader (pri), usepri (up), resconvert (resc) {}
	#endif
	};
	class application: public std::map <std::wstring, std::wstring>
	{
		using base = std::map <std::wstring, std::wstring>;
		public:
		using base::base;
		application () = default;
		std::wstring user_model_id () { return this->at (L"AppUserModelID"); }
		friend bool operator == (application &a1, application &a2) { return !_wcsicmp (a1.user_model_id ().c_str (), a2.user_model_id ().c_str ()); }
		friend bool operator != (application &a1, application &a2) { return _wcsicmp (a1.user_model_id ().c_str (), a2.user_model_id ().c_str ()); }
		explicit operator bool () { return this->user_model_id ().empty (); }
		std::wstring &operator [] (const std::wstring &key)
		{
			auto it = this->find (key);
			if (it == this->end ())
			{
				it = this->insert (std::make_pair (key, L"")).first;
			}
			if (!enable_pri ()) return it->second;
		#ifdef _PRI_READER_CLI_HEADER_
			if (IsMsResourcePrefix (it->second.c_str ()))
			{
				std::wstring privalue = pri_get_res (it->second);
				if (!privalue.empty ()) return privalue;
				return it->second;
			}
			else if (std::find (g_filepathitems.begin (), g_filepathitems.end (), it->first) != g_filepathitems.end () && !it->second.empty ())
			{
				std::wstring privalue = pri_get_res (it->second);
				if (!privalue.empty ()) return privalue;
				return it->second;
			}
		#endif
			return it->second;
		}
		typename base::iterator find_case_insensitive (const std::wstring &key)
		{
			for (auto it = this->begin (); it != this->end (); ++it)
			{
				if (_wcsicmp (it->first.c_str (), key.c_str ()) == 0)
					return it;
			}
			return this->end ();
		}
		typename base::const_iterator find_case_insensitive (const std::wstring &key) const
		{
			for (auto it = this->begin (); it != this->end (); ++ it)
			{
				if (_wcsicmp (it->first.c_str (), key.c_str ()) == 0)
					return it;
			}
			return this->end ();
		}
		std::wstring at (const std::wstring &key)
		{
			auto it = this->find_case_insensitive (key);
			if (it == this->end ()) throw std::out_of_range ("application::at: key not found");
			if (!enable_pri ()) return it->second;
		#ifdef _PRI_READER_CLI_HEADER_
			if (IsMsResourcePrefix (it->second.c_str ()))
			{
				std::wstring privalue = pri_get_res (it->second);
				if (!privalue.empty ()) return privalue;
			}
		#endif
			return it->second;
		}
		std::wstring newat (const std::wstring &key, bool to_pri_string = true)
		{
			auto it = this->find (key);
			if (it == this->end ())
			{
				it = this->insert (std::make_pair (key, L"")).first;
			}
			if (!enable_pri () && to_pri_string) return it->second;
		#ifdef _PRI_READER_CLI_HEADER_
			if (IsMsResourcePrefix (it->second.c_str ()))
			{
				std::wstring privalue = pri_get_res (it->second);
				if (!privalue.empty ()) return privalue;
				return it->second;
			}
			else if (std::find (g_filepathitems.begin (), g_filepathitems.end (), it->first) != g_filepathitems.end () && !it->second.empty ())
			{
				std::wstring privalue = pri_get_res (it->second);
				if (!privalue.empty ()) return privalue;
				return it->second;
			}
		#endif
			return it->second;
		}
		// 仅支持文件
		std::wstring newat_base64 (const std::wstring &key)
		{
		#ifdef _PRI_READER_CLI_HEADER_
			std::wstring value = newat (key);
			if (std::find (g_filepathitems.begin (), g_filepathitems.end (), key) != g_filepathitems.end () && !value.empty ())
			{
				switch (GetPackageType (hReader))
				{
					case PKGTYPE_APPX: {
						HANDLE pic = GetAppxFileFromAppxPackage (hReader, value.c_str ());
						destruct relp ([&pic] () {
							if (pic) DestroyAppxFileStream (pic);
							pic = nullptr;
						});
						LPWSTR lpstr = nullptr;
						destruct rel ([&lpstr] () {
							if (lpstr) free (lpstr);
							lpstr = nullptr;
						});
						lpstr = StreamToBase64W (pic, nullptr, 0, nullptr);
						return lpstr ? lpstr : L"";
					} break;
					case PKGTYPE_BUNDLE: {
						HANDLE pkg = nullptr, pic = nullptr;
						destruct relp ([&pic, &pkg] () {
							if (pic == pkg) pkg = nullptr; // 虽然来说这个代码永远无法达到，但是保险起见
							if (pic) DestroyAppxFileStream (pic);
							if (pkg) DestroyAppxFileStream (pkg);
							pkg = nullptr;
							pic = nullptr;
						});
						GetSuitablePackageFromBundle (hReader, nullptr, &pkg);
						pic = GetFileFromPayloadPackage (pkg, value.c_str ());
						LPWSTR lpstr = nullptr;
						destruct rel ([&lpstr] () {
							if (lpstr) free (lpstr);
							lpstr = nullptr;
						});
						lpstr = StreamToBase64W (pic, nullptr, 0, nullptr);
						return lpstr ? lpstr : L"";
					} break;
				}
				return L"";
			}
			else return L"";
		#else
			return L"";
		#endif
		}
	#ifdef _PRI_READER_CLI_HEADER_
		HPKGREAD hReader = nullptr;
		prifilebundle *pbreader = nullptr;
		bool *usepri = nullptr;
		bool *resconvert = nullptr;
	#endif
		bool enable_pri () const
		{
		#ifdef _PRI_READER_CLI_HEADER_
			if (!pbreader) return false;
			if (!usepri || !*usepri) return false;
			if (!resconvert) return false;
			return *resconvert;
		#else
			return false;
		#endif
		}
		std::wstring pri_get_res (const std::wstring &resname)
		{
		#ifdef _PRI_READER_CLI_HEADER_
			if (resname.empty ()) return L"";
			if (!pbreader) return L"";
			return pbreader->resource (resname);
		#else
			return L"";
		#endif
		}
	#ifdef _PRI_READER_CLI_HEADER_
		application (HPKGREAD hReader, prifilebundle *pri, bool *up, bool *resc):
			hReader (hReader), pbreader (pri), usepri (up), resconvert (resc) {}
	#endif
	};
	class applications
	{
		private:
		HPKGREAD &hReader;
		HAPPENUMERATOR hList = nullptr;
		public:
		applications (HPKGREAD &hReader): hReader (hReader)
		{
			hList = GetPackageApplications (hReader);
		}
		~applications ()
		{
			if (hList) DestroyPackageApplications (hList);
			hList = nullptr;
		}
		size_t get (std::vector <application> &apps)
		{
			apps.clear ();
			if (!hList) return 0;
			HLIST_PVOID hMapList = ApplicationsToMap (hList);
			deconstr endt ([&hMapList] {
				if (hMapList) DestroyApplicationsMap (hMapList);
				hMapList = nullptr;
			});
			if (!hMapList) return 0;
			for (size_t i = 0; i < hMapList->dwSize; i ++)
			{
				HLIST_PVOID &hKeyValues = ((HLIST_PVOID *)hMapList->alpVoid) [i];
			#ifdef _PRI_READER_CLI_HEADER_
				application app (hReader, pbreader, usepri, resconvert);
			#else
				application app;
			#endif
				for (size_t j = 0; j < hKeyValues->dwSize; j ++)
				{
					HPAIR_PVOID &hPair = ((HPAIR_PVOID *)hKeyValues->alpVoid) [j];
					LPWSTR lpKey = (LPWSTR)hPair->lpKey;
					LPWSTR lpValue = (LPWSTR)hPair->lpValue;
					if (!lpKey || !*lpKey) continue;
					app [lpKey] = lpValue ? lpValue : L"";
				}
				apps.push_back (app);
			}
			return apps.size ();
		}
	#ifdef _PRI_READER_CLI_HEADER_
		prifilebundle *pbreader = nullptr;
		bool *usepri = nullptr;
		bool *resconvert = nullptr;
		applications (HPKGREAD &hReader, prifilebundle *pri, bool *up, bool *resc):
			hReader (hReader), pbreader (pri), usepri (up), resconvert (resc) { hList = GetPackageApplications (hReader); }
	#endif
	};
	class capabilities: public base_subitems
	{
		using base = base_subitems;
		public:
		using base::base;
		size_t capabilities_name (std::vector <std::wstring> &output) const
		{
			output.clear ();
			HLIST_PVOID hList = GetCapabilitiesList (hReader);
			deconstr endt ([&hList] () {
				if (hList) DestroyCapabilitiesList (hList);
				hList = nullptr;
			});
			if (!hList) return 0;
			for (size_t i = 0; i < hList->dwSize; i ++)
			{
				LPWSTR lpstr = (LPWSTR)hList->alpVoid [i];
				if (!lpstr) continue;
				output.push_back (lpstr);
			}
			return output.size ();
		}
		size_t device_capabilities (std::vector <std::wstring> &output) const
		{
			output.clear ();
			HLIST_PVOID hList = GetDeviceCapabilitiesList (hReader);
			deconstr endt ([&hList] () {
				if (hList) DestroyDeviceCapabilitiesList (hList);
				hList = nullptr;
			});
			if (!hList) return 0;
			for (size_t i = 0; i < hList->dwSize; i ++)
			{
				LPWSTR lpstr = (LPWSTR)hList->alpVoid [i];
				if (!lpstr) continue;
				output.push_back (lpstr);
			}
			return output.size ();
		}
	};
	struct dependency
	{
		std::wstring name, publisher;
		VERSION vermin;
		dependency (const std::wstring &name = L"", const std::wstring &pub = L"", const VERSION &ver = VERSION ()):
			name (name), publisher (pub), vermin (ver) {}
	};
	class dependencies: public base_subitems
	{
		using base = base_subitems;
		public:
		using base::base;
		size_t get (std::vector <dependency> &output) const
		{
			auto hList = GetDependencesInfoList (hReader);
			deconstr rel ([&hList] () {
				DestroyDependencesInfoList (hList);
			});
			if (!hList) return 0;
			for (size_t i = 0; i < hList->dwSize; i ++)
			{
				DEPENDENCY_INFO &depinf = ((DEPENDENCY_INFO *)hList->aDepInfo) [i];
				if (!depinf.lpName || !*depinf.lpName) continue;
				output.push_back (dependency (depinf.lpName, depinf.lpPublisher ? depinf.lpPublisher : L"", depinf.verMin));
			}
			return output.size ();
		}
	};
	class resources: public base_subitems
	{
		using base = base_subitems;
		public:
		using base::base;
		size_t languages (std::vector <std::wstring> &langs) const
		{
			langs.clear ();
			auto hList = GetResourcesLanguages (hReader);
			deconstr rel ([&hList] () {
				if (hList) DestroyResourcesLanguagesList (hList);
				hList = nullptr;
			});
			if (!hList) return 0;
			for (size_t i = 0; i < hList->dwSize; i ++)
			{
				LPWSTR lpstr = ((LPWSTR *)hList->alpVoid) [i];
				if (lpstr && *lpstr) langs.push_back (std::wstring (lpstr));
			}
			return langs.size ();
		}
		size_t languages (std::vector <LCID> &langs) const
		{
			langs.clear ();
			auto hList = GetResourcesLanguagesToLcid (hReader);
			deconstr rel ([&hList] () {
				if (hList) DestroyResourcesLanguagesLcidList (hList);
				hList = nullptr;
			});
			if (!hList) return 0;
			for (size_t i = 0; i < hList->dwSize; i ++)
			{
				if (hList->aLcid [i]) langs.push_back (hList->aLcid [i]);
			}
			return langs.size ();
		}
		size_t scales (std::vector <UINT32> &output) const
		{
			output.clear ();
			auto hList = GetResourcesScales (hReader);
			deconstr rel ([&hList] () {
				if (hList) DestroyResourcesScalesList (hList);
			});
			if (!hList) return 0;
			for (size_t i = 0; i < hList->dwSize; i ++)
			{
				UINT32 s = hList->aUI32 [i];
				if (s) output.push_back (s);
			}
			return output.size ();
		}
		DWORD dx_feature_level () const { return GetResourcesDxFeatureLevels (hReader); }
		// 向数组添加的是以 PKG_RESOURCES_DXFEATURE_* 前缀的常量
		size_t dx_feature_level (std::vector <DWORD> &ret)
		{
			DWORD dx = dx_feature_level ();
			if (dx & PKG_RESOURCES_DXFEATURE_LEVEL9) ret.push_back (PKG_RESOURCES_DXFEATURE_LEVEL9);
			else if (dx & PKG_RESOURCES_DXFEATURE_LEVEL10) ret.push_back (PKG_RESOURCES_DXFEATURE_LEVEL10);
			else if (dx & PKG_RESOURCES_DXFEATURE_LEVEL11) ret.push_back (PKG_RESOURCES_DXFEATURE_LEVEL11);
			else if (dx & PKG_RESOURCES_DXFEATURE_LEVEL12) ret.push_back (PKG_RESOURCES_DXFEATURE_LEVEL12);
			return ret.size ();
		}
	};
	class prerequisites: public base_subitems
	{
		using base = base_subitems;
		public:
		using base::base;
		VERSION get_version (const std::wstring &name) const
		{
			VERSION ver;
			GetPackagePrerequisite (hReader, name.c_str (), &ver);
			return ver;
		}
		VERSION os_min_version () const { return get_version (PKG_PREREQUISITE_OS_MIN_VERSION); }
		VERSION os_max_version_tested () const { return get_version (PKG_PREREQUISITE_OS_MAX_VERSION_TESTED); }
		std::wstring get_description (const std::wstring &name) const
		{
			LPWSTR lpstr = GetPackagePrerequistieSystemVersionName (hReader, name.c_str ());
			deconstr relt ([&lpstr] () {
				if (lpstr) free (lpstr);
				lpstr = nullptr;
			});
			return lpstr ? lpstr : L"";
		}
		std::wstring os_min_version_description () const { return get_description (PKG_PREREQUISITE_OS_MIN_VERSION); }
		std::wstring os_max_version_tested_description () const { return get_description (PKG_PREREQUISITE_OS_MAX_VERSION_TESTED); }
	};
	package_reader (): hReader (CreatePackageReader ()) {}
	package_reader (const std::wstring &fpath): hReader (CreatePackageReader ())
	{
		file (fpath);
	}
	~package_reader () 
	{ 
		DestroyPackageReader (hReader); 
		hReader = nullptr;
	#ifdef _PRI_READER_CLI_HEADER_
		pribundlereader.destroy ();
		for (auto &it : prifilestreams) if (it != nullptr) DestroyAppxFileStream (it);
		prifilestreams.clear ();
	#endif
	}
	std::wstring file () const { return filepath; }
	bool file (const std::wstring &path)
	{
		return LoadPackageFromFile (hReader, (filepath = path).c_str ());
	}
	// PKGTYPE_* 前缀常量
	WORD package_type () const { return GetPackageType (hReader); }
	bool valid () const { return hReader && IsPackageValid (hReader); }
	// PKGROLE_* 前缀常量
	WORD package_role () const { return GetPackageRole (hReader); }
	identity get_identity () { return identity (hReader); }
	properties get_properties () 
	{
		return properties (hReader
		#ifdef _PRI_READER_CLI_HEADER_
			,
			&pribundlereader,
			&usepri,
			&resswitch
		#endif
		); 
	}
	applications get_applications () 
	{ 
		return applications (hReader
		#ifdef _PRI_READER_CLI_HEADER_
			,
			&pribundlereader,
			&usepri,
			&resswitch
		#endif
		); 
	}
	capabilities get_capabilities () { return capabilities (hReader); }
	dependencies get_dependencies () { return dependencies (hReader); }
	resources get_resources () { return resources (hReader); }
	prerequisites get_prerequisites () { return prerequisites (hReader); }
	// 是否允许使用 PRI
	bool use_pri () const 
	{ 
	#ifdef _PRI_READER_CLI_HEADER_
		return usepri;
	#else
		return false;
	#endif
	}
	// 是否允许使用 PRI
	bool use_pri (bool value)
	{
	#ifdef _PRI_READER_CLI_HEADER_
		bool laststatus = usepri;
		usepri = value;
		if (laststatus ^ usepri) initpri ();
		return usepri;
	#else
		return usepri = false;
	#endif
	}
	// 是否自动从 PRI 中提取资源
	bool enable_pri_convert () const { return resswitch; }
	// 是否自动从 PRI 中提取资源
	bool enable_pri_convert (bool value) 
	{
	#ifdef _PRI_READER_CLI_HEADER_
		return resswitch = value;
	#else
		return resswitch = false;
	#endif
	}
};
#endif

#endif
