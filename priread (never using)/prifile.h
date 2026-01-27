#pragma once
#include <atlbase.h>
#include <string>
#include <vector>
#include <functional>
#include <type_traits>
#include <memory>
#include <stdexcept>
#include <map>
#include <thread>
#include <chrono>
#ifdef _CONSOLE
#include <iostream>
#include <iomanip>
#endif
#include "nstring.h"
#include "localeex.h"
#include "themeinfo.h"
// #define UNALIGN_MEMORY

#ifdef UNALIGN_MEMORY
#pragma pack(push, 1)
#endif
#ifdef min
#undef min
#endif
#ifdef max
#undef max
#endif
struct destruct
{
	std::function <void ()> endtask = nullptr;
	destruct (std::function <void ()> init): endtask (init) {}
	~destruct () { if (endtask) endtask (); }
};
template <typename T, typename BASE_INT> struct LargeIntBase
{
	BASE_INT val;
	LargeIntBase () { val.QuadPart = 0; }
	LargeIntBase (T v) { val.QuadPart = v; }
	LargeIntBase (const LargeIntBase &other) { val.QuadPart = other.val.QuadPart; }
	LargeIntBase (const BASE_INT &other) { val = other; }
	operator BASE_INT () const { return val; }
	operator T () const { return val.QuadPart; }
	explicit operator bool () const { return val.QuadPart != 0; }
	T *ptr_num () { return &val.QuadPart; }
	BASE_INT *ptr_union () { return &val; }
	size_t sizeof_num () const { return sizeof (val.QuadPart); }
	size_t sizeof_union () const { return sizeof (val); }
	T num () const { return val.QuadPart; }
	BASE_INT win_union () const { return val; }
	LargeIntBase operator + (const LargeIntBase &rhs) const { return LargeIntBase (val.QuadPart + rhs.val.QuadPart); }
	LargeIntBase operator - (const LargeIntBase &rhs) const { return LargeIntBase (val.QuadPart - rhs.val.QuadPart); }
	LargeIntBase operator * (const LargeIntBase &rhs) const { return LargeIntBase (val.QuadPart * rhs.val.QuadPart); }
	LargeIntBase operator / (const LargeIntBase &rhs) const { return LargeIntBase (val.QuadPart / rhs.val.QuadPart); }
	LargeIntBase operator % (const LargeIntBase &rhs) const { return LargeIntBase (val.QuadPart % rhs.val.QuadPart); }
	LargeIntBase &operator += (const LargeIntBase &rhs) { val.QuadPart += rhs.val.QuadPart; return *this; }
	LargeIntBase &operator -= (const LargeIntBase &rhs) { val.QuadPart -= rhs.val.QuadPart; return *this; }
	LargeIntBase &operator *= (const LargeIntBase &rhs) { val.QuadPart *= rhs.val.QuadPart; return *this; }
	LargeIntBase &operator /= (const LargeIntBase &rhs) { val.QuadPart /= rhs.val.QuadPart; return *this; }
	LargeIntBase &operator %= (const LargeIntBase &rhs) { val.QuadPart %= rhs.val.QuadPart; return *this; }
	LargeIntBase operator & (const LargeIntBase &rhs) const { return LargeIntBase (val.QuadPart & rhs.val.QuadPart); }
	LargeIntBase operator | (const LargeIntBase &rhs) const { return LargeIntBase (val.QuadPart | rhs.val.QuadPart); }
	LargeIntBase operator ^ (const LargeIntBase &rhs) const { return LargeIntBase (val.QuadPart ^ rhs.val.QuadPart); }
	LargeIntBase operator << (int n) const { return LargeIntBase (val.QuadPart << n); }
	LargeIntBase operator >> (int n) const { return LargeIntBase (val.QuadPart >> n); }
	LargeIntBase &operator &= (const LargeIntBase &rhs) { val.QuadPart &= rhs.val.QuadPart; return *this; }
	LargeIntBase &operator |= (const LargeIntBase &rhs) { val.QuadPart |= rhs.val.QuadPart; return *this; }
	LargeIntBase &operator ^= (const LargeIntBase &rhs) { val.QuadPart ^= rhs.val.QuadPart; return *this; }
	LargeIntBase &operator <<= (int n) { val.QuadPart <<= n; return *this; }
	LargeIntBase &operator >>= (int n) { val.QuadPart >>= n; return *this; }
	LargeIntBase &operator ++ () { ++val.QuadPart; return *this; }
	LargeIntBase operator ++ (int) { LargeIntBase tmp (*this); ++val.QuadPart; return tmp; }
	LargeIntBase &operator -- () { --val.QuadPart; return *this; }
	LargeIntBase operator -- (int) { LargeIntBase tmp (*this); --val.QuadPart; return tmp; }
	bool operator <  (const LargeIntBase &rhs) const { return val.QuadPart <  rhs.val.QuadPart; }
	bool operator >  (const LargeIntBase &rhs) const { return val.QuadPart >  rhs.val.QuadPart; }
	bool operator <= (const LargeIntBase &rhs) const { return val.QuadPart <= rhs.val.QuadPart; }
	bool operator >= (const LargeIntBase &rhs) const { return val.QuadPart >= rhs.val.QuadPart; }
	bool operator == (const LargeIntBase &rhs) const { return val.QuadPart == rhs.val.QuadPart; }
	bool operator != (const LargeIntBase &rhs) const { return val.QuadPart != rhs.val.QuadPart; }
	bool operator ! () const { return !val.QuadPart; }
	LargeIntBase operator ~ () const { return LargeIntBase (~val.QuadPart); }
};
typedef LargeIntBase <LONGLONG, LARGE_INTEGER> LargeInt, lint;
typedef LargeIntBase <ULONGLONG, ULARGE_INTEGER> ULargeInt, ulint;
template <typename ct, typename tr = std::char_traits <ct>, typename al = std::allocator <ct>> class basic_priid: public std::basic_nstring <ct, tr, al>
{
	using base = std::basic_nstring <ct, tr, al>;
	public:
	using typename base::size_type;
	using typename base::value_type;
	using base::base;
	basic_priid (const ct *buf, size_t sz = 16): base (buf, sz) {}
	template <std::size_t N> basic_priid (const ct (&arr) [N]) : base (arr, N) {}
};
typedef basic_priid <CHAR> pri_sectid;
std::string ReadStringEndwithNullA (IStream *pStream)
{
	if (!pStream) return "";
	std::string result;
	char ch = 0;
	ULONG cbRead = 0;
	while (true)
	{
		HRESULT hr = pStream->Read (&ch, 1, &cbRead);
		if (FAILED (hr) || cbRead == 0) break; 
		if (ch == '\0') break;
		result.push_back (ch);
	}
	return result;
}
std::wstring ReadStringEndwithNullW (IStream *pStream)
{
	if (!pStream) return L"";
	std::wstring result;
	WCHAR ch = 0;
	ULONG cbRead = 0;
	while (true)
	{
		HRESULT hr = pStream->Read (&ch, sizeof (WCHAR), &cbRead);
		if (FAILED (hr) || cbRead < sizeof (WCHAR)) break;
		if (ch == L'\0') break; 
		result.push_back (ch);
	}
	return result;
}
std::string ReadStringEndwithNull (IStream *ifile, std::string &ret)
{
	return ret = ReadStringEndwithNullA (ifile);
}
std::wstring ReadStringEndwithNull (IStream *ifile, std::wstring &ret)
{
	return ret = ReadStringEndwithNullW (ifile);
}
std::string ReadStringA (IStream *pStream, size_t length)
{
	if (!pStream || length <= 0) return "";
	std::string result;
	result.resize (length);
	ULONG cbRead = 0;
	HRESULT hr = pStream->Read (&result [0], length, &cbRead);
	if (FAILED (hr) || cbRead == 0)
	{
		result.clear ();
		return result;
	}
	if (cbRead < (ULONG)length) result.resize (cbRead);
	return result;
}
std::wstring ReadStringW (IStream *pStream, size_t length)
{
	if (!pStream || length <= 0) return L"";
	std::wstring result;
	result.resize (length);
	ULONG cbRead = 0;
	HRESULT hr = pStream->Read (&result [0], length * sizeof (WCHAR), &cbRead);
	if (FAILED (hr) || cbRead == 0)
	{
		result.clear ();
		return result;
	}
	if (cbRead < (ULONG)(length * sizeof (WCHAR))) result.resize (cbRead / sizeof (WCHAR));
	return result;
}
enum class seekpos: DWORD
{
	start = STREAM_SEEK_SET,
	current = STREAM_SEEK_CUR,
	end = STREAM_SEEK_END
};
class bytesstream
{
	std::vector <BYTE> bytes;
	size_t pos = 0;
	public:
	using seekpos = ::seekpos;
	// 文件长度/绝对偏移位置。（都是 0 起始）
	using fsize_t = uint64_t;
	// 文件指针相对移动长度，正数向右，负数向左
	using fmove_t = int64_t;
	bool read (void *p, size_t size, size_t *ret = nullptr)
	{
		if (!p || size == 0) return false;
		size_t available = 0;
		if (pos < bytes.size ()) available = bytes.size () - pos;
		size_t readSize = (available >= size) ? size : available;
		if (readSize > 0) memcpy_s (p, size, bytes.data () + pos, readSize);
		else memset (p, 0, size); 
		pos += size; 
		if (ret) *ret = readSize;
		return true;
	}
	template <typename T, typename = typename std::enable_if <!std::is_pointer <T>::value>::type> size_t read (T &var)
	{
		size_t ret = 0;
		read (&var, sizeof (var), &ret);
		return ret;
	}
	template <typename T, typename = typename std::enable_if <!std::is_pointer <T>::value>::type> friend bytesstream &operator >> (bytesstream &s, T &variable)
	{
		s.read (&variable, sizeof (variable));
		return s;
	}
	friend bytesstream &operator >> (bytesstream &in, std::string &ret)
	{
		in.read_string_endwith_null (ret);
		return in;
	}
	friend bytesstream &operator >> (bytesstream &in, std::wstring &ret)
	{
		in.read_string_endwith_null (ret);
		return in;
	}
	bytesstream (const std::vector <BYTE> &vec): bytes (vec) {}
	bytesstream (const BYTE *ptr, size_t len = 0): bytes (len, *ptr) {}
	bytesstream () = default;
	auto &data () { return bytes; }
	void set (const std::vector <BYTE> &bver)
	{
		bytes = bver;
	}
	auto position ()
	{ 
		if (pos > bytes.size ()) pos = bytes.size ();
		return pos;
	}
	auto position (size_t v) { pos = v; if (pos > bytes.size ()) pos = bytes.size (); return pos; }
	auto length () const { return bytes.size (); }
	auto size () const { return length (); }
	std::string read_string_endwith_null_a ();
	std::wstring read_string_endwith_null_w ();
	size_t read_string_endwith_null (std::string &ret);
	size_t read_string_endwith_null (std::wstring &ret);
	std::string read_string_a (size_t length);
	std::wstring read_string_w (size_t length);
	size_t read_string (size_t length, std::wstring &ret);
	size_t read_string (size_t length, std::string &ret);
	HRESULT seek (fmove_t movelen = 0, seekpos origin = seekpos::current, fsize_t *newpos = nullptr)
	{
		if (bytes.empty ()) return E_INVALIDARG;
		int64_t newPosition = 0;
		switch (origin)
		{
			case seekpos::start:
				newPosition = movelen;
				break;
			case seekpos::current:
				newPosition = static_cast <int64_t> (pos) + movelen;
				break;
			case seekpos::end:
				newPosition = static_cast <int64_t> (bytes.size ()) + movelen;
				break;
			default: return E_INVALIDARG;
		}
		if (newPosition < 0) newPosition = 0;
		pos = static_cast <size_t> (newPosition);
		if (newpos) *newpos = static_cast <fsize_t> (pos);
		return S_OK;
	}
	auto &buffer () const { return bytes; }
	auto &buffer () { return bytes; }
	size_t remain () const 
	{ 
		if (pos > size ()) return 0;
		return size () - pos;
	}
	void clear ()
	{
		bytes.clear ();
		pos = 0;
	}
	void resize (size_t len) { bytes.resize (len); }
	const BYTE *ptr () const { return bytes.data (); }
	BYTE *ptr () { return bytes.data (); }
	template <typename T, typename = typename std::enable_if <!std::is_pointer <T>::value>::type>
	T read_bytes (size_t byteslen = sizeof (T), size_t *returnread = nullptr)
	{
		std::vector <BYTE> bytesbuf (byteslen);
		size_t rl = 0;
		read (bytesbuf.data (), byteslen, &rl);
		if (returnread) *returnread = rl;
		bytesbuf.resize (!rl ? sizeof (T) : rl);
		return *(T *)bytesbuf.data ();
	}
};
std::string ReadStringEndwithNullA (bytesstream &stream)
{
	std::string result;
	char ch = 0;
	size_t cbRead = 0;
	while (true)
	{
		if (!stream.read (&ch, 1, &cbRead) || cbRead == 0) break;
		if (ch == '\0') break;
		result.push_back (ch);
	}
	return result;
}
std::wstring ReadStringEndwithNullW (bytesstream &stream)
{
	std::wstring result;
	WCHAR ch = 0;
	size_t cbRead = 0;
	while (true)
	{
		if (!stream.read (&ch, sizeof (WCHAR), &cbRead) || cbRead < sizeof (WCHAR)) break;
		if (ch == L'\0') break;
		result.push_back (ch);
	}
	return result;
}
std::string ReadStringEndwithNull (bytesstream &stream, std::string &ret)
{
	return ret = ReadStringEndwithNullA (stream);
}
std::wstring ReadStringEndwithNull (bytesstream &stream, std::wstring &ret)
{
	return ret = ReadStringEndwithNullW (stream);
}
std::string ReadStringA (bytesstream &stream, size_t length)
{
	if (length == 0) return "";
	std::string result (length, '\0');
	size_t cbRead = 0;
	if (!stream.read (&result [0], length, &cbRead) || cbRead == 0)
	{
		result.clear ();
		return result;
	}
	if (cbRead < length) result.resize (cbRead);
	return result;
}
std::wstring ReadStringW (bytesstream &stream, size_t length)
{
	if (length == 0) return L"";
	std::wstring result (length, L'\0');
	size_t cbRead = 0;
	if (!stream.read (&result [0], length * sizeof (WCHAR), &cbRead) || cbRead == 0)
	{
		result.clear ();
		return result;
	}
	if (cbRead < length * sizeof (WCHAR)) result.resize (cbRead / sizeof (WCHAR));
	return result;
}
std::string bytesstream::read_string_endwith_null_a () { return ReadStringEndwithNullA (*this); }
std::wstring bytesstream::read_string_endwith_null_w () { return ReadStringEndwithNullW (*this); }
size_t bytesstream::read_string_endwith_null (std::string &ret)
{
	return (ret = read_string_endwith_null_a ()).length ();
}
size_t bytesstream::read_string_endwith_null (std::wstring &ret)
{
	return (ret = read_string_endwith_null_w ()).length ();
}
std::string bytesstream::read_string_a (size_t length) { return ReadStringA (*this, length); }
std::wstring bytesstream::read_string_w (size_t length) { return ReadStringW (*this, length); }
size_t bytesstream::read_string (size_t length, std::wstring &ret) { return (ret = read_string_w (length)).length (); }
size_t bytesstream::read_string (size_t length, std::string &ret) { return (ret = read_string_a (length)).length (); }
// 注：销毁时不会释放指针。仅是为一种操作类
class istreamstream
{
	private:
	// COM 接口：流指针
	IStream *comStream = nullptr;
	// 缓存，最长 210 字节
	bytesstream bsCache;
	// 将缓存视为窗口，这里记录缓存相对于文件的位置。
	uint64_t ulWndOffset = 0;
	static const size_t MAX_CACHE_SIZE = 210;
	public:
	// 文件长度/绝对偏移位置。（都是 0 起始）
	using fsize_t = uint64_t;
	// 文件指针相对移动长度，正数向右，负数向左
	using fmove_t = int64_t;
	using seekpos = ::seekpos;
	istreamstream (IStream *iptr): comStream (iptr)
	{ 
		// bsCache.buffer ().reserve (MAX_CACHE_SIZE);
	}
	auto &buffer () const { return bsCache; }
	auto &buffer () { return bsCache; }
	void set_stream (IStream *iptr) { comStream = iptr; }
	IStream *get_stream () { return comStream; }
	// read 处理机制
	// 构建缓存：会读取最长 210 字节的缓存
	// 读取缓存：在缓存中读取字节，流指针与字节指针一起移动。字节指针经过计算一定与流指针相等。
	HRESULT read (void *buf, size_t size, size_t *readbytes = nullptr)
	{
		if (!comStream || !buf) return E_INVALIDARG;
		if (position () < ulWndOffset || position () >= ulWndOffset + bsCache.size () || position () != ulWndOffset + position ()) bsCache.buffer ().clear ();
		if (size > MAX_CACHE_SIZE)
		{
			bsCache.clear ();
			ULONG cbread = 0;
			HRESULT hr = comStream->Read (buf, size, &cbread);
			if (readbytes) *readbytes = cbread;
			return hr;
		}
		else if (bsCache.remain () < size)
		{
			bsCache.clear ();
			bsCache.resize (MAX_CACHE_SIZE);
			auto nowpos = position ();
			ULONG cbread = 0;
			HRESULT hr = comStream->Read (bsCache.ptr (), MAX_CACHE_SIZE, &cbread);
			comStream->Seek (lint (nowpos), STREAM_SEEK_SET, nullptr);
			ulWndOffset = nowpos;
			bsCache.resize (cbread);
			size_t bufread = 0;
			bool res = bsCache.read (buf, size, &bufread);
			if (readbytes) *readbytes = bufread;
			comStream->Seek (lint (bufread), STREAM_SEEK_CUR, nullptr);
			if (res) return S_OK;
			else return FAILED (hr) ? hr : E_FAIL;
		}
		else
		{
			size_t bufread = 0;
			bool res = bsCache.read (buf, size, &bufread);
			if (readbytes) *readbytes = bufread;
			comStream->Seek (lint (bufread), STREAM_SEEK_CUR, nullptr);
			return res ? S_OK : E_FAIL;
		}
	}
	// 使用场景有限，且不支持传入指针。
	template <typename T, typename = typename std::enable_if <!std::is_pointer <T>::value>::type>
	HRESULT read (T &variable, size_t *readbytes = nullptr)
	{
		return read (&variable, sizeof (variable), readbytes);
	}
	std::string read_string_endwith_null_a () { return ReadStringEndwithNullA (comStream); }
	std::wstring read_string_endwith_null_w () { return ReadStringEndwithNullW (comStream); }
	size_t read_string_endwith_null (std::string &ret)
	{
		return (ret = read_string_endwith_null_a ()).length ();
	}
	size_t read_string_endwith_null (std::wstring &ret)
	{
		return (ret = read_string_endwith_null_w ()).length ();
	}
	std::string read_string_a (size_t length) { return ReadStringA (comStream, length); }
	std::wstring read_string_w (size_t length) { return ReadStringW (comStream, length); }
	size_t read_string (size_t length, std::wstring &ret) { return (ret = read_string_w (length)).length (); }
	size_t read_string (size_t length, std::string &ret) { return (ret = read_string_a (length)).length (); }
	HRESULT write (const void *bytes, size_t size, size_t *writebytes = nullptr)
	{
		if (!comStream) return E_INVALIDARG;
		ULONG retsize = 0;
		HRESULT hr = comStream->Write (bytes, size, &retsize);
		if (writebytes) *writebytes = retsize;
		return hr;
	}
	// 使用场景有限，且不支持传入指针。
	template <typename T, typename = typename std::enable_if <!std::is_pointer <T>::value>::type>
	HRESULT write (const T &variable, size_t *writebytes = nullptr)
	{
		return write (&variable, sizeof (variable), writebytes);
	}
	HRESULT seek (fmove_t movelen = 0, seekpos origin = seekpos::current, fsize_t *newpos = nullptr)
	{
		if (!comStream) return E_INVALIDARG;
		ulint ret = 0;
		HRESULT hr = comStream->Seek (lint (movelen), (DWORD)origin, ret.ptr_union ());
		if ((movelen != 0 || origin != seekpos::current) && bsCache.size ()) bsCache.clear ();
		if (newpos) *newpos = ret;
		return hr;
	}
	fsize_t position ()
	{
		fsize_t pos = 0;
		seek (0, seekpos::current, &pos);
		return pos;
	}
	HRESULT stat (STATSTG *data, DWORD statflags)
	{
		if (!comStream) return E_INVALIDARG;
		return comStream->Stat (data, statflags);
	}
	HRESULT size (fsize_t newsize)
	{
		if (!comStream) return E_INVALIDARG;
		return comStream->SetSize (ulint (newsize));
	}
	fsize_t size ()
	{
		STATSTG stg = {0};
		if (SUCCEEDED (stat (&stg, STATFLAG_NONAME))) return stg.cbSize.QuadPart;
		else return 0;
	}
	HRESULT copy_to (istreamstream &pstm, fsize_t cb, fsize_t *pcbRead = nullptr, fsize_t *pcbWritten = nullptr)
	{
		if (!comStream) return E_INVALIDARG;
		ulint r = 0, w = 0;
		HRESULT hr = comStream->CopyTo (pstm.comStream, ulint (cb), r.ptr_union (), w.ptr_union ());
		if (pcbRead) *pcbRead = r;
		if (pcbWritten) *pcbWritten = w;
		return hr;
	}
	HRESULT commit (DWORD grfCommitFlags)
	{
		if (!comStream) return E_INVALIDARG;
		return comStream->Commit (grfCommitFlags);
	}
	HRESULT revert ()
	{
		if (!comStream) return E_INVALIDARG;
		return comStream->Revert ();
	}
	HRESULT lock_region (fsize_t libOffset, fsize_t cb, DWORD dwLockType)
	{
		if (!comStream) return E_INVALIDARG;
		return comStream->LockRegion (ulint (libOffset), ulint (cb), dwLockType);
	}
	HRESULT unlock_region (fsize_t libOffset, fsize_t cb, DWORD dwLockType)
	{
		if (!comStream) return E_INVALIDARG;
		return comStream->UnlockRegion (ulint (libOffset), ulint (cb), dwLockType);
	}
	HRESULT clone (istreamstream &anotherptr)
	{
		if (!comStream) return E_INVALIDARG;
		return comStream->Clone (&anotherptr.comStream);
	}
	template <typename T, typename = typename std::enable_if <!std::is_pointer <T>::value>::type>
	friend istreamstream &operator >> (istreamstream &in, T &variable)
	{
		in.read (variable);
		return in;
	}
	template <typename T, typename = typename std::enable_if <!std::is_pointer <T>::value>::type>
	friend istreamstream &operator << (istreamstream &out, const T &variable)
	{
		out.write (variable);
		return out;
	}
	friend istreamstream &operator >> (istreamstream &in, std::string &ret)
	{
		in.read_string_endwith_null (ret);
		return in;
	}
	friend istreamstream &operator >> (istreamstream &in, std::wstring &ret)
	{
		in.read_string_endwith_null (ret);
		return in;
	}
	operator IStream * () { return comStream; }
	IStream *operator -> () { return comStream; }
	template <typename T, typename = typename std::enable_if <!std::is_pointer <T>::value>::type>
	// 读取一个值，并判断是否符合预期。throwerror 为真时，如果值不符合预期则通过抛出异常来判断。否则只通过返回值来判断。
	bool expect (const T &expect_value, bool throwerror = true, T *retvalue = nullptr)
	{
		T value;
		this->read (value);
		if (retvalue) *retvalue = value;
		bool res = value == expect_value;
		if (!res && throwerror) throw std::exception ("Unexpected value read.");
		return res;
	}
	template <typename T, typename = typename std::enable_if <!std::is_pointer <T>::value>::type>
	T read_bytes (size_t byteslen = sizeof (T), size_t *returnread = nullptr)
	{
		std::vector <BYTE> bytesbuf (byteslen);
		size_t rl = 0;
		read (bytesbuf.data (), byteslen, &rl);
		if (returnread) *returnread = rl;
		bytesbuf.resize (!rl ? sizeof (T) : rl);
		return *(T *)bytesbuf.data ();
	}
};

