(function(global) {
    function _createImage(src, onload, onerror) {
        var img = new Image();

        img.onload = function() {
            onload(img);
        };

        img.onerror = function() {
            onerror && onerror();
        };

        img.src = src;
    }

    function getSolidOpaqueBackgroundColor(source, callback) {

        function processImage(img) {
            if (!img || !img.complete) {
                callback(null);
                return;
            }

            var canvas = document.createElement("canvas");
            var ctx = canvas.getContext("2d");

            canvas.width = img.naturalWidth || img.width;
            canvas.height = img.naturalHeight || img.height;

            ctx.drawImage(img, 0, 0);

            try {
                var imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
            } catch (e) {
                // 跨域导致的安全异常
                callback(null);
                return;
            }

            var data = imageData.data;
            var w = canvas.width;
            var h = canvas.height;

            var colors = {};
            var total = 0;

            function pushColor(r, g, b, a) {
                if (a !== 255) return;
                var key = r + "," + g + "," + b;
                colors[key] = (colors[key] || 0) + 1;
                total++;
            }

            // top + bottom
            for (var x = 0; x < w; x++) {
                var topIndex = (0 * w + x) * 4;
                var botIndex = ((h - 1) * w + x) * 4;
                pushColor(data[topIndex], data[topIndex + 1], data[topIndex + 2], data[topIndex + 3]);
                pushColor(data[botIndex], data[botIndex + 1], data[botIndex + 2], data[botIndex + 3]);
            }

            // left + right
            for (var y = 1; y < h - 1; y++) {
                var leftIndex = (y * w + 0) * 4;
                var rightIndex = (y * w + (w - 1)) * 4;
                pushColor(data[leftIndex], data[leftIndex + 1], data[leftIndex + 2], data[leftIndex + 3]);
                pushColor(data[rightIndex], data[rightIndex + 1], data[rightIndex + 2], data[rightIndex + 3]);
            }

            if (total === 0) {
                callback(null);
                return;
            }

            var bestKey = null;
            var bestCount = 0;

            for (var key in colors) {
                if (colors.hasOwnProperty(key)) {
                    if (colors[key] > bestCount) {
                        bestCount = colors[key];
                        bestKey = key;
                    }
                }
            }

            // 95% 纯色阈值
            if (bestCount / total < 0.95) {
                callback(null);
                return;
            }

            callback(bestKey);
        }

        // 如果传入的是 img 元素
        if (source && source.tagName && source.tagName.toLowerCase() === "img") {
            processImage(source);
            return;
        }

        // 如果传入的是 data url 或普通 url
        if (typeof source === "string") {
            _createImage(source, processImage, function() {
                callback(null);
            });
            return;
        }

        callback(null);
    }

    function getHamonyColor(source, callback) {

        function _createImage(src, onload, onerror) {
            var img = new Image();
            img.onload = function() { onload(img); };
            img.onerror = function() { onerror && onerror(); };
            img.src = src;
        }

        function _toKey(r, g, b) {
            return r + "," + g + "," + b;
        }

        function _rgbToHsl(r, g, b) {
            r /= 255;
            g /= 255;
            b /= 255;
            var max = Math.max(r, g, b);
            var min = Math.min(r, g, b);
            var h, s, l = (max + min) / 2;

            if (max === min) {
                h = s = 0;
            } else {
                var d = max - min;
                s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
                switch (max) {
                    case r:
                        h = (g - b) / d + (g < b ? 6 : 0);
                        break;
                    case g:
                        h = (b - r) / d + 2;
                        break;
                    case b:
                        h = (r - g) / d + 4;
                        break;
                }
                h /= 6;
            }
            return { h: h, s: s, l: l };
        }

        function _hslToRgb(h, s, l) {
            var r, g, b;

            function hue2rgb(p, q, t) {
                if (t < 0) t += 1;
                if (t > 1) t -= 1;
                if (t < 1 / 6) return p + (q - p) * 6 * t;
                if (t < 1 / 2) return q;
                if (t < 2 / 3) return p + (q - p) * (2 / 3 - t) * 6;
                return p;
            }

            if (s === 0) {
                r = g = b = l;
            } else {
                var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                var p = 2 * l - q;
                r = hue2rgb(p, q, h + 1 / 3);
                g = hue2rgb(p, q, h);
                b = hue2rgb(p, q, h - 1 / 3);
            }

            return {
                r: Math.round(r * 255),
                g: Math.round(g * 255),
                b: Math.round(b * 255)
            };
        }

        function _lum(r, g, b) {
            function f(x) {
                x = x / 255;
                return x <= 0.03928 ? x / 12.92 : Math.pow((x + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * f(r) + 0.7152 * f(g) + 0.0722 * f(b);
        }

        function _contrast(a, b) {
            var L1 = _lum(a.r, a.g, a.b);
            var L2 = _lum(b.r, b.g, b.b);
            var lighter = Math.max(L1, L2);
            var darker = Math.min(L1, L2);
            return (lighter + 0.05) / (darker + 0.05);
        }

        function _tryPureBackground(data, w, h) {
            var edgeColors = {};
            var edgeTotal = 0;

            function push(r, g, b, a) {
                if (a !== 255) return;
                var k = _toKey(r, g, b);
                edgeColors[k] = (edgeColors[k] || 0) + 1;
                edgeTotal++;
            }

            for (var x = 0; x < w; x++) {
                var top = (0 * w + x) * 4;
                var bot = ((h - 1) * w + x) * 4;
                push(data[top], data[top + 1], data[top + 2], data[top + 3]);
                push(data[bot], data[bot + 1], data[bot + 2], data[bot + 3]);
            }
            for (var y = 1; y < h - 1; y++) {
                var left = (y * w + 0) * 4;
                var right = (y * w + (w - 1)) * 4;
                push(data[left], data[left + 1], data[left + 2], data[left + 3]);
                push(data[right], data[right + 1], data[right + 2], data[right + 3]);
            }

            if (edgeTotal === 0) return null;

            var best = null,
                bestCount = 0;
            for (var k in edgeColors) {
                if (edgeColors.hasOwnProperty(k) && edgeColors[k] > bestCount) {
                    bestCount = edgeColors[k];
                    best = k;
                }
            }
            if (best && bestCount / edgeTotal >= 0.95) return best;
            return null;
        }

        function _process(img) {
            if (!img || !img.complete) { callback(null); return; }

            var canvas = document.createElement("canvas");
            var ctx = canvas.getContext("2d");
            canvas.width = img.naturalWidth || img.width;
            canvas.height = img.naturalHeight || img.height;
            ctx.drawImage(img, 0, 0);

            var imageData;
            try {
                imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
            } catch (e) {
                callback(null);
                return;
            }

            var data = imageData.data;
            var w = canvas.width,
                h = canvas.height;

            // 1) 尝试纯色背景
            var pure = _tryPureBackground(data, w, h);
            if (pure) { callback(pure); return; }

            // 2) 统计不透明像素（抽样）
            var sumR = 0,
                sumG = 0,
                sumB = 0,
                count = 0;
            var samples = 0;
            var step = 4; // 4x抽样，减少性能消耗
            for (var y = 0; y < h; y += step) {
                for (var x = 0; x < w; x += step) {
                    var i = (y * w + x) * 4;
                    var a = data[i + 3];
                    if (a === 255) {
                        sumR += data[i];
                        sumG += data[i + 1];
                        sumB += data[i + 2];
                        count++;
                    }
                    samples++;
                }
            }
            if (count === 0) { callback(null); return; }

            var avgR = sumR / count,
                avgG = sumG / count,
                avgB = sumB / count;

            // 3) 生成候选色（借鉴流行配色）
            var base = _rgbToHsl(avgR, avgG, avgB);

            function clamp(v, min, max) { return Math.max(min, Math.min(max, v)); }

            var candidates = [];

            // 中性色（低饱和）
            candidates.push(_hslToRgb(base.h, 0.05, 0.5));
            candidates.push(_hslToRgb(base.h, 0.1, 0.6));
            candidates.push(_hslToRgb(base.h, 0.1, 0.4));

            // 平均色去饱和
            candidates.push(_hslToRgb(base.h, clamp(base.s * 0.4, 0.05, 0.2), clamp(base.l, 0.2, 0.8)));

            // 互补色（活泼）
            candidates.push(_hslToRgb((base.h + 0.5) % 1, clamp(base.s * 0.6, 0.1, 0.8), clamp(base.l, 0.35, 0.7)));

            // 类似色
            candidates.push(_hslToRgb((base.h + 0.083) % 1, clamp(base.s * 0.5, 0.1, 0.8), clamp(base.l, 0.35, 0.7)));
            candidates.push(_hslToRgb((base.h - 0.083 + 1) % 1, clamp(base.s * 0.5, 0.1, 0.8), clamp(base.l, 0.35, 0.7)));

            // 三分色
            candidates.push(_hslToRgb((base.h + 0.333) % 1, clamp(base.s * 0.6, 0.1, 0.8), clamp(base.l, 0.35, 0.7)));
            candidates.push(_hslToRgb((base.h - 0.333 + 1) % 1, clamp(base.s * 0.6, 0.1, 0.8), clamp(base.l, 0.35, 0.7)));

            // 4) 计算最小对比度（与所有不透明像素）
            function minContrastWithImage(bg) {
                var bgObj = { r: bg.r, g: bg.g, b: bg.b };
                var minC = Infinity;

                for (var y = 0; y < h; y += step) {
                    for (var x = 0; x < w; x += step) {
                        var i = (y * w + x) * 4;
                        if (data[i + 3] !== 255) continue;
                        var px = { r: data[i], g: data[i + 1], b: data[i + 2] };
                        var c = _contrast(bgObj, px);
                        if (c < minC) minC = c;
                    }
                }
                return minC;
            }

            var best = null;
            for (var i = 0; i < candidates.length; i++) {
                var c = candidates[i];
                var minC = minContrastWithImage(c);
                if (minC >= 4.5) {
                    best = c;
                    break;
                }
            }

            if (best) {
                callback(_toKey(best.r, best.g, best.b));
            } else {
                callback(null);
            }
        }

        if (source && source.tagName && source.tagName.toLowerCase() === "img") {
            _process(source);
        } else if (typeof source === "string") {
            _createImage(source, _process, function() { callback(null); });
        } else {
            callback(null);
        }
    }

    function getSuitableBackgroundColor(source, callback) {
        getSolidOpaqueBackgroundColor(source, function(color) {
            if (color) {
                callback(color);
            } else {
                getHamonyColor(source, callback);
            }
        });
    }

    function createLocalizedCompare(locale) {
        return function(a, b) {
            a = a || "";
            b = b || "";

            return a.localeCompare(b, locale, {
                numeric: true, // 2 < 10
                sensitivity: "base" // 不区分大小写 / 重音
            });
        };
    }
    var pagemgr = new PageManager();
    OnLoad.add(function() {
        var listContainer = document.getElementById("applist");
        var appItemTemplate = document.getElementById("appitem-template");
        var mgr = Package.manager;
        var nstr = Bridge.NString;
        var datasrc = new DataView.DataSource();
        var themeColor = Bridge.UI.themeColor;
        var loadingDisplay = document.getElementById("applist-loading");
        var loadingStatus = loadingDisplay.querySelector(".title");
        var listView = new DataView.ListView(listContainer, function(item) {
            var appItem = appItemTemplate.cloneNode(true);
            appItem.id = "";
            appItem.style.display = "";
            var logoimg = appItem.querySelector("img");
            logoimg.src = item.Properties.LogoBase64 || logoimg.src;
            logoimg.parentElement.style.backgroundColor = themeColor;
            var appName = appItem.querySelector(".displayName");
            appName.textContent = item.Properties.DisplayName || item.Identity.Name;
            var appPub = appItem.querySelector(".publisher");
            appPub.textContent = item.Properties.Publisher;
            appItem.data = item;
            appItem.setAttribute("data-install-location", item.InstallLocation);
            appItem.setAttribute("data-development-mode", item.DevelopmentMode);
            appItem.setAttribute("data-is-bundle", item.IsBundle);
            appItem.setAttribute("data-is-framework", item.Properties.Framework);
            appItem.setAttribute("data-family-name", item.Identity.FamilyName);
            appItem.setAttribute("data-full-name", item.Identity.FullName);
            appItem.setAttribute("data-version", item.Identity.Version.Expression);
            appItem.setAttribute("data-users", item.Users);
            appItem.setAttribute("data-publisher-id", item.Identity.PublisherId);
            setTimeout(function(a, b) {
                getSolidOpaqueBackgroundColor(a, function(color) {
                    try {
                        var pipes = color.split(",");
                        var colorobj = new Color(parseInt(pipes[0]), parseInt(pipes[1]), parseInt(pipes[2]));
                        if (colorobj.hex == "#ffffff" || colorobj.hex == "#000000") throw "too white or black";
                        var rgbstr = colorobj.RGB.toString();
                        b.style.backgroundColor = rgbstr;
                    } catch (e) {}
                });
            }, 0, item.Properties.LogoBase64, logoimg.parentElement);
            Windows.UI.Event.Util.addEvent(appItem.querySelector("div[role=advance] a"), "click", function(e) {
                e.stopPropagation();
                try {
                    pagemgr.go("appinfo", this.parentNode.parentNode.parentNode.data);
                } catch (ex) {}
            });
            return appItem;
        });
        listView.selectionMode = "single";
        listView.bind(datasrc);
        var timer = null;

        function refreshAppList() {
            function update(datas) {
                var newDatas = [];
                for (var i = 0; i < datas.length; i++) {
                    var data = datas[i];
                    if (data.Properties.Framework) continue; // 过滤依赖项
                    var isfind = false; // 过滤系统应用
                    for (var j = 0; data && data.Users && j < data.Users.length; j++) {
                        if (Bridge.NString.equals(data.Users[j], "NT AUTHORITY\\SYSTEM")) {
                            isfind = true;
                            break;
                        }
                    }
                    if (isfind) continue;
                    newDatas.push(data);
                }
                datasrc.updateList(newDatas, function(item) {
                    return item.Identity.FullName || "";
                });
                var compare = function(a, b) { return a - b; };
                try {
                    compare = createLocalizedCompare(external.System.Locale.currentLocale);
                } catch (e) {
                    try {
                        compare = createLocalizedCompare(navigator.language);
                    } catch (e) {
                        compare = function(a, b) {
                            if (a < b) return -1;
                            if (a > b) return 1;
                            return 0;
                        };
                    }
                }
                datasrc.sort(function(a, b) {
                    return compare(a.Properties.DisplayName, b.Properties.DisplayName);
                });
            }
            if (timer) clearTimeout(timer);
            timer = null;
            loadingDisplay.style.display = "";
            loadingDisplay.classList.remove("noloading");

            function waitAndHide() {
                if (timer) clearTimeout(timer);
                timer = null;
                timer = setTimeout(function() {
                    loadingDisplay.style.display = "none";
                }, 10000);
            }
            loadingStatus.textContent = "正在加载数据...";
            return mgr.get().then(function(result) {
                loadingDisplay.classList.add("noloading");
                loadingStatus.textContent = "已经加载了所有数据";
                update(result.list);
                waitAndHide();
            }, function(error) {
                loadingDisplay.classList.add("noloading");
                loadingStatus.textContent = "更新时出错: " + (error.result ? (error.result.message || error.result.ErrorCode || "获取失败") : (error.message || error.error || error));
                try { update(error.list); } catch (e) {}
                waitAndHide();
            })
        }
        var appbar = document.getElementById("appBar");
        var appbarControl = new AppBar.AppBar(appbar);
        var refreshButton = new AppBar.Command();
        refreshButton.icon = "&#57623;";
        refreshButton.label = "刷新";
        global.refreshAppList2 = function refreshAppList2() {
            appbarControl.hide();
            refreshButton.disabled = true;
            refreshAppList().done(function() {
                refreshButton.disabled = false;
            }, function(error) {
                refreshButton.disabled = false;
            });
        }
        refreshButton.addEventListener("click", refreshAppList2);
        appbarControl.add(refreshButton);
        refreshAppList2();
        pagemgr.register("manager", document.getElementById("tag-manager"), document.getElementById("page-manager"));
        pagemgr.register("appinfo", document.getElementById("tag-appinfo"), document.getElementById("page-appinfo"), setAppInfoPageContent);
        var appinfoBackPage = document.getElementById("page-appinfo").querySelector(".win-backbutton");
        Windows.UI.Event.Util.addEvent(appinfoBackPage, "click", function(e) {
            pagemgr.back();
        });
        pagemgr.addEventListener("load", function(e) {
            appbarControl.enabled = e == "manager";
            refreshButton.style.display = e == "manager" ? "" : "none";
        });
        pagemgr.go("manager");
    });
})(this);