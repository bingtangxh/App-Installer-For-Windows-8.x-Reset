(function(global) {
    var strres = external.StringResources;
    var conf = external.Config.current;
    var set = conf.getSection("Settings");

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
    OnLoad.add(function() {
        var mgr = Package.manager;
        var nstr = Bridge.NString;
        var datasrc = new DataView.DataSource();
        datasrc.setKeySelector(function(item) {
            if (item === null || item === void 0) return null;
            return Bridge.String.tolower(Bridge.String.trim(item.Identity.FullName));
        });
        var themeColor = Bridge.UI.themeColor;
        var appbar = document.getElementById("appBar");
        var appbarControl = new AppBar.AppBar(appbar);
        appbarControl.enabled = false;
        pagemgr.register("reader", document.getElementById("tag-reader"), document.getElementById("page-reader"));
        pagemgr.go("reader");
    });
})(this);