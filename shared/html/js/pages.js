(function(global) {
    "use strict";
    Object.defineProperty(global, "pagecontainer", {
        get: function() { return document.querySelector(".pagecontainer.main"); }
    });

    function getPage() {
        return pagecontainer.querySelector(".page");
    }
    var supportPageList = [
        "splash",
        "loading",
        "select",
        "preinstall",
        "installing",
        "installsuccess",
        "installfailed"
    ];
    var supportMulPageList = [
        "preinstall",
        "installing",
        "installsuccess",
        "installfailed"
    ];

    var splashBackColor = "";

    var timer = {
        title: null,
        text: null,
        logo: null,
        progress: null,
        controls: null,
    };

    function setPage(swPageLabel, bIsMulti) {
        var page = getPage();
        swPageLabel = ("" + (swPageLabel || ""));
        for (var i = 0; i < supportPageList.length; i++) {
            if (Bridge.NString.equals(swPageLabel, supportPageList[i])) {
                if (!page.classList.contains(supportPageList[i])) page.classList.add(supportPageList[i]);
            } else {
                if (page.classList.contains(supportPageList[i])) page.classList.remove(supportPageList[i]);
            }
        }
        if (Bridge.NString.equals(swPageLabel, "splash")) {
            if (splashBackColor) {
                page.style.backgroundColor = splashBackColor;
            }
        } else {
            page.style.backgroundColor = "";
        }
        if (page.classList.contains("multiple")) page.classList.remove("multiple");
        for (var j = 0; j < supportMulPageList.length; j++) {
            if (Bridge.NString.equals(swPageLabel, supportMulPageList[j]) && bIsMulti) {
                page.classList.add("multiple");
                break;
            }
        }
        var strutils = Bridge.NString;
        if (swPageLabel == "splash" || swPageLabel == "loading") {
            var controls = page.querySelector(".controls");
            if (controls) controls.style.display = "none";
        } else {
            var controls = page.querySelector(".controls");
            if (controls) controls.style.display = "flex";
            if (controls.style.display == 'none') controls.style.display = "-ms-flex";
            if (controls.style.display == 'none') controls.style.display = "-ms-flexbox";
        }
        (function() {
            if (Bridge.NString.equals(swPageLabel, "loading")) {
                var content = page.querySelector(".content.loading");
                if (content) content.style.visibility = "hidden";
                setTimeout(function(node) {
                    if (node) {
                        node.style.visibility = "visible";
                        var animation = Windows.UI.Animation;
                        animation.runAsync(node, animation.Keyframes.Flyout.toLeft, 500);
                    }
                }, 0, content);
                var loading = content.querySelector(".ring-loading");
                Windows.UI.Animation.loading(loading, true);
            } else {
                var content = page.querySelector(".content.loading");
                if (content) {
                    var loading = content.querySelector(".ring-loading");
                    Windows.UI.Animation.loading(loading, false);
                }
            }
        })();
        (function() {
            function push_nonull(arr, item) {
                if (item) arr.push(item);
            }
            var timerkeys = Object.keys(timer);
            for (var i = 0; i < timerkeys.length; i++) {
                if (timer[timerkeys[i]]) {
                    clearTimeout(timer[timerkeys[i]]);
                    timer[timerkeys[i]] = null;
                }
            }
            var content = page.querySelector(".content." + Bridge.String.trim(swPageLabel));
            var controls = page.querySelector(".controls");
            var progress = page.querySelector(".progress");
            var reason = page.querySelector(".reason");
            var titlepart = [];
            var textpart = [];
            var storelogo = null;
            if (content) {
                if (bIsMulti) push_nonull(titlepart, content.querySelector(".currentfile"));
                push_nonull(titlepart, content.querySelector(".pkgtitle"));
                if (bIsMulti) push_nonull(titlepart, content.querySelector(".pkgtitle.multiple"));
                push_nonull(titlepart, content.querySelector(".pkgapplabel"));
                push_nonull(titlepart, content.querySelector(".pkgpublisher"));
                push_nonull(titlepart, content.querySelector(".pkgversion"));
                push_nonull(titlepart, content.querySelector(".pkgsupport"));
                push_nonull(textpart, content.querySelector(".pkgfunctions-label"));
                push_nonull(textpart, content.querySelector(".functions"));
                push_nonull(textpart, content.querySelector(".moreinfo"));
                storelogo = content.querySelector(".storelogo");
            }
            var refresh = function(nodes) {
                for (var i = 0; i < nodes.titlepart.length; i++) nodes.titlepart[i].style.visibility = "hidden";
                for (var i = 0; i < nodes.textpart.length; i++) nodes.textpart[i].style.visibility = "hidden";
                if (nodes.storelogo) nodes.storelogo.style.visibility = "hidden";
                if (nodes.progress) nodes.progress.style.visibility = "hidden";
                if (nodes.controls) nodes.controls.style.visibility = "hidden";
                if (nodes.reason) nodes.reason.style.visibility = "hidden";
                var animation = Windows.UI.Animation;
                timer.title = setTimeout(function(titlenodes) {
                    if (titlenodes) {
                        for (var i = 0; i < titlenodes.length; i++) {
                            animation.runAsync(titlenodes[i], animation.Keyframes.Flyout.toLeft, 500);
                        }
                        for (var i = 0; i < titlenodes.length; i++) titlenodes[i].style.visibility = "visible";
                    }
                }, 0, nodes.titlepart);
                timer.text = setTimeout(function(textnodes) {
                    if (textnodes) {
                        for (var i = 0; i < textnodes.length; i++) {
                            animation.runAsync(textnodes[i], animation.Keyframes.Flyout.toLeft, 500);
                        }
                        for (var i = 0; i < textnodes.length; i++) textnodes[i].style.visibility = "visible";
                    }
                }, 50, nodes.textpart);
                timer.logo = setTimeout(function(logonode) {
                    if (logonode) {
                        animation.runAsync(logonode, "scale-visible", 500);
                        logonode.style.visibility = "visible";
                    }
                }, 100, nodes.storelogo);
                timer.progress = setTimeout(function(progressnode) {
                    if (progressnode) {
                        animation.runAsync(progressnode, animation.Keyframes.Flyout.toLeft, 500);
                        progressnode.style.visibility = "visible";
                    }
                }, 100, nodes.progress);
                timer.controls = setTimeout(function(controlnodes) {
                    if (controlnodes) {
                        animation.runAsync(controlnodes, animation.Keyframes.Flyout.toTop, 500);
                        controlnodes.style.visibility = "visible";
                    }
                }, 100, nodes.controls);
                timer.reason = setTimeout(function(reasonnode) {
                    if (reasonnode) {
                        animation.runAsync(reasonnode, animation.Keyframes.Flyout.toLeft, 500);
                        reasonnode.style.visibility = "visible";
                    }
                }, 100, nodes.reason);
            };
            refresh({
                titlepart: titlepart,
                textpart: textpart,
                storelogo: storelogo,
                progress: progress,
                controls: controls,
                reason: reason,
            });
        })();
        (function() {
            var page = document.querySelector(".page");
            var progress = page.querySelector(".progress");
            var ringLoading = progress.querySelector(".ring-loading");
            if (Bridge.NString.equals(swPageLabel, "installing")) {
                Windows.UI.Animation.loading(ringLoading, true);
            } else {
                Windows.UI.Animation.loading(ringLoading, false);
            }
        })();
    }

    function getPageLabel() {
        var page = getPage();
        for (var i = 0; i < supportPageList.length; i++) {
            if (page.classList.contains(supportPageList[i])) {
                return supportPageList[i];
            }
        }
        return "";
    }

    function isMultiPage() {
        var page = getPage();
        return page.classList.contains("multiple");
    }

    function setPageMultiple(bIsMulti) {
        setPage(getPageLabel(), bIsMulti);
    }

    function getSplashPage() {
        var page = document.querySelector(".page.splash");
        if (!page) return null;
        var ret = {};
        Object.defineProperty(ret, "background", {
            get: function() {
                var page = document.querySelector(".page.splash");
                return page.style.backgroundColor || splashBackColor;
            },
            set: function(value) {
                splashBackColor = value;
                var page = document.querySelector(".page.splash");
                if (page) page.style.backgroundColor = value;
            }
        });
        Object.defineProperty(ret, "imagesrc", {
            get: function() {
                var splashimg = page.querySelector("img.splash");
                return splashimg.src;
            },
            set: function(value) {
                var splashimg = page.querySelector("img.splash");
                splashimg.src = value;
            }
        });
        Object.defineProperty(ret, "content", {
            get: function() {
                try {
                    var content = page.querySelector(".content.splash");
                    return content;
                } catch (e) { return null; }
            },
        });
        Object.defineProperty(ret, "statusText", {
            get: function() {
                var statustext = page.querySelector(".content.splash .status-text");
                return statustext.textContent;
            },
            set: function(value) {
                var statustext = page.querySelector(".content.splash .status-text");
                if (!statustext) return;
                statustext.textContent = value;
            }
        });
        ret["getImageUrl"] = function() { return ret.imagesrc; };
        ret["setImageUrl"] = function(value) { ret.imagesrc = value; };
        ret["getBackground"] = function() { return ret.background; };
        ret["setBackground"] = function(value) { ret.background = value; };
        ret["setStatusText"] = function(value) { ret.statusText = value; };
        ret["getStatusText"] = function() { return ret.statusText; };
        ret["getContent"] = function() { return ret.content; };
        return ret;
    }

    function getPreinstallPage() {
        return document.querySelector(".page.preinstall");
    }

    module.exports = {
        Page: {
            set: setPage,
            get: function() {
                if (isMultiPage()) {
                    return [getPageLabel(), "multiple"];
                } else {
                    return getPageLabel();
                }
            }
        }
    };
    Object.defineProperty(Page, "current", {
        get: function() { return getPageLabel(); },
        set: function(swPageLabel) { setPage(swPageLabel, isMultiPage()); }
    });
    Object.defineProperty(Page, "multiple", {
        get: function() { return isMultiPage(); },
        set: function(bIsMulti) { setPage(getPageLabel(), bIsMulti); }
    });
    Object.defineProperty(Page, "splash", {
        get: function() { return getSplashPage(); }
    });
    module.exports = {
        setPage: setPage,
        getPage: getPage,
        getPageIsMulti: isMultiPage,
        setPageMultiple: setPageMultiple,
        getSplashPage: getSplashPage,
        getSplashPageImageUrl: function() { return getSplashPage().imagesrc; },
        setSplashPageImageUrl: function(value) { getSplashPage().imagesrc = value; },
        getSplashPageBackground: function() { return getSplashPage().background; },
        setSplashPageBackground: function(value) { getSplashPage().background = value; },
        getSplashPageStatusText: function() { return getSplashPage().statusText; },
        setSplashPageStatusText: function(value) { try { getSplashPage().statusText = value; } catch (e) {} },
        getSplashPageContent: function() { return getSplashPage().content; },
    };

    function parseVersion(version) {
        var v = (version || "0.0.0.0").split(".");
        var ret = { major: 0, minor: 0, build: 0, revision: 0 };
        if (v.length >= 1) ret.major = parseInt(v[0]);
        if (v.length >= 2) ret.minor = parseInt(v[1]);
        if (v.length >= 3) ret.build = parseInt(v[2]);
        if (v.length >= 4) ret.revision = parseInt(v[3]);
        return ret;
    }

    function stringifyVersion(version) {
        return version.major + "." + version.minor + "." + version.build + "." + version.revision;
    }

    function setFlyoutDisplayInfo(pkginfo) {
        var flyout = document.getElementById("moreinfo-flyout");
        var content = document.getElementById("moreinfo-flyout-content");
        var ret = {
            architectureSupport: false,
            osminversionSupport: false,
            showArchitectureText: "",
            showOsminversionText: "",
        };
        var sysArch = Bridge.External.System.Archievement;
        (function() {
            var name = content.querySelector(".id.name");
            name.textContent = pkginfo.identity.name;
            var publisher = content.querySelector(".id.publisher");
            publisher.textContent = pkginfo.identity.publisher;
            var version = content.querySelector(".id.version");
            version.textContent = stringifyVersion(pkginfo.identity.realver);
            var arch = content.querySelector(".id.arch");
            console.log(pkginfo.identity.architecture);
            if (pkginfo.type === 1) {
                switch (pkginfo.identity.architecture) {
                    case 1:
                        arch.textContent = "x86";
                        if (sysArch == 0 || sysArch == 9) ret.architectureSupport = true;
                        break;
                    case 2:
                        arch.textContent = "x64";
                        if (sysArch == 9) ret.architectureSupport = true;
                        break;
                    case 4:
                        arch.textContent = "ARM";
                        if (sysArch == 5 || sysArch == 12) ret.architectureSupport = true;
                        break;
                    case 8:
                        arch.textContent = "ARM64";
                        if (sysArch == 12) ret.architectureSupport = true;
                        break;
                    case 15:
                        arch.textContent = "Neutral";
                        if (sysArch != 6 && sysArch > 0) ret.architectureSupport = true;
                        break;
                }
                ret.showArchitectureText = arch.textContent;
            } else if (pkginfo.type === 2) {
                var varch = pkginfo.identity.architecture;
                var strarr = [];
                if (varch & 1) {
                    strarr.push("x86");
                    if (sysArch == 0 || sysArch == 9) ret.architectureSupport = true;
                }
                if (varch & 2) {
                    strarr.push("x64");
                    if (sysArch == 9) ret.architectureSupport = true;
                }
                if (varch & 4) {
                    strarr.push("ARM");
                    if (sysArch == 5 || sysArch == 12) ret.architectureSupport = true;
                }
                if (varch & 8) {
                    strarr.push("ARM64");
                    if (sysArch == 12) ret.architectureSupport = true;
                }
                arch.textContent = strarr.join(", ");
                ret.showArchitectureText = arch.textContent;
            }
            var family = content.querySelector(".id.family");
            family.textContent = pkginfo.identity.package_family_name;
            var full = content.querySelector(".id.full");
            full.textContent = pkginfo.identity.package_full_name;
        })();
        (function() {
            var framework = content.querySelector(".prop.framework");
            framework.textContent = Bridge.Resources.byname(pkginfo.properties.framework ? "IDS_MOREINFO_PROPYES" : "IDS_MOREINFO_PROPNO");
            var respkg = content.querySelector(".prop.respkg");
            respkg.textContent = Bridge.Resources.byname(pkginfo.properties.removable ? "IDS_MOREINFO_PROPYES" : "IDS_MOREINFO_PROPNO");
        })();
        (function() {
            var sys = content.querySelector(".info.sys");
            var strutils = Bridge.External.String;
            sys.textContent = strutils.format(Bridge.Resources.byname("IDS_MOREINFO_INFOSYS_VALUE"), pkginfo.prerequisites.os_min_version_description, stringifyVersion(pkginfo.prerequisites.os_min_version));
            var pkgSupportVer = pkginfo.prerequisites.os_min_version;
            var systemSupportVer = Bridge.External.System.Version;
            ret.showOsminversionText = stringifyVersion(pkgSupportVer);
            if (pkgSupportVer.major < systemSupportVer.major) {
                ret.osminversionSupport = true;
            } else if (pkgSupportVer.major == systemSupportVer.major) {
                if (pkgSupportVer.minor < systemSupportVer.minor) {
                    ret.osminversionSupport = true;
                } else if (pkgSupportVer.minor == systemSupportVer.minor) {
                    if (pkgSupportVer.major == 6 && pkgSupportVer.minor == 3 && pkgSupportVer.build == 1) {
                        if (pkginfo.identity.architecture & (4 | 8)) {
                            ret.osminversionSupport = true;
                        }
                    } else {
                        ret.osminversionSupport = true;
                    }
                }
            }
            var lang = content.querySelector(".info.langs");
            lang.innerHTML = "";
            for (var i = 0; i < pkginfo.resources.languages.length; i++) {
                var localeName = pkginfo.resources.languages[i];
                var li = document.createElement("li");
                li.textContent = Bridge.Locale.localeInfoEx(localeName, 2);
                lang.appendChild(li);
            }
        })();
        return ret;
    }

    function noticeLoadPreinstallPage(ismul) {
        setPage("preinstall", ismul);
        var page = getPreinstallPage();
        if (!page) return;
        var content = page.querySelector(".content.preinstall");
        if (!content) return;
        var filelist = Bridge.Package.filepaths();
        var fselect = content.querySelector(".currentfile select");
        for (var i = 0; i < filelist.length; i++) {
            var option = document.createElement("option");
            option.value = filelist[i];
            option.text = filelist[i];
            fselect.appendChild(option);
        }
        if (filelist.length > 0) {
            var strutils = Bridge.External.String;
            var pkgtitle = null;
            if (filelist.length <= 1) {
                pkgtitle = content.querySelector(".pkgtitle");
            } else {
                pkgtitle = content.querySelector(".pkgtitle.multiple");
            }
            var pkgpublisher = content.querySelector(".pkgpublisher");
            var pkgversion = content.querySelector(".pkgversion");
            var storelogo = content.querySelector(".storelogo");
            var storelogoimg = storelogo.querySelector("img");
            var storelogofilter = storelogo.querySelector(".filter");
            var pkginfo = Bridge.Package.pkginfo(filelist[0]);
            if (filelist.length <= 1) {
                if (!pkgtitle.hasAttribute("data-pkgname"))
                    pkgtitle.setAttribute("data-pkgname", pkginfo.properties.display_name);
                if (!pkgtitle.hasAttribute("data-titlefmt"))
                    pkgtitle.setAttribute("data-titlefmt", Bridge.Resources.byname("IDS_PREINSTALL_TITLE"));
                pkgtitle.innerHTML = strutils.formatInnerHtml(pkgtitle.getAttribute("data-titlefmt"), pkgtitle.getAttribute("data-pkgname"));
            } else {
                pkgtitle.innerHTML = strutils.formatInnerHtml(Bridge.Resources.byname("IDS_PREINSTALL_MPKGNAME"), pkginfo.properties.display_name);
            }
            pkgpublisher.innerHTML = strutils.formatInnerHtml(Bridge.Resources.byname("IDS_PUBLISHER"), pkginfo.properties.publisher_display_name);
            pkgversion.innerHTML = strutils.formatInnerHtml(Bridge.Resources.byname("IDS_VERSION"), stringifyVersion(pkginfo.identity.version));
            storelogoimg.src = pkginfo.properties.logo_base64;
            storelogo.setAttribute("data-logoimg", pkginfo.properties.logo);
            var backcolor = "";
            if (pkginfo.applications.length > 0) {
                var appinfo = pkginfo.applications[0];
                backcolor = appinfo.BackgroundColor || Bridge.UI.themeColor;
                if (strutils.tolower(backcolor) == "transparent") {
                    backcolor = Bridge.UI.themeColor;
                }
            } else { backcolor = Bridge.UI.themeColor; }
            storelogo.style.backgroundColor = backcolor;
            storelogofilter.style.background = Color.genTileBackFilter(backcolor);
            var funclist = content.querySelector(".functions ul");
            for (var j = 0; j < pkginfo.capabilities.capabilities_name.length; j++) {
                var li = document.createElement("li");
                var capname = pkginfo.capabilities.capabilities_name[j];
                li.setAttribute("data-capability", capname);
                li.textContent = Bridge.Package.capabilityDisplayName(capname);
                if (Bridge.NString.empty(li.textContent)) li.textContent = capname;
                funclist.appendChild(li);
            }
            for (var j = 0; j < pkginfo.capabilities.device_capabilities.length; j++) {
                var capname = pkginfo.capabilities.device_capabilities[j];
                var cdname = Bridge.Package.capabilityDisplayName(capname);
                if (!Bridge.NString.empty(cdname)) {
                    var li = document.createElement("li");
                    li.setAttribute("data-capability", capname);
                    li.textContent = Bridge.Package.capabilityDisplayName(capname);
                    funclist.appendChild(li);
                }
            }
            var support = setFlyoutDisplayInfo(pkginfo);
            var supportDisplay = page.querySelector(".pkgsupport");
            if (support.architectureSupport && support.osminversionSupport) {
                supportDisplay.classList.add("support");
                supportDisplay.classList.remove("not-support");
            } else {
                supportDisplay.classList.add("not-support");
                supportDisplay.classList.remove("support");
            }
            supportDisplay.setAttribute("title", strutils.format("OS Min Version: {0}, Architecture: {1}", support.showOsminversionText, support.showArchitectureText));
        }
    }

    function setPreinstallPagePkgTitleFormatSingle(fmt) {
        var page = document.querySelector(".page");
        if (!page) return;
        var content = page.querySelector(".content.preinstall");
        if (!content) return;
        var pkgtitle = content.querySelector("h1.pkgtitle");
        pkgtitle.setAttribute("data-titlefmt", fmt);
    }

    function setControlButtonState(serial, title, display, ban) {
        var page = document.querySelector(".page");
        if (!page) return;
        var controls = page.querySelector(".controls");
        if (!controls) return;
        var commands = controls.querySelectorAll(".command button");
        try {
            if (title !== void 0) commands[serial - 1].textContent = title;
            if (display !== void 0) commands[serial - 1].style.display = display ? "" : "none";
            if (ban !== void 0) commands[serial - 1].disabled = ban;
        } catch (e) {}
    }

    function setLaunchWhenReady(selected, ban) {
        var page = document.querySelector(".page");
        if (!page) return;
        var controls = page.querySelector(".controls");
        if (!controls) return;
        var checkbox = controls.querySelector(".checkbox input");
        if (!checkbox) return;
        if (selected !== void 0) checkbox.checked = selected;
        if (ban !== void 0) checkbox.disabled = ban;
    }

    function getLaunchWhenReady() {
        var page = document.querySelector(".page");
        if (!page) return false;
        var controls = page.querySelector(".controls");
        if (!controls) return false;
        var checkbox = controls.querySelector(".checkbox input");
        if (!checkbox) return false;
        if (checkbox.disabled === true) return false;
        return checkbox.checked === true || checkbox.checked + "" === "true";
    }

    function noticeLoadSelectPage() {
        setPage("select", false);
        setPreinstallPagePkgTitleFormatSingle(Bridge.Resources.byname("IDS_SELECT_TITLE"))
        setControlButtonState(1, Bridge.Resources.byname("IDS_SELECT_OPENFILE"), true, false);
        setControlButtonState(2, Bridge.Resources.byname("IDS_PREINSTALL_CANCEL"), true, false);
    }

    function setInstallingPackageInfoMultiple(filepath) {
        var page = document.querySelector(".page");
        if (!page) return;
        var content = page.querySelector(".content.installing");
        if (!content) return;
        var progress = page.querySelector(".progress");
        if (!progress) return;
        var pkgtitle = content.querySelector(".pkgtitle.multiple");
        var title = content.querySelector(".title.multiple");
        var pkgpublisher = content.querySelector(".pkgpublisher");
        var pkgversion = content.querySelector(".pkgversion");
        var storelogo = content.querySelector(".storelogo");
        var storelogoimg = storelogo.querySelector("img");
        var storelogofilter = storelogo.querySelector(".filter");
        var pkginfo = Bridge.Package.pkginfo(filepath);
        pkgtitle.innerHTML = Bridge.String.formatInnerHtml(Bridge.Resources.byname("IDS_PREINSTALL_MPKGNAME"), pkginfo.properties.display_name);
        pkgpublisher.innerHTML = Bridge.String.formatInnerHtml(Bridge.Resources.byname("IDS_PUBLISHER"), pkginfo.properties.publisher_display_name);
        pkgversion.innerHTML = Bridge.String.formatInnerHtml(Bridge.Resources.byname("IDS_VERSION"), stringifyVersion(pkginfo.identity.version));
        storelogoimg.src = pkginfo.properties.logo_base64;
        storelogo.setAttribute("data-logoimg", pkginfo.properties.logo);
        var backcolor = "";
        var strutils = Bridge.External.String;
        if (pkginfo.applications.length > 0) {
            var appinfo = pkginfo.applications[0];
            backcolor = appinfo.BackgroundColor || Bridge.UI.themeColor;
            if (strutils.tolower(backcolor) == "transparent") {
                backcolor = Bridge.UI.themeColor;
            }
        } else { backcolor = Bridge.UI.themeColor; }
        storelogo.style.backgroundColor = backcolor;
        storelogofilter.style.background = Color.genTileBackFilter(backcolor);
    }

    function noticeLoadInstallingPage(ismul) {
        setPage("installing", ismul);
        var page = document.querySelector(".page");
        if (!page) return;
        var content = page.querySelector(".content.installing");
        if (!content) return;
        var progress = page.querySelector(".progress");
        if (!progress) return;
        var pkgtitle = null;
        if (!ismul) {
            pkgtitle = content.querySelector(".pkgtitle");
            pkgtitle.setAttribute("data-titlefmt", Bridge.Resources.byname("IDS_INSTALLING_TITLE"));
        }
        var title = null;
        if (ismul) {
            title = content.querySelector(".title.multiple");
            title.textContent = Bridge.Resources.byname("IDS_INSTALLING_MTITLE");
        } else title = content.querySelector(".title");
    }
    // 0 - 100, float
    function setInstallingProgress(percent) {
        var page = document.querySelector(".page");
        if (!page) return;
        var progress = page.querySelector(".progress");
        if (!progress) return;
        var bar = progress.querySelector("progress");
        if (typeof bar.max !== "number") bar.max = 100;
        if (typeof bar.min !== "number") bar.min = 0;
        bar.value = bar.min + (bar.max - bar.min) * (percent * 0.01);
        if (bar.value > bar.max) bar.value = bar.max;
        if (bar.value < bar.min) bar.value = bar.min;
    }

    function setInstallingStatus(status) {
        var page = document.querySelector(".page");
        if (!page) return;
        var progress = page.querySelector(".progress");
        if (!progress) return;
        var statusbar = progress.querySelector(".status");
        if (!statusbar) return;
        statusbar.textContent = status;
    }

    function noticeLoadInstallSuccessPage(ismul) {
        setPage("installsuccess", ismul);
        var page = document.querySelector(".page");
        var files = Bridge.Package.filepaths();
        var pkginfo = Bridge.Package.pkginfo(files[0]);
        if (ismul) {
            var content = page.querySelector(".content.installsuccess");
            if (!content) return;
            var title = content.querySelector(".currentfile .title.multiple");
            title.textContent = Bridge.Resources.byname("IDS_SUCCESS_MTITLE");
            var hasApp = false;
            for (var i = 0; i < files.length; i++) {
                var pi = Bridge.Package.pkginfo(files[i]);
                if (pi.applications && pi.applications.length > 0) {
                    hasApp = true;
                    break;
                }
            }
            setControlButtonState(1, Bridge.Resources.byname(hasApp ? "IDS_SUCCESS_LAUNCH" : "IDS_COMMAND_CANCEL"), true, false);
        } else {
            setPreinstallPagePkgTitleFormatSingle(Bridge.Resources.byname("IDS_SUCCESS_TITLE"));
            setControlButtonState(1, Bridge.Resources.byname(pkginfo.applications && pkginfo.applications.length > 0 ? "IDS_SUCCESS_LAUNCH" : "IDS_COMMAND_CANCEL"), true, false);
        }
    }

    function noticeLoadInstallFailedPage(ismul) {
        setPage("installfailed", ismul);
        var page = document.querySelector(".page");
        if (!page) return;
        var content = page.querySelector(".content.installfailed");
        if (!content) return;
        var files = Bridge.Package.filepaths();
        setControlButtonState(1, Bridge.Resources.byname("IDS_COMMAND_CANCEL"), true, false);
        var title = null;
        var reason = page.querySelector(".reason textarea");
        if (ismul) {
            title = content.querySelector(".currentfile .title.multiple");
            title.textContent = Bridge.Resources.byname("IDS_FAILED_MTITLE");
            var select = content.querySelector(".currentfile select");
            select.value = "";
            select.value = files[0];
            try {
                if (document.createEvent) {
                    var event = document.createEvent('HTMLEvents');
                    event.initEvent('change', true, false); // bubbles, cancelable
                    select.dispatchEvent(event);
                } else if (select.fireEvent) { // IE <= 8
                    select.fireEvent('onchange');
                }
            } catch (e) {}
        } else {
            title = content.querySelector(".title");
            setPreinstallPagePkgTitleFormatSingle(Bridge.Resources.byname("IDS_FAILED_STITLE"));
            var hres = Bridge.Package.installResult(files[0]);
            reason.textContent = hres.message;
        }
    }
    module.exports = {
        noticeLoadPreinstallPage: noticeLoadPreinstallPage,
        parseVersion: parseVersion,
        stringifyVersion: stringifyVersion,
        setFlyoutDisplayInfo: setFlyoutDisplayInfo,
        setPreinstallPagePkgTitleFormatSingle: setPreinstallPagePkgTitleFormatSingle,
        setControlButtonState: setControlButtonState,
        setLaunchWhenReady: setLaunchWhenReady,
        getLaunchWhenReady: getLaunchWhenReady,
        noticeLoadSelectPage: noticeLoadSelectPage,
        setInstallingPackageInfoMultiple: setInstallingPackageInfoMultiple,
        noticeLoadInstallingPage: noticeLoadInstallingPage,
        setInstallingProgress: setInstallingProgress,
        setInstallingStatus: setInstallingStatus,
        noticeLoadInstallSuccessPage: noticeLoadInstallSuccessPage,
        noticeLoadInstallFailedPage: noticeLoadInstallFailedPage
    };
})(this);