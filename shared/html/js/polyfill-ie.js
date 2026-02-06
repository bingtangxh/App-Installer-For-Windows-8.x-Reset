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