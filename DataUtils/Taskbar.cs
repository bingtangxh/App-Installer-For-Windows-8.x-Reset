using System;
using System.Runtime.InteropServices;

namespace DataUtils
{
	public enum TBPFLAG
	{
		TBPF_NOPROGRESS = 0x0,
		TBPF_INDETERMINATE = 0x1,
		TBPF_NORMAL = 0x2,
		TBPF_ERROR = 0x4,
		TBPF_PAUSED = 0x8
	}
	[ComImport]
	[Guid ("EA1AFB91-9E28-4B86-90E9-9E9F8A5EEA84")]
	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
	public interface ITaskbarList3
	{
		void HrInit ();
		void AddTab (IntPtr hwnd);
		void DeleteTab (IntPtr hwnd);
		void ActivateTab (IntPtr hwnd);
		void SetActiveAlt (IntPtr hwnd);
		void MarkFullscreenWindow (IntPtr hwnd, [MarshalAs (UnmanagedType.Bool)] bool fFullscreen);
		void SetProgressValue (IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
		void SetProgressState (IntPtr hwnd, TBPFLAG tbpFlags);
		void RegisterTab (IntPtr hwndTab, IntPtr hwndMDI);
		void UnregisterTab (IntPtr hwndTab);
		void SetTabOrder (IntPtr hwndTab, IntPtr hwndInsertBefore);
		void SetTabActive (IntPtr hwndTab, IntPtr hwndMDI, uint dwReserved);
		void ThumbBarAddButtons (IntPtr hwnd, uint cButtons, IntPtr pButtons);
		void ThumbBarUpdateButtons (IntPtr hwnd, uint cButtons, IntPtr pButtons);
		void ThumbBarSetImageList (IntPtr hwnd, IntPtr himl);
		void SetOverlayIcon (IntPtr hwnd, IntPtr hIcon, string pszDescription);
		void SetThumbnailTooltip (IntPtr hwnd, string pszTip);
		void SetThumbnailClip (IntPtr hwnd, ref RECT prcClip);
	}
	[ComImport]
	[Guid ("56FDF344-FD6D-11d0-958A-006097C9A090")]
	public class TaskbarList { }
	[StructLayout (LayoutKind.Sequential)]
	public struct RECT
	{
		public int left;
		public int top;
		public int right;
		public int bottom;
	}
	public sealed class TaskbarProgress: IDisposable
	{
		private readonly IntPtr _hwnd;
		private ITaskbarList3 _taskbar3;
		public TaskbarProgress (IntPtr hwnd)
		{
			_hwnd = hwnd;
			object obj = new TaskbarList ();
			ITaskbarList baseTaskbar = (ITaskbarList)obj;
			baseTaskbar.HrInit ();
			_taskbar3 = obj as ITaskbarList3;
		}
		public bool IsSupported
		{
			get { return _taskbar3 != null; }
		}
		public void SetState (TBPFLAG state)
		{
			if (_taskbar3 != null)
				_taskbar3.SetProgressState (_hwnd, state);
		}
		public void SetValue (ulong completed, ulong total)
		{
			if (_taskbar3 != null)
				_taskbar3.SetProgressValue (_hwnd, completed, total);
		}
		public void Clear ()
		{
			if (_taskbar3 != null)
				_taskbar3.SetProgressState (_hwnd, TBPFLAG.TBPF_NOPROGRESS);
		}
		public void Dispose ()
		{
			if (_taskbar3 != null)
			{
				Marshal.ReleaseComObject (_taskbar3);
				_taskbar3 = null;
			}
		}
		~TaskbarProgress () { Dispose (); }
		public ITaskbarList3 Instance => _taskbar3;
	}
	[ComImport]
	[Guid ("56FDF342-FD6D-11d0-958A-006097C9A090")]
	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
	public interface ITaskbarList
	{
		void HrInit ();
		void AddTab (IntPtr hwnd);
		void DeleteTab (IntPtr hwnd);
		void ActivateTab (IntPtr hwnd);
		void SetActiveAlt (IntPtr hwnd);
	}
	public interface ITaskbarProgress
	{
		double ProgressValue { set; }
		TBPFLAG ProgressStatus { set; }
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Taskbar
	{
		private ITaskbarProgress taskbar = null;
		public double Progress { set { if (taskbar == null) return; taskbar.ProgressValue = value; } }
		public int Status { set { if (taskbar == null) return; taskbar.ProgressStatus = (TBPFLAG)value; } }
		public _I_Taskbar (ITaskbarProgress tp) { taskbar = tp; }
	}
}
