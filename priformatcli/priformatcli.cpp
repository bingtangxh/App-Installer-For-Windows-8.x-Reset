// priformatcli.cpp : 定义 DLL 应用程序的导出函数。
//

#include "stdafx.h"
#include "priformatcli.h"
#include "prifile.h"
#include "typestrans.h"
#include "mpstr.h"
#include "nstring.h"
#include "themeinfo.h"
#include "localeex.h"
#include "syncutil.h"
#include "strcmp.h"

#include <string>
#include <vector>
#include <map>
#include <cwchar>
#include <functional>
#include <algorithm>
#include <set>

const std::wstring g_swMsResUriProtocolName = L"ms-resource:";
const size_t g_cbMsResPNameLength = lstrlenW (g_swMsResUriProtocolName.c_str ());
std::wstring g_swExcept = L"";
CriticalSection g_csLastErr;
CriticalSection g_threadlock;
CriticalSection g_iterlock;
struct destruct
{
	std::function <void ()> endtask = nullptr;
	destruct (std::function <void ()> init): endtask (init) {}
	~destruct () { if (endtask) endtask (); }
};
void SetPriLastError (const std::wstring &lpMsg)
{
	CreateScopedLock (g_csLastErr);
	g_swExcept = lpMsg;
}
enum class OpenType
{
	Unknown,
	IStream,
	Stream
};
ref class PriFileInst
{
	public:
	PriFile ^inst = nullptr;
	OpenType opentype = OpenType::Unknown;
	IStream *isptr = nullptr;
	System::IO::Stream ^fsptr = nullptr;
	operator PriFile ^ () { return inst; }
	operator IStream * () { return isptr; }
	operator System::IO::Stream ^ () { return fsptr; }
	explicit operator bool () { return inst && (int)opentype && ((bool)isptr ^ (fsptr != nullptr)); }
	size_t Seek (int64_t offset, System::IO::SeekOrigin origin)
	{
		if (isptr)
		{
			ULARGE_INTEGER ul;
			ul.QuadPart = 0;
			LARGE_INTEGER lo;
			lo.QuadPart = offset;
			DWORD dwOrigin = 0;
			switch (origin)
			{
				case System::IO::SeekOrigin::Begin: dwOrigin = STREAM_SEEK_SET;
					break;
				case System::IO::SeekOrigin::Current: dwOrigin = STREAM_SEEK_CUR;
					break;
				case System::IO::SeekOrigin::End: dwOrigin = STREAM_SEEK_END;
					break;
				default:
					break;
			}
			HRESULT hr = isptr->Seek (lo, dwOrigin, &ul);
			return ul.QuadPart;
		}
		else if (fsptr)
		{
			return fsptr->Seek (offset, origin);
		}
		throw gcnew NullReferenceException ("Error: cannot get the stream.");
		return 0;
	}
	!PriFileInst ()
	{
		if (fsptr)
		{
			fsptr->Close ();
			delete fsptr;
			fsptr = nullptr;
		}
		if (inst)
		{
			delete inst;
			inst = nullptr;
		}
	}
	~PriFileInst ()
	{
		if (fsptr)
		{
			fsptr->Close ();
			delete fsptr;
			fsptr = nullptr;
		}
		if (inst)
		{
			delete inst;
			inst = nullptr;
		}
	}
};
size_t KeyToPath (const std::wstring &key, std::vector <std::wnstring> &output);
typedef struct _TASKITEM_SEARCH
{
	std::wstring swKey;
	int iTaskType; // 0: 字符串，1: 文件路径
	operator std::wstring () { return swKey; }
	operator LPCWSTR () { return swKey.c_str (); }
	operator std::wnstring () { return swKey; }
	void set_key (const std::wstring &value)
	{
		iTaskType = std::wnstring (GetStringLeft (std::wnstring::trim (value), g_cbMsResPNameLength)) != g_swMsResUriProtocolName;
		swKey = value;
	}
	bool isuri () const
	{
		return std::wnstring (GetStringLeft (std::wnstring::trim (swKey), g_cbMsResPNameLength)) == g_swMsResUriProtocolName;
	}
	bool isfulluri () const
	{
		const std::wstring root = L"//";
		return std::wnstring (GetStringLeft (std::wnstring::trim (swKey), g_cbMsResPNameLength + root.length ())) == g_swMsResUriProtocolName + root;
	}
	bool isfilepath () const { return !isuri (); }
	bool isrelativeuri () const { return !isfulluri () && isuri (); }
	size_t get_path (std::vector <std::wnstring> &output) const
	{
		output.clear ();
		auto &path = output;
		KeyToPath (swKey, path);
		if (isrelativeuri ())
		{
			std::wstring nopre = GetStringRight (swKey, swKey.length () - g_cbMsResPNameLength);
			std::wstring firstch = GetStringLeft (nopre, 1);
			if (firstch [0] != L'/') path.insert (path.begin (), L"resources");
		}
		else if (isfilepath ()) path.insert (path.begin (), L"Files");
		return output.size ();
	}
	_TASKITEM_SEARCH &operator = (const std::wstring &v)
	{
		set_key (v);
		return *this;
	}
	explicit _TASKITEM_SEARCH (const std::wstring &v, int type = -1)
	{
		if (type < 0 || type > 1) set_key (v);
		else
		{
			swKey = v;
			iTaskType = type;
		}
	}
	_TASKITEM_SEARCH (int type, const std::wstring &v = L"")
	{
		if (type < 0 || type > 1) set_key (v);
		else
		{
			swKey = v;
			iTaskType = type;
		}
	}
	_TASKITEM_SEARCH () = default;
	bool operator == (const _TASKITEM_SEARCH &another) const 
	{
		return std::wnstring (swKey).equals (another.swKey);
	}
	bool operator < (const _TASKITEM_SEARCH &another) const
	{
		return std::wnstring (swKey).compare (another.swKey) < 0;
	}
} TASKITEM_SEARCH;
typedef struct _TASKRESULT_FIND
{
	std::wstring swValue = L"";
	int iFindResult = -1; // -1 未进行查找，0：查找但未找到，1：查找且已找到
	operator std::wstring () { return swValue; }
	operator LPCWSTR () { return swValue.c_str (); }
	operator std::wnstring () { return swValue; }
	_TASKRESULT_FIND (const std::wstring &v, int findres = -1):
		swValue (v), iFindResult (findres) {}
	_TASKRESULT_FIND (int findres, const std::wstring &v = L""):
		swValue (v), iFindResult (findres) {}
	_TASKRESULT_FIND () = default;
	// 是否查找到
	bool is_find () const { return iFindResult > 0; }
	// 是否进行过查找
	bool has_search () const { return iFindResult >= 0; }
} TASKRESULT_FIND;
typedef struct _TASKINFO_SEARCH
{
	bool bIsRunning = false;
	std::map <TASKITEM_SEARCH, TASKRESULT_FIND> mapTasks;
	operator std::map <TASKITEM_SEARCH, TASKRESULT_FIND> () { return mapTasks; }
} TASKINFO_SEARCH;

