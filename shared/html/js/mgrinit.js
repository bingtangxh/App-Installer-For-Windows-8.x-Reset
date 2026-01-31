(function(global) {
    var strres = external.StringResources;

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
        var listContainer = document.getElementById("applist");
        var appItemTemplate = document.getElementById("appitem-template");
        var mgr = Package.manager;
        var nstr = Bridge.NString;
        var datasrc = new DataView.DataSource();
        datasrc.setKeySelector(function(item) {
            if (item === null || item === void 0) return null;
            return Bridge.String.tolower(Bridge.String.trim(item.Identity.FullName));
        });
        var themeColor = Bridge.UI.themeColor;
        var loadingDisplay = document.getElementById("applist-loading");
        var loadingStatus = loadingDisplay.querySelector(".title");
        var emptyDisplay = document.createElement("div");
        var dataLengthDisplay = document.getElementById("applist-datalen");
        var appSearchList = document.getElementById("applist-search");
        emptyDisplay.textContent = strres.get("MANAGER_MANAGE_LISTEMPTY");
        var listView = new DataView.ListView(listContainer, function(item) {
            var appItem = appItemTemplate.cloneNode(true);
            appItem.id = "";
            appItem.style.display = "";
            var logoimg = appItem.querySelector("img");
            logoimg.src = item.Properties.LogoBase64 || logoimg.src;
            logoimg.parentElement.style.backgroundColor = themeColor;
            var appName = appItem.querySelector(".displayName");
            appName.textContent = item.Properties.DisplayName || item.Identity.Name;
            var appPub = appItem.querySelector(".publisher");
            appPub.textContent = item.Properties.Publisher;
            appItem.data = item;
            appItem.setAttribute("data-install-location", item.InstallLocation);
            appItem.setAttribute("data-development-mode", item.DevelopmentMode);
            appItem.setAttribute("data-is-bundle", item.IsBundle);
            appItem.setAttribute("data-is-framework", item.Properties.Framework);
            appItem.setAttribute("data-family-name", item.Identity.FamilyName);
            appItem.setAttribute("data-full-name", item.Identity.FullName);
            appItem.setAttribute("data-version", item.Identity.Version.Expression);
            appItem.setAttribute("data-users", item.Users);
            appItem.setAttribute("data-publisher-id", item.Identity.PublisherId);
            logoimg.parentElement.style.backgroundColor = item.BackgroundColor;
            if (item.BackgroundColor === "transparent") {
                logoimg.parentElement.style.backgroundColor = themeColor;
            }
            var uninstallButton = appItem.querySelector("div[role=control] .uninstall");
            Windows.UI.Event.Util.addEvent(uninstallButton, "click", function(e) {
                e.stopPropagation();
                this.disabled = true;
                var itemNode = this.parentNode.parentNode.parentNode;
                var flyout = document.getElementById("app-uninstall-flyout");
                if (typeof flyout.appDataSource !== "undefined") flyout.appDataSource.clear();
                if (typeof flyout.appDataSource !== "undefined") {
                    Package.reader.readFromInstallLocation(this.parentNode.parentNode.parentNode.data.InstallLocation, true).then(function(result) {
                        try {
                            if (typeof result.json.applications === "undefined" || result.json.applications.length === 0) {
                                result.json.applications = [{
                                    DisplayName: item.Properties.DisplayName || item.Identity.Name,
                                    SmallLogo_Base64: item.Properties.LogoBase64,
                                }];
                            }
                            flyout.appDataSource.updateList(result.json.applications);

                        } catch (e) {}
                    }, function(result) {
                        try { flyout.appDataSource.updateList(result.json.applications); } catch (e) {}
                    });
                }
                var self = this;
                var confirm = flyout.querySelector(".confirm");
                confirm.onclick = null;
                confirm.onclick = function() {
                    self.disabled = true;
                    flyout.winControl.hide();
                    var fullName = itemNode.getAttribute("data-full-name");
                    itemNode.classList.add("uninstalling");
                    var progressPart = itemNode.querySelector("div[role=progress]");
                    var statusDisplay = progressPart.querySelector(".status");
                    statusDisplay.textContent = strres.get("MANAGER_APP_UNINSTALL_ING");
                    var progressDisplay = progressPart.querySelector(".progress");
                    progressDisplay.removeAttribute("value");
                    self.disabled = true;
                    (function(itemNode, statusDisplay, progressDisplay, self) {
                        mgr.remove(fullName).then(function(_s) {
                            itemNode.classList.remove("uninstalling");
                            itemNode.classList.add("uninstalled");
                            if (_s.succeeded) {
                                statusDisplay.textContent = strres.get("MANAGER_APP_UNINSTALL_SUCCEED");
                                datasrc.remove(itemNode.data);
                            } else {
                                statusDisplay.textContent = _s.message;
                                setTimeout(function(iNode, uButton) {
                                    iNode.classList.remove("uninstalled");
                                    uButton.disabled = false;
                                }, 5000, itemNode, self);
                            }
                        }, function(_f) {
                            itemNode.classList.remove("uninstalling");
                            itemNode.classList.add("uninstalled");
                            try {
                                if (_f.succeeded) {
                                    statusDisplay.textContent = strres.get("MANAGER_APP_UNINSTALL_SUCCEED");
                                    datasrc.remove(itemNode.data);
                                } else {
                                    statusDisplay.textContent = _f.message;
                                    setTimeout(function(iNode, uButton) {
                                        iNode.classList.remove("uninstalled");
                                        uButton.disabled = false;
                                    }, 5000, itemNode, self);
                                }
                            } catch (e) {
                                statusDisplay.textContent = e.message;
                                setTimeout(function(iNode, uButton) {
                                    iNode.classList.remove("uninstalled");
                                    uButton.disabled = false;
                                }, 5000, itemNode, self);
                            }
                            self.disabled = false;
                        }, function(_p) {
                            statusDisplay.textContent = Bridge.String.format(
                                strres.get("MANAGER_APP_UNINSTALL_PROGRESSING"),
                                _p
                            );
                            progressDisplay.value = _p;
                        });
                    })(itemNode, statusDisplay, progressDisplay, self);
                };
                var winFlyout = flyout.winControl;
                if (winFlyout._beforehideHandler) {
                    winFlyout.removeEventListener("beforehide", winFlyout._beforehideHandler);
                }
                winFlyout._beforehideHandler = function() {
                    self.disabled = false;
                };
                winFlyout.addEventListener("beforehide", winFlyout._beforehideHandler);
                flyout.winControl.show(this);
            });
            Windows.UI.Event.Util.addEvent(appItem.querySelector("div[role=advance] a"), "click", function(e) {
                e.stopPropagation();
                try {
                    pagemgr.go("appinfo", this.parentNode.parentNode.parentNode.data);
                } catch (ex) {}
            });
            return appItem;
        });
        listView.selectionMode = "single";
        listView.bind(datasrc);
        listView.emptyView = emptyDisplay;
        listView.searchHandler = function(text, item) {
            return ((item.Properties.DisplayName || item.Identity.Name || "") + (item.Properties.Publisher || "")).indexOf(text) >= 0;
        };
        appSearchList.control = new Search.Box(appSearchList, {
            placeholderText: strres.get("MANAGER_MANAGE_SEARCHPLACEHOLDER"),
            chooseSuggestionOnEnter: false
        });
        appSearchList.control.ontextchanged = function(ev) {
            console.log(ev.text);
            listView.searchText = ev.detail.text;
        };
        listView.onsearchend = function() {
            dataLengthDisplay.textContent = external.String.format(strres.get("MANAGER_MANAGE_FINDAPPS"), listView.findItemLength);
        };
        var timer = null;

        function refreshAppList() {
            dataLengthDisplay.textContent = "";

            function processData(manifest, dataitem) {
                //if (dataitem.Identity.FamilyName = "Microsoft.MicrosoftEdge.Stable_8wekyb3d8bbwe") debugger;
                dataitem.Properties.DisplayName = dataitem.Properties.DisplayName || manifest.properties.display_name || dataitem.Properties.DisplayName;
                if ((dataitem.Properties.DisplayName || "").indexOf("ms-resource:") === 0) {
                    dataitem.Properties.DisplayName = "";
                }
                if (manifest.applications.length === 1) {
                    dataitem.Properties.DisplayName = dataitem.Properties.DisplayName || manifest.applications[0].DisplayName || "";
                }
                if ((dataitem.Properties.DisplayName || "").indexOf("ms-resource:") === 0) {
                    dataitem.Properties.DisplayName = "";
                }
                if (manifest.applications.length === 1) {
                    dataitem.Properties.DisplayName = dataitem.Properties.DisplayName || manifest.applications[0].ShortName || "";
                }
                if ((dataitem.Properties.DisplayName || "").indexOf("ms-resource:") === 0) {
                    dataitem.Properties.DisplayName = "";
                }
                dataitem.Properties.DisplayName = dataitem.Properties.DisplayName || dataitem.Identity.FamilyName;
                dataitem.Properties.Puvlisher = dataitem.Properties.Publisher || manifest.properties.publisher_display_name || dataitem.Properties.Publisher;
                dataitem.Properties.Framework = dataitem.Properties.Framework || manifest.properties.framework;
                dataitem.Properties.Logo = dataitem.Properties.Logo || manifest.properties.logo;
                dataitem.Properties.LogoBase64 = dataitem.Properties.LogoBase64 || manifest.properties.logo_base64;
                if (manifest.applications.length === 1) {
                    dataitem.Properties.LogoBase64 = dataitem.Properties.LogoBase64 || manifest.applications[0].Square44x44Logo_Base64 || manifest.applications[0].SmallLogo_Base64;
                }
                dataitem.Properties.ResourcePackage = dataitem.Properties.ResourcePackage || manifest.properties.resource_package;
                dataitem.Properties.Description = dataitem.Properties.Description || manifest.properties.description;
                try {
                    dataitem.BackgroundColor = manifest.applications[0].BackgroundColor || "transparent";
                } catch (e) {
                    dataitem.BackgroundColor = "transparent";
                }
                return dataitem;
            }

            function update(datas) {
                var newDatas = [];
                var promises = [];
                for (var i = 0; i < datas.length; i++) {
                    var data = datas[i];
                    if (external.System.isWindows10) {
                        if (data.Properties.DisplayName === null || data.Properties.DisplayName === "" || data.Properties.DisplayName === void 0 ||
                            data.Properties.LogoBase64 === null || data.Properties.LogoBase64 === "" || data.Properties.LogoBase64 === void 0
                        ) {
                            promises.push(function(item, arr) {
                                return Package.reader.readFromInstallLocation(item.InstallLocation, true).then(function(result) {
                                    try {
                                        arr.push(processData(result.json, item));
                                    } catch (e) {
                                        item.BackgroundColor = "transparent";
                                        arr.push(item);
                                    }
                                }, function(result) {
                                    try {
                                        arr.push(processData(result.json, item));
                                    } catch (e) {
                                        item.BackgroundColor = "transparent";
                                        arr.push(item);
                                    }
                                });
                            }(data, newDatas));
                        } else {
                            promises.push(function(item, arr) {
                                return Package.reader.readFromInstallLocation(item.InstallLocation, false).then(function(result) {
                                    try {
                                        item.BackgroundColor = result.json.applications[0].BackgroundColor;
                                        arr.push(item);
                                    } catch (e) {
                                        item.BackgroundColor = "transparent";
                                        arr.push(item);
                                    }
                                }, function(result) {
                                    try {
                                        item.BackgroundColor = result.json.applications[0].BackgroundColor;
                                        arr.push(item);
                                    } catch (e) {
                                        item.BackgroundColor = "transparent";
                                        arr.push(item);
                                    }
                                });
                            }(data, newDatas));
                        }
                    } else {
                        promises.push(function(item, arr) {
                            return Package.reader.readFromInstallLocation(item.InstallLocation, true).then(function(result) {
                                try {
                                    arr.push(processData(result.json, item));
                                } catch (e) {
                                    item.BackgroundColor = "transparent";
                                    arr.push(item);
                                }
                            }, function(result) {
                                try {
                                    arr.push(processData(result.json, item));
                                } catch (e) {
                                    item.BackgroundColor = "transparent";
                                    arr.push(item);
                                }
                            });
                        }(data, newDatas));
                    }
                }

                function updateDatas() {
                    datasrc.updateList(newDatas, function(item) {
                        return item.Identity.FullName || "";
                    });
                    var compare = function(a, b) { return a - b; };
                    try {
                        compare = createLocalizedCompare(external.System.Locale.currentLocale);
                    } catch (e) {
                        try {
                            compare = createLocalizedCompare(navigator.language);
                        } catch (e) {
                            compare = function(a, b) {
                                if (a < b) return -1;
                                if (a > b) return 1;
                                return 0;
                            };
                        }
                    }
                    datasrc.sort(function(a, b) {
                        return compare(a.Properties.DisplayName, b.Properties.DisplayName);
                    });
                    dataLengthDisplay.textContent = external.String.format(strres.get("MANAGER_MANAGE_FINDAPPS"), listView.findItemLength);
                }
                return Promise.join(promises).then(updateDatas, updateDatas);
            }
            if (timer) clearTimeout(timer);
            timer = null;
            loadingDisplay.style.display = "";
            loadingDisplay.classList.remove("noloading");
            loadingDisplay.bar.show();

            function waitAndHide() {
                return new Promise(function(resolve, reject) {
                    if (timer) clearTimeout(timer);
                    timer = null;
                    timer = setTimeout(function(rs, rj) {
                        //loadingDisplay.style.display = "none";
                        loadingDisplay.bar.hide();
                        rs();
                    }, 5000, resolve, reject);
                });
            }
            loadingStatus.textContent = strres.get("MANAGER_APP_INSTALLEDAPPS_LOADING");
            return mgr.get().then(function(result) {
                return update(result.list).then(function() {
                    loadingDisplay.classList.add("noloading");
                    loadingStatus.textContent = strres.get("MANAGER_APP_INSTALLEDAPPS_SUCCEED");
                    setTimeout(function(lv) {
                        lv.refresh();
                    }, 500, listView);
                }).then(waitAndHide);
            }, function(error) {
                loadingDisplay.classList.add("noloading");
                var errmsg = (error.result ? (error.result.message || error.result.ErrorCode || "获取失败") : (error.message || error.error || error));
                loadingStatus.textContent = external.String.format(strres.get("MANAGER_APP_INSTALLEDAPPS_FAILED"), errmsg);
                try { update(error.list); } catch (e) {}
                setTimeout(function(lv) {
                    lv.refresh();
                }, 500, listView);
                return waitAndHide();
            });
        }
        var appbar = document.getElementById("appBar");
        var appbarControl = new AppBar.AppBar(appbar);
        var refreshButton = new AppBar.Command();
        refreshButton.icon = "&#57623;";
        refreshButton.label = strres.get("MANAGER_APP_REFRESH");
        global.refreshAppList2 = function refreshAppList2() {
            appbarControl.hide();
            refreshButton.disabled = true;
            return refreshAppList().then(function() {
                refreshButton.disabled = false;
            }, function(error) {
                refreshButton.disabled = false;
            });
        }
        var showSystemApps = document.getElementById("applist-showsystemapp");
        var showFrameworks = document.getElementById("applist-showframework");
        listView.filter = function(item) {
            try {
                if (!showFrameworks.checked && item.Properties.Framework) return false;
                if (!showSystemApps.checked && item.Users.indexOf("NT AUTHORITY\\SYSTEM") !== -1) return false;
                return true;
            } catch (e) {
                return false;
            }
        };
        Windows.UI.Event.Util.addEvent(showSystemApps, "change", function() {
            listView.refresh();
            dataLengthDisplay.textContent = external.String.format(strres.get("MANAGER_MANAGE_FINDAPPS"), listView.findItemLength);
        });
        Windows.UI.Event.Util.addEvent(showFrameworks, "change", function() {
            listView.refresh();
            dataLengthDisplay.textContent = external.String.format(strres.get("MANAGER_MANAGE_FINDAPPS"), listView.findItemLength);
        });
        refreshButton.addEventListener("click", refreshAppList2);
        appbarControl.add(refreshButton);
        refreshAppList2();
        var appDetailPage = document.getElementById("page-appinfo");
        pagemgr.register("manager", document.getElementById("tag-manager"), document.getElementById("page-manager"));
        pagemgr.register("appinfo", document.getElementById("tag-appinfo"), document.getElementById("page-appinfo"), setAppInfoPageContent);
        var appinfoBackPage = appDetailPage.querySelector(".win-backbutton");
        Windows.UI.Event.Util.addEvent(appinfoBackPage, "click", function(e) {
            pagemgr.back();
        });
        appDetailPage.appDataSource = new DataView.DataSource();
        var appListView = new DataView.ListView(appDetailPage.querySelector(".apps"), function(item) {
            var appItem = appItemTemplate.cloneNode(true);
            appItem.id = "";
            appItem.style.display = "";
            var logoimg = appItem.querySelector("img");
            logoimg.src = item.Square44x44Logo_Base64 || item.SmallLogo_Base64;
            if (logoimg.src == "" || logoimg.src == null || logoimg.src == void 0) logoimg.removeAttribute("src");
            logoimg.parentElement.style.backgroundColor = item.BackgroundColor;
            if (Bridge.NString.equals(item.BackgroundColor, "transparent")) logoimg.parentElement.style.backgroundColor = themeColor;
            var appName = appItem.querySelector(".displayName");
            appName.textContent = item.DisplayName || item.ShortName;
            var appPub = appItem.querySelector(".publisher");
            appPub.style.display = "none";
            appItem.querySelector("div[role=advance]").style.display = "none";
            var ctrls = appItem.querySelector("div[role=control]");
            ctrls.innerHTML = "";
            appItem.data = item;
            var launchButton = document.createElement("button");
            launchButton.textContent = strres.get("MANAGER_APP_LAUNCH");
            launchButton.setAttribute("data-app-user-model-id", item.AppUserModelID);
            var createShortcutButton = document.createElement("button");
            createShortcutButton.textContent = strres.get("MANAGER_APP_CREATESHORTCUT");
            createShortcutButton.style.marginRight = "10px";
            Windows.UI.Event.Util.addEvent(launchButton, "click", function(e) {
                e.stopPropagation();
                Package.manager.active(this.getAttribute("data-app-user-model-id"));
            });
            ctrls.appendChild(launchButton);
            ctrls.appendChild(createShortcutButton);
            return appItem;
        });
        appListView.selectionMode = "single";
        appListView.bind(appDetailPage.appDataSource);
        appListView.emptyView = emptyDisplay.cloneNode(true);
        var appDetailUninstall = appDetailPage.querySelector("#detail-uninstall-btn");
        var appDetailUninstallStatusBlock = appDetailPage.querySelector("#appinfo-uninstallstatus");
        var appDetailUninstallProgress = appDetailUninstallStatusBlock.querySelector(".progress");
        var appDetailUninstallProgressStatus = appDetailUninstallStatusBlock.querySelector(".status");
        appDetailUninstallStatusBlock.bar = new TransitionPanel(appDetailUninstallStatusBlock, {
            axis: 'y',
            duration: 500,
        });
        Windows.UI.Event.Util.addEvent(appDetailUninstall, "click", function(e) {
            e.stopPropagation();
            appinfoBackPage.disabled = true;
            appDetailUninstallProgress.removeAttribute("value");
            var item = appDetailPage.data;
            var flyout = document.getElementById("app-uninstall-flyout");
            if (typeof flyout.appDataSource !== "undefined") flyout.appDataSource.clear();
            if (typeof flyout.appDataSource !== "undefined") {
                flyout.appDataSource.updateList(appDetailPage.appDataSource.get());
            }
            var self = this;
            var confirm = flyout.querySelector(".confirm");
            confirm.onclick = null;
            confirm.onclick = function() {
                self.disabled = true;
                flyout.winControl.hide();
                var fullName = item.Identity.FullName;
                var progressPart = appDetailUninstallStatusBlock;
                var statusDisplay = appDetailUninstallProgressStatus;
                statusDisplay.textContent = strres.get("MANAGER_APP_UNINSTALL_ING");
                var progressDisplay = appDetailUninstallProgress;
                progressDisplay.style.display = "";
                self.disabled = true;
                progressPart.bar.show();
                (function(statusDisplay, progressDisplay, self, item) {
                    mgr.remove(fullName).then(function(_s) {
                        if (_s.succeeded) {
                            statusDisplay.textContent = strres.get("MANAGER_APP_UNINSTALL_SUCCEED");
                            datasrc.remove(item);
                            appinfoBackPage.disabled = false;
                        } else {
                            statusDisplay.textContent = _s.message;
                        }
                        setTimeout(function(uButton, isSuccess) {
                            appinfoBackPage.disabled = false;
                            uButton.disabled = isSuccess;
                            progressPart.bar.hide();
                        }, 5000, self, _s.succeeded);
                        progressDisplay.style.display = "none";
                    }, function(_f) {
                        try {
                            if (_f.succeeded) {
                                statusDisplay.textContent = strres.get("MANAGER_APP_UNINSTALL_SUCCEED");
                                datasrc.remove(item);
                                appinfoBackPage.disabled = false;
                            } else {
                                statusDisplay.textContent = _f.message;
                            }
                            setTimeout(function(uButton, isSuccess) {
                                appinfoBackPage.disabled = false;
                                uButton.disabled = isSuccess;
                                progressPart.bar.hide();
                            }, 5000, self, _f.succeeded);
                        } catch (e) {
                            statusDisplay.textContent = e.message;
                            appinfoBackPage.disabled = false;
                            setTimeout(function(uButton, isSuccess) {
                                appinfoBackPage.disabled = false;
                                uButton.disabled = isSuccess;
                                progressPart.bar.hide();
                            }, 5000, self, _f.succeeded);
                        }
                        self.disabled = false;
                        progressDisplay.style.display = "none";
                    }, function(_p) {
                        statusDisplay.textContent = Bridge.String.format(
                            strres.get("MANAGER_APP_UNINSTALL_PROGRESSING"),
                            _p
                        );
                        progressDisplay.value = _p;
                    });
                })(statusDisplay, progressDisplay, self, item);
            };
            var winFlyout = flyout.winControl;
            if (winFlyout._beforehideHandler) {
                winFlyout.removeEventListener("beforehide", winFlyout._beforehideHandler);
            }
            winFlyout._beforehideHandler = function() {
                self.disabled = false;
            };
            winFlyout.addEventListener("beforehide", winFlyout._beforehideHandler);
            flyout.winControl.show(this);
        });
        var uninstallFlyout = document.getElementById("app-uninstall-flyout");
        uninstallFlyout.appListView = new DataView.ListView(uninstallFlyout.querySelector(".applist"), function(item) {
            var appItem = appItemTemplate.cloneNode(true);
            appItem.id = "";
            appItem.style.display = "";
            var logoimg = appItem.querySelector("img");
            logoimg.src = item.Square44x44Logo_Base64 || item.SmallLogo_Base64;
            if (logoimg.src == "" || logoimg.src == null || logoimg.src == void 0) logoimg.removeAttribute("src");
            logoimg.parentElement.style.backgroundColor = item.BackgroundColor;
            if (Bridge.NString.equals(item.BackgroundColor, "transparent")) logoimg.parentElement.style.backgroundColor = themeColor;
            var appName = appItem.querySelector(".displayName");
            appName.style.wordBreak = "normal";
            appName.style.wordWrap = "normal";
            appName.textContent = item.DisplayName || item.ShortName;
            var appPub = appItem.querySelector(".publisher");
            appPub.style.display = "none";
            appItem.querySelector("div[role=advance]").style.display = "none";
            var ctrls = appItem.querySelector("div[role=control]");
            ctrls.innerHTML = "";
            appItem.data = item;
            return appItem;
        });
        uninstallFlyout.appDataSource = new DataView.DataSource();
        uninstallFlyout.appListView.bind(uninstallFlyout.appDataSource);
        pagemgr.addEventListener("load", function(e) {
            appbarControl.enabled = e == "manager";
            refreshButton.style.display = e == "manager" ? "" : "none";
        });
        pagemgr.go("manager");
    });
})(this);