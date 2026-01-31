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
            removeApplicationReadItem: function(swItemName) { return external.Package.Reader.removeApplicationItem(swItemName); }
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
    };
})(this);