size_t UriToPath (System::Uri ^uri, std::vector <std::wnstring> &output)
{
	output.clear ();
	try
	{
		auto path = uri->AbsolutePath;
		auto arr = path->Split ('/');
		for (size_t i = 0; i < arr->Length; i ++)
		{
			auto str = arr [i];
			std::wnstring cppstr = MPStringToStdW (str);
			if (cppstr.empty ()) continue;
			output.push_back (cppstr);
		}
	}
	catch (Exception ^e)
	{
		SetPriLastError (MPStringToStdW (e->Message));
	}
	return output.size ();
}
std::vector <std::wstring> split_wcstok (const std::wstring &str, const std::wstring &delim)
{
	std::vector <std::wstring> result;
	std::wstring cpy = L"" + str;
	LPWSTR context = nullptr;
	LPWSTR token = wcstok ((LPWSTR)cpy.c_str (), delim.c_str (), &context);
	while (token)
	{
		result.push_back (token);
		token = wcstok (nullptr, delim.c_str (), &context);
	}
	return result;
}
std::vector <std::wnstring> VecWStringToWNString (const std::vector <std::wstring> &vec)
{
	std::vector <std::wnstring> wns;
	wns.reserve (vec.size ());
	for (auto &it : vec) wns.push_back (it);
	return wns;
}
size_t KeyToPath (const std::wstring &key, std::vector <std::wnstring> &output)
{
	output.clear ();
	try
	{
		// 1: 字符串，0: 文件路径
		int iTaskType = std::wnstring (GetStringLeft (key, g_cbMsResPNameLength)) == g_swMsResUriProtocolName;
		if (iTaskType)
		{
			Uri ^uri = gcnew Uri (CStringToMPString (key.c_str ()));
			size_t ret = UriToPath (uri, output);
			delete uri;
			uri = nullptr;
			return ret;
		}
		else
		{
			auto arr = split_wcstok (key, L"\\");
			for (auto &it : arr)
			{
				if (std::wnstring (it).empty ()) continue;
				else output.push_back (it);
			}
		}
	}
	catch (Exception ^e)
	{
		auto arr = split_wcstok (key, L"\\");
		for (auto &it : arr)
		{
			if (std::wnstring (it).empty ()) continue;
			else output.push_back (it);
		}
	}
	return output.size ();
}
size_t KeyToPath (const TASKITEM_SEARCH &key, std::vector <std::wnstring> &output)
{
	return KeyToPath (key.swKey, output);
}
bool PathEquals (const std::vector <std::wnstring> &left, const std::vector <std::wnstring> &right)
{
	if (left.size () != right.size ()) return false;
	try
	{
		for (size_t i = 0; i < left.size () && i < right.size (); i ++)
		{
			if (left.at (i) != right.at (i)) return false;
		}
		return true;
	}
	catch (const std::exception &e) {}
	return false;
}

std::map <PCSPRIFILE, TASKINFO_SEARCH> g_tasklist;

