(function(global) {
    var strres = external.StringResources;
    var conf = external.Config.current;
    var set = conf.getSection("Settings");
    var metadata = conf.getSection("PackageReader:AppMetadatas");

    function createLocalizedCompare(locale) {
        return function(a, b) {
            a = a || "";
            b = b || "";

            return a.localeCompare(b, locale, {
                numeric: true, // 2 < 10
                sensitivity: "base" // 不区分大小写 / 重音
            });
        };
    }
    var pagemgr = new PageManager();
    (function() {
        var nstrutil = Bridge.NString;
        var boolTrue = ["true", "1", "yes", "on", "y", "t", "zhen", "真"];
        var boolFalse = ["false", "0", "no", "off", "n", "f", "jia", "假"];
        global.parseBool = function(str) {
            if (typeof str === "boolean") return str;
            str = "" + str;
            for (var i = 0; i < boolTrue.length; i++) {
                if (nstrutil.equals(str, boolTrue[i])) {
                    return true;
                }
            }
            for (var i = 0; i < boolFalse.length; i++) {
                if (nstrutil.equals(str, boolFalse[i])) {
                    return false;
                }
            }
            return null;
        };
    })();
    OnLoad.add(function() {
        var mgr = Package.manager;
        var nstr = Bridge.NString;
        var datasrc = new DataView.DataSource();
        datasrc.setKeySelector(function(item) {
            if (item === null || item === void 0) return null;
            return Bridge.String.tolower(Bridge.String.trim(item.Identity.FullName));
        });
        var themeColor = Bridge.UI.themeColor;
        var reader = Package.reader;
        var appitems = [
            "Id",
            "StartPage",
            "EntryPoint",
            "Executable",
            "BackgroundColor",
            "DisplayName",
            "Description",
            "ShortName",
            "ForegroundText",
            "SmallLogo",
            "Square30x30Logo",
            "Square44x44Logo",
            "Square70x70Logo",
            "Square71x71Logo",
            "Logo",
            "Square150x150Logo",
            "WideLogo",
            "Wide310x150Logo",
            "Square310x310Logo",
            "Tall150x310Logo",
            "LockScreenLogo",
            "LockScreenNotification",
            "DefaultSize",
            "AppListEntry",
            "VisualGroup",
            "MinWidth",
        ];
        var defaultItems = [
            "Id",
            "DisplayName",
            "BackgroundColor",
            "ForegroundText",
            "ShortName",
            "Square44x44Logo",
            "SmallLogo"
        ];
        var metaitemlist = [];
        for (var i = 0; i < appitems.length; i++) {
            var item = appitems[i];
            var isenable = metadata.getKey(item).value;
            if (isenable === null || isenable === void 0 || isenable === "") {
                isenable = defaultItems.indexOf(item) >= 0;
            }
            if (parseBool(isenable) == true) {
                metaitemlist.push(item);
            }
        }
        reader.updateApplicationReadItems(metaitemlist);
        pagemgr.register("reader", document.getElementById("tag-reader"), document.getElementById("page-reader"));
        pagemgr.register("acquire", document.getElementById("tag-acquire"), document.getElementById("page-acquire"));
        pagemgr.register("search", document.getElementById("tag-search"), document.getElementById("page-search"));
        pagemgr.go("reader");
    });
})(this);