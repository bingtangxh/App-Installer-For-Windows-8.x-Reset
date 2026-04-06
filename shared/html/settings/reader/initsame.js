(function(global) {
    "use strict";

    function ready(e) {
        Windows.UI.DPI.mode = 1
        var pagesection = document.querySelector(".pagesection");
        if (pagesection) {
            var backcolor = slideback;
            setTimeout(function() {
                var h2style = document.getElementById("h2-style");
                if (!h2style) {
                    h2style = document.createElement("style");
                    h2style.id = "h2-style";
                }
                h2style.innerHTML = ".main h2 { color: " + Color.getSuitableForegroundTextColor(Color.parse("#F3F3F3"), [Color.parse(backcolor), Color.Const.black]).RGBA.stringify() + " }";
                document.head.appendChild(h2style);
            }, 0);
        }
    }
    OnLoad.add(ready);
})(this);