PCSPRIFILE CreatePriFileInstanceFromStream (PCOISTREAM pStream)
{
	if (!pStream) return nullptr;
	try
	{
		HRESULT hr = S_OK;
		if (pStream) hr = ((IStream *)pStream)->Seek (LARGE_INTEGER {}, STREAM_SEEK_SET, nullptr);
		System::IO::Stream ^stream = nullptr;
		auto pri = PriFile::Parse (ComIStreamToCliIStream (reinterpret_cast <IStream *> (pStream)), stream);
		PriFileInst ^inst = gcnew PriFileInst ();
		inst->inst = pri;
		inst->opentype = OpenType::IStream;
		inst->isptr = reinterpret_cast <IStream *> (pStream);
		inst->fsptr = stream;
		auto handle = System::Runtime::InteropServices::GCHandle::Alloc (inst);
		IntPtr token = System::Runtime::InteropServices::GCHandle::ToIntPtr (handle);
		return reinterpret_cast <PCSPRIFILE> (token.ToPointer ());
	}
	catch (System::Exception ^e)
	{
		SetPriLastError (MPStringToStdW (e->Message));
	}
	return nullptr;
}
PCSPRIFILE CreatePriFileInstanceFromPath (LPCWSTR lpswFilePath)
{
	if (!lpswFilePath) return nullptr;
	if (!*lpswFilePath) return nullptr;
	try
	{
		auto fstream = System::IO::File::OpenRead (CStringToMPString (lpswFilePath ? lpswFilePath : L""));
		auto pri = PriFile::Parse (fstream);
		PriFileInst ^inst = gcnew PriFileInst ();
		inst->inst = pri;
		inst->opentype = OpenType::Stream;
		inst->fsptr = fstream;
		auto handle = System::Runtime::InteropServices::GCHandle::Alloc (inst);
		IntPtr token = System::Runtime::InteropServices::GCHandle::ToIntPtr (handle);
		return reinterpret_cast <PCSPRIFILE> (token.ToPointer ());
	}
	catch (System::Exception ^e)
	{
		SetPriLastError (MPStringToStdW (e->Message));
	}
	return nullptr;
}
//void DestroyPriFileInstance (PCSPRIFILE pFilePri)
//{
//	if (!pFilePri) return;
//	try
//	{
//		if (g_tasklist.find (pFilePri) != g_tasklist.end ())
//		{
//			g_tasklist [pFilePri].bIsRunning = false;
//			g_tasklist.erase (pFilePri);
//		}
//		IntPtr handlePtr = IntPtr (pFilePri); 
//		System::Runtime::InteropServices::GCHandle handle = System::Runtime::InteropServices::GCHandle::FromIntPtr (handlePtr);
//		PriFileInst ^inst = safe_cast <PriFileInst ^> (handle.Target);
//		delete inst;
//		handle.Free ();
//		System::GC::Collect ();
//		System::GC::WaitForPendingFinalizers ();
//		System::GC::Collect ();
//	}
//	catch (System::Exception ^e)
//	{
//		SetPriLastError (MPStringToStdW (e->Message));
//	}
//}
void DestroyPriFileInstance (PCSPRIFILE pFilePri)
{
	if (!pFilePri) return;
	try
	{
		CreateScopedLock (g_threadlock);
		auto it = g_tasklist.find (pFilePri);
		if (it != g_tasklist.end ())
		{
			it->second.bIsRunning = false;
			g_tasklist.erase (it);
		}
		IntPtr handlePtr = IntPtr (pFilePri);
		System::Runtime::InteropServices::GCHandle handle = System::Runtime::InteropServices::GCHandle::FromIntPtr (handlePtr);
		PriFileInst ^inst = safe_cast <PriFileInst ^> (handle.Target);
		delete inst;
		handle.Free ();
	}
	catch (System::Exception ^e)
	{
		SetPriLastError (MPStringToStdW (e->Message));
	}
	System::GC::Collect ();
	System::GC::WaitForPendingFinalizers ();
	System::GC::Collect ();
}
LPCWSTR PriFileGetLastError ()
{
	CreateScopedLock (g_csLastErr);
	return g_swExcept.c_str ();
}
enum class Contrast
{
	None = 0,
	White = 1,
	Black = 2,
	High = 3,
	Low = 4
};
struct candidate_value
{
	int type; // 1:语言；2:缩放和对比度；0:未知
	std::wstring value;
	union restitem
	{
		struct // type==1
		{
			std::wstring languages;
		};
		struct // type==2
		{
			uint32_t scale;
			Contrast contrast;
		};
		struct // type==0
		{
			std::wstring not_support_restrict;
		};
		restitem () {} // 不做初始化，由外层控制构造
		~restitem () {} // 不自动析构，由外层控制析构
	} restitems;
	candidate_value (const std::wstring &val, const std::wstring &lang): type (1), value (val)
	{
		new(&restitems.languages) std::wstring (lang);
	}
	candidate_value (const std::wstring &val, uint32_t scale, Contrast contrast = Contrast::None): type (2), value (val)
	{
		restitems.scale = scale;
		restitems.contrast = contrast;
	}
	candidate_value (const std::wstring &val): type (0), value (val)
	{
		new (&restitems.not_support_restrict) std::wstring (L"");
	}
	candidate_value (const candidate_value &other): type (other.type), value (other.value)
	{
		if (type == 1) new(&restitems.languages) std::wstring (other.restitems.languages);
		else if (type == 2)
		{
			restitems.scale = other.restitems.scale;
			restitems.contrast = other.restitems.contrast;
		}
		else new (&restitems.not_support_restrict) std::wstring (other.restitems.not_support_restrict);
	}
	candidate_value &operator = (const candidate_value &other)
	{
		if (this != &other)
		{
			this->~candidate_value (); // 先析构旧内容
			new (this) candidate_value (other); // 再调用拷贝构造
		}
		return *this;
	}
	~candidate_value ()
	{
		if (type == 1) restitems.languages.~basic_string ();
		else if (type == 0) restitems.not_support_restrict.~basic_string ();
	}
	std::wstring get_language () const { return type == 1 ? restitems.languages : L""; }
	uint32_t get_scale () const { return type == 2 ? restitems.scale : 0; }
	Contrast get_contrast () const { return type == 2 ? restitems.contrast : Contrast::None; }
};
std::wstring GetStringValueByLocale (std::vector <candidate_value> &stringcand, const std::wstring &llc)
{
	for (auto &it : stringcand)
	{
		if (LocaleNameCompare (it.get_language (), llc)) return it.value;
	}
	std::vector <std::wstring> scrc;
	for (auto &it : stringcand)
	{
		std::wstring rc;
		scrc.push_back (rc = GetLocaleRestrictedCodeFromLcidW (LocaleCodeToLcidW (it.get_language ())));
		if (LocaleNameCompare (rc, llc)) return it.value;
	}
	std::wstring lrc = GetLocaleRestrictedCodeFromLcidW (LocaleCodeToLcidW (llc));
	for (size_t i = 0; i < stringcand.size (); i ++)
	{
		auto &rc = scrc.at (i);
		auto &it = stringcand.at (i);
		if (LocaleNameCompare (rc, llc)) return it.value;
	}
	return L"";
}
std::wstring GetSuitableStringValue (std::vector <candidate_value> &stringcand)
{
	std::wstring ret = GetStringValueByLocale (stringcand, GetComputerLocaleCodeW ());
	if (ret.empty () || std::wnstring::trim (ret).length () == 0) ret = GetStringValueByLocale (stringcand, L"en-US");
	if (ret.empty () || std::wnstring::trim (ret).length () == 0)
	{
		for (auto &it : stringcand) return it.value;
	}
	return ret;
}
std::wstring GetSuitablePathValueByDPI (std::vector<candidate_value> &pathcand)
{
	std::sort (pathcand.begin (), pathcand.end (),
		[] (const candidate_value &v1, const candidate_value &v2) {
		return v1.get_scale () < v2.get_scale ();
	});
	if (pathcand.empty ()) return L"";
	uint32_t nowdpi = GetDPI ();
	for (auto &cv : pathcand) if (cv.get_scale () >= nowdpi && !StrInclude (cv.value, L"layoutdir-RTL", true)) return cv.value;
	return pathcand.back ().value;
}
std::wstring GetSuitablePathValue (std::vector <candidate_value> &pathcand)
{
	std::vector <candidate_value> contrasted;
	for (auto &it : pathcand) if (it.get_contrast () == Contrast::None) contrasted.emplace_back (it);
	std::wstring ret = GetSuitablePathValueByDPI (contrasted);
	if (std::wnstring (ret).empty ())
	{
		contrasted.clear ();
		for (auto &it: pathcand) if (it.get_contrast () == Contrast::White) contrasted.emplace_back (it);
		ret = GetSuitablePathValueByDPI (contrasted);
	}
	if (std::wnstring (ret).empty ())
	{
		contrasted.clear ();
		for (auto &it : pathcand) if (it.get_contrast () == Contrast::Black) contrasted.emplace_back (it);
		ret = GetSuitablePathValueByDPI (contrasted);
	}
	if (std::wnstring (ret).empty ())
	{
		contrasted.clear ();
		for (auto &it : pathcand) contrasted.emplace_back (it);
		ret = GetSuitablePathValueByDPI (contrasted);
	}
	return ret;
}

