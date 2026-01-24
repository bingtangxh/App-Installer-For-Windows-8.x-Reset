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
        this.add = function(item) {
            _list.push(item);
            emit(new PMChangeEvent(
                DataView.ChangeType.add, [item], { index: _list.length - 1 }
            ));
        };
        this.removeAt = function(index) {
            if (index < 0 || index >= _list.length) return;
            var item = _list.splice(index, 1)[0];
            emit(new PMChangeEvent(
                DataView.ChangeType.remove, [item], { index: index }
            ));
        };
        this.changeAt = function(index, newItem) {
            if (index < 0 || index >= _list.length) return;
            _list[index] = newItem;
            emit(new PMChangeEvent(
                DataView.ChangeType.change, [newItem], { index: index }
            ));
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

            // 1️⃣ 找 remove
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

            // 2️⃣ 找 add / change
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

            // 3️⃣ 执行 remove（从后往前）
            if (removed.length > 0) {
                for (i = 0; i < removed.length; i++) {
                    _list.splice(removed[i].index, 1);
                }
                emit(new PMChangeEvent(
                    DataView.ChangeType.remove,
                    removed
                ));
            }

            // 4️⃣ 执行 add / change（重建顺序）
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

    }

    function PMDataListView(container, templateFn) {
        this.container = container;
        this.templateFn = templateFn;
        this.listViewControl = this;
    }
    PMDataListView.prototype.bind = function(ds) {
        var self = this;
        var items = ds.get();
        self.container.innerHTML = "";

        // 动画队列，保证异步操作不会乱序
        var queue = Promise.resolve();

        function renderItem(data, index) {
            var el = self.templateFn(data, index);

            el.addEventListener("click", function() {
                self._toggleSelect(el);
            });

            return el;
        }

        // 初始化渲染
        for (var i = 0; i < items.length; i++) {
            self.container.appendChild(renderItem(items[i], i));
        }

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
                                nodes.push(n);
                                self.container.appendChild(n);
                            }

                            // 如果数量>=20，动画并行，否则串行
                            if (datas.length >= 20) {
                                var promises = [];
                                for (var j = 0; j < nodes.length; j++) {
                                    promises.push(showItemAmine(nodes[j]));
                                }
                                return Promise.all(promises);
                            } else {
                                // 串行
                                var p = Promise.resolve();
                                for (var k = 0; k < nodes.length; k++) {
                                    (function(node) {
                                        p = p.then(function() {
                                            return showItemAmine(node);
                                        });
                                    })(nodes[k]);
                                }
                                return p;
                            }
                        }

                    case DataView.ChangeType.remove:
                        {
                            var node = self.container.children[evt.detail.index];
                            if (!node) return;

                            // 隐藏动画完成后再移除
                            return hideItemAmine(node).then(function() {
                                self.container.removeChild(node);
                            });
                        }

                    case DataView.ChangeType.change:
                        {
                            var oldNode = self.container.children[evt.detail.index];
                            if (!oldNode) return;

                            // 先淡出旧节点
                            return hideItemAmine(oldNode).then(function() {
                                // 替换节点
                                var newNode = renderItem(evt.datas[0], evt.detail.index);
                                self.container.replaceChild(newNode, oldNode);

                                // 再淡入新节点
                                return showItemAmine(newNode);
                            });
                        }

                    case DataView.ChangeType.clear:
                        self.container.innerHTML = "";
                        return Promise.resolve();

                    case DataView.ChangeType.move:
                        {
                            var node = self.container.children[evt.detail.from];
                            var ref = self.container.children[evt.detail.to] || null;
                            if (node) self.container.insertBefore(node, ref);
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
            });
        });
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

    global.DataView.ChangeEvent = PMChangeEvent;
    global.DataView.DataSource = PMDataSource;
    global.DataView.ListView = PMDataListView;
})(this);