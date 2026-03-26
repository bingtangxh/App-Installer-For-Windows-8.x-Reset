/// <reference path="//Microsoft.WinJS.2.0/js/base.js" />
(function(global) {
    "use strict";
    /**
     * PromisePolyfill 构造函数。
     * 
     * 模拟 WinJS.Promise 的构造形式：
     * new PromisePolyfill(init, oncancel)
     * 
     * @constructor
     * @param {function(function(any):void,function(any):void,function(any):void):void} pfInit
     *        Promise 初始化函数。
     *        该函数在 Promise 创建时立即执行，并接收三个回调：
     *        - complete(value)  : 完成 Promise
     *        - error(reason)    : 使 Promise 失败
     *        - progress(value)  : 发送进度通知（当前实现为占位）
     * 
     * @param {function():void} [pfOnCancel]
     *        Promise 取消回调。当调用 promise.cancel() 时执行。
     * 
     * @example
     * var p = new PromisePolyfill(
     *     function (complete, error, progress) {
     *         setTimeout(function () {
     *             complete("done");
     *         }, 1000);
     *     },
     *     function () {
     *         console.log("Promise canceled");
     *     }
     * );
     */
    function PromisePolyfill(pfInit, pfOnCancel) {
        /// <param name="pfInit" type="Function">
        /// Promise 初始化函数。
        /// 形参签名：
        /// function(
        ///     complete : function(any):void,
        ///     error    : function(any):void,
        ///     progress : function(any):void
        /// )
        /// </param>
        /// <param name="pfOnCancel" type="Function" optional="true">
        /// Promise 取消回调函数。当 promise.cancel() 被调用时执行。
        /// </param>
        var swState = "pending"; // "fulfilled" | "rejected"
        var vValue = undefined;
        var aHandlers = [];

        function invokeHandlers() {
            if (swState === "pending") return;
            for (var i = 0; i < aHandlers.length; i++) {
                handle(aHandlers[i]);
            }
            aHandlers = [];
        }

        function handle(hHandler) {
            if (swState === "pending") {
                aHandlers.push(hHandler);
                return;
            }

            var pfCallback = swState === "fulfilled" ?
                hHandler.onFulfilled :
                hHandler.onRejected;

            if (!pfCallback) {
                if (swState === "fulfilled") {
                    hHandler.resolve(vValue);
                } else {
                    hHandler.reject(vValue);
                }
                return;
            }

            try {
                var vResult = pfCallback(vValue);
                hHandler.resolve(vResult);
            } catch (ex) {
                hHandler.reject(ex);
            }
        }

        function resolve(vResult) {
            try {

                if (vResult === self)
                    throw new TypeError("A promise cannot be resolved with itself.");

                if (vResult && (typeof vResult === "object" || typeof vResult === "function")) {
                    var pfThen = vResult.then;
                    if (typeof pfThen === "function") {
                        pfThen.call(vResult, resolve, reject);
                        return;
                    }
                }

                swState = "fulfilled";
                vValue = vResult;
                invokeHandlers();

            } catch (ex) {
                reject(ex);
            }
        }

        function reject(vReason) {
            swState = "rejected";
            vValue = vReason;

            if (typeof PromisePolyfill.onerror === "function") {
                PromisePolyfill.onerror(vReason);
            }

            invokeHandlers();
        }

        // WinJS Promise progress（当前仅占位）
        function progress(vProgress) {
            // 当前 polyfill 未实现 progress 传播
        }

        var self = this;

        try {
            if (typeof pfInit === "function") {
                pfInit(resolve, reject, progress);
            }
        } catch (ex) {
            reject(ex);
        }

        this.then = function(pfOnFulfilled, pfOnRejected) {

            return new PromisePolyfill(function(resolve, reject) {

                handle({
                    onFulfilled: pfOnFulfilled,
                    onRejected: pfOnRejected,
                    resolve: resolve,
                    reject: reject
                });

            });

        };

        this["catch"] = function(pfOnRejected) {
            return this.then(null, pfOnRejected);
        };

        this.done = function(pfOnFulfilled, pfOnRejected) {

            this.then(pfOnFulfilled, pfOnRejected)["catch"](function(ex) {

                setTimeout(function() {
                    throw ex;
                }, 0);

            });

        };

        this.cancel = function() {

            if (pfOnCancel) {
                try {
                    pfOnCancel();
                } catch (ex) {}
            }

            reject(new Error("Promise was canceled"));

        };

        this._oncancel = pfOnCancel;
        this._state = swState;
        this._value = vValue;
    }
    /**
     * 检查对象是否为 PromisePolyfill 实例
     * @param {any} vObj 待检查对象
     * @returns {boolean} 是否为 PromisePolyfill 实例
     */
    PromisePolyfill.is = function(vObj) {
        return vObj instanceof PromisePolyfill;
    };
    /**
     * 创建一个已完成的 PromisePolyfill
     * @param {any} vValue 要返回的值
     * @returns {PromisePolyfill} 已完成的 PromisePolyfill
     */
    PromisePolyfill.resolve = function(vValue) {
        return new PromisePolyfill(function(resolve) { resolve(vValue); });
    };
    /**
     * 创建一个已拒绝的 PromisePolyfill
     * @param {any} vReason 拒绝原因
     * @returns {PromisePolyfill} 已拒绝的 PromisePolyfill
     */
    PromisePolyfill.reject = function(vReason) {
        return new PromisePolyfill(function(resolve, reject) { reject(vReason); });
    };
    /**
     * 等待所有 Promise 完成
     * @param {Array<PromisePolyfill|any>} aPromises 待处理的 Promise 或普通值数组
     * @returns {PromisePolyfill<Array<any>>} 返回包含所有结果的 Promise
     */
    PromisePolyfill.all = function(aPromises) {
        return new PromisePolyfill(function(resolve, reject) {
            var nRemaining = aPromises.length;
            var aResults = new Array(nRemaining);
            if (nRemaining === 0) resolve([]);

            function resolver(iIndex) {
                return function(vValue) {
                    aResults[iIndex] = vValue;
                    nRemaining--;
                    if (nRemaining === 0) resolve(aResults);
                };
            }
            for (var i = 0; i < aPromises.length; i++) {
                PromisePolyfill.resolve(aPromises[i]).then(resolver(i), reject);
            }
        });
    };
    /**
     * 竞速 Promise，谁先完成就返回谁的结果
     * @param {Array<PromisePolyfill|any>} aPromises 待处理的 Promise 或普通值数组
     * @returns {PromisePolyfill<any>} 最先完成的 Promise 的值
     */
    PromisePolyfill.race = function(aPromises) {
        return new PromisePolyfill(function(resolve, reject) {
            for (var i = 0; i < aPromises.length; i++) {
                PromisePolyfill.resolve(aPromises[i]).then(resolve, reject);
            }
        });
    };
    /**
     * Promise join，同 all
     * @param {Array<PromisePolyfill|any>} aPromises 待处理的 Promise 或普通值数组
     * @returns {PromisePolyfill<Array<any>>} 返回包含所有结果的 Promise
     */
    PromisePolyfill.join = function(aPromises) {
        return PromisePolyfill.all(aPromises);
    };
    /**
     * 任意 Promise 完成即返回
     * @param {Array<PromisePolyfill|any>} aPromises 待处理的 Promise 或普通值数组
     * @returns {PromisePolyfill<any>} 最先完成的 Promise 的值，若都失败则 reject 一个错误数组
     */
    PromisePolyfill.any = function(aPromises) {
        return new PromisePolyfill(function(resolve, reject) {
            var nRemaining = aPromises.length;
            var aErrors = new Array(nRemaining);
            if (nRemaining === 0) reject(new Error("No promises provided."));

            function resolver(vValue) { resolve(vValue); }

            function rejecter(iIndex) {
                return function(ex) {
                    aErrors[iIndex] = ex;
                    nRemaining--;
                    if (nRemaining === 0) reject(aErrors);
                };
            }
            for (var i = 0; i < aPromises.length; i++) {
                PromisePolyfill.resolve(aPromises[i]).then(resolver, rejecter(i));
            }
        });
    };
    /**
     * 给 Promise 添加超时处理
     * @param {PromisePolyfill|any} pPromise 要处理的 Promise
     * @param {number} nMilliseconds 超时时间（毫秒）
     * @returns {PromisePolyfill<any>} 超时或原 Promise 完成后 resolve/reject
     */
    PromisePolyfill.timeout = function(pPromise, nMilliseconds) {
        return new PromisePolyfill(function(resolve, reject) {
            var hTimer = setTimeout(function() {
                reject(new Error("Promise timed out after " + nMilliseconds + "ms"));
            }, nMilliseconds);
            PromisePolyfill.resolve(pPromise).then(function(vValue) {
                clearTimeout(hTimer);
                resolve(vValue);
            }, function(ex) {
                clearTimeout(hTimer);
                reject(ex);
            });
        });
    };
    PromisePolyfill.as = function(vValue) {
        return PromisePolyfill.resolve(vValue);
    };
    PromisePolyfill.wrap = function(vValue) {
        return PromisePolyfill.resolve(vValue);
    };
    PromisePolyfill.wrapError = function(vError) {
        return PromisePolyfill.reject(vError);
    };
    /**
     * 将数组的每个值依次执行回调
     * @param {Array<any>} aValues 数组
     * @param {function(any, number): any | PromisePolyfill<any>} pfCallback 回调函数
     * @returns {PromisePolyfill<Array<any>>} 所有回调完成的结果数组
     */
    PromisePolyfill.thenEach = function(aValues, pfCallback) {
        var aPromises = [];
        for (var i = 0; i < aValues.length; i++) {
            aPromises.push(PromisePolyfill.resolve(aValues[i]).then(pfCallback));
        }
        return PromisePolyfill.all(aPromises);
    };
    var hListeners = {};
    /**
     * 全局事件注册
     * @param {string} sType 事件类型
     * @param {function(any):void} pfHandler 回调函数
     */
    PromisePolyfill.addEventListener = function(sType, pfHandler) {
        if (!hListeners[sType]) hListeners[sType] = [];
        hListeners[sType].push(pfHandler);
    };
    /**
     * 全局事件移除
     * @param {string} sType 事件类型
     * @param {function(any):void} pfHandler 回调函数
     */
    PromisePolyfill.removeEventListener = function(sType, pfHandler) {
        if (!hListeners[sType]) return;
        var aList = hListeners[sType];
        for (var i = 0; i < aList.length; i++) {
            if (aList[i] === pfHandler) {
                aList.splice(i, 1);
                break;
            }
        }
    };
    /**
     * 全局事件派发
     * @param {string} sType 事件类型
     * @param {any} vDetail 事件详情
     */
    PromisePolyfill.dispatchEvent = function(sType, vDetail) {
        if (!hListeners[sType]) return;
        var aList = hListeners[sType].slice();
        for (var i = 0; i < aList.length; i++) {
            try { aList[i](vDetail); } catch (ex) {}
        }
    };
    PromisePolyfill.supportedForProcessing = true;
    PromisePolyfill.onerror = null;
    /**
     * 创建一个在指定毫秒数后完成的 Promise。
     *
     * @param {number} nMilliseconds
     *        延迟时间（毫秒）。
     *
     * @returns {PromisePolyfill}
     *          返回 Promise，在延迟结束后完成。
     *
     * @example
     * WinJS.Promise.delay(500).then(function () {
     *     console.log("500ms elapsed");
     * });
     */
    PromisePolyfill.delay = function(nMilliseconds) {
        /// <param name="nMilliseconds" type="Number">
        /// 延迟时间（毫秒）。
        /// </param>
        /// <returns type="PromisePolyfill"/>
        var hTimer = null;
        return new PromisePolyfill(
            function(complete, error, progress) {
                hTimer = setTimeout(function() {
                    complete();
                }, nMilliseconds);
            },
            function() {
                if (hTimer !== null) {
                    clearTimeout(hTimer);
                    hTimer = null;
                }
            }
        );
    };
    /**
     * 创建一个循环执行的 Promise，类似 setInterval。
     *
     * 该 Promise 不会自动完成，除非：
     * 1. 调用 promise.cancel()
     * 2. callback 抛出异常
     *
     * @param {function(): (any|PromisePolyfill|WinJS.Promise)} pfCallback
     *        每次循环执行的回调函数。可以返回 Promise。
     *
     * @param {number} nDelay
     *        每次执行之间的间隔时间（毫秒）。
     *
     * @returns {PromisePolyfill}
     *          返回 Promise 对象，可通过 cancel() 停止循环。
     *
     * @example
     * var p = WinJS.Promise.interval(function () {
     *     console.log("tick");
     * }, 1000);
     *
     * setTimeout(function () {
     *     p.cancel();
     * }, 5000);
     */
    PromisePolyfill.interval = function(pfCallback, nDelay) {

        /// <param name="pfCallback" type="Function">
        /// 每次间隔执行的函数。可以返回 Promise。
        /// </param>
        /// <param name="nDelay" type="Number">
        /// 执行间隔（毫秒）。
        /// </param>
        /// <returns type="PromisePolyfill"/>

        var bCanceled = false;

        return new PromisePolyfill(

            function(complete, error, progress) {

                function loop() {

                    if (bCanceled) {
                        complete();
                        return;
                    }

                    try {

                        var vResult = pfCallback();

                        if (vResult && typeof vResult.then === "function") {

                            vResult.then(waitNext, error);

                        } else {

                            waitNext();

                        }

                    } catch (ex) {

                        error(ex);

                    }

                }

                function waitNext() {

                    if (bCanceled) {
                        complete();
                        return;
                    }

                    setTimeout(loop, nDelay);

                }

                loop();

            },

            function() {

                bCanceled = true;

            }

        );

    };
    if (typeof global.Promise !== "undefined") {
        global.Promise.delay = PromisePolyfill.delay;
        global.Promise.interval = PromisePolyfill.interval;
    }
    if (typeof global.WinJS !== "undefined" && typeof global.WinJS.Promise !== "undefined") {
        global.WinJS.Promise.delay = PromisePolyfill.delay;
        global.WinJS.Promise.interval = PromisePolyfill.interval;
    }
    if (typeof global.Promise !== "undefined") {
        var p = global.Promise;
        if (!p.join) p.join = p.all;
        if (!p.any) p.any = PromisePolyfill.any;
        if (!p.timeout) p.timeout = PromisePolyfill.timeout;
        if (!p.as) p.as = p.resolve;
        if (!p.wrap) p.wrap = p.resolve;
        if (!p.wrapError) p.wrapError = p.reject;
        if (!p.thenEach) p.thenEach = PromisePolyfill.thenEach;
        if (!p.is) p.is = function(vObj) { return vObj instanceof p; };
        if (!p.supportedForProcessing) p.supportedForProcessing = true;
        if (!p.addEventListener) p.addEventListener = PromisePolyfill.addEventListener;
        if (!p.removeEventListener) p.removeEventListener = PromisePolyfill.removeEventListener;
        if (!p.dispatchEvent) p.dispatchEvent = PromisePolyfill.dispatchEvent;
        if (!p.onerror) p.onerror = null;
    }
    if (typeof global.WinJS !== "undefined" && typeof global.WinJS.Promise !== "undefined") {
        var wp = global.WinJS.Promise;
        if (!wp.resolve) wp.resolve = function(vValue) { return new wp(function(c) { c(vValue); }); };
        if (!wp.reject) wp.reject = function(vReason) { return new wp(function(c, e) { e(vReason); }); };
        if (!wp.all) wp.all = function(aPromises) { return wp.join(aPromises); };
        if (!wp.race) wp.race = PromisePolyfill.race;
        global.Promise = wp;
        if (typeof global.Promise === "undefined") global.Promise = wp;
    }
    if (typeof global.Promise === "undefined" && typeof global.WinJS === "undefined") {
        global.Promise = PromisePolyfill;
    }
})(this);