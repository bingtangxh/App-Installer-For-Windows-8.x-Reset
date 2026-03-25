(function(global) {
    "use strict";
    /**
     * DomEvent 命名空间
     * @namespace DomEvent
     */
    if (!global.DomEvent) global.DomEvent = {};
    /**
     * DOM 事件监控类型常量
     * @readonly
     * @enum {string}
     */
    global.DomEvent.Types = Object.freeze({
        resize: "resize", // 尺寸变化
        position: "position", // 位置变化
        attribute: "attribute", // 属性变化
        child: "child", // 子节点变化
        text: "text", // 文本内容变化
        attach: "attach", // 节点附加到 DOM
        detach: "detach", // 节点从 DOM 移除
        visible: "visible", // 可见性变化
        scrollresize: "scrollresize" // 滚动尺寸变化
    });
})(window);

(function(global) {
    "use strict";
    if (!global.DomEvent) global.DomEvent = {};
    if (!global.DomEvent.Types) throw new Error("DomEvent.Types must be defined first.");
    var Types = global.DomEvent.Types;
    /**
     * DOM 节点监控器
     * @namespace DomEvent.Monitor
     */
    var Monitor = (function() {
        var registry = {}; // 存储所有节点及对应事件
        var polling = false; // 是否正在轮询
        var loopTimer = null; // 定时器
        var interval = 120; // 轮询间隔 ms
        // 初始化 registry，每种事件类型对应 Map
        Object.keys(Types).forEach(function(key) {
            registry[Types[key]] = new Map();
        });
        /**
         * 获取元素快照
         * @param {HTMLElement} el DOM 元素
         * @returns {Object} 元素快照对象
         */
        function getSnapshot(el) {
            return {
                rect: el.getBoundingClientRect(),
                text: el.textContent,
                attr: el.attributes.length,
                child: el.childNodes.length,
                attached: document.body.contains(el),
                visible: !!(el.offsetWidth || el.offsetHeight),
                scrollWidth: el.scrollWidth,
                scrollHeight: el.scrollHeight
            };
        }
        /**
         * 判断元素快照是否发生变化
         * @param {string} type 事件类型
         * @param {Object} oldSnap 旧快照
         * @param {Object} newSnap 新快照
         * @returns {boolean} 是否发生变化
         */
        function hasChanged(type, oldSnap, newSnap) {
            switch (type) {
                case Types.resize:
                    return oldSnap.rect.width !== newSnap.rect.width ||
                        oldSnap.rect.height !== newSnap.rect.height;
                case Types.position:
                    return oldSnap.rect.top !== newSnap.rect.top ||
                        oldSnap.rect.left !== newSnap.rect.left;
                case Types.attribute:
                    return oldSnap.attr !== newSnap.attr;
                case Types.child:
                    return oldSnap.child !== newSnap.child;
                case Types.text:
                    return oldSnap.text !== newSnap.text;
                case Types.attach:
                    return !oldSnap.attached && newSnap.attached;
                case Types.detach:
                    return oldSnap.attached && !newSnap.attached;
                case Types.visible:
                    return oldSnap.visible !== newSnap.visible;
                case Types.scrollresize:
                    return oldSnap.scrollWidth !== newSnap.scrollWidth ||
                        oldSnap.scrollHeight !== newSnap.scrollHeight;
            }
            return false;
        }
        /**
         * 执行轮询检测
         * @private
         */
        function poll() {
            Object.keys(registry).forEach(function(type) {
                registry[type].forEach(function(data, el) {
                    if (!document.body.contains(el)) {
                        registry[type].delete(el);
                        return;
                    }
                    var newSnap = getSnapshot(el);
                    if (hasChanged(type, data.snapshot, newSnap)) {
                        data.snapshot = newSnap;
                        data.handlers.forEach(function(handler) {
                            try {
                                handler.call(el, {
                                    type: type,
                                    rect: newSnap.rect,
                                    text: newSnap.text,
                                    visible: newSnap.visible
                                });
                            } catch (e) {
                                console.error(e);
                            }
                        });
                    }
                });
            });
        }
        /**
         * 启动轮询
         * @private
         */
        function start() {
            if (polling) return;
            polling = true;

            function loop() {
                poll();
                loopTimer = setTimeout(loop, interval);
            }
            loop();
        }
        /**
         * 检查是否有节点存在，空则停止轮询
         * @private
         */
        function stopIfEmpty() {
            var hasAny = Object.keys(registry).some(function(type) {
                return registry[type].size > 0;
            });
            if (!hasAny) {
                clearTimeout(loopTimer);
                polling = false;
            }
        }
        /**
         * 监听指定元素的事件
         * @param {HTMLElement} el DOM 元素
         * @param {string} type 事件类型
         * @param {Function} handler 回调函数
         */
        function observe(el, type, handler) {
            if (!registry[type])
                throw new Error("Unsupported type: " + type);

            var map = registry[type];
            if (!map.has(el)) {
                map.set(el, {
                    snapshot: getSnapshot(el),
                    handlers: new Set()
                });
            }
            map.get(el).handlers.add(handler);
            start();
        }
        /**
         * 移除指定元素的事件监听
         * @param {HTMLElement} el DOM 元素
         * @param {string} type 事件类型
         * @param {Function} [handler] 回调函数，可选，未指定则移除所有
         */
        function remove(el, type, handler) {
            if (!registry[type]) return;
            var map = registry[type];
            if (!map.has(el)) return;

            if (handler) {
                map.get(el).handlers.delete(handler);
            } else {
                map.delete(el);
            }

            if (map.has(el) && map.get(el).handlers.size === 0) {
                map.delete(el);
            }
            stopIfEmpty();
        }
        /**
         * 移除元素的所有事件监听
         * @param {HTMLElement} el DOM 元素
         */
        function removeAll(el) {
            Object.keys(registry).forEach(function(type) {
                registry[type].delete(el);
            });
            stopIfEmpty();
        }

        /**
         * 清空所有监听
         */
        function clear() {
            Object.keys(registry).forEach(function(type) {
                registry[type].clear();
            });
            stopIfEmpty();
        }
        return {
            observe: observe,
            remove: remove,
            removeAll: removeAll,
            clear: clear
        };
    })();
    global.DomEvent.Monitor = Monitor;
})(window);
(function(global) {
    "use strict";
    if (!global.DomEvent) global.DomEvent = {};
    /**
     * DOM 事件工具方法
     * @namespace DomEvent.Utils
     */
    var Utils = {};
    var eventStore = new WeakMap();
    /**
     * 添加原生事件监听
     * @param {HTMLElement} el DOM 元素
     * @param {string} type 事件类型
     * @param {Function} handler 回调函数
     * @param {boolean} [capture=false] 是否捕获
     */
    Utils.add = function(el, type, handler, capture) {
        capture = !!capture;
        el.addEventListener(type, handler, capture);
        if (!eventStore.has(el)) eventStore.set(el, []);
        eventStore.get(el).push({
            type: type,
            handler: handler,
            capture: capture
        });
    };
    /**
     * 移除原生事件监听
     * @param {HTMLElement} el DOM 元素
     * @param {string} type 事件类型
     * @param {Function} handler 回调函数
     * @param {boolean} [capture=false] 是否捕获
     */
    Utils.remove = function(el, type, handler, capture) {
        capture = !!capture;
        el.removeEventListener(type, handler, capture);
        if (!eventStore.has(el)) return;

        var list = eventStore.get(el);
        eventStore.set(list.filter(function(item) {
            return !(item.type === type && item.handler === handler);
        }));
    };
    /**
     * 移除元素的所有事件监听
     * @param {HTMLElement} el DOM 元素
     */
    Utils.removeAll = function(el) {
        if (!eventStore.has(el)) return;
        var list = eventStore.get(el);
        list.forEach(function(item) {
            el.removeEventListener(item.type, item.handler, item.capture);
        });
        eventStore.delete(el);
    };
    /**
     * 清空所有事件监听
     */
    Utils.clearAll = function() {
        eventStore = new WeakMap();
    };
    /**
     * 节流函数
     * @param {Function} fn 原函数
     * @param {Number} delay 节流间隔 ms
     * @returns {Function} 包装后的函数
     */
    Utils.throttle = function(fn, delay) {
        var last = 0;
        var timer = null;
        return function() {
            var context = this;
            var args = arguments;
            var now = Date.now();
            if (now - last >= delay) {
                last = now;
                fn.apply(context, args);
            } else if (!timer) {
                timer = setTimeout(function() {
                    last = Date.now();
                    timer = null;
                    fn.apply(context, args);
                }, delay - (now - last));
            }
        };
    };
    /**
     * 防抖函数
     * @param {Function} fn 原函数
     * @param {Number} delay 防抖延迟 ms
     * @returns {Function} 包装后的函数
     */
    Utils.debounce = function(fn, delay) {
        var timer = null;
        return function() {
            var context = this;
            var args = arguments;
            clearTimeout(timer);
            timer = setTimeout(function() {
                fn.apply(context, args);
            }, delay);
        };
    };
    global.DomEvent.Utils = Utils;
})(window);