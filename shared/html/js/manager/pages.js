(function(global) {
    "use strict";
    var pkg_ns = external.Package;

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

    function setAppInfoPageContent(info) {
        var page = document.getElementById("page-appinfo");
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
        var pkg = pkg_ns.fromInstallLocation(il);
        var json = pkg.jsonText;
        console.log(JSON.parse(json));
    }
    global.setAppInfoPageContent = setAppInfoPageContent;
})(this);