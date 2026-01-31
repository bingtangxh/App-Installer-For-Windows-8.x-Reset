#pragma once
#include <map>
#include <string>
#include <vector>
#include <Shlwapi.h>
#include <AppxPackaging.h>
#include <strsafe.h>
#include <sstream>
#include <atlbase.h>
#include <functional>
#include <utility>
#include <xmllite.h>
#include "dynarr.h"
#include "version.h"
#include "stringres.h"
#include "norstr.h"
#include "raii.h"
#include "filepath.h"

HRESULT GetBundleReader (_In_ LPCWSTR inputFileName, _Outptr_ IAppxBundleReader** bundleReader)
{
	if (bundleReader == NULL) return E_POINTER;
	*bundleReader = NULL;
	HRESULT hr = S_OK;
	CComPtr <IAppxBundleFactory> appxBundleFactory;
	CComPtr <IStream> inputStream;
	hr = CoCreateInstance (__uuidof(AppxBundleFactory), NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS (&appxBundleFactory));
	if (FAILED (hr)) return hr;
	hr = SHCreateStreamOnFileEx (inputFileName, STGM_READ | STGM_SHARE_DENY_NONE, 0, FALSE, NULL, &inputStream);
	if (FAILED (hr)) return hr;
	hr = appxBundleFactory->CreateBundleReader (inputStream, bundleReader);
	return hr;
}
HRESULT GetPackageReader (_In_ LPCWSTR inputFileName, _Outptr_ IAppxPackageReader **reader)
{
	if (reader == NULL) return E_POINTER;
	*reader = NULL;
	HRESULT hr = S_OK;
	CComPtr <IAppxFactory> appxFactory;
	CComPtr <IStream> inputStream;
	hr = CoCreateInstance (__uuidof(AppxFactory), NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS (&appxFactory));
	if (FAILED (hr)) return hr;
	hr = SHCreateStreamOnFileEx (inputFileName, STGM_READ | STGM_SHARE_DENY_NONE, 0, FALSE, NULL, &inputStream);
	if (FAILED (hr)) return hr;
	hr = appxFactory->CreatePackageReader (inputStream, reader);
	return hr;
}
HRESULT GetAppxPackageReader (_In_ IStream *inputStream, _Outptr_ IAppxPackageReader **packageReader)
{
	HRESULT hr = S_OK;
	CComPtr <IAppxFactory> appxFactory = NULL;
	hr = CoCreateInstance (__uuidof (AppxFactory), NULL, CLSCTX_INPROC_SERVER, __uuidof (IAppxFactory), (LPVOID *)(&appxFactory));
	if (SUCCEEDED (hr)) hr = appxFactory->CreatePackageReader (inputStream, packageReader);
	return hr;
}
HRESULT GetAppxManifestReader2 (IAppxManifestReader *m, IAppxManifestReader2 **m2)
{
	if (!m | !m2) return E_FAIL;
	return m->QueryInterface (__uuidof (IAppxManifestReader2), (void **)m2);
}

class app_info: public std::map <std::wnstring, std::wstring>
{
	using Base = std::map <std::wnstring, std::wstring>;
	public:
	using Base::Base;
	std::wstring user_model_id () const { return this->find (L"AppUserModelID")->second; }
	friend bool operator == (const app_info &a1, const app_info &a2) { return std::wnstring (a1.user_model_id ()) == std::wnstring (a2.user_model_id ()); }
	friend bool operator != (const app_info &a1, const app_info &a2) { return std::wnstring (a1.user_model_id ()) != std::wnstring (a2.user_model_id ()); }
	explicit operator bool () const { return std::wnstring::to_nstring (this->user_model_id ()).empty (); }
};

class dep_info
{
	public:
	std::wstring name = L"";
	std::wstring publisher = L"";
	version minversion;
	dep_info (const std::wstring &name, const std::wstring &publisher, version versionLimit):
		name (std::wnstring::trim (name)), publisher (std::wnstring::trim (publisher)), minversion (versionLimit) {}
	dep_info (IAppxManifestPackageDependency *&dependency)
	{
		LPWSTR lpName = nullptr, lpPublisher = nullptr;
		UINT64 u64Ver = 0;
		raii rel ([&lpName, &lpPublisher] () -> void {
			if (lpName) CoTaskMemFree (lpName);
			if (lpPublisher) CoTaskMemFree (lpPublisher);
		});
		if (SUCCEEDED (dependency->GetName (&lpName)) && lpName) name += lpName;
		if (SUCCEEDED (dependency->GetPublisher (&lpPublisher)) && lpPublisher) publisher += lpPublisher;
		if (SUCCEEDED (dependency->GetMinVersion (&u64Ver))) minversion = u64Ver;
	}
	dep_info () {}
	bool operator == (const dep_info &another) { return std::wnstring (this->name) == std::wnstring (another.name); }
	bool empty () { return std::wnstring (name).empty (); }
};

template <typename GetType, typename RetType = GetType> RetType process_default (GetType &g)
{
	return RetType (g);
}
template <typename EnumerationType, typename GetType, typename RetType = GetType, typename RetVectorType = std::vector <RetType>>
size_t EnumerateComInterface (EnumerationType *iEnum, RetVectorType &retarr, std::function <RetType (GetType &)> process = process_default <GetType, RetType>)
{
	BOOL hasCurrent;
	HRESULT hr = iEnum->GetHasCurrent (&hasCurrent);
	if (SUCCEEDED (hr) && hasCurrent) retarr.reserve (8);
	while (SUCCEEDED (hr) && hasCurrent)
	{
		GetType value;
		hr = iEnum->GetCurrent (&value);
		if (SUCCEEDED (hr))
		{
			try
			{
				push_normal (retarr, process (value));
			}
			catch (const std::exception &e) {}
		}
		hr = iEnum->MoveNext (&hasCurrent);
	}
	return retarr.size ();
}
template <typename T> bool is_data_valid_default (T &data) { return true; }

