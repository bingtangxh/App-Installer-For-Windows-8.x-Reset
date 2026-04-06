(function(global) {
    function isString(obj) {
        return typeof obj === "string" || obj instanceof String;
    }

    function isFunction(obj) {
        return typeof obj === "function";
    }

    function isBool(obj) {
        return typeof obj === "boolean" || obj instanceof Boolean;
    }

    function isNumber(obj) {
        return typeof obj === "number" || obj instanceof Number;
    }

    function isDate(obj) {
        return obj instanceof Date;
    }

    function isDevToolsOpen() {
        if (window.__IE_DEVTOOLBAR_CONSOLE_COMMAND_LINE) {
            return true; // IE10 或更早版本
        }
        if (window.__BROWSERTOOLS_CONSOLE) {
            return true; // IE11
        }
        if (window.__BROWSERTOOLS_DOMEXPLORER_ADDED) {
            return true; // IE11
        }
        return false;
    }
    var APIs = [
        "Windows.ApplicationModel.DesignMode.designModeEnabled",
        "Windows.ApplicationModel.Resources.Core.ResourceContext",
        "Windows.ApplicationModel.Resources.Core.ResourceManager",
        "Windows.ApplicationModel.Search.Core.SearchSuggestionManager",
        "Windows.ApplicationModel.Search.SearchQueryLinguisticDetails",
        "Windows.Data.Text.SemanticTextQuery",
        "Windows.Foundation.Collections.CollectionChange",
        "Windows.Foundation.Uri",
        "Windows.Globalization.ApplicationLanguages",
        "Windows.Globalization.Calendar",
        "Windows.Globalization.DateTimeFormatting",
        "Windows.Globalization.Language",
        "Windows.Phone.UI.Input.HardwareButtons",
        "Windows.Storage.ApplicationData",
        "Windows.Storage.CreationCollisionOption",
        "Windows.Storage.BulkAccess.FileInformationFactory",
        "Windows.Storage.FileIO",
        "Windows.Storage.FileProperties.ThumbnailType",
        "Windows.Storage.FileProperties.ThumbnailMode",
        "Windows.Storage.FileProperties.ThumbnailOptions",
        "Windows.Storage.KnownFolders",
        "Windows.Storage.Search.FolderDepth",
        "Windows.Storage.Search.IndexerOption",
        "Windows.Storage.Streams.RandomAccessStreamReference",
        "Windows.UI.ApplicationSettings.SettingsEdgeLocation",
        "Windows.UI.ApplicationSettings.SettingsCommand",
        "Windows.UI.ApplicationSettings.SettingsPane",
        "Windows.UI.Core.AnimationMetrics",
        "Windows.UI.Input.EdgeGesture",
        "Windows.UI.Input.EdgeGestureKind",
        "Windows.UI.Input.PointerPoint",
        "Windows.UI.ViewManagement.HandPreference",
        "Windows.UI.ViewManagement.InputPane",
        "Windows.UI.ViewManagement.UISettings",
        "Windows.UI.WebUI.Core.WebUICommandBar",
        "Windows.UI.WebUI.Core.WebUICommandBarBitmapIcon",
        "Windows.UI.WebUI.Core.WebUICommandBarClosedDisplayMode",
        "Windows.UI.WebUI.Core.WebUICommandBarIconButton",
        "Windows.UI.WebUI.Core.WebUICommandBarSymbolIcon",
        "Windows.UI.WebUI.WebUIApplication",
    ];
    for (var i = 0; i < APIs.length; i++) {
        var api = APIs[i];
        apiNs = api.split(".");
        var lastNs = global;
        for (var j = 0; j < apiNs.length - 1; j++) {
            var ns = apiNs[j];
            if (typeof lastNs[ns] === "undefined") lastNs[ns] = {};
            lastNs = lastNs[ns];
        }
        var leaf = apiNs[apiNs.length - 1];
        if ("abcdefghijklmnopqrstuvwxyz".indexOf(leaf[0]) < 0) {
            lastNs[leaf] = {};
            Object.defineProperty(lastNs[leaf], "current", {
                get: function() { return null; },
                enumerable: true,
            });
        }
    }
    Object.defineProperty(Windows.ApplicationModel.DesignMode, "designModeEnabled", {
        get: isDevToolsOpen,
        enumerable: true,
    });
    Windows.Foundation.Collections.CollectionChange = {
        reset: 0,
        itemInserted: 1,
        itemRemoved: 2,
        itemChanged: 3
    };
    Windows.Foundation.Uri = function(uri) {
        var inst = null;
        if (arguments.length === 2) {
            inst = external.Utilities.createUri2(uri, arguments[1]);
        } else {
            inst = external.Utilities.createUri(uri);
        }
        inst.jsType = this;
        return inst;
    };
    Windows.Globalization.ApplicationLanguages = {};
    Object.defineProperty(Windows.Globalization.ApplicationLanguages, "languages", {
        get: function() {
            try {
                var list = [];
                var langs = external.System.Locale.recommendLocaleNames;
                langs.forEach(function(item) {
                    list.push(item);
                });
                return list;
            } catch (e) {
                return navigator.languages;
            }
        },
        enumerable: true,
    });
    Object.defineProperty(Windows.Globalization.ApplicationLanguages, "Languages", {
        get: function() {
            return Windows.Globalization.ApplicationLanguages.languages;
        },
        enumerable: true,
    });
    Windows.Globalization.LanguageLayoutDirection = {
        ltr: 0,
        rtl: 1,
        ttbLtr: 2,
        ttbRtl: 3
    };
    Windows.Globalization.Language = function(languageTag) {
        var inst = external.System.Locale.createLanguage(languageTag);
        inst.jsType = this;
        return inst;
    };
    Object.defineProperty(Windows.Globalization.Language, "currentInputMethodLanguageTag", {
        get: function() {
            return external.System.Locale.currentInputMethodLanguageTag;
        },
        enumerable: true,
    });
    Windows.Globalization.Language.isWellFormed = external.System.Locale.isWellFormed;
    Windows.Globalization.Language.getMuiCompatibleLanguageListFromLanguageTags = external.System.Locale.getMuiCompatibleLanguageListFromLanguageTags;
    Windows.Globalization.Calendar = function(args) {
        var inst = null;
        if (arguments.length === 0) {
            inst = external.Utilities.createCalendar(args);
        } else if (arguments.length === 1) {
            inst = external.Utilities.createCalendar2(args);
        } else if (arguments.length === 3) {
            inst = external.Utilities.createCalendar3(args, arguments[1], arguments[2], arguments[3]);
        } else if (arguments.length === 4) {
            inst = external.Utilities.createCalendar4(args, arguments[1], arguments[2], arguments[3], arguments[4]);
        }
        return inst;
    };
    Windows.Globalization.DateTimeFormatting = {};
    Windows.Globalization.DateTimeFormatting.DayFormat = {
        default: 1,
        none: 0
    };
    Windows.Globalization.DateTimeFormatting.DayOfWeekFormat = {
        none: 0,
        default: 1,
        abberviated: 2,
        full: 3
    };
    Windows.Globalization.DateTimeFormatting.HourFormat = {
        none: 0,
        default: 1
    };
    Windows.Globalization.DateTimeFormatting.MinuteFormat = {
        none: 0,
        default: 1
    };
    Windows.Globalization.DateTimeFormatting.MonthFormat = {
        none: 0,
        default: 1,
        abberviated: 2,
        full: 3,
        numeric: 4
    };
    Windows.Globalization.DateTimeFormatting.SecondFormat = {
        default: 1,
        none: 0
    };
    Windows.Globalization.DateTimeFormatting.YearFormat = {
        default: 1,
        none: 0,
        abberviated: 2,
        full: 3,
    };
    Windows.Globalization.DateTimeFormatting.DateTimeFormatter = function() {
        var args = Array.prototype.slice.call(arguments);
        var inst = null;
        if (args.length === 1) inst = external.Utilities.createDateTimeFormatterFromTemplate(args[0]);
        else if (args.length === 2) inst = external.Utilities.CreateDateTimeFormatterFromTemplateAndLanguages(args[0], args[1]);
        else if (args.length === 3) inst = external.Utilities.CreateDateTimeFormatterFromTimeEnums(args[0], args[1], args[2]);
        else if (args.length === 4) inst = external.Utilities.CreateDateTimeFormatterFromDateEnums(args[0], args[1], args[2], args[3]);
        else if (args.length === 5) inst = external.Utilities.CreateDateTimeFormatterFromTemplateFull(args[0], args[1], args[2], args[3], args[4]);
        else if (args.length === 8) inst = external.Utilities.CreateDateTimeFormatterFromDateTimeEnums(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]);
        else if (args.length === 11) inst = external.Utilities.CreateDateTimeFormatterFromDateTimeEnumsFull(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10]);
        return inst;
    };
    Windows.Storage.CreationCollisionOption = {
        generateUniqueName: 0,
        replaceExisting: 1,
        failIfExists: 2,
        openIfExists: 3
    };
    Windows.Storage.FileProperties = {
        ThumbnailType: {
            image: 0,
            icon: 1
        },
        ThumbnailMode: {
            pictureView: 0,
            videosView: 1,
            musicView: 2,
            documentsView: 3,
            listView: 4,
            singleItem: 5
        },
        ThumbnailType: {
            image: 0,
            icon: 1
        },
        VideoOrientation: {
            normal: 0,
            rotate90: 90,
            rotate180: 180,
            rotate270: 270
        },
        PhotoOrientation: {
            unspecified: 0,
            normal: 1,
            flipHorizontal: 2,
            rotate180: 3,
            flipVertical: 4,
            transpose: 5,
            rotate270: 6,
            transverse: 7,
            rotate90: 8
        },
    };
    Windows.Storage.Search = {
        FolderDepth: function() {},
        IndexerOption: function() {}
    };
    Windows.Storage.Search.FolderDepth.deep = 1;
    Windows.Storage.Search.FolderDepth.shallow = 0;
    Windows.Storage.Search.IndexerOption.useIndexerWhenAvailable = 0;
    Windows.Storage.Search.IndexerOption.onlyUseIndexer = 1;
    Windows.Storage.Search.IndexerOption.doNotUseIndexer = 2;
    Windows.Storage.Search.IndexerOption.onlyUseIndexerAndOptimizeForIndexedProperties = 3;
    Windows.UI.ApplicationSettings.SettingsEdgeLocation = {
        right: 0,
        left: 1
    };
    Windows.UI.Input.EdgeGestureKind = {
        touch: 0,
        keyboard: 1,
        mouse: 2
    };
    Windows.UI.ViewManagement.HandPreference = {
        leftHanded: 0,
        rightHanded: 1
    };
    Windows.UI.WebUI.Core.WebUICommandBarClosedDisplayMode = {
        default: 0,
        minimal: 1,
        compact: 2
    };
})(this);