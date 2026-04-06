(function(global) {
    "use strict";

    function ready(e) {
        var page = document.querySelector("#settingpage");
        var guide = page.querySelector(".page.guide");
        var slide = guide.querySelector("aside");
        setTimeout(function() {
            var barcolor = visual["BackgroundColor"];
            slide.style.backgroundColor = barcolor;
            slide.style.color = Color.getSuitableForegroundTextColor(Color.parse(barcolor), [Color.Const.white, Color.Const.black]).stringify();
        }, 50);
        var content = guide.querySelector(".main");
        var list = slide.querySelector("ul");
        var backbtn = slide.querySelector("#back");
        var title = slide.querySelector("#apptitle");
        list.innerHTML = "";
        var items = pages;
        var tags = Object.keys(items);
        var eventutil = Windows.UI.Event.Util;
        for (var i = 0; i < tags.length; i++) {
            var tag = tags[i];
            var item = items[tag];
            var li = document.createElement("li");
            li.setAttribute("data-page", item.page);
            li.setAttribute("data-tag", item.tag);
            li.innerHTML = item.title;
            eventutil.addEvent(li, "click", function() {
                if (li.hasAttribute("data-require-disabled")) return;
                content.style.display = "none";
                for (var j = 0; j < list.children.length; j++) {
                    var child = list.children[j];
                    if (child.classList.contains("selected"))
                        child.classList.remove("selected");
                }
                content.src = this.getAttribute("data-page");
                setTimeout(function() {
                    content.style.display = "";
                    Windows.UI.Animation.runAsync(content, [Windows.UI.Animation.Keyframes.Flyout.toLeft, Windows.UI.Animation.Keyframes.Opacity.visible]);
                }, 0);
                this.classList.add("selected");
            });
            list.appendChild(li);
        }
        content.src = guidePage.page;
        /*for (var i = 0; i < list.children.length; i++) {
            var child = list.children[i];
            child.click();
            break;
        }*/
        var jumppage = "";
        try { var args = cmdargs; if (args.length > 1) jumppage = args[1]; } catch (e) {}
        if (jumppage && jumppage.length > 0 && !Bridge.External.jump2) {
            for (var i = 0; i < list.children.length; i++) {
                var child = list.children[i];
                if (Bridge.NString.equals(child.getAttribute("data-tag"), jumppage)) {
                    Bridge.External.jump2 = true;
                    setTimeout(function(thisnode) {
                        thisnode.click();
                    }, 0, child)
                    break;
                }
            }
        }
        global.setDisabledForOperation = function(disabled) {
            var list = document.querySelector("#settingpage .guide aside ul");
            for (var i = 0; i < list.children.length; i++) {
                var child = list.children[i];
                if (disabled) {
                    child.setAttribute("data-require-disabled", "true");
                } else {
                    child.removeAttribute("data-require-disabled");
                }
            }
            if (disabled) {
                backbtn.disabled = true;
                title.style.marginLeft = backbtn.style.marginLeft;
            } else {
                backbtn.disabled = false;
                title.style.marginLeft = "";
            }
        }
        global.setItemHighlight = function(tag) {
            var list = document.querySelector("#settingpage .guide aside ul");
            for (var i = 0; i < list.children.length; i++) {
                var child = list.children[i];
                if (Bridge.NString.equals(child.getAttribute("data-tag"), tag)) {
                    if (!child.classList.contains("selected")) child.classList.add("selected");
                } else {
                    if (child.classList.contains("selected")) child.classList.remove("selected");
                }
            }
        }
    }
    OnLoad.add(ready);
})(this);