enum class PackageType: UINT8
{
	unknown = 0,	// 未知，也是无效包
	single = 1,		// Appx 包
	bundle = 2		// AppxBundle 包
};
enum class PackageRole: UINT8
{
	unknown = 0,		// 未知
	application = 1,	// 应用包，安装后是为可以运行的包
	framework = 2,		// 框架包（依赖项），安装后可为其他人
	resource = 3		// 资源包，为应用包提供语言等资源
};
template <typename ComInterface> class com_info
{
	private:
	CComPtr <ComInterface> icom = nullptr;
	protected:
	ComInterface *pointer () const noexcept { return icom; }
	void attach (ComInterface *ptr) { icom.Attach (ptr); }
	void set (ComInterface *ptr) { icom = ptr; }
	template <typename Fn> std::wstring get (_In_ Fn func) const
	{
		if (!icom) return L"";
		LPWSTR lpstr = nullptr;
		raii task ([&lpstr] () {
			if (lpstr) CoTaskMemFree (lpstr);
			lpstr = nullptr;
		});
		return (SUCCEEDED ((icom->*func)(&lpstr)) && lpstr) ? std::wstring (lpstr) : L"";
	}
	template <typename GetType, typename RetType = GetType, typename Fn> RetType get (_In_ Fn func, _In_ std::function <RetType (GetType &)> process = process_default <GetType, RetType>) const
	{
		if (!icom) return RetType ();
		GetType value {};
		if (SUCCEEDED ((icom->*func) (&value))) return process (value);
		else return RetType ();
	}
	template <typename EnumInterfaceType, typename Fn> size_t enumerate (_In_ Fn func, _Out_ std::vector <std::wstring> &output) const
	{
		output.clear ();
		if (!icom) return 0;
		CComPtr <EnumInterfaceType> ienum;
		if (FAILED ((icom->*func) (&ienum))) return 0;
		return EnumerateComInterface <EnumInterfaceType, LPWSTR, std::wstring> (ienum, output, [] (LPWSTR &lpstr) -> std::wstring {
			raii reltask ([&lpstr] () { if (lpstr) { CoTaskMemFree (lpstr); lpstr = nullptr; } });
			return std::wstring (lpstr ? lpstr : L"");
		});
	}
	template <typename EnumInterfaceType, typename GetType, typename RetType = GetType, typename RetVectorType = std::vector <RetType>, typename Fn> size_t enumerate (_In_ Fn func, _Out_ RetVectorType &output, _In_ std::function <RetType (GetType &)> process = process_default <GetType, RetType>) const
	{
		output.clear ();
		if (!icom) return 0;
		CComPtr <EnumInterfaceType> ienum;
		if (FAILED (icom->*func (&ienum)) || !ienum) return 0;
		return EnumerateComInterface <EnumInterfaceType, GetType, RetType, RetVectorType> (ienum, output, process);
	}
	public:
	using Interface = ComInterface;
	explicit com_info (ComInterface *ptr) { icom.Attach (ptr); }
	com_info (const com_info &) = delete;
	com_info &operator = (const com_info &) = delete;
	com_info (com_info &&) noexcept = default;
	com_info &operator = (com_info &&) noexcept = default;
	bool valid () const { return !!icom; }
};
template <typename ComInterface> class com_info_quote
{
	using IComInterface = ComInterface *;
	private:
	IComInterface &icom = nullptr;
	protected:
	template <typename Fn> std::wstring get (_In_ Fn func) const
	{
		if (!icom) return L"";
		LPWSTR lpstr = nullptr;
		raii task ([&lpstr] () {
			if (lpstr) CoTaskMemFree (lpstr);
			lpstr = nullptr;
		});
		return (SUCCEEDED ((icom->*func)(&lpstr)) && lpstr) ? std::wstring (lpstr) : L"";
	}
	template <typename GetType, typename RetType = GetType, typename Fn> RetType get (_In_ Fn func, _In_ std::function <RetType (GetType &)> process = process_default <GetType, RetType>) const
	{
		if (!icom) return RetType ();
		GetType value {};
		if (SUCCEEDED ((icom->*func) (&value))) return process (value);
		else return RetType ();
	}
	template <typename EnumInterfaceType, typename Fn> size_t enumerate (_In_ Fn func, _Out_ std::vector <std::wstring> &output) const
	{
		output.clear ();
		if (!icom) return 0;
		CComPtr <EnumInterfaceType> ienum;
		if (FAILED ((icom->*func) (&ienum))) return 0;
		return EnumerateComInterface <EnumInterfaceType, LPWSTR, std::wstring> (ienum, output, [] (LPWSTR &lpstr) -> std::wstring {
			raii reltask ([&lpstr] () { if (lpstr) { CoTaskMemFree (lpstr); lpstr = nullptr; } });
			return std::wstring (lpstr ? lpstr : L"");
		});
	}
	template <typename EnumInterfaceType, typename GetType, typename RetType = GetType, typename RetVectorType = std::vector <RetType>, typename Fn> size_t enumerate (_In_ Fn func, _Out_ RetVectorType &output, _In_ std::function <RetType (GetType &)> process = process_default <GetType, RetType>) const
	{
		output.clear ();
		if (!icom) return 0;
		CComPtr <EnumInterfaceType> ienum;
		if (FAILED (icom->*func (&ienum)) || !ienum) return 0;
		return EnumerateComInterface <EnumInterfaceType, GetType, RetType, RetVectorType> (ienum, output, process);
	}
	public:
	using Interface = ComInterface;
	explicit com_info_quote (IComInterface &ptr): icom (ptr) {}
	com_info_quote (const com_info_quote &) = delete;
	com_info_quote &operator = (const com_info_quote &) = delete;
	com_info_quote (com_info_quote &&) noexcept = default;
	com_info_quote &operator = (com_info_quote &&) noexcept = default;
	bool valid () const { return !!icom; }
	ComInterface *pointer () const noexcept { return icom; }
};
namespace appx_info
{
	class appx_id: virtual public com_info <IAppxManifestPackageId>
	{
		using Base = com_info <IAppxManifestPackageId>;
		public:
		using Base::Base;
		std::wstring name () const { return get (&Interface::GetName); }
		std::wstring publisher () const { return get (&Interface::GetPublisher); }
		std::wstring package_family_name () const { return get (&Interface::GetPackageFamilyName); }
		std::wstring package_full_name () const { return get (&Interface::GetPackageFullName); }
		std::wstring resource_id () const { return get (&Interface::GetResourceId); }
		version version () const { return get <UINT64, Version> (&Interface::GetVersion); }
		APPX_PACKAGE_ARCHITECTURE architecture () const { return get <APPX_PACKAGE_ARCHITECTURE> (&Interface::GetArchitecture); }
		bool publisher_compare (const std::wstring &another) const
		{
			BOOL ret = FALSE;
			return (SUCCEEDED (pointer ()->ComparePublisher (another.c_str (), &ret)) ? ret != FALSE : false);
		}
	};
	class appx_qres: virtual public com_info <IAppxManifestQualifiedResourcesEnumerator>
	{
		using Base = com_info <IAppxManifestQualifiedResourcesEnumerator>;
		public:
		using Base::Base;
		size_t enumerate (_In_ std::function <void (IAppxManifestQualifiedResource *)> callback) const
		{
			size_t cnt = 0;
			BOOL hasCurrent = FALSE;
			HRESULT hr = pointer ()->GetHasCurrent (&hasCurrent);
			while (SUCCEEDED (hr) && hasCurrent)
			{
				CComPtr <IAppxManifestQualifiedResource> qres;
				hr = pointer ()->GetCurrent (&qres);
				if (SUCCEEDED (hr))
				{
					cnt ++;
					callback (qres);
				}
				hr = pointer ()->MoveNext (&hasCurrent);
			}
			return cnt;
		}
		size_t qualified_resources (
			_Out_ std::vector <std::wstring> *languages = nullptr,
			_Out_ std::vector <UINT32> *scales = nullptr,
			_Out_ std::vector <DX_FEATURE_LEVEL> *dxlevels = nullptr
		) const
		{
			if (languages) languages->clear ();
			if (scales) scales->clear ();
			if (dxlevels) dxlevels->clear ();
			return enumerate ([&languages, &scales, &dxlevels] (IAppxManifestQualifiedResource *qr) {
				LPWSTR lpstr = nullptr;
				raii endt ([&lpstr] () {
					if (lpstr) CoTaskMemFree (lpstr);
					lpstr = nullptr;
				});
				UINT32 u32 = 0;
				DX_FEATURE_LEVEL dx = DX_FEATURE_LEVEL::DX_FEATURE_LEVEL_UNSPECIFIED;
				if (SUCCEEDED (qr->GetLanguage (&lpstr)) && lpstr && languages) languages->push_back (lpstr);
				else if (SUCCEEDED (qr->GetScale (&u32)) && u32 && scales) scales->push_back (u32);
				else if (SUCCEEDED (qr->GetDXFeatureLevel (&dx)) && dx != DX_FEATURE_LEVEL::DX_FEATURE_LEVEL_UNSPECIFIED && dxlevels) dxlevels->push_back (dx);
			});
		}
		size_t languages (_Out_ std::vector <std::wstring> &languages) const { return qualified_resources (&languages); }
		size_t languages (_Out_ std::vector <std::wnstring> &output) const
		{
			output.clear ();
			BOOL hasCurrent = false;
			HRESULT hr = pointer ()->GetHasCurrent (&hasCurrent);
			while (SUCCEEDED (hr) && hasCurrent)
			{
				CComPtr <IAppxManifestQualifiedResource> qr;
				hr = pointer ()->GetCurrent (&qr);
				if (SUCCEEDED (hr))
				{
					LPWSTR lpstr = nullptr;
					raii endt ([&lpstr] () {
						if (lpstr) CoTaskMemFree (lpstr);
						lpstr = nullptr;
					});
					hr = qr->GetLanguage (&lpstr);
					if (SUCCEEDED (hr) && lpstr) push_unique (output, std::wnstring (lpstr));
				}
				hr = pointer ()->MoveNext (&hasCurrent);
			}
			return output.size ();
		}
		size_t scales (_Out_ std::vector <UINT32> &scales) const { return qualified_resources (nullptr, &scales); }
		size_t dx_feature_level (_Out_ std::vector <DX_FEATURE_LEVEL> &dxlevels) const { return qualified_resources (nullptr, nullptr, &dxlevels); }
	};
	class appx_res: virtual public com_info <IAppxManifestReader>
	{
		using Base = com_info <IAppxManifestReader>;
		public:
		using Base::Base;
		appx_res (IAppxManifestReader *ptr = nullptr): com_info (nullptr) { set (ptr); }
		size_t languages (_Out_ std::vector <std::wstring> &output) const
		{
			output.clear ();
			return enumerate <IAppxManifestResourcesEnumerator> (&Interface::GetResources, output);
		}
		size_t languages (_Out_ std::vector <std::wnstring> &output) const
		{
			output.clear ();
			BOOL hasCurrent = false;
			CComPtr <IAppxManifestResourcesEnumerator> re;
			HRESULT hr = pointer ()->GetResources (&re);
			if (FAILED (hr)) return false;
			hr = re->GetHasCurrent (&hasCurrent);
			while (SUCCEEDED (hr) && hasCurrent)
			{
				LPWSTR lpstr = nullptr;
				raii endt ([&lpstr] () {
					if (lpstr) CoTaskMemFree (lpstr);
					lpstr = nullptr;
				});
				hr = re->GetCurrent (&lpstr);
				if (SUCCEEDED (hr) && lpstr)
				{
					push_unique (output, std::wnstring (lpstr));
				}
				hr = re->MoveNext (&hasCurrent);
			}
			return output.size ();
		}
		size_t scales (_Out_ std::vector <UINT32> &scales) const
		{
			return qualified_resources <UINT32> (&IAppxManifestQualifiedResource::GetScale, scales, [] (UINT32 &v) -> bool {
				return v != 0;
			});
		}
		size_t dx_feature_level (_Out_ std::vector <DX_FEATURE_LEVEL> &dxlevels) const
		{
			return qualified_resources <DX_FEATURE_LEVEL> (&IAppxManifestQualifiedResource::GetDXFeatureLevel, dxlevels, [] (DX_FEATURE_LEVEL &v) -> bool {
				return v != DX_FEATURE_LEVEL::DX_FEATURE_LEVEL_UNSPECIFIED;
			});
		}
		appx_qres to_qualified_resources () const
		{
			CComPtr <IAppxManifestReader2> m2;
			IAppxManifestQualifiedResourcesEnumerator *ip = nullptr;
			if (SUCCEEDED (GetAppxManifestReader2 (pointer (), &m2))) m2->GetQualifiedResources (&ip);
			return appx_qres (ip);
		}
		private:
		template <typename GetValue, typename RetValue = GetValue, typename Fn, typename Vector = std::vector <RetValue>>
		size_t qualified_resources (_In_ Fn func, _Out_ Vector &output, _In_ std::function <bool (GetValue &)> valid = is_data_valid_default <GetValue>, _In_ std::function <RetValue (GetValue &)> process = process_default <GetValue, RetValue>) const
		{
			output.clear ();
			CComPtr <IAppxManifestReader2> m2;
			if (FAILED (GetAppxManifestReader2 (pointer (), &m2))) return 0;
			CComPtr <IAppxManifestQualifiedResourcesEnumerator> e;
			if (FAILED (m2->GetQualifiedResources (&e))) return 0;
			BOOL hasCurrent = false;
			HRESULT hr = e->GetHasCurrent (&hasCurrent);
			while (SUCCEEDED (hr) && hasCurrent)
			{
				CComPtr <IAppxManifestQualifiedResource> res;
				if (SUCCEEDED (e->GetCurrent (&res)))
				{
					GetValue value;
					hr = (res->*func) (&value);
					if (SUCCEEDED (hr) && valid (value))
					{
						output.push_back (process (value));
					}
				}
				hr = e->MoveNext (&hasCurrent);
			}
			return output.size ();
		}
		size_t qualified_resources (_Out_ std::vector <std::wstring> &languages, _Out_ std::vector <UINT32> &scales, _Out_ std::vector <DX_FEATURE_LEVEL> &dxlevels)
		{
			languages.clear ();
			scales.clear ();
			dxlevels.clear ();
			size_t cnt = 0;
			CComPtr <IAppxManifestReader2> m2;
			if (FAILED (GetAppxManifestReader2 (pointer (), &m2))) return 0;
			CComPtr <IAppxManifestQualifiedResourcesEnumerator> e;
			if (FAILED (m2->GetQualifiedResources (&e))) return 0;
			BOOL hasCurrent = false;
			HRESULT hr = e->GetHasCurrent (&hasCurrent);
			while (SUCCEEDED (hr) && hasCurrent)
			{
				CComPtr <IAppxManifestQualifiedResource> res;
				if (SUCCEEDED (e->GetCurrent (&res)))
				{
					LPWSTR lpstr = nullptr;
					raii endt ([&lpstr] () {
						if (lpstr) CoTaskMemFree (lpstr);
						lpstr = nullptr;
					});
					UINT32 scale = 0;
					DX_FEATURE_LEVEL dx = DX_FEATURE_LEVEL::DX_FEATURE_LEVEL_UNSPECIFIED;
					if (SUCCEEDED (res->GetLanguage (&lpstr)) && lpstr)
					{
						push_unique <std::wstring> (languages, std::wstring (lpstr), [] (const std::wstring &v1, const std::wstring &v2) -> bool {
							return std::wnstring (v1) == std::wnstring (v2);
						});
					}
					if (SUCCEEDED (res->GetScale (&scale)) && scale)
					{
						push_unique (scales, scale);
					}
					if (SUCCEEDED (res->GetDXFeatureLevel (&dx)) && dx != DX_FEATURE_LEVEL::DX_FEATURE_LEVEL_UNSPECIFIED)
					{
						push_unique (dxlevels, dx);
					}
					cnt ++;
				}
				hr = e->MoveNext (&hasCurrent);
			}
			return cnt;
		}
	};
	class appx_prop: virtual public com_info <IAppxManifestProperties>
	{
		using Base = com_info <IAppxManifestProperties>;
		public:
		using Base::Base;
		std::wstring string_value (const std::wstring &name) const
		{
			LPWSTR lpstr = nullptr;
			raii endt ([&lpstr] () {
				if (lpstr) CoTaskMemFree (lpstr);
				lpstr = nullptr;
			});
			HRESULT hr = pointer ()->GetStringValue (name.c_str (), &lpstr);
			if (SUCCEEDED (hr) && lpstr) return std::wstring () + lpstr;
			return L"";
		}
		bool bool_value (const std::wstring &name) const
		{
			BOOL boolvalue = FALSE;
			HRESULT hr = pointer ()->GetBoolValue (name.c_str (), &boolvalue);
			if (SUCCEEDED (hr)) return boolvalue != FALSE;
			return FALSE;
		}
		std::wstring display_name () const { return string_value (L"DisplayName"); }
		std::wstring description () const { return string_value (L"Description"); }
		std::wstring publisher () const { return string_value (L"PublisherDisplayName"); }
		std::wstring logo () const { return string_value (L"Logo"); }
		// 判断这个包是否为依赖项（框架包/运行库）
		bool framework () const { return bool_value (L"Framework"); }
		// 判断这个包是否为资源包
		bool resource_package () const { return bool_value (L"ResourcePackage"); }
	};
	class appx_preq: virtual public com_info <IAppxManifestReader>
	{
		using Base = com_info <IAppxManifestReader>;
		public:
		using Base::Base;
		appx_preq (IAppxManifestReader *ptr = nullptr): com_info (nullptr) { set (ptr); }
		version os_min_version () const { return get_version (L"OSMinVersion"); }
		version os_max_version_tested () const { return get_version (L"OSMaxVersionTested"); }
		version get_version (const std::wstring &name) const
		{
			UINT64 u64 = 0;
			if (SUCCEEDED (pointer ()->GetPrerequisite (name.c_str (), &u64))) return version (u64);
			return version ();
		}
	};
	class appx_apps: virtual public com_info <IAppxManifestApplicationsEnumerator>
	{
		using Base = com_info <IAppxManifestApplicationsEnumerator>;
		CComPtr <IAppxManifestReader> manifest = nullptr;
		protected:
		mutable std::map<std::wstring, std::map<std::wstring, std::wstring>> xml_cache;
		mutable bool xml_parsed = false;
		void parse_xml () const
		{
			if (xml_parsed || !manifest) return;

			CComPtr<IStream> xmlstream;
			if (FAILED (manifest->GetStream (&xmlstream)) || !xmlstream)
				return;

			LARGE_INTEGER zero {};
			xmlstream->Seek (zero, STREAM_SEEK_SET, nullptr);

			CComPtr<IXmlReader> reader;
			if (FAILED (CreateXmlReader (__uuidof(IXmlReader), (void**)&reader, nullptr)))
				return;

			reader->SetInput (xmlstream);

			XmlNodeType nodeType;
			bool inApplication = false;
			std::wstring currentId;
			std::map<std::wstring, std::wstring> currentValues;

			while (S_OK == reader->Read (&nodeType))
			{
				if (nodeType == XmlNodeType_Element)
				{
					LPCWSTR name = nullptr;
					reader->GetLocalName (&name, nullptr);

					if (!_wcsicmp (name, L"Application"))
					{
						inApplication = true;
						currentValues.clear ();
						currentId.clear ();

						auto read_attr = [&] (const wchar_t* xmlName, const wchar_t* key)
						{
							if (SUCCEEDED (reader->MoveToAttributeByName (xmlName, nullptr)))
							{
								LPCWSTR v = nullptr;
								reader->GetValue (&v, nullptr);
								if (v) currentValues [key] = v;
								reader->MoveToElement ();
							}
						};

						read_attr (L"Id", L"ID");
						read_attr (L"Executable", L"Executable");
						read_attr (L"EntryPoint", L"EntryPoint");
						read_attr (L"StartPage", L"StartPage");

						currentId = currentValues [L"ID"];
					}
					else if (inApplication)
					{
						auto read_attr = [&] (const wchar_t* xmlName, const wchar_t* key)
						{
							if (SUCCEEDED (reader->MoveToAttributeByName (xmlName, nullptr)))
							{
								LPCWSTR v = nullptr;
								reader->GetValue (&v, nullptr);
								if (v) currentValues [key] = v;
								reader->MoveToElement ();
							}
						};

						if (!_wcsicmp (name, L"VisualElements"))
						{
							read_attr (L"DisplayName", L"DisplayName");
							read_attr (L"Description", L"Description");
							read_attr (L"BackgroundColor", L"BackgroundColor");
							read_attr (L"ForegroundText", L"ForegroundText");
							read_attr (L"Logo", L"Logo");
							read_attr (L"SmallLogo", L"SmallLogo");
							read_attr (L"Square44x44Logo", L"Square44x44Logo");
							read_attr (L"Square150x150Logo", L"Square150x150Logo");
						}
						else if (!_wcsicmp (name, L"DefaultTile"))
						{
							read_attr (L"ShortName", L"ShortName");
							read_attr (L"WideLogo", L"WideLogo");
							read_attr (L"Wide310x150Logo", L"Wide310x150Logo");
							read_attr (L"Square310x310Logo", L"Square310x310Logo");
							read_attr (L"Square71x71Logo", L"Square71x71Logo");
						}
						else if (!_wcsicmp (name, L"LockScreen"))
						{
							read_attr (L"BadgeLogo", L"LockScreenLogo");
							read_attr (L"Notification", L"LockScreenNotification");
						}
					}
				}
				else if (nodeType == XmlNodeType_EndElement)
				{
					LPCWSTR name = nullptr;
					reader->GetLocalName (&name, nullptr);
					if (!_wcsicmp (name, L"Application"))
					{
						// fallback常量
						if (currentValues [L"Wide310x150Logo"].empty ())
							currentValues [L"Wide310x150Logo"] = currentValues [L"WideLogo"];
						if (currentValues [L"Square70x70Logo"].empty ())
							currentValues [L"Square70x70Logo"] = currentValues [L"Square71x71Logo"];
						if (!currentId.empty ()) xml_cache [currentId] = currentValues;
						inApplication = false;
						currentValues.clear ();
						currentId.clear ();
					}
				}
			}
			xml_parsed = true;
		}
		std::wstring get_value_ext (const std::wstring& appId, const std::wstring& item) const
		{
			parse_xml (); // 只会解析一次
			auto it = xml_cache.find (appId);
			if (it == xml_cache.end ()) return L"";

			auto jt = it->second.find (item);
			return (jt != it->second.end ()) ? jt->second : L"";
		}
		public:
		appx_apps (IAppxManifestApplicationsEnumerator *ptr, IAppxManifestReader *mptr):
			Base (ptr), manifest (mptr) {}
		size_t applications (_Out_ std::vector <app_info> &output) const
		{
			auto ret = EnumerateComInterface <IAppxManifestApplicationsEnumerator, IAppxManifestApplication *, app_info> (pointer (), output, [] (IAppxManifestApplication *&p) -> app_info {
				raii rel ([&p] () {
					if (p) p->Release ();
					p = nullptr;
				});
				app_info app;
				LPWSTR userid;
				{
					raii endt ([&userid] () {
						if (userid) CoTaskMemFree (userid);
						userid = nullptr;
					});
					if (SUCCEEDED (p->GetAppUserModelId (&userid)))
					{
						app [L"AppUserModelID"] = userid;
					}
				}
				for (auto &it : GetApplicationAttributeItems ())
				{
					LPWSTR lpstr;
					raii endt1 ([&lpstr] () {
						if (lpstr) CoTaskMemFree (lpstr);
						lpstr = nullptr;
					});
					if (std::wnstring (it) == std::wnstring (L"AppUserModelID")) continue;
					if (SUCCEEDED (p->GetStringValue (it.c_str (), &lpstr)) && lpstr)
						app [it] = lpstr;
					else app [it] = std::wstring ();
				}
				return app;
			});
			for (auto &it : output)
			{
				auto id = it [L"Id"];
				for (auto &kv : it)
				{
					if (IsNormalizeStringEmpty (kv.second))
						kv.second = get_value_ext (id, kv.first);
				}
			}
			return ret;
		}
		size_t app_user_model_ids (_Out_ std::vector <std::wstring> &output) const
		{
			return EnumerateComInterface <IAppxManifestApplicationsEnumerator, IAppxManifestApplication *, std::wstring> (pointer (), output, [] (IAppxManifestApplication *&p) -> std::wstring {
				raii rel ([&p] () {
					if (p) p->Release ();
					p = nullptr;
				});
				LPWSTR userid;
				raii endt ([&userid] () {
					if (userid) CoTaskMemFree (userid);
					userid = nullptr;
				});
				if (SUCCEEDED (p->GetAppUserModelId (&userid))) return userid;
				else return L"";
			});
		}
		size_t string_values (_In_ const std::wstring &name, _Out_ std::vector <std::wstring> &output) const
		{
			return EnumerateComInterface <IAppxManifestApplicationsEnumerator, IAppxManifestApplication *, std::wstring> (pointer (), output, [&name] (IAppxManifestApplication *&p) -> std::wstring {
				raii rel ([&p] () {
					if (p) p->Release ();
					p = nullptr;
				});
				LPWSTR str = nullptr;
				raii endt ([&str] () {
					if (str) CoTaskMemFree (str);
					str = nullptr;
				});
				if (SUCCEEDED (p->GetStringValue (name.c_str (), &str))) return std::wstring (str);
				else return L"";
			});
		}
		size_t id_s (_Out_ std::vector <std::wstring> &output) const { return string_values (L"Id", output); }
		size_t visualelements_displaynames (_Out_ std::vector <std::wstring> &output) const { return string_values (L"DisplayName", output); }
		size_t visualelements_backgroundcolors (_Out_ std::vector <std::wstring> &output) const { return string_values (L"BackgroundColor", output); }
		size_t visualelements_foregroundcolors (_Out_ std::vector <std::wstring> &output) const { return string_values (L"ForegroundText", output); }
		size_t visualelements_shortnames (_Out_ std::vector <std::wstring> &output) const { return string_values (L"ShortName", output); }
		size_t visualelements_44x44logos (_Out_ std::vector <std::wstring> &output) const { return string_values (L"Square44x44Logo", output); }
		size_t size () const
		{
			size_t cnt = 0;
			BOOL hasCurrent = FALSE;
			HRESULT hr = pointer ()->GetHasCurrent (&hasCurrent);
			while (SUCCEEDED (hr) && hasCurrent)
			{
				hr = pointer ()->GetHasCurrent (&hasCurrent);
				if (SUCCEEDED (hr) && hasCurrent) cnt ++;
				hr = pointer ()->MoveNext (&hasCurrent);
			}
			return cnt;
		}
	};
	class appx_capabs: virtual public com_info <IAppxManifestDeviceCapabilitiesEnumerator>
	{
		using Base = com_info <IAppxManifestDeviceCapabilitiesEnumerator>;
		APPX_CAPABILITIES cflags;
		std::vector <std::wnstring> cnames;
		public:
		using Base::Base;
		appx_capabs (IAppxManifestDeviceCapabilitiesEnumerator *devicec, APPX_CAPABILITIES capa, IAppxManifestReader *&r): cflags (capa), com_info (devicec) 
		{
			if (!r) return;
			CComPtr <IStream> xmlstream;
			if (FAILED (r->GetStream (&xmlstream))) return;
			CComPtr <IXmlReader> xmlreader;
			if (FAILED (CreateXmlReader (__uuidof (IXmlReader), (void **)&xmlreader, nullptr))) return;
			xmlreader->SetInput (xmlstream.p);
			XmlNodeType nodeType;
			bool inPackage = false;
			bool inCapabilities = false;
			HRESULT hr = S_OK;
			auto &reader = xmlreader;
			while (SUCCEEDED (hr = xmlreader->Read (&nodeType)) && hr == S_OK)
			{
				if (nodeType == XmlNodeType_Element)
				{
					LPCWSTR localName = nullptr;
					reader->GetLocalName (&localName, nullptr);
					// <Package>
					if (!inPackage && _wcsicmp (localName, L"Package") == 0) inPackage = true;
					// <Capabilities>
					else if (inPackage && !inCapabilities && _wcsicmp (localName, L"Capabilities") == 0) inCapabilities = true;
					// <Capability>
					else if (inCapabilities && _wcsicmp (localName, L"Capability") == 0)
					{
						if (SUCCEEDED (reader->MoveToAttributeByName (L"Name", nullptr)))
						{
							LPCWSTR value = nullptr;
							reader->GetValue (&value, nullptr);
							if (value && *value) cnames.push_back (value);
						}
						reader->MoveToElement ();
					}
				}
				else if (nodeType == XmlNodeType_EndElement)
				{
					LPCWSTR localName = nullptr;
					reader->GetLocalName (&localName, nullptr);
					if (inCapabilities && _wcsicmp (localName, L"Capabilities") == 0) inCapabilities = false;
					else if (inPackage && _wcsicmp (localName, L"Package") == 0) break; 
				}
			}
		}
		appx_capabs () = default;
		APPX_CAPABILITIES capabilities () const { return cflags; }
		size_t device_capabilities (_Out_ std::vector <std::wstring> &output) const
		{
			return EnumerateComInterface <IAppxManifestDeviceCapabilitiesEnumerator, LPWSTR, std::wstring> (pointer (), output, [] (LPWSTR &lp) {
				raii ([&lp] () {
					if (lp) CoTaskMemFree (lp);
					lp = nullptr;
				});
				return lp ? lp : L"";
			});
		}
		// 从功能按位数据转换到功能名。
		size_t capabilities_names (_Out_ std::vector <std::wstring> &output) const
		{
			output.clear ();
			if (cnames.empty ()) CapabilitiesFlagsToNames (cflags, output);
			else for (auto &it : cnames) output.push_back (it);
			return output.size ();
		}
		// 获取功能和设备功能的所有功能名
		size_t all_capabilities (_Out_ std::vector <std::wstring> &output) const
		{
			output.clear ();
			std::vector <std::wstring> devs;
			device_capabilities (devs);
			capabilities_names (output);
			for (auto &it : devs) push_unique (output, it);
			return output.size ();
		}
	};
	class appx_deps: virtual public com_info <IAppxManifestPackageDependenciesEnumerator>
	{
		using Base = com_info <IAppxManifestPackageDependenciesEnumerator>;
		public:
		using Base::Base;
		size_t dependencies (_Out_ std::vector <dep_info> &output) const
		{
			return EnumerateComInterface <IAppxManifestPackageDependenciesEnumerator, IAppxManifestPackageDependency *, dep_info> (pointer (), output, [] (IAppxManifestPackageDependency *&ptr) -> dep_info {
				raii endt ([&ptr] () {
					if (ptr) ptr->Release ();
					ptr = nullptr;
				});
				dep_info dep (ptr);
				return dep;
			});
		}
		size_t size () const
		{
			size_t cnt = 0;
			BOOL hasCurrent = FALSE;
			HRESULT hr = pointer ()->GetHasCurrent (&hasCurrent);
			while (SUCCEEDED (hr) && hasCurrent)
			{
				hr = pointer ()->GetHasCurrent (&hasCurrent);
				if (SUCCEEDED (hr) && hasCurrent) cnt ++;
				hr = pointer ()->MoveNext (&hasCurrent);
			}
			return cnt;
		}
	};
	class appx_iditem: virtual public com_info <IAppxBundleManifestPackageInfo>
	{
		using Base = com_info <IAppxBundleManifestPackageInfo>;
		public:
		using Base::Base;
		appx_iditem (IAppxBundleManifestPackageInfo *ptr): com_info (nullptr) { set (ptr); }
		std::wstring file_name () const { return get (&Interface::GetFileName); }
		UINT64 offset () const { return get <UINT64> (&Interface::GetOffset); }
		HRESULT get_identity (_Outptr_ IAppxManifestPackageId **output) const { return pointer ()->GetPackageId (output); }
		appx_id identity () const
		{
			IAppxManifestPackageId *ip = nullptr;
			HRESULT hr = get_identity (&ip);
			return appx_id (ip);
		}
		APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE type () const { return get <APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE> (&Interface::GetPackageType); }
		HRESULT get_qualified_resources (_Outptr_ IAppxManifestQualifiedResourcesEnumerator **output) const { return pointer ()->GetResources (output); }
		appx_qres qualified_resources () const
		{
			IAppxManifestQualifiedResourcesEnumerator *ip = nullptr;
			get_qualified_resources (&ip);
			return appx_qres (ip);
		}
		UINT64 size () const { return get <UINT64> (&Interface::GetSize); }
	};
	enum class ResourceType
	{
		unknown = 0b00,
		language = 0b01,
		scale = 0b10,
		both = 0b11
	};
	struct resource
	{
		ResourceType restype = ResourceType::unknown;
		APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE pkgtype = APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE_APPLICATION;
		struct resource_content
		{
			std::vector <std::wnstring> languages;
			std::vector <uint32_t> scales;
			resource_content (const std::vector <std::wnstring> &langs): languages (langs) {}
			resource_content (const std::vector <uint32_t> &ss): scales (ss) {}
			resource_content (const std::vector <std::wnstring> &langs, const std::vector <uint32_t> &ss): languages (langs), scales (ss) {}
			resource_content () = default;
		} resvalue;
		resource (const std::vector <std::wnstring> &languages):
			restype (ResourceType::language), resvalue (languages) {}
		resource (const std::vector <uint32_t> &ss):
			restype (ResourceType::scale), resvalue (ss) {}
		resource (const std::vector <std::wnstring> &langs, const std::vector <uint32_t> &ss):
			restype (ResourceType::both), resvalue (langs, ss) {}
		resource (): restype (ResourceType::unknown), resvalue () {}
	};
	class appx_iditems: virtual public com_info <IAppxBundleManifestPackageInfoEnumerator>
	{
		using Base = com_info <IAppxBundleManifestPackageInfoEnumerator>;
		public:
		using Base::Base;
		size_t enumerate (_In_ std::function <void (IAppxBundleManifestPackageInfo *)> callback, APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE enumtype = (APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE)-1) const
		{
			BOOL hasCurrent = FALSE;
			HRESULT hr = pointer ()->GetHasCurrent (&hasCurrent);
			size_t cnt = 0;
			while (SUCCEEDED (hr) && hasCurrent)
			{
				CComPtr <IAppxBundleManifestPackageInfo> item;
				hr = pointer ()->GetCurrent (&item);
				if (SUCCEEDED (hr))
				{
					APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE type = (APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE)-1;
					item->GetPackageType (&type);
					switch (type)
					{
						case APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE::APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE_APPLICATION:
							cnt ++;
							callback (item);
							break;
						case APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE::APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE_RESOURCE:
							cnt ++;
							callback (item);
							break;
						default:
							cnt ++;
							callback (item);
							break;
					}
				}
				hr = pointer ()->MoveNext (&hasCurrent);
			}
			return cnt;
		}
		size_t enumerate (_Out_ std::vector <appx_iditem> &output, _In_ APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE enumtype = (APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE)-1) const
		{
			output.clear ();
			enumerate ([&output] (IAppxBundleManifestPackageInfo *ip) {
				output.push_back (appx_iditem (ip));
			}, enumtype);
			return output.size ();
		}
		size_t enumerate (_Out_ std::vector <appx_iditem> &apps, _Out_ std::vector <appx_iditem> &ress) const
		{
			return enumerate ([&apps, &ress] (IAppxBundleManifestPackageInfo *p) {
				APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE type;
				p->GetPackageType (&type);
				switch (type)
				{
					case APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE::APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE_APPLICATION:
						apps.push_back (appx_iditem (p)); break;
					case APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE::APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE_RESOURCE:
						ress.push_back (appx_iditem (p)); break;
				}
			});
		}
		size_t application_packages (_Out_ std::vector <appx_iditem> &output) const { return enumerate (output, APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE::APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE_APPLICATION); }
		size_t resource_packages (_Out_ std::vector <appx_iditem> &output) const { return enumerate (output, APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE::APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE_RESOURCE); }
		size_t size () const
		{
			BOOL hasCurrent = FALSE;
			HRESULT hr = pointer ()->GetHasCurrent (&hasCurrent);
			size_t cnt = 0;
			while (SUCCEEDED (hr) && hasCurrent)
			{
				cnt ++;
				hr = pointer ()->MoveNext (&hasCurrent);
			}
			return cnt;
		}
		size_t resource_info (std::map <std::wnstring, resource> &output) const
		{
			output.clear ();
			std::vector <appx_iditem> items;
			enumerate (items);
			for (auto &it : items)
			{
				std::wnstring fpath = it.file_name ();
				auto qres = it.qualified_resources ();
				std::vector <std::wstring> langs;
				std::vector <uint32_t> scales;
				qres.qualified_resources (&langs, &scales);
				std::vector <std::wnstring> langs_n;
				for (auto &it_l : langs) langs_n.push_back (it_l);
				BYTE status = (bool)langs_n.size () << 1 | (bool)scales.size ();
				switch (status)
				{
					case 0b01: output [fpath] = resource (scales); break;
					case 0b10: output [fpath] = resource (langs_n); break;
					case 0b11: output [fpath] = resource (langs_n, scales); break;
					default:
					case 0b00: output [fpath] = resource (); break;
				}
				output [fpath].pkgtype = it.type ();
			}
			return output.size ();
		}
	};
}
class appxreader: virtual public com_info_quote <IAppxPackageReader>
{
	using Base = com_info_quote <IAppxPackageReader>;
	template <typename IComPtr, typename ReturnType, typename Fn> HRESULT get (IComPtr iptr, Fn func, ReturnType *retvalue) const { if (!iptr) return E_FAIL; return (iptr->*func) (retvalue); }
	using Package = IAppxPackageReader;
	using Manifest = IAppxManifestReader;
	public:
	using Base::Base;
	HRESULT manifest (_Outptr_ IAppxManifestReader **output) const { return get (pointer (), &Package::GetManifest, output); }
	template <class IComPtr, class Func> HRESULT get_from_manifest (Func fn, _Outptr_ IComPtr *output) const
	{
		CComPtr <IAppxManifestReader> m;
		HRESULT hr = manifest (&m);
		if (FAILED (hr)) return hr;
		return hr = get (m, fn, output);
	}
	HRESULT get_identity (_Outptr_ IAppxManifestPackageId **output) const { return get_from_manifest <IAppxManifestPackageId *> (&Manifest::GetPackageId, output); }
	appx_info::appx_id identity () const
	{
		IAppxManifestPackageId *ip = nullptr;
		get_identity (&ip);
		return appx_info::appx_id (ip);
	}
	appx_info::appx_res resources () const
	{
		CComPtr <IAppxManifestReader> m;
		HRESULT hr = manifest (&m);
		return appx_info::appx_res (m);
	}
	HRESULT get_properties (_Outptr_ IAppxManifestProperties **output) const { return get_from_manifest <IAppxManifestProperties *> (&Manifest::GetProperties, output); }
	appx_info::appx_prop properties () const
	{
		IAppxManifestProperties *ip = nullptr;
		HRESULT hr = get_properties (&ip);
		return appx_info::appx_prop (ip);
	}
	appx_info::appx_preq prerequisites () const
	{
		CComPtr <IAppxManifestReader> m;
		HRESULT hr = manifest (&m);
		return appx_info::appx_preq (m);
	}
	HRESULT get_applications (_Outptr_ IAppxManifestApplicationsEnumerator **output) const { return get_from_manifest <IAppxManifestApplicationsEnumerator *> (&Manifest::GetApplications, output); }
	appx_info::appx_apps applications () const
	{
		IAppxManifestReader *m = nullptr;
		IAppxManifestApplicationsEnumerator *ip = nullptr;
		manifest (&m);
		get_applications (&ip);
		return appx_info::appx_apps (ip, m);
	}
	HRESULT get_capabilities (_Outptr_ APPX_CAPABILITIES *output) const { return get_from_manifest <APPX_CAPABILITIES> (&Manifest::GetCapabilities, output); }
	HRESULT get_device_capabilities (_Outptr_ IAppxManifestDeviceCapabilitiesEnumerator **output) const { return get_from_manifest <IAppxManifestDeviceCapabilitiesEnumerator *> (&Manifest::GetDeviceCapabilities, output); }
	appx_info::appx_capabs capabilities () const
	{
		APPX_CAPABILITIES caps;
		IAppxManifestDeviceCapabilitiesEnumerator *ip = nullptr;
		CComPtr <IAppxManifestReader> im;
		if (SUCCEEDED (manifest (&im))) im->GetDeviceCapabilities (&ip);
		if (SUCCEEDED (get_capabilities (&caps))) return appx_info::appx_capabs (ip, caps, im.p);
		return appx_info::appx_capabs (ip);
	}
	HRESULT get_dependencies (_Outptr_ IAppxManifestPackageDependenciesEnumerator **output) const { return get_from_manifest <IAppxManifestPackageDependenciesEnumerator *> (&Manifest::GetPackageDependencies, output); }
	appx_info::appx_deps dependencies () const
	{
		IAppxManifestPackageDependenciesEnumerator *ip = nullptr;
		get_dependencies (&ip);
		return appx_info::appx_deps (ip);
	}
	PackageRole package_role () const
	{
		auto prop = properties ();
		if (prop.framework ()) return PackageRole::framework;
		try { if (prop.resource_package ()) return PackageRole::resource; }
		catch (const std::exception &e) {}
		auto app = applications ();
		std::vector <std::wstring> apps;
		if (app.app_user_model_ids (apps)) return PackageRole::application;
		else return PackageRole::unknown;
	}
	HRESULT payload_stream (_In_ const std::wstring &filename, _Outptr_ IAppxFile **output) const { return pointer ()->GetPayloadFile (filename.c_str (), output); }
	HRESULT payloads_stream (_Outptr_ IAppxFilesEnumerator **output) const { return pointer ()->GetPayloadFiles (output); }
	HRESULT footprint_stream (_In_ APPX_FOOTPRINT_FILE_TYPE type, _Outptr_ IAppxFile **output) const { return pointer ()->GetFootprintFile (type, output); }
	HRESULT resources_pri_stream (_Outptr_ IAppxFile **output) const { return payload_stream (L"resources.pri", output); }
	HRESULT blockmap (_Outptr_ IAppxBlockMapReader **output) const { return pointer ()->GetBlockMap (output); }
};
class bundlereader: virtual public com_info_quote <IAppxBundleReader>
{
	using Base = com_info_quote <IAppxBundleReader>;
	public:
	using Base::Base;
	template <typename IComPtr, typename ReturnType, typename Fn> HRESULT get (IComPtr iptr, Fn func, ReturnType *retvalue) const { if (!iptr) return E_FAIL; return (iptr->*func) (retvalue); }
	using Package = IAppxBundleReader;
	using Manifest = IAppxBundleManifestReader;
	HRESULT manifest (_Outptr_ IAppxBundleManifestReader **output) const { return get (pointer (), &Package::GetManifest, output); }
	template <class IComPtr, class Func> HRESULT get_from_manifest (Func fn, _Outptr_ IComPtr *output) const
	{
		CComPtr <IAppxBundleManifestReader> m;
		HRESULT hr = manifest (&m);
		if (FAILED (hr)) return hr;
		return hr = get (m, fn, output);
	}
	HRESULT get_identity (_Outptr_ IAppxManifestPackageId **output) const { return get_from_manifest (&Manifest::GetPackageId, output); }
	appx_info::appx_id identity () const
	{
		IAppxManifestPackageId *ip = nullptr;
		get_identity (&ip);
		return appx_info::appx_id (ip);
	}
	HRESULT get_package_id_items (_Outptr_ IAppxBundleManifestPackageInfoEnumerator **output) const { return get_from_manifest (&Manifest::GetPackageInfoItems, output); }
	appx_info::appx_iditems package_id_items () const
	{
		IAppxBundleManifestPackageInfoEnumerator *ip = nullptr;
		get_package_id_items (&ip);
		return appx_info::appx_iditems (ip);
	}
	HRESULT get_payload_package (_In_ const std::wstring &filename, _Outptr_ IAppxFile **output) const { return pointer ()->GetPayloadPackage (filename.c_str (), output); }
	HRESULT get_payload_packages (_Outptr_ IAppxFilesEnumerator **output) const { return pointer ()->GetPayloadPackages (output); }
	HRESULT footprint_stream (_In_ APPX_BUNDLE_FOOTPRINT_FILE_TYPE type, _Outptr_ IAppxFile **output) const { return pointer ()->GetFootprintFile (type, output); }
	HRESULT blockmap (_Outptr_ IAppxBlockMapReader **output) const { return pointer ()->GetBlockMap (output); }
	HRESULT payload_package (_In_ const std::wstring &filename, std::function <void (IAppxPackageReader *)> callback) const
	{
		CComPtr <IAppxFile> appxfile;
		CComPtr <IStream> stream;
		CComPtr <IAppxPackageReader> ip;
		HRESULT hr = S_OK;
		if (FAILED (hr = get_payload_package (filename, &appxfile))) return hr;
		if (FAILED (hr = appxfile->GetStream (&stream))) return hr;
		hr = GetAppxPackageReader (stream, &ip);
		if (callback) callback (ip);
		return hr;
	}
	size_t payload_packages (std::function <void (IAppxPackageReader *, LPCWSTR)> callback) const
	{
		CComPtr <IAppxFilesEnumerator> iae;
		BOOL hasCurrent = false;
		HRESULT hr = get_payload_packages (&iae);
		hr = iae->GetHasCurrent (&hasCurrent);
		size_t cnt = 0;
		while (SUCCEEDED (hr) && hasCurrent)
		{
			CComPtr <IAppxFile> afile;
			if (SUCCEEDED (iae->GetCurrent (&afile)))
			{
				LPWSTR filename = nullptr;
				raii endt ([&filename] () {
					if (filename) CoTaskMemFree (filename);
					filename = nullptr;
				});
				afile->GetName (&filename);
				CComPtr <IStream> stream;
				if (SUCCEEDED (afile->GetStream (&stream)))
				{
					CComPtr <IAppxPackageReader> ip;
					if (SUCCEEDED (GetAppxPackageReader (stream, &ip)))
					{
						cnt ++;
						if (callback) callback (ip, filename);
					}
				}
			}
			hr = iae->MoveNext (&hasCurrent);
		}
		return cnt;
	}
	HRESULT random_application_package (_Outptr_ IAppxPackageReader **output) const
	{
		CComPtr <IAppxBundleManifestPackageInfoEnumerator> iditems;
		HRESULT hr = get_package_id_items (&iditems);
		if (FAILED (hr)) return hr;
		BOOL hc = FALSE;
		hr = iditems->GetHasCurrent (&hc);
		bool find = false;
		std::wstring fname = L"";
		while (SUCCEEDED (hr) && hc)
		{
			CComPtr <IAppxBundleManifestPackageInfo> iditem;
			hr = iditems->GetCurrent (&iditem);
			if (SUCCEEDED (hr))
			{
				APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE ptype;
				if (!(SUCCEEDED (iditem->GetPackageType (&ptype)) && ptype == APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE::APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE_APPLICATION)) 
				{
					hr = iditems->MoveNext (&hc);
					continue;
				}
				LPWSTR lpfname = nullptr;
				raii endt ([&lpfname] () {
					if (lpfname) CoTaskMemFree (lpfname);
					lpfname = nullptr;
				});
				if (SUCCEEDED (iditem->GetFileName (&lpfname)) && lpfname)
				{
					fname += lpfname;
					find = true;
					break;
				}
			}
			hr = iditems->MoveNext (&hc);
		}
		if (!find) return FAILED (hr) ? hr : E_FAIL;
		CComPtr <IAppxFile> afile;
		hr = get_payload_package (fname, &afile);
		if (FAILED (hr)) return hr;
		CComPtr <IStream> ist;
		hr = afile->GetStream (&ist);
		if (FAILED (hr)) return hr;
		return GetAppxPackageReader (ist, output);
	}
	HRESULT random_resource_package (_Outptr_ IAppxPackageReader **output) const
	{
		CComPtr <IAppxBundleManifestPackageInfoEnumerator> iditems;
		HRESULT hr = get_package_id_items (&iditems);
		if (FAILED (hr)) return hr;
		BOOL hc = FALSE;
		hr = iditems->GetHasCurrent (&hc);
		bool find = false;
		std::wstring fname = L"";
		while (SUCCEEDED (hr) && hc)
		{
			CComPtr <IAppxBundleManifestPackageInfo> iditem;
			hr = iditems->GetCurrent (&iditem);
			if (SUCCEEDED (hr))
			{
				APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE ptype;
				if (!(SUCCEEDED (iditem->GetPackageType (&ptype)) && ptype == APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE::APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE_RESOURCE)) 
				{
					hr = iditems->MoveNext (&hc);
					continue;
				}
				LPWSTR lpfname = nullptr;
				raii endt ([&lpfname] () {
					if (lpfname) CoTaskMemFree (lpfname);
					lpfname = nullptr;
				});
				if (SUCCEEDED (iditem->GetFileName (&lpfname)) && lpfname)
				{
					fname += lpfname;
					find = true;
					break;
				}
			}
			hr = iditems->MoveNext (&hc);
		}
		if (!find) return FAILED (hr) ? hr : E_FAIL;
		CComPtr <IAppxFile> afile;
		hr = get_payload_package (fname, &afile);
		if (FAILED (hr)) return hr;
		CComPtr <IStream> ist;
		hr = afile->GetStream (&ist);
		if (FAILED (hr)) return hr;
		return GetAppxPackageReader (ist, output);
	}
	HRESULT random_application_package (_Outptr_ IAppxFile **output) const
	{
		CComPtr <IAppxBundleManifestPackageInfoEnumerator> iditems;
		HRESULT hr = get_package_id_items (&iditems);
		if (FAILED (hr)) return hr;
		BOOL hc = FALSE;
		hr = iditems->GetHasCurrent (&hc);
		bool find = false;
		std::wstring fname = L"";
		while (SUCCEEDED (hr) && hc)
		{
			CComPtr <IAppxBundleManifestPackageInfo> iditem;
			hr = iditems->GetCurrent (&iditem);
			if (SUCCEEDED (hr))
			{
				APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE ptype;
				if (!(SUCCEEDED (iditem->GetPackageType (&ptype)) && ptype == APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE::APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE_APPLICATION))
				{
					hr = iditems->MoveNext (&hc);
					continue;
				}
				LPWSTR lpfname = nullptr;
				raii endt ([&lpfname] () {
					if (lpfname) CoTaskMemFree (lpfname);
					lpfname = nullptr;
				});
				if (SUCCEEDED (iditem->GetFileName (&lpfname)) && lpfname)
				{
					fname += lpfname;
					find = true;
					break;
				}
			}
			hr = iditems->MoveNext (&hc);
		}
		if (!find) return FAILED (hr) ? hr : E_FAIL;
		CComPtr <IAppxFile> afile;
		return get_payload_package (fname, output);
	}
	HRESULT random_resource_package (_Outptr_ IAppxFile **output) const
	{
		CComPtr <IAppxBundleManifestPackageInfoEnumerator> iditems;
		HRESULT hr = get_package_id_items (&iditems);
		if (FAILED (hr)) return hr;
		BOOL hc = FALSE;
		hr = iditems->GetHasCurrent (&hc);
		bool find = false;
		std::wstring fname = L"";
		while (SUCCEEDED (hr) && hc)
		{
			CComPtr <IAppxBundleManifestPackageInfo> iditem;
			hr = iditems->GetCurrent (&iditem);
			if (SUCCEEDED (hr))
			{
				APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE ptype;
				if (!(SUCCEEDED (iditem->GetPackageType (&ptype)) && ptype == APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE::APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE_RESOURCE))
				{
					hr = iditems->MoveNext (&hc);
					continue;
				}
				LPWSTR lpfname = nullptr;
				raii endt ([&lpfname] () {
					if (lpfname) CoTaskMemFree (lpfname);
					lpfname = nullptr;
				});
				if (SUCCEEDED (iditem->GetFileName (&lpfname)) && lpfname)
				{
					fname += lpfname;
					find = true;
					break;
				}
			}
			hr = iditems->MoveNext (&hc);
		}
		if (!find) return FAILED (hr) ? hr : E_FAIL;
		CComPtr <IAppxFile> afile;
		return hr = get_payload_package (fname, output);
	}
	HRESULT random_application_package (_Outptr_ IStream **output) const
	{
		CComPtr <IAppxFile> iaf;
		HRESULT hr = S_OK;
		if (FAILED (hr = random_application_package (&iaf))) return hr;
		return iaf->GetStream (output);
	}
	HRESULT random_resource_package (_Outptr_ IStream **output) const
	{
		CComPtr <IAppxFile> iaf;
		HRESULT hr = S_OK;
		if (FAILED (hr = random_resource_package (&iaf))) return hr;
		return iaf->GetStream (output);
	}
	size_t application_packages (std::function <void (IAppxPackageReader *)> callback) const
	{
		CComPtr <IAppxBundleManifestPackageInfoEnumerator> iditems;
		HRESULT hr = get_package_id_items (&iditems);
		if (FAILED (hr)) return hr;
		BOOL hc = FALSE;
		hr = iditems->GetHasCurrent (&hc);
		std::vector <std::wstring> files;
		while (SUCCEEDED (hr) && hc)
		{
			CComPtr <IAppxBundleManifestPackageInfo> iditem;
			hr = iditems->GetCurrent (&iditem);
			if (SUCCEEDED (hr))
			{
				APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE ptype;
				if (!(SUCCEEDED (iditem->GetPackageType (&ptype)) && ptype == APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE::APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE_APPLICATION))
				{ hr = iditems->MoveNext (&hc); continue; }
				LPWSTR lpfname = nullptr;
				raii endt ([&lpfname] () { if (lpfname) CoTaskMemFree (lpfname); lpfname = nullptr; });
				if (SUCCEEDED (iditem->GetFileName (&lpfname)) && lpfname) files.push_back (lpfname);
			}
			hr = iditems->MoveNext (&hc);
		}
		size_t cnt = 0;
		for (auto &fname : files)
		{
			CComPtr <IAppxFile> afile;
			hr = get_payload_package (fname, &afile);
			if (SUCCEEDED (hr))
			{
				CComPtr <IStream> istrm;
				hr = afile->GetStream (&istrm);
				if (SUCCEEDED (hr))
				{
					CComPtr <IAppxPackageReader> aread;
					hr = GetAppxPackageReader (istrm, &aread);
					if (SUCCEEDED (hr))
					{
						cnt ++;
						callback (aread);
					}
				}
			}
		}
		return cnt;
	}
	size_t resource_packages (std::function <void (IAppxPackageReader *)> callback) const
	{
		CComPtr <IAppxBundleManifestPackageInfoEnumerator> iditems;
		HRESULT hr = get_package_id_items (&iditems);
		if (FAILED (hr)) return hr;
		BOOL hc = FALSE;
		hr = iditems->GetHasCurrent (&hc);
		std::vector <std::wstring> files;
		while (SUCCEEDED (hr) && hc)
		{
			CComPtr <IAppxBundleManifestPackageInfo> iditem;
			hr = iditems->GetCurrent (&iditem);
			if (SUCCEEDED (hr))
			{
				APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE ptype;
				if (!(SUCCEEDED (iditem->GetPackageType (&ptype)) && ptype == APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE::APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE_RESOURCE))
				{ hr = iditems->MoveNext (&hc); continue; }
				LPWSTR lpfname = nullptr;
				raii endt ([&lpfname] () { if (lpfname) CoTaskMemFree (lpfname); lpfname = nullptr; });
				if (SUCCEEDED (iditem->GetFileName (&lpfname)) && lpfname) files.push_back (lpfname);
			}
			hr = iditems->MoveNext (&hc);
		}
		size_t cnt = 0;
		for (auto &fname : files)
		{
			CComPtr <IAppxFile> afile;
			hr = get_payload_package (fname, &afile);
			if (SUCCEEDED (hr))
			{
				CComPtr <IStream> istrm;
				hr = afile->GetStream (&istrm);
				if (SUCCEEDED (hr))
				{
					CComPtr <IAppxPackageReader> aread;
					hr = GetAppxPackageReader (istrm, &aread);
					if (SUCCEEDED (hr))
					{
						cnt ++;
						callback (aread);
					}
				}
			}
		}
		return cnt;
	}
};