void PriFileIterateTask (PCSPRIFILE pFilePri)
{
	CreateScopedLock (g_threadlock);
	if (g_tasklist.find (pFilePri) == g_tasklist.end ()) g_tasklist [pFilePri] = TASKINFO_SEARCH ();
	try { g_tasklist.at (pFilePri); } catch (const std::exception &e) { return; }
	auto &task = g_tasklist.at (pFilePri);
	if (task.bIsRunning == false) task.bIsRunning = true;
	else return;
	destruct endt ([&task] () {
		task.bIsRunning = false;
	});
	auto &taskitems = task.mapTasks;
	IntPtr handlePtr = IntPtr (pFilePri);
	System::Runtime::InteropServices::GCHandle handle = System::Runtime::InteropServices::GCHandle::FromIntPtr (handlePtr);
	auto pri = safe_cast <PriFileInst ^> (handle.Target);
	auto &priFile = pri;
	auto resmapsect = pri->inst->PriDescriptorSection->ResourceMapSections;
	bool isallsearched = true;
	size_t allitemslen = 0;
	std::map <std::wnstring, size_t> mapitemscnt;
	auto begtime = System::DateTime::Now;
SearchLoop:
	allitemslen = 0;
	for (size_t i = 0; i < resmapsect->Count; i ++)
	{
		CreateScopedLock (g_iterlock);
		auto resourceMapSectionRef = resmapsect [i];
		auto resourceMapSection = pri->inst->GetSectionByRef (resourceMapSectionRef);
		if (resourceMapSection->HierarchicalSchemaReference != nullptr) continue;
		auto decisionInfoSection = pri->inst->GetSectionByRef (resourceMapSection->DecisionInfoSection);
		for each (auto candidateSet in resourceMapSection->CandidateSets->Values)
		{
			// 超时强制退出（也就没有遍及的必要了）
			if ((System::DateTime::Now - begtime).TotalSeconds > 60) return;
			allitemslen ++;
			auto item = pri->inst->GetResourceMapItemByRef (candidateSet->ResourceMapItem);
			std::wstring itemfullname = MPStringToStdW (item->FullName);
			std::vector <std::wnstring> itempath;
			{
				auto ips = split_wcstok (itemfullname, L"\\");
				for (auto &it : ips)
				{
					if (std::wnstring::empty (it)) continue;
					itempath.push_back (it);
				}
			}
			bool willcont = true;
			TASKITEM_SEARCH *taskkey = nullptr;
			for (auto &it : taskitems)
			{
				auto &key = it.first;
				auto &value = it.second;
				mapitemscnt [key.swKey] ++;
				if (value.has_search ()) continue;
				std::vector <std::wnstring> namepath;
				key.get_path (namepath);
				// KeyToPath (key, namepath);
				if (PathEquals (itempath, namepath))
				{
					taskkey = (TASKITEM_SEARCH *)&key;
					willcont = false;
					break;
				}
			}
			if (willcont) continue;
			auto keyname = taskkey->swKey;
			auto keytype = taskkey->iTaskType;
			std::vector <candidate_value> cands;
			for each (auto candidate in candidateSet->Candidates)
			{
				System::String ^value = nullptr;
				if (candidate->SourceFile.HasValue)
				{
					// 内嵌资源，暂无法处理
					// value = System::String::Format ("<external in {0}>", pri->GetReferencedFileByRef (candidate->SourceFile.Value)->FullName);
					value = pri->inst->GetReferencedFileByRef (candidate->SourceFile.Value)->FullName;
				}
				else
				{
					ByteSpan ^byteSpan = nullptr;
					if (candidate->DataItem.HasValue) byteSpan = priFile->inst->GetDataItemByRef (candidate->DataItem.Value);
					else byteSpan = candidate->Data.Value;
					pri->Seek (byteSpan->Offset, System::IO::SeekOrigin::Begin);
					auto binaryReader = gcnew System::IO::BinaryReader (pri, System::Text::Encoding::Default, true);
					auto data = binaryReader->ReadBytes ((int)byteSpan->Length);
					switch (candidate->Type)
					{
						case ResourceValueType::AsciiPath:
						case ResourceValueType::AsciiString:
							value = System::Text::Encoding::ASCII->GetString (data)->TrimEnd ('\0');
							break;
						case ResourceValueType::Utf8Path:
						case ResourceValueType::Utf8String:
							value = System::Text::Encoding::UTF8->GetString (data)->TrimEnd ('\0');
							break;
						case ResourceValueType::Path:
						case ResourceValueType::String:
							value = System::Text::Encoding::Unicode->GetString (data)->TrimEnd ('\0');
							break;
						case ResourceValueType::EmbeddedData:
							value = Convert::ToBase64String (data);
							break;
					}
					delete binaryReader;
					delete data;
					binaryReader = nullptr;
					data = nullptr;
				}
				auto qualifierSet = decisionInfoSection->QualifierSets [candidate->QualifierSet];
				auto qualis = gcnew System::Collections::Generic::Dictionary <QualifierType, Object ^> ();
				for each (auto quali in qualifierSet->Qualifiers)
				{
					auto type = quali->Type;
					auto value = quali->Value;
					qualis->Add (type, value);
				}
				if (qualis->ContainsKey (QualifierType::Language))
				{
					cands.push_back (candidate_value (MPStringToStdW (value ? value : System::String::Empty), MPStringToStdW (qualis [QualifierType::Language]->ToString ())));
				}
				else if (qualis->ContainsKey (QualifierType::Scale))
				{
					if (qualis->ContainsKey (QualifierType::Contrast))
					{
						Contrast ct = Contrast::None;
						auto contstr = std::wnstring (MPStringToStdW (qualis [QualifierType::Contrast]->ToString ()->Trim ()->ToUpper ()));
						if (contstr.equals (L"WHITE")) ct = Contrast::White;
						else if (contstr.equals (L"BLACK")) ct = Contrast::Black;
						else if (contstr.equals (L"HIGH")) ct = Contrast::High;
						cands.push_back (candidate_value (
							MPStringToStdW (value ? value : System::String::Empty),
							Convert::ToUInt32 (qualis [QualifierType::Scale]),
							ct
						));
					}
					else
					{
						cands.push_back (candidate_value (
							MPStringToStdW (value ? value : System::String::Empty),
							Convert::ToUInt32 (qualis [QualifierType::Scale])
						));
					}
				}
				else cands.push_back (candidate_value (MPStringToStdW (value ? value : System::String::Empty)));
				delete qualis;
				qualis = nullptr;
			}
			switch (keytype)
			{
				case 0: {
					TASKRESULT_FIND tfind;
					tfind.iFindResult = 1;
					tfind.swValue = GetSuitableStringValue (cands);
					taskitems [*taskkey] = tfind;
				} break;
				case 1: {
					TASKRESULT_FIND tfind;
					tfind.iFindResult = 1;
					tfind.swValue = GetSuitablePathValue (cands);
					taskitems [*taskkey] = tfind;
				} break;
				default: {
					TASKRESULT_FIND tfind;
					tfind.iFindResult = 0;
					try
					{
						tfind.swValue = cands.at (0).value;
					} 
					catch (const std::exception &e) {}
					taskitems [*taskkey] = tfind;
				} break;
			}
		}
		// delete resourceMapSection;
		resourceMapSection = nullptr;
	}
	isallsearched = true;
	for (auto &it : mapitemscnt)
	{
		auto &result = taskitems [TASKITEM_SEARCH (it.first)];
		isallsearched = isallsearched && (it.second >= allitemslen && result.has_search ());
		if (it.second >= allitemslen)
		{
			if (!result.has_search ()) result.iFindResult = 0;
		}
		it.second = 0;
	}
	if (!isallsearched)
	{
		for (auto &it : mapitemscnt) it.second = 0;
		goto SearchLoop;
	}
	// task.bIsRunning = false;
}
void PriFileIterateTaskCli (Object^ pFilePriObj)
{
	if (pFilePriObj == nullptr) return;
	IntPtr ptr = safe_cast <IntPtr> (pFilePriObj);
	PCSPRIFILE pFilePri = (PCSPRIFILE)ptr.ToPointer ();
	PriFileIterateTask (pFilePri);
}
//void AddPriResourceName (PCSPRIFILE pFilePri, const std::vector <std::wnstring> &urilist)
//{
//	if (!pFilePri) return;
//	if (!urilist.size ()) return;
//	try { g_tasklist.at (pFilePri); } catch (const std::exception &e) { g_tasklist [pFilePri] = TASKINFO_SEARCH (); }
//	auto &task = g_tasklist.at (pFilePri);
//	bool isallfined = true;
//	{
//		CreateScopedLock (g_threadlock);
//		CreateScopedLock (g_iterlock);
//		for (auto &it : urilist)
//		{
//			if (it.empty ()) continue;
//			try
//			{
//				if (task.mapTasks [TASKITEM_SEARCH (it)].has_search ())
//				{
//					isallfined = isallfined && true;
//					continue;
//				}
//				else isallfined = isallfined && false;
//			}
//			catch (const std::exception &e)
//			{
//				task.mapTasks [TASKITEM_SEARCH (it)] = TASKRESULT_FIND ();
//				isallfined = isallfined && false;
//			}
//		}
//	}
//	if (isallfined) return;
//	// while (task.bIsRunning) { Sleep (200); }
//	System::Threading::Thread ^t = nullptr;
//	if (!task.bIsRunning)
//	{
//		// task.bIsRunning = true;
//		t = gcnew System::Threading::Thread (gcnew System::Threading::ParameterizedThreadStart (PriFileIterateTaskCli));
//		t->IsBackground = true;
//		t->Start (IntPtr (pFilePri));
//	}
//}
void AddPriResourceName (PCSPRIFILE pFilePri, const std::vector <std::wnstring> &urilist)
{
	if (!pFilePri) return;
	if (!urilist.size ()) return;
	{
		CreateScopedLock (g_threadlock);
		if (g_tasklist.find (pFilePri) == g_tasklist.end ())
		{
			g_tasklist [pFilePri] = TASKINFO_SEARCH ();
		}
	}
	TASKINFO_SEARCH *ptask = nullptr;
	{
		CreateScopedLock (g_threadlock);
		ptask = &g_tasklist.at (pFilePri);
	}
	auto &task = *ptask;
	bool isallfined = true;
	{
		CreateScopedLock (g_iterlock);
		for (auto &it : urilist)
		{
			if (it.empty ()) continue;
			TASKITEM_SEARCH key (it);
			auto itFound = task.mapTasks.find (key);
			if (itFound != task.mapTasks.end ())
			{
				if (itFound->second.has_search ())
				{
					isallfined = isallfined && true;
					continue;
				}
				else isallfined = isallfined && false;
			}
			else
			{
				task.mapTasks [key] = TASKRESULT_FIND ();
				isallfined = isallfined && false;
			}
		}
	}
	if (isallfined) return;
	{
		CreateScopedLock (g_threadlock);
		if (!task.bIsRunning)
		{
			System::Threading::Thread ^t = gcnew System::Threading::Thread (gcnew System::Threading::ParameterizedThreadStart (PriFileIterateTaskCli));
			t->IsBackground = true;
			t->Start (IntPtr (pFilePri));
		}
	}
}
void FindPriResource (PCSPRIFILE pFilePri, HLPCWSTRLIST hUriList)
{
	if (!pFilePri) return;
	if (!hUriList || !hUriList->dwLength) return;
	std::vector <std::wnstring> list;
	for (size_t i = 0; i < hUriList->dwLength; i ++)
	{
		auto &str = hUriList->aswArray [i];
		if (!str || !*str) continue;
		std::wnstring wstr (str);
		if (wstr.empty ()) continue;
		list.emplace_back (wstr);
	}
	AddPriResourceName (pFilePri, list);
}
void FindPriStringResource (PCSPRIFILE pFilePri, HLPCWSTRLIST hUriList)
{
	FindPriResource (pFilePri, hUriList);
}
void FindPriPathResource (PCSPRIFILE pFilePri, HLPCWSTRLIST hPathList)
{
	FindPriResource (pFilePri, hPathList);
}
LPWSTR GetPriResource (PCSPRIFILE pFilePri, LPCWSTR lpswResId)
{
	if (!pFilePri || !lpswResId || !*lpswResId) return nullptr;
	try { g_tasklist.at (pFilePri); }
	catch (const std::exception &e) { g_tasklist [pFilePri]; }
	auto &task = g_tasklist.at (pFilePri);
	{
		auto &result = task.mapTasks [TASKITEM_SEARCH (lpswResId)];
		if (result.has_search ()) return _wcsdup (result.swValue.c_str ());
	}
	BYTE buf [sizeof (LPCWSTRLIST) + sizeof (LPCWSTR)] = {0};
	HLPCWSTRLIST hStrList = (HLPCWSTRLIST)buf;
	hStrList->dwLength = 1;
	hStrList->aswArray [0] = lpswResId;
	FindPriResource (pFilePri, hStrList);
	while (task.bIsRunning) { Sleep (200); }
	try
	{
		auto item = task.mapTasks.at (TASKITEM_SEARCH (lpswResId));
		if (!item.has_search ()) return GetPriResource (pFilePri, lpswResId);
		return _wcsdup (item.swValue.c_str ());
	}
	catch (const std::exception &e)
	{
		SetPriLastError (StringToWString (e.what () ? e.what () : "Error: cannot find the resource."));
		return nullptr;
	}
}
LPWSTR GetPriStringResource (PCSPRIFILE pFilePri, LPCWSTR lpswUri) { return GetPriResource (pFilePri, lpswUri); }
LPWSTR GetPriPathResource (PCSPRIFILE pFilePri, LPCWSTR lpswFilePath) { return GetPriResource (pFilePri, lpswFilePath); }
void ClearPriCacheData ()
{
	g_tasklist.clear ();
}

