(function(global) {
    var storage = Bridge.External.Storage;
    var path = storage.path;
    var root = path.getDir(path.program);
    var exepath = path.combine(root, "settings.exe");
    var id = "Manager";
    var ve = Bridge.External.VisualElements.get(id);
    var slideback = ve["BackgroundColor"];
    global.slideback = slideback;
    global.exepath = exepath;
    global.visual = ve;
    var strutil = Bridge.External.String;
    var nstrutil = Bridge.NString;
    var boolTrue = ["true", "1", "yes", "on", "y", "t", "zhen", "真"];
    var boolFalse = ["false", "0", "no", "off", "n", "f", "jia", "假"];
    global.parseBool = function(str) {
        str = "" + str;
        for (var i = 0; i < boolTrue.length; i++) {
            if (nstrutil.equals(str, boolTrue[i])) {
                return true;
            }
        }
        for (var i = 0; i < boolFalse.length; i++) {
            if (nstrutil.equals(str, boolFalse[i])) {
                return false;
            }
        }
        return null;
    };
})(this);