//											 0         1
//											 012345678901234
#define PRI_SECT_ID_PRI_DESCRIPTOR			"[mrm_pridescex]"
#define PRI_SECT_ID_HIERARCHICAL_SCHEMA		"[mrm_hschema]  "
#define PRI_SECT_ID_HIERARCHICAL_SCHEMAEX	"[mrm_hschemaex]"
#define PRI_SECT_ID_DECISION_INFO			"[mrm_decn_info]"
#define PRI_SECT_ID_RESOURCE_MAP			"[mrm_res_map__]"
#define PRI_SECT_ID_RESOURCE_MAP2			"[mrm_res_map2_]"
#define PRI_SECT_ID_DATA_ITEM				"[mrm_dataitem] "
#define PRI_SECT_ID_REVERSE_MAP				"[mrm_rev_map]  "
#define PRI_SECT_ID_REFERENCED_FILE			"[def_file_list]"
enum class SectionType
{
	Unknown,              // 未知类型 - 处理未识别的节区
	PriDescriptor,        // PRI 描述符 - 包含文件整体结构和引用信息
	HierarchicalSchema,	  // 层次结构模式 - 定义资源命名空间和层次结构
	HierarchicalSchemaEx, // 层次结构模式 - 定义资源命名空间和层次结构
	DecisionInfo,         // 决策信息 - 管理资源限定符和决策逻辑
	ResourceMap,          // 资源映射 - 将资源项映射到具体候选值
	ResourceMap2,         // 资源映射 - 将资源项映射到具体候选值
	DataItem,             // 数据项 - 存储实际的资源数据
	ReverseMap,           // 反向映射 - 提供从资源名到ID的映射
	ReferencedFile        // 引用文件 - 管理引用的外部文件
};
std::wstring EnumToStringW (SectionType value)
{
	switch (value)
	{
		case SectionType::PriDescriptor: return L"PriDescriptor"; break;
		case SectionType::HierarchicalSchema: return L"HierarchicalSchema"; break;
		case SectionType::HierarchicalSchemaEx: return L"HierarchicalSchemaEx"; break;
		case SectionType::DecisionInfo: return L"DecisionInfo"; break;
		case SectionType::ResourceMap: return L"ResourceMap"; break;
		case SectionType::ResourceMap2: return L"ResourceMap2"; break;
		case SectionType::DataItem: return L"DataItem"; break;
		case SectionType::ReverseMap: return L"ReverseMap"; break;
		case SectionType::ReferencedFile: return L"ReferencedFile"; break;
		default:
		case SectionType::Unknown: return L"Unknown"; break;
	}
}

