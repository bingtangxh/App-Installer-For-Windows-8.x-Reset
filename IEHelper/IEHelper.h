// IEHelper.h

#pragma once

using namespace System;
using IEWebView = System::Windows::Forms::WebBrowser;
namespace IEHelper {
	public ref class WebBrowserHelper
	{
		public:
		static int GetPageScale (IEWebView ^webui);
		static void SetPageScale (IEWebView ^webui, int zoom);
	};
}
