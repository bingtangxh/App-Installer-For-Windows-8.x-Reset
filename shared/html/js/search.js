/*!
 * Search.Box - standalone SearchBox control (ES5, IE10 compatible)
 * Exposes constructor as global.Search.Box
 *
 * Features:
 * - API compatible-ish with WinJS.UI.SearchBox: properties (placeholderText, queryText, chooseSuggestionOnEnter, disabled)
 * - Events: querychanged, querysubmitted, resultsuggestionchosen, suggestionsrequested
 *   - supports both `element.addEventListener("querychanged", handler)` and `instance.onquerychanged = handler`
 * - Methods: setSuggestions(array), clearSuggestions(), dispose(), setLocalContentSuggestionSettings(settings) (noop)
 * - Suggestions kinds: Query (0), Result (1), Separator (2) OR string names 'query'/'result'/'separator'
 * - Hit highlighting: uses item.hits if provided, otherwise simple substring match of current input
 * - No WinRT / WinJS dependency
 *
 * Usage:
 *   var box = new Search.Box(hostElement, options);
 *   box.setSuggestions([{ kind: 0, text: "hello" }, ...]);
 */

(function(global) {
    "use strict";

    // Ensure namespace
    if (!global.Search) {
        global.Search = {};
    }

    // Suggestion kinds
    var SuggestionKind = {
        Query: 0,
        Result: 1,
        Separator: 2
    };

    // Utility: create id
    function uniqueId(prefix) {
        return prefix + Math.random().toString(36).slice(2);
    }

    // Simple CustomEvent fallback for IE10
    function createCustomEvent(type, detail) {
        var ev;
        try {
            ev = document.createEvent("CustomEvent");
            ev.initCustomEvent(type, true, true, detail || {});
        } catch (e) {
            ev = document.createEvent("Event");
            ev.initEvent(type, true, true);
            ev.detail = detail || {};
        }
        return ev;
    }

    // Constructor
    function SearchBox(element, options) {
        element = element || document.createElement("div");

        if (element.__searchBoxInstance) {
            throw new Error("Search.Box: duplicate construction on same element");
        }
        element.__searchBoxInstance = this;

        // DOM elements
        this._root = element;
        this._input = document.createElement("input");
        this._input.type = "search";
        this._button = document.createElement("div");
        this._button.tabIndex = -1;
        this._flyout = document.createElement("div");
        this._repeater = document.createElement("div"); // container for suggestion items
        this._flyout.style.display = "none";
        // state
        this._suggestions = [];
        this._currentSelectedIndex = -1; // fake focus/selection index
        this._currentFocusedIndex = -1; // navigation focus index
        this._prevQueryText = "";
        this._chooseSuggestionOnEnter = false;
        this._disposed = false;
        this._lastKeyPressLanguage = "";

        // classes follow WinJS naming where convenient (so your existing CSS can still be used)
        this._root.className = (this._root.className ? this._root.className + " " : "") + "win-searchbox";
        this._input.className = "win-searchbox-input";
        this._button.className = "win-searchbox-button";
        this._flyout.className = "win-searchbox-flyout";
        this._repeater.className = "win-searchbox-repeater";

        // assemble
        this._flyout.appendChild(this._repeater);
        this._root.appendChild(this._input);
        this._root.appendChild(this._button);
        this._root.appendChild(this._flyout);

        // accessibility basics
        this._root.setAttribute("role", "group");
        this._input.setAttribute("role", "textbox");
        this._button.setAttribute("role", "button");
        this._repeater.setAttribute("role", "listbox");
        if (!this._repeater.id) {
            this._repeater.id = uniqueId("search_repeater_");
        }
        this._input.setAttribute("aria-controls", this._repeater.id);
        this._repeater.setAttribute("aria-live", "polite");

        // user-assignable event handlers (older style)
        this.onquerychanged = null;
        this.onquerysubmitted = null;
        this.onresultsuggestionchosen = null;
        this.onsuggestionsrequested = null;

        // wire events
        this._wireEvents();

        // options
        options = options || {};
        if (options.placeholderText) this.placeholderText = options.placeholderText;
        if (options.queryText) this.queryText = options.queryText;
        if (options.chooseSuggestionOnEnter) this.chooseSuggestionOnEnter = !!options.chooseSuggestionOnEnter;
        if (options.disabled) this.disabled = !!options.disabled;

        // new events
        this.ontextchanged = null;
    }

    // Prototype
    SearchBox.prototype = {
        // Properties
        get element() {
            return this._root;
        },

        get placeholderText() {
            return this._input.placeholder;
        },
        set placeholderText(value) {
            this._input.placeholder = value || "";
        },

        get queryText() {
            return this._input.value;
        },
        set queryText(value) {
            this._input.value = value == null ? "" : value;
        },

        get chooseSuggestionOnEnter() {
            return this._chooseSuggestionOnEnter;
        },
        set chooseSuggestionOnEnter(v) {
            this._chooseSuggestionOnEnter = !!v;
            this._updateButtonClass();
        },

        get disabled() {
            return !!this._input.disabled;
        },
        set disabled(v) {
            var val = !!v;
            if (val === this.disabled) return;
            this._input.disabled = val;
            try { this._button.disabled = val; } catch (e) {}
            if (val) {
                this._root.className = (this._root.className + " win-searchbox-disabled").trim();
                this.hideFlyout();
            } else {
                this._root.className = this._root.className.replace(/\bwin-searchbox-disabled\b/g, "").trim();
            }
        },

        // Public methods
        setSuggestions: function(arr) {
            // Expect array of objects with keys: kind (0/1/2 or 'query'/'result'/'separator'), text, detailText, tag, imageUrl, hits
            this._suggestions = (arr && arr.slice(0)) || [];
            this._currentSelectedIndex = -1;
            this._currentFocusedIndex = -1;
            this._renderSuggestions();
            if (this._suggestions.length) this.showFlyout();
            else this.hideFlyout();
        },

        clearSuggestions: function() {
            this.setSuggestions([]);
        },

        showFlyout: function() {
            if (!this._suggestions || this._suggestions.length === 0) return;
            this._flyout.style.display = "block";
            this._updateButtonClass();
        },

        hideFlyout: function() {
            this._flyout.style.display = "none";
            this._updateButtonClass();
        },

        dispose: function() {
            if (this._disposed) return;
            // detach event listeners by cloning elements (simple way)
            var newRoot = this._root.cloneNode(true);
            if (this._root.parentNode) {
                this._root.parentNode.replaceChild(newRoot, this._root);
            }
            try {
                delete this._root.__searchBoxInstance;
            } catch (e) {}
            this._disposed = true;
        },

        setLocalContentSuggestionSettings: function(settings) {
            // No-op in non-WinRT environment; kept for API compatibility.
        },

        // Internal / rendering
        _wireEvents: function() {
            var that = this;

            this._input.addEventListener("input", function(ev) {
                that._onInputChange(ev);
            }, false);

            this._input.addEventListener("keydown", function(ev) {
                that._onKeyDown(ev);
            }, false);

            this._input.addEventListener("keypress", function(ev) {
                // capture locale if available
                try { that._lastKeyPressLanguage = ev.locale || that._lastKeyPressLanguage; } catch (e) {}
            }, false);

            this._input.addEventListener("focus", function() {
                if (that._suggestions.length) {
                    that.showFlyout();
                    that._updateFakeFocus();
                }
                that._root.className = (that._root.className + " win-searchbox-input-focus").trim();
                that._updateButtonClass();
            }, false);

            this._input.addEventListener("blur", function() {
                // small timeout to allow suggestion click to process
                setTimeout(function() {
                    if (!that._root.contains(document.activeElement)) {
                        that.hideFlyout();
                        that._root.className = that._root.className.replace(/\bwin-searchbox-input-focus\b/g, "").trim();
                        that._currentFocusedIndex = -1;
                        that._currentSelectedIndex = -1;
                    }
                }, 0);
            }, false);

            this._button.addEventListener("click", function(ev) {
                that._input.focus();
                that._submitQuery(that._input.value, ev);
                that.hideFlyout();
            }, false);

            // delegate click for suggestions: attach on repeater container (works in IE10)
            this._repeater.addEventListener("click", function(ev) {
                var el = ev.target;
                // climb until we find child with data-index
                while (el && el !== that._repeater) {
                    if (el.hasAttribute && el.hasAttribute("data-index")) break;
                    el = el.parentNode;
                }
                if (el && el !== that._repeater) {
                    var idx = parseInt(el.getAttribute("data-index"), 10);
                    var item = that._suggestions[idx];
                    if (item) {
                        that._input.focus();
                        that._processSuggestionChosen(item, ev);
                    }
                }
            }, false);
        },

        _onInputChange: function(ev) {
            if (this.disabled) return;
            var v = this._input.value;

            this._emit("textchanged", {
                text: v
            }, this.ontextchanged);

            var changed = (v !== this._prevQueryText);
            this._prevQueryText = v;

            // fire querychanged
            var evDetail = {
                language: this._getBrowserLanguage(),
                queryText: v,
                linguisticDetails: { queryTextAlternatives: [], queryTextCompositionStart: 0, queryTextCompositionLength: 0 }
            };
            this._emit("querychanged", evDetail, this.onquerychanged);

            // fire suggestionsrequested - allow client to call setSuggestions
            var suggestionsDetail = {
                queryText: v,
                language: this._getBrowserLanguage(),
                setSuggestions: (function(thatRef) {
                    return function(arr) {
                        thatRef.setSuggestions(arr || []);
                    };
                })(this)
            };
            this._emit("suggestionsrequested", suggestionsDetail, this.onsuggestionsrequested);
        },

        _submitQuery: function(queryText, ev) {
            var detail = {
                language: this._getBrowserLanguage(),
                queryText: queryText,
                keyModifiers: this._getKeyModifiers(ev)
            };
            this._emit("querysubmitted", detail, this.onquerysubmitted);
        },

        _processSuggestionChosen: function(item, ev) {
            // normalize kind
            var kind = item.kind;
            if (typeof kind === "string") {
                if (kind.toLowerCase() === "query") kind = SuggestionKind.Query;
                else if (kind.toLowerCase() === "result") kind = SuggestionKind.Result;
                else if (kind.toLowerCase() === "separator") kind = SuggestionKind.Separator;
            }

            this.queryText = item.text || "";
            if (kind === SuggestionKind.Query || kind === undefined) {
                // choose query -> submit
                this._submitQuery(item.text || "", ev);
            } else if (kind === SuggestionKind.Result) {
                this._emit("resultsuggestionchosen", {
                    tag: item.tag,
                    keyModifiers: this._getKeyModifiers(ev),
                    storageFile: null
                }, this.onresultsuggestionchosen);
            }
            this.hideFlyout();
        },

        _renderSuggestions: function() {
            // clear repeater
            while (this._repeater.firstChild) this._repeater.removeChild(this._repeater.firstChild);

            var frag = document.createDocumentFragment();
            for (var i = 0; i < this._suggestions.length; i++) {
                var s = this._suggestions[i];
                var itemEl = this._renderSuggestion(s, i);
                frag.appendChild(itemEl);
            }
            this._repeater.appendChild(frag);
            this._updateFakeFocus();
        },

        _renderSuggestion: function(item, index) {
            var that = this;
            var kind = item.kind;
            if (typeof kind === "string") {
                kind = kind.toLowerCase() === "query" ? SuggestionKind.Query :
                    kind.toLowerCase() === "result" ? SuggestionKind.Result :
                    kind.toLowerCase() === "separator" ? SuggestionKind.Separator : kind;
            }

            var root = document.createElement("div");
            root.setAttribute("data-index", index);
            root.id = this._repeater.id + "_" + index;

            if (kind === SuggestionKind.Separator) {
                root.className = "win-searchbox-suggestion-separator";
                if (item.text) {
                    var textEl = document.createElement("div");
                    textEl.innerText = item.text;
                    textEl.setAttribute("aria-hidden", "true");
                    root.appendChild(textEl);
                }
                root.insertAdjacentHTML("beforeend", "<hr/>");
                root.setAttribute("role", "separator");
                root.setAttribute("aria-label", item.text || "");
                return root;
            }

            if (kind === SuggestionKind.Result) {
                root.className = "win-searchbox-suggestion-result";
                // image
                var img = document.createElement("img");
                img.setAttribute("aria-hidden", "true");
                if (item.imageUrl) {
                    img.onload = function() {
                        img.style.opacity = "1";
                    };
                    img.style.opacity = "0";
                    img.src = item.imageUrl;
                } else {
                    img.style.display = "none";
                }
                root.appendChild(img);

                var textDiv = document.createElement("div");
                textDiv.className = "win-searchbox-suggestion-result-text";
                textDiv.setAttribute("aria-hidden", "true");
                this._addHitHighlightedText(textDiv, item, item.text || "");
                textDiv.title = item.text || "";
                root.appendChild(textDiv);

                var detail = document.createElement("span");
                detail.className = "win-searchbox-suggestion-result-detailed-text";
                detail.setAttribute("aria-hidden", "true");
                this._addHitHighlightedText(detail, item, item.detailText || "");
                textDiv.appendChild(document.createElement("br"));
                textDiv.appendChild(detail);

                root.setAttribute("role", "option");
                root.setAttribute("aria-label", (item.text || "") + " " + (item.detailText || ""));
                return root;
            }

            // default / query
            root.className = "win-searchbox-suggestion-query";
            this._addHitHighlightedText(root, item, item.text || "");
            root.title = item.text || "";
            root.setAttribute("role", "option");
            root.setAttribute("aria-label", item.text || "");
            return root;
        },

        _addHitHighlightedText: function(container, item, text) {
            // Remove existing children
            while (container.firstChild) container.removeChild(container.firstChild);
            if (!text) return;

            // Build hits from item.hits if present (array of {startPosition, length}), otherwise simple substring matches of current input
            var hits = [];
            if (item && item.hits && item.hits.length) {
                for (var i = 0; i < item.hits.length; i++) {
                    hits.push({ startPosition: item.hits[i].startPosition, length: item.hits[i].length });
                }
            } else {
                var q = this._input.value || "";
                if (q) {
                    var low = text.toLowerCase();
                    var lq = q.toLowerCase();
                    var pos = 0;
                    while (true) {
                        var idx = low.indexOf(lq, pos);
                        if (idx === -1) break;
                        hits.push({ startPosition: idx, length: q.length });
                        pos = idx + q.length;
                    }
                }
            }

            // Merge overlapping hits & sort
            hits.sort(function(a, b) { return a.startPosition - b.startPosition; });
            var merged = [];
            for (var j = 0; j < hits.length; j++) {
                if (merged.length === 0) {
                    merged.push({ startPosition: hits[j].startPosition, length: hits[j].length });
                } else {
                    var cur = merged[merged.length - 1];
                    var curEnd = cur.startPosition + cur.length;
                    if (hits[j].startPosition <= curEnd) {
                        var nextEnd = hits[j].startPosition + hits[j].length;
                        if (nextEnd > curEnd) {
                            cur.length = nextEnd - cur.startPosition;
                        }
                    } else {
                        merged.push({ startPosition: hits[j].startPosition, length: hits[j].length });
                    }
                }
            }

            var last = 0;
            for (var k = 0; k < merged.length; k++) {
                var h = merged[k];
                if (h.startPosition > last) {
                    var pre = document.createElement("span");
                    pre.innerText = text.substring(last, h.startPosition);
                    pre.setAttribute("aria-hidden", "true");
                    container.appendChild(pre);
                }
                var hitSpan = document.createElement("span");
                hitSpan.innerText = text.substring(h.startPosition, h.startPosition + h.length);
                hitSpan.className = "win-searchbox-flyout-highlighttext";
                hitSpan.setAttribute("aria-hidden", "true");
                container.appendChild(hitSpan);
                last = h.startPosition + h.length;
            }
            if (last < text.length) {
                var post = document.createElement("span");
                post.innerText = text.substring(last);
                post.setAttribute("aria-hidden", "true");
                container.appendChild(post);
            }

            if (merged.length === 0) {
                // no hits - append plain text
                var whole = document.createElement("span");
                whole.innerText = text;
                whole.setAttribute("aria-hidden", "true");
                container.appendChild(whole);
            }
        },

        _updateFakeFocus: function() {
            var firstIndex = -1;
            if ((this._flyout.style.display !== "none") && this._chooseSuggestionOnEnter) {
                for (var i = 0; i < this._suggestions.length; i++) {
                    var s = this._suggestions[i];
                    var kind = s.kind;
                    if (typeof kind === "string") {
                        kind = kind.toLowerCase() === "query" ? SuggestionKind.Query :
                            kind.toLowerCase() === "result" ? SuggestionKind.Result :
                            kind.toLowerCase() === "separator" ? SuggestionKind.Separator : kind;
                    }
                    if (kind === SuggestionKind.Query || kind === SuggestionKind.Result) {
                        firstIndex = i;
                        break;
                    }
                }
            }
            this._selectSuggestionAtIndex(firstIndex);
        },

        _selectSuggestionAtIndex: function(index) {
            for (var i = 0; i < this._repeater.children.length; i++) {
                var el = this._repeater.children[i];
                if (!el) continue;
                if (i === index) {
                    if (el.className.indexOf("win-searchbox-suggestion-selected") === -1) {
                        el.className = (el.className + " win-searchbox-suggestion-selected").trim();
                    }
                    el.setAttribute("aria-selected", "true");
                    try {
                        this._input.setAttribute("aria-activedescendant", el.id);
                    } catch (e) {}
                    // ensure visible
                    try {
                        var top = el.offsetTop;
                        var bottom = top + el.offsetHeight;
                        var scrollTop = this._flyout.scrollTop;
                        var height = this._flyout.clientHeight;
                        if (bottom > scrollTop + height) this._flyout.scrollTop = bottom - height;
                        else if (top < scrollTop) this._flyout.scrollTop = top;
                    } catch (e) {}
                } else {
                    el.className = el.className.replace(/\bwin-searchbox-suggestion-selected\b/g, "").trim();
                    el.setAttribute("aria-selected", "false");
                }
            }
            if (index === -1) {
                try { this._input.removeAttribute("aria-activedescendant"); } catch (e) {}
            }
            this._currentSelectedIndex = index;
            this._updateButtonClass();
        },

        _updateButtonClass: function() {
            if ((this._currentSelectedIndex !== -1) || (document.activeElement !== this._input)) {
                this._button.className = this._button.className.replace(/\bwin-searchbox-button-input-focus\b/g, "").trim();
            } else if (document.activeElement === this._input) {
                if (this._button.className.indexOf("win-searchbox-button-input-focus") === -1) {
                    this._button.className = (this._button.className + " win-searchbox-button-input-focus").trim();
                }
            }
        },

        _onKeyDown: function(ev) {
            // Normalize key
            var key = ev.key || "";
            if (!key && ev.keyCode) {
                if (ev.keyCode === 13) key = "Enter";
                else if (ev.keyCode === 27) key = "Esc";
                else if (ev.keyCode === 38) key = "Up";
                else if (ev.keyCode === 40) key = "Down";
                else if (ev.keyCode === 9) key = "Tab";
            }

            if (key === "Tab") {
                // handle tab navigation into suggestions
                if (ev.shiftKey) {
                    // shift+tab: allow default behavior
                } else {
                    if (this._currentFocusedIndex === -1) {
                        this._currentFocusedIndex = this._findNextSuggestionElementIndex(-1);
                    }
                    if (this._currentFocusedIndex !== -1) {
                        this._selectSuggestionAtIndex(this._currentFocusedIndex);
                        this._updateQueryTextWithSuggestionText(this._currentFocusedIndex);
                        ev.preventDefault();
                        ev.stopPropagation();
                    }
                }
            } else if (key === "Esc") {
                if (this._currentFocusedIndex !== -1) {
                    this.queryText = this._prevQueryText;
                    this._currentFocusedIndex = -1;
                    this._selectSuggestionAtIndex(-1);
                    this._updateButtonClass();
                    ev.preventDefault();
                    ev.stopPropagation();
                } else if (this.queryText !== "") {
                    this.queryText = "";
                    // trigger querychanged handlers
                    this._onInputChange(null);
                    this._updateButtonClass();
                    ev.preventDefault();
                    ev.stopPropagation();
                }
            } else if (key === "Up") {
                var prev;
                if (this._currentSelectedIndex !== -1) {
                    prev = this._findPreviousSuggestionElementIndex(this._currentSelectedIndex);
                    if (prev === -1) this.queryText = this._prevQueryText;
                } else {
                    prev = this._findPreviousSuggestionElementIndex(this._suggestions.length);
                }
                this._currentFocusedIndex = prev;
                this._selectSuggestionAtIndex(prev);
                this._updateQueryTextWithSuggestionText(this._currentFocusedIndex);
                this._updateButtonClass();
                ev.preventDefault();
                ev.stopPropagation();
            } else if (key === "Down") {
                var next = this._findNextSuggestionElementIndex(this._currentSelectedIndex);
                if ((this._currentSelectedIndex !== -1) && (next === -1)) this.queryText = this._prevQueryText;
                this._currentFocusedIndex = next;
                this._selectSuggestionAtIndex(next);
                this._updateQueryTextWithSuggestionText(this._currentFocusedIndex);
                this._updateButtonClass();
                ev.preventDefault();
                ev.stopPropagation();
            } else if (key === "Enter") {
                if (this._currentSelectedIndex === -1) {
                    this._submitQuery(this._input.value, ev);
                } else {
                    var chosen = this._suggestions[this._currentSelectedIndex];
                    if (chosen) this._processSuggestionChosen(chosen, ev);
                    else this._submitQuery(this._input.value, ev);
                }
                this.hideFlyout();
                ev.preventDefault();
                ev.stopPropagation();
            } else {
                // typing -> clear selection
                if (this._currentFocusedIndex !== -1) {
                    this._currentFocusedIndex = -1;
                    this._selectSuggestionAtIndex(-1);
                    this._updateFakeFocus();
                }
            }
        },

        _findNextSuggestionElementIndex: function(curIndex) {
            var start = curIndex + 1;
            if (start < 0) start = 0;
            for (var i = start; i < this._suggestions.length; i++) {
                var s = this._suggestions[i];
                var k = s.kind;
                if (typeof k === "string") {
                    k = k.toLowerCase() === "query" ? SuggestionKind.Query :
                        k.toLowerCase() === "result" ? SuggestionKind.Result :
                        k.toLowerCase() === "separator" ? SuggestionKind.Separator : k;
                }
                if (k === SuggestionKind.Query || k === SuggestionKind.Result) return i;
            }
            return -1;
        },

        _findPreviousSuggestionElementIndex: function(curIndex) {
            var start = curIndex - 1;
            if (start >= this._suggestions.length) start = this._suggestions.length - 1;
            for (var i = start; i >= 0; i--) {
                var s = this._suggestions[i];
                var k = s.kind;
                if (typeof k === "string") {
                    k = k.toLowerCase() === "query" ? SuggestionKind.Query :
                        k.toLowerCase() === "result" ? SuggestionKind.Result :
                        k.toLowerCase() === "separator" ? SuggestionKind.Separator : k;
                }
                if (k === SuggestionKind.Query || k === SuggestionKind.Result) return i;
            }
            return -1;
        },

        _updateQueryTextWithSuggestionText: function(idx) {
            if ((idx >= 0) && (idx < this._suggestions.length)) {
                this.queryText = this._suggestions[idx].text || "";
            }
        },

        _emit: function(type, detail, handler) {
            var ev = createCustomEvent(type, detail);
            // call handler property first
            if (typeof handler === "function") {
                try { handler.call(this, ev); } catch (e) { /* swallow */ }
            }
            try { this._root.dispatchEvent(ev); } catch (e) { /* swallow */ }
            return ev;
        },

        _getBrowserLanguage: function() {
            try {
                return (navigator && (navigator.language || navigator.userLanguage)) || "";
            } catch (e) {
                return "";
            }
        },

        _getKeyModifiers: function(ev) {
            var m = 0;
            if (!ev) return m;
            if (ev.ctrlKey) m |= 1;
            if (ev.altKey) m |= 2;
            if (ev.shiftKey) m |= 4;
            return m;
        }
    };

    // export
    global.Search.Box = SearchBox;

})(this);