struct head
{
	// 版本标识符 / version identifier
	// “mrm_pri0”：客户端 6.2.1（Windows 8）。
	// “mrm_pri1”：客户端 6.3.0（Windows 8.1）。
	// “mrm_prif”：Windows Phone 6.3.1。
	// “mrm_pri2”：Universal 10.0.0（Windows 10）。
	CHAR szMagic [8] = {};		
	WORD wPlaceholder1 = -1,	// 未知，0 / unknown, zero
		wPlaceholder2 = 0;		// 未知，1 / unknown, one
	DWORD dwFileSize = 0,		// 文件总大小 / total file size
		dwToCOffset = 0,		// 目录表偏移 / offset of table of contents
		dwSectStartOffset = 0;	// 第一节的偏移 / offset of first section
	WORD wSectCount = 0,		// 节数量 / number of sections
		wPlaceholder3 = 0;		// 未知，0xFFFF / unknown, 0xFFFF
	DWORD dwPlaceholder4 = -1;	// 未知，0 / unknown, zero
	bool valid ()
	{
		CHAR m7 = szMagic [7];
		destruct endt ([this, m7] () {
			if (m7) szMagic [7] = m7;
		});
		szMagic [7] = '\0';
		if (_stricmp (szMagic, "mrm_pri")) return false;
		switch (m7)
		{
			case '0': case '1': case '2': case 'f': case 'F': break;
			default: return false;
		}
		if (wPlaceholder1 != 0) return false;
		if (wPlaceholder2 != 1) return false;
		if (wPlaceholder3 != 0xFFFF) return false;
		if (dwPlaceholder4 != 0) return false;
		return true;
	}
};
struct foot
{
	DWORD dwChkCode = 0;		// 0xDEFFFADE
	DWORD dwTotalFileSize = 0;	// total file size, as in header
	CHAR szMagic [8] = {0};		// version identifier, as in header
	bool valid (const head &fh)
	{
		if (dwChkCode != 0xDEFFFADE) return false;
		if (dwTotalFileSize != fh.dwFileSize) return false;
		for (int i = 0; i < 8; i ++)
		{
			if (szMagic [i] != fh.szMagic [i] && tolower (szMagic [i]) != tolower (fh.szMagic [i])) return false;
		}
		return true;
	}
};
struct tocentry
{
	CHAR szIdentifier [16] = {0};	// 节标识符 / section identifier
	WORD wFlags = 0;				// 标志 / flags
	WORD wSectFlags = 0;			// 节标志 / section flags
	DWORD dwSectQualifier = 0;		// 节限定符 / section qualifier
	DWORD dwSectOffset = 0;			// 节偏移（相对于第一节偏移） / section offset (relative to offset of first section)
	DWORD dwSectLength = 0;			// 节长度 / section length
};
struct section_header
{
	CHAR szIdentifier [16] = {0};	// 节标识符 / section identifier
	DWORD dwQualifier = 0;			// 节限定符 / section qualifier
	WORD wFlags = 0;				// 标志 / flags
	WORD wSectFlags = 0;			// 节标志 / section flags
	DWORD dwLength = 0;				// 节长度 / section length
	DWORD dwPlaceholder1 = -1;		// 未知，0 / unknown, zero
	bool valid () const { return szIdentifier [0] && !dwPlaceholder1; }
};
struct section_check
{
	DWORD dwChkCode = 0;	// 魔数 0xF5DEDEFA
	DWORD dwSectLength = 0;	// 节长度（与节头中的相同） / section length, as in section header
	bool valid (const section_header &h) const { return dwChkCode == 0xDEF5FADE && dwSectLength == h.dwLength; }
};
struct substream
{
	IStream *&ifile;
	ULONGLONG offset = 0;
	ULONGLONG size = 0;
	substream (IStream *&ifile, ulint ofs = 0, ulint siz = 0): ifile (ifile), offset (ofs), size (siz) {}
	void set (ulint p_offset, ulint p_size)
	{
		offset = p_offset;
		size = p_size;
	}
	HRESULT seek ()
	{
		istreamstream iss (ifile);
		return iss.seek (offset, istreamstream::seekpos::start);
	}
};
struct prifile;
struct section
{
	section_header head;
	section_check foot;
	substream childst;
	// 请从 type 方法获取类型，而不是直接通过 sect_type 来读取（因为未初始化）
	SectionType sect_type = SectionType::Unknown;
	section (IStream *&ifile, prifile &prif): childst (ifile), pri_file (prif) {}
	bool valid () const { return head.valid () && foot.valid (head); }
	SectionType type ()
	{
		if (sect_type == SectionType::Unknown)
		{
			pri_sectid pid (head.szIdentifier, 16);
			if (pid.equals (PRI_SECT_ID_PRI_DESCRIPTOR)) sect_type = SectionType::PriDescriptor;
			else if (pid.equals (PRI_SECT_ID_HIERARCHICAL_SCHEMA)) sect_type = SectionType::HierarchicalSchema;
			else if (pid.equals (PRI_SECT_ID_HIERARCHICAL_SCHEMAEX)) sect_type = SectionType::HierarchicalSchemaEx;
			else if (pid.equals (PRI_SECT_ID_DECISION_INFO)) sect_type = SectionType::DecisionInfo;
			else if (pid.equals (PRI_SECT_ID_RESOURCE_MAP)) sect_type = SectionType::ResourceMap;
			else if (pid.equals (PRI_SECT_ID_RESOURCE_MAP2)) sect_type = SectionType::ResourceMap2;
			else if (pid.equals (PRI_SECT_ID_DATA_ITEM)) sect_type = SectionType::DataItem;
			else if (pid.equals (PRI_SECT_ID_REVERSE_MAP)) sect_type = SectionType::ReverseMap;
			else if (pid.equals (PRI_SECT_ID_REFERENCED_FILE)) sect_type = SectionType::ReferencedFile;
			else sect_type = SectionType::Unknown;
		}
		return sect_type;
	}
	size_t length () const { return head.dwLength; }
	prifile &pri_file;
#ifdef _CONSOLE
	friend std::wostream &operator << (std::wostream &o, section &s)
	{
		return o << L"Section " << EnumToStringW (s.type ()) << L" Length " << s.head.dwLength;
	}
#endif
};
namespace pri
{
	struct sect_pridesp; // PriDescriptor	PRI 描述符 - 包含文件整体结构和引用信息
	struct sect_hierasche; // HierarchicalSchema & HierarchicalSchemaEx	层次结构模式 - 定义资源命名空间和层次结构
	struct sect_decinfo; // DecisionInfo	决策信息 - 管理资源限定符和决策逻辑
	struct sect_resmap; // ResourceMap & ResourceMap2	资源映射 - 将资源项映射到具体候选值
	struct sect_dataitem; // DataItem	数据项 - 存储实际的资源数据
	struct sect_revmap; // ReverseMap	反向映射 - 提供从资源名到ID的映射
	struct sect_reffile; // ReferencedFile	引用文件 - 管理引用的外部文件
	struct sect_unknown; // Unknown	未知类型 - 处理未识别的节区

	// PriDescriptorFlags

	enum class PRI_SEC_DESP: DWORD
	{
		INVALID = 0,
		AUTOMERGE = 0x1, // AutoMerge
		DEPLOY_MERGEABLE = 0x2, // IsDeploymentMergeable
		DEPLOY_MERGE_RESULT = 0x4, // IsDeploymentMergeResult
		AUTO_MERGE_RESULT = 0x8 // IsAutomergeMergeResult
	};

	// HierarchicalSchema & HierarchicalSchemaEx

	typedef struct _HSCHEMA_VERSION_INFO
	{
		WORD wMajor = 0;		// 主版本号 / major version
		WORD wMinor = 0;		// 次版本号 / minor version
		DWORD dwUnknown1 = -1;	// 未知，0
		// 校验和（checksum）
		// checksum: a CRC32-based checksum computed on the unique name, 
		// the name, the section indices of the Resource Map Section 
		// and Data Item Section, and the names of all scopes and items
		DWORD dwCheckSum = 0;	
		DWORD dwScopeCount = 0;	// scope 数量
		DWORD dwItemCount = 0;	// item 数量
		bool empty ()
		{
			return dwUnknown1 != 0;
		}
	} HSCHEMA_VERSION_INFO;
	// 资源名称（scope/item）按以下格式存储（示例字段）：parent scope index、full path 长度、
	// 名字首字母（大写）、名字长度、名字偏移、index property 等。

	typedef struct _SCOPE_ITEM_INFO
	{
		WORD wParentScopeIndex = -1;	// parent scope index
		WORD wFullPathLength = 0;		// length of full path
		WCHAR wchUpperFirst = L'\0';	// uppercase first character of name, '\0' if name is empty
		// length of name in characters, null-terminator excluded, 0 if the length is bigger than 255
		// 在原作者的 C# 代码中是这么读取的：uint nameOffset = binaryReader.ReadUInt16() | (uint)((flags & 0xF) << 16);
		// 所以不能直接使用成员 bNameLength。请使用结构体中 name_offset () 方法来获取值
		BYTE bNameLength = 0;			
		BYTE bFlags = 0;				// flags and upper bits of name offset
		WORD wNameOffset = 0;			// offset of name in ASCII or Unicode name block in characters
		// index property
		// bits 0-3: upper bits 16-19 of name offset
		// bit 4: set if resource name is a scope, unset if it is an item
		// bit 5 : set if name is stored in the ASCII name block, unset if 
		// it is stored in the Unicode name block
		WORD wIndexProp = 0;
		bool is_scope () const { return bFlags & 0x10; }
		bool name_in_ascii () const { return bFlags & 0x20; }
		DWORD name_offset () const { return (DWORD)wNameOffset | ((DWORD)(bFlags & 0xF) << 16); }
		DWORD index () const { return wIndexProp; }
		//friend istreamstream &operator >> (istreamstream &i, _SCOPE_ITEM_INFO &s)
		//{
		//	i >> s.wParentScopeIndex >>
		//		s.wFullPathLength;
		//	s.wchUpperFirst = i.read_bytes <WCHAR> (sizeof (UINT16));
		//	s.bNameLength = i.read_bytes <BYTE> ();
		//	s.bFlags = i.read_bytes <BYTE> ();
		//	s.wNameOffset = i.read_bytes <UINT16> ();
		//	s.wIndexProp = i.read_bytes <UINT16> ();
		//	return i;
		//}
		//friend bytesstream &operator >> (bytesstream &i, _SCOPE_ITEM_INFO &s)
		//{
		//	i >> s.wParentScopeIndex >>
		//		s.wFullPathLength;
		//	s.wchUpperFirst = i.read_bytes <WCHAR> (sizeof (UINT16));
		//	s.bNameLength = i.read_bytes <BYTE> ();
		//	s.bFlags = i.read_bytes <BYTE> ();
		//	s.wNameOffset = i.read_bytes <UINT16> ();
		//	s.wIndexProp = i.read_bytes <UINT16> ();
		//	return i;
		//}
	} SCOPE_ITEM_INFO;
	typedef struct _SCOPE_EX_INFO
	{
		WORD wScopeIndex = 0;	// scope index
		WORD wChildCount = 0;	// child count
		WORD wFirstChild = 0;	// scope or item index of first child, all other children follow sequentially
		WORD wUnknown1 = 0;		// unknown, zero
		//friend istreamstream &operator >> (istreamstream &i, _SCOPE_EX_INFO &e)
		//{
		//	i >> e.wScopeIndex >>
		//		e.wChildCount >>
		//		e.wFirstChild;
		//	i.expect <UINT16> (0, true, &e.wUnknown1);
		//	return i;
		//}
		bool valid () const { return !wUnknown1; }
	} SCOPE_EX_INFO;
	typedef WORD ITEM_INDEX;
	enum class RES_MAP_OBJTYPE
	{
		ENTRY = 0,
		SCOPE = 1,
		ITEM = 2
	};
	class RES_MAP_SCOPE;
	class RES_MAP_ENTRY
	{
		public:
		ITEM_INDEX wIndex = 0;
		std::wstring strName;
		RES_MAP_SCOPE *pParent = nullptr;
		RES_MAP_ENTRY (ITEM_INDEX index = 0, RES_MAP_SCOPE *parent = {}, const std::wstring &name = L"", RES_MAP_OBJTYPE type = RES_MAP_OBJTYPE::ENTRY, bool setnull = true):
			wIndex (index), strName (name), pParent (parent), eType (type), bIsNull (setnull) {}
		std::wstring full_name (WCHAR divide = L'\\');
		size_t path (std::vector <std::wstring> &output);
		void refresh_full_name () { strFullName.clear (); }
		RES_MAP_OBJTYPE type () const { return eType; }
		bool bIsNull = false;
		protected:
		std::wstring strFullName; 
		RES_MAP_OBJTYPE eType;
	};
	class RES_MAP_SCOPE: public RES_MAP_ENTRY
	{
		public:
		explicit RES_MAP_SCOPE (ITEM_INDEX index = 0, RES_MAP_SCOPE *parent = nullptr, const std::wstring &name = L"", bool setnull = true):
			RES_MAP_ENTRY (index, parent, name, RES_MAP_OBJTYPE::SCOPE, setnull) {}
		std::vector <RES_MAP_ENTRY *> vecChild;
	};
	std::wstring RES_MAP_ENTRY::full_name (WCHAR divide)
	{
		if (strFullName.empty ())
		{
			if (pParent) strFullName = pParent->full_name (divide) + divide + strName;
			else strFullName = strName;
		}
		return strFullName;
	}
	size_t RES_MAP_ENTRY::path (std::vector <std::wstring> &output)
	{
		output.clear ();
		if (pParent) pParent->path (output);
		output.push_back (strName);
		return output.size ();
	}
	class RES_MAP_ITEM: public RES_MAP_ENTRY
	{
		public:
		RES_MAP_ITEM (ITEM_INDEX index = 0, RES_MAP_SCOPE *parent = nullptr, const std::wstring &name = L"", bool setnull = true): 
			RES_MAP_ENTRY (index, parent, name, RES_MAP_OBJTYPE::ITEM, setnull) {}
	};

	// DecisionInfo

	typedef struct _DECISION_INFO
	{
		WORD wFirstQualiIndex = 0;	// index of the first qualifier set index in the index table
		WORD wQualiSetsCount = 0;	// number of qualifiers sets in decision
	} DECISION_INFO;
	typedef struct _QUALIFIER_SET_INFO
	{
		WORD wFirstQualiIndex = 0;	// index of the first qualifier index in the index table // firstQualifierIndexIndex
		WORD wQualiSetsCount = 0;	// number of qualifiers in qualifier set // numQualifiersInSet
	} QUALIFIER_SET_INFO;
	typedef struct _QUALIFIER_INFO
	{
		WORD wDistQualiIndex = 0;	// index of distinct qualifier
		WORD wPriority = 0;			// priority
		WORD wFallbackScore = -1;	// fallback score, values range from 0 to 1000
		WORD wUnknown1 = -1;		// unknown, zero
	} QUALIFIER_INFO;
	enum class QUALIFIER_TYPE: WORD
	{
		LANGUAGE = 0,		// 语言 (0)
		CONTRAST = 1,		// 对比度 (1)
		SCALE = 2,			// 比例 (2)
		HOMEREGION = 3,		// 主屏幕区域 (3)
		TARGETSIZE = 4,		// 目标尺寸 (4)
		LAYOUTDIR = 5,		// 布局方向 (5)
		THEME = 6,			// 主题 (6)
		ALTERNATEFORM = 7,	// 替代格式 (7)
		DXFEATURELEVEL = 8,	// DX 功能等级 (8)
		CONFIG = 9,			// 配置 (9)
		DEVICEFAMILY = 10,	// 设备系列 (10)
		CUSTOM = 11,		// 自定义 (11)
	};
	std::wstring EnumToStringW (QUALIFIER_TYPE value)
	{
		switch (value)
		{
			case QUALIFIER_TYPE::LANGUAGE: return L"Language"; break;
			case QUALIFIER_TYPE::CONTRAST: return L"Contrast"; break;
			case QUALIFIER_TYPE::SCALE: return L"Scale"; break;
			case QUALIFIER_TYPE::HOMEREGION: return L"HomeRegion"; break;
			case QUALIFIER_TYPE::TARGETSIZE: return L"TargetSize"; break;
			case QUALIFIER_TYPE::LAYOUTDIR: return L"LayoutDir"; break;
			case QUALIFIER_TYPE::THEME: return L"Theme"; break;
			case QUALIFIER_TYPE::ALTERNATEFORM: return L"AlternateForm"; break;
			case QUALIFIER_TYPE::DXFEATURELEVEL: return L"DxFeatureLevel"; break;
			case QUALIFIER_TYPE::CONFIG: return L"Configure"; break;
			case QUALIFIER_TYPE::DEVICEFAMILY: return L"DeviceFamily"; break;
			case QUALIFIER_TYPE::CUSTOM: return L"Custom"; break;
		}
	}
	typedef struct _DISTINCE_QUALIFIER_INFO
	{
		WORD wUnknown1 = 0;			// unknown
		WORD wQualiType = 0;		// qualifier type
		WORD wUnknown2 = 0;			// unknown
		WORD wUnknown3 = 0;			// unknown
		DWORD wQualiValueOffset = 0;	// offset of qualifier value in qualifier value block, in characters
	} DISTINCE_QUALIFIER_INFO;
	typedef struct _QUALIFIER
	{
		ITEM_INDEX wIndex = 0;
		QUALIFIER_TYPE eType = QUALIFIER_TYPE::CUSTOM;
		WORD wPriority = 0;
		DOUBLE dFallbackScope = 0;
		std::wstring swValue = 0;
		_QUALIFIER (ITEM_INDEX index = 0, QUALIFIER_TYPE type = QUALIFIER_TYPE::CUSTOM, WORD priority = 0, DOUBLE fallbackScope = 0, const std::wstring &value = L""):
			wIndex (index), eType (type), wPriority (priority), dFallbackScope (fallbackScope), swValue (value) {}
	} QUALIFIER;
	typedef struct _QUALIFIER_SET
	{
		WORD wIndex = 0;
		std::vector <QUALIFIER> vecQuals;
		_QUALIFIER_SET (WORD index = 0, const std::vector <QUALIFIER> &quals = {}):
			wIndex (index), vecQuals (quals) {}
	} QUALIFIER_SET;
	typedef struct _DECISION
	{
		WORD wIndex = 0;
		std::vector <QUALIFIER_SET> verQualSets;
		_DECISION (WORD index, const std::vector <QUALIFIER_SET> &qualsets = {}):
			wIndex (index), verQualSets (qualsets) {}
	} DECISION;

