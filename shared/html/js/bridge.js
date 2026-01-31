(function(global) {
    "use strict";
    var ext = global.external;

    function blankFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10) {
        console.log("blankFunc called with arguments: " + arg1 + ", " + arg2 + ", " + arg3 + ", " + arg4 + ", " + arg5 + ", " + arg6 + ", " + arg7 + ", " + arg8 + ", " + arg9 + ", " + arg10);
    }

    global.Bridge = {
        External: ext,
        Frame: {
            isIe10: function() { return ext.IEFrame.Version === 10; },
            isIe11: function() { return ext.IEFrame.Version === 11; },
            callEvent: function(funcName, e) {
                ext.Window.CallEvent(funcName, e);
            }
        },
        UI: {
            Splash: {
                show: function() { ext.System.UI.ShowSplash(); },
                fadeAway: function() { ext.System.UI.FadeAwaySplash(); },
                fadeOut: function() { ext.System.UI.FadeOutSplash(); }
            }
        },
        String: ext.String,
        /*         {
                    trim: function(str) { return ext.String.Trim(str); },
                    tolower: function(str) { return ext.String.ToLower(str); },
                    toupper: function(str) { return ext.String.ToUpper(str); },
                }, */
        NString: {
            equals: function(str1, str2) { return ext.String.NString.NEquals(str1, str2); },
            compare: function(str1, str2) { return ext.String.NString.Compare(str1, str2); },
            empty: function(str) { return ext.String.NString.Empty(str); },
            length: function(str) { return ext.String.NString.Length(str); },
        },
        Resources: {
            byname: function(resname) { return ext.System.Resources.GetByName(resname); },
            byid: function(resid) { return ext.System.Resources.GetById(resid); },
            nameToId: function(resname) { return ext.System.Resources.ToId(resname); },
            idToName: function(resid) { return ext.System.Resources.ToName(resid); },
            fromOthers: function(filepath, resid) { return ext.System.Resources.GetFromOthers(filepath, resid); },
            fromFile: function(filepath, resid) { return ext.System.Resources.GetFromOthers(filepath, resid); },
            fromfile: function(filepath, resid) { return ext.System.Resources.GetFromOthers(filepath, resid); },
        },
        Package: {
            filepaths: function() {
                return JSON.parse(ext.Package.GetPackagesToJson() || "[]");
            },
            pkginfo: function(filepath) {
                return JSON.parse(ext.Package.GetPackageInfoToJson(filepath) || "{}");
            },
            capabilityDisplayName: function(capability) {
                return ext.Package.GetCapabilityDisplayName(capability);
            },
            installResult: function(filepath) {
                return ext.Package.GetPackageInstallResult(filepath);
            },
        },
        Locale: {
            toLcid: function(localeName) { return ext.System.Locale.ToLcid(localeName); },
            toLocaleName: function(lcid) { return ext.System.Locale.ToLocaleName(lcid); },
            localeInfo: function(lcid, lcType) { return ext.System.Locale.LocaleInfo(lcid, lcType); },
            localeInfoEx: function(localeName, lcType) { return ext.System.Locale.LocaleInfoEx(localeName, lcType); }
        }
    };
    Object.defineProperty(global.Bridge.Frame, "scale", {
        get: function() { return ext.IEFrame.Scale; },
        set: function(value) { ext.IEFrame.Scale = value; return ext.IEFrame.Scale; }
    });
    Object.defineProperty(global.Bridge.Frame, "version", {
        get: function() { return ext.IEFrame.Version; },
    });
    Object.defineProperty(global.Bridge.Frame, "WindowSize", {
        get: function() { return ext.System.UI.WndSize; },
    });
    Object.defineProperty(global.Bridge.Frame, "ClientSize", {
        get: function() { return ext.System.UI.ClientSize; },
    });
    Object.defineProperty(global.Bridge.UI, "dpiPercent", {
        get: function() { return ext.System.UI.DPIPercent; }
    });
    Object.defineProperty(global.Bridge.UI, "dpi", {
        get: function() { return ext.System.UI.DPI; }
    });
    Object.defineProperty(global.Bridge.UI, "themeColor", {
        get: function() { return ext.System.UI.ThemeColor; }
    });
    Object.defineProperty(global.Bridge.UI, "contrast", {
        get: function() { return ext.System.UI.HighContrast; }
    });
    Object.defineProperty(global.Bridge.UI, "darkmode", {
        get: function() { return ext.System.UI.DarkMode; }
    });
    try {
        Object.defineProperty(global.Bridge.UI.Splash, "backcolor", {
            get: function() { return ext.System.UI.SplashBackgroundColor; },
        });
    } catch (e) {}
    try {
        Object.defineProperty(global.Bridge.UI.Splash, "imageurl", {
            get: function() { return ext.System.UI.SplashImage; },
        });
    } catch (e) {}
    // 下面是有关 String 方法的补充
    if (typeof String.prototype.trim !== "function") {
        String.prototype.trim = function() {
            return Bridge.String.trim(this);
        };
    }
    if (typeof String.prototype.toLowerCase !== "function") {
        String.prototype.toLowerCase = function() {
            return Bridge.String.tolower(this);
        };
    }
    if (typeof String.prototype.toUpperCase !== "function") {
        String.prototype.toUpperCase = function() {
            return Bridge.String.toupper(this);
        };
    }
})(this);