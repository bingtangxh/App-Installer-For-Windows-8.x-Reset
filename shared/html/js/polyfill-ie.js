(function(global) {
    if (typeof msWriteProfilerMark === "undefined") {
        function msWriteProfilerMark(swMark) {
            if (typeof performance !== "undefined" && typeof performance.mark === "function") {
                return performance.mark(swMark);
            } else if (typeof console !== "undefined" && typeof console.log === "function") {
                return console.log(swMark);
            }
        }
        module.exports = {
            msWriteProfilerMark: msWriteProfilerMark
        };
    }
})(this);
(function(global) {

    if (typeof global.Debug === "undefined") {
        var fakeDebug = {};

        // 基本属性
        fakeDebug.debuggerEnabled = true;
        fakeDebug.setNonUserCodeExceptions = false;

        // 常量
        fakeDebug.MS_ASYNC_CALLBACK_STATUS_ASSIGN_DELEGATE = 0;
        fakeDebug.MS_ASYNC_CALLBACK_STATUS_JOIN = 1;
        fakeDebug.MS_ASYNC_CALLBACK_STATUS_CHOOSEANY = 2;
        fakeDebug.MS_ASYNC_CALLBACK_STATUS_CANCEL = 3;
        fakeDebug.MS_ASYNC_CALLBACK_STATUS_ERROR = 4;

        fakeDebug.MS_ASYNC_OP_STATUS_SUCCESS = 1;
        fakeDebug.MS_ASYNC_OP_STATUS_CANCELED = 2;
        fakeDebug.MS_ASYNC_OP_STATUS_ERROR = 3;

        // 方法：输出
        fakeDebug.write = function(msg) {
            if (console && console.log) console.log("[Debug.write] " + msg);
        };
        fakeDebug.writeln = function(msg) {
            if (console && console.log) console.log("[Debug.writeln] " + msg);
        };

        // 方法：断言 / 中断
        fakeDebug.assert = function(cond, msg) {
            if (!cond) {
                if (console && console.error) {
                    console.error("[Debug.assert] Assertion failed: " + (msg || ""));
                }
                // 可选触发断点
                // debugger;
            }
        };
        fakeDebug.break = function() {
            debugger;
        };

        // 方法：异步跟踪（空实现）
        fakeDebug.msTraceAsyncCallbackCompleted = function() {};
        fakeDebug.msTraceAsyncCallbackStarting = function() {};
        fakeDebug.msTraceAsyncOperationCompleted = function() {};
        fakeDebug.msTraceAsyncOperationStarting = function() {};
        fakeDebug.msUpdateAsyncCallbackRelation = function() {};

        global.Debug = fakeDebug;
    }

})(this);
(function(global) {

    if (typeof global.setImmediate === "undefined") {
        var nextHandle = 1; // 唯一任务 id
        var tasksByHandle = {};
        var currentlyRunning = false;

        function addTask(fn, args) {
            tasksByHandle[nextHandle] = function() {
                fn.apply(undefined, args);
            };
            return nextHandle++;
        }

        function run(handle) {
            if (currentlyRunning) {
                // 如果已经在运行，延迟一下
                setTimeout(run, 0, handle);
            } else {
                var task = tasksByHandle[handle];
                if (task) {
                    currentlyRunning = true;
                    try {
                        task();
                    } finally {
                        delete tasksByHandle[handle];
                        currentlyRunning = false;
                    }
                }
            }
        }

        function installSetImmediate() {
            if (typeof MessageChannel !== "undefined") {
                var channel = new MessageChannel();
                channel.port1.onmessage = function(event) {
                    run(event.data);
                };
                return function() {
                    var handle = addTask(arguments[0], Array.prototype.slice.call(arguments, 1));
                    channel.port2.postMessage(handle);
                    return handle;
                };
            } else {
                // fallback: setTimeout
                return function() {
                    var handle = addTask(arguments[0], Array.prototype.slice.call(arguments, 1));
                    setTimeout(run, 0, handle);
                    return handle;
                };
            }
        }

        global.setImmediate = installSetImmediate();

        // 对应 clearImmediate
        if (typeof global.clearImmediate === "undefined") {
            global.clearImmediate = function(handle) {
                delete tasksByHandle[handle];
            };
        }
    }
})(this);
// attachEvent / detachEvent polyfill for IE11+
(function() {
    if (!Element.prototype.attachEvent) {
        Element.prototype.attachEvent = function(eventName, handler) {
            // IE attachEvent 的事件名需要 "on" 前缀
            eventName = eventName.toLowerCase();

            // 包装函数，模仿旧 IE 的 event 对象
            var wrapper = function(e) {
                e = e || window.event;

                // 兼容 IE 风格 event 属性
                e.srcElement = e.target || this;
                e.returnValue = true;
                e.cancelBubble = false;

                // 模拟 IE 的防止默认行为
                Object.defineProperty(e, "cancelBubble", {
                    set: function(val) {
                        if (val) e.stopPropagation();
                    }
                });
                Object.defineProperty(e, "returnValue", {
                    set: function(val) {
                        if (val === false) e.preventDefault();
                    }
                });

                // 调用原事件处理函数
                return handler.call(this, e);
            };

            // 存储 handler 映射，供 detachEvent 用
            if (!this._attachEventWrappers) this._attachEventWrappers = {};
            if (!this._attachEventWrappers[eventName]) this._attachEventWrappers[eventName] = [];

            this._attachEventWrappers[eventName].push({
                original: handler,
                wrapped: wrapper
            });

            this.addEventListener(eventName.replace(/^on/, ""), wrapper, false);
        };

        Element.prototype.detachEvent = function(eventName, handler) {
            eventName = eventName.toLowerCase();
            if (!this._attachEventWrappers || !this._attachEventWrappers[eventName]) return;

            var list = this._attachEventWrappers[eventName];

            for (var i = 0; i < list.length; i++) {
                if (list[i].original === handler) {
                    this.removeEventListener(eventName.replace(/^on/, ""), list[i].wrapped, false);
                    list.splice(i, 1);
                    break;
                }
            }
        };
    }
})();
(function(global) {
    "use strict";
    if (typeof Array.prototype.forEach === "undefined") {
        Array.prototype.forEach = function(callback, thisArg) {
            var T, k;
            if (this == null) {
                throw new TypeError(" this is null or not defined");
            }
            var O = Object(this);
            var len = O.length >>> 0;
            if (typeof callback !== "function") {
                throw new TypeError(callback + " is not a function");
            }
            if (arguments.length > 1) {
                T = thisArg;
            }
            k = 0;
            while (k < len) {
                var kValue;
                if (k in O) {
                    kValue = O[k];
                    callback.call(T, kValue, k, O);
                }
                k++;
            }
        };
    }
})(this);
(function(global) {
    if (typeof global.Map === "undefined") {
        function Map() {
            this.elements = new Array();
            // 获取Map元素个数
            this.size = function() {
                    return this.elements.length;
                },
                // 判断Map是否为空
                this.isEmpty = function() {
                    return (this.elements.length < 1);
                },
                // 删除Map所有元素
                this.clear = function() {
                    this.elements = new Array();
                },
                // 向Map中增加元素（key, value)
                this.put = function(_key, _value) {
                    if (this.containsKey(_key) == true) {
                        if (this.containsValue(_value)) {
                            if (this.remove(_key) == true) {
                                this.elements.push({
                                    key: _key,
                                    value: _value
                                });
                            }
                        } else {
                            this.elements.push({
                                key: _key,
                                value: _value
                            });
                        }
                    } else {
                        this.elements.push({
                            key: _key,
                            value: _value
                        });
                    }
                },
                // 向Map中增加元素（key, value)
                this.set = function(_key, _value) {
                    if (this.containsKey(_key) == true) {
                        if (this.containsValue(_value)) {
                            if (this.remove(_key) == true) {
                                this.elements.push({
                                    key: _key,
                                    value: _value
                                });
                            }
                        } else {
                            this.elements.push({
                                key: _key,
                                value: _value
                            });
                        }
                    } else {
                        this.elements.push({
                            key: _key,
                            value: _value
                        });
                    }
                },
                // 删除指定key的元素，成功返回true，失败返回false
                this.remove = function(_key) {
                    var bln = false;
                    try {
                        for (i = 0; i < this.elements.length; i++) {
                            if (this.elements[i].key == _key) {
                                this.elements.splice(i, 1);
                                return true;
                            }
                        }
                    } catch (e) {
                        bln = false;
                    }
                    return bln;
                },

                // 删除指定key的元素，成功返回true，失败返回false
                this.delete = function(_key) {
                    var bln = false;
                    try {
                        for (i = 0; i < this.elements.length; i++) {
                            if (this.elements[i].key == _key) {
                                this.elements.splice(i, 1);
                                return true;
                            }
                        }
                    } catch (e) {
                        bln = false;
                    }
                    return bln;
                },

                // 获取指定key的元素值value，失败返回null
                this.get = function(_key) {
                    try {
                        for (i = 0; i < this.elements.length; i++) {
                            if (this.elements[i].key == _key) {
                                return this.elements[i].value;
                            }
                        }
                    } catch (e) {
                        return null;
                    }
                },

                // set指定key的元素值value
                this.setValue = function(_key, _value) {
                    var bln = false;
                    try {
                        for (i = 0; i < this.elements.length; i++) {
                            if (this.elements[i].key == _key) {
                                this.elements[i].value = _value;
                                return true;
                            }
                        }
                    } catch (e) {
                        bln = false;
                    }
                    return bln;
                },

                // 获取指定索引的元素（使用element.key，element.value获取key和value），失败返回null
                this.element = function(_index) {
                    if (_index < 0 || _index >= this.elements.length) {
                        return null;
                    }
                    return this.elements[_index];
                },

                // 判断Map中是否含有指定key的元素
                this.containsKey = function(_key) {
                    var bln = false;
                    try {
                        for (i = 0; i < this.elements.length; i++) {
                            if (this.elements[i].key == _key) {
                                bln = true;
                            }
                        }
                    } catch (e) {
                        bln = false;
                    }
                    return bln;
                },

                // 判断Map中是否含有指定key的元素
                this.has = function(_key) {
                    var bln = false;
                    try {
                        for (i = 0; i < this.elements.length; i++) {
                            if (this.elements[i].key == _key) {
                                bln = true;
                            }
                        }
                    } catch (e) {
                        bln = false;
                    }
                    return bln;
                },

                // 判断Map中是否含有指定value的元素
                this.containsValue = function(_value) {
                    var bln = false;
                    try {
                        for (i = 0; i < this.elements.length; i++) {
                            if (this.elements[i].value == _value) {
                                bln = true;
                            }
                        }
                    } catch (e) {
                        bln = false;
                    }
                    return bln;
                },

                // 获取Map中所有key的数组（array）
                this.keys = function() {
                    var arr = new Array();
                    for (i = 0; i < this.elements.length; i++) {
                        arr.push(this.elements[i].key);
                    }
                    return arr;
                },

                // 获取Map中所有value的数组（array）
                this.values = function() {
                    var arr = new Array();
                    for (i = 0; i < this.elements.length; i++) {
                        arr.push(this.elements[i].value);
                    }
                    return arr;
                };

            /**
             * map遍历数组
             * @param callback [function] 回调函数；
             * @param context [object] 上下文；
             */
            this.forEach = function forEach(callback, context) {
                context = context || window;

                //IE6-8下自己编写回调函数执行的逻辑
                var newAry = new Array();
                for (var i = 0; i < this.elements.length; i++) {
                    if (typeof callback === 'function') {
                        var val = callback.call(context, this.elements[i].value, this.elements[i].key, this.elements);
                        newAry.push(this.elements[i].value);
                    }
                }
                return newAry;
            }

        }
        global.Map = Map;
    }
})(this);
(function(global) {
    if (typeof global.Set === "undefined") {
        function Set() {
            var items = {};
            this.size = 0;
            // 实现has方法
            // has(val)方法
            this.has = function(val) {
                // 对象都有hasOwnProperty方法，判断是否拥有特定属性
                return items.hasOwnProperty(val);
            };

            // 实现add
            this.add = function(val) {
                if (!this.has(val)) {
                    items[val] = val;
                    this.size++; // 累加集合成员数量
                    return true;
                }
                return false;
            };

            // delete(val)方法
            this.delete = function(val) {
                if (this.has(val)) {
                    delete items[val]; // 将items对象上的属性删掉
                    this.size--;
                    return true;
                }
                return false;
            };
            // clear方法
            this.clear = function() {
                items = {}; // 直接将集合赋一个空对象即可
                this.size = 0;
            };

            // keys()方法
            this.keys = function() {
                return Object.keys(items); // 返回遍历集合的所有键名的数组
            };
            // values()方法
            this.values = function() {
                return Object.values(items); // 返回遍历集合的所有键值的数组
            };

            // forEach(fn, context)方法
            this.forEach = function(fn, context) {
                for (var i = 0; i < this.size; i++) {
                    var item = Object.keys(items)[i];
                    fn.call(context, item, item, items);
                }
            };

            // 并集
            this.union = function(other) {
                var union = new Set();
                var values = this.values();
                for (var i = 0; i < values.length; i++) {
                    union.add(values[i]);
                }
                values = other.values(); // 将values重新赋值为新的集合
                for (var i = 0; i < values.length; i++) {
                    union.add(values[i]);
                }

                return union;
            };
            // 交集
            this.intersect = function(other) {
                var intersect = new Set();
                var values = this.values();
                for (var i = 0; i < values.length; i++) {
                    if (other.has(values[i])) { // 查看是否也存在于other中
                        intersect.add(values[i]); // 存在的话就像intersect中添加元素
                    }
                }
                return intersect;
            };

            // 差集
            this.difference = function(other) {
                var difference = new Set();
                var values = this.values();
                for (var i = 0; i < values.length; i++) {
                    if (!other.has(values[i])) { // 将不存在于other集合中的添加到新的集合中
                        difference.add(values[i]);
                    }
                }
                return difference;
            };


        }
        global.Set = Set;
    }
})(this);
(function(global) {
    // 如果原生 WeakMap 已存在，则不覆盖
    if (typeof global.WeakMap !== "undefined") {
        return;
    }

    function WeakMap(iterable) {
        // 必须使用 new 调用
        if (!(this instanceof WeakMap)) {
            throw new TypeError('Constructor WeakMap requires "new"');
        }

        // 私有存储：每个实例独立维护一个键值对数组
        var entries = [];

        // 验证 key 必须是对象或函数（不能是 null 或原始值）
        function validateKey(key) {
            if (key === null || (typeof key !== 'object' && typeof key !== 'function')) {
                throw new TypeError('WeakMap key must be an object');
            }
        }

        // 设置键值对
        this.set = function(key, value) {
            validateKey(key);
            for (var i = 0; i < entries.length; i++) {
                if (entries[i][0] === key) {
                    entries[i][1] = value;
                    return this;
                }
            }
            entries.push([key, value]);
            return this;
        };

        // 获取键对应的值
        this.get = function(key) {
            validateKey(key);
            for (var i = 0; i < entries.length; i++) {
                if (entries[i][0] === key) {
                    return entries[i][1];
                }
            }
            return undefined;
        };

        // 判断是否存在指定键
        this.has = function(key) {
            validateKey(key);
            for (var i = 0; i < entries.length; i++) {
                if (entries[i][0] === key) {
                    return true;
                }
            }
            return false;
        };

        // 删除指定键及其值
        this.delete = function(key) {
            validateKey(key);
            for (var i = 0; i < entries.length; i++) {
                if (entries[i][0] === key) {
                    entries.splice(i, 1);
                    return true;
                }
            }
            return false;
        };

        // 处理可选的初始化参数（iterable，例如 [[key1, value1], [key2, value2]]）
        if (iterable !== null && iterable !== undefined) {
            // 支持数组或类数组（具备 forEach 方法的对象）
            if (typeof iterable.forEach === 'function') {
                var self = this;
                iterable.forEach(function(item) {
                    if (!Array.isArray(item) || item.length < 2) {
                        throw new TypeError('Iterator value is not an entry object');
                    }
                    self.set(item[0], item[1]);
                });
            } else if (typeof iterable.length === 'number') {
                // 类数组对象（如 arguments）
                for (var i = 0; i < iterable.length; i++) {
                    var item = iterable[i];
                    if (!Array.isArray(item) || item.length < 2) {
                        throw new TypeError('Iterator value is not an entry object');
                    }
                    this.set(item[0], item[1]);
                }
            } else {
                throw new TypeError('WeakMap iterable is not iterable');
            }
        }
    }

    // 挂载到全局对象
    global.WeakMap = WeakMap;
})(this);