class package
{
	IAppxPackageReader *appx = nullptr;
	IAppxBundleReader *bundle = nullptr;
	public:
	~package () { destroy (); }
	package (const std::wstring &filepath = L"") { create (filepath); }
	void destroy ()
	{
		if (appx) { appx->Release (); appx = nullptr; }
		if (bundle) { bundle->Release (); bundle = nullptr; }
	}
	bool create (const std::wstring &filepath)
	{
		destroy ();
		if (!IsFileExists (filepath)) return false;
		HRESULT hr = GetBundleReader (filepath.c_str (), &bundle);
		if (SUCCEEDED (hr)) return true;
		if (bundle) { bundle->Release (); bundle = nullptr; }
		hr = GetPackageReader (filepath.c_str (), &appx);
		if (SUCCEEDED (hr)) return true;
		if (appx) { appx->Release (); appx = nullptr; }
		return false;
	}
	bool valid () const { return (bool)appx ^ (bool)bundle; }
	PackageType type () const
	{
		if (appx) return PackageType::single;
		if (bundle) return PackageType::bundle;
		return PackageType::unknown;
	}
	appxreader appx_reader () const { return appxreader (*(IAppxPackageReader **)&this->appx); }
	bundlereader bundle_reader () const { return bundlereader (*(IAppxBundleReader **)&this->bundle); }
};