BOOL IsMsResourcePrefix (LPCWSTR pResName)
{
	return std::wnstring (GetStringLeft (std::wnstring::trim (std::wstring (pResName ? pResName : L"")), g_cbMsResPNameLength)) == g_swMsResUriProtocolName;
}
BOOL IsMsResourceUriFull (LPCWSTR pResUri)
{
	const std::wstring root = L"//";
	return std::wnstring (GetStringLeft (std::wnstring::trim (std::wstring (pResUri)), g_cbMsResPNameLength + root.length ())) == g_swMsResUriProtocolName + root;
}
BOOL IsMsResourceUri (LPCWSTR pResUri)
{
	if (!IsMsResourcePrefix (pResUri)) return false;
	try { Uri ^uri = gcnew Uri (gcnew String (pResUri ? pResUri : L"")); delete uri; }
	catch (Exception ^e) { return false; }
	return true;
}

void PriFormatFreeString (LPWSTR lpStrFromThisDll)
{
	if (!lpStrFromThisDll) return;
	free (lpStrFromThisDll);
}

PriFileInst ^GetPriFileInst (PCSPRIFILE file)
{
	if (!file) return nullptr;
	using namespace System::Runtime::InteropServices;
	IntPtr ptr (file); // void* → IntPtr
	GCHandle handle = GCHandle::FromIntPtr (ptr);
	return safe_cast<PriFileInst^>(handle.Target);
}

