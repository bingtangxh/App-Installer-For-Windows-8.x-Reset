#pragma once
#include <string>
#include <locale>
#include <cctype>
namespace l0km
{
	template <typename E, typename TR = std::char_traits <E>, typename AL = std::allocator <E>> inline std::basic_string<E, TR, AL> toupper (const std::basic_string <E, TR, AL> &src)
	{
		std::basic_string <E, TR, AL> dst = src;
		static const std::locale loc;
		const std::ctype <E> &ctype = std::use_facet <std::ctype <E>> (loc);
		for (typename std::basic_string <E, TR, AL>::size_type i = 0; i < src.size (); ++ i)
		{
			dst [i] = ctype.toupper (src [i]);
		}
		return dst;
	}
	template <typename E, typename TR = std::char_traits <E>, typename AL = std::allocator <E>> inline std::basic_string <E, TR, AL> tolower (const std::basic_string <E, TR, AL> &src)
	{
		std::basic_string <E, TR, AL> dst = src;
		static const std::locale loc;
		const std::ctype <E> &ctype = std::use_facet <std::ctype <E>> (loc);
		for (typename std::basic_string <E, TR, AL>::size_type i = 0; i < src.size (); ++ i)
		{
			dst [i] = ctype.tolower (src [i]);
		}
		return dst;
	}
	inline char toupper (char ch)
	{
		if (ch < -1) return ch;
		static const std::locale loc;
		return std::use_facet <std::ctype <char>> (loc).toupper (ch);
	}
	inline char tolower (char ch)
	{
		if (ch < -1) return ch;
		static const std::locale loc;
		return std::use_facet <std::ctype <char>> (loc).tolower (ch);
	}
	inline wchar_t toupper (wchar_t ch)
	{
		if (ch < -1) return ch;
		static const std::locale loc;
		return std::use_facet <std::ctype <wchar_t>> (loc).toupper (ch);
	}
	inline wchar_t tolower (wchar_t ch)
	{
		if (ch < -1) return ch;
		static const std::locale loc;
		return std::use_facet <std::ctype <wchar_t>> (loc).tolower (ch);
	}
	inline int toupper (int ch)
	{
		if (ch < -1) return ch;
		static const std::locale loc;
		return std::use_facet <std::ctype <int>> (loc).toupper (ch);
	}
	inline int tolower (int ch)
	{
		if (ch < -1) return ch;
		static const std::locale loc;
		return std::use_facet <std::ctype <int>> (loc).tolower (ch);
	}
}
template <typename ct> bool is_blank (ct &ch)
{
	return ch == ct (' ') || ch == ct ('\t') || ch == ct ('\n');
}
template <typename E, typename TR = std::char_traits <E>, typename AL = std::allocator <E>> std::basic_string <E, TR, AL> NormalizeString (const std::basic_string <E, TR, AL> &str, bool upper = false, bool includemidblank = false)
{
	typedef std::basic_string <E, TR, AL> string_type;
	string_type result;
	if (str.empty ()) return result;
	auto begin_it = str.begin ();
	auto end_it = str.end ();
	while (begin_it != end_it && is_blank (*begin_it)) ++begin_it;
	while (end_it != begin_it && is_blank (*(end_it - 1))) --end_it;
	bool in_space = false;
	for (auto it = begin_it; it != end_it; ++ it)
	{
		if (is_blank (*it))
		{
			if (includemidblank)
			{
				if (!in_space)
				{
					result.push_back (E (' '));
					in_space = true;
				}
			}
			else
			{
				result.push_back (*it);
				in_space = true;
			}
		}
		else
		{
			result.push_back (*it);
			in_space = false;
		}
	}
	if (upper) return l0km::toupper (result);
	else return l0km::tolower (result);
}
template <typename E, typename TR = std::char_traits <E>, typename AL = std::allocator <E>> bool IsNormalizeStringEquals (const std::basic_string <E, TR, AL> &l, const std::basic_string <E, TR, AL> &r, bool includemidblank = false)
{
	auto _local_strlen = [] (const E *p) -> size_t {
		size_t cnt = 0;
		while (*(p + cnt)) { cnt ++; }
		return cnt;
	};
	const E *pl = l.c_str ();
	const E *pr = r.c_str ();
	while (*pl && is_blank (*pl)) ++ pl;
	while (*pr && is_blank (*pr)) ++ pr;
	const E *el = l.c_str () + _local_strlen (l.c_str ());
	const E *er = r.c_str () + _local_strlen (r.c_str ());
	while (el > pl && is_blank (*(el - 1))) --el;
	while (er > pr && is_blank (*(er - 1))) --er;
	while (pl < el && pr < er) 
	{
		if (includemidblank)
		{
			if (is_blank (*pl) && is_blank (*pr))
			{
				while (pl < el && is_blank (*pl)) ++pl;
				while (pr < er && is_blank (*pr)) ++pr;
				continue;
			}
			else if (is_blank (*pl))
			{
				while (pl < el && is_blank (*pl)) ++pl;
				continue;
			}
			else if (is_blank (*pr))
			{
				while (pr < er && is_blank (*pr)) ++pr;
				continue;
			}
		}
		if (l0km::tolower (*pl) != l0km::tolower (*pr)) return false;
		++ pl;
		++ pr;
	}
	while (pl < el && is_blank (*pl)) ++ pl;
	while (pr < er && is_blank (*pr)) ++ pr;
	return pl == el && pr == er;
}
template <typename E, typename TR = std::char_traits <E>, typename AL = std::allocator <E>> int64_t NormalizeStringCompare (const std::basic_string <E, TR, AL> &l, const std::basic_string <E, TR, AL> &r, bool includemidblank = false)
{
	auto _local_strlen = [] (const E *p) -> size_t {
		size_t cnt = 0;
		while (*(p + cnt)) { cnt ++; }
		return cnt;
	};
	const E *pl = l.c_str ();
	const E *pr = r.c_str ();
	while (*pl && is_blank (*pl)) ++ pl;
	while (*pr && is_blank (*pr)) ++ pr;
	const E *el = l.c_str () + _local_strlen (l.c_str ());
	const E *er = r.c_str () + _local_strlen (r.c_str ());
	while (el > pl && is_blank (*(el - 1))) -- el;
	while (er > pr && is_blank (*(er - 1))) -- er;
	while (pl < el && pr < er)
	{
		if (includemidblank)
		{
			if (is_blank (*pl) && is_blank (*pr))
			{
				while (pl < el && is_blank (*pl)) ++pl;
				while (pr < er && is_blank (*pr)) ++pr;
				continue;
			}
			else if (is_blank (*pl))
			{
				while (pl < el && is_blank (*pl)) ++pl;
				continue;
			}
			else if (is_blank (*pr))
			{
				while (pr < er && is_blank (*pr)) ++pr;
				continue;
			}
		}
		E chl = l0km::tolower (*pl);
		E chr = l0km::tolower (*pr);
		if (chl != chr) return (int64_t)chl - (int64_t)chr;
		++ pl;
		++ pr;
	}
	while (pl < el && is_blank (*pl)) ++ pl;
	while (pr < er && is_blank (*pr)) ++ pr;
	if (pl == el && pr == er) return 0;
	if (pl == el) return -1;
	if (pr == er) return 1;
	return (int64_t)l0km::tolower (*pl) - (int64_t)l0km::tolower (*pr);
}
template <typename CharT> bool IsNormalizeStringEquals (const CharT *l, const CharT *r, bool includemidblank = false)
{
	if (!l || !r) return l == r;
	auto skip_blank = [] (const CharT *&p)
	{
		while (*p && is_blank (*p)) ++ p;
	};
	const CharT *p1 = l;
	const CharT *p2 = r;
	skip_blank (p1);
	skip_blank (p2);
	while (*p1 && *p2)
	{
		CharT ch1 = l0km::tolower (*p1);
		CharT ch2 = l0km::tolower (*p2);
		if (ch1 != ch2) return false;
		++ p1;
		++ p2;
		if (includemidblank)
		{
			if (is_blank (*p1) || is_blank (*p2))
			{
				skip_blank (p1);
				skip_blank (p2);
			}
		}
	}
	skip_blank (p1);
	skip_blank (p2);
	return *p1 == 0 && *p2 == 0;
}
template <typename CharT> int64_t NormalizeStringCompare (const CharT *l, const CharT *r, bool includemidblank = false)
{
	if (!l || !r) return l ? 1 : (r ? -1 : 0);
	auto skip_blank = [] (const CharT *&p)
	{
		while (*p && is_blank (*p)) ++ p;
	};
	const CharT *p1 = l;
	const CharT *p2 = r;
	skip_blank (p1);
	skip_blank (p2);
	while (*p1 && *p2)
	{
		CharT ch1 = l0km::tolower (*p1);
		CharT ch2 = l0km::tolower (*p2);
		if (ch1 != ch2) return (ch1 < ch2) ? -1 : 1;
		++ p1;
		++ p2;
		if (includemidblank)
		{
			if (is_blank (*p1) || is_blank (*p2))
			{
				skip_blank (p1);
				skip_blank (p2);
			}
		}
	}
	skip_blank (p1);
	skip_blank (p2);
	if (*p1 == 0 && *p2 == 0) return 0;
	if (*p1 == 0) return -1;
	return 1;
}
template <typename E, typename TR = std::char_traits <E>, typename AL = std::allocator <E>> bool IsNormalizeStringEmpty (const std::basic_string <E, TR, AL> &str)
{
	return IsNormalizeStringEquals (str, std::basic_string <E, TR, AL> ());
}
template <typename E, typename TR = std::char_traits <E>, typename AL = std::allocator <E>> std::basic_string <E, TR, AL> StringTrim (const std::basic_string <E, TR, AL> &str, bool includemidblank = false)
{
	typedef std::basic_string <E, TR, AL> string_type;
	typedef typename string_type::size_type size_type;
	if (str.empty ()) return string_type ();
	size_type first = 0;
	size_type last = str.size ();
	while (first < last && is_blank (str [first])) ++first;
	while (last > first && is_blank (str [last - 1])) --last;
	if (first == last) return string_type ();
	string_type result;
	result.reserve (last - first);
	bool in_space = false;
	for (size_type i = first; i < last; ++ i)
	{
		if (is_blank (str [i]))
		{
			if (includemidblank)
			{
				if (!in_space)
				{
					result.push_back (E (' '));
					in_space = true;
				}
			}
			else
			{
				result.push_back (str [i]);
				in_space = true;
			}
		}
		else
		{
			result.push_back (str [i]);
			in_space = false;
		}
	}
	return result;
}
template <typename E, typename TR = std::char_traits<E>, typename AL = std::allocator <E>> size_t GetNormalizeStringLength (const std::basic_string <E, TR, AL> &str, bool includemidblank = false)
{
	typedef typename std::basic_string <E, TR, AL>::size_type size_type;
	if (str.empty ()) return 0;
	size_type first = 0, last = str.size ();
	while (first < last && is_blank (str [first])) ++first;
	while (last > first && is_blank (str [last - 1])) --last;
	if (first == last) return 0;
	size_t length = 0;
	bool in_space = false;
	for (size_type i = first; i < last; ++i)
	{
		if (is_blank (str [i]))
		{
			if (includemidblank)
			{
				if (!in_space)
				{
					++ length;
					in_space = true;
				}
			}
			else
			{
				++ length;
				in_space = true;
			}
		}
		else
		{
			++ length;
			in_space = false;
		}
	}
	return length;
}
namespace std
{
	