	// ResourceMap & ResourceMap2

	typedef struct _HSCHEMA_REF_BLOCK
	{
		HSCHEMA_VERSION_INFO verHschema;	// hierarchical schema version info
		struct
		{
			WORD wUniIdLength = 0;				// length of unique id in characters, null-terminator included
			WORD wUnknown1 = -1;				// unknown, zero
			DWORD dwUnknown2 = 0;				// unknown
			DWORD dwUnknown3 = 0;				// unknown
		} part2;
		std::wstring swUniqueId = L"";		// unique id
		bool empty ()
		{
			return swUniqueId.empty () && part2.wUnknown1 != 0;
		}
	} HSCHEMA_REF_BLOCK;
	enum class RES_VALUE_TYPE: DWORD
	{
		STRING = 0,			// String (0)
		PATH = 1,			// Path (1)
		EMBEDDEDDATA = 2,	// EmbeddedData (2)
		ASCIISTRING = 3,	// AsciiString (3)
		UTF8STRING = 4,		// Utf8String (4)
		ASCIIPATH = 5,		// AsciiPath (5)
		UTF8PATH = 6		// Utf8Path (6)
	};
	typedef struct _RES_VALUE_TYPE_TABLE
	{
		DWORD dwUnknown1 = 0;	// unknown, 4
		DWORD dwResType = -1;	// resource value type
	} RES_VALUE_TYPE_TABLE;
	typedef struct _ITEM_ITEMINFO_GROUP_TABLE_ENTRY
	{
		WORD wFirstIndexProperty = 0;	// index property of first resource item
		WORD wItemInfoGroupIndex = 0;	// index of iteminfo group
	} ITEM_ITEMINFO_GROUP_TABLE_ENTRY;
	typedef struct _ITEMINFO_GROUP_TABLE_ENTRY
	{
		WORD wItemInfoCount;	// number of iteminfos in this group
		WORD wFirstItemIndex;	// index of the first iteminfo in this group
		_ITEMINFO_GROUP_TABLE_ENTRY (WORD count = 0, WORD firstIndex = 0): wItemInfoCount (count), wFirstItemIndex (firstIndex) {}
	} ITEMINFO_GROUP_TABLE_ENTRY;
	typedef struct _ITEM_ITEMINFO_TABLE_ENTRY
	{
		WORD wDecisionIndex = 0;	// index of decision
		WORD wFirstCandiIndex = 0;	// index of first candidate
	} ITEM_ITEMINFO_TABLE_ENTRY;
	typedef struct _TABLE_EXT_BLOCK
	{
		DWORD dwItemAdditEntCount = 0;		// number of additional entries of the item to iteminfo group table // ItemToItemInfoGroupCountLarge
		DWORD dwItemGroupAdditEntCount = 0;	// number of additional entries of the item info group table // itemInfoGroupCountLarge
		DWORD dwItemTableAdditEntCount = 0;	// number of additional entries of the iteminfo table // itemInfoCountLarge
	} TABLE_EXT_BLOCK;
	typedef BYTE CANDIDATE_TYPE;
	typedef struct _CANDIDATE0_DATA
	{
		BYTE bResValueType = 0;		// resource value type, specified as an index into the resource value type table
		WORD wEmbeddedLength = 0;	// embedded data length
		DWORD dwEmbeddedOffset = 0;	// offset of the embedded data in the embedded data block
	} CANDIDATE0_DATA;
	typedef struct _CANDIDATE1_DATA
	{
		BYTE bResValueType = 0;		// resource value type, specified as an index into the resource value type table
		WORD wSrcFile = 0;			// source file // sourceFileIndex
		WORD wDataIndex = 0;		// index of the data item storing the data // valueLocation
		WORD wSectIndex = 0;		// section index of the Data Item Section storing the data // dataItemSection
	} CANDIDATE1_DATA;
	typedef struct _CANDIDATE_INFO
	{
		BYTE bCandidateType = 0; // 0 或 1
		union CANDIDATE
		{
			CANDIDATE0_DATA _0;
			CANDIDATE1_DATA _1;
			CANDIDATE (CANDIDATE0_DATA can0): _0 (can0) {}
			CANDIDATE (CANDIDATE1_DATA can1): _1 (can1) {}
			CANDIDATE () {}
		} objCandidate;
		_CANDIDATE_INFO (CANDIDATE0_DATA can0): bCandidateType (0), objCandidate (can0) {}
		_CANDIDATE_INFO (CANDIDATE1_DATA can1): bCandidateType (1), objCandidate (can1) {}
		_CANDIDATE_INFO (): bCandidateType (-1) {}
		bool candidate_0 () const { return bCandidateType == 0; }
		bool candidate_1 () const { return bCandidateType == 1; }
		bool valid () const { return candidate_0 () ^ candidate_1 (); }
	} CANDIDATE_INFO;
	struct basic_sect;
	template <typename SectionType> struct ref_sect
	{
		using classtype = ref_sect <SectionType>;
		// static_assert (std::is_base_of <basic_sect, SectionType>::value, "SectionType must derive from basic_sect");
		int index;
		ref_sect (int sect_index = -1): index (sect_index) {}
		using sect_type = SectionType;
		explicit operator int () { return index; }
		bool valid () const { return index != -1; }
		void reset () { index = -1; }
		bool operator == (const classtype &another) const { return index == another.index; }
		classtype &operator = (const classtype &another) { index = another.index; return *this; }
		classtype &operator = (int newvalue) { index = newvalue; return *this; }
	};
	typedef struct _RES_MAP_ITEM_REF
	{
		ref_sect <sect_hierasche> wSchemaSect;
		int iItemIndex;
		_RES_MAP_ITEM_REF (const ref_sect <sect_hierasche> &ssect, int itemindex):
			wSchemaSect (ssect), iItemIndex (itemindex) {}
		_RES_MAP_ITEM_REF () {}
	} RES_MAP_ITEM_REF;
	struct _CANDIDATE;
	typedef struct _CANDIDATE_SET
	{
		RES_MAP_ITEM_REF refResMapItem;
		WORD wDecisionIndex = 0;
		std::vector <_CANDIDATE> vecCandidates;
	} CANDIDATE_SET;
	typedef struct _BASIC_REF
	{
		INT64 llIndex = -1;
		bool valid () const { return llIndex >= 0; }
		void reset () { llIndex = -1; }
		void set (int index) { llIndex = index; }
		void setnull () { llIndex = -1; }
		int get () { return llIndex; }
		bool isnull () { return !valid (); }
		_BASIC_REF (int index = 0, bool isnull = false):
			llIndex (isnull ? -1 : index) {}
		operator int () { return get (); }
		_BASIC_REF &operator = (int index) { set (index); return *this; }
		_BASIC_REF &operator = (const _BASIC_REF &rfr) { this->llIndex = rfr.llIndex; return *this; }
		bool operator == (const _BASIC_REF &another) const { return this->llIndex == another.llIndex; }
		bool operator == (int index) const { return (int)this->llIndex == index; }
	} BASIC_REF;
	typedef struct _REF_FILE_REF: public BASIC_REF 
	{
		using BASIC_REF::_BASIC_REF;
	} REF_FILE_REF;
	typedef struct _DATA_ITEM_REF: public BASIC_REF
	{
		ref_sect <sect_dataitem> iDataSectIndex;
		_DATA_ITEM_REF (int itemIndex = 0, int itemDataSectIndex = 0, bool isnull = false):
			BASIC_REF (itemIndex, isnull), iDataSectIndex (itemDataSectIndex) {}
		operator int () = delete;
		_DATA_ITEM_REF &operator = (int) = delete;
		_DATA_ITEM_REF &operator = (const _BASIC_REF &) = delete;
		_DATA_ITEM_REF &operator = (const _DATA_ITEM_REF &another)
		{
			this->llIndex = another.llIndex;
			this->iDataSectIndex = another.iDataSectIndex;
			return *this;
		}
		bool operator == (int) = delete;
		bool operator == (const _BASIC_REF &) = delete;
		bool operator == (const _DATA_ITEM_REF &another) const
		{ return llIndex == another.llIndex && iDataSectIndex == another.iDataSectIndex; }
	} DATA_ITEM_REF;
	typedef struct _BYTE_SPAN
	{
		ulint offset = 0;
		size_t length = 0;
		bool isnull = false;
		using classtype = _BYTE_SPAN;
		_BYTE_SPAN (ulint p_of = 0, size_t len = 0, bool nullstatus = false): offset (p_of), length (len), isnull (nullstatus) {}
		// 读取时会更改文件指针位置
		HRESULT get_bytes (istreamstream istream, std::vector <BYTE> &retbytes, size_t *retbyteslen = nullptr, bool cutbytesnoread = false)
		{
			retbytes.clear ();
			retbytes.resize (length);
			size_t bytesread = 0;
			if (retbyteslen) *retbyteslen = 0;
			HRESULT hr = istream.seek (offset, istreamstream::seekpos::start);
		#ifdef _DEBUG
			auto allsize = istream.size ();
		#endif
			if (FAILED (hr)) return hr;
			hr = istream.read (retbytes.data (), length, &bytesread);
			if (retbyteslen) *retbyteslen = bytesread;
			if (cutbytesnoread) retbytes.resize (bytesread);
			return hr;
		}
	} BYTE_SPAN;
	typedef struct _CANDIDATE
	{
		WORD wQualifierSet = 0;
		RES_VALUE_TYPE dwResType = RES_VALUE_TYPE::STRING;
		REF_FILE_REF iSrcFileIndex = 0;
		DATA_ITEM_REF iDataItem = 0;
		BYTE_SPAN posData;
		_CANDIDATE (WORD qualifierSet, RES_VALUE_TYPE resType, REF_FILE_REF srcFile, DATA_ITEM_REF dataItem):
			wQualifierSet (qualifierSet), dwResType (resType), iSrcFileIndex (srcFile), iDataItem (dataItem), posData (0, 0, true) {}
		_CANDIDATE (WORD qualifierSet, RES_VALUE_TYPE resType, BYTE_SPAN data):
			wQualifierSet (qualifierSet), dwResType (resType), iSrcFileIndex (-1, true), iDataItem (-1, 0, true), posData (data) {}
		_CANDIDATE (): iSrcFileIndex (-1, true), iDataItem (-1, 0, true), posData (0, 0, true) {}
		// 无效会返回 nullptr
		REF_FILE_REF *source_file_index () { return iSrcFileIndex.valid () ? &iSrcFileIndex : nullptr; }
		// 无效会返回 nullptr
		DATA_ITEM_REF *data_item_ref () { return iDataItem.valid () ? &iDataItem : nullptr; }
		// 无效会返回 nullptr
		BYTE_SPAN *data_position () { return posData.isnull ? nullptr : &posData; }
	} CANDIDATE;

	// DataItem

	typedef struct _STORED_STRING_INFO
	{
		WORD wStringOffset = 0;	// string offset, relative to start of stored data
		WORD wStringLength = 0; // string length in bytes
	} STORED_STRING_INFO;
	typedef struct _STORED_BLOB_INFO
	{
		DWORD dwBlobOffset = 0;	// blob offset, relative to start of stored data
		DWORD dwBlobLength = 0;	// blob length in bytes
	} STORED_BLOB_INFO;

	// ReferencedFile

	typedef struct _REF_FOLDER_INFO
	{
		WORD wUnknown1 = -1;			// unknown, zero
		WORD wParentIndex = 0xFFFF;		// index of parent folder, 0xFFFF if no parent exists (root)
		WORD wFolderCount = 0;			// number of folders in this folder
		WORD wFirstFolderIndex = 0;		// index of first folder in this folder
		WORD wFileCount = 0;			// number of files in this folder
		WORD wFirstFileIndex = 0;		// index of first file in this folder
		WORD wFolderNameLength = 0;		// length of folder name in characters
		WORD wFolderFullPathLength = 0;	// length of full folder path
		DWORD dwFolderNameOffset = 0;	// offset of folder name in Unicode name block
		friend istreamstream &operator >> (istreamstream &i, _REF_FOLDER_INFO &reff)
		{
			return i >> 
				reff.wUnknown1 >>
				reff.wParentIndex >>
				reff.wFolderCount >>
				reff.wFirstFolderIndex >>
				reff.wFileCount >>
				reff.wFirstFileIndex >>
				reff.wFolderNameLength >>
				reff.wFolderFullPathLength >>
				reff.dwFolderNameOffset;
		}
		friend bytesstream &operator >> (bytesstream &i, _REF_FOLDER_INFO &reff)
		{
			return i >>
				reff.wUnknown1 >>
				reff.wParentIndex >>
				reff.wFolderCount >>
				reff.wFirstFolderIndex >>
				reff.wFileCount >>
				reff.wFirstFileIndex >>
				reff.wFolderNameLength >>
				reff.wFolderFullPathLength >>
				reff.dwFolderNameOffset;
		}
	} REF_FOLDER_INFO;
	typedef struct _REF_FILE_INFO
	{
		WORD wUnknown1 = 0;				// unknown
		WORD wParentIndex = 0;			// index of parent folder
		WORD wFileFullPathLength = 0;	// length of full file path
		WORD wFileNameLength = 0;		// length of file name in characters
		DWORD dwFileNameOffset = 0;		// offset of file name in Unicode name block
		friend istreamstream &operator >> (istreamstream &i, _REF_FILE_INFO &r)
		{
			return i >> r.wUnknown1 >>
				r.wParentIndex >>
				r.wFileFullPathLength >>
				r.wFileNameLength >>
				r.dwFileNameOffset;
		}
		friend bytesstream &operator >> (bytesstream &i, _REF_FILE_INFO &r)
		{
			return i >> r.wUnknown1 >>
				r.wParentIndex >>
				r.wFileFullPathLength >>
				r.wFileNameLength >>
				r.dwFileNameOffset;
		}
	} REF_FILE_INFO;
	struct _REF_FOLDER;
	typedef struct _REF_FILE_ENTRY
	{
		enum class ENTRYTYPE
		{
			UNKNOWN = 0,
			FOLDER = 1,
			FILE = 2
		};
		_REF_FOLDER *rfParent;
		std::wstring swName = L"";
		std::wstring fullname ();
		std::wstring refresh_fullname () { swFullName.clear (); }
		size_t path (std::vector <std::wstring> &output);
		ENTRYTYPE type () const { return eType; }
		_REF_FILE_ENTRY (const std::wstring &name = L"", _REF_FOLDER *parent = nullptr, ENTRYTYPE type = ENTRYTYPE::UNKNOWN):
			swName (name), eType (type), rfParent (parent) {}
		protected:
		std::wstring swFullName = L"";
		ENTRYTYPE eType = ENTRYTYPE::UNKNOWN;
	} REF_FILE_ENTRY;
	typedef struct _REF_FOLDER: public _REF_FILE_ENTRY
	{
		std::vector <REF_FILE_ENTRY *> vecChildrens;
		_REF_FOLDER (const std::wstring &name = L"", _REF_FOLDER *parent = nullptr):
			REF_FILE_ENTRY (name, parent, REF_FILE_ENTRY::ENTRYTYPE::FOLDER) {}
	} REF_FOLDER;
	std::wstring _REF_FILE_ENTRY::fullname ()
	{
		if (std::wnstring::empty (swFullName))
		{
			if (rfParent) swFullName = rfParent->fullname () + L"\\" + swName;
			else swFullName = swName;
		}
		return swFullName;
	}
	size_t _REF_FILE_ENTRY::path (std::vector <std::wstring> &output)
	{
		output.clear ();
		if (rfParent) rfParent->path (output);
		output.push_back (swName);
		return output.size ();
	}
	typedef struct _REF_FILE: public REF_FILE_ENTRY
	{
		_REF_FILE (const std::wstring &name = L"", _REF_FOLDER *parent = nullptr):
			REF_FILE_ENTRY (name, parent, REF_FILE_ENTRY::ENTRYTYPE::FILE) {}
	} REF_FILE;

