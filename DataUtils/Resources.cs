using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DataUtils
{
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Resources
	{
		// Flags for LoadLibraryEx
		private const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
		private const uint LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020;

		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern IntPtr LoadLibraryEx (string lpFileName, IntPtr hFile, uint dwFlags);

		[DllImport ("kernel32.dll", SetLastError = true)]
		[return: MarshalAs (UnmanagedType.Bool)]
		private static extern bool FreeLibrary (IntPtr hModule);

		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern IntPtr GetModuleHandle (string lpModuleName);

		[DllImport ("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern int LoadString (IntPtr hInstance, uint uID, StringBuilder lpBuffer, int nBufferMax);

		/// <summary>
		/// Load a string resource from another module (DLL/EXE) identified by file path, or from the current module if filepath is null/empty.
		/// </summary>
		/// <param name="filepath">Path to the module file. If null/empty, use current process module.</param>
		/// <param name="resid">Resource ID to load.</param>
		/// <returns>The resource string, or empty string if not found or on error.</returns>
		public string GetFromOthers (string filepath, uint resid)
		{
			IntPtr hModule = IntPtr.Zero;
			bool needFree = false;

			try
			{
				if (!string.IsNullOrWhiteSpace (filepath))
				{
					// Load as datafile + image resource so we can access resources without executing DllMain of the module.
					hModule = LoadLibraryEx (filepath, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE | LOAD_LIBRARY_AS_IMAGE_RESOURCE);
					if (hModule == IntPtr.Zero)
					{
						// Failed to load; return empty string
						return string.Empty;
					}
					needFree = true;
				}
				else
				{
					// Get handle of current process module (exe)
					hModule = GetModuleHandle (null);
					if (hModule == IntPtr.Zero)
						return string.Empty;
				}

				// Prepare buffer. Typical string resources are not huge; 4096 should be enough.
				const int BUFFER_SIZE = 4096;
				StringBuilder sb = new StringBuilder (BUFFER_SIZE);

				int copied = LoadString (hModule, resid, sb, sb.Capacity);
				if (copied > 0)
				{
					// LoadString returns number of characters copied (excluding terminating null)
					return sb.ToString (0, copied);
				}
				return string.Empty;
			}
			finally
			{
				if (needFree && hModule != IntPtr.Zero)
				{
					try { FreeLibrary (hModule); }
					catch { /* ignore */ }
				}
			}
		}
	}
}
