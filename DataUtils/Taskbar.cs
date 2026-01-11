using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
		private readonly ITaskbarList3 _taskbar;
		private readonly IntPtr _hwnd;
		public TaskbarProgress (IntPtr hwnd)
		{
			_hwnd = hwnd;
			_taskbar = (ITaskbarList3)new TaskbarList ();
			_taskbar.HrInit ();
		}
		public void SetState (TBPFLAG state)
		{
			_taskbar.SetProgressState (_hwnd, state);
		}
		public void SetValue (ulong completed, ulong total)
		{
			_taskbar.SetProgressValue (_hwnd, completed, total);
		}
		public void Clear ()
		{
			_taskbar.SetProgressState (_hwnd, TBPFLAG.TBPF_NOPROGRESS);
		}
		public void Dispose ()
		{
			Clear ();
			Marshal.ReleaseComObject (_taskbar);
		}
	}
}