	template <typename ct, typename tr = std::char_traits <ct>, typename al = std::allocator <ct>> class basic_nstring: public std::basic_string <ct, tr, al>
	{
		using base = std::basic_string <ct, tr, al>;
		bool default_upper = false, default_include_blank_in_str = false;
		public:
		using typename base::size_type;
		using typename base::value_type;
		using base::base;
		basic_nstring (): base (), default_upper (false), default_include_blank_in_str (false) {}
		basic_nstring (const ct *pStr): base (pStr), default_upper (false), default_include_blank_in_str (false) {}
		basic_nstring (const base &str): base (str) {}
		basic_nstring (base &&str): base (std::move (str)) {}
		basic_nstring (const ct *data, size_type count): base (data, count), default_upper (false), default_include_blank_in_str (false) {}
		template <std::size_t N> basic_nstring (const ct (&arr) [N]) : base (arr, N) {}
		template <typename InputIt> basic_nstring (InputIt first, InputIt last): base (first, last), default_upper (false), default_include_blank_in_str (false) {}
		bool upper_default () const { return this->default_upper; }
		bool upper_default (bool value) { return this->default_upper = value; }
		bool include_blank_in_str_middle () const { return this->default_include_blank_in_str; }
		bool include_blank_in_str_middle (bool value) { return this->default_include_blank_in_str = value; }
		base normalize (bool upper, bool includemidblank) const
		{
			return NormalizeString <ct, tr, al> (*this, upper, includemidblank);
		}
		base normalize (bool upper) const
		{
			return this->normalize (upper, default_include_blank_in_str);
		}
		base normalize () const { return this->normalize (default_upper); }
		base upper (bool includemidblank) const
		{
			return NormalizeString <ct, tr, al> (*this, true, includemidblank);
		}
		base upper () const { return this->upper (default_include_blank_in_str); }
		base lower (bool includemidblank) const
		{
			return NormalizeString <ct, tr, al> (*this, false, includemidblank);
		}
		base lower () const { return this->lower (default_include_blank_in_str); }
		base trim (bool includemidblank) const
		{
			return StringTrim <ct, tr, al> (*this, includemidblank);
		}
		base trim () const { return this->trim (default_include_blank_in_str); }
		size_t length (bool includemidblank) const { return GetNormalizeStringLength (*this, includemidblank); }
		size_t length () const { return length (default_include_blank_in_str); }
		bool empty () const
		{
			return IsNormalizeStringEmpty (*this);
		}
		bool equals (const base &another, bool includemidblank) const
		{
			return IsNormalizeStringEquals <ct, tr, al> (*this, another, includemidblank);
		}
		bool equals (const base &another) const { return equals (another, default_include_blank_in_str); }
		int64_t compare (const base &another, bool includemidblank) const
		{
			return NormalizeStringCompare <ct, tr, al> (*this, another, includemidblank);
		}
		int64_t compare (const base &another) const { return compare (another, default_include_blank_in_str); }
		base &string () { return *this; }
		base to_string (bool upper, bool includemidblank) const { return this->normalize (upper, includemidblank); }
		base to_string (bool upper) const { return this->normalize (upper, default_include_blank_in_str); }
		base to_string () const { return this->normalize (default_upper); }
		bool operator == (const base &other) const { return equals (other, false); }
		bool operator != (const base &other) const { return !equals (other, false); }
		bool operator < (const base &other) const { return compare (other, false) < 0; }
		bool operator > (const base &other) const { return compare (other, false) > 0; }
		bool operator <= (const base &other) const { return compare (other, false) <= 0; }
		bool operator >= (const base &other) const { return compare (other, false) >= 0; }
		int64_t operator - (const base &other) const { return compare (other, false); }
		template <typename E, typename TR = std::char_traits <E>, typename AL = std::allocator <E>>
		static bool equals (const std::basic_string <E> &l, const std::basic_string <E> &r, bool remove_mid_blank = false)
		{
			return IsNormalizeStringEquals <E, TR, AL> (l, r, remove_mid_blank);
		}
		template <typename E, typename TR = std::char_traits <E>, typename AL = std::allocator <E>>
		static int64_t compare (const std::basic_string <E> &l, const std::basic_string <E> &r, bool remove_mid_blank = false)
		{
			return NormalizeStringCompare <E, TR, AL> (l, r, remove_mid_blank);
		}
		template <typename E, typename TR = std::char_traits <E>, typename AL = std::allocator <E>>
		static std::basic_string <E, TR, AL> normalize (const std::basic_string <E> &str, bool to_upper = false, bool remove_mid_blank = false)
		{
			return NormalizeString <E, TR, AL> (str, to_upper, remove_mid_blank);
		}
		template <typename E, typename TR = std::char_traits <E>, typename AL = std::allocator <E>>
		static std::basic_string <E, TR, AL> trim (const std::basic_string <E> &str, bool remove_mid_blank = false)
		{
			return StringTrim <E, TR, AL> (str, remove_mid_blank);
		}
		template <typename E, typename TR = std::char_traits <E>, typename AL = std::allocator <E>>
		static size_t length (const std::basic_string <E> &str, bool remove_mid_blank = false)
		{
			return GetNormalizeStringLength <E, TR, AL> (str, remove_mid_blank);
		}
		template <typename E, typename TR = std::char_traits <E>, typename AL = std::allocator <E>>
		static bool empty (const std::basic_string <E> &str)
		{
			return IsNormalizeStringEmpty <E, TR, AL> (str);
		}
		template <typename E, typename TR = std::char_traits <E>, typename AL = std::allocator <E>>
		static std::basic_nstring <E, TR, AL> to_nstring (std::basic_string <E> &str) { return std::basic_nstring <E> (str); }
		template <typename E, typename TR = std::char_traits <E>, typename AL = std::allocator <E>>
		static std::basic_nstring <E, TR, AL> toupper (const std::basic_nstring <E, TR, AL> &str) { return l0km::toupper (str); }
		template <typename E, typename TR = std::char_traits <E>, typename AL = std::allocator <E>>
		static std::basic_nstring <E, TR, AL> tolower (const std::basic_nstring <E, TR, AL> &str) { return l0km::tolower (str); }
	};

	typedef basic_nstring <char> nstring;
	typedef basic_nstring <wchar_t> wnstring;
}