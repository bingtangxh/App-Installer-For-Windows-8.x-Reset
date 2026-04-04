(function(global) {
    "use strict";
    var mgr = external.Package.manager;

    function parseJsonCallback(swJson, callback) {
        var ret = swJson;
        try {
            if (swJson) ret = JSON.parse(swJson);
        } catch (e) {}
        try {
            if (ret && typeof ret.jsontext !== "undefined") {
                ret["json"] = JSON.parse(ret.jsontext);
                delete ret.jsontext;
            }
        } catch (e) {}
        if (callback) callback(ret);
    }
    global.Package = {
        reader: {
            package: function(pkgPath) { return external.Package.Reader.package(pkgPath); },
            manifest: function(swManifestPath) { return external.Package.Reader.manifest(swManifestPath); },
            manifestFromInstallLocation: function(swInstallLocation) { return external.Package.Reader.fromInstallLocation(swInstallLocation); },
            readFromPackage: function(swPkgPath, bUsePri) {
                if (bUsePri === null || bUsePri === void 0) bUsePri = false;
                return new Promise(function(resolve, reject) {
                    external.Package.Reader.readFromPackageAsync(swPkgPath, bUsePri, function(result) {
                        parseJsonCallback(result, resolve);
                    }, function(error) {
                        parseJsonCallback(error, reject);
                    });
                });
            },
            readFromManifest: function(swManifestPath, bUsePri) {
                if (bUsePri === null || bUsePri === void 0) bUsePri = false;
                return new Promise(function(resolve, reject) {
                    external.Package.Reader.readFromManifestAsync(swManifestPath, bUsePri, function(result) {
                        parseJsonCallback(result, resolve);
                    }, function(error) {
                        parseJsonCallback(error, reject);
                    });
                });
            },
            readFromInstallLocation: function(swInstallLocation, bUsePri) {
                if (bUsePri === null || bUsePri === void 0) bUsePri = false;
                return new Promise(function(resolve, reject) {
                    external.Package.Reader.readFromInstallLocationAsync(swInstallLocation, bUsePri, function(result) {
                        parseJsonCallback(result, resolve);
                    }, function(error) {
                        parseJsonCallback(error, reject);
                    });
                });
            },
            cancelAll: function() { external.Package.Reader.cancelAll(); },
            addApplicationReadItem: function(swItemName) { return external.Package.Reader.addApplicationItem(swItemName); },
            removeApplicationReadItem: function(swItemName) { return external.Package.Reader.removeApplicationItem(swItemName); },
            updateApplicationReadItems: function(aswArray) {
                external.Package.Reader.updateApplicationItems(aswArray);
            },
            getApplicationReadItems: function() {
                return JSON.parse(external.Package.Reader.getApplicationItemsToJson());
            },
        },
        manager: {
            add: function(swPkgPath, uOptions) {
                return new Promise(function(resolve, reject, progress) {
                    mgr.addPackage(swPkgPath, uOptions, function(result) {
                        parseJsonCallback(result, resolve);
                    }, function(result) {
                        parseJsonCallback(result, reject);
                    }, progress);
                })
            },
            get: function() {
                return new Promise(function(resolve, reject) {
                    mgr.getPackages(function(result) {
                        parseJsonCallback(result, resolve);
                    }, function(result) {
                        parseJsonCallback(result, reject);
                    });
                });
            },
            remove: function(swPkgFullName) {
                return new Promise(function(resolve, reject, progress) {
                    mgr.removePackage(swPkgFullName, function(result) {
                        parseJsonCallback(result, resolve);
                    }, function(result) {
                        parseJsonCallback(result, reject);
                    }, progress);
                });
            },
            clearup: function(swPkgName, swUserSID) {
                return new Promise(function(resolve, reject, progress) {
                    mgr.clearupPackage(swPkgName, swUserSID, function(result) {
                        parseJsonCallback(result, resolve);
                    }, function(result) {
                        parseJsonCallback(result, reject);
                    }, progress);
                });
            },
            register: function(swPkgPath, uOptions) {
                return new Promise(function(resolve, reject, progress) {
                    mgr.registerPackage(swPkgPath, uOptions, function(result) {
                        parseJsonCallback(result, resolve);
                    }, function(result) {
                        parseJsonCallback(result, reject);
                    }, progress);
                });
            },
            registerByFullName: function(swPkgFullName, uOptions) {
                return new Promise(function(resolve, reject, progress) {
                    mgr.registerPackageByFullName(swPkgFullName, uOptions, function(result) {
                        parseJsonCallback(result, resolve);
                    }, function(result) {
                        parseJsonCallback(result, reject);
                    }, progress);
                });
            },
            setStatus: function(swPkgFullName, uStatus) {
                mgr.setPackageStatus(swPkgFullName, uStatus);
            },
            stage: function(swPkgPath, uOptions) {
                return new Promise(function(resolve, reject, progress) {
                    mgr.stagePackage(swPkgPath, uOptions, function(result) {
                        parseJsonCallback(result, resolve);
                    }, function(result) {
                        parseJsonCallback(result, reject);
                    }, progress);
                });
            },
            stageUserData: function(swPkgFullName) {
                return new Promise(function(resolve, reject, progress) {
                    mgr.stagePackageUserData(swPkgFullName, function(result) {
                        parseJsonCallback(result, resolve);
                    }, function(result) {
                        parseJsonCallback(result, reject);
                    }, progress);
                });
            },
            update: function(swPkgPath, uOptions) {
                return new Promise(function(resolve, reject, progress) {
                    mgr.updatePackage(swPkgPath, uOptions, function(result) {
                        parseJsonCallback(result, resolve);
                    }, function(result) {
                        parseJsonCallback(result, reject);
                    }, progress);
                });
            },
            findByIdentity: function(swIdName, swIdPublisher) {
                return new Promise(function(resolve, reject) {
                    mgr.findPackageByIdentity(swIdName, swIdPublisher, function(result) {
                        parseJsonCallback(result, resolve);
                    }, function(result) {
                        parseJsonCallback(result, reject);
                    });
                });
            },
            findByFamilyName: function(swFamilyName) {
                return new Promise(function(resolve, reject) {
                    mgr.findPackageByFamilyName(swFamilyName, function(result) {
                        parseJsonCallback(result, resolve);
                    }, function(result) {
                        parseJsonCallback(result, reject);
                    });
                });
            },
            findByFullName: function(swPkgFullName) {
                return new Promise(function(resolve, reject) {
                    mgr.findPackageByFullName(swPkgFullName, function(result) {
                        parseJsonCallback(result, resolve);
                    }, function(result) {
                        parseJsonCallback(result, reject);
                    });
                });
            },
            cancelAll: function() { mgr.cancelAll(); },
            active: function(swAppUserModelID, swArgs) { return mgr.activeApp(swAppUserModelID, swArgs || null); }
        },
        utils: (function() {
            // 解析包全名，格式: <name>_<version>_<processorArchitecture>_<resourceId>_<publisherId>
            // resourceId 可能为空（表现为两个连续下划线）
            function parsePackageFullName(fullName) {
                var parts = fullName.split('_');
                // 按格式应有5部分: [name, version, architecture, resourceId, publisherId]
                return {
                    name: parts[0],
                    version: parts[1],
                    architecture: parts[2],
                    resourceId: parts[3],
                    publisherId: parts[4]
                };
            }

            // 解析包系列名，格式: <name>_<publisherId>
            function parsePackageFamilyName(familyName) {
                var underscoreIndex = familyName.indexOf('_');
                if (underscoreIndex === -1) {
                    // 异常情况，按原字符串处理，但题目不会出现
                    return { name: familyName, publisherId: '' };
                }
                return {
                    name: familyName.substring(0, underscoreIndex),
                    publisherId: familyName.substring(underscoreIndex + 1)
                };
            }

            // 将对象转换为包全名字符串
            // 对象应包含 name, version, architecture, publisherId 字段，resourceId 可选
            function stringifyPackageFullName(pkg) {
                var resourcePart = (pkg.resourceId === undefined || pkg.resourceId === null) ? '' : pkg.resourceId;
                return pkg.name + '_' + pkg.version + '_' + pkg.architecture + '_' + resourcePart + '_' + pkg.publisherId;
            }

            // 将对象转换为包系列名字符串
            // 对象应包含 name, publisherId 字段
            function stringifyPackageFamilyName(pkgFamily) {
                return pkgFamily.name + '_' + pkgFamily.publisherId;
            }
            return {
                parsePackageFullName: parsePackageFullName,
                parsePackageFamilyName: parsePackageFamilyName,
                stringifyPackageFullName: stringifyPackageFullName,
                stringifyPackageFamilyName: stringifyPackageFamilyName,
                isDependency: function(swPkgIdentityName) {
                    var list = [
                        "Microsoft.Net.Native.Framework",
                        "Microsoft.Net.Native.Runtime",
                        "Microsoft.Net.CoreRuntime",
                        "WindowsPreview.Kinect",
                        "Microsoft.VCLibs",
                        "Microsoft.WinJS",
                        "Microsoft.UI.Xaml",
                        "Microsoft.WindowsAppRuntime",
                        "Microsoft.Advertising.XAML",
                        "Microsoft.Midi.Gmdls",
                        "Microsoft.Services.Store.Engagement",
                        "Microsoft.Media.PlayReadyClient"
                    ];
                    for (var i = 0; i < list.length; i++) {
                        if (swPkgIdentityName.toLowerCase().indexOf(list[i].toLowerCase()) !== -1)
                            return true;
                    }
                    return false;
                }
            }
        })()
    };
    global.Package.Reader = global.Package.reader;
    global.Package.Manager = global.Package.manager;
    global.Package.Utils = global.Package.utils;
})(this);