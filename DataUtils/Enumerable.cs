using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace DataUtils
{
	[ComVisible (true)]
	[InterfaceType (ComInterfaceType.InterfaceIsDual)]
	public interface _I_Enumerable
	{
		int Length { get; set; }
		object this [int index] { get; set; }
		void Add (object value);        // push
		void Push (object value);
		object Pop ();                  // 删除并返回末尾元素
		object Shift ();                // 删除并返回开头元素
		void Unshift (object value);    // 在开头插入
		void RemoveAt (int index);      // 删除任意索引
		void Clear ();                  // 清空数组
		_I_Enumerable Slice (int start, int end);                     // 返回子数组
		void Splice (int start, int deleteCount, object [] items);     // 删除并插入
		int IndexOf (object value);      // 查找索引
		bool Includes (object value);    // 是否包含
		void ForEach (object callback, bool suppressExceptions = false);  // 遍历，callback(item, index)
		_I_Enumerable Concat (_I_Enumerable other);  // 拼接
		string Join (string separator);   // 转字符串
		object GetItem (int index);           // 返回 { key, data } 或直接 data
		void SetAt (int index, object value); // 替换元素
		int IndexOfKey (int key);             // 按内部 key 查找
		void Move (int index, int newIndex);  // 移动元素
		void PushAll (object [] items);        // 一次性 push 多个
	}
	public class _I_List: _I_Enumerable, IList
	{
		public _I_List (IEnumerable<object> initArr, bool readOnly = false, bool fixedSize = false, bool sync = true)
		{
			_list = initArr?.ToList () ?? new List<object> ();
			IsFixedSize = fixedSize;
			IsReadOnly = readOnly;
			IsSynchronized = sync;
		}
		protected List<object> _list;
		protected object _lock = new object ();
		public object this [int index] { get { return _list [index]; } set { _list [index] = value; } }
		public int Count => _list.Count;
		public bool IsFixedSize { get; }
		public bool IsReadOnly { get; }
		public bool IsSynchronized { get; }
		public int Length
		{
			get { return _list.Count; }
			set
			{
				if (!IsFixedSize && !IsReadOnly)
				{
					_list.Capacity = value;
				}
			}
		}
		public object SyncRoot => _lock;
		public void Add (object value) => _list.Add (value);
		public void Push (object value) => _list.Add (value);
		public void RemoveAt (int index) => _list.RemoveAt (index);
		public void Clear () => _list.Clear ();
		public int IndexOf (object value) => _list.IndexOf (value);
		int IList.Add (object value)
		{
			_list.Add (value);
			return _list.Count - 1;
		}
		public bool Contains (object value) => _list.Contains (value);
		public void Insert (int index, object value) => _list.Insert (index, value);
		public void Remove (object value) => _list.Remove (value);
		public void ForEach (object callback, bool suppressExceptions = false)
		{
			for (int i = 0; i < _list.Count; i++)
			{
				var item = _list [i];
				if (suppressExceptions)
				{
					try { JsUtils.Call (callback, item, i); } catch { }
				}
				else JsUtils.Call (callback, item, i);
			}
		}
		public IEnumerator GetEnumerator () => _list.GetEnumerator ();
		public _I_Enumerable Slice (int start, int end)
		{
			if (end < 0) end = _list.Count + end;
			start = Math.Max (0, start);
			end = Math.Min (_list.Count, end);
			var arr = _list.GetRange (start, Math.Max (0, end - start));
			return new _I_List (arr);
		}
		public void Splice (int start, int deleteCount, object [] items)
		{
			if (start < 0) start = Math.Max (0, _list.Count + start);
			int count = Math.Min (deleteCount, _list.Count - start);
			_list.RemoveRange (start, count);
			if (items != null && items.Length > 0)
				_list.InsertRange (start, items);
		}
		public bool Includes (object value) => _list.Contains (value);
		public _I_Enumerable Concat (_I_Enumerable other)
		{
			var newList = new List<object> (_list);
			if (other is _I_List)
                newList.AddRange ((other as _I_List)._list);
			return new _I_List (newList);
		}
		public string Join (string separator)
		{
			return string.Join (separator ?? ",", _list.Select (x => x?.ToString () ?? ""));
		}
		public object GetItem (int index) => _list [index];
		public void SetAt (int index, object value) => _list [index] = value;
		public int IndexOfKey (int key)
		{
			return key >= 0 && key < _list.Count ? key : -1;
		}
		public void Move (int index, int newIndex)
		{
			if (index < 0 || index >= _list.Count || newIndex < 0 || newIndex >= _list.Count) return;
			var item = _list [index];
			_list.RemoveAt (index);
			_list.Insert (newIndex, item);
		}
		public void PushAll (object [] items)
		{
			if (items == null || items.Length == 0) return;
			_list.AddRange (items);
		}
		public void CopyTo (Array array, int index)
		{
			if (array == null) throw new ArgumentNullException (nameof (array));
			for (int i = 0; i < _list.Count && index + i < array.Length; i++)
				array.SetValue (_list [i], index + i);
		}
		public object Pop ()
		{
			if (_list.Count == 0) return null;
			var last = _list [_list.Count - 1];
			_list.RemoveAt (_list.Count - 1);
			return last;
		}
		public object Shift ()
		{
			if (_list.Count == 0) return null;
			var first = _list [0];
			_list.RemoveAt (0);
			return first;
		}
		public void Unshift (object value) => _list.Insert (0, value);
	}
}
