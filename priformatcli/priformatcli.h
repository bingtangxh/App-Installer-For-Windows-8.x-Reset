// 下列 ifdef 块是创建使从 DLL 导出更简单的
// 宏的标准方法。此 DLL 中的所有文件都是用命令行上定义的 PRIFORMATCLI_EXPORTS
// 符号编译的。在使用此 DLL 的
// 任何其他项目上不应定义此符号。这样，源文件中包含此文件的任何其他项目都会将
// PRIFORMATCLI_API 函数视为是从 DLL 导入的，而此 DLL 则将用此宏定义的
// 符号视为是被导出的。
#ifdef PRIFORMATCLI_EXPORTS
#define PRIFORMATCLI_API __declspec(dllexport)
#else
#define PRIFORMATCLI_API __declspec(dllimport)
#endif

#ifndef _PRI_READER_CLI_HEADER_
#define _PRI_READER_CLI_HEADER_

#ifdef __cplusplus
#define _DEFAULT_VALUE_SET(valueexpr) = valueexpr
#ifndef PRIFORMATCLI_EXPORTS
#define _DEFAULT_VALUE_SET_FUNC(valueexpr) _DEFAULT_VALUE_SET (valueexpr)
#else
#define _DEFAULT_VALUE_SET_FUNC(valueexpr)
#endif
#else
#define _DEFAULT_VALUE_SET(valueexpr)
#define _DEFAULT_VALUE_SET_FUNC(valueexpr)
#endif
#ifdef __cplusplus
extern "C" {
#endif
	// 为了兼容。将 IStream * 转换成 PCOISTREAM 使用（C 用强制类型转换，C++ 也可以用强制类型，或者 reinterpret_cast?）。
	typedef struct COISTREAM__ *PCOISTREAM;
	// 返回 PriFile 实例指针。
	typedef struct CSPRIFILE__ *PCSPRIFILE;
	// 创建 PriFile 实例。请传入有效指针。失败返回 nullptr。
	typedef struct LPCWSTRLIST__
	{
		DWORD dwLength _DEFAULT_VALUE_SET (0);
		LPCWSTR aswArray [1] _DEFAULT_VALUE_SET ({0});
	} LPCWSTRLIST, *HLPCWSTRLIST;
	// 注意：PriFile 使用 IStream * 流类似于“借用”关系。要保证 PriFile 实例的生命周期
	// 在 IStream * 的生命周期内。
	PRIFORMATCLI_API PCSPRIFILE CreatePriFileInstanceFromStream (PCOISTREAM pStream);
	// 销毁 PriFile 对象。注意： PriFile 的销毁不会影响 IStream *。
	PRIFORMATCLI_API void DestroyPriFileInstance (PCSPRIFILE pFilePri);
	// 获取 PriFile 的文本资源。文本资源是会根据当前系统/用户正在使用的语言来选择最合适的文本。
	// 这里没有自定义获取。注：返回的有效非空指针请用 free 来释放。
	// 传入的是 ms-resource: 开头的字符串，以 URI 形式。
	// 示例：
	// 1. ms-resource://microsoft.windowscommunicationsapps/hxoutlookintl/AppManifest_OutlookDesktop_DisplayName (常见于 UWP 应用)
	// 2. ms-resource:ApplicationTitleWithBranding (常见于 Windows 8.x 应用)
	PRIFORMATCLI_API LPWSTR GetPriStringResource (PCSPRIFILE pFilePri, LPCWSTR lpswUri);
	// 获取 PriFile 的文件路径资源。对于一些如图像资源，会有对于系统不同 DPI 的适应
	// 目录分隔符为“\”（在应用清单中对于文件路径一直使用“\”），“/”不会支持。且一定为相对路径。
	// 注：返回的有效非空指针请用 free 释放。
	PRIFORMATCLI_API LPWSTR GetPriPathResource (PCSPRIFILE pFilePri, LPCWSTR lpswFilePath);
	// 从文件路径创建 PriFile 实例。（虽然用不上）。销毁同样通过 DestroyPriFileInstance。
	PRIFORMATCLI_API PCSPRIFILE CreatePriFileInstanceFromPath (LPCWSTR lpswFilePath);
	// 注意：可能会由于并发等导致返回的异常信息会错乱。
	PRIFORMATCLI_API LPCWSTR PriFileGetLastError ();
	// 对于多个资源，建议一次遍及，预先进行缓存。通过 GetPriStringResource 获取值。
	PRIFORMATCLI_API void FindPriStringResource (PCSPRIFILE pFilePri, HLPCWSTRLIST hUriList);
	// 对于多个资源，建议一次遍及，预先进行缓存。通过 GetPriPathResource 获取值。
	PRIFORMATCLI_API void FindPriPathResource (PCSPRIFILE pFilePri, HLPCWSTRLIST hPathList);
	// 清理内存缓存数据，仅用于测试用。注意：清理指的是所有。
	PRIFORMATCLI_API void ClearPriCacheData ();
	// 获取资源，作用与 GetPriStringResource 和 GetPriPathResource 一样。（因为这两个函数都是调用此函数）
	PRIFORMATCLI_API LPWSTR GetPriResource (PCSPRIFILE pFilePri, LPCWSTR lpswResId);
	// 开始查找资源，与 FindPriStringResource 和 FindPriPathResource 一样（因为这两个函数都是调用此函数）
	PRIFORMATCLI_API void FindPriResource (PCSPRIFILE pFilePri, HLPCWSTRLIST hUriList);
	// 工具函数，用于判断是否为 ms-resource: 前缀
	PRIFORMATCLI_API BOOL IsMsResourcePrefix (LPCWSTR pResName);
	// 工具函数，用于判断是否为 ms-resource:// 前缀
	PRIFORMATCLI_API BOOL IsMsResourceUriFull (LPCWSTR pResUri);
	// 工具函数，用于判断是否为 MS-Resource URI
	PRIFORMATCLI_API BOOL IsMsResourceUri (LPCWSTR pResUri);
	// 工具函数，用于释放本地 DLL 返回的字符串
	PRIFORMATCLI_API void PriFormatFreeString (LPWSTR lpStrFromThisDll);
	typedef struct DWORDWSTRPAIR__
	{
		DWORD dwKey _DEFAULT_VALUE_SET (0);
		LPWSTR lpValue _DEFAULT_VALUE_SET (NULL);
	} DWORDWSTRPAIR;
	typedef struct DWSPAIRLIST__
	{
		DWORD dwLength _DEFAULT_VALUE_SET (0);
		DWORDWSTRPAIR lpArray [1];
	} DWSPAIRLIST, *HDWSPAIRLIST;
	// HDWSPAIRLIST 由此销毁
	PRIFORMATCLI_API void DestroyPriResourceAllValueList (HDWSPAIRLIST list);
	// DWORD 部分
	// 高 16 位：
	// 高 4 位：0: Scale 资源, 1: TargetSize 资源, 2: 字符串资源（防止无法获取资源）
	// 低 4 位：对比度 0 None, 1 White, 2 Black, 3 High, 4 Low
	// 低 16 位：
	// Scale 或 TargetSize 或 LCID
	PRIFORMATCLI_API HDWSPAIRLIST GetPriResourceAllValueList (PCSPRIFILE pPriFile, LPCWSTR lpResName);
	typedef struct WSDSPAIR__
	{
		LPWSTR lpKey;
		HDWSPAIRLIST lpValue;
	} WSDSPAIR;
	typedef struct WSDSPAIRLIST___
	{
		DWORD dwLength _DEFAULT_VALUE_SET (0);
		WSDSPAIR lpArray [1];
	} WSDSPAIRLIST, *HWSDSPAIRLIST;
	// HWSDSPAIRLIST 由此销毁
	PRIFORMATCLI_API void DestroyResourcesAllValuesList (HWSDSPAIRLIST list);
	// DWORD 部分
	// 高 16 位：
	// 高 4 位：0: Scale 资源, 1: TargetSize 资源, 2: 字符串资源（防止无法获取资源）
	// 低 4 位：对比度 0 None, 1 White, 2 Black, 3 High, 4 Low
	// 低 16 位：
	// Scale 或 TargetSize 或 LCID
	PRIFORMATCLI_API HWSDSPAIRLIST GetPriResourcesAllValuesList (PCSPRIFILE pPriFile, const LPCWSTR *lpResNames, DWORD dwCount);
#ifdef _DEFAULT_VALUE_SET
#undef _DEFAULT_VALUE_SET
#endif
#ifdef _DEFAULT_VALUE_SET_FUNC
#undef _DEFAULT_VALUE_SET_FUNC
#endif
#ifdef __cplusplus
}
#endif

