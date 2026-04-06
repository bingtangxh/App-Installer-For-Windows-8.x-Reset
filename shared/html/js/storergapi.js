/**
 * Microsoft Store - Generation Project (v1.2.3) [by @rgadguard & mkuba50]
 * Website: https://store.rg-adguard.net/
 * This API is not publicly available and is currently restricted by the website, meaning it's only useful after a webpage is opened. Therefore, this JS file is only for documenting its usage; it will remain unusable unless a specific method is used to address this issue.
 */
(function(global) {
    "use strict";

    function joinUrl(url, path) {
        if (url.charAt(url.length - 1) === "/") {
            url = url.slice(0, -1);
        }
        if (path.charAt(0) === "/") {
            path = path.slice(1);
        }
        return url + "/" + path;
    }

    function buildParams(params) {
        var queryString = "";
        var keys = Object.keys(params);
        for (var i = 0; i < keys.length; i++) {
            var key = keys[i];
            var value = params[key];
            if (value === null || value === void 0) {
                continue;
            }
            queryString += encodeURIComponent(key) + "=" + encodeURIComponent(value) + "&";
        }
        return queryString.slice(0, -1);
    }
    var baseUrl = "https://store.rg-adguard.net/";
    var getFilesApi = joinUrl(baseUrl, "api/GetFiles");
    var proxyPrefix = "https://api.allorigins.win/get?url=";
    var proxyApi = proxyPrefix + encodeURIComponent(getFilesApi);
    var reqMethod = "POST";

    function getXHR() {
        var xmlhttp;
        try {
            xmlhttp = new ActiveXObject("Msxml2.XMLHTTP");
        } catch (e) {
            try {
                xmlhttp = new ActiveXObject("Microsoft.XMLHTTP");
            } catch (E) {
                xmlhttp = false;
            }
        }
        if (!xmlhttp && typeof XMLHttpRequest != 'undefined') {
            xmlhttp = new XMLHttpRequest();
        }
        return xmlhttp;
    }
    /**
     * file structure: QueryFileItem 
     * @param {string} file 
     * @param {string} expire 
     * @param {string} sha1 
     * @param {string} size 
     * @param {string} url 
     */
    function QueryFileItem(file, expire, sha1, size, url) {
        /**
         * class name.
         * @type {string}
         */
        this.name = "QueryFileItem";
        this.file = file;
        this.expire = expire;
        this.sha1 = sha1;
        this.size = size;
        this.url = url;
    }
    /**
     * the type of value which will be send.
     * @enum {string}
     */
    var ValueType = {
        url: "url",
        productId: "ProductId",
        packageFamilyName: "PackageFamilyName",
        categoryId: "CategoryId"
    };
    /**
     * the channel of the store.
     * @enum {string}
     */
    var Channel = {
        fast: "WIF",
        slow: "WIS",
        releasePreview: "RP",
        retail: "Retail"
    };
    /**
     * fetch and get the response.
     * @param {string} value 
     * @param {StoreRG.ValueType} type 
     * @param {StoreRG.Channel} channel 
     * @param {[boolean]} isAsync default is true.
     * @returns {Promise <XMLHttpRequest>} the response.
     */
    function queryFiles(value, type, channel, isAsync) {
        if (typeof isAsync === "undefined" || isAsync === null) {
            isAsync = true;
        }
        var xhr = getXHR();
        xhr.open(reqMethod, getFilesApi, isAsync);
        xhr.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
        var params = "type={type}&url={url}&ring={ring}&lang={lang}"
            .replace("{type}", type)
            .replace("{url}", encodeURIComponent(value))
            .replace("{ring}", channel)
            .replace("{lang}", navigator.language);
        return new Promise(function(c, e) {
            try {
                xhr.onreadystatechange = function() {
                    if (xhr.readyState == 4) {
                        if (xhr.status == 200) {
                            if (c) c(xhr);
                        } else {
                            if (e) e(xhr.statusText + " (" + xhr.status + ")");
                        }
                    }
                };
                xhr.send(params);
            } catch (ex) {
                if (e) e(ex);
            }
        });
    }
    /**
     * fetch and get the response. And parse the response to get the QueryFileItem array.
     * @param {string} value 
     * @param {StoreRG.ValueType} type 
     * @param {StoreRG.Channel} channel 
     * @param {[boolean]} isAsync default is true.
     * @returns {Promise <{categoryId: string, datas: QueryFileItem[]}>} the QueryFileItem array.
     */
    queryFiles.parse = function(value, type, channel, isAsync) {
        return queryFiles(value, type, channel, isAsync).then(function(resp) {
            var tempcontainer = document.createElement("div");
            tempcontainer.innerHTML = resp.responseText;
            var styles = tempcontainer.querySelectorAll("style");
            var scripts = tempcontainer.querySelectorAll("script");
            for (var i = 0; i < styles.length; i++) {
                styles[i].parentNode.removeChild(styles[i]);
            }
            for (var i = 0; i < scripts.length; i++) {
                scripts[i].parentNode.removeChild(scripts[i]);
            }
            var categoryId = "";
            try {
                categoryId = tempcontainer.querySelector("i").textContent;
            } catch (e) {}
            var datas = [];
            var table = tempcontainer.querySelector("table");
            if (table) {
                var rows = table.querySelectorAll("tr");
                for (var i = 1; i < rows.length; i++) {
                    var cells = rows[i].querySelectorAll("td");
                    datas.push(new QueryFileItem(
                        cells[0].textContent,
                        cells[1].textContent,
                        cells[2].textContent,
                        cells[3].textContent,
                        cells[0].querySelector("a").href
                    ));
                }
            }
            return {
                categoryId: categoryId,
                datas: datas
            };
        });
    };

    function smartMatch(input) {
        if (!input) return null;
        var str = String(input);
        var reCategoryId = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i;
        var rePFN = /[A-Za-z0-9.-]{3,50}_[a-z0-9]{13}/i;
        var reProductId = /(^|[^a-z0-9])([a-z0-9]{12})(?=$|[^a-z0-9])/i;
        var m;
        m = str.match(reCategoryId);
        if (m) {
            return {
                type: ValueType.categoryId,
                match: m[0].toLowerCase()
            };
        }
        m = str.match(rePFN);
        if (m) {
            return {
                type: ValueType.packageFamilyName,
                match: m[0]
            };
        }
        m = str.match(reProductId);
        if (m) {
            return {
                type: ValueType.productId,
                match: m[2].toLowerCase()
            };
        }
        return {
            type: ValueType.url,
            match: str
        };
    }
    /**
     * 自动查询，无需设置其余参数。
     * @param {string} value 传入的查询值。
     * @returns {Promise <{categoryId: string, datas: QueryFileItem[]}>}
     */
    queryFiles.smartQuery = function(value) {
        var match = smartMatch(value);
        return queryFiles.parse(match.match, match.type, Channel.retail);
    };
    /**
     * @namespace StoreRG
     */
    global.StoreRG = {
        ValueType: ValueType,
        Channel: Channel,
        xhr: queryFiles,
    };
})(this);