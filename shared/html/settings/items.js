(function(global) {
    "use strict";
    var res = Bridge.Resources;
    var storage = Bridge.External.Storage;
    var path = storage.path;
    var root = path.getDir(path.program);

    function getLibRes(libfilename, resid) {
        var libpath = path.combine(root, libfilename);
        return res.fromfile(libpath, resid);
    }

    function getStringRes(resid) {
        return Bridge.External.StringResources.getString(resid);
    }

    function getSettingsItem(page, displayName) {
        return {
            page: page,
            displayName: displayName
        };
    }
    var settingItems = {
        appinstaller: getSettingsItem("appinstaller.html", getLibRes("appinstaller.exe", 300)),
        manager: getSettingsItem("manager.html", getStringRes("MANAGER_APPTITLE")),
        settings: getSettingsItem("settings.html", getLibRes("settings.exe", 200)),
    };
    Object.defineProperty(global, "settingPages", {
        get: function() { return settingItems; }
    });
})(this);