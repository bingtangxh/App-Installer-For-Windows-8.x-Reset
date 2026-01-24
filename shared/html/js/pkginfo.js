(function(global) {
    "use strict";
    var mgr = external.Package.manager;

    function parseJsonCallback(swJson, callback) {
        var ret = swJson;
        try {
            if (swJson) ret = JSON.parse(swJson);
        } catch (e) {}
        if (callback) callback(ret);
    }
    global.Package = {
        reader: function(pkgPath) { external.Package.reader(pkgPath); },
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
        },
    };
})(this);