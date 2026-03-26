(function(global) {
    "use strict";
    var pkg_ns = external.Package;
    var strres = external.StringResources;

    function archsToStr(archs) {
        var arr = [];
        for (var i = 0; i < archs.length; i++) {
            switch (archs[i]) {
                case 0:
                    arr.push("x86");
                    break;
                case 5:
                    arr.push("ARM");
                    break;
                case 9:
                    arr.push("x64");
                    break;
                case 11:
                    arr.push("Neutral");
                    break;
                case 12:
                    arr.push("ARM64");
                    break;
                case 65535:
                    arr.push("Unknown");
                    break;
            }
        }
        return arr.join(", ");
    }
    var showAppDetailTimer = null;

    function updateAppDataSource(page, result, bar) {
        try {
            var json = result.json;
            console.log(json);
            page.appDataSource.updateList(json.applications);
        } catch (e) {}
        showAppDetailTimer = setTimeout(function() {
            showAppDetailTimer = null;
            bar.hide();
        }, 3000);
    }

    function setAppInfoPageContent(info) {
        var page = document.getElementById("page-appinfo");
        page.data = info;
        page.querySelector(".display-name").textContent = info.Properties.DisplayName;
        page.querySelector(".publisher-display-name").textContent = info.Properties.Publisher;
        page.querySelector(".version").textContent = info.Identity.Version.Expression;
        page.querySelector(".description").textContent = info.Properties.Description;
        page.querySelector(".identity .name").textContent = info.Identity.Name;
        page.querySelector(".identity .publisher").textContent = info.Identity.Publisher;
        page.querySelector(".identity .publisher-id").textContent = info.Identity.PublisherId;
        page.querySelector(".identity .family-name").textContent = info.Identity.FamilyName;
        page.querySelector(".identity .full-name").textContent = info.Identity.FullName;
        page.querySelector(".identity .architecture").textContent = archsToStr(info.Identity.ProcessArchitecture);
        var il = info.InstallLocation;
        try { page.appDataSource.clear(); } catch (e) {}
        var appLoading = page.querySelector("#appinfo-loading");
        appLoading.classList.remove("noloading");
        if (showAppDetailTimer) clearTimeout(showAppDetailTimer);
        if (typeof appLoading.bar === "undefined") {
            appLoading.bar = new TransitionPanel(appLoading, {
                axis: 'y',
                duration: 500,
            });
        }
        appLoading.bar.show();
        var appLoadingStatus = page.querySelector(".title");
        appLoadingStatus.textContent = strres.get("MANAGER_APP_INSTALLEDAPPS_LOADING");
        return Package.reader.readFromInstallLocation(il, true).then(
            function(result) {
                try {
                    var displayNameNode = page.querySelector(".display-name");
                    displayNameNode.textContent = displayNameNode.textContent || result.json.properties.display_name;
                    if ((displayNameNode.textContent || "").indexOf("ms-resource:") === 0) {
                        displayNameNode.textContent = "";
                    }
                    if (result.json.applications.length === 1) {
                        displayNameNode.textContent = displayNameNode.textContent || result.json.applications[0].DisplayName || result.json.applications[0].ShortName;
                    }
                    if ((displayNameNode.textContent || "").indexOf("ms-resource:") === 0) {
                        displayNameNode.textContent = "";
                    }
                    if (result.json.applications.length === 1) {
                        displayNameNode.textContent = displayNameNode.textContent || result.json.applications[0].ShortName;
                    }
                    if ((displayNameNode.textContent || "").indexOf("ms-resource:") === 0) {
                        displayNameNode.textContent = "";
                    }
                    displayNameNode.textContent = displayNameNode.textContent || info.Identity.FamilyName;
                } catch (e) {}
                appLoadingStatus.textContent = strres.get("MANAGER_APP_INSTALLEDAPPS_SUCCEED");
                appLoading.classList.add("noloading");
                updateAppDataSource(page, result, appLoading.bar);
            },
            function(result) {
                try {
                    var displayNameNode = page.querySelector(".display-name");
                    displayNameNode.textContent = displayNameNode.textContent || result.json.properties.display_name;
                    if ((displayNameNode.textContent || "").indexOf("ms-resource:") === 0) {
                        displayNameNode.textContent = "";
                    }
                    if (result.json.applications.length === 1) {
                        displayNameNode.textContent = displayNameNode.textContent || result.json.applications[0].DisplayName || result.json.applications[0].ShortName;
                    }
                    if ((displayNameNode.textContent || "").indexOf("ms-resource:") === 0) {
                        displayNameNode.textContent = "";
                    }
                    if (result.json.applications.length === 1) {
                        displayNameNode.textContent = displayNameNode.textContent || result.json.applications[0].ShortName;
                    }
                    if ((displayNameNode.textContent || "").indexOf("ms-resource:") === 0) {
                        displayNameNode.textContent = "";
                    }
                    displayNameNode.textContent = displayNameNode.textContent || info.Identity.FamilyName;
                } catch (e) {}
                var msg = result.message;
                appLoadingStatus.textContent = external.String.format(strres.get("MANAGER_APP_INSTALLEDAPPS_FAILED"), msg);
                appLoading.classList.add("noloading");
                updateAppDataSource(page, result, appLoading.bar);
            }
        );
    }

    function initLoaderPage() {
        var page = document.getElementById("page-load");
        var prefixs = ["ins", "reg", "sta", "upd"];
        var opdict = {
            ins: Package.manager.add,
            reg: Package.manager.register,
            sta: Package.manager.stage,
            upd: Package.manager.update
        };
        var ingdict = {
            ins: strres.get("MANAGER_LOAD_INSTALL_ING"),
            reg: strres.get("MANAGER_LOAD_REGISTER_ING"),
            sta: strres.get("MANAGER_LOAD_STAGE_ING"),
            upd: strres.get("MANAGER_LOAD_UPDATE_ING")
        };
        var sdict = {
            ins: strres.get("MANAGER_LOAD_INSTALL_SUCCEED"),
            reg: strres.get("MANAGER_LOAD_REGISTER_SUCCEED"),
            sta: strres.get("MANAGER_LOAD_STAGE_SUCCEED"),
            upd: strres.get("MANAGER_LOAD_UPDATE_SUCCEED")
        }
        var explorer = external.Storage.Explorer;
        prefixs.forEach(function(prefix) {
            var checklist = document.getElementById(prefix + "-deployoptions");
            var btn = document.getElementById(prefix + "-btn");
            var statusbar = document.getElementById(prefix + "-progress");
            var progressbar = statusbar.querySelector(".progress");
            var status = statusbar.querySelector(".status");
            progressbar.removeAttribute("value");
            status.textContent = "";
            var hideTimer = null;
            btn.onclick = function() {
                var self = this;
                self.disabled = true;
                var nextNextStep = function(statusbar) {
                    if (hideTimer) clearTimeout(hideTimer);
                    hideTimer = setTimeout(function() {
                        hideTimer = null;
                        statusbar.bar.hide();
                    }, 10000);
                }
                var nextStep = function(filename) {
                    if (filename === "" || filename === null || filename === void 0) {
                        self.disabled = false;
                        return;
                    }
                    if (hideTimer) clearTimeout(hideTimer);
                    var op = opdict[prefix];
                    var options = 0;
                    var items = checklist.querySelectorAll("input:checked");
                    if (items) {
                        for (var i = 0; i < items.length; i++) {
                            options |= parseInt(items[i].value);
                        }
                    }
                    progressbar.removeAttribute("value");
                    status.textContent = external.String.format(ingdict[prefix], "");
                    statusbar.bar.show();
                    op(filename, options).then(function(result) {
                        self.disabled = false;
                        status.textContent = sdict[prefix];
                        refreshAppList2();
                        nextNextStep(statusbar);
                    }, function(error) {
                        self.disabled = false;
                        status.textContent = error.message;
                        nextNextStep(statusbar);
                    }, function(_p) {
                        status.textContent = external.String.format(ingdict[prefix], _p + "%");
                        progressbar.value = _p;
                    });
                }

                if (prefix === "ins" || prefix === "sta" || prefix === "upd") explorer.file(
                    external.String.format("{0}|{1}|{2}|{3}",
                        strres.get("MANAGER_LOAD_INS_OR_STA_FILTERDISPLAY"),
                        "*.appx;*.appxbundle;*.msix;*.msixbundle",
                        strres.get("MANAGER_LOAD_ALLFILES"),
                        "*.*"
                    ),
                    "",
                    nextStep
                );
                else if (prefix === "reg") explorer.file(
                    external.String.format("{0}|{1}|{2}|{3}",
                        strres.get("MANAGER_LOAD_REG_FILTERDISPLAY"),
                        "*.appxmanifest;AppxManifest.xml",
                        strres.get("MANAGER_LOAD_ALLFILES"),
                        "*.*"
                    ),
                    "",
                    nextStep
                );
            };
        });
    }
    global.setAppInfoPageContent = setAppInfoPageContent;
    global.initLoaderPage = initLoaderPage;
})(this);