class appxmanifest: virtual public com_info_quote <IAppxManifestReader>
{
	using Base = com_info_quote <IAppxManifestReader>;
	template <typename IComPtr, typename ReturnType, typename Fn> HRESULT get (IComPtr iptr, Fn func, ReturnType *retvalue) const { if (!iptr) return E_FAIL; return (iptr->*func) (retvalue); }
	using Manifest = IAppxManifestReader;
	public:
	using Base::Base;
	Manifest *manifest () { return pointer (); }
	template <class IComPtr, class Func> HRESULT get_from_manifest (Func fn, _Outptr_ IComPtr *output) const
	{
		if (!pointer ()) return E_FAIL;
		return get (pointer (), fn, output);
	}
	HRESULT get_identity (_Outptr_ IAppxManifestPackageId **output) const { return get_from_manifest <IAppxManifestPackageId *> (&Manifest::GetPackageId, output); }
	appx_info::appx_id identity () const
	{
		IAppxManifestPackageId *ip = nullptr;
		get_identity (&ip);
		return appx_info::appx_id (ip);
	}
	appx_info::appx_res resources () const
	{
		return appx_info::appx_res (pointer ());
	}
	HRESULT get_properties (_Outptr_ IAppxManifestProperties **output) const { return get_from_manifest <IAppxManifestProperties *> (&Manifest::GetProperties, output); }
	appx_info::appx_prop properties () const
	{
		IAppxManifestProperties *ip = nullptr;
		HRESULT hr = get_properties (&ip);
		return appx_info::appx_prop (ip);
	}
	appx_info::appx_preq prerequisites () const
	{
		return appx_info::appx_preq (pointer ());
	}
	HRESULT get_applications (_Outptr_ IAppxManifestApplicationsEnumerator **output) const { return get_from_manifest <IAppxManifestApplicationsEnumerator *> (&Manifest::GetApplications, output); }
	appx_info::appx_apps applications () const
	{
		IAppxManifestApplicationsEnumerator *ip = nullptr;
		get_applications (&ip);
		return appx_info::appx_apps (ip, pointer ());
	}
	HRESULT get_capabilities (_Outptr_ APPX_CAPABILITIES *output) const { return get_from_manifest <APPX_CAPABILITIES> (&Manifest::GetCapabilities, output); }
	HRESULT get_device_capabilities (_Outptr_ IAppxManifestDeviceCapabilitiesEnumerator **output) const { return get_from_manifest <IAppxManifestDeviceCapabilitiesEnumerator *> (&Manifest::GetDeviceCapabilities, output); }
	appx_info::appx_capabs capabilities () const
	{
		APPX_CAPABILITIES caps;
		IAppxManifestDeviceCapabilitiesEnumerator *ip = nullptr;
		if (pointer ()) pointer ()->GetDeviceCapabilities (&ip);
		auto im = pointer ();
		if (SUCCEEDED (get_capabilities (&caps))) return appx_info::appx_capabs (ip, caps, im);
		return appx_info::appx_capabs (ip);
	}
	HRESULT get_dependencies (_Outptr_ IAppxManifestPackageDependenciesEnumerator **output) const { return get_from_manifest <IAppxManifestPackageDependenciesEnumerator *> (&Manifest::GetPackageDependencies, output); }
	appx_info::appx_deps dependencies () const
	{
		IAppxManifestPackageDependenciesEnumerator *ip = nullptr;
		get_dependencies (&ip);
		return appx_info::appx_deps (ip);
	}
	PackageRole package_role () const
	{
		auto prop = properties ();
		if (prop.framework ()) return PackageRole::framework;
		try { if (prop.resource_package ()) return PackageRole::resource; }
		catch (const std::exception &e) {}
		auto app = applications ();
		std::vector <std::wstring> apps;
		if (app.app_user_model_ids (apps)) return PackageRole::application;
		else return PackageRole::unknown;
	}
};
class bundlemanifest: virtual public com_info_quote <IAppxBundleManifestReader>
{
	using Base = com_info_quote <IAppxBundleManifestReader>;
	public:
	using Base::Base;
	template <typename IComPtr, typename ReturnType, typename Fn> HRESULT get (IComPtr iptr, Fn func, ReturnType *retvalue) const { if (!iptr) return E_FAIL; return (iptr->*func) (retvalue); }
	using Manifest = IAppxBundleManifestReader;
	Manifest *manifest () { return pointer (); }
	template <class IComPtr, class Func> HRESULT get_from_manifest (Func fn, _Outptr_ IComPtr *output) const
	{
		if (!pointer ()) return E_FAIL;
		return get (pointer (), fn, output);
	}
	HRESULT get_identity (_Outptr_ IAppxManifestPackageId **output) const { return get_from_manifest (&Manifest::GetPackageId, output); }
	appx_info::appx_id identity () const
	{
		IAppxManifestPackageId *ip = nullptr;
		get_identity (&ip);
		return appx_info::appx_id (ip);
	}
	HRESULT get_package_id_items (_Outptr_ IAppxBundleManifestPackageInfoEnumerator **output) const { return get_from_manifest (&Manifest::GetPackageInfoItems, output); }
	appx_info::appx_iditems package_id_items () const
	{
		IAppxBundleManifestPackageInfoEnumerator *ip = nullptr;
		get_package_id_items (&ip);
		return appx_info::appx_iditems (ip);
	}
};