#if defined (__cplusplus)
#include <string>
#include <atlbase.h>
#include <Shlwapi.h>
#include <functional>
#include <vector>
#include <map>
// 注意：此类仅为操作类，如果以 IStream * 来初始化类的话在类销毁时不会释放 IStream *。请在此类实例生命期结束后必须释放 IStream *
typedef class prifile
{
	private:
	PCSPRIFILE m_hPriFile = nullptr;
	class reltask
	{
		public:
		std::function <void ()> endtask;
		reltask (std::function <void ()> pf): endtask (pf) {}
		reltask () { if (endtask) endtask (); }
	};
	public:
	bool valid () const { return m_hPriFile != nullptr; }
	void destroy ()
	{
		if (m_hPriFile)
		{
			DestroyPriFileInstance (m_hPriFile);
			m_hPriFile = nullptr;
		}
	}
	~prifile () { destroy (); }
	bool create (IStream *p)
	{
		try
		{
			destroy ();
			if (!p) return false;
			m_hPriFile = CreatePriFileInstanceFromStream ((PCOISTREAM)p);
		}
		catch (const std::exception &e)
		{
			m_hPriFile = nullptr;
		}
		return valid ();
	}
	bool create (const std::wstring &filepath)
	{
		try
		{
			destroy ();
			if (filepath.empty ()) return false;
			m_hPriFile = CreatePriFileInstanceFromPath (filepath.c_str ());
		}
		catch (const std::exception &e)
		{
			m_hPriFile = nullptr;
		}
		return valid ();
	}
	prifile (IStream *pStream) { create (pStream); }
	prifile (const std::wstring &filepath) { create (filepath); }
	prifile () = default;
	void add_search (const std::vector <std::wstring> &arr) const 
	{
		BYTE *buf = (BYTE *)malloc (sizeof (LPCWSTRLIST) + sizeof (LPWSTR) * arr.size ());
		reltask release ([&buf] () {
			if (buf) free (buf);
			buf = nullptr;
		});
		HLPCWSTRLIST hList = (HLPCWSTRLIST)buf;
		hList->dwLength = arr.size ();
		for (size_t i = 0; i < hList->dwLength; i ++)
		{
			hList->aswArray [i] = arr [i].c_str ();
		}
		FindPriResource (m_hPriFile, hList);
	}
	void add_search (const std::wstring &arr) const
	{
		BYTE buf [sizeof (LPCWSTRLIST) + sizeof (LPWSTR)] = {0};
		HLPCWSTRLIST hList = (HLPCWSTRLIST)buf;
		hList->dwLength = 1;
		hList->aswArray [0] = arr.c_str ();
		FindPriResource (m_hPriFile, hList);
	}
	std::wstring resource (const std::wstring &resname) const
	{
		LPWSTR lpstr = nullptr;
		reltask release ([&lpstr] () {
			if (lpstr) free (lpstr);
			lpstr = nullptr;
		});
		lpstr = GetPriResource (m_hPriFile, resname.c_str ());
		return lpstr ? lpstr : L"";
	}
	size_t resources (const std::vector <std::wstring> &resnames, std::map <std::wstring, std::wstring> &output) const
	{
		add_search (resnames);
		for (auto &it : resnames) output [it] = resource (it);
		return output.size ();
	}
	LPCWSTR last_error () { return PriFileGetLastError (); }
	std::wstring path (const std::wstring &resname) const { return resource (resname); }
	size_t paths (const std::vector <std::wstring> &resnames, std::map <std::wstring, std::wstring> &output) const { return resources (resnames, output); }
	std::wstring string (const std::wstring &resname) const { return resource (resname); }
	size_t strings (const std::vector <std::wstring> &resnames, std::map <std::wstring, std::wstring> &output) const { return resources (resnames, output); }
} prifilecli;
// 注意：此类仅为操作类，如果以 IStream * 来初始化类的话在类销毁时不会释放 IStream *。请在此类实例生命期结束后必须释放 IStream *
typedef class prifilebundle
{
	public:
	struct PRI_INST
	{
		// 0b01 语言 0b10 缩放资源 0b11 两者（或者是应用包的资源包）
		uint8_t type = 0;
		prifile priinst;
		bool res_language () const { return type & 0b01; }
		bool res_scale () const { return type & 0b10; }
		bool res_both () const { return type & 0b11; }
		bool is_valid () const { return type & 0b11; }
		// 类型：0: 无效，不许插入，1：语言资源，2：缩放资源，3：两者都有
		// 必须插入有效的内容
		PRI_INST (uint8_t type = 0, IStream *ipri = nullptr):
			type (type), priinst (ipri) {}
	};
	private:
	std::vector <PRI_INST> prifiles;
	std::map <uint8_t, PRI_INST *> mappri;
	public:
	prifilebundle () { prifiles.reserve (3); }
	// 类型：0: 无效，不许插入，1：语言资源，2：缩放资源，3：两者都有
	// 插入 3 类型的不会顶替 1 和 2 类型，只会顶替 3 类型。所以类中储存的实例最多只有三个。
	// 必须指定有效类型。如果 prifile 为 nullptr，则以位置相应类型的 pri 要执行销毁。
	bool set (uint8_t type, IStream *prifile)
	{
		uint8_t realt = type & 0b11;
		if (!type) return false;
		if (mappri.find (type & 0b11) != mappri.end ())
		{
			auto &prii = mappri [realt];
			prii->priinst.destroy ();
			if (prifile) prii->priinst.create (prifile);
		}
		else
		{
			if (!prifile) return false;
			prifiles.emplace_back (realt, prifile);
			for (auto &it : prifiles)
			{
				if (it.type == realt) { mappri [realt] = &it; break; }
			}
		}
		return true;
	}
	// 类型：0: 无效，不许插入，1：语言资源，2：缩放资源，3：两者都有
	// 失败返回 nullptr
	// 注意：获取 1 和 2 对应的 pri 资源，如果不存在会尝试返回 3 的资源。
	// mustreturn 指的是必须返回一个 pri 实例，不管是否符合。这是个没办法的办法
	prifile *get (uint8_t type, bool mustreturn = false) 
	{
		type &= 0b11;
		auto it = mappri.find (type);
		if (it != mappri.end ()) return &it->second->priinst;
		if (type != 0b11)
		{
			it = mappri.find (0b11);
			if (it != mappri.end ()) return &it->second->priinst;
		}
		if (!prifiles.empty () && mustreturn) return &prifiles.front ().priinst;
		return nullptr;
	}
	void add_search (const std::vector <std::wstring> &arr) 
	{
		std::vector <std::wstring> strresname;
		std::vector <std::wstring> pathresname;
		for (auto &it : arr)
		{
			if (IsMsResourcePrefix (it.c_str ())) strresname.push_back (it);
			else pathresname.push_back (it);
		}
		auto langpri = get (1, true);
		auto scalepri = get (2, true);
		if (langpri) langpri->add_search (strresname);
		if (scalepri) scalepri->add_search (pathresname);
	}
	void add_search (const std::wstring &resname)
	{
		if (IsMsResourcePrefix (resname.c_str ()))
		{
			auto langpri = get (1, true);
			if (langpri) langpri->add_search (resname);
		}
		else
		{
			auto scalepri = get (2, true);
			if (scalepri) scalepri->add_search (resname);
		}
	}
	std::wstring resource (const std::wstring &resname)
	{
		if (IsMsResourcePrefix (resname.c_str ()))
		{
			auto langpri = get (1, true);
			if (langpri) return langpri->resource (resname);
		}
		else
		{
			auto scalepri = get (2, true);
			if (scalepri) return scalepri->resource (resname);
		}
		return L"";
	}
	size_t resources (const std::vector <std::wstring> &resnames, std::map <std::wstring, std::wstring> &output)
	{
		add_search (resnames);
		for (auto &it : resnames) output [it] = resource (it);
		return output.size ();
	}
	std::wstring path (const std::wstring &resname) { return resource (resname); }
	size_t paths (const std::vector <std::wstring> &resnames, std::map <std::wstring, std::wstring> &output) { return resources (resnames, output); }
	std::wstring string (const std::wstring &resname) { return resource (resname); }
	size_t strings (const std::vector <std::wstring> &resnames, std::map <std::wstring, std::wstring> &output) { return resources (resnames, output); }
	void destroy ()
	{
		mappri.clear ();
		prifiles.clear ();
	}
} prifilebcli;
#endif

#endif