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
(function(global) {
    global.StoreRG.test = function testDatas() {
        return Promise.as().then(function() {
            return {
                categoryId: "16db93bf-8748-449a-96ba-e9ed3a5f872d",
                datas: [{
                        "name": "QueryFileItem",
                        "file": "774d2daa-952f-459d-89ab-a8746741894e",
                        "expire": "2026-04-04 08:42:08 GMT",
                        "sha1": "6ba2df79d2e215e3bf01ae9616d93494e19fdeba",
                        "size": "5.88 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/774d2daa-952f-459d-89ab-a8746741894e?P1=1775292128&P2=404&P3=2&P4=BPjPYARH0%2f0sTmG%2f130POmUfgf8zcIE05IOyL1ngfCi0J4sVwC2FxOKYIiBcksKRRmrz9YA6y5ecewd3VGSCUA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.Media.PlayReadyClient.2_2.11.2154.0_arm__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "51dbe1e74ed28cff34402166c73be9552f531950",
                        "size": "1.98 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/e12f6b1b-a741-4582-b812-d515789cc8a2"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.Media.PlayReadyClient.2_2.11.2154.0_arm__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:40:47 GMT",
                        "sha1": "4e0dd2dcc3bfaae17e0068f0376e17ce525c2bab",
                        "size": "1.11 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/a7a74dab-0c39-4c3a-8381-3497596f57c2?P1=1775292047&P2=404&P3=2&P4=lzzyWa5aieJW9BmVTfmPODYD36N0N2nCqQ%2bDPbKcvDsMlss%2foXV0tbFNqm72KWU0yjBKT%2fZvqEKZf7gUZaTO6w%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.Media.PlayReadyClient.2_2.11.2154.0_x64__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "73e0bf631229b34375ee3db94c648276f8499739",
                        "size": "2.94 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/5b6da86b-03da-4b32-917a-9a3e324f63fe"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.Media.PlayReadyClient.2_2.11.2154.0_x64__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:40:47 GMT",
                        "sha1": "e26f17f70af01abbb298240e04cdc3603bc53d80",
                        "size": "1.83 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/37177ff7-8a67-4071-8ec5-05958e3e8c64?P1=1775292047&P2=404&P3=2&P4=PwT3ZduYsVmIoLb%2bg760gSTfTSIaz8PJUbGNykbO611yxEji3Dy5qzGSAzoPKcNZlrAD%2bhUgsaeJ4RhYXPfcyQ%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.Media.PlayReadyClient.2_2.11.2154.0_x86__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "f926341e51c54ef35436d9fce47dba45ea040384",
                        "size": "2.48 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/8e35a10e-0ab6-48b4-80b9-b75f5aada9b9"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.Media.PlayReadyClient.2_2.11.2154.0_x86__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:40:47 GMT",
                        "sha1": "889679b69512041da3f36ed0be802ecaa68ab9b0",
                        "size": "1.49 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/f8658bef-472a-4ff7-99ba-18b5a56d367e?P1=1775292047&P2=404&P3=2&P4=bOGGN6HY%2fjUsbpBX8BVmbjG6ue3hH%2fkkAVK%2fQM2%2fPscsHyLVrKnsF7y34yO6A4QtQjwPgjKSjl%2f2hA1e1KT7KA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.Media.PlayReadyClient_2.3.1678.0_arm__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "a51d7b16e8363df1a0d24979acff0a78f9509589",
                        "size": "1.76 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/9452a6f8-6ca3-4551-8b01-e602389960a9"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.Media.PlayReadyClient_2.3.1678.0_arm__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:40:27 GMT",
                        "sha1": "ffa4e060b44e0b2cd2510e80b9b097d5880fe303",
                        "size": "985.79 KB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/db99e500-4b9c-49f3-91c7-789baa840a07?P1=1775292027&P2=404&P3=2&P4=Bkl9BIkZX7LnK0%2fqptk%2fgvgZM0R7pqP9M0JnqStDKBOCSZgAtwaKx%2bNlKiLeW%2fwZx%2bXWVOYHLG0kNYx27rGYmQ%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.Media.PlayReadyClient_2.3.1678.0_x64__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "00b544660f77379782ed06058f307992a0f6ec45",
                        "size": "2.23 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/e8da56d0-f518-49e7-915e-0ef25d4e22fa"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.Media.PlayReadyClient_2.3.1678.0_x64__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:40:47 GMT",
                        "sha1": "95537bbd671d7236f1f4c60f3116910bbb94f4a9",
                        "size": "1.28 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/673862d5-4866-4107-ad09-e09a4627060f?P1=1775292047&P2=404&P3=2&P4=hyhhfxIskEweudGU085Wm4fY2rKPF%2bqiDfztzggrnjQy0IF%2fERfjoiwgnbRS7ZvwHmHYYcwX4oX%2fP%2b55YQzkig%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.Media.PlayReadyClient_2.3.1678.0_x86__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "a7560d7ad9a6d05993a795ace97d6294f701fb59",
                        "size": "1.99 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/dcf70773-7194-476b-8813-b28e168c6f7e"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.Media.PlayReadyClient_2.3.1678.0_x86__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:40:47 GMT",
                        "sha1": "10e4bffc9ebd15df5154791c8a6f81d9b01aa5fe",
                        "size": "1.11 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/ba06c0a4-a807-4b38-99ce-4eb19cdeb1e0?P1=1775292047&P2=404&P3=2&P4=guTP6CgtV4MqDLyc%2fRow3DwbwrTpapwCMO0VHsa6qCyxV0NlVr0EfRa3OsspMuCOuyAEy%2fGj9eKfnIGyTOpSeA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.NET.Native.Framework.2.2_2.2.29512.0_arm64__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "488ed95ce8834e3cb5aae2758168bb1d927e79b7",
                        "size": "7.03 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/38ddde04-2646-4c40-a8cc-ef73fa04d659"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.NET.Native.Framework.2.2_2.2.29512.0_arm64__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:41:47 GMT",
                        "sha1": "cc3b900e13f913b446500dcc109213e161b31f37",
                        "size": "4.93 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/e01cabbf-a9f5-41b0-b2c5-062f0b716b34?P1=1775292107&P2=404&P3=2&P4=U%2fIxWH1skxcYt8rtJMRMbkK7%2fJWlD4kMTAfH8rbGt94xnaBuOpASKaF4JEy2g4x0XiJya4oxdKskZg2%2b2V5OVA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.NET.Native.Framework.2.2_2.2.29512.0_arm__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "1668b177fa7afd2bebde408e1bc9154cfca38afc",
                        "size": "6.77 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/f7b26cb3-caf0-4cb8-bcfd-1367e187c439"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.NET.Native.Framework.2.2_2.2.29512.0_arm__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:41:47 GMT",
                        "sha1": "1e3cffd74dbe2a5fa14d35cd91216cac7e5f96d8",
                        "size": "4.74 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/dda33e1d-c984-4834-b6be-96919a27f1dc?P1=1775292107&P2=404&P3=2&P4=cHvdovov5iBHAUgZC8d0vRR3MZ0TryJUspW2FM1FM2IlGz8bRU9mpWkfzrBlhWqysyqI6zb0kdu2090H077Yuw%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.NET.Native.Framework.2.2_2.2.29512.0_x64__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "6fc4bd14dc3ccf3f0c89992f1b3d88405e9fba26",
                        "size": "7.06 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/36f54537-d528-42e3-9529-b1b9be734586"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.NET.Native.Framework.2.2_2.2.29512.0_x64__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:41:47 GMT",
                        "sha1": "612ad442b8740f4c57b8c84e6bf465ba4699118c",
                        "size": "4.96 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/4dd135ac-f0f3-4ae8-b910-14e9839b77cf?P1=1775292107&P2=404&P3=2&P4=gqc9EaEx1TrpbGBVy3N25MBwRc%2bPHYSJQFHIWzjXUvtq9hfp0eT6kPcva%2flC8vZzGrtu%2f4cPZYuQjmzzTbgKTw%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.NET.Native.Framework.2.2_2.2.29512.0_x86__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "65d92465309a144eb0b3c4b2729a803e70db3589",
                        "size": "6.38 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/5205d888-e86a-423d-9e16-0e268091f0e3"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.NET.Native.Framework.2.2_2.2.29512.0_x86__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:41:47 GMT",
                        "sha1": "af66e12c1bb9d8519da21259d0fcd88c247cb4f1",
                        "size": "4.43 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/76864380-9ee8-4f9b-907f-7ccce9c7ca57?P1=1775292107&P2=404&P3=2&P4=DvA%2fh%2fxRP0D50EZHYoYr8dQJH%2fFuEjFpg5KXjSefbatJD8MRi85Goi7Ym0j1u9Mppubza8HrbXHHmtU9ppyrJA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.NET.Native.Runtime.2.2_2.2.28604.0_arm64__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "4a8104411c1691ccfc902f857239e533bf4a52ce",
                        "size": "777 B",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/f96a2888-4103-46a1-9f3e-bb2c8ff84fc9"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.NET.Native.Runtime.2.2_2.2.28604.0_arm64__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:40:27 GMT",
                        "sha1": "43a7b55eb71df71966ef2fc35758eaa5914f95fd",
                        "size": "242.37 KB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/1b258fca-ed02-4324-bb41-0d0f51d1de11?P1=1775292027&P2=404&P3=2&P4=ITyKN8v4lRgKn22frvTbCRjMCAPqge2zyRJ2ihkl%2fRnVW%2bCeCZroFNr5DVZh4L1uonktJ5K50ueZ9A6%2fVhwKjA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.NET.Native.Runtime.2.2_2.2.28604.0_arm__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "9fa5ce0631e36078274495b122964c150a4d53ea",
                        "size": "737 B",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/856450b5-7b29-4cb3-8570-bb77fc2d5fb3"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.NET.Native.Runtime.2.2_2.2.28604.0_arm__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:40:27 GMT",
                        "sha1": "735760840e3b9d12382692a5d6529be9af2ae7cd",
                        "size": "210.25 KB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/44664ff1-3b9f-4984-83c4-6737a49f4034?P1=1775292027&P2=404&P3=2&P4=hHOLAh%2bVBT2FlkSqgJh%2bVMh55MeaTtOBt1%2b1e4FDNrRxZVmtQaIDMTUXx5YNgSNbfAN7mSkBeSZWzY2DMWsBRQ%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.NET.Native.Runtime.2.2_2.2.28604.0_x64__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "28d0660df3630dd323148a093f9947987b71bb2b",
                        "size": "783 B",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/eff5f698-03be-46ae-85d8-ebe1c27ae95e"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.NET.Native.Runtime.2.2_2.2.28604.0_x64__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:40:27 GMT",
                        "sha1": "e1a85885fd4453165061351651289cce8f8590c4",
                        "size": "238.8 KB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/6c5ccbb5-88d3-4e90-ac79-3880010d81af?P1=1775292027&P2=404&P3=2&P4=nqOkkFiksEsTXvlgUPq7oep5laFi7PlcW5HRbQLU2UWhkLjZHNNhteGXq%2bF0Md%2bbKuFKaevvEf8KOtqqNrTe0w%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.NET.Native.Runtime.2.2_2.2.28604.0_x86__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "837a6295b75e4abb79538dcae5e6aa5f3ffc4521",
                        "size": "731 B",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/c120d9fe-fb46-4dc3-ac29-e266027139a9"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.NET.Native.Runtime.2.2_2.2.28604.0_x86__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:40:27 GMT",
                        "sha1": "f3535a3b47819a04c6d5ee18905493be086e801e",
                        "size": "194.44 KB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/bc2b77dd-5801-4779-b34d-77119629866c?P1=1775292027&P2=404&P3=2&P4=I8yQ0Exb8vmaNtKU%2fr6qyByDSBwvu6QmAP14rN9YxAEyvVnQYNmjZinpkZh3DzCYcdiWJqmHJgY9XkF6T6Tq7g%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.UI.Xaml.2.4_2.42007.9001.0_arm64__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "560f6975418aff3e7f7ea0dad2acae3d5c21d8ea",
                        "size": "4.8 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/0de048b5-398d-4a5f-a2d9-b28412f30276"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.UI.Xaml.2.4_2.42007.9001.0_arm64__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:41:27 GMT",
                        "sha1": "04ce52ae7eeac046968189755cbf58673c6f2565",
                        "size": "3.24 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/8f3e2c7d-6353-4aba-9e61-7f33ca53b8d2?P1=1775292087&P2=404&P3=2&P4=NPm4Ggrw8n2%2b07AafOp7em4XepcAbAIo%2f%2b7%2fMeeukiTuNgbH3Z3BFtsfq4ed9su3%2fJnOCTYBHL4Lmu0HIc94AA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.UI.Xaml.2.4_2.42007.9001.0_arm__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "ed90a9b2873390302c9130cde8498bcede1ae1d7",
                        "size": "4.75 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/b9d15041-ff68-4a34-93a0-b6e4a21f4825"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.UI.Xaml.2.4_2.42007.9001.0_arm__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:41:27 GMT",
                        "sha1": "1c2b8bc55b36e025ce9727ebc876f0c1a054d1e5",
                        "size": "3.21 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/830d87f6-d977-477c-88e2-c4e5cb59e3de?P1=1775292087&P2=404&P3=2&P4=V%2bZyQLpLxnsWTUp4bwY5RmsP961Ie7bY5BpHyeX%2f%2fCSXn0h7mOSLm0P8WKXTmPFtaVarM1C4jHSO0cvm9ZMnZA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.UI.Xaml.2.4_2.42007.9001.0_x64__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "7d281c599e1d1e86baeda4dfcf1c1074f9745c31",
                        "size": "5.01 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/370d1cfc-98d3-4614-9f17-a618db996ac4"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.UI.Xaml.2.4_2.42007.9001.0_x64__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:41:27 GMT",
                        "sha1": "91d4534ed68121c4e212bfb4d22dcca863df420b",
                        "size": "3.38 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/b478ed53-87fb-4751-99cf-bb9ddb911a82?P1=1775292087&P2=404&P3=2&P4=FuylMCOppUY51uJFeHpLFNbnIpNoxxnJ8EKUwq8VLiaY1WIlqQ%2f0KN0DbQm%2bfusiM%2fU0bw1%2fScXRYqGmsnK%2bWg%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.UI.Xaml.2.4_2.42007.9001.0_x86__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "63f2222870ed418f42564bd1a99372796e9b5b28",
                        "size": "4.7 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/22ecf5bf-1576-427d-9f4e-ac1138524ea9"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.UI.Xaml.2.4_2.42007.9001.0_x86__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:41:27 GMT",
                        "sha1": "4a297c75cfdd97432c1c615ec1043ba5845ac704",
                        "size": "3.15 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/9e7e9486-b756-4d8c-a96c-810e32c91ce4?P1=1775292087&P2=404&P3=2&P4=WLol8dPid7aRt3roDWKW2m4PVUd97nXuFILrxJuiMhMuwMU1jhFCAiKJjbKivN9IsRI97t9hpIYl8j9cPwtWvQ%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.UI.Xaml.2.8_8.2501.31001.0_arm64__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "746a8ab3b93837d1d97669f39abf48d3271cfd62",
                        "size": "6.9 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/241ae7b3-2175-4276-9f4c-e61b72d9e0cf"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.UI.Xaml.2.8_8.2501.31001.0_arm64__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:41:47 GMT",
                        "sha1": "90964f7e5bba97e990fd7ec4cae27854cc7b3906",
                        "size": "4.82 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/bcedb4c1-c5ea-476c-b810-1ca6b97ceb31?P1=1775292107&P2=404&P3=2&P4=KBCb2Ias4g0qx7zS%2fo38pFFt%2bZEF5owJMbz50mmeuA4sRW6yYUlouiZHvQgE7Jc9qo0GmKzcZSw9M1Lz73SAaw%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.UI.Xaml.2.8_8.2501.31001.0_arm__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "e94cb978473b99f4c06ae4f249dcd4b613cb2c71",
                        "size": "6.84 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/1fb9d28e-9031-4880-8102-7c46cd747bbf"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.UI.Xaml.2.8_8.2501.31001.0_arm__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:41:47 GMT",
                        "sha1": "7ba0eef03042317683fc04c37120fa818ea30608",
                        "size": "4.76 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/adf6261a-4006-4d53-b856-92f8e70ba95f?P1=1775292107&P2=404&P3=2&P4=GsYLx4HvANlmUQMtUZMnVxT2dfVJlTQREiWnZb6YoAO8NUfn9HEaC0pT77nM8113T2p7EOcBkzo4elFdiDeR7Q%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.UI.Xaml.2.8_8.2501.31001.0_x64__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "a227f0162aae7bd982658cdddb4d24c2ba6d823d",
                        "size": "6.74 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/d8f791c3-4d30-4ee5-a8ce-2e22a94f6ba0"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.UI.Xaml.2.8_8.2501.31001.0_x64__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:41:47 GMT",
                        "sha1": "e6d716249e88d301a9719f5f2d52e19648f71738",
                        "size": "4.7 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/baf6388f-cf6b-4980-a220-0f1cc8c2f857?P1=1775292107&P2=404&P3=2&P4=Eg8qyELtKo%2fZOqm%2fMPQ2xskx6OyZ%2ftHakBJvoQMSuBFY3grfXxkg5R96AHxkacPpa9aRhMpGrHBaW7wpjG5tKQ%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.UI.Xaml.2.8_8.2501.31001.0_x86__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "df18112e7d0c93c252e77db462ca4893f43dd205",
                        "size": "6.36 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/22d00940-bba2-4122-8294-9917f6523f52"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.UI.Xaml.2.8_8.2501.31001.0_x86__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:41:47 GMT",
                        "sha1": "3af482b63af1a78cf47a1726ec86fa121f7a76d1",
                        "size": "4.41 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/d8e5a058-a962-49d1-a201-a691c531ba6d?P1=1775292107&P2=404&P3=2&P4=h96EqecnkuNEFnzvycCcXpLD9edrkXtHLv9cmgzSrPm09yNNnsdFZSzLn8NUapaYw2bkm3EAYGItZasGISPLaQ%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.110.00_11.0.51106.1_arm__8wekyb3d8bbwe.Appx",
                        "expire": "2026-04-04 08:40:27 GMT",
                        "sha1": "c6c7bb90583142cc2eda609b1ec61523c275a8b1",
                        "size": "794.88 KB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/eebfcb2d-669a-4a68-94e1-daa58ca8aee3?P1=1775292027&P2=404&P3=2&P4=aH749PWoNLw9FzjAUdcCNR4pSyf3c7upkEl3A1qF3Vlf0GvddHqkJiBXx72vFtIM%2b3hQsVGMyhWFBlGH%2f7GboA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.110.00_11.0.51106.1_arm__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "305972fc883d8755df8900a65c34b9f009613bf6",
                        "size": "1.5 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/4a8498c3-6308-451b-a541-7d869ef15e9e"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.110.00_11.0.51106.1_x64__8wekyb3d8bbwe.Appx",
                        "expire": "2026-04-04 08:40:27 GMT",
                        "sha1": "b14312c292c5e220b275c5319bf155b05fd2c90d",
                        "size": "939.22 KB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/b6066b71-dd8b-44bf-818f-c1c103a0b5c7?P1=1775292027&P2=404&P3=2&P4=mLAXB8xyh59HsPdzw%2fUH1Bi%2fujkKSWO3Ulx2Xm%2fOWLBxsp1QmI79Kj6eVkS%2fCqLkI77prD%2fCqyOQ6MVt1cINKw%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.110.00_11.0.51106.1_x64__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "96fdfd8fec79d9075bd61da973b6ab93ce42519c",
                        "size": "1.71 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/3699595d-9817-4246-b6b1-cb031a2ccfc7"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.110.00_11.0.51106.1_x86__8wekyb3d8bbwe.Appx",
                        "expire": "2026-04-04 08:40:27 GMT",
                        "sha1": "c524e7909383d5656ab8549212213fb78c6919f7",
                        "size": "925.05 KB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/09a07af9-f8cb-40ba-a645-9e3523375a3f?P1=1775292027&P2=404&P3=2&P4=dDN4dYBgsokxXGWu81S2JpThJ6Hk%2b2ygXBoJo4vLQApN6scSlz0LrVeBpRalvd9qkG60%2fTwguIagqpPUSvHD5A%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.110.00_11.0.51106.1_x86__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "1ba178c133f8c748c5347218804d60c7a8c9c01d",
                        "size": "1.67 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/1b05f933-7b88-4b36-bd52-42e0477c86b2"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.120.00_12.0.21005.1_arm__8wekyb3d8bbwe.Appx",
                        "expire": "2026-04-04 08:40:27 GMT",
                        "sha1": "f21aa6cc37f12a23562d64546f90d20b51141c80",
                        "size": "828.11 KB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/8a7579f5-35b5-4767-80d1-652c8d84a773?P1=1775292027&P2=404&P3=2&P4=Q1RXNdMSw9%2bJtO1EnVs56sDhW0MZX2Hy8adOLQuXOIQbVlQoyrSL5flLt8zcHhoNz9LphtlbYfkFsu3%2bjBAkLg%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.120.00_12.0.21005.1_arm__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "0000b15e836698615773194a112c3e21fce04735",
                        "size": "1.54 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/a81e4258-d20b-4349-9d60-4ec198de4f9e"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.120.00_12.0.21005.1_x64__8wekyb3d8bbwe.Appx",
                        "expire": "2026-04-04 08:40:27 GMT",
                        "sha1": "9671e25d9eadcbf900e3371d7f405261496f4d77",
                        "size": "952.53 KB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/07991083-08a6-4564-8aa5-76b242236669?P1=1775292027&P2=404&P3=2&P4=V0ugH1MIUeB5WLtAUr%2bLhURyyrbfAWXV1%2bhqkMo8OFosW2N1lLKH1Pu05VmTb0nG%2byE621l004hL8DjPjiy3oA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.120.00_12.0.21005.1_x64__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "ea8bd65a06c608b9d9d4fcadef79a03a88d06146",
                        "size": "1.72 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/01298121-e61c-452f-9076-86167057c075"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.120.00_12.0.21005.1_x86__8wekyb3d8bbwe.Appx",
                        "expire": "2026-04-04 08:40:27 GMT",
                        "sha1": "b4e27f92df68b8950a2863f987266b997b2018aa",
                        "size": "879.32 KB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/de500ead-add4-4212-aefe-3fb3b5209607?P1=1775292027&P2=404&P3=2&P4=OzC%2b7D8nYqYfPGOqS4rE8PbCcit5rtpGoUQOI6F3hH%2bDEdaZU5q791oPmUgA81qh0Hki4PRiOIpe61fy6DxlZA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.120.00_12.0.21005.1_x86__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "663fa8a112756b33d53985c4a78f09ab67d80105",
                        "size": "1.63 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/52595a79-6616-497e-ac1f-1eeb9e07a6bf"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.140.00_14.0.33519.0_arm64__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "3cbda789dee60a590124b1d6101f14119766e24f",
                        "size": "2.47 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/65e92492-cdf5-4c5f-a027-cfd9f24b6515"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.140.00_14.0.33519.0_arm64__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:40:47 GMT",
                        "sha1": "14db2b39eda03ed3f4f66540858faea7eb8ccf76",
                        "size": "1.5 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/4aa2f4c4-ecd2-41d1-8089-304a95524359?P1=1775292047&P2=404&P3=2&P4=RUU%2biDCKvavavQWtvIUrRVwwjl3vQonrKdBgaBs7MyiSs8uW7hCgns%2bPW28KfQ0QdoJiR5wBZ1hF3osxotCTiA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.140.00_14.0.33519.0_arm__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "799f7135ecbf7e8d722a77dd8db3d4115a67d096",
                        "size": "1.55 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/96aa6ac1-abe7-47d5-83a3-8186695bb581"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.140.00_14.0.33519.0_arm__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:40:27 GMT",
                        "sha1": "41cbe825eaf03d9a951e101eb7bd98519bd7fde2",
                        "size": "816.03 KB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/f52e53fc-9664-4720-800e-a637f6f5c125?P1=1775292027&P2=404&P3=2&P4=CO%2fsrs58YREXJ%2fsm20ag4DJgcdc2NmwqTLQzzl5VYeNQfhtcgj4TeeY2dMjooYbHWGQb%2bz%2fec2VJeGUw59x8ig%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.140.00_14.0.33519.0_x64__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "a9f44104d0b78a11cfdecbd17709800773e921ae",
                        "size": "1.63 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/604ca404-9d05-4057-86ad-8075511f9c2c"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.140.00_14.0.33519.0_x64__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:40:27 GMT",
                        "sha1": "00c5a18b3243c99296724d4c02975ba8fc3ff353",
                        "size": "875.57 KB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/c452d4ef-2486-4efe-9c99-36b3d23e0160?P1=1775292027&P2=404&P3=2&P4=MR6yhnGin3ohk7vQyQLZhxEsv8Skk2HuYmuI2WZRSKbLR7Wkc6950OvmXY76hCzVtr4DHmoACYIivw3IB37ZKQ%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.140.00_14.0.33519.0_x86__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "477c0a243daa358c6d76ec39d04cdcc6fe81960e",
                        "size": "1.46 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/662cba60-b2ad-4d28-be91-0193121b138a"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.VCLibs.140.00_14.0.33519.0_x86__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:40:27 GMT",
                        "sha1": "fc358891923a5c9c31398fecfc600ecb1b992014",
                        "size": "740.77 KB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/9683459a-02fa-4bd6-9ae6-af8ddfbeef35?P1=1775292027&P2=404&P3=2&P4=NBc3R6dcKUboX2%2fKXuWmaJZaWhopnHqd9qZPvNX6GpG2uPW9GVmh%2fpstgI7IQ35Yr80GEPjHt5njmVlm6K0CMQ%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.WinJS.1.0_1.0.9200.20789_neutral__8wekyb3d8bbwe.Appx",
                        "expire": "2026-04-04 08:40:27 GMT",
                        "sha1": "2a1ef5a2ffe06d3f20215c81bd996ac301b10e9d",
                        "size": "849.4 KB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/6fcff48f-b3d8-4122-a374-74905fb2a71d?P1=1775292027&P2=404&P3=2&P4=Pky%2foxSZjpTV4srACAHkPZm3xCzxbVOiCAYE4BqW9mndjIvEiFkp5HBRbwD1JisWRrw5UfWzrJ2A7kA0d6tgbQ%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.WinJS.1.0_1.0.9200.20789_neutral__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "dafab0ea6c9bd29682bd13d10b1bc5c4fca3b4e2",
                        "size": "1.59 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/c5dbe091-845d-47bc-8f69-57d85d61ded3"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.WinJS.2.0_1.0.9600.17018_neutral__8wekyb3d8bbwe.Appx",
                        "expire": "2026-04-04 08:40:47 GMT",
                        "sha1": "95f691e79446c9e6e67c41b2bb12a572b95c257c",
                        "size": "1.04 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/d0e6de1f-ed6c-498b-b247-40c708b101a4?P1=1775292047&P2=404&P3=2&P4=j9pg1CqR64y5QWdSgPNW8Uk2lhrLV6yGR5SFRbjaCkdiU2jUjetdI%2f94cLT6bw7IFWEDnBunRw2nNApySdpT9g%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.WinJS.2.0_1.0.9600.17018_neutral__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "d243ea20125363297c8091edffaa79577ad03f47",
                        "size": "1.9 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/3e3821e0-29df-47e6-ab56-8b2fca7dc78a"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_1.5.216.0_arm__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "3e2b6c1643afc5b6cb3bb759c7d1f75265174d07",
                        "size": "15.96 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/35ef37b2-2bc0-4adc-842a-de7f3cdeacd4"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_1.5.216.0_arm__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:44:08 GMT",
                        "sha1": "730e6ddc4d554f06563f576262f5f0ca29143cf0",
                        "size": "11.84 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/411b0b33-9905-464e-92ec-147643508b30?P1=1775292248&P2=404&P3=2&P4=M3pm%2bZvSUnP32MG4vEssK0OwWlIZMwnEVKTR6gqf6vERu1pSUQUwO1%2baCc%2fX%2bqovzIbkv0u710fnd3fKd5EPLg%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_1.5.216.0_x64__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "bb0fc74c51048e3492775520912fb59d621d662a",
                        "size": "16.37 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/c4fecd7c-1844-42de-89f4-0a484d43215a"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_1.5.216.0_x64__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:44:28 GMT",
                        "sha1": "a5a3519df9ad9790efdef420f6522f6aded0c95c",
                        "size": "12.18 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/e8db06a7-fdad-468b-8cb9-b4004b4c068f?P1=1775292268&P2=404&P3=2&P4=HRo4LALabaXiF65T78tp8HshckwMdVlSBx2OrBYgTaU7UKSw08X9U%2fZuAI%2b3B%2fA8AhPdiFUMEvxKzfVmucD8mw%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_1.5.216.0_x86__8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "498ef52f81a20b0e1495aa7661bc6f0fc6da53d3",
                        "size": "15.3 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/4e574d6c-ab1c-4f2b-8e73-be4bf4c07a40"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_1.5.216.0_x86__8wekyb3d8bbwe.appx",
                        "expire": "2026-04-04 08:44:08 GMT",
                        "sha1": "54bf2c61cdb5634599d2182752298337a12bc3f9",
                        "size": "11.34 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/9b995f37-3171-4ae4-9e96-a35afab27cb1?P1=1775292248&P2=404&P3=2&P4=ORvNIS9Wylg%2fkFimX7auu5NXOMSHRTWm0qP7DaWx4E4TnobQKW7cyZcucgKtN2FORy1LdYlLt%2be1qYJINYnVrw%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2507.6.0_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "1ffb3495d4eea3e30aa3e927fe13779738f3664c",
                        "size": "48.89 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/feb96ba6-6545-4ab9-97c0-e132d01a237d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2507.6.0_neutral_~_8wekyb3d8bbwe.msixbundle",
                        "expire": "2026-04-04 08:52:47 GMT",
                        "sha1": "941effaf1ba39cac47979782172916680405141f",
                        "size": "37.79 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/d3748dfe-4f9c-42a5-83cd-01743f81a244?P1=1775292767&P2=404&P3=2&P4=NmRoTS0JarKtNHcgnRgUGhodFwfZGYcKazJlUun03Aj8YD0KjdOzPv4B%2b3dmG1gRYsaEM5YiLtpGJYwGaR3VUQ%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2507.6.70_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "02d5534ff20113aa77d0a6ba4007844f188e6a6e",
                        "size": "132.14 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/a61fbfc9-d0f8-45fd-9f30-5ccdb78f52f2"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2507.6.70_neutral_~_8wekyb3d8bbwe.emsixbundle",
                        "expire": "2026-04-04 09:15:07 GMT",
                        "sha1": "ab017d9e8b1721771a49563f5614409708ffa102",
                        "size": "104.82 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/ebbaf2a3-3955-4168-b992-175487fbbc11?P1=1775294107&P2=404&P3=2&P4=SuItnOfXsOdxYL7gztuspgZRRD5Lf6EsNA4lbjLjLYA1Hrbs1W1SRgVMrRaDjeaT1lrxY841UY5QzohqAQux1A%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2508.31.0_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "6c72be63c6146262a9b82a35b40c6460a0a6507f",
                        "size": "48.87 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/02c0d533-002a-41a5-873b-ec0b728aa88a"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2508.31.0_neutral_~_8wekyb3d8bbwe.msixbundle",
                        "expire": "2026-04-04 08:52:48 GMT",
                        "sha1": "fc8f2edc05e46861681499b66e8d72d7cc8e73b0",
                        "size": "37.79 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/030e8760-e40b-4cbe-8efd-8c7cd139a26f?P1=1775292768&P2=404&P3=2&P4=EPpx6P82mQhXdk6sGb0sXyf%2bD8K05b4zlEmkR1iweVx%2frokMBPdyWIgvgxNjpOdqCYAc2ZDTBABBSrZ82VLHOA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2508.31.70_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "f5c900cac143ed89989f8db184063d53e0086aa3",
                        "size": "132.55 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/a495c831-61f1-4362-ba90-7e32d5b676cd"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2508.31.70_neutral_~_8wekyb3d8bbwe.emsixbundle",
                        "expire": "2026-04-04 09:15:07 GMT",
                        "sha1": "a990c4087bfc49c83456d35fa311e3ce948eb9d8",
                        "size": "104.82 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/9c19ad04-6bc5-404d-86ed-5d80bb788cdd?P1=1775294107&P2=404&P3=2&P4=i0xyVl0b3VNXSKm1Lod5jbREPdxY4keA5HrZTEk7y16WPZAGMm6czyiFskLBypBDefwhmIwEw5TKz4YeiDwCWg%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2509.7.0_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "8f4a5b86abb9ca3101f3be5d340245c4a48d4128",
                        "size": "48.81 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/aa1137c2-7b20-4a70-a7a3-62354b6166b9"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2509.7.0_neutral_~_8wekyb3d8bbwe.msixbundle",
                        "expire": "2026-04-04 08:52:47 GMT",
                        "sha1": "4ae4274a5cab63655abfb20e847a60ca3d95c36d",
                        "size": "37.79 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/5ccb382c-ef0e-452d-ae45-82f47468ab13?P1=1775292767&P2=404&P3=2&P4=XsyI0R2TkC%2bU5po9aXN1%2bBjgRb%2fJtXxBr5kQEL%2fgzDbAeHfDl1m%2fmLxG0XqR05w7Lz%2fSGomgrO1FHbls8LEgxg%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2509.7.70_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "a8de8edbb9da305a27e31b505861030d8ad352db",
                        "size": "132.63 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/c9a92949-8914-4281-b477-eea65e9fddda"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2509.7.70_neutral_~_8wekyb3d8bbwe.emsixbundle",
                        "expire": "2026-04-04 09:15:08 GMT",
                        "sha1": "946131668073102cbeaa5b760864e19c87ad297a",
                        "size": "104.83 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/df82a331-ee22-4102-b20d-6c2adecf4527?P1=1775294108&P2=404&P3=2&P4=X2DCHBlals1PJwcU0HAhu3n6NCd98zXBZJ4GT1sTbSHZXxo7Adi8KevrhDtSZLLjwZJSpfZflRoCX6Dvdvkg%2bg%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2510.7.0_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "c91bd70b444d054b02cbb296b8ab17dfc2b97e5f",
                        "size": "48.77 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/6189ce16-9fe9-4ba1-8058-e6903bfa75c2"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2510.7.0_neutral_~_8wekyb3d8bbwe.msixbundle",
                        "expire": "2026-04-04 08:52:48 GMT",
                        "sha1": "29d26e85995daba48251d45ffeeb99e96c04b955",
                        "size": "37.79 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/3c7edaa1-b324-4d7f-99ee-7215c6e30415?P1=1775292768&P2=404&P3=2&P4=cGy5w6BO6E%2fMCxoBWfGdqS7qVgj%2fdtxUxgaiJGTOVSAHxUQXQkzJsRdeuaS8405y3gYJTJGIASA2j1NBxHguag%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2510.7.70_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "c30602fd0330893b8a7dc83e207de5f6004fc066",
                        "size": "132.58 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/248e3c6b-0856-412e-ae39-d79942c37062"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2510.7.70_neutral_~_8wekyb3d8bbwe.emsixbundle",
                        "expire": "2026-04-04 09:15:08 GMT",
                        "sha1": "6a951d7e10a6574dc1a6c9a4d75933f249078c16",
                        "size": "104.82 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/d6bc14a8-5130-4502-8aaa-91f6e72a1b2c?P1=1775294108&P2=404&P3=2&P4=PTtmeajbwsOIRBLUm2jKXjSeilGmEHNTeAW%2f6K3RE%2bxla861AY8CrTSUT2tERYF7batQzjpbGC%2faghC%2b2sKWgA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2511.5.0_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "ecaea8b0873e67a75f6d23b06c07d6b72a8a6cc9",
                        "size": "48.94 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/2908e2dc-5992-4e33-a417-e8f5e6de4c68"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2511.5.0_neutral_~_8wekyb3d8bbwe.msixbundle",
                        "expire": "2026-04-04 08:52:48 GMT",
                        "sha1": "866dfa8c126175004ca607cfb5e53aa2d6525b33",
                        "size": "37.79 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/7de9503a-a638-4dc2-bc02-7e286238ca8c?P1=1775292768&P2=404&P3=2&P4=ArHYaGdnVcJZ%2bsvZ2GFBcQQqQWLAGjwhcxool%2bOObhq2kUjFVXjajS2mh77%2fFnhcoynHBjX9XP%2fo1%2fT2iUywnA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2511.5.70_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "f6c164e3b5c8c5351da9b8a9e332a9e5b36a3a58",
                        "size": "132.29 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/eb2d51a0-72d5-41c8-80e0-956d84f511f5"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2511.5.70_neutral_~_8wekyb3d8bbwe.emsixbundle",
                        "expire": "2026-04-04 09:15:08 GMT",
                        "sha1": "70fb6b8412e5f0a59922b79b49a7ec8630b67207",
                        "size": "104.83 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/e3bcd1a6-23da-4f6b-b60c-091278ff068f?P1=1775294108&P2=404&P3=2&P4=m54vZHyhDput%2b%2b0YG4YA%2bFISZ6YKQSHtou1PbyZbbEnRi4GL%2fCPaxzBA%2fOfqnX9rrk7nd3cfpho49euqOPsegw%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2512.10.0_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "0374980e10008d07eea268ddf97eff1fabfd3c86",
                        "size": "48.52 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/95085448-9690-4d80-9fe7-892526a3312a"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2512.10.0_neutral_~_8wekyb3d8bbwe.msixbundle",
                        "expire": "2026-04-04 08:52:48 GMT",
                        "sha1": "c6cdfa255a93cc10201933c9b9a5987d6be4745c",
                        "size": "37.58 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/976eceaa-eb21-4188-8664-cef8d93c12ab?P1=1775292768&P2=404&P3=2&P4=kARZln9JbN3LfDEssf7IkFDmxCKpH1DmwEVNwY8GWsnxQy9MtN%2bRqqHZgN%2b%2fC2Y4Xxnqo0Ui2Lsr1MEyOzuLzQ%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2512.10.70_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "288af982289f4c82954c3b1a9ae470b5b25d3913",
                        "size": "131.88 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/0a13e900-890c-4b28-9409-16dcebb5d475"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2512.10.70_neutral_~_8wekyb3d8bbwe.emsixbundle",
                        "expire": "2026-04-04 09:15:07 GMT",
                        "sha1": "9222a3ae85bc44ac534ed39168037aff60b3fa9d",
                        "size": "104.38 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/aff39e54-8df6-4012-ad37-06910157a4d0?P1=1775294107&P2=404&P3=2&P4=mZKn%2f5By1bUmuZ%2bJATwUFlnq79pV0FWfSrdszFhlpqabuSxT57Fk3y6mvUIRYrIDTd5abG5c6cB3N8vJMg2pEQ%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2601.11.0_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "4ee5cc09918dd91fc654a02c9d7a1a1696bb4368",
                        "size": "48.72 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/400a03e5-e26e-40dc-8378-e16fbf1e6b61"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2601.11.0_neutral_~_8wekyb3d8bbwe.msixbundle",
                        "expire": "2026-04-04 08:52:48 GMT",
                        "sha1": "e2eb028a6ed4e739021a22cd819fff402062837f",
                        "size": "37.66 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/ac2fd71a-3844-4d84-b7eb-9e252e40d668?P1=1775292768&P2=404&P3=2&P4=T9UioKI4U%2fB%2fNG3zm1guKKNQqt%2buteGzWRx6cxap81JLU%2f4I9DQFH4kGKT%2fAyIRvunYhGFd1K8tVRm6Aok%2bSFA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2601.11.70_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "0f6b2cc00ee6f78ac22d0651d3ead1c5f4abbb78",
                        "size": "132.28 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/30d5f061-7960-4621-8538-33835557f222"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_11.2601.11.70_neutral_~_8wekyb3d8bbwe.emsixbundle",
                        "expire": "2026-04-04 09:15:07 GMT",
                        "sha1": "4dac6c602ceb46225da363b736745755737cf13e",
                        "size": "104.74 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/02494e92-9955-4fe5-909a-21c120dead45?P1=1775294107&P2=404&P3=2&P4=QYwg6lXQszTJfc%2bONyLUQhhWLUm0TDtFo6NXo7b1RVGxwvAJSCbyTiZ922v%2fWq87eB2DgW%2biar8T8xMYPdg%2f0A%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2.6.678.0_arm__8wekyb3d8bbwe.xap",
                        "expire": "2026-04-04 08:44:48 GMT",
                        "sha1": "1e01611c4cbbcbd1c841d1bd369edb5d5bbdc032",
                        "size": "13.69 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/f0d10b72-b8eb-4cf6-9233-16124cfefbce?P1=1775292288&P2=404&P3=2&P4=gkAsaJ6EGCYvZM8QP2i%2bgEaNkv%2fxDSOsx%2f3QzTYdT3e8jm%2fHdd5M33uFUVysz7pyjolEAZ3mGgQhvk0iXDXamQ%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2015.310.1544.2913_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "1227f9fc6e6a7da254de0cbc8f793ba6cfe10f1c",
                        "size": "57.74 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/2990602d-c6d9-4c9e-8249-3fbd91b00829"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2015.310.1544.2913_neutral_~_8wekyb3d8bbwe.appxbundle",
                        "expire": "2026-04-04 08:55:08 GMT",
                        "sha1": "7c662e33f76930620c7aa9b89bf032bcd07d6e42",
                        "size": "44.85 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/e2c652c8-369d-46a6-b792-4e9c9fdb0351?P1=1775292908&P2=404&P3=2&P4=XHb0LIvDVxpVmAcoHrM1cedpHyXR%2f5mD9Wa43snqDLdrq8nlEGbKA9ACzj7omQahfJcJZVx0L%2fS3cH75JaWQbQ%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.16102.10341.1000_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "52cfe7d8cdbb5dbf1bf7c1dbeb1107f72092355c",
                        "size": "42.2 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/3f881352-1324-47d1-b738-83a2028f4e92"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.16102.10341.1000_neutral_~_8wekyb3d8bbwe.appxbundle",
                        "expire": "2026-04-04 08:51:08 GMT",
                        "sha1": "7c13e15eb8c8a54e6af92142aaf91534661a5dd8",
                        "size": "32.55 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/e078d4fc-4cc8-4683-81eb-120c075692b3?P1=1775292668&P2=404&P3=2&P4=FF7RqwI4%2fqTL1dsUjD%2bO6vEpD1NQq9zqRLZNp6yYILwnXlRg94ZDUmjhFrBJvepX5eRO5%2fBtRdDhJH9P77rVgA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.16102.10342.1000_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "a8b306791e8a6c23b9413b32edd84ccadd954ce3",
                        "size": "24.4 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/b79d9934-3cae-4c2c-810f-80ee5ba84b2e"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.16102.10342.1000_neutral_~_8wekyb3d8bbwe.appxbundle",
                        "expire": "2026-04-04 08:46:28 GMT",
                        "sha1": "64aad044bb70eb15f30bb7f693cf9c9cf48aabc0",
                        "size": "18.49 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/a990c508-7f7e-4c64-aa96-830373acb993?P1=1775292388&P2=404&P3=2&P4=Xq68hcS5fpnmngTZF94K9EViHS%2fhEWGmOMisIbzOcZyEB5M%2fqYhlAHU%2fydoIS1UHlxzGUbekCpWL7%2f730QQxVw%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.16102.10343.1000_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "9cd04af59db1bcd3a3e150bd92ec401be14b0015",
                        "size": "21.75 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/4110f01a-dd14-4c86-ae2e-766ad462d8a1"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.16102.10343.1000_neutral_~_8wekyb3d8bbwe.appxbundle",
                        "expire": "2026-04-04 08:45:48 GMT",
                        "sha1": "48d46fa66c8fa0f032a9f1f58a4d2dee0cfbea24",
                        "size": "16.37 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/9368b938-3401-4636-a2d0-7f6613692379?P1=1775292348&P2=404&P3=2&P4=LQRdZLaz9pIP91AVcSnnL1nFSCkRItUbKdnU6WVgpzK%2fzs3Sxt%2fY0m1CgSUTNWkr2AqQxRGq5KMrmTEnyfYmiQ%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.17012.10313.1000_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "f9e8e752e8bce1fd6f5f5ee14eff5ce950a38dce",
                        "size": "21.58 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/fda13c69-ffb7-4da1-ab53-9ab3cb90d080"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.17012.10313.1000_neutral_~_8wekyb3d8bbwe.appxbundle",
                        "expire": "2026-04-04 08:45:48 GMT",
                        "sha1": "3b08259184fc9a3f8bf9a9f66f185857fa462b12",
                        "size": "16.26 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/a8855a79-726a-410b-9175-a234ed9ff6f0?P1=1775292348&P2=404&P3=2&P4=Z6xf%2f49mfzV%2fBFv1NjjZuIWtTrfiWcklGoeu0LsIw4Qp%2f2tcQ%2bcmKYVsp2hZop8qoYY%2bwVsrSxmHU%2fVhlDQzNA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.17054.15413.1000_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "1c634b4806937db520dd7e6e41c2b8fdf13e1ce6",
                        "size": "36.43 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/bd225f17-fac9-4468-ac48-182247f8f895"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.17054.15413.1000_neutral_~_8wekyb3d8bbwe.appxbundle",
                        "expire": "2026-04-04 08:49:28 GMT",
                        "sha1": "ddaee8c30c9e9cfafded10920c3d8ec0a4f971f1",
                        "size": "27.95 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/1ffed486-dce2-4c1e-b7d3-e25a62f769b4?P1=1775292568&P2=404&P3=2&P4=mC9yif1hgM660TxsnSpgI4%2fbxoBq4U28Y2nuxX7JvdLJsdc2poOKWwz6qSL6V28IulUBufQpCKoZsxDE32F9hA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.18052.11111.0_neutral_~_8wekyb3d8bbwe.AppxBundle",
                        "expire": "2026-04-04 08:56:28 GMT",
                        "sha1": "63cf8b9dbcd2d8bf0c97fa24894977212663ac22",
                        "size": "48.48 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/fc7d8228-9828-4828-bcce-7c0c7bdbbcc3?P1=1775292988&P2=404&P3=2&P4=Ph8EvSPIevqdoHs%2f0x8gx6o4msxuPwTrmWE7l7KvQUqVLtFOtQYFAi0lrXUS4Nfpa8hLP%2f4um%2fKnqsWXLDQYVg%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.18052.11111.0_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "4aeba269d0590bebaab22a42144000149d28c214",
                        "size": "62.3 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/2802d82e-0306-46a8-8d5c-997bae9b0b74"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.18052.11112.0_neutral_~_8wekyb3d8bbwe.AppxBundle",
                        "expire": "2026-04-04 08:46:08 GMT",
                        "sha1": "08003adfdc0b2783b40289a5406555fb8c214b49",
                        "size": "17.86 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/b14d8222-b625-4d57-9d7b-27c065899f1f?P1=1775292368&P2=404&P3=2&P4=cL%2fx6bbaNM%2b7L59y9oI5nBS5MxAyxA3tUkkIJ0kWct4dQaB80zNGY17IY8UljlBPufvTI6PiPlGuj6NsN7WCPQ%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.18052.11112.0_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "b6661885aad876b73636fd12be90673d11f21ae8",
                        "size": "23.61 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/d920f606-9305-4f6f-b4ef-799491019751"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.18052.11114.70_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "fe414eac57c2e2cb25ae228a81f43e2f31deaef3",
                        "size": "71.45 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/b8330c18-d4e0-48dc-a2b2-ca3f0eea4494"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.18052.11114.70_neutral_~_8wekyb3d8bbwe.eappxbundle",
                        "expire": "2026-04-04 08:58:48 GMT",
                        "sha1": "b1d8d9c5b09a90d16e7270e1fb4aabfd36cf669c",
                        "size": "55.86 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/3cd6cb85-efed-4da3-a6da-9f51bdb581c8?P1=1775293128&P2=404&P3=2&P4=FO%2fvFhMvX9o%2fq3MsJg1dqLKagV5Z3CAJhddi8vi5XXRHrVy3F5POrlvVVSJidy0phucqKuls1ypNfN7p2TGB9Q%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.18052.11115.0_neutral_~_8wekyb3d8bbwe.AppxBundle",
                        "expire": "2026-04-04 08:45:28 GMT",
                        "sha1": "97acdab90874d17e2dfc9eecfb738a064dffd85a",
                        "size": "15.46 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/b7e78fc9-55a4-49d2-a157-1b6dd12bffa7?P1=1775292328&P2=404&P3=2&P4=XjmzXOdmyMdkDiM7Wx1daTxCuLiATCUph6QXYFMJOhKHziIAabKGHuA7SeiQNTCiRa398J4HB2G0dfvCz14%2fHA%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.18052.11115.0_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "71d28fb81a01a62bab0c615eb5d067b82a56f3d4",
                        "size": "20.57 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/770aa6b5-3285-4e12-92b2-b1818f342572"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.20112.10111.0_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "8daab109e25d49b6d0d182992d5df1586fa9c300",
                        "size": "52.02 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/ff95969d-b4e7-49dc-933e-3522ac6286aa"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.20112.10111.0_neutral_~_8wekyb3d8bbwe.appxbundle",
                        "expire": "2026-04-04 08:53:48 GMT",
                        "sha1": "932da6926e5c1f99608e0aabbee9d87f474764d5",
                        "size": "40.32 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/c92cf6fc-faca-4aff-94b9-a032ddb4541b?P1=1775292828&P2=404&P3=2&P4=Dkq3TX5YNFsEFKteSXgAvq9XRbL4oDmsXZ1Imco4O%2bd5%2fh5YHb0WvoFkQrVxv3RBG2L929NpEOrxAUIziLRyeg%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.20112.10114.70_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "09d75a107793cd2fe0d62933446314655371894c",
                        "size": "57.47 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/bc5bc471-31c9-4677-aba3-79f19379180e"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.20112.10114.70_neutral_~_8wekyb3d8bbwe.eappxbundle",
                        "expire": "2026-04-04 08:55:08 GMT",
                        "sha1": "d5d7acce3e9946ddee40997eba67601081d8a180",
                        "size": "44.65 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/750d2a59-9878-48e6-a752-dd5e1442a221?P1=1775292908&P2=404&P3=2&P4=Srt9lfNu%2fGX8nYohUf9wNWIzICQZ5mSW1y%2fnT9zbzLqjVjsNt4mV1SqYlt48MVuVCUltlyX0pWapjLvMdHy%2bfg%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.20112.10115.0_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "141c6a80953b55390c19958b47a065d0f8a0964f",
                        "size": "34.09 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/3b75aac5-aa12-4455-befe-f9ec4d338297"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.20112.10115.0_neutral_~_8wekyb3d8bbwe.appxbundle",
                        "expire": "2026-04-04 08:49:08 GMT",
                        "sha1": "c7557d71eadce6ad1a550c45504c84bcf39e751f",
                        "size": "26.13 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/ca597a98-c138-497d-9e94-1779a19d2b7e?P1=1775292548&P2=404&P3=2&P4=B6PBSMRpk8n3sTbSCt2zcWfjxyyalIkSjV0UbFwi8EX2uTGO0zML%2fmLzSVodVgZyRqHQ%2fievsL7VNVqR4hZSMg%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.20112.10116.0_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "5c0f782d9dca547ed8799b67af3e9a8315523306",
                        "size": "52.05 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/71dfb5e8-89d8-4061-9253-f2c36d147cdd"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.20112.10116.0_neutral_~_8wekyb3d8bbwe.appxbundle",
                        "expire": "2026-04-04 08:53:48 GMT",
                        "sha1": "8195a5e424a6f00f32553cb95024e5ef0ab32a41",
                        "size": "40.32 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/2a835131-727e-4307-84b7-3462a32daae4?P1=1775292828&P2=404&P3=2&P4=mdQxgBPka%2bx0ezOOKWtDVfm6BO%2bU9lrOkYaptGrNJC7DE1sQOXXFfN4BeTs1KrHRr%2fIFeURMr%2fGTOjqdCvRXqg%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.22031.10091.0_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "56782aa0bfcf152496b02526888aaabd70ac5235",
                        "size": "51.02 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/897e954b-c0d0-4e9a-8573-9798a68c7a85"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.22031.10091.0_neutral_~_8wekyb3d8bbwe.appxbundle",
                        "expire": "2026-04-04 08:53:28 GMT",
                        "sha1": "380c3a9531ef647fcde8ad03bda88c577f7d74c7",
                        "size": "39.53 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/ee9dceb0-ccca-4ed1-bc8a-8c616128a039?P1=1775292808&P2=404&P3=2&P4=Oz3wQcedKojXjraUYqiMO18RM0TqjtsCQZL9%2fW%2bI3gBcPOixa2hSxr3UFkESn0a3vuC28FO8Y9jkLAnqvs9xrw%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.22031.10094.70_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "45f39f0811deccc50a10a429bd31795c3faeeae4",
                        "size": "54.69 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/bb52c32c-b363-46c0-89cb-77d6b62cf852"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.22031.10094.70_neutral_~_8wekyb3d8bbwe.eappxbundle",
                        "expire": "2026-04-04 08:54:28 GMT",
                        "sha1": "7d07f52a085d682bf74fec0ecb083e91278b3273",
                        "size": "42.44 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/98653c05-9e5e-447b-831d-830739b2010f?P1=1775292868&P2=404&P3=2&P4=j2TLfFxeBmPHoYhirwmed8u6BPyFkjNXJo253itrK3ANfSf8Qp7MZEgR0wKRpp%2fHB%2fgcItq6JfQcDO%2frRb1L5Q%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.22031.10095.0_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "109143102babf0547a916915b00053536e04669d",
                        "size": "35.19 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/83772988-b836-43a0-9cab-7715863c1d9c"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.22031.10095.0_neutral_~_8wekyb3d8bbwe.appxbundle",
                        "expire": "2026-04-04 08:49:08 GMT",
                        "sha1": "6a5ecdd2c222fc9fe8d12481b733fbe4336d2986",
                        "size": "26.99 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/c65cb3ed-4e54-4891-bd85-5b25d90170eb?P1=1775292548&P2=404&P3=2&P4=cenRA1o%2bWqBVqmRcHcU1R3u0r%2f38ObcINoBVZh5%2bHiSgUls%2fPi7vTu3aFk%2fCzg%2bRjm1kKOVsEUyZGLDeLb2s2A%3d%3d"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.22031.10096.0_neutral_~_8wekyb3d8bbwe.BlockMap",
                        "expire": "1970-01-01 00:00:00 GMT",
                        "sha1": "c42b8b3baa69191f69c3176243009a4078a6f642",
                        "size": "51 KB",
                        "url": "http://dl.delivery.mp.microsoft.com/filestreamingservice/files/006ae870-0264-4979-84d9-b7138a30b811"
                    },
                    {
                        "name": "QueryFileItem",
                        "file": "Microsoft.ZuneMusic_2019.22031.10096.0_neutral_~_8wekyb3d8bbwe.appxbundle",
                        "expire": "2026-04-04 08:53:28 GMT",
                        "sha1": "75fe6c6129159e2884f9407334e9b2e0cd7078db",
                        "size": "39.53 MB",
                        "url": "http://tlu.dl.delivery.mp.microsoft.com/filestreamingservice/files/5b0a77d2-283a-4df6-88f4-7d7a2c4fd6d0?P1=1775292808&P2=404&P3=2&P4=U8Y1mWxtr%2f%2ftL4Ywql5%2fbmj14myJEs6vvatcEUnZ8MsXNh09uOA30uuIpFfw01AwtRqMifqJuh4s73LjJUogoA%3d%3d"
                    }
                ]
            }
        });
    }
})(this);