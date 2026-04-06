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
        pagemgr.register("reader", document.getElementById("tag-reader"), document.getElementById("page-reader"));
        pagemgr.register("acquire", document.getElementById("tag-acquire"), document.getElementById("page-acquire"));
        pagemgr.register("search", document.getElementById("tag-search"), document.getElementById("page-search"));
        pagemgr.go("reader");
    });
})(this);