HRESULT GetAppxManifestReader (_In_ LPCWSTR inputPath, _Outptr_ IAppxManifestReader **manifestReader)
{
	if (!manifestReader) return E_POINTER;
	*manifestReader = nullptr;
	HRESULT hr;
	CComPtr <IStream> stream;
	CComPtr <IAppxFactory> factory;
	CComPtr <IAppxPackageReader> packageReader;
	hr = SHCreateStreamOnFileEx (
		inputPath,
		STGM_READ | STGM_SHARE_DENY_NONE,
		FILE_ATTRIBUTE_NORMAL,
		FALSE,
		nullptr,
		&stream
	);
	if (FAILED (hr)) return hr;
	hr = CoCreateInstance (
		__uuidof (AppxFactory),
		nullptr,
		CLSCTX_INPROC_SERVER,
		IID_PPV_ARGS (&factory)
	);
	if (FAILED (hr)) return hr;
	hr = factory->CreatePackageReader (stream, &packageReader);
	if (SUCCEEDED (hr))
	{
		return packageReader->GetManifest (manifestReader);
	}
	hr = factory->CreateManifestReader (stream, manifestReader);
	return hr;
}
HRESULT GetBundleManifestReader (_In_ LPCWSTR bundlePath, _Outptr_ IAppxBundleManifestReader **manifestReader)
{
	if (!manifestReader) return E_POINTER;
	*manifestReader = nullptr;
	HRESULT hr;
	CComPtr <IStream> stream;
	CComPtr <IAppxBundleFactory> factory;
	CComPtr <IAppxBundleReader> bundleReader;
	hr = SHCreateStreamOnFileEx (
		bundlePath,
		STGM_READ | STGM_SHARE_DENY_NONE,
		FILE_ATTRIBUTE_NORMAL,
		FALSE,
		nullptr,
		&stream
	);
	if (FAILED (hr)) return hr;
	hr = CoCreateInstance (
		__uuidof(AppxBundleFactory),
		nullptr,
		CLSCTX_INPROC_SERVER,
		IID_PPV_ARGS (&factory)
	);
	if (FAILED (hr)) return hr;
	hr = factory->CreateBundleReader (stream, &bundleReader);
	if (FAILED (hr)) return hr;
	hr = bundleReader->GetManifest (manifestReader);
	if (FAILED (hr)) return hr = factory->CreateBundleManifestReader (stream, manifestReader);
	return hr;
}

