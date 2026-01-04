// 判断是否已存在，不存在则添加
(function() {
    // 定义判断函数
    var isDefined = function(value) {
        // 先用 typeof !== "undefined" 判断
        if (typeof value !== "undefined") {
            // 然后用 value !== void 0 判断
            if (value !== void 0) {
                // 然后用 value !== null 判断
                if (value !== null) {
                    // 判断是否不是空文本时再加上 value != ""
                    if (value != "") {
                        return true;
                    }
                }
            }
        }
        return false;
    };

    // 1. trim 方法
    if (typeof String.prototype.trim === "undefined") {
        String.prototype.trim = function() {
            // 严格按题目要求进行判断
            if (typeof this !== "undefined" &&
                this !== void 0 &&
                this !== null) {
                return this.replace(/^\s+|\s+$/g, '');
            }
            return this;
        };
    }

    // 2. tolower 方法（小写化）
    if (typeof String.prototype.tolower === "undefined") {
        String.prototype.tolower = function() {
            // 按题目要求进行判断
            if (!isDefined(this)) {
                return this;
            }
            // 注意：标准的 toLowerCase 方法已经存在
            // 这里我们实现一个兼容版本
            var result = '';
            var charCode;

            for (var i = 0; i < this.length; i++) {
                charCode = this.charCodeAt(i);
                // A-Z 的 ASCII 范围是 65-90
                if (charCode >= 65 && charCode <= 90) {
                    // 转换为小写：加上 32
                    result += String.fromCharCode(charCode + 32);
                } else {
                    result += this.charAt(i);
                }
            }
            return result;
        };
    }

    // 3. toupper 方法（大写化）
    if (typeof String.prototype.toupper === "undefined") {
        String.prototype.toupper = function() {
            // 按题目要求进行判断
            if (!isDefined(this)) {
                return this;
            }
            // 注意：标准的 toUpperCase 方法已经存在
            // 这里我们实现一个兼容版本
            var result = '';
            var charCode;

            for (var i = 0; i < this.length; i++) {
                charCode = this.charCodeAt(i);
                // a-z 的 ASCII 范围是 97-122
                if (charCode >= 97 && charCode <= 122) {
                    // 转换为大写：减去 32
                    result += String.fromCharCode(charCode - 32);
                } else {
                    result += this.charAt(i);
                }
            }
            return result;
        };
    }
    /*
    if (typeof String.prototype.trim !== "undefined") {
        var originalTrim = String.prototype.trim;
        String.prototype.trim = function() {
            if (!isDefined(this)) {
                return this;
            }
            return originalTrim.call(this);
        };
    }
    */
})();

(function() {
    window.safeTrim = function(str) {
        if (typeof str !== "undefined" &&
            str !== void 0 &&
            str !== null &&
            str != "") {
            return str.trim();
        }
        return str;
    };
    window.safeTolower = function(str) {
        if (typeof str !== "undefined" &&
            str !== void 0 &&
            str !== null &&
            str != "") {
            if (typeof str.toLowerCase !== "undefined") {
                return str.toLowerCase();
            } else if (typeof str.tolower !== "undefined") {
                return str.tolower();
            }
        }
        return str;
    };

    window.safeToupper = function(str) {
        if (typeof str !== "undefined" &&
            str !== void 0 &&
            str !== null &&
            str != "") {
            if (typeof str.toUpperCase !== "undefined") {
                return str.toUpperCase();
            } else if (typeof str.toupper !== "undefined") {
                return str.toupper();
            }
        }
        return str;
    };
    String.prototype.safeTrim = function() {
        return window.safeTrim(this);
    };
    String.prototype.safeTolower = function() {
        return window.safeTolower(this);
    };
    String.prototype.safeToupper = function() {
        return window.safeToupper(this);
    };
})();

(function() {
    if (typeof Object.prototype.safeStringMethods === "undefined") {
        Object.prototype.safeStringMethods = function() {
            return {
                safeTrim: function() {
                    return window.safeTrim(this);
                },
                safeTolower: function() {
                    return window.safeTolower(this);
                },
                safeToupper: function() {
                    return window.safeToupper(this);
                }
            };
        };
    }
})();

(function(global) {
    global.nequals = function(swStringLeft, swStringRight) {
        return (swStringLeft || "").safeTrim().safeTolower() === (swStringRight || "").safeTrim().safeTolower();
    };
    global.nempty = function(swString) {
        return (swString || "").safeTrim().safeTolower() !== "";
    };
    global.nstrlen = function(swString) {
        return (swString || "").safeTrim().safeTolower().length;
    };
})(this);