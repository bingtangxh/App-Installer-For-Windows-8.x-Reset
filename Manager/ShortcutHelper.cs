using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Manager
{
	public static class ShortcutHelper
	{
		public static void CreateShortcut (
			string shortcutPath,
			string targetPath,
			string arguments,
			string workingDirectory,
			string iconPath,
			string description,
			string appUserModelID)
		{
			IShellLinkW link = (IShellLinkW)new CShellLink ();

			link.SetPath (targetPath);

			if (!string.IsNullOrEmpty (arguments))
				link.SetArguments (arguments);

			if (!string.IsNullOrEmpty (workingDirectory))
				link.SetWorkingDirectory (workingDirectory);

			if (!string.IsNullOrEmpty (description))
				link.SetDescription (description);

			if (!string.IsNullOrEmpty (iconPath))
				link.SetIconLocation (iconPath, 0);

			if (!string.IsNullOrEmpty (appUserModelID))
			{
				IPropertyStore propertyStore = (IPropertyStore)link;

				PROPERTYKEY key = PROPERTYKEY.AppUserModel_ID;

				using (PropVariant pv = new PropVariant (appUserModelID))
				{
					propertyStore.SetValue (ref key, pv);
					propertyStore.Commit ();
				}
			}

			IPersistFile file = (IPersistFile)link;
			file.Save (shortcutPath, false);

			Marshal.ReleaseComObject (link);
		}

		#region COM 定义（全部放进类内部）

		[ComImport]
		[Guid ("00021401-0000-0000-C000-000000000046")]
		internal class CShellLink
		{
		}

		[ComImport]
		[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
		[Guid ("000214F9-0000-0000-C000-000000000046")]
		internal interface IShellLinkW
		{
			void GetPath ([Out, MarshalAs (UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, int fFlags);
			void GetIDList (out IntPtr ppidl);
			void SetIDList (IntPtr pidl);
			void GetDescription ([Out, MarshalAs (UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
			void SetDescription ([MarshalAs (UnmanagedType.LPWStr)] string pszName);
			void GetWorkingDirectory ([Out, MarshalAs (UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
			void SetWorkingDirectory ([MarshalAs (UnmanagedType.LPWStr)] string pszDir);
			void GetArguments ([Out, MarshalAs (UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
			void SetArguments ([MarshalAs (UnmanagedType.LPWStr)] string pszArgs);
			void GetHotkey (out short pwHotkey);
			void SetHotkey (short wHotkey);
			void GetShowCmd (out int piShowCmd);
			void SetShowCmd (int iShowCmd);
			void GetIconLocation ([Out, MarshalAs (UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
			void SetIconLocation ([MarshalAs (UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
			void SetRelativePath ([MarshalAs (UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
			void Resolve (IntPtr hwnd, int fFlags);
			void SetPath ([MarshalAs (UnmanagedType.LPWStr)] string pszFile);
		}

		[ComImport]
		[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
		[Guid ("0000010b-0000-0000-C000-000000000046")]
		internal interface IPersistFile
		{
			void GetClassID (out Guid pClassID);
			void IsDirty ();
			void Load ([MarshalAs (UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
			void Save ([MarshalAs (UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
			void SaveCompleted ([MarshalAs (UnmanagedType.LPWStr)] string pszFileName);
			void GetCurFile ([MarshalAs (UnmanagedType.LPWStr)] out string ppszFileName);
		}

		[ComImport]
		[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
		[Guid ("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
		internal interface IPropertyStore
		{
			uint GetCount (out uint cProps);
			uint GetAt (uint iProp, out PROPERTYKEY pkey);
			uint GetValue (ref PROPERTYKEY key, out PropVariant pv);
			uint SetValue (ref PROPERTYKEY key, PropVariant pv);
			uint Commit ();
		}

		[StructLayout (LayoutKind.Sequential, Pack = 4)]
		internal struct PROPERTYKEY
		{
			public Guid fmtid;
			public uint pid;

			public static PROPERTYKEY AppUserModel_ID =
				new PROPERTYKEY
				{
					fmtid = new Guid ("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
					pid = 5
				};
		}

		[StructLayout (LayoutKind.Sequential)]
		internal sealed class PropVariant: IDisposable
		{
			short vt;
			short wReserved1;
			short wReserved2;
			short wReserved3;
			IntPtr ptr;
			int int32;

			private const short VT_LPWSTR = 31;

			public PropVariant (string value)
			{
				vt = VT_LPWSTR;
				ptr = Marshal.StringToCoTaskMemUni (value);
			}

			public void Dispose ()
			{
				if (ptr != IntPtr.Zero)
				{
					Marshal.FreeCoTaskMem (ptr);
					ptr = IntPtr.Zero;
				}
			}
		}

		#endregion
	}
}