	// ReverseMap

	typedef struct _SCOPE_AND_ITEM_INFO
	{
		WORD wParent;
		WORD wFullPathLength;
		DWORD dwHashCode;
		// 不能直接使用，而是使用方法 name_offset 
		WORD wNameOffset;
		WORD wIndex;
		DWORD name_offset () const
		{
			return (DWORD)wNameOffset | (((dwHashCode >> 24) & 0xF) << 16);
		}
		bool name_in_ascii () const { return dwHashCode & 0x20000000; }
		bool is_scope () const { return dwHashCode & 0x10000000; }
		auto Item1 () const { return wParent; }
		auto Item2 () const { return wFullPathLength; }
		auto Item3 () const { return dwHashCode; }
		auto Item4 () const { return name_offset (); }
		auto Item5 () const { return wIndex; }
	} SCOPE_AND_ITEM_INFO;

	struct basic_sect
	{
		section &sect;
		basic_sect (section &s): sect (s) {}
		explicit operator section () { return sect; }
		SectionType type () const { return sect.type (); }
	};
	struct basic_sect_func
	{
		public:
		virtual bool valid () = 0;
		virtual void reset () = 0;
		virtual bool parse () = 0;
	};
#define counter(_count_, _variable_) for (size_t _variable_ = 0, _counter_##_variable_##_total_ = _count_; _variable_ < _counter_##_variable_##_total_; _variable_ ++)
	struct sect_pridesp: public basic_sect, public basic_sect_func
	{
		sect_pridesp (section &s): basic_sect (s)
		{
			if (s.type () != SectionType::PriDescriptor) throw std::exception ("Error: Section type error.");
			parse ();
		}
		struct ContentStruct
		{
			WORD wFlags = 0;				// 标志 / flags
			WORD wIncFileListIndex = -1;	// 包含文件列表节（Included File List）索引，若不存在则为 0xFFFF
			WORD wUnknown1 = -1;			// 未知，0
			WORD wHieraScheCount = 0;		// Hierarchical Schema 节数量
			WORD wDecInfoCount = 0;			// Decision Info 节数量
			WORD wResMapCount = 0;			// Resource Map 节数量
			WORD wResMapBegIndex = -1;		// 主资源映射（primary resource map）的节索引，若无则 0xFFFF
			WORD wRefFileCount = 0;			// Referenced File 节数量
			WORD wDataItemCount = 0;		// Data Item 节数量
			WORD wUnknown2 = -1;			// 未知，0
			ContentStruct () = default;
			ContentStruct (WORD f, WORD inc, WORD unk1, WORD hiera, WORD dec, WORD res, WORD resBeg, WORD ref, WORD data, WORD unk2)
				: wFlags (f), wIncFileListIndex (inc), wUnknown1 (unk1), wHieraScheCount (hiera),
				wDecInfoCount (dec), wResMapCount (res), wResMapBegIndex (resBeg), wRefFileCount (ref),
				wDataItemCount (data), wUnknown2 (unk2) {}
		} content;
		std::vector <ref_sect <sect_hierasche>> vec_ref_hs;
		std::vector <ref_sect <sect_decinfo>> vec_ref_deci;
		std::vector <ref_sect <sect_resmap>> vec_ref_rm;
		std::vector <ref_sect <sect_reffile>> vec_ref_rf;
		std::vector <ref_sect <sect_dataitem>> vec_ref_dati;
		ref_sect <sect_resmap> primary_resmap;
		bool valid () { return content.wUnknown1 == 0 && content.wUnknown2 == 0; }
		void reset ()
		{
			vec_ref_hs.clear ();	
			vec_ref_deci.clear ();
			vec_ref_rm.clear ();
			vec_ref_rf.clear ();	
			vec_ref_dati.clear ();	
			primary_resmap.reset ();
			content = {0, (WORD)-1, (WORD)-1, 0, 0, 0, (WORD)-1, 0, 0, (WORD)-1};
		}
		bool parse ()
		{
			reset ();
			HRESULT hr = sect.childst.seek ();
			istreamstream fp (sect.childst.ifile);
			DWORD dwContent = 0;
			hr = sect.childst.ifile->Read (&content, sizeof (content), &dwContent);
			if (!valid ()) return false;
			if (content.wResMapBegIndex != 0xFFFF) primary_resmap.index = content.wResMapBegIndex;
			counter (content.wHieraScheCount, i)
			{
				vec_ref_hs.push_back (fp.read_bytes <UINT16> ());
			}
			counter (content.wDecInfoCount, i)
			{
				vec_ref_deci.push_back (fp.read_bytes <UINT16> ());
			}
			counter (content.wResMapCount, i)
			{
				vec_ref_rm.push_back (fp.read_bytes <UINT16> ());
			}
			counter (content.wRefFileCount, i)
			{
				vec_ref_rf.push_back (fp.read_bytes <UINT16> ());
			}
			counter (content.wDataItemCount, i)
			{
				vec_ref_dati.push_back (fp.read_bytes <UINT16> ());
			}
			return true;
		}
	};
	struct sect_hierasche: public basic_sect, public basic_sect_func
	{
		struct
		{
			struct
			{
				WORD wUnknown1 = 0;					// 未知，1
				WORD wUniqRMNameLen = 0;			// 资源映射唯一名长度（字符数，含终止符）
				WORD wResMapNameLen = 0;			// 资源映射名称长度（字符数，含终止符）
				WORD wUnknown2 = -1;				// unknown, zero
			} part1;
			struct
			{
				// hname 标识符（仅 extended 存在）
				// hname identifier: only present in the extended Hierarchical Schema Section. 
				// Observed values are "[def_hnames] \0" and "[def_hnamesx] \0".
				CHAR szHNameExt [16] = {0};
			} part2;
			struct
			{
				HSCHEMA_VERSION_INFO verSchema;		// 层次化 schema 版本信息
			} part3;
			struct
			{
				std::wstring swUniqueRMName = L"";	// 资源映射的唯一名（unique name）
				std::wstring swResMapName = L"";	// name of resource map
				WORD wUnknown3 = -1;				// unknown, zero
				WORD wMaxFullPathLength = 0;		// length of longest full path of all resource names
				WORD wUnknown3_5 = -1;				// unknown, zero
				DWORD dwResNameCount = 0;			// number of resource names, usually number of scopes + items
				DWORD dwScopeCount = 0;				// number of scopes
				DWORD dwItemsCount = 0;				// number of items
				DWORD dwUniNameLemgth = 0;			// length of Unicode name block
				DWORD dwUnknown4 = 0;				// unknown
													// unknown at 70 + ?: only present in the extended Hierarchical 
													// Schema Section and if hname identifier is "[def_hnamesx] \0".
				DWORD dwUnknown5 = 0;
				void *get_buf_first_dir () { return &wUnknown3; }
				size_t get_buf_size_of ()
				{
					return sizeof (wUnknown3) + sizeof (wMaxFullPathLength) + sizeof (wUnknown3_5) +
						sizeof (dwScopeCount) + sizeof (dwItemsCount) + sizeof (dwResNameCount) +
						sizeof (dwUniNameLemgth) + sizeof (dwUnknown4) +
						sizeof (dwUnknown5);
				}
			} part4;
		} content;
		BOOL ex = FALSE;
		BOOL exHName = FALSE;
		std::vector <SCOPE_ITEM_INFO> vec_scope_and_items;
		std::vector <SCOPE_EX_INFO> vec_scope_ex;
		std::vector <ITEM_INDEX> vec_item_index;
		std::vector <RES_MAP_SCOPE> vec_scopes;
		std::vector <RES_MAP_ITEM> vec_items;
		sect_hierasche (section &s): basic_sect (s)
		{
			if (s.type () != SectionType::HierarchicalSchema && s.type () != SectionType::HierarchicalSchemaEx) throw std::exception ("Error: Section type error.");
			if (s.type () == SectionType::HierarchicalSchemaEx) ex = TRUE;
		}
		void throwexpect (const std::string &reason = "Error: unexpected value.")
		{
			throw std::exception (reason.c_str ());
		}
		bool valid ()
		{
			return content.part1.wUnknown1 == 1 &&
				content.part1.wUnknown2 == 0 &&
				content.part3.verSchema.dwUnknown1 == 0 &&
				content.part4.wUnknown3 == 0 &&
				content.part4.wUnknown3_5 == 0 &&
				content.part4.dwResNameCount == content.part3.verSchema.dwScopeCount + content.part3.verSchema.dwItemCount &&
				content.part4.dwScopeCount == content.part3.verSchema.dwScopeCount &&
				content.part4.dwItemsCount == content.part3.verSchema.dwItemCount &&
				content.part4.dwUniNameLemgth == content.part3.verSchema.dwItemCount &&
				content.part4.swUniqueRMName.length () == content.part1.wUniqRMNameLen &&
				content.part4.swResMapName.length () == content.part1.wResMapNameLen - 1;
		}
		void reset ()
		{
			vec_scope_and_items.clear ();
			vec_scope_ex.clear ();
			vec_item_index.clear ();
			vec_items.clear ();
			vec_scopes.clear ();
			ZeroMemory (&content.part1, sizeof (content.part1));
			content.part1.wUnknown2 = -1;
			ZeroMemory (&content.part2, sizeof (content.part2));
			ZeroMemory (&content.part3, sizeof (content.part3));
			content.part3.verSchema.dwUnknown1 = -1;
			content.part4.swUniqueRMName = L"";
			content.part4.swResMapName = L"";
			ZeroMemory (content.part4.get_buf_first_dir (), content.part4.get_buf_size_of ());
			content.part4.wUnknown3 = -1;
			content.part4.wUnknown3_5 = -1;
		}
		bool parse ()
		{
			reset ();
			sect.childst.seek ();
			istreamstream fp (sect.childst.ifile);
			if (sect.childst.size == 0) return true;
			auto &uniqueNameLength = content.part1.wUniqRMNameLen;
			auto &nameLength = content.part1.wResMapNameLen;
			fp >> content.part1;
			if (content.part1.wUnknown1 != 1 || content.part1.wUnknown2 != 0) throwexpect ();
			auto &extendedVersion = ex;
			if (ex)
			{
				fp >> content.part2;
				if (pri_sectid (content.part2.szHNameExt, 16).equals ("[def_hnamesx]")) exHName = true;
				else if (pri_sectid (content.part2.szHNameExt, 16).equals ("[def_hnames]")) exHName = false;
				else return false;
			}
			else exHName = false;
			auto &extendedHNames = exHName;
			auto &majorVersion = content.part3.verSchema.wMajor;
			auto &minorVersion = content.part3.verSchema.wMinor;
			auto &checksum = content.part3.verSchema.dwCheckSum;
			auto &numScopes = content.part3.verSchema.dwScopeCount;
			auto &numItems = content.part3.verSchema.dwItemCount;
			fp >> content.part3;
			if (content.part3.verSchema.dwUnknown1 != 0) throwexpect ();
			auto &Version = content.part3.verSchema;
			auto &UniqueName = content.part4.swUniqueRMName;
			auto &Name = content.part4.swResMapName;
			content.part4.swUniqueRMName = fp.read_string_endwith_null_w ();
			content.part4.swResMapName = fp.read_string_endwith_null_w ();
			fp.expect <WORD> (0, true, &content.part4.wUnknown3);
			auto &maxFullPathLength = content.part4.wMaxFullPathLength;
			fp >> content.part4.wMaxFullPathLength;
			fp.expect <WORD> (0, true, &content.part4.wUnknown3_5);
			fp.expect <DWORD> (numScopes + numItems, true, &content.part4.dwResNameCount);
			fp.expect <DWORD> (numScopes, true, &content.part4.dwScopeCount);
			fp.expect <DWORD> (numItems, true, &content.part4.dwItemsCount);
			auto &unicodeDataLength = content.part4.dwUniNameLemgth;
			fp >> unicodeDataLength;
			fp >> content.part4.dwUnknown4;
			if (extendedHNames) fp >> content.part4.dwUnknown5;
			auto &scopeAndItemInfos = vec_scope_and_items;
			scopeAndItemInfos.resize (content.part4.dwResNameCount);
			fp.read (scopeAndItemInfos.data (), (content.part4.dwResNameCount) * sizeof (SCOPE_ITEM_INFO));
			auto &scopeExInfos = vec_scope_ex;
			scopeExInfos.resize (numScopes);
			fp.read (scopeExInfos.data (), numScopes * sizeof (SCOPE_EX_INFO));
			auto &itemIndexPropertyToIndex = vec_item_index;
			itemIndexPropertyToIndex.resize (numItems);
			fp.read (itemIndexPropertyToIndex.data (), sizeof (ITEM_INDEX) * numItems);
			auto unicodeDataOffset = fp.position ();
			auto asciiDataOffset = fp.position () + unicodeDataOffset * 2;
			auto &scopes = vec_scopes;
			auto &items = vec_items;
			scopes.resize (numScopes);
			items.resize (numItems);
			counter (content.part4.dwResNameCount, i)
			{
				istreamstream::fsize_t pos = 0;
				if (scopeAndItemInfos [i].name_in_ascii ())
					pos = asciiDataOffset + scopeAndItemInfos [i].name_offset ();
				else pos = unicodeDataOffset + scopeAndItemInfos [i].name_offset () * 2;
				fp.seek (pos, istreamstream::seekpos::start);
				std::wstring name;
				if (scopeAndItemInfos [i].wFullPathLength != 0)
				{
					if (scopeAndItemInfos [i].name_in_ascii ())
						name = StringToWString (fp.read_string_endwith_null_a ());
					else name = fp.read_string_endwith_null_w ();
				}
				ITEM_INDEX index = scopeAndItemInfos [i].index ();
				if (scopeAndItemInfos [i].is_scope ())
				{
					if (!scopes [index].bIsNull) throwexpect ();
					else scopes.at (index) = RES_MAP_SCOPE (index, nullptr, name, false);
				}
				else
				{
					if (!items [index].bIsNull) throwexpect ();
					else items.at (index) = RES_MAP_ITEM (index, nullptr, name, false);
				}
			}
			counter (content.part4.dwResNameCount, i)
			{
				ITEM_INDEX index = scopeAndItemInfos [i].index ();
				WORD parent = scopeAndItemInfos [scopeAndItemInfos [i].wParentScopeIndex].index ();
				if (parent != 0xFFFF)
				{
					if (scopeAndItemInfos [i].is_scope ())
					{
						if (parent != index) scopes.at (index).pParent = &scopes [parent];
					}
					else items.at (index).pParent = &scopes [parent];
				}
			}
			counter (numScopes, i)
			{
				auto &scope = scopes [i];
				auto &children = scope.vecChild;
				counter (scopeExInfos [i].wChildCount, j)
				{
					auto &saiInfo = scopeAndItemInfos [scopeExInfos [i].wFirstChild + j];
					if (saiInfo.is_scope ()) children.push_back (&scopes.at (saiInfo.index ()));
					else children.push_back (&items.at (saiInfo.index ()));
				}
			}
			return valid ();
		}
	};
	struct sect_decinfo: public basic_sect, public basic_sect_func
	{
		sect_decinfo (section &s): basic_sect (s)
		{
			if (s.type () != SectionType::DecisionInfo) throw std::exception ("Error: Section type error.");
		}
		struct
		{
			WORD wDistQualiCount = 0;		// 不同的 distinct qualifiers 数量 / number of distinct qualifiers
			WORD wQualifierCount = 0;		// qualifiers 数量
			WORD wQualSetsCount = 0;		// qualifier sets 数量
			WORD wDecisionCount = 0;		// decisions 数量 / number of decisions
			WORD wEntriesCount = 0;			// index table 条目数 / number of entries in the index table
			WORD wQualiValueLength = 0;		// qualifier value block 长度（字符数） / length of qualifier value block in characters
		} content;
		std::vector <QUALIFIER_SET> vec_qua_set;
		std::vector <QUALIFIER> vec_qua;
		std::vector <DECISION> vec_dec;
		bool valid () 
		{ 
			return content.wDecisionCount ||
				content.wQualifierCount ||
				content.wDistQualiCount ||
				content.wQualSetsCount ||
				content.wEntriesCount ||
				content.wQualiValueLength ||
				vec_qua.size () ||
				vec_qua_set.size () ||
				vec_dec.size () ||
				0;
		}
		void reset ()
		{
			vec_qua.clear ();
			vec_qua_set.clear ();
			vec_dec.clear ();
			ZeroMemory (&content, sizeof (content));
		}
		bool parse ()
		{
			reset ();
			istreamstream fp (sect.childst.ifile);
			sect.childst.seek ();
			auto &numDistinctQualifiers = content.wDistQualiCount;
			auto &numQualifiers = content.wQualifierCount;
			auto &numQualifierSets = content.wQualSetsCount;
			auto &numDecisions = content.wDecisionCount;
			auto &numIndexTableEntries = content.wEntriesCount;
			auto &totalDataLength = content.wQualiValueLength;
			fp >> content;
			std::vector <DECISION_INFO> decisionInfos (numDecisions);
			std::vector <QUALIFIER_SET_INFO> qualifierSetInfos (numQualifierSets);
			std::vector <QUALIFIER_INFO> qualifierInfos (numQualifiers);
			std::vector <DISTINCE_QUALIFIER_INFO> distinctQualifierInfos (numDistinctQualifiers);
			std::vector <ITEM_INDEX> indexTable (numIndexTableEntries);
			auto &vec_dis_qua_info = distinctQualifierInfos;
			auto &vec_qua_info = qualifierInfos;
			auto &vec_qua_set_info = qualifierSetInfos;
			auto &indexs = indexTable;
			auto &vec_dec_info = decisionInfos;
			fp.read (decisionInfos.data (), sizeof (DECISION_INFO) * numDecisions);
			fp.read (qualifierSetInfos.data (), sizeof (DECISION_INFO) * numQualifierSets);
			fp.read (qualifierInfos.data (), sizeof (QUALIFIER_INFO) * numQualifiers);
			for (auto &it : qualifierInfos) { if (it.wUnknown1 != 0) throw std::exception ("Error: unexpective value."); } 
			fp.read (distinctQualifierInfos.data (), sizeof (DISTINCE_QUALIFIER_INFO) * numDistinctQualifiers);
			fp.read (indexTable.data (), sizeof (ITEM_INDEX) * numIndexTableEntries);
			auto currentpos = fp.position ();
			std::vector <WCHAR> buf (128);
			fp.read (buf.data (), 128 * sizeof (WCHAR));
			fp.seek (currentpos, istreamstream::seekpos::start);
			counter (content.wQualifierCount, i)
			{
				auto &dinfo = vec_dis_qua_info [vec_qua_info [i].wDistQualiIndex];
				auto &qinfo = vec_qua_info [i];
				fp.seek (currentpos + dinfo.wQualiValueOffset * 2, istreamstream::seekpos::start);
				std::wstring value = fp.read_string_endwith_null_w ();
				QUALIFIER qual (i, (QUALIFIER_TYPE)dinfo.wQualiType, qinfo.wPriority, (double)qinfo.wFallbackScore * 0.001, value);
				vec_qua.push_back (qual);
			}
			counter (content.wQualSetsCount, i)
			{
				std::vector <QUALIFIER> quals;
				auto qset = vec_qua_set_info [i];
				counter (qset.wQualiSetsCount, j)
				{
					auto &ind = indexs [qset.wFirstQualiIndex + j];
					auto &qual = vec_qua [ind];
					quals.push_back (qual);
				}
				vec_qua_set.emplace_back (QUALIFIER_SET (i, quals));
			}
			counter (content.wDecisionCount, i)
			{
				auto &dec = vec_dec_info [i];
				std::vector <QUALIFIER_SET> qsets;
				counter (dec.wQualiSetsCount, j)
				{
					auto &ind = indexs [dec.wFirstQualiIndex + j];
					auto &qset = vec_qua_set.at (ind);
					qsets.emplace_back (qset);
				}
				vec_dec.emplace_back (DECISION (i, qsets));
			}
			return valid ();
		}
	};
	struct sect_resmap: public basic_sect, public basic_sect_func
	{
		sect_resmap (section &s): basic_sect (s)
		{
			if (s.type () != SectionType::ResourceMap && s.type () != SectionType::ResourceMap2) throw std::exception ("Error: Section type error.");
			if (s.type () == SectionType::ResourceMap2) ver2 = true;
			else ver2 = false;
		}
		struct
		{
			WORD wEnvRefLength = 0;			// length of environment references block // environmentReferencesLength
			WORD wRefCount = 0;				// number of references in environment references block // numEnvironmentReferences
			WORD wHSSectIndex = 0;			// section index of Hierarchical Schema Section // SchemaSection
			WORD wHSRefLength = 0;			// length of hierarchical schema reference block // hierarchicalSchemaReferenceLength
			WORD wDecInfSectIndex = 0;		// section index of Decision Info Section // DecisionInfoSection
			WORD wResTypeEntCount = 0;		// number of entries in resource value type table // resourceValueTypeTableSize
			WORD wItemEntCount = 0;			// number of entries in item to iteminfo group table // ItemToItemInfoGroupCount
			WORD wItemGroupEntCount = 0;	// number of entries in iteminfo group table // itemInfoGroupCount
			DWORD dwItemTableEntCount = 0;	// number of entries in iteminfo table // itemInfoCount
			DWORD dwCandidateCount = 0;		// number of candidates // numCandidates
			DWORD dwEmbededDataCount = 0;	// length of embedded data bloc // dataLength
			DWORD dwTableExtCount = 0;		// length of table extension block // largeTableLength
		} content;
		BOOL ver2 = FALSE;
		std::vector <BYTE> bvecEnvRefData;
		std::vector <BYTE> bvecScheRefData;
		HSCHEMA_REF_BLOCK hschema_ref;
		std::vector <RES_VALUE_TYPE> vecResTypes;
		std::vector <ITEM_ITEMINFO_GROUP_TABLE_ENTRY> vecItemToItemInfoGroup;
		std::vector <ITEMINFO_GROUP_TABLE_ENTRY> vecItemInfoGroups;
		std::vector <ITEM_ITEMINFO_TABLE_ENTRY> vecItemInfo;
		std::vector <CANDIDATE_INFO> vecCandidateInfo;
		std::map <WORD, CANDIDATE_SET> mapCandidateSet;
		bool valid ()
		{
			if (parseError) return false;
			UINT64 *p = (UINT64 *)&content;
			bool res = false;
			res = (!ver2)
				? (content.wEnvRefLength != 0 && content.wRefCount != 0)
				: (content.wEnvRefLength == 0 && content.wRefCount == 0);
			if (!res) return false;
			if (content.wHSRefLength != 0)
			{
				if (hschema_ref.verHschema.dwUnknown1 != 0) return false;
				if (hschema_ref.part2.wUnknown1 != 0) return false;
			}
			return res;
		}
		void reset ()
		{
			parseError = false;
			bvecEnvRefData.clear ();
			bvecScheRefData.clear ();
			vecResTypes.clear ();
			vecItemToItemInfoGroup.clear ();
			vecItemInfoGroups.clear ();
			vecItemInfo.clear ();
			vecCandidateInfo.clear ();
			UINT64 *p = (UINT64 *)&content;
			bool res = false;
			size_t len = sizeof (content) / sizeof (UINT64);
			for (size_t i = 0; i < len; i ++) p [i] = 0;
			hschema_ref = HSCHEMA_REF_BLOCK ();
		}
		bool parse ();
		private:
		BOOL parseError = false;
	};
	struct sect_dataitem: public basic_sect
	{
		sect_dataitem (section &s): basic_sect (s)
		{
			if (s.type () != SectionType::DataItem) throw std::exception ("Error: Section type error.");
		}
		struct
		{
			DWORD dwUnknown1 = -1;	// unknown, zero
			WORD wStrCount = 0;		// number of stored strings
			WORD wBlobCount = 0;	// number of stored blobs
			DWORD dwStoredLength = 0;	// total length of stored data
		} content;
		std::vector <BYTE_SPAN> vecDataItems;
		bool valid ()
		{
			return content.dwUnknown1 == 0;
		}
		void reset ()
		{
			vecDataItems.clear ();
			ZeroMemory (&content, sizeof (content));
			content.dwUnknown1 = -1;
		}
		bool parse ()
		{
			reset ();
			sect.childst.seek ();
			istreamstream fp (sect.childst.ifile);
			auto sectionPosition = fp.position ();
			fp >> content;
			std::vector <BYTE_SPAN> &dataItems = vecDataItems;
			istreamstream::fsize_t dataStartOffset =
				fp.position () +
				content.wStrCount * 2 * sizeof (WORD) +
				content.wBlobCount * 2 * sizeof (DWORD);
			dataItems.reserve (content.wStrCount + content.wBlobCount);
			std::vector <STORED_STRING_INFO> storedStringInfos (content.wStrCount);
			std::vector <STORED_BLOB_INFO> storedBlobInfo (content.wBlobCount);
			fp.read (storedStringInfos.data (), sizeof (STORED_STRING_INFO) * content.wStrCount);
			fp.read (storedBlobInfo.data (), sizeof (STORED_BLOB_INFO) * content.wBlobCount);
			size_t cnt = 0;
			counter (content.wStrCount, i)
			{
				auto &sstr = storedStringInfos.at (i);
				dataItems.push_back (BYTE_SPAN (dataStartOffset + sstr.wStringOffset, sstr.wStringLength));
			}
			counter (content.wBlobCount, i)
			{
				auto &sblo = storedBlobInfo.at (i);
				dataItems.push_back (BYTE_SPAN (dataStartOffset + sblo.dwBlobOffset, sblo.dwBlobLength));
			}
			return valid ();
		}
	};
	struct sect_reffile: public basic_sect
	{
		sect_reffile (section &s): basic_sect (s)
		{
			if (s.type () != SectionType::ReferencedFile) throw std::exception ("Error: Section type error.");
		}
		struct
		{
			WORD wRootCount = 0;	// number of roots
			WORD wFolderCount = 0;	// number of folders
			WORD wFileCount = 0;	// number of folders
			WORD wUnknown1 = -1;	// unknown, zero
			DWORD dwNameLength = 0;	// length of Unicode name block in characters
		} content;
		std::vector <REF_FOLDER_INFO> vecFolderInfo;
		std::vector <REF_FILE_INFO> vecFileInfo;
		std::vector <REF_FOLDER> vecRefFolders;
		std::vector <REF_FILE> vecRefFiles;
		bool valid ()
		{
			return content.wUnknown1 == 0;
		}
		void reset ()
		{
			vecFolderInfo.clear ();
			vecFileInfo.clear ();
			vecRefFiles.clear ();
			vecRefFolders.clear ();
			ZeroMemory (&content, sizeof (content));
			content.wUnknown1 = -1;
		}
		bool parse ()
		{
			reset ();
			sect.childst.seek ();
			istreamstream fp (sect.childst.ifile);
			fp >> content;
			counter (content.wFolderCount, i)
			{
				REF_FOLDER_INFO folder;
				fp >> folder;
				if (folder.wUnknown1 != 0) throw std::exception ("Error: cannot get valid data in ReferencedFile Section.");
				vecFolderInfo.push_back (folder);
			}
			counter (content.wFileCount, i)
			{
				REF_FILE_INFO file;
				fp >> file;
				vecFileInfo.push_back (file);
			}
			auto dataStartPosition = fp.position ();
			auto &referencedFolders = vecRefFolders;
			using seekpos = istreamstream::seekpos;
			counter (content.wFolderCount, i)
			{
				fp.seek (dataStartPosition + vecFolderInfo [i].dwFolderNameOffset * 2, seekpos::start);
				std::wstring name = fp.read_string_w (vecFolderInfo [i].wFolderNameLength);
				referencedFolders.push_back (REF_FOLDER (name));
			}
			counter (content.wFolderCount, i)
			{
				if (vecFolderInfo [i].wParentIndex != 0xFFFF)
				{
					referencedFolders [i].rfParent = &referencedFolders [vecFolderInfo [i].wParentIndex];
				}
			}
			counter (content.wFileCount, i)
			{
				REF_FILE file;
				auto &fileInfo = vecFileInfo [i];
				fp.seek (dataStartPosition + fileInfo.dwFileNameOffset * 2, seekpos::start);
				std::wstring name = fp.read_string_w (fileInfo.wFileNameLength);
				file.swName = name;
				REF_FOLDER *parent = nullptr;
				if (vecFileInfo [i].wParentIndex != 0xFFFF) parent = &referencedFolders [fileInfo.wParentIndex];
				file.rfParent = parent;
				vecRefFiles.push_back (file);
			}
			counter (content.wFolderCount, i)
			{
				auto &folderInfo = vecFolderInfo [i];
				auto &referencedFolder = referencedFolders [i];
				counter (folderInfo.wFolderCount, j)
				{
					auto &folder = referencedFolders [folderInfo.wFirstFolderIndex + j];
					referencedFolder.vecChildrens.push_back (&folder);
				}
				counter (folderInfo.wFileCount, j)
				{
					auto &file = vecRefFiles [folderInfo.wFirstFileIndex + j];
					referencedFolder.vecChildrens.push_back (&file);
				}
			}
			return valid ();
		}
	};
	struct sect_revmap: public basic_sect, public basic_sect_func
	{
		sect_revmap (section &s): basic_sect (s)
		{
			if (s.type () != SectionType::ReverseMap) throw std::exception ("Error: Section type error.");
		}
		struct
		{
			struct
			{
				DWORD dwItemsNumber = 0;
				DWORD dwCheckCode = 0;
			} part1;
			struct
			{
				WORD wFullPathLength = 0;
				WORD wUnknown1 = -1; // 0
				DWORD dwEntries = 0;
				DWORD dwScopes = 0;
				DWORD dwCheckItemsNumber = 0;
				DWORD dwUnicodeDataLength = 0;
				DWORD dwSkipPadding = 0;
			} part2;
		} content;
		std::vector <DWORD> adwMap;
		std::vector <SCOPE_AND_ITEM_INFO> aobjScopeAndItem;
		std::vector <SCOPE_EX_INFO> aobjScopeExts;
		std::vector <ITEM_INDEX> awItemIndexs;
		std::vector <RES_MAP_SCOPE> aobjScopes;
		std::vector <RES_MAP_ITEM> aobjItems;
		bool valid ()
		{
			bool res = content.part2.wUnknown1 == 0 &&
				content.part2.dwCheckItemsNumber == content.part1.dwItemsNumber ||
				0;
			return res;
		}
		void reset ()
		{
			adwMap.clear ();
			aobjScopeAndItem.clear ();
			aobjScopeExts.clear ();
			awItemIndexs.clear ();
			aobjScopes.clear ();
			aobjItems.clear ();
			ZeroMemory (&content.part1, sizeof (content.part1));
			ZeroMemory (&content.part2, sizeof (content.part2));
			content.part2.wUnknown1 - 1;
		}
		bool parse ()
		{
			reset ();
			sect.childst.seek ();
			istreamstream fp (sect.childst.ifile);
			fp >> content.part1;
			bool chk = content.part1.dwCheckCode == fp.size () - 8;
			if (!chk) return false;
			adwMap.resize (content.part1.dwItemsNumber);
			fp.read (adwMap.data (), sizeof (DWORD) * content.part1.dwItemsNumber);
			fp >> content.part2;
			chk = content.part2.wUnknown1 == 0 && content.part2.dwCheckItemsNumber == content.part1.dwItemsNumber;
			if (!chk) return false;
			counter (content.part2.dwScopes + content.part1.dwItemsNumber, i)
			{
				SCOPE_AND_ITEM_INFO sii;
				fp >> sii;
				aobjScopeAndItem.push_back (sii);
			}
			counter (content.part2.dwScopes, i)
			{
				SCOPE_EX_INFO sei;
				fp >> sei;
				if (sei.wUnknown1 != 0) throw std::exception ("Error: read invalid data in ReverseMap Section.");
				aobjScopeExts.push_back (sei);
			}
			awItemIndexs.resize (content.part1.dwItemsNumber);
			fp.read (awItemIndexs.data (), content.part1.dwItemsNumber * sizeof (ITEM_INDEX));
			auto unicodeDataOffset = fp.position (),
				asciiDataOffset = fp.position () + content.part2.dwUnicodeDataLength * 2;
			aobjScopes.resize (content.part2.dwScopes);
			aobjItems.resize (content.part1.dwItemsNumber);
			counter (content.part1.dwItemsNumber + content.part2.dwScopes, i)
			{
				auto &sii = aobjScopeAndItem [i];
				bool nameInAscii = sii.name_in_ascii ();
				UINT64 pos = (nameInAscii ? asciiDataOffset : unicodeDataOffset) + (sii.Item4 () * (nameInAscii ? 1 : 2));
				fp.seek (pos, istreamstream::seekpos::start);
				std::wstring name;
				if (sii.Item2 ())
				{
					if (nameInAscii) name = StringToWString (fp.read_string_endwith_null_a ());
					else name = fp.read_string_endwith_null_w ();
				}
				auto index = sii.Item5 ();
				bool isScope = sii.is_scope ();
				if (isScope)
				{
					auto &it = aobjScopes.at (index);
					if (!it.bIsNull) throw std::exception ("Error: invalid scope data in ReverseMap Section.");
					else it = RES_MAP_SCOPE (index, nullptr, name);
				}
				else
				{
					auto &it = aobjItems.at (index);
					if (!it.bIsNull) throw std::exception ("Error: invalid item data in ReverseMap Section.");
					else it = RES_MAP_ITEM (index, nullptr, name);
				}
			}
			counter (content.part1.dwItemsNumber + content.part2.dwScopes, i)
			{
				auto &sii = aobjScopeAndItem [i];
				auto index = sii.Item5 ();
				bool isScope = sii.is_scope ();
				auto parent = aobjScopeAndItem [sii.Item1 ()].Item5 ();
				if (parent != 0xFFFF)
				{
					if (isScope && parent != index)
					{
						auto &it = aobjScopes.at (index);
						it.pParent = &aobjScopes.at (parent);
					}
					else 
					{
						auto &it = aobjItems.at (index);
						it.pParent = &aobjScopes.at (parent);
					}
				}

			}
			counter (content.part2.dwScopes, i)
			{
				auto &sei = aobjScopeExts [i];
				auto &scope = aobjScopes [i];
				counter (sei.wChildCount, j)
				{
					auto &saiInfo = aobjScopeAndItem [sei.wFirstChild + j];
					bool isScope = saiInfo.is_scope ();
					if (isScope)
					{
						auto &prt = aobjScopes [saiInfo.Item5 ()];
						scope.vecChild.push_back (&prt);
					}
					else
					{
						auto &prt = aobjItems [saiInfo.Item5 ()];
						scope.vecChild.push_back (&prt);
					}
				}
			}
			return true;
		}
	};
	struct sect_unknown: public basic_sect
	{
		sect_unknown (section &s): basic_sect (s) {}
		UINT64 dwLength = 0;
		void reset ()
		{
			dwLength = 0;
		}
		bool parse ()
		{
			reset ();
			sect.childst.seek ();
			istreamstream fp (sect.childst.ifile);
			dwLength = fp.size () - fp.position ();
			return true;
		}
		size_t bytes (std::vector <BYTE> &bytes)
		{
			sect.childst.seek ();
			istreamstream fp (sect.childst.ifile);
			bytes.resize (dwLength);
			size_t readlen = 0;
			fp.read (bytes.data (), dwLength, &readlen);
			bytes.resize (readlen > dwLength ? dwLength : readlen);
			return readlen;
		}
	};
}
class prifile
{
	private:
	IStream *pfile = nullptr;
	head header; foot footer;
	std::vector <tocentry> toclist;
	std::vector <section> sectlist;
	enum class searchtype
	{
		unknown,
		string,
		file
	};
	struct search_key
	{
		std::wnstring key = L"";
		searchtype type = searchtype::unknown;
		search_key (const std::wstring &k = L"", searchtype t = searchtype::unknown):
			key (k), type (t) {}
		bool operator == (const search_key &another) const { return key == another.key && type == another.type; }
		bool operator == (const std::wstring &an_key) const { return key.equals (an_key); }
		operator searchtype () const { return type; }
		operator LPCWSTR () const { return key.c_str (); }
	};
	struct search_value
	{
		std::wstring value = L"";
		bool isfind = false;
		int begindex = -1;
		bool finishsearch = false;
		operator LPCWSTR () const { return value.c_str (); }
		operator bool () const { return isfind; }
	};
	std::map <search_key, search_value> vecTaskSearch;
	bool isrunningtask = false;
	public:
	void close ()
	{
		header = head ();
		footer = foot ();
		toclist.clear ();
		sectlist.clear ();
	}
	bool load (IStream *ifile)
	{
		close ();
		if (!ifile) return false;
		ifile->Seek (lint (0), STREAM_SEEK_SET, nullptr);
		DWORD dwhead = 0, dwfoot = 0;
		ifile->Read (&header, sizeof (header), &dwhead);
		if (!dwhead) return false;
		if (!header.valid ()) return false;
		ifile->Seek (lint (header.dwFileSize - 16), STREAM_SEEK_SET, nullptr);
		ifile->Read (&footer, sizeof (footer), &dwfoot);
		if (!dwfoot) return false;
		if (!footer.valid (header)) return false;
		pfile = ifile;
		inittoc ();
		initsect ();
		return true;
	} 
	operator istreamstream () { return istreamstream (pfile); }
	pri::sect_pridesp section_pri_descriptor ()
	{
		for (auto &it : sectlist)
		{
			if (it.type () == SectionType::PriDescriptor)
			{
				try
				{
					auto sect = pri::sect_pridesp (it);
					sect.parse ();
					return sect;
				}
				catch (const std::exception &e) { continue; }
			}
		}
		throw std::exception ("Error: cannot get the pri descriptor section.");
	}
	section &get_section_by_ref (int index)
	{
		return sectlist.at (index);
	}
	template <typename SectionT> SectionT get_section_by_ref (pri::ref_sect <SectionT> ref)
	{
		return SectionT (sectlist.at (ref.index));
	}
	void inittoc ()
	{
		toclist.clear ();
		pfile->Seek (lint (header.dwToCOffset), STREAM_SEEK_SET, nullptr);
		for (size_t i = 0; i < header.wSectCount; i ++)
		{
			tocentry toc;
			DWORD dwRead;
			pfile->Read (&toc, sizeof (toc), &dwRead);
			toclist.push_back (toc);
		}
	}
	void initsect ()
	{
		sectlist.clear ();
		istreamstream iss (pfile);
		for (size_t i = 0; i < header.wSectCount; i ++)
		{
			iss.seek (header.dwSectStartOffset + toclist [i].dwSectOffset, istreamstream::seekpos::start);
			section sect (this->pfile, *this);
			iss.read (sect.head);
			iss.seek (sect.head.dwLength - 16 - 24);
			iss.read (sect.foot);
			iss.seek (header.dwSectStartOffset + toclist [i].dwSectOffset, istreamstream::seekpos::start);
			iss.seek (32);
			sect.childst.set (iss.position (), sect.head.dwLength - 16 - 24);
			sectlist.push_back (sect);
		}
	}
	// 用法：
	// auto rmsect = this->get_resmap_sect_by_ref (candidateSet.refResMapItem);
	// rmsect.parse ();
	// auto item = rmsect.vec_items [candidateSet.refResMapItem.iItemIndex];
	auto get_resmap_sect_by_ref (pri::RES_MAP_ITEM_REF resourceMapItemRef)
	{
		return get_section_by_ref <pri::sect_hierasche> (resourceMapItemRef.wSchemaSect);
	}
	// 用法：
	// auto ds = get_dataitem_sect_by_ref (dataItemRef);
	// ds.parse ();
	// ds.vecDataItems.at (dataItemRef.get ());
	auto get_dataitem_sect_by_ref (pri::DATA_ITEM_REF dataItemRef)
	{
		return get_section_by_ref <pri::sect_dataitem> (dataItemRef.iDataSectIndex);
	}
	bool get_reffile_by_ref (pri::REF_FILE_REF referencedFileRef, std::function <void (pri::REF_FILE &rf)> callback)
	{
		try
		{
			auto sect = get_section_by_ref (section_pri_descriptor ().vec_ref_rf.front ());
			auto &rf = sect.vecRefFiles.at (referencedFileRef);
			if (callback) callback (rf);
			return true;
		}
		catch (const std::exception &e)
		{
			return false;
		}
	}
	void end_taskrunning () { isrunningtask = false; }
	static void search_task (prifile &priinst)
	{
		destruct ([&priinst] () {
			priinst.end_taskrunning ();
		});
		if (priinst.isrunningtask) return;
		else priinst.isrunningtask = true;
		auto &tasklist = priinst.vecTaskSearch;

	}
	void across_all (std::wostream &out)
	{
	#ifdef _CONSOLE
		struct loadingamine
		{
			const WCHAR *charcollect = L"-\\|/-\\|/";
			WCHAR nowchar = L' ';
			bool isend = false;
			bool enablecallback = true;
			std::function <void (const WCHAR &)> callback = nullptr;
			void exectask ()
			{
				size_t cnt = 0;
				size_t charlen = lstrlenW (charcollect);
				std::function <void (const WCHAR &)> cb = callback;
				while (!isend)
				{
					nowchar = charcollect [(cnt ++) % charlen];
					if (cb && enablecallback) cb (nowchar);
					std::this_thread::sleep_for (std::chrono::milliseconds (300));
				}
			}
			void run ()
			{
				std::thread th (&loadingamine::exectask, this);
				th.detach ();
			}
			void jump () { isend = true; }
		} loadchar;
		destruct endt ([&loadchar] () {
			loadchar.isend = true;
		});
		std::wcout << L"   0 %";
		loadchar.callback = [] (const WCHAR &wch) {
			wprintf (L"\r %c 0 %%", wch);
		};
		loadchar.run ();
		auto DiffSystemTimeMs = [] (const SYSTEMTIME &st1, const SYSTEMTIME &st2) -> LONGLONG
		{
			FILETIME ft1, ft2;
			ULARGE_INTEGER t1, t2;
			SystemTimeToFileTime (&st1, &ft1);
			SystemTimeToFileTime (&st2, &ft2);

			t1.LowPart = ft1.dwLowDateTime;
			t1.HighPart = ft1.dwHighDateTime;
			t2.LowPart = ft2.dwLowDateTime;
			t2.HighPart = ft2.dwHighDateTime;

			// FILETIME 单位是 100 纳秒（1秒 = 10,000,000）
			LONGLONG diff100ns = t2.QuadPart - t1.QuadPart;

			// 转换为毫秒
			return diff100ns / 10000;  // 1 毫秒 = 10,000 * 100ns
		};
		out << L"Read Start: ";
		WCHAR buf [64];
		SYSTEMTIME st_start;
		GetLocalTime (&st_start);
		swprintf (buf, 64, L"%4d.%02d.%02d %02d:%02d:%02d",
			st_start.wYear, st_start.wMonth, st_start.wDay,
			st_start.wHour, st_start.wMinute, st_start.wSecond);
		out << buf << std::endl;
		out << L"Sections: " << sectlist.size () << std::endl;
		for (auto &it : sectlist)
		{
			out << L"    " << it << std::endl;
		}
		out << std::endl;
		auto pri_desp = this->section_pri_descriptor ();
		loadchar.callback = nullptr;
		loadchar.enablecallback = false;
		out << L"Candidates: " << pri_desp.vec_ref_rm.size () << std::endl;
		size_t cnt_i = 0;
		for (auto &it : pri_desp.vec_ref_rm)
		{
			auto resmap_sect = this->get_section_by_ref (it);
			resmap_sect.parse ();
			if (!resmap_sect.hschema_ref.empty ()) continue;
			auto decisionInfoSection = get_section_by_ref <pri::sect_decinfo> (resmap_sect.content.wDecInfSectIndex);
			decisionInfoSection.parse ();
			size_t cnt_j = 0;
			for (auto &it_cs : resmap_sect.mapCandidateSet)
			{
				auto &candidateSet = it_cs.second;
				auto rmsect = this->get_resmap_sect_by_ref (candidateSet.refResMapItem);
				rmsect.parse ();
				auto item = rmsect.vec_items [candidateSet.refResMapItem.iItemIndex];
				out << L"    " << item.full_name () << std::endl;
				size_t cnt_k = 0;
				for (auto &candidate : candidateSet.vecCandidates)
				{
					std::wstring value;
					if (candidate.source_file_index ())
					{
						auto temp = get_reffile_by_ref (*candidate.source_file_index (), [&value] (pri::REF_FILE &rf) {
							value += L"<external in " + rf.fullname () + L">";
						});
					}
					else
					{
						pri::BYTE_SPAN byteSpan;
						if (candidate.data_item_ref ())
						{
							auto dis = this->get_dataitem_sect_by_ref (*candidate.data_item_ref ());
							dis.parse ();
							byteSpan = dis.vecDataItems.at (candidate.data_item_ref ()->get ());
						}
						else
						{
							if (candidate.data_position ()) byteSpan = *candidate.data_position ();
							else byteSpan.isnull = true;
						}
						std::vector <BYTE> bytes (byteSpan.length + 2);
						size_t ret;
						HRESULT hr = byteSpan.get_bytes (pfile, bytes, &ret);
						bytes.resize (bytes.size () + 2);
						using restype = pri::RES_VALUE_TYPE;
						switch (candidate.dwResType)
						{
							case restype::ASCIIPATH:
							case restype::ASCIISTRING:
								value += StringToWString ((CHAR *)bytes.data ()); break;
							case restype::UTF8PATH:
							case restype::UTF8STRING:
								value += StringToWString ((CHAR *)bytes.data (), CP_UTF8); break;
							case restype::STRING:
							case restype::PATH:
								value += (WCHAR *)bytes.data (); break;
							case restype::EMBEDDEDDATA:
								value += L"<" + std::to_wstring (ret) + L" bytes>"; break;
						}
					}
					auto qualifierSet = decisionInfoSection.vec_qua_set [candidate.wQualifierSet];
					std::wstring qualifiers;
					for (auto qual : qualifierSet.vecQuals)
					{
						std::wstring str = L"            ";
						str += EnumToStringW (qual.eType) + L" = " + qual.swValue + L"\n";
						qualifiers += str;
					}
					out << L"        Value {" << value << L"}" << std::endl;
					out << qualifiers;
					double progress = (double)((cnt_i + (cnt_j + (double)cnt_k / candidateSet.vecCandidates.size ()) / resmap_sect.mapCandidateSet.size ()) / pri_desp.vec_ref_rm.size () * 100.0);
					std::wcout << L"\r " 
						<< loadchar.nowchar
						<< L" "
						<< std::fixed << std::setprecision (2)
						<< progress
						<< L" % [("
						<< cnt_k
						<< L" / "
						<< candidateSet.vecCandidates.size ()
						<< L") of ("
						<< cnt_j
						<< L" / "
						<< resmap_sect.mapCandidateSet.size ()
						<< L") of ("
						<< cnt_i
						<< L" / "
						<< pri_desp.vec_ref_rm.size ()
						<< L")]"
						<< L"              "
						;
					cnt_k ++;
				}
				cnt_j ++;
			}
			int i = 0;
			cnt_i ++;
		}
		int j = 0;
		std::wcout << L"\r   100 %                                                  " << std::endl;
		out << L"Read Completed: ";
		SYSTEMTIME st_end;
		GetLocalTime (&st_end);
		ZeroMemory (buf, 60 * sizeof (WCHAR));
		swprintf (buf, 64, L"%4d.%02d.%02d %02d:%02d:%02d",
			st_end.wYear, st_end.wMonth, st_end.wDay,
			st_end.wHour, st_end.wMinute, st_end.wSecond);
		out << buf << std::endl;
		out << L"Time Spend: " << DiffSystemTimeMs (st_start, st_end) * 0.001 << L"s" << std::endl;
	#endif
	}
};
bool pri::sect_resmap::parse ()

