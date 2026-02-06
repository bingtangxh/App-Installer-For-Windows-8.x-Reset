(function(global) {
    "use strict";
    var storage = Bridge.External.Storage;
    var path = storage.path;
    var root = path.getDir(path.program);
    var respath = path.combine(root, "reslib.dll");
    var res = Bridge.Resources;
    global.respath = respath;
    global.getPublicRes = function(resId) {
        return res.fromfile(respath, resId);
    }
    global.publicRes = function(resId) {
        return getFileResPair(respath, resId);
    }
    global.stringRes = function(resId) {
        return Bridge.External.StringResources.getString(resId);
    }

    function ready(e) {
        function nextstep() {
            Resources.processAll();
        }
        if (typeof WinJS !== "undefined") WinJS.UI.processAll().done(nextstep);
        else nextstep();
    }
    OnLoad.add(ready);
})(this);