class manifest
{
	IAppxManifestReader *appx = nullptr;
	IAppxBundleManifestReader *bundle = nullptr;
	public:
	~manifest () { destroy (); }
	manifest (const std::wstring &filepath = L"") { create (filepath); }
	void destroy ()
	{
		if (appx) { appx->Release (); appx = nullptr; }
		if (bundle) { bundle->Release (); bundle = nullptr; }
	}
	bool create (const std::wstring &filepath)
	{
		destroy ();
		if (!IsFileExists (filepath)) return false;
		HRESULT hr = GetBundleManifestReader (filepath.c_str (), &bundle);
		if (SUCCEEDED (hr)) return true;
		if (bundle) { bundle->Release (); bundle = nullptr; }
		hr = GetAppxManifestReader (filepath.c_str (), &appx);
		if (SUCCEEDED (hr)) return true;
		if (appx) { appx->Release (); appx = nullptr; }
		return false;
	}
	bool valid () const { return (bool)appx ^ (bool)bundle; }
	PackageType type () const
	{
		if (appx) return PackageType::single;
		if (bundle) return PackageType::bundle;
		return PackageType::unknown;
	}
	appxmanifest appx_reader () const { return appxmanifest (*(IAppxManifestReader **)&this->appx); }
	bundlemanifest bundle_reader () const { return bundlemanifest (*(IAppxBundleManifestReader **)&this->bundle); }
};