/**
 * DBox API
 * Docs: https://dbox.tools/api/docs
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
    var baseUrl = "https://dbox.tools/";
    var baseApiUrl = joinUrl(baseUrl, "api");
    var dboxApi = {
        /**
         * @enum {string} DBox.API.System
         * @readonly
         */
        System: {
            /** @type {string} Xbox */
            xbox: "XBOX",
            /** @type {string} Xbox 360 */
            xbox360: "XBOX360",
            /** @type {string} Xbox One */
            xboxOne: "XBOXONE",
            /** @type {string} Xbox Series X */
            xboxSeriesX: "XBOXSERIESX",
            /** @type {string} PC */
            pc: "PC",
            /** @type {string} PS3 */
            mobile: "MOBILE"
        },
        Discs: {
            /**
             * Get and filter discs
             * @param {string | null} name query
             * @param {DBox.API.System} system query
             * @param {integer | number} [limit] query, default: 100
             * @param {integer | number} [offset] query, default: 0
             * @returns {string} request URL, method: GET
             */
            getDiscs: function(name, system, limit, offset) {
                var params = {};
                if (name) params.name = name;
                if (system) params.system = system;
                if (limit) params.limit = limit;
                if (offset) params.offset = offset;

                return joinUrl(baseApiUrl, "discs") +
                    "?" + buildParams(params);
            },
            /**
             * Get single disc by DBox ID
             * @param {integer | number} discId path, required
             * @returns {string} request URL, method: GET
             */
            getSingleDiscById: function(discId) {
                return joinUrl(baseApiUrl, "discs/" + discId);
            },
            /**
             * Get single disc by Redump ID
             * @param {integer | number} redumpId path, required
             * @returns {string} request URL, method: GET
             */
            getSingleDiscByRedumpId: function(redumpId) {
                return joinUrl(baseApiUrl, "discs/redump/" + redumpId);
            },
            /**
             * Get single disc by its XMID (OG XBOX only)
             * @param {string} xmid path, required
             * @returns {string} request URL, method: GET
             */
            getSingleDiscByXMId: function(xmid) {
                return joinUrl(baseApiUrl, "discs/xmid/" + xmid);
            },
            /**
             * Get single disc by its XeMID (XBOX 360 only)
             * @param {string} xeMid path, required
             * @returns {string} request URL, method: GET
             */
            getSingleDiscByXeMId: function(xeMid) {
                return joinUrl(baseApiUrl, "discs/xemid/" + xeMid);
            },
            /**
             * Get single disc by its media ID. Media ID v1 (XBOX & XBOX 360) and Media ID v2 (XBOX One & XBOX Series) are combined in a single query
             * @param {string} mediaId path, required
             * @returns {string} request URL, method: GET
             */
            getSingleDiscByMediaId: function(mediaId) {
                return joinUrl(baseApiUrl, "discs/media_id/" + mediaId);
            },
            /**
             * Get the dirs and files (if available) for a disc. Filetype indicates if it is a supported filetype that has further metadata available in the database.
             * @param {integer | number} discId path, required
             * @returns {string} request URL, method: GET
             */
            getDiscFiles: function(discId) {
                return joinUrl(baseApiUrl, "discs/" + discId + "/files");
            }
        },
        Releases: {
            /**
             * Get and filter releases
             * @param {string | null} name query 
             * @param {string | null} edition query 
             * @param {string | null} barcode query
             * @param {DBox.API.System} system query
             * @param {integer | number} limit query, default: 100
             * @param {integer | number} offset query, default: 0
             * @returns {string} request URL, method: GET
             */
            getReleases: function(name, edition, barcode, system, limit, offset) {
                var params = {};
                if (name) params.name = name;
                if (edition) params.edition = edition;
                if (barcode) params.barcode = barcode;
                if (system) params.system = system;
                if (limit) params.limit = limit;
                if (offset) params.offset = offset;
                return joinUrl(baseApiUrl, "releases") +
                    "?" + buildParams(params);
            },
            /**
             * Get single release by DBox ID
             * @param {string} releaseId path, required 
             * @returns {string} request URL, method: GET
             */
            getSingleRelease: function(releaseId) {
                return joinUrl(baseApiUrl, "releases/" + releaseId);
            }
        },
        Files: {
            /**
             * Get all discs that contain a particular file. NOTE: not all discs have been parsed on file-level yet
             * @param {string} md5 path, required
             * @returns {string} request URL, method: GET
             */
            getFiles: function(md5) {
                return joinUrl(baseApiUrl, "files/" + md5 + "/discs");
            },
            /**
             * Filter all distinct xbe files. NOTE: not all discs have been parsed on file-level yet
             * @param {string | null} titleId query 
             * @param {integer | null} allowrdMedia query 
             * @param {integer | null} region query, Available values: 1, 2, 3, 4, 7, 2147483648
             * @param {integer | null} gameRating query
             * @param {integer | null} discNumber query
             * @param {integer | null} version query 
             * @param {integer | number} limit query, default: 100
             * @param {integer | number} offset query, default: 0
             * @returns 
             */
            getXbeFiles: function(titleId, allowrdMedia, region, gameRating, discNumber, version, limit, offset) {
                if (region !== null && region !== undefined) {
                    var valid = [1, 2, 3, 4, 7, 2147483648];
                    if (!valid.includes(region)) throw new Error('Invalid region');
                }
                var params = {};
                if (titleId) params.title_id = titleId;
                if (allowrdMedia) params.allowrd_media = allowrdMedia;
                if (region) params.region = region;
                if (gameRating) params.game_rating = gameRating;
                if (discNumber) params.disc_number = discNumber;
                if (version) params.version = version;
                if (limit) params.limit = limit;
                if (offset) params.offset = offset;
                return joinUrl(baseApiUrl, "files/xbe") +
                    "?" + buildParams(params);
            },
            /**
             * Get xbe file by md5. NOTE: not all discs have been parsed on file-level yet
             * @param {string} md5 path, required
             * @returns {string} request URL, method: GET
             */
            getXbeFileByMd5: function(md5) {
                return joinUrl(baseApiUrl, "files/xbe/" + md5);
            },
            /**
             * Get stfs file by md5. NOTE: not all discs have been parsed on file-level yet
             * @param {string} md5 path, required
             * @returns {string} request URL, method: GET
             */
            getStfsFileByMd5: function(md5) {
                return joinUrl(baseApiUrl, "files/stfs/" + md5);
            },
        },
        TitleIDs: {
            /**
             * Get and filter title IDs
             * @param {string | (string | null)} name query 
             * @param {DBox.API.System | null} system query 
             * @param {string | (string | null)($uuid)} bingId query
             * @param {string | (string | null)($uuid)} serviceConfigId query
             * @param {integer} limit query, default: 100
             * @param {integer} offset query, default: 0
             * @returns {string} request URL, method: GET
             */
            getTitleIds: function(name, system, bingId, serviceConfigId, limit, offset) {
                var params = {};
                if (name) params.name = name;
                if (system) params.system = system;
                if (bingId) params.bing_id = bingId;
                if (serviceConfigId) params.service_config_id = serviceConfigId;
                if (limit) params.limit = limit;
                if (offset) params.offset = offset;
                return joinUrl(baseApiUrl, "title_ids") +
                    "?" + buildParams(params);
            },
            /**
             * Get single title ID by its hexadecimal value
             * @param {string} titleId path, required
             * @returns {string} request URL, method: GET
             */
            getSingleTitleId: function(titleId) {
                return joinUrl(baseApiUrl, "title_ids/" + titleId);
            }
        },
        Achievements: {
            /**
             * Get achievements for a title-id. Xbox 360/GFWL only
             * @param {string} titleId path, required
             * @returns {string} request URL, method: GET
             */
            getAchievementsV1: function(titleId) {
                return joinUrl(baseApiUrl, "achievements/v1/" + titleId);
            },
            /**
             * Get achievements for a title-id. Xbox One/Series only
             * @param {string} titleId path, required 
             * @returns {string} request URL, method: GET
             */
            getAchievementsV2: function(titleId) {
                return joinUrl(baseApiUrl, "achievements/v2/" + titleId);
            },
        },
        Marketplace: {
            /**
             * Get and filter marketplace products
             * @param {integer | (integer | null)} productType query
             * @param {integer} limit query, default: 100
             * @param {integer} offset query, default: 0
             * @returns {string} request URL, method: GET
             */
            getProducts: function(productType, limit, offset) {
                var params = {};
                if (productType) params.product_type = productType;
                if (limit) params.limit = limit;
                if (offset) params.offset = offset;
                return joinUrl(baseApiUrl, "marketplace/products") +
                    "?" + buildParams(params);
            },
            /**
             * Get single marketplace product by marketplace product ID
             * @param {string} productId path, required
             * @returns {string} request URL, method: GET
             */
            getSingleProduct: function(productId) {
                return joinUrl(baseApiUrl, "marketplace/products/" + productId);
            },
            /**
             * Get children of a marketplace product
             * @param {string} productId  path, required
             * @param {integer | (integer | null)} productType query
             * @returns {string} request URL, method: GET
             */
            getProductChildren: function(productId, productType) {
                var params = {};
                if (productType) params.product_type = productType;
                return joinUrl(baseApiUrl, "marketplace/products/" + productId + "/children") +
                    "?" + buildParams(params);
            },
            /**
             * Get and filter marketplace product instances
             * @param {string | (string | null)($uuid)} productId query
             * @param {string | (string | null)} hexOfferId query
             * @param {integer | (integer | null)} licenseTypeId query
             * @param {integer | (integer | null)} packageType query
             * @param {integer | (integer | null)} gameRegion query
             * @param {integer} limit query, default: 100
             * @param {integer} offset query, default: 0
             * @returns {string} request URL, method: GET
             */
            getProductInstances: function(productId, hexOfferId, licenseTypeId, packageType, gameRegion, limit, offset) {
                var params = {};
                if (productId) params.product_id = productId;
                if (hexOfferId) params.hex_offer_id = hexOfferId;
                if (licenseTypeId) params.license_type_id = licenseTypeId;
                if (packageType) params.package_type = packageType;
                if (gameRegion) params.game_region = gameRegion;
                if (limit) params.limit = limit;
                if (offset) params.offset = offset;
                return joinUrl(baseApiUrl, "marketplace/product-instances") +
                    "?" + buildParams(params);
            },
            /**
             * Get single marketplace product instance by marketplace product instance ID
             * @param {string} instanceId path, required
             * @returns {string} request URL, method: GET
             */
            getProductInstanceById: function(instanceId) {
                return joinUrl(baseApiUrl, "marketplace/product-instances/" + instanceId);
            },
            /**
             * Get all marketplace product types
             * @returns {string} request URL, method: GET
             */
            getProductTypes: function() {
                return joinUrl(baseApiUrl, "marketplace/product-types");
            },
            /**
             * Get single marketplace category
             * @param {integer} productTypeId path, required
             * @returns {string} request URL, method: GET
             */
            getSingleProductType: function(productTypeId) {
                return joinUrl(baseApiUrl, "marketplace/product-types/" + productTypeId);
            },
            /**
             * Get and filter all marketplace categories
             * @param {integer | (integer | null)} parent query
             * @returns {string} request URL, method: GET
             */
            getCategories: function(parent) {
                var params = {};
                if (parent) params.parent = parent;
                return joinUrl(baseApiUrl, "marketplace/categories") +
                    "?" + buildParams(params);
            },
            /**
             * Get single marketplace category
             * @param {integer} categoryId path, required
             * @returns {string} request URL, method: GET
             */
            getSingleCategory: function(categoryId) {
                return joinUrl(baseApiUrl, "marketplace/categories/" + categoryId);
            },
            /**
             * Get all marketplace locales
             * @returns {string} request URL, method: GET
             */
            getLocales: function() {
                return joinUrl(baseApiUrl, "marketplace/locales");
            },
            /**
             * Get single marketplace locale by locale string
             * @param {string} localeId path, required
             * @returns {string} request URL, method: GET
             */
            getSingleLocale: function(localeId) {
                return joinUrl(baseApiUrl, "marketplace/locales/" + localeId);
            },
        },
        /**
         * Enum for product types, used in store API
         * @enum {string} DBox.API.ProductType
         */
        ProductType: {
            application: "Application",
            avatarItem: "AvatarItem",
            consumable: "Consumable",
            durable: "Durable",
            game: "Game",
            movie: "Movie",
            pass: "PASS",
            tvSeries: "TvSeries",
            tvSeason: "TvSeason",
            tvEpisode: "TVEpisode",
            unmanagedConsumable: "UnmanagedConsumable",
        },
        /**
         * Enum for product families, used in store API
         * @enum {string} DBox.API.ProductFamily
         */
        ProductFamily: {
            apps: "Apps",
            avatars: "Avatars",
            games: "Games",
            movies: "Movies",
            passes: "Passes",
            tv: "TV",
        },
        /**
         * Enum for order by, used in store API
         * @enum {string} DBox.API.OrderBy
         */
        OrderBy: {
            productId: "product_id",
            titleId: "title_id",
            revisionId: "revision_id",
            /** such as "23654onetwoonestudio.cctv_kdpw61jgbrs34" */
            packageFamilyName: "package_family_name",
            /** such as "23654onetwoonestudio.cctv" */
            packageIdentityName: "package_identity_name",
        },
        /**
         * Enum for order direction, used in store API
         * @enum {string} DBox.API.OrderDirection
         */
        OrderDirection: {
            /** @type {string} 升序 */
            asc: "asc",
            /** @type {string} 降序 */
            desc: "desc",
        },
        Store: {
            /**
             * Get store products
             * @param {string | (string | null)} category query
             * @param {string | (string | null)} titleId query
             * @param {DBox.API.ProductType | null} productType query
             * @param {DBox.API.ProductFamily | null} productFamily query
             * @param {DBox.API.OrderBy | null} orderBy query, default: productId
             * @param {DBox.API.OrderDirection | null} orderDirection query, default: asc
             * @param {integer} limit query, default: 100
             * @param {integer} offset query, default: 0
             * @returns {string} request URL, method: GET
             */
            getProducts: function(category, titleId, productType, productFamily, orderBy, orderDirection, limit, offset) {
                var params = {};
                if (category) params.category = category;
                if (titleId) params.title_id = titleId;
                if (productType) params.product_type = productType;
                if (productFamily) params.product_family = productFamily;
                if (orderBy) params.order_by = orderBy;
                if (orderDirection) params.order_direction = orderDirection;
                if (limit) params.limit = limit;
                if (offset) params.offset = offset;
                return joinUrl(baseApiUrl, "store/products") +
                    "?" + buildParams(params);
            },
            /**
             * Get single store product
             * @param {string} productId path, required
             * @returns {string} request URL, method: GET
             */
            getSingleProduct: function(productId) {
                return joinUrl(baseApiUrl, "store/products/" + productId);
            },
            /**
             * Get all related products for a product id. Includes both child and parent relationships. Check the product-ids for relationship direction. The relationship_type is parent -> child direction. Same combinations can appear multiple times with different relationship types.
             * @param {string} productId path, required
             * @returns {string} request URL, method: GET
             */
            getAllReleatedProducts: function(productId) {
                return joinUrl(baseApiUrl, "store/products/" + productId + "/related");
            },
            /**
             * Get single sku for store product
             * @param {string} productId path, required
             * @param {string} skuId path, required
             * @returns {string} request URL, method: GET
             */
            getSingleSkuFromProduct: function(productId, skuId) {
                return joinUrl(baseApiUrl, "store/products/" + productId + "/sku/" + skuId);
            }
        }
    };

    function getXhr() {
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
     * Send an HTTP request to the DBox API and return a Promise that resolves with the response.
     * @param {string} api DBox API
     * @param {string} method 
     * @param {[boolean]} isAsync default: true
     * @param {[string]} username
     * @param {[string]} pwd
     * @returns {Promise <XMLHttpRequest>} A Promise that resolves with the response.
     */
    var dboxXHR = function(api, method, isAsync, username, pwd) {
        method = method || "GET";
        if (typeof isAsync === "undefined" || isAsync === null) isAsync = true;
        var xhr = getXhr();
        if (username && pwd) {
            try {
                xhr.open(method, api, isAsync, username, pwd);
            } catch (e) {
                xhr.open(method, api, isAsync);
            }
        } else {
            xhr.open(method, api, isAsync);
        }
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
                xhr.send("");
            } catch (ex) {
                if (e) e(ex);
            }
        });
    };
    /**
     * Send an HTTP request to the DBox API and return a Promise that resolves with the response as JSON.
     * @param {string} api DBox API
     * @param {string} method 
     * @param {[boolean]} isAsync default: true
     * @param {[string]} username
     * @param {[string]} pwd
     * @returns {Promise <object>} A Promise that resolves with the response as JSON.
     */
    dboxXHR.parseJson = function(api, method, isAsync, username, pwd) {
            return dboxXHR(api, method, isAsync, username, pwd).then(function(xhr) {
                return JSON.parse(xhr.responseText);
            });
        }
        /**
         * DBox namespace
         * @namespace {DBox}
         */
    global.DBox = {
        /**
         * DBox API namespace
         * @namespace {DBox.API}
         */
        API: dboxApi,
        /**
         * @function {DBox.XHR}
         */
        xhr: dboxXHR,
    };
})(this);