// 高 16 位：
// 高 4 位：0: Scale 资源, 1: TargetSize 资源, 2: 字符串资源（防止无法获取资源）
// 低 4 位：对比度 0 None, 1 White, 2 Black, 3 High, 4 Low
// 低 16 位：
// Scale 或 TargetSize 或 LCID
// 运行时，外部不可使用 output
#define PRI_TYPE_SHIFT        28
#define PRI_CONTRAST_SHIFT    24
#define PRI_TYPE_MASK         0xF0000000
#define PRI_CONTRAST_MASK     0x0F000000
#define PRI_VALUE_MASK        0x0000FFFF
#define PRI_MAKE_KEY(type, contrast, value) \
    ( ((DWORD)(type)     << PRI_TYPE_SHIFT)     | \
      ((DWORD)(contrast) << PRI_CONTRAST_SHIFT) | \
      ((DWORD)(value)    &  PRI_VALUE_MASK) )
#define PRI_MAKE_SCALE(scale, contrast) PRI_MAKE_KEY(0, contrast, scale)
#define PRI_MAKE_TARGETSIZE(size, contrast) PRI_MAKE_KEY(1, contrast, size)
#define PRI_MAKE_STRING(lcid) PRI_MAKE_KEY(2, 0, lcid)
size_t GetPriScaleAndTargetSizeFileList (
	PCSPRIFILE pPriFile, 
	const std::vector <std::wstring> &resnames,
	std::map <std::wnstring, std::map <DWORD, std::wnstring>> &output
)
{
	output.clear ();
	if (!pPriFile) return 0;
	auto inst = GetPriFileInst (pPriFile);
	auto pri = inst->inst;
	auto priFile = inst;
	auto resmapsect = pri->PriDescriptorSection->ResourceMapSections;
	std::set <std::wnstring> rnlist;
	for (auto &it : resnames) rnlist.insert (std::wnstring (it));
	for (size_t i = 0; i < resmapsect->Count; i ++)
	{
		auto resourceMapSectionRef = resmapsect [i];
		auto resourceMapSection = pri->GetSectionByRef (resourceMapSectionRef);
		if (resourceMapSection->HierarchicalSchemaReference != nullptr) continue;
		auto decisionInfoSection = pri->GetSectionByRef (resourceMapSection->DecisionInfoSection);
		for each (auto candidateSet in resourceMapSection->CandidateSets->Values)
		{
			auto item = pri->GetResourceMapItemByRef (candidateSet->ResourceMapItem);
			std::wstring itemfullname = MPStringToStdW (item->FullName);
			std::vector <std::wnstring> itempath;
			{
				auto ips = split_wcstok (itemfullname, L"\\");
				for (auto &it : ips)
				{
					if (std::wnstring::empty (it)) continue;
					itempath.push_back (it);
				}
			}
			bool isfind = false;
			std::wnstring taskkey = L"";
			int tasktype = 1;
			for (auto &it : rnlist)
			{
				TASKITEM_SEARCH key (it);
				std::vector <std::wnstring> namepath;
				key.get_path (namepath);
				if (PathEquals (itempath, namepath))
				{
					taskkey = it;
					tasktype = key.iTaskType;
					isfind = true;
					break;
				}
			}
			if (!isfind) continue;
			std::map <DWORD, std::wnstring> values;
			for each (auto candidate in candidateSet->Candidates)
			{
				DWORD resc = 0;
				System::String ^value = nullptr;
				if (candidate->SourceFile.HasValue)
				{
					// 内嵌资源，暂无法处理
					// value = System::String::Format ("<external in {0}>", pri->GetReferencedFileByRef (candidate->SourceFile.Value)->FullName);
					value = pri->GetReferencedFileByRef (candidate->SourceFile.Value)->FullName;
				}
				else
				{
					ByteSpan ^byteSpan = nullptr;
					if (candidate->DataItem.HasValue) byteSpan = priFile->inst->GetDataItemByRef (candidate->DataItem.Value);
					else byteSpan = candidate->Data.Value;
					priFile->Seek (byteSpan->Offset, System::IO::SeekOrigin::Begin);
					auto binaryReader = gcnew System::IO::BinaryReader (priFile, System::Text::Encoding::Default, true);
					auto data = binaryReader->ReadBytes ((int)byteSpan->Length);
					switch (candidate->Type)
					{
						case ResourceValueType::AsciiPath:
						case ResourceValueType::AsciiString:
							value = System::Text::Encoding::ASCII->GetString (data)->TrimEnd ('\0');
							break;
						case ResourceValueType::Utf8Path:
						case ResourceValueType::Utf8String:
							value = System::Text::Encoding::UTF8->GetString (data)->TrimEnd ('\0');
							break;
						case ResourceValueType::Path:
						case ResourceValueType::String:
							value = System::Text::Encoding::Unicode->GetString (data)->TrimEnd ('\0');
							break;
						case ResourceValueType::EmbeddedData:
							value = Convert::ToBase64String (data);
							break;
					}
					delete binaryReader;
					delete data;
					binaryReader = nullptr;
					data = nullptr;
				}
				auto qualifierSet = decisionInfoSection->QualifierSets [candidate->QualifierSet];
				auto qualis = gcnew System::Collections::Generic::Dictionary <QualifierType, Object ^> ();
				for each (auto quali in qualifierSet->Qualifiers)
				{
					auto type = quali->Type;
					auto value = quali->Value;
					qualis->Add (type, value);
				}
				if (qualis->ContainsKey (QualifierType::Language))
				{
					resc = PRI_MAKE_STRING (LocaleCodeToLcidW (MPStringToStdW (qualis [QualifierType::Language]->ToString ())));
					values [resc] = MPStringToStdW (value ? value : System::String::Empty);
				}
				else if (qualis->ContainsKey (QualifierType::TargetSize))
				{
					if (qualis->ContainsKey (QualifierType::Contrast))
					{
						Contrast ct = Contrast::None;
						auto contstr = std::wnstring (MPStringToStdW (qualis [QualifierType::Contrast]->ToString ()->Trim ()->ToUpper ()));
						if (contstr.equals (L"WHITE")) ct = Contrast::White;
						else if (contstr.equals (L"BLACK")) ct = Contrast::Black;
						else if (contstr.equals (L"HIGH")) ct = Contrast::High;
						resc = PRI_MAKE_TARGETSIZE (Convert::ToUInt32 (qualis [QualifierType::TargetSize]), (DWORD)ct);
						values [resc] = MPStringToStdW (value ? value : System::String::Empty);
					}
					else
					{
						resc = PRI_MAKE_TARGETSIZE (Convert::ToUInt32 (qualis [QualifierType::TargetSize]), 0);
						values [resc] = MPStringToStdW (value ? value : System::String::Empty);
					}
				}
				else if (qualis->ContainsKey (QualifierType::Scale))
				{
					if (qualis->ContainsKey (QualifierType::Contrast))
					{
						Contrast ct = Contrast::None;
						auto contstr = std::wnstring (MPStringToStdW (qualis [QualifierType::Contrast]->ToString ()->Trim ()->ToUpper ()));
						if (contstr.equals (L"WHITE")) ct = Contrast::White;
						else if (contstr.equals (L"BLACK")) ct = Contrast::Black;
						else if (contstr.equals (L"HIGH")) ct = Contrast::High;
						resc = PRI_MAKE_SCALE (Convert::ToUInt32 (qualis [QualifierType::Scale]), (DWORD)ct);
						values [resc] = MPStringToStdW (value ? value : System::String::Empty);
					}
					else
					{
						resc = PRI_MAKE_SCALE (Convert::ToUInt32 (qualis [QualifierType::Scale]), 0);
						values [resc] = MPStringToStdW (value ? value : System::String::Empty);
					}
				}
				delete qualis;
				qualis = nullptr;
			}
			output [taskkey] = values;
			rnlist.erase (taskkey);
		}
		resourceMapSection = nullptr;
	}
}
HDWSPAIRLIST CreateDWSPAIRLISTFromMap (const std::map <DWORD, std::wstring> &input)
{
	DWORD count = (DWORD)input.size ();
	if (count == 0) return nullptr;
	size_t totalSize = sizeof (DWSPAIRLIST) + sizeof (DWORDWSTRPAIR) * (count - 1);
	HDWSPAIRLIST list = (HDWSPAIRLIST)malloc (totalSize);
	if (!list) return nullptr;
	list->dwLength = count;
	DWORD index = 0;
	for (auto &it : input)
	{
		list->lpArray [index].dwKey = it.first;
		list->lpArray [index].lpValue = _wcsdup (it.second.c_str ());
		index ++;
	}
	return list;
}
HDWSPAIRLIST CreateDWSPAIRLISTFromMap (const std::map <DWORD, std::wnstring> &input)
{
	DWORD count = (DWORD)input.size ();
	if (count == 0) return nullptr;
	size_t totalSize = sizeof (DWSPAIRLIST) + sizeof (DWORDWSTRPAIR) * (count - 1);
	HDWSPAIRLIST list = (HDWSPAIRLIST)malloc (totalSize);
	if (!list) return nullptr;
	list->dwLength = count;
	DWORD index = 0;
	for (auto &it : input)
	{
		list->lpArray [index].dwKey = it.first;
		list->lpArray [index].lpValue = _wcsdup (it.second.c_str ());
		index ++;
	}
	return list;
}
void DestroyPriResourceAllValueList (HDWSPAIRLIST list)
{
	if (!list) return;
	for (DWORD i = 0; i < list->dwLength; i++)
	{
		if (list->lpArray [i].lpValue)
		{
			free (list->lpArray [i].lpValue);  // 对应 _wcsdup
			list->lpArray [i].lpValue = NULL;
		}
	}
	free (list);
}
HDWSPAIRLIST GetPriResourceAllValueList (PCSPRIFILE pPriFile, LPCWSTR lpResName)
{
	if (!pPriFile || !lpResName) return nullptr;
	std::map <std::wnstring, std::map <DWORD, std::wnstring>> rnout;
	std::vector <std::wstring> rnl = {lpResName ? lpResName : L""};
	GetPriScaleAndTargetSizeFileList (pPriFile, rnl, rnout);
	for (auto &it : rnout) return CreateDWSPAIRLISTFromMap (it.second);
	return nullptr;
}
HWSDSPAIRLIST CreateWSDSPAIRLISTFromMap (const std::map <std::wnstring, std::map <DWORD, std::wnstring>> &input)
{
	DWORD count = (DWORD)input.size ();
	if (count == 0) return nullptr;
	size_t totalSize = sizeof (WSDSPAIRLIST) + sizeof (WSDSPAIR) * (count - 1);
	HWSDSPAIRLIST list = (HWSDSPAIRLIST)malloc (totalSize);
	if (!list) return nullptr;
	list->dwLength = count;
	DWORD index = 0;
	for (auto &it : input)
	{
		list->lpArray [index].lpKey = _wcsdup (it.first.c_str ());
		list->lpArray [index].lpValue = nullptr;
		if (!it.second.empty ())
		{
			list->lpArray [index].lpValue = CreateDWSPAIRLISTFromMap (it.second);
		}
		index ++;
	}
	return list;
}
void DestroyResourcesAllValuesList (HWSDSPAIRLIST list)
{
	if (!list) return;
	for (DWORD i = 0; i < list->dwLength; i++)
	{
		if (list->lpArray [i].lpKey)
		{
			free (list->lpArray [i].lpKey);
			list->lpArray [i].lpKey = nullptr;
		}
		if (list->lpArray [i].lpValue)
		{
			DestroyPriResourceAllValueList (list->lpArray [i].lpValue);
			list->lpArray [i].lpValue = nullptr;
		}
	}
	free (list);
}
HWSDSPAIRLIST GetPriResourcesAllValuesList (PCSPRIFILE pPriFile, const LPCWSTR *lpResNames, DWORD dwCount)
{
	if (!pPriFile || !lpResNames || dwCount == 0) return nullptr;
	std::map <std::wnstring, std::map <DWORD, std::wnstring>> rnout;
	std::vector<std::wstring> rnl;
	rnl.reserve (dwCount);
	for (DWORD i = 0; i < dwCount; i++)
	{
		if (lpResNames [i])
			rnl.emplace_back (lpResNames [i]);
	}
	if (rnl.empty ()) return nullptr;
	GetPriScaleAndTargetSizeFileList (pPriFile, rnl, rnout);
	if (rnout.empty ()) return nullptr;
	return CreateWSDSPAIRLISTFromMap (rnout);
}
