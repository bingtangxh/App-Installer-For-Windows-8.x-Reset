using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DataUtils;
using Newtonsoft.Json;

namespace Manager
{
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class BridgeExt: Bridge._I_BridgeBase
	{
		Form currentWnd = null;
		public BridgeExt (Form wnd, IScriptBridge isc, IWebBrowserPageScale iwbps, ITaskbarProgress itp) : base (wnd, isc, iwbps, itp)
		{
			currentWnd = wnd;
		}
		private static void CallJS (object jsFunc, params object [] args)
		{
			if (jsFunc == null) return;
			try
			{
				// 这里固定第一个参数为 thisArg（比如 1）
				object [] realArgs = new object [args.Length + 1];
				realArgs [0] = jsFunc;     // thisArg
				Array.Copy (args, 0, realArgs, 1, args.Length);

				jsFunc.GetType ().InvokeMember (
					"call",
					BindingFlags.InvokeMethod,
					null,
					jsFunc,
					realArgs
				);
			}
			catch
			{
				// ignore errors in callback invocation
			}
		}
		public Task CreateAppShortcut (string installLocation, string appUserModelId, object successCallback, object failedCallback)
		{
			var tcs = new TaskCompletionSource<bool> ();
			var scf = new ShortcutCreateForm ();
			scf.Owner = currentWnd;
			scf.FormClosed += (s, e) =>
			{
				bool success = scf.IsSuccess;
				tcs.TrySetResult (success);
				var data = new
				{
					succeeded = scf.IsSuccess,
					message = scf.Message
				};
				string json = JsonConvert.SerializeObject (data);
				if (currentWnd.InvokeRequired)
				{
					currentWnd.BeginInvoke (new Action (() =>
					{
						if (success)
							CallJS (successCallback, json);
						else
							CallJS (failedCallback, json);
					}));
				}
				else
				{
					if (success)
						CallJS (successCallback, json);
					else
						CallJS (failedCallback, json);
				}
				scf.Dispose ();
			};
			scf.Show (currentWnd);
			scf.InitCreater (installLocation, appUserModelId);
			return tcs.Task;
		}
	}
}