{
	reset ();
	istreamstream fp (sect.childst.ifile);
	sect.childst.seek ();
	ulint sectpos = 0;
	fp->Seek (lint (0), STREAM_SEEK_CUR, sectpos.ptr_union ());
	fp->Read (&content, sizeof (content), nullptr);
	bool res = (!ver2) ? (content.wEnvRefLength != 0 && content.wRefCount != 0) : (content.wEnvRefLength == 0 && content.wRefCount == 0);
	if (!res) return false;
	{
		auto currpos = fp.position ();
		try
		{
			auto dest = sect.pri_file.get_section_by_ref <sect_decinfo> (ref_sect <sect_decinfo> (content.wDecInfSectIndex));
			dest.parse ();
		}
		catch (const std::exception &e)
		{
			parseError = true;
			return false;
		}
		fp.seek (currpos, istreamstream::seekpos::start);
	}
	bvecEnvRefData.resize (content.wEnvRefLength);
	bvecScheRefData.resize (content.wHSRefLength);
	ZeroMemory (bvecEnvRefData.data (), sizeof (BYTE) * content.wEnvRefLength);
	ZeroMemory (bvecScheRefData.data (), sizeof (BYTE) * content.wHSRefLength);
	fp->Read (bvecEnvRefData.data (), sizeof (BYTE) * content.wEnvRefLength, nullptr);
	fp->Read (bvecScheRefData.data (), sizeof (BYTE) * content.wHSRefLength, nullptr);
	if (content.wHSRefLength != 0)
	{
		bytesstream srdata (bvecScheRefData);
		srdata.read (&hschema_ref.verHschema, sizeof (hschema_ref.verHschema));
		if (hschema_ref.verHschema.dwUnknown1 != 0) return false;
		srdata.read (&hschema_ref.part2, sizeof (hschema_ref.part2));
		if (hschema_ref.part2.wUnknown1 != 0) return false;
		hschema_ref.swUniqueId = ReadStringEndwithNullW (fp);
	}
	for (size_t i = 0; i < content.wResTypeEntCount; i ++)
	{
		RES_VALUE_TYPE_TABLE rvtt;
		fp->Read (&rvtt, sizeof (rvtt), nullptr);
		if (rvtt.dwUnknown1 != 4) return false;
		vecResTypes.push_back ((RES_VALUE_TYPE)rvtt.dwResType);
	}
	for (size_t i = 0; i < content.wItemEntCount; i ++)
	{
		ITEM_ITEMINFO_GROUP_TABLE_ENTRY iigte;
		fp->Read (&iigte, sizeof (iigte), nullptr);
		vecItemToItemInfoGroup.push_back (iigte);
	}
	for (size_t i = 0; i < content.wItemGroupEntCount; i ++)
	{
		ITEMINFO_GROUP_TABLE_ENTRY iigte;
		fp->Read (&iigte, sizeof (iigte), nullptr);
		vecItemInfoGroups.push_back (iigte);
	}
	for (size_t i = 0; i < content.dwItemTableEntCount; i ++)
	{
		ITEM_ITEMINFO_TABLE_ENTRY iite;
		fp->Read (&iite, sizeof (iite), nullptr);
		vecItemInfo.push_back (iite);
	}
	std::vector <BYTE> largeTable (content.dwTableExtCount);
	fp->Read (largeTable.data (), sizeof (BYTE) * content.dwTableExtCount, nullptr);
	if (largeTable.size () != 0)
	{
		bytesstream bytes (largeTable);
		TABLE_EXT_BLOCK teb;
		bytes.read (&teb, sizeof (teb));
		for (size_t i = 0; i < teb.dwItemAdditEntCount; i ++)
		{
			ITEM_ITEMINFO_GROUP_TABLE_ENTRY iiigte;
			bytes.read (&iiigte, sizeof (iiigte));
			vecItemToItemInfoGroup.push_back (iiigte);
		}
		for (size_t i = 0; i < teb.dwItemGroupAdditEntCount; i ++)
		{
			ITEMINFO_GROUP_TABLE_ENTRY iigte;
			bytes.read (&iigte, sizeof (iigte));
			vecItemInfoGroups.push_back (iigte);
		}
		for (size_t i = 0; i < teb.dwItemTableAdditEntCount; i ++)
		{
			ITEM_ITEMINFO_TABLE_ENTRY iiite;
			bytes.read (&iiite, sizeof (iiite));
			vecItemInfo.push_back (iiite);
		}
		if (bytes.position () > bytes.length ()) throw std::exception ("Error: invalid data in ResourceMap or ResourceMap2 Section.");
	}
	for (size_t i = 0; i < content.dwCandidateCount; i ++)
	{
		BYTE bType = -1;
		fp->Read (&bType, sizeof (bType), nullptr);
		switch (bType)
		{
			case 0x00: {
				CANDIDATE_TYPE rvtype = 0;
				fp->Read (&rvtype, sizeof (rvtype), nullptr);
				CANDIDATE0_DATA cdata;
				cdata.bResValueType = (BYTE)vecResTypes.at (rvtype);
				auto &length = cdata.wEmbeddedLength;
				auto &stringOffset = cdata.dwEmbeddedOffset;
				fp >> length >> stringOffset;
				vecCandidateInfo.emplace_back (CANDIDATE_INFO (cdata));
			} break;
			case 0x01: {
				CANDIDATE_TYPE rvtype = 0;
				fp->Read (&rvtype, sizeof (rvtype), nullptr);
				CANDIDATE1_DATA cdata;
				auto &resourceValueType = cdata.bResValueType;
				auto &sourceFileIndex = cdata.wSrcFile;
				auto &valueLocation = cdata.wDataIndex;
				auto &dataItemSection = cdata.wSectIndex;
				resourceValueType = (BYTE)vecResTypes.at (rvtype);
				fp >> sourceFileIndex >> valueLocation >> dataItemSection;
				vecCandidateInfo.emplace_back (CANDIDATE_INFO (cdata));
			} break;
			default: {
				throw std::domain_error ("Error: invalid data read in ResourceMap or ResourceMap2 section.");
			} break;
		}
	}
	ulint strbegpos = 0;
	fp->Seek (lint (0), STREAM_SEEK_CUR, strbegpos.ptr_union ());
	for (size_t i = 0; i < vecItemToItemInfoGroup.size (); i ++)
	{
		auto &itemToItemGroup = vecItemToItemInfoGroup [i];
		ITEMINFO_GROUP_TABLE_ENTRY itemInfoGroup;
		if (itemToItemGroup.wItemInfoGroupIndex < vecItemInfoGroups.size ())
			itemInfoGroup = (vecItemInfoGroups [itemToItemGroup.wItemInfoGroupIndex]);
		else itemInfoGroup = {1, (WORD)(itemToItemGroup.wItemInfoGroupIndex - vecItemInfoGroups.size ())};
		for (size_t j = itemInfoGroup.wFirstItemIndex; j < itemInfoGroup.wFirstItemIndex + itemInfoGroup.wItemInfoCount; j ++)
		{
			auto &itemInfo = vecItemInfo [j];
			auto decIndex = itemInfo.wDecisionIndex;
			auto &decSect = sect.pri_file.get_section_by_ref (ref_sect <sect_decinfo> (content.wDecInfSectIndex));
			decSect.parse ();
			auto dec = decSect.vec_dec [decIndex];
			CANDIDATE_SET candidateSet;
			std::vector <CANDIDATE> &candidates = candidateSet.vecCandidates;
			for (size_t k = 0; k < dec.verQualSets.size (); k ++)
			{
				auto can_info = vecCandidateInfo [itemInfo.wFirstCandiIndex + k];
				switch (can_info.bCandidateType)
				{
					case 0x01: {
						REF_FILE_REF sourceFile;
						if (!can_info.objCandidate._1.wSrcFile) sourceFile.setnull ();
						else sourceFile.set (can_info.objCandidate._1.wSrcFile - 1);
						candidates.push_back (
							CANDIDATE (
								dec.verQualSets [k].wIndex,
								(RES_VALUE_TYPE)can_info.objCandidate._1.bResValueType,
								sourceFile,
								DATA_ITEM_REF (can_info.objCandidate._1.wDataIndex, can_info.objCandidate._1.wSectIndex
								)
							)
						);
					} break;
					case 0x00: {
						BYTE_SPAN bspan (
							sectpos + strbegpos + ulint (can_info.objCandidate._0.dwEmbeddedOffset),
							can_info.objCandidate._0.wEmbeddedLength
						);
						candidates.push_back (
							CANDIDATE (
								dec.verQualSets [k].wIndex,
								(RES_VALUE_TYPE)can_info.objCandidate._1.bResValueType,
								bspan
							)
						);
					} break;
				}
			}
			WORD resourceMapItemIndex = itemToItemGroup.wFirstIndexProperty + (j - itemInfoGroup.wFirstItemIndex);
			candidateSet.refResMapItem = RES_MAP_ITEM_REF (content.wHSSectIndex, resourceMapItemIndex);
			candidateSet.wDecisionIndex = decIndex;
			mapCandidateSet [resourceMapItemIndex] = candidateSet;
		}
	}
	return valid ();
}
#ifdef UNALIGN_MEMORY
#pragma pack(pop)
#endif