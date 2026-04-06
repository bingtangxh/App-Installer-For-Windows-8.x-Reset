(function(global) {
    if (typeof global.Web === "undefined") global.Web = {};

    function dataToString(obj) {
        if (typeof obj === "string") return obj;
        if (typeof obj === "object") return JSON.stringify(obj);
        return String(obj);
    }
    global.Web.Http = {
        Request: function() {
            var inst = external.Web.Http.createHttpRequest();
            this.open = function(method, url) { inst.open(method, url); };
            this.getHeaders = function() { return JSON.parse(inst.getHeadersToJson()); };
            this.getHeader = function(swName) { try { return inst.getHeader(swName); } catch (e) { return void 0; } };
            this.setHeader = function(swName, swValue) { inst.setHeader(swName, swValue); };
            this.removeHeader = function(swName) { inst.removeHeader(swName); };
            this.clearHeader = function() { inst.clearHeader(); };
            this.updateHeaders = function(headers) {
                var keys = Object.keys(headers);
                this.clearHeader();
                for (var i = 0; i < keys.length; i++) {
                    this.setHeader(keys[i], dataToString(headers[keys[i]]));
                }
            };
            this.setHeaders = function(headers) {
                var keys = Object.keys(headers);
                for (var i = 0; i < keys.length; i++) {
                    inst.setHeader(keys[i], dataToString(headers[keys[i]]));
                }
            };
            this.send = function(body, encoding) { return inst.send(body, encoding); };
            this.sendAsync = function(body, encoding) {
                if (!encoding) encoding = "utf-8";
                return new Promise(function(c, e) {
                    try {
                        inst.sendAsync(body, encoding, function(resp) {
                            if (c) c(resp);
                        }, function(err) {
                            if (e) e(err);
                        });
                    } catch (ex) { if (e) e(ex); }
                });
            };
            Object.defineProperty(this, "timeout", {
                get: function() { return inst.timeout; },
                set: function(value) { inst.timeout = value; }
            });
            Object.defineProperty(this, "readWriteTimeout", {
                get: function() { return inst.readWriteTimeout; },
                set: function(value) { inst.readWriteTimeout = value; }
            });
            Object.defineProperty(this, "allowAutoRedirect", {
                get: function() { return inst.allowAutoRedirect; },
                set: function(value) { inst.allowAutoRedirect = value; }
            });
            Object.defineProperty(this, "allowWriteStreamBuffering", {
                get: function() { return inst.allowWriteStreamBuffering; },
                set: function(value) { inst.allowWriteStreamBuffering = value; }
            });
            Object.defineProperty(this, "keepAlive", {
                get: function() { return inst.keepAlive; },
                set: function(value) { inst.keepAlive = value; }
            });
            Object.defineProperty(this, "maximumAutomaticRedirections", {
                get: function() { return inst.maximumAutomaticRedirections; },
                set: function(value) { inst.maximumAutomaticRedirections = value; }
            });
            Object.defineProperty(this, "userAgent", {
                get: function() { return inst.userAgent; },
                set: function(value) { inst.userAgent = value; }
            });
            Object.defineProperty(this, "referer", {
                get: function() { return inst.referer; },
                set: function(value) { inst.referer = value; }
            });
            Object.defineProperty(this, "contentType", {
                get: function() { return inst.contentType; },
                set: function(value) { inst.contentType = value; }
            });
            Object.defineProperty(this, "accept", {
                get: function() { return inst.accept; },
                set: function(value) { inst.accept = value; }
            });
            Object.defineProperty(this, "proxy", {
                get: function() { return inst.proxy; },
                set: function(value) { inst.proxy = value; }
            });
            Object.defineProperty(this, "cookieContainer", {
                get: function() { return inst.cookieContainer; },
                set: function(value) { inst.cookieContainer = value; }
            });
            Object.defineProperty(this, "protocolVersion", {
                get: function() { return inst.protocolVersion; },
                set: function(value) { inst.protocolVersion = value; }
            });
            Object.defineProperty(this, "preAuthenticate", {
                get: function() { return inst.preAuthenticate; },
                set: function(value) { inst.preAuthenticate = value; }
            });
            Object.defineProperty(this, "credentials", {
                get: function() { return inst.credentials; },
                set: function(value) { inst.credentials = value; }
            });
            Object.defineProperty(this, "automaticDecompression", {
                get: function() { return inst.automaticDecompression; },
                set: function(value) { inst.automaticDecompression = value; }
            });
            this.dispose = function() { inst.dispose(); };
        },
        Header: {
            // 通用头（既可用于请求也可用于响应）
            General: {
                cacheControl: "Cache-Control",
                connection: "Connection",
                date: "Date",
                pragma: "Pragma",
                trailer: "Trailer",
                transferEncoding: "Transfer-Encoding",
                upgrade: "Upgrade",
                via: "Via",
                warning: "Warning"
            },
            // 请求头
            Request: {
                accept: "Accept",
                acceptCharset: "Accept-Charset",
                acceptEncoding: "Accept-Encoding",
                acceptLanguage: "Accept-Language",
                authorization: "Authorization",
                expect: "Expect",
                from: "From",
                host: "Host",
                ifMatch: "If-Match",
                ifModifiedSince: "If-Modified-Since",
                ifNoneMatch: "If-None-Match",
                ifRange: "If-Range",
                ifUnmodifiedSince: "If-Unmodified-Since",
                maxForwards: "Max-Forwards",
                proxyAuthorization: "Proxy-Authorization",
                range: "Range",
                referer: "Referer",
                te: "TE",
                userAgent: "User-Agent"
            },
            // 响应头
            Response: {
                acceptRanges: "Accept-Ranges",
                age: "Age",
                allow: "Allow",
                contentEncoding: "Content-Encoding",
                contentLanguage: "Content-Language",
                contentLength: "Content-Length",
                contentLocation: "Content-Location",
                contentRange: "Content-Range",
                contentType: "Content-Type",
                etag: "ETag",
                expires: "Expires",
                lastModified: "Last-Modified",
                location: "Location",
                proxyAuthenticate: "Proxy-Authenticate",
                retryAfter: "Retry-After",
                server: "Server",
                setCookie: "Set-Cookie",
                vary: "Vary",
                wwwAuthenticate: "WWW-Authenticate"
            },
            // CORS 相关头（常单独列出，也可归入请求/响应）
            Cors: {
                accessControlAllowOrigin: "Access-Control-Allow-Origin",
                accessControlAllowCredentials: "Access-Control-Allow-Credentials",
                accessControlAllowHeaders: "Access-Control-Allow-Headers",
                accessControlAllowMethods: "Access-Control-Allow-Methods",
                accessControlExposeHeaders: "Access-Control-Expose-Headers",
                accessControlMaxAge: "Access-Control-Max-Age",
                accessControlRequestHeaders: "Access-Control-Request-Headers",
                accessControlRequestMethod: "Access-Control-Request-Method",
                origin: "Origin"
            },
            // 安全/非标准常用头
            Security: {
                xFrameOptions: "X-Frame-Options",
                xContentTypeOptions: "X-Content-Type-Options",
                xXssProtection: "X-XSS-Protection",
                strictTransportSecurity: "Strict-Transport-Security",
                contentSecurityPolicy: "Content-Security-Policy",
                referrerPolicy: "Referrer-Policy",
                xRequestedWith: "X-Requested-With",
                xForwardedFor: "X-Forwarded-For",
                xForwardedProto: "X-Forwarded-Proto",
                xRealIp: "X-Real-IP"
            }
        },
        Method: {
            get: "GET",
            post: "POST",
            put: "PUT",
            delete: "DELETE",
            head: "HEAD",
            options: "OPTIONS",
            trace: "TRACE",
            connect: "CONNECT"
        },
        Version: {
            v1_0: "1.0",
            v1_1: "1.1",
        }
    }
})(this);