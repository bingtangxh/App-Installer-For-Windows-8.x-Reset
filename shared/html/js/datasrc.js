(function(global) {
    "use strict";
    global.DataView = {
        ChangeType: {
            add: "add",
            remove: "remove",
            change: "change",
            clear: "clear",
            move: "move",
            sort: "sort",
        },
    };
    var childAnimeDuration = 120;
    var parentAnimeDuration = 400;


    function showItemAmine(node) {
        return Windows.UI.Animation.runAsync(node, [
            Windows.UI.Animation.Keyframes.Scale.up,
            Windows.UI.Animation.Keyframes.Opacity.visible,
        ], childAnimeDuration);
    }

    function hideItemAmine(node) {
        return Windows.UI.Animation.runAsync(node, [
            Windows.UI.Animation.Keyframes.Scale.down,
            Windows.UI.Animation.Keyframes.Opacity.hidden,
        ], childAnimeDuration);
    }

    function noAnime(node) {
        return Promise.resolve(node);
    }

    function runShowAnime(node, enable) {
        if (enable === void 0) enable = true;
        if (!enable) return noAnime(node);
        return showItemAmine(node);
    }

    function runHideAnime(node, enable) {
        if (enable === void 0) enable = true;
        if (!enable) return noAnime(node);
        return hideItemAmine(node);
    }


    function updateItemAmine(node, updateCallback) {
        return Windows.UI.Animation.runAsync(node, [
            Windows.UI.Animation.Keyframes.Opacity.hidden,
            Windows.UI.Animation.Keyframes.Scale.down
        ], 120).then(function() {
            if (updateCallback && typeof updateCallback === 'function') {
                updateCallback(node);
            }
            return Windows.UI.Animation.runAsync(node, [
                Windows.UI.Animation.Keyframes.Opacity.visible,
                Windows.UI.Animation.Keyframes.Scale.up
            ], 120);
        }).then(function() {
            return node;
        });
    }

    function PMChangeEvent(type, datas, detailOperation) {
        this.type = type; // ChangeType
        this.datas = datas || []; // 受影响的数据
        this.detail = detailOperation || null;
    }

    function PMDataSource() {
        var _list = [];
        var _listeners = [];

        var _keySelector = null;
        var _autoKeySeed = 1;
        this.setKeySelector = function(fn) {
            _keySelector = (typeof fn === "function") ? fn : null;
        };

        function getKey(item) {
            if (!item) return null;

            // 用户提供
            if (_keySelector) {
                return _keySelector(item);
            }

            // 自动注入（对象）
            if (typeof item === "object") {
                if (item.__pm_key !== void 0) {
                    return item.__pm_key;
                }

                try {
                    Object.defineProperty(item, "__pm_key", {
                        value: "pm_" + (_autoKeySeed++),
                        enumerable: false
                    });
                } catch (e) {
                    // IE10 兜底
                    item.__pm_key = "pm_" + (_autoKeySeed++);
                }
                return item.__pm_key;
            }

            // 原始类型兜底
            return typeof item + ":" + item;
        }

        this.subscribe = function(fn) {
            if (typeof fn === "function") {
                _listeners.push(fn);
            }
        };

        function emit(evt) {
            for (var i = 0; i < _listeners.length; i++) {
                _listeners[i](evt);
            }
        }
        this.indexOf = function(item) {
            var key = getKey(item);
            for (var i = 0; i < _list.length; i++) {
                if (getKey(_list[i]) === key) {
                    return i;
                }
            }
            return -1;
        };
        this.add = function(item) {
            _list.push(item);
            emit(new PMChangeEvent(
                DataView.ChangeType.add, [{ item: item, index: _list.length - 1, key: getKey(item) }]
            ));
        };
        this.removeAt = function(index) {
            if (index < 0 || index >= _list.length) return;
            var item = _list.splice(index, 1)[0];
            emit(new PMChangeEvent(
                DataView.ChangeType.remove, [{ item: item, index: index, key: getKey(item) }]
            ));
        };
        this.remove = function(item) {
            var index = this.indexOf(item);
            if (index >= 0) {
                this.removeAt(index);
            }
        };
        this.changeAt = function(index, newItem) {
            if (index < 0 || index >= _list.length) return;
            _list[index] = newItem;
            emit(new PMChangeEvent(
                DataView.ChangeType.change, [{ item: newItem, index: index, key: getKey(newItem) }]
            ));
        };
        this.change = function(oldItem, newItem) {
            var index = this.indexOf(oldItem);
            if (index >= 0) {
                this.changeAt(index, newItem);
            }
        };
        this.clear = function() {
            _list.length = 0;
            emit(new PMChangeEvent(
                DataView.ChangeType.clear
            ));
        };
        this.move = function(from, to) {
            if (from === to ||
                from < 0 || to < 0 ||
                from >= _list.length || to >= _list.length) {
                return;
            }
            var item = _list.splice(from, 1)[0];
            _list.splice(to, 0, item);
            emit(new PMChangeEvent(
                DataView.ChangeType.move, [item], { from: from, to: to }
            ));
        };
        this.sort = function(compareFn) {
            _list.sort(compareFn);
            emit(new PMChangeEvent(
                DataView.ChangeType.sort,
                _list.slice(0), { compare: compareFn }
            ));
        };
        this.get = function() {
            return _list.slice(0);
        };
        this.addList = function(list, keySelector) {
            if (!list || !list.length) return;

            var added = [];
            var changed = [];

            var useKey = keySelector !== void 0;
            var getKey;

            if (keySelector === null) {
                getKey = function(item) {
                    return item && item.id;
                };
            } else if (typeof keySelector === "function") {
                getKey = keySelector;
            }

            for (var i = 0; i < list.length; i++) {
                var item = list[i];

                if (!useKey) {
                    _list.push(item);
                    added.push({ item: item, index: _list.length - 1 });
                    continue;
                }

                var key = getKey(item);
                if (key === void 0) {
                    _list.push(item);
                    added.push({ item: item, index: _list.length - 1, key: key });
                    continue;
                }

                var found = -1;
                for (var j = 0; j < _list.length; j++) {
                    if (getKey(_list[j]) === key) {
                        found = j;
                        break;
                    }
                }

                if (found >= 0) {
                    _list[found] = item;
                    changed.push({ item: item, index: found, key: key });
                } else {
                    _list.push(item);
                    added.push({ item: item, index: _list.length - 1, key: key });
                }
            }

            // 统一发出一个事件
            if (added.length > 0) {
                emit(new PMChangeEvent(DataView.ChangeType.add, added));
            }
            if (changed.length > 0) {
                emit(new PMChangeEvent(DataView.ChangeType.change, changed));
            }
        };
        this.updateList = function(list, fnGetKey) {
            if (!list) list = [];

            var getKey;

            if (fnGetKey === null) {
                getKey = function(item) {
                    return item && item.id;
                };
            } else if (typeof fnGetKey === "function") {
                getKey = fnGetKey;
            } else {
                // 不提供 key：直接整体替换
                _list = list.slice(0);
                emit(new PMChangeEvent(
                    DataView.ChangeType.clear
                ));
                emit(new PMChangeEvent(
                    DataView.ChangeType.add,
                    list.map(function(item, index) {
                        return { item: item, index: index };
                    })
                ));
                return;
            }

            var oldList = _list;
            var newList = list;

            var oldKeyIndex = {};
            var newKeyIndex = {};

            var i;

            // 建立旧列表 key → index
            for (i = 0; i < oldList.length; i++) {
                var ok = getKey(oldList[i]);
                if (ok !== void 0) {
                    oldKeyIndex[ok] = i;
                }
            }

            // 建立新列表 key → index
            for (i = 0; i < newList.length; i++) {
                var nk = getKey(newList[i]);
                if (nk !== void 0) {
                    newKeyIndex[nk] = i;
                }
            }

            var added = [];
            var changed = [];
            var removed = [];

            for (i = oldList.length - 1; i >= 0; i--) {
                var oldItem = oldList[i];
                var oldKey = getKey(oldItem);

                if (oldKey === void 0 || newKeyIndex[oldKey] === void 0) {
                    removed.push({
                        item: oldItem,
                        index: i,
                        key: oldKey
                    });
                }
            }

            for (i = 0; i < newList.length; i++) {
                var newItem = newList[i];
                var newKey = getKey(newItem);

                if (newKey === void 0 || oldKeyIndex[newKey] === void 0) {
                    added.push({
                        item: newItem,
                        index: i,
                        key: newKey
                    });
                } else {
                    var oldIndex = oldKeyIndex[newKey];
                    var oldItem2 = oldList[oldIndex];

                    if (oldItem2 !== newItem) {
                        changed.push({
                            item: newItem,
                            index: oldIndex,
                            key: newKey
                        });
                    }
                }
            }

            if (removed.length > 0) {
                for (i = 0; i < removed.length; i++) {
                    _list.splice(removed[i].index, 1);
                }
                emit(new PMChangeEvent(
                    DataView.ChangeType.remove,
                    removed
                ));
            }

            _list = newList.slice(0);

            if (added.length > 0) {
                emit(new PMChangeEvent(
                    DataView.ChangeType.add,
                    added
                ));
            }

            if (changed.length > 0) {
                emit(new PMChangeEvent(
                    DataView.ChangeType.change,
                    changed
                ));
            }
        };
        this._getKey = getKey;
    }
    var MAX_ANIMATE_COUNT = 100;

    function PMDataListView(container, templateFn) {
        this.container = container;
        this.templateFn = templateFn;
        this.listViewControl = this;
        this._emptyView = null;
        // === 新增 ===
        this._filter = null;

        this._searchHandler = null;
        this._searchText = null;
        this._searchSuggestProvider = null;

        this.onSearchSuggest = null; // function(text, list)
        this._isSearching = false;
        this.onsearchstart = null;
        this.onsearchend = null;
    }
    PMDataListView.prototype.bind = function(ds) {
        var self = this;
        this._ds = ds;

        self.container.innerHTML = "";

        var items = ds.get();

        // 动画队列，保证异步操作不会乱序
        var queue = Promise.resolve();

        function renderItem(data, index) {
            var el = self.templateFn(data, index);

            var key = ds && ds._getKey ? ds._getKey(data) : null;

            el.__pm_item = data;
            el.__pm_key = key;

            if (key != null) {
                el.setAttribute("data-pm-key", key);
            }

            el.addEventListener("click", function() {
                self._toggleSelect(el);
            });

            return el;
        }


        // 初始化渲染
        for (var i = 0; i < items.length; i++) {
            self.container.appendChild(renderItem(items[i], i));
        }
        // 初始化 emptyView 状态
        self._updateEmptyView();

        ds.subscribe(function(evt) {

            // 把每次事件放进队列，保证顺序执行
            queue = queue.then(function() {
                switch (evt.type) {

                    case DataView.ChangeType.add:
                        {
                            // evt.datas = [{item, index}, ...]
                            var datas = evt.datas;

                            // 先批量 append 到 DOM（顺序必须保持）
                            var nodes = [];
                            for (var i = 0; i < datas.length; i++) {
                                var n = renderItem(datas[i].item, datas[i].index);
                                n.style.display = "none";
                                nodes.push(n);
                                self.container.appendChild(n);
                            }
                            var enableAnime = datas.length <= MAX_ANIMATE_COUNT;
                            if (!enableAnime) {
                                return Promise.resolve();
                            }
                            // 如果数量>=20，动画串行，否则并行
                            if (datas.length <= 20) {
                                var promises = [];
                                for (var j = 0; j < nodes.length; j++) {
                                    promises.push((function(node) {
                                        node.style.display = "";
                                        return showItemAmine(node);
                                    })(nodes[j]));
                                }
                                return Promise.all(promises);
                            } else {
                                // 串行
                                var p = Promise.resolve();
                                var group = [];
                                for (var k = 0; k < nodes.length; k++) {
                                    group.push((function(node) {
                                            node.style.display = "";
                                            return showItemAmine(node);
                                        })
                                        (nodes[k]));
                                    if (group.length === 20 || k === nodes.length - 1) {
                                        (function(g) {
                                            p = p.then(function() {
                                                return Promise.join(g);
                                            });
                                        })(group);
                                        group = [];
                                    }
                                }
                                (function(g) {
                                    p = p.then(function() {
                                        return Promise.join(g);
                                    });
                                })(group);
                                return p;
                            }
                        }

                    case DataView.ChangeType.remove:
                        {
                            var info = evt.datas[0];
                            var node = self._findNodeByKey(info.key);
                            if (!node) return;

                            return hideItemAmine(node).then(function() {
                                self.container.removeChild(node);
                            });
                        }

                    case DataView.ChangeType.change:
                        {
                            var info = evt.datas[0];
                            var oldNode = self._findNodeByKey(info.key);
                            if (!oldNode) return;

                            return hideItemAmine(oldNode).then(function() {
                                var newNode = renderItem(info.item);
                                self.container.replaceChild(newNode, oldNode);
                                return showItemAmine(newNode);
                            });
                        }


                    case DataView.ChangeType.clear:
                        self.container.innerHTML = "";
                        return Promise.resolve();

                    case DataView.ChangeType.move:
                        {
                            var info = evt.datas[0];
                            var node = self._findNodeByKey(info.key);
                            if (!node) return;

                            var ref = self.container.children[evt.detail.to] || null;
                            self.container.insertBefore(node, ref);
                            return Promise.resolve();
                        }

                    case DataView.ChangeType.sort:
                        {
                            self.container.innerHTML = "";
                            for (var i = 0; i < evt.datas.length; i++) {
                                self.container.appendChild(renderItem(evt.datas[i], i));
                            }
                            return Promise.resolve();
                        }
                }
                promises.push(self._refreshVisibility());
                return Promise.join(promises);
            });
        });
    };
    PMDataListView.prototype._findNodeByKey = function(key) {
        if (key == null) return null;

        var children = this.container.children;
        for (var i = 0; i < children.length; i++) {
            if (children[i].__pm_key === key) {
                return children[i];
            }
        }
        return null;
    };

    PMDataListView.prototype._toggleSelect = function(ele) {
        // 如果选择模式为 none，则不处理
        if (this.selectionMode === "none") return;

        var isSelected = ele.classList.contains("selected");

        if (this.selectionMode === "single") {
            // 单选：先取消所有选中
            this._clearSelected();
            if (!isSelected) {
                ele.classList.add("selected");
            }
        } else if (this.selectionMode === "multiple") {
            // 多选：点一次切换状态
            if (isSelected) {
                ele.classList.remove("selected");
            } else {
                ele.classList.add("selected");
            }
        }
    };
    PMDataListView.prototype._clearSelected = function() {
        var selected = this.container.querySelectorAll(".selected");
        for (var i = 0; i < selected.length; i++) {
            selected[i].classList.remove("selected");
        }
    };
    Object.defineProperty(PMDataListView.prototype, "selectionMode", {
        get: function() {
            return this._selectionMode || "none";
        },
        set: function(value) {
            var mode = String(value).toLowerCase();
            if (mode !== "none" && mode !== "single" && mode !== "multiple") {
                mode = "none";
            }
            this._selectionMode = mode;

            // 切换模式时，清空选中状态（可选）
            if (mode === "none") {
                this._clearSelected();
            }
            if (mode === "single") {
                // 单选模式：如果多选了多个，保留第一个
                var selected = this.container.querySelectorAll(".selected");
                if (selected.length > 1) {
                    for (var i = 1; i < selected.length; i++) {
                        selected[i].classList.remove("selected");
                    }
                }
            }
        }
    });
    Object.defineProperty(PMDataListView.prototype, "selectedItems", {
        get: function() {
            return Array.prototype.slice.call(this.container.querySelectorAll(".selected"));
        }
    });
    PMDataListView.prototype._updateEmptyView = function() {
        if (!this._emptyView) return;

        // container 中是否还有 item
        var hasItem = this.container.children.length > 0;

        if (hasItem) {
            if (this._emptyView.parentNode) {
                this._emptyView.style.display = "none";
            }
        } else {
            if (!this._emptyView.parentNode) {
                this.container.appendChild(this._emptyView);
            }
            this._emptyView.style.display = "";
        }
    };
    Object.defineProperty(PMDataListView.prototype, "emptyView", {
        get: function() {
            return this._emptyView;
        },
        set: function(value) {
            // 只接受 HTMLElement 或 null / undefined
            if (value !== null && value !== void 0 && !(value instanceof HTMLElement)) {
                return;
            }

            // 移除旧的
            if (this._emptyView && this._emptyView.parentNode) {
                this._emptyView.parentNode.removeChild(this._emptyView);
            }

            this._emptyView = value || null;

            // 设置后立刻刷新一次
            this._updateEmptyView();
        }
    });
    PMDataListView.prototype._isItemVisible = function(item) {
        // 1️⃣ filter
        if (this._filter) {
            if (!this._filter(item)) return false;
        }

        // 2️⃣ search（自动启用 / 禁用）
        var handler = this._searchHandler;
        var text = this._searchText;

        if (typeof handler === "function") {
            if (text != null) {
                text = ("" + text).replace(/^\s+|\s+$/g, "");
            }

            if (text && text.length > 0) {
                if (!handler(text, item)) {
                    return false;
                }
            }
        }

        return true;
    };
    PMDataListView.prototype._refreshVisibility = function() {
        var self = this;
        var children = self.container.children;
        var animes = [];
        for (var i = 0; i < children.length; i++) {
            (function(node) {
                var item = node.__pm_item;
                if (!item) return;

                var visible = self._isItemVisible(item);

                var enableAnime = animes.length < MAX_ANIMATE_COUNT;
                if (visible) {
                    if (node.style.display === "none") {
                        node.style.display = "";
                        animes.push(runShowAnime(node, enableAnime));
                    }
                } else {
                    if (node.style.display !== "none") {
                        // 移除选择状态
                        node.classList.remove("selected");
                        animes.push(runHideAnime(node, enableAnime).then(function() {
                            node.style.display = "none";
                        }));
                    }
                }
            })(children[i]);
        }
        return Promise.join(animes);
    };
    Object.defineProperty(PMDataListView.prototype, "filter", {
        get: function() {
            return this._filter;
        },
        set: function(fn) {
            this._filter = (typeof fn === "function") ? fn : null;
            this._refreshVisibility();
        }
    });
    Object.defineProperty(PMDataListView.prototype, "searchHandler", {
        get: function() {
            return this._searchHandler;
        },
        set: function(fn) {
            this._searchHandler = (typeof fn === "function") ? fn : null;
            this._refreshVisibility();
        }
    });
    Object.defineProperty(PMDataListView.prototype, "searchText", {
        get: function() {
            return this._searchText;
        },
        set: function(text) {
            var oldText = this._searchText;

            this._searchText = text;

            var oldActive = !!(oldText && oldText.trim());
            var newActive = !!(text && ("" + text).trim());

            //if (!oldActive && newActive) {
            this._isSearching = true;
            this._emitSearchEvent("searchstart");
            //}

            var handler = this._searchHandler;
            var provider = this._searchSuggestProvider;
            var cb = this.onSearchSuggest;

            var t = text;
            if (t != null) {
                t = ("" + t).replace(/^\s+|\s+$/g, "");
            }

            // 搜索建议
            if (
                typeof handler === "function" &&
                t &&
                t.length > 0 &&
                typeof provider === "function" &&
                typeof cb === "function"
            ) {
                var list = provider(t);
                if (list && list.length) {
                    cb(t, list.slice(0, 10));
                }
            }
            var self = this;
            var func = function() {
                //if (oldActive && !newActive) {
                self._isSearching = false;
                self._emitSearchEvent("searchend");
                //}
            };
            this._refreshVisibility().done(func, func);
        }
    });
    Object.defineProperty(PMDataListView.prototype, "searchSuggestProvider", {
        get: function() {
            return this._searchSuggestProvider;
        },
        set: function(fn) {
            this._searchSuggestProvider = (typeof fn === "function") ? fn : null;
        }
    });
    Object.defineProperty(PMDataListView.prototype, "findItemLength", {
        get: function() {
            var count = 0;
            var children = this.container.children;
            for (var i = 0; i < children.length; i++) {
                var item = children[i].__pm_item;
                if (this._isItemVisible(item)) {
                    count++;
                }
            }
            return count;
        }
    });
    PMDataListView.prototype._emitSearchEvent = function(type) {
        if (typeof this["on" + type] === "function") {
            try {
                this["on" + type].call(this);
            } catch (e) {}
        }

        try {
            var ev = document.createEvent("Event");
            ev.initEvent(type, true, true);
            this.container.dispatchEvent(ev);
        } catch (e) {}
    };
    PMDataListView.prototype.refresh = function() {
        this._refreshVisibility();
    };
    global.DataView.ChangeEvent = PMChangeEvent;
    global.DataView.DataSource = PMDataSource;
    global.DataView.ListView = PMDataListView;
})(this);