// 这是主 DLL 文件。

#include "stdafx.h"

#include "IEHelper.h"

using namespace System::Runtime::InteropServices;
HRESULT GetWebBrowser2Interface (System::Windows::Forms::WebBrowser ^fwb, IWebBrowser2 **output)
{
	if (fwb == nullptr || output == nullptr) return E_INVALIDARG;
	*output = nullptr;
	Object ^activeX = fwb->ActiveXInstance;
	if (activeX == nullptr) return E_FAIL;
	IntPtr pUnk = Marshal::GetIUnknownForObject (activeX);
	if (pUnk == IntPtr::Zero) return E_FAIL;
	HRESULT hr = ((IUnknown *)pUnk.ToPointer ())->QueryInterface (IID_IWebBrowser2, (void **)output);
	Marshal::Release (pUnk);
	return hr;
}
int IEHelper::WebBrowserHelper::GetPageScale (IEWebView ^webui)
{
	CComPtr <IWebBrowser2> web2;
	HRESULT hr = GetWebBrowser2Interface (webui, &web2);
	if (FAILED (hr)) return 0;
	VARIANT v;
	VariantInit (&v);
	hr = web2->ExecWB (OLECMDID_OPTICAL_ZOOM, OLECMDEXECOPT_DODEFAULT, nullptr, &v);
	if (FAILED (hr) || v.vt != VT_I4) return 0;
	int val = v.lVal;
	VariantClear (&v);
	return val;
}
void IEHelper::WebBrowserHelper::SetPageScale (IEWebView ^webui, int value)
{
	CComPtr <IWebBrowser2> web2;
	HRESULT hr = GetWebBrowser2Interface (webui, &web2);
	if (FAILED (hr)) return;
	VARIANT v;
	VariantInit (&v);
	v.vt = VT_I4;
	v.lVal = value;
	web2->ExecWB (OLECMDID_OPTICAL_ZOOM, OLECMDEXECOPT_DONTPROMPTUSER, &v, nullptr);
}