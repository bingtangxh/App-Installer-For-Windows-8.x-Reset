(function(global) {
    "use strict";
    var eu = Windows.UI.Event.Util;
    var anime = Windows.UI.Animation;

    function PagePair(guideNode, pageNode, respHandler) {
        var _guide = guideNode;
        var _page = pageNode;
        var _handler = respHandler || null;
        Object.defineProperty(this, "guide", {
            get: function() { return _guide; },
            set: function(value) { _guide = value; }
        });
        Object.defineProperty(this, "page", {
            get: function() { return _page; },
            set: function(value) { _page = value; }
        });
        Object.defineProperty(this, "handler", {
            get: function() { return _handler; },
            set: function(value) { _handler = value; }
        });
    }

    function PageManager() {
        var dict = {};
        var stack = [];
        var current = -1;
        var record = {}; // 记录哪些界面已经第一次加载过
        var paramStack = [];
        // scrollStack 与 stack 对齐：scrollStack[i] 对应 stack[i]
        var scrollStack = [];
        var nowScroll = 0;
        var events = {
            firstload: [],
            beforeload: [],
            load: [],
            afterload: [],
            willunload: [],
            unload: []
        };

        function addHandler(type, fn) {
            if (typeof fn !== "function") return;
            events[type].push(fn);
        }

        function removeHandler(type, fn) {
            var list = events[type];
            for (var i = list.length - 1; i >= 0; i--) {
                if (list[i] === fn) {
                    list.splice(i, 1);
                }
            }
        }

        function emit(type, arg) {
            var list = events[type];
            for (var i = 0; i < list.length; i++) {
                try {
                    list[i](arg);
                } catch (e) {}
            }
        }

        function emitCancelable(type, arg) {
            var list = events[type];
            for (var i = 0; i < list.length; i++) {
                try {
                    var r = list[i](arg);
                    if (r === false) return false;
                } catch (e) {}
            }
            return true;
        }
        /**
         * 添加载入事件
         * @param {string} type 支持："firstload"
        "beforeload"
        "load"
        "afterload"
        "willunload"
        "unload"
        
         * @param {function} fn 
         */
        this.addEventListener = function(type, fn) {
            addHandler(type, fn);
        };
        /**
         * 移除载入事件
         * @param {string} type 支持："firstload"
        "beforeload"
        "load"
        "afterload"
        "willunload"
        "unload"
        
         * @param {function} fn 
         */
        this.removeEventListener = function(type, fn) {
            removeHandler(type, fn);
        };


        function guideClickHandler(e) {
            var tag = this.__pageTag;
            if (!tag) return;
            if (this.classList.contains("selected")) return;
            self.go(tag);
            return;
            var keys = Object.keys(dict);
            var promises = [];
            for (var i = 0; i < keys.length; i++) {
                var key = keys[i];
                var pair = dict[key];
                if (pair.guide.classList.contains("selected")) {
                    promises.push(anime.runAsync(
                        pair.page, [
                            anime.Keyframes.Opacity.hidden,
                            anime.Keyframes.Scale.down
                        ]
                    ).then(function(el) {
                        el.style.display = "none";
                    }));
                }
            }
            this.classList.add("selected");
            var after = Promise.join(promises);
            for (var i = 0; i < keys.length; i++) {
                var key = keys[i];
                var pair = dict[key];
                if (pair.guide.classList.contains("selected")) {
                    pair.page.style.display = "";
                    after.then(function() {
                        anime.runAsync(
                            pair.page, [
                                anime.Keyframes.Opacity.visible,
                                anime.Keyframes.Flyout.toLeft
                            ]
                        );
                    });
                }
            }
        }
        var self = this;

        function _activate(tag, args, fromHistory) {
            var pair = dict[tag];
            if (!pair) throw "Page not found: " + tag;
            if (!emitCancelable("beforeload", tag)) {
                return;
            }
            var keys = Object.keys(dict);
            var promises = [];
            var oldTags = [];
            for (var i = 0; i < keys.length; i++) {
                var k = keys[i];
                var p = dict[k];
                if (p.guide.classList.contains("selected") && k !== tag) {
                    if (!emitCancelable("willunload", k)) {
                        return;
                    }
                    oldTags.push(k);
                    promises.push(
                        anime.runAsync(p.page, [
                            anime.Keyframes.Opacity.hidden
                        ]).then((function(page, key) {
                            return function() {
                                page.style.display = "none";
                                page.style.opacity = 0;
                                emit("unload", key);
                            };
                        })(p.page, k))
                    );
                    p.guide.classList.remove("selected");
                }
            }
            pair.guide.classList.add("selected");
            pair.page.style.display = "";
            emit("load", tag);
            var after = Promise.join(promises);
            after.then(function() {
                if (!record[tag]) {
                    record[tag] = true;
                    emit("firstload", tag);
                }
                pair.page.style.opacity = 1;
                if (pair.handler) {
                    // fix: use pair.handler
                    pair.handler(args);
                }
                try {
                    setTimeout(function(tnode) {
                        try {
                            tnode.scrollTop = nowScroll || 0;
                        } catch (ex) {}
                    }, 10, pair.page.parentNode);
                } catch (ex) {}
                return anime.runAsync(pair.page, [
                    anime.Keyframes.Opacity.visible,
                    anime.Keyframes.Flyout.toLeft
                ]).then(function() {

                });
            }).then(function() {
                emit("afterload", tag);
            });
        }
        this.register = function(tag, guideNode, pageNode, respHandler) {
            pageNode.style.opacity = 0;
            dict[tag] = new PagePair(guideNode, pageNode, respHandler);
            guideNode.__pageTag = tag;
            try {
                eu.removeEvent(guideNode, "click", guideClickHandler);
                eu.addEvent(guideNode, "click", guideClickHandler);
            } catch (e) {}
        };
        this.edit = function(tag, pagePair) {
            try {
                if (dict[tag] && dict[tag].guide) {
                    dict[tag].guide.__pageTag = null;
                }
            } catch (e) {}
            dict[tag] = pagePair;
            try {
                pagePair.guide.__pageTag = tag;
                eu.removeEvent(pagePair.guide, "click", guideClickHandler);
                eu.addEvent(pagePair.guide, "click", guideClickHandler);
            } catch (e) {}
        };
        this.get = function(tag) {
            return dict[tag];
        };
        this.getGuide = function(tag) {
            return dict[tag].guide;
        };
        this.getPage = function(tag) {
            return dict[tag].page;
        };
        this.getHandler = function(tag) {
            return dict[tag].handler;
        };
        this.setGuide = function(tag, guideNode) {
            try {
                if (dict[tag] && dict[tag].guide) {
                    eu.removeEvent(dict[tag].guide, "click", guideClickHandler);
                    dict[tag].guide.__pageTag = null;
                }
            } catch (e) {}
            dict[tag].guide = guideNode;
            try {
                guideNode.__pageTag = tag;
                eu.removeEvent(guideNode, "click", guideClickHandler);
                eu.addEvent(guideNode, "click", guideClickHandler);
            } catch (e) {}
        };
        this.setPage = function(tag, pageNode) {
            dict[tag].page = pageNode;
        };
        this.setHandler = function(tag, handler) {
            dict[tag].handler = handler;
        };
        this.remove = function(tag) {
            try {
                try {
                    if (dict[tag] && dict[tag].guide) {
                        eu.removeEvent(dict[tag].guide, "click", guideClickHandler);
                    }
                } catch (e) {}
                delete dict[tag];
            } catch (e) {}
        };
        this.clear = function() {
            try {
                var keys = Object.keys(dict);
                for (var i = 0; i < keys.length; i++) {
                    this.remove(keys[i]);
                }
            } catch (e) {}
        };
        this.jump = function(tag, args) {
            _activate(tag, args, true);
        };
        this.go = function(tag, params) {
            // limit history
            if (stack.length > 300) {
                stack.length = 0;
                paramStack.length = 0;
                scrollStack.length = 0;
                current = -1;
            }
            // if we are in the middle, truncate forward history
            if (current < stack.length - 1) {
                stack.splice(current + 1);
                paramStack.splice(current + 1);
                scrollStack.splice(current + 1);
            }
            // save current page scrollTop
            try {
                if (current >= 0 && stack[current] && dict[stack[current]] && dict[stack[current]].page && dict[stack[current]].page.parentNode) {
                    scrollStack[current] = dict[stack[current]].page.parentNode.scrollTop;
                }
            } catch (e) {}
            // push new entry
            stack.push(tag);
            paramStack.push(params);
            // initialize scroll value for the new page (will be used if user goes back to it later)
            scrollStack.push(0);
            current++;
            _activate(tag, params, false);
        };
        this.back = function() {
            if (current <= 0) return false;
            // save scroll of current page
            try {
                if (stack[current] && dict[stack[current]] && dict[stack[current]].page && dict[stack[current]].page.parentNode) {
                    scrollStack[current] = dict[stack[current]].page.parentNode.scrollTop;
                }
            } catch (e) {}
            // move back
            current--;
            // restore scroll for new current
            nowScroll = scrollStack[current] || 0;
            _activate(stack[current], paramStack[current], true);
            return true;
        };
        this.next = function() {
            if (current >= stack.length - 1) return false;
            // save scroll of current page
            try {
                if (stack[current] && dict[stack[current]] && dict[stack[current]].page && dict[stack[current]].page.parentNode) {
                    scrollStack[current] = dict[stack[current]].page.parentNode.scrollTop;
                }
            } catch (e) {}
            // move forward
            current++;
            // restore scroll for new current
            nowScroll = scrollStack[current] || 0;
            _activate(stack[current], paramStack[current], true);
            return true;
        };
        Object.defineProperty(this, "current", {
            get: function() { return stack[current]; },
            set: function(value) {
                if (value < 0 || value >= stack.length) return;
                current = value;
                // restore scroll for assigned current
                nowScroll = scrollStack[current] || 0;
                _activate(stack[current], paramStack[current], true);
            }
        });
        Object.defineProperty(this, "canback", {
            get: function() { return current > 0; }
        });
        Object.defineProperty(this, "cannext", {
            get: function() { return current < stack.length - 1; }
        });
    }
    global.PageManager = PageManager;
})(this);