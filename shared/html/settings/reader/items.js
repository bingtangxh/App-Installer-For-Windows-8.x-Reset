(function(global) {
    "use strict";

    function getPage(tag, page, display) {
        return {
            tag: tag,
            page: page,
            title: display
        };
    }
    var pages = {
        general: getPage("general", "reader/general.html", getPublicRes(101)),
        appitems: getPage("appitems", "reader/appitems.html", stringRes("READER_SETTINGS_APPITEMS")),
    };
    Object.defineProperty(global, "pages", {
        get: function() {
            return pages;
        }
    });
    Object.defineProperty(global, "guidePage", {
        get: function() {
            return getPage("guide", "reader/guide.html", "guide");
        }
    });
})(this);