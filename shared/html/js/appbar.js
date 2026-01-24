(function(global) {
    "use strict";

    // 基类：提供 element 管理与基础 dispose 行为
    function PMAppBarBaseMember() {
        var _element = null;
        Object.defineProperty(this, "element", {
            configurable: true, // <- 关键：允许子类重定义该属性
            enumerable: false,
            get: function() { return _element; },
            set: function(value) {
                _element = value;
                try {
                    if (_element) {
                        // 让 DOM 节点可以反查到对应的 member
                        _element.appBarMember = this;
                    }
                } catch (e) {}
            }
        });
        // 可被子类或外部调用来从 DOM 中移除自身
        this.dispose = function() {
            try {
                if (this.element && this.element.parentNode) {
                    this.element.parentNode.removeChild(this.element);
                }
            } catch (e) {}
        };
    }

    function PMAppBarCommand() {
        PMAppBarBaseMember.call(this);
        var _button = document.createElement("button");
        var _iconcontainer = document.createElement("span");
        var _iconnode = document.createElement("span");
        var _labelnode = document.createElement("span");
        _button.appendChild(_iconcontainer);
        _iconcontainer.appendChild(_iconnode);
        _button.appendChild(_labelnode);
        _button.classList.add("win-command");
        _button.setAttribute("role", "menuitem");
        _iconcontainer.classList.add("win-commandicon");
        _iconcontainer.classList.add("win-commandring");
        _iconnode.classList.add("win-commandimage");
        _labelnode.classList.add("win-label");
        _iconcontainer.tabIndex = -1;
        _iconnode.tabIndex = -1;
        _labelnode.tabIndex = -1;
        _button.classList.add("win-global");
        Windows.UI.Event.Util.addEvent(_button, "keydown", function(event) {
            if (event.keyCode === 13) {
                _button.click();
            }
        });
        Object.defineProperty(this, "element", {
            get: function() { return _button; },
            set: function(value) { _button = value; }
        });
        Object.defineProperty(this, "icon", {
            get: function() { return _iconnode.innerHTML; },
            set: function(value) { _iconnode.innerHTML = value; }
        });
        Object.defineProperty(this, "label", {
            get: function() { return _labelnode.textContent; },
            set: function(value) { _labelnode.textContent = value; }
        });
        Object.defineProperty(this, "onclick", {
            get: function() { return _button.onclick; },
            set: function(value) { _button.onclick = value; }
        });
        Object.defineProperty(this, "selectable", {
            get: function() { return _button.classList.contains("win-selectable"); },
            set: function(value) {
                try { Windows.UI.Event.Util.removeEvent(this.element, "click", selectHandler); } catch (e) {}
                _button.classList.toggle("win-selectable", value);
                if (!value) {
                    if (_button.classList.contains("win-selected")) {
                        _button.classList.remove("win-selected");
                    }
                }
                if (value) Windows.UI.Event.Util.addEvent(this.element, "click", selectHandler);
                else Windows.UI.Event.Util.removeEvent(this.element, "click", selectHandler);
            }
        });
        Object.defineProperty(this, "selected", {
            get: function() { return _button.classList.contains("win-selected"); },
            set: function(value) { _button.classList.toggle("win-selected", value); }
        });
        Object.defineProperty(this, "disabled", {
            get: function() { try { return this.element.disabled; } catch (e) { return false; } },
            set: function(value) { try { this.element.disabled = value; } catch (e) {} }
        });
        // global 或 selection （始终显示或有选择时显示）
        Object.defineProperty(this, "section", {
            get: function() {
                if (_button.classList.contains("win-global")) return "global";
                if (_button.classList.contains("win-selection")) return "selection";
                return "none";
            },
            set: function(value) {
                _button.classList.remove("win-global");
                _button.classList.remove("win-selection");
                if (value == "global") _button.classList.add("win-global");
                if (value == "selection") _button.classList.add("win-selection");
            }
        });

        function selectHandler(event) {
            _button.classList.toggle("win-selected");
        }
        this.addEventListener = function(type, listener) {
            try { Windows.UI.Event.Util.addEvent(this.element, type, listener); } catch (e) {}
        };
        this.removeEventListener = function(type, listener) {
            try { Windows.UI.Event.Util.removeEvent(this.element, type, listener); } catch (e) {}
        };

    }

    function PMAppBarSeparator() {
        PMAppBarBaseMember.call(this);
        var _hr = document.createElement("hr");
        _hr.classList.add("win-command");
        _hr.classList.add("win-global");
        _hr.setAttribute("role", "separator");
        Object.defineProperty(this, "element", {
            get: function() { return _hr; },
            set: function(value) { _hr = value; }
        });
    }

    function PMAppBar(container) {
        var _container = container;
        var _enable = true;

        function init(node) {
            var classNames = [
                "win-overlay",
                "win-commandlayout",
                "win-appbar",
                "appbar"
            ]
            try {
                for (var i = 0; i < classNames.length; i++) {
                    if (!node.classList.contains(classNames[i]))
                        node.classList.add(classNames[i]);
                }
            } catch (e) {}
            try {
                node.appBarControl = this;
            } catch (e) {}
        }
        Object.defineProperty(this, "element", {
            get: function() { return _container; },
            set: function(value) {
                _container = value;
                init(value);
                // 将已有成员渲染到新的容器中
                try {
                    // 先移除所有成员 DOM（如果之前挂载过）
                    for (var i = 0; i < this._members.length; i++) {
                        try {
                            var mEl = this._members[i].element;
                            if (mEl && mEl.parentNode === _container) {
                                _container.removeChild(mEl);
                            }
                        } catch (e) {}
                    }
                    // 重新挂载所有成员，按数组顺序
                    for (i = 0; i < this._members.length; i++) {
                        try {
                            var el = this._members[i].element;
                            if (el) _container.appendChild(el);
                        } catch (e) {}
                    }
                } catch (e) {}
            }
        });

        // 成员管理
        this._members = [];

        // 返回内部数组引用（只读语义上）
        Object.defineProperty(this, "members", {
            get: function() { return this._members; }
        });

        // 添加成员到末尾，返回索引；若失败返回 -1
        this.add = function(member) {
            if (!member || !member.element) return -1;
            this._members.push(member);
            try {
                if (_container) _container.appendChild(member.element);
            } catch (e) {}
            this._updateSelectionVisibility();
            return this._members.length - 1;
        };
        this.addMember = this.add; // alias

        // 在指定索引处插入（如果 index 为 undefined 或超范围，则 append）
        this.insertAt = function(member, index) {
            if (!member || !member.element) return -1;
            var len = this._members.length;
            if (typeof index !== "number" || index < 0 || index > len) {
                return this.add(member);
            }
            this._members.splice(index, 0, member);
            try {
                if (_container) {
                    var refNode = _container.childNodes[index] || null;
                    _container.insertBefore(member.element, refNode);
                }
            } catch (e) {}
            this._updateSelectionVisibility();
            return index;
        };

        // remove 接受成员对象或索引
        this.remove = function(memberOrIndex) {
            var idx = -1;
            if (typeof memberOrIndex === "number") {
                idx = memberOrIndex;
            } else {
                idx = this._members.indexOf(memberOrIndex);
            }
            if (idx < 0 || idx >= this._members.length) return false;
            var removed = this._members.splice(idx, 1)[0];
            try {
                if (removed && removed.element && removed.element.parentNode) {
                    removed.element.parentNode.removeChild(removed.element);
                }
            } catch (e) {}
            this._updateSelectionVisibility();
            return true;
        };

        // 替换指定索引的成员，返回 true/false
        this.replaceAt = function(index, member) {
            if (!member || !member.element) return false;
            if (typeof index !== "number" || index < 0 || index >= this._members.length) return false;
            var old = this._members[index];
            this._members[index] = member;
            try {
                if (_container && old && old.element) {
                    // 如果 old.element 在容器中，直接 replaceChild
                    if (old.element.parentNode === _container) {
                        _container.replaceChild(member.element, old.element);
                    } else {
                        // 备用：在位置 index 插入
                        var ref = _container.childNodes[index] || null;
                        _container.insertBefore(member.element, ref);
                    }
                } else if (_container) {
                    // 没有 old 元素，直接 append
                    _container.appendChild(member.element);
                }
            } catch (e) {}
            this._updateSelectionVisibility();
            return true;
        };

        this.getMember = function(index) {
            return this._members[index];
        };

        this.indexOf = function(member) {
            return this._members.indexOf(member);
        };

        this.clear = function() {
            while (this._members.length) {
                var m = this._members.shift();
                try {
                    if (m && m.element && m.element.parentNode) {
                        m.element.parentNode.removeChild(m.element);
                    }
                } catch (e) {}
            }
        };

        var timer = null;
        var isupdating = false;

        function waitTimer(ms) {
            clearTimeout(timer);
            isupdating = true;
            timer = setTimeout(function(t) {
                isupdating = false;
                t = null;
            }, ms, timer);
        }
        Object.defineProperty(this, "isupdating", {
            get: function() { return isupdating; }
        });
        var touchHide = document.createElement("div");
        touchHide.classList.add("appbar-touchhide");
        touchHide.style.display = "none";
        Windows.UI.Event.Util.addEvent(touchHide, "click", function(event) {
            touchHide.style.display = "none";
            this.hide();
        }.bind(this));
        document.body.appendChild(touchHide);

        function showTouchHide() {
            if (touchHide == null || touchHide == void 0) {
                touchHide = document.createElement("div");
                touchHide.classList.add("appbar-touchhide");
            }
            touchHide.style.display = "";
        }

        function hideTouchHide() {
            touchHide.style.display = "none";
        }
        this.show = function() {
            try {
                if (!_enable) return;
                if (!this.element.classList.contains("show"))
                    this.element.classList.add("show");
                waitTimer(500);
                showTouchHide();
            } catch (e) {}
        };
        this.hide = function() {
            try {
                if (this.element.classList.contains("show"))
                    this.element.classList.remove("show");
                waitTimer(500);
                hideTouchHide();
            } catch (e) {}
        };
        this.setSelectionActive = function(active) {
            this._hasSelection = !!active;
            this._updateSelectionVisibility();
        };
        this._updateSelectionVisibility = function() {
            for (var i = 0; i < this._members.length; i++) {
                var el = this._members[i].element;
                if (el && el.classList && el.classList.contains("win-selection")) {
                    el.style.display = this._hasSelection ? "" : "none";
                }
            }
        };
        Object.defineProperty(this, "enabled", {
            get: function() { return _enable; },
            set: function(value) {
                _enable = value;
                if (!value) {
                    this.hide();
                }
            }
        });
        Object.defineProperty(this, "isshowing", {
            get: function() { return this.element.classList.contains("show"); },
            set: function(value) {
                if (value) {
                    this.show();
                } else {
                    this.hide();
                }
            }
        });
        this._eventShowHandler = function(event) {
            if (!this.isshowing) this.show();
            else this.hide();
        };
        this._eventHideHandler = function(event) {
            this.hide();
        };
        var EventUtil = Windows.UI.Event.Util;
        var self = this;
        EventUtil.addEvent(document, "contextmenu", function(event) {
            self._eventShowHandler(event);
            event.preventDefault();
        });
        var pressTimer = null;

        EventUtil.addEvent(document, "mousedown", function(event) {
            if (!self._enable) return;
            pressTimer = setTimeout(function(e) {
                self._eventShowHandler(e);
                event.preventDefault();
            }, 600, event);
        });

        EventUtil.addEvent(document, "mouseup", function() {
            clearTimeout(pressTimer);
        });
    }
    global.AppBar = {
        AppBar: PMAppBar,
        Command: PMAppBarCommand,
        Separator: PMAppBarSeparator,
        BaseMember: PMAppBarBaseMember
    };
})(this);