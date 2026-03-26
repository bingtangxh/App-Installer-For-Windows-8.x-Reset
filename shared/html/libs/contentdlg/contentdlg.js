/// <reference path="//Microsoft.WinJS.2.0/js/base.js" />
/// <reference path="//Microsoft.WinJS.2.0/js/ui.js" />

(function (global) {
	"use strict";

	if (typeof global.WinJS === "undefined" || !global.WinJS) global.WinJS = {};
	if (typeof global.WinJS.UI === "undefined" || !global.WinJS.UI) global.WinJS.UI = {};

	// 事件混入
	function mixEventMixin(target) {
		var eventMixin = WinJS.Utilities.eventMixin;
		target.addEventListener = eventMixin.addEventListener;
		target.removeEventListener = eventMixin.removeEventListener;
		target.dispatchEvent = eventMixin.dispatchEvent;
		target._listeners = target._listeners || {};
	}

	/**
     * 表示 ContentDialog 中的一个按钮命令。
     * @class
     * @param {string} label 按钮显示文本
     * @param {function(Event=): (boolean|void)} [handler] 按钮点击处理函数
     * @param {string} [commandId] 命令唯一标识
     */
	function ContentDialogCommand(label, handler, commandId) {
		this.label = label;
		this.handler = handler;
		this.commandId = commandId;
	}

	/**
     * 模拟 WinRT ContentDialog 的对话框
     * @class
     * @memberof WinJS.UI
     * @param {HTMLElement} element 容器
     * @param {Object} [options] 初始化选项
     */
	function ContentDialog(element, options) {
		var container = element;
		container.innerHTML = toStaticHTML('<div class="win-contentdialog-dialog"><div class="win-contentdialog-title"></div><div class="win-contentdialog-content"></div><div class="win-contentdialog-commands"></div></div>');
		container.classList.add("win-contentdialog");
		container.classList.add("hide");

		var title = container.querySelector(".win-contentdialog-title");
		var content = container.querySelector(".win-contentdialog-content");
		var commandContainer = container.querySelector(".win-contentdialog-commands");
		var _isdisposed = false;

		mixEventMixin(this);
		var self = this;

		// 命令集合
		this._commands = new WinJS.Binding.List();
		var _showAsyncPromise = null;
		var _showAsyncResolve = null;

		// 渲染按钮
		function renderCommands() {
			while (commandContainer.firstChild) {
				commandContainer.removeChild(commandContainer.firstChild);
			}
			self._commands.forEach(function (cmd, index) {
				var btn = document.createElement("button");
				btn.textContent = cmd.label;
				btn.setAttribute("data-command-id", cmd.commandId || index);

				btn.addEventListener("click", function (evt) {
					handleCommandClick(cmd, evt);
				});

				commandContainer.appendChild(btn);
			});
			if (self._commands.length > 0) {
				self.primaryCommandIndex = 0;
			}
		}

		// 按钮点击处理
		function handleCommandClick(cmd, evt) {
			var result;

			if (typeof cmd.handler === "function") {
				result = cmd.handler.call(self, evt);
			}

			function complete() {
				self.hide().then(function () {
					if (_showAsyncResolve) {
						_showAsyncResolve(cmd.commandId);
						clearAsyncState();
					}
				});
			}

			// handler 返回 false → 不关闭
			if (result === false) return;

			// handler 返回 Promise → 等待
			if (result && typeof result.then === "function") {
				result.then(complete);
				return;
			}

			complete();
		}

		function clearAsyncState() {
			_showAsyncPromise = null;
			_showAsyncResolve = null;
		}

		// 可取消事件
		function createCancelableEvent(type) {
			var defaultPrevented = false;
			return {
				type: type,
				target: self,
				preventDefault: function () { defaultPrevented = true; },
				get defaultPrevented() { return defaultPrevented; }
			};
		}

		function raiseEvent(type, cancelable) {
			var eventObj = cancelable ? createCancelableEvent(type) : { type: type, target: self };
			self.dispatchEvent(type, eventObj);
			var handler = self["on" + type];
			if (typeof handler === "function") {
				handler.call(self, eventObj);
			}
			return eventObj;
		}

		// 命令列表事件
		this._commands.addEventListener("iteminserted", renderCommands);
		this._commands.addEventListener("itemremoved", renderCommands);
		this._commands.addEventListener("itemchanged", renderCommands);
		this._commands.addEventListener("reload", renderCommands);

		// 属性
		Object.defineProperty(this, "element", { get: function () { return container; }, enumerable: true });
		Object.defineProperty(this, "hidden", { get: function () { return container.classList.contains("hide"); }, enumerable: true });
		Object.defineProperty(this, "title", { get: function () { return title.textContent; }, set: function (v) { title.textContent = v; }, enumerable: true });
		Object.defineProperty(this, "content", {
			get: function () { return content.firstChild; },
			set: function (v) {
				if (typeof v === "string" || typeof v === "number") v = document.createTextNode(v);
				while (content.firstChild) content.removeChild(content.firstChild);
				content.appendChild(v);
			},
			enumerable: true
		});
		Object.defineProperty(this, "commands", { get: function () { return self._commands; }, enumerable: true });
		Object.defineProperty(this, "primaryCommandIndex", {
			get: function () {
				var btns = commandContainer.querySelectorAll("button");
				for (var i = 0; i < btns.length; i++) {
					if (btns[i].type === "submit") return i;
				}
				return -1;
			},
			set: function (value) {
				var btns = commandContainer.querySelectorAll("button");
				for (var i = 0; i < btns.length; i++) {
					btns[i].removeAttribute("type");
					if (i === value) btns[i].type = "submit";
				}
			},
			enumerable: true
		});
		this._darkMode = true;
		this._backgroundColor = null;
		this._foregroundColor = null;
		Object.defineProperty(this, "darkMode", {
			get: function () {
				return self._darkMode;
			},
			set: function (value) {
				self._darkMode = !!value;
				if (self._backgroundColor === null) {
					if (self._darkMode) {
						container.classList.add("win-ui-dark");
						container.classList.remove("win-ui-light");
					} else {
						container.classList.add("win-ui-light");
						container.classList.remove("win-ui-dark");
					}
				}
			},
			enumerable: true
		});
		Object.defineProperty(this, "backgroundColor", {
			get: function () {
				return self._backgroundColor;
			},
			set: function (value) {
				self._backgroundColor = value;
				var dialog = container.querySelector(".win-contentdialog-dialog");
				if (dialog) {
					if (value !== null) {
						dialog.style.backgroundColor = value;
					} else {
						// 还原暗/亮模式背景
						if (self._darkMode) {
							dialog.style.backgroundColor = "rgb(31, 0, 104)";
						} else {
							dialog.style.backgroundColor = "white";
						}
					}
				}
			},
			enumerable: true
		});
		Object.defineProperty(this, "foregroundColor", {
			get: function () {
				return self._foregroundColor;
			},
			set: function (value) {
				self._foregroundColor = value;
				var titleEl = container.querySelector(".win-contentdialog-title");
				var contentEl = container.querySelector(".win-contentdialog-content");
				if (titleEl) titleEl.style.color = value || "";
				if (contentEl) contentEl.style.color = value || "";
			},
			enumerable: true
		});
		container.classList.add("win-ui-dark");
		// 显示 / 隐藏
		this.show = function () {
			if (!container.classList.contains("hide")) return Promise.as();
			var ev = raiseEvent("beforeshow", true);
			if (ev.defaultPrevented) return Promise.as(); // 取消显示时直接返回
			container.classList.remove("hide");
			return new WinJS.Promise(function (complete) {
				setTimeout(function () {
					raiseEvent("aftershow", false);
					complete();
				}, 150);
			});
		};

		this.hide = function () {
			if (container.classList.contains("hide")) return Promise.as();
			var ev = raiseEvent("beforehide", true);
			if (ev.defaultPrevented) return Promise.as();
			container.classList.add("hide");
			return new WinJS.Promise(function (complete) {
				setTimeout(function () {
					raiseEvent("afterhide", false);
					complete();
				}, 150);
			});
		};

		// 释放资源
		this.dispose = function () {
			if (!_isdisposed) {
				_isdisposed = true;
				try { container.removeNode(false); } catch (e) { }
			}
		};

		// 事件回调
		this.onbeforeshow = null;
		this.onaftershow = null;
		this.onbeforehide = null;
		this.onafterhide = null;

		// showAsync
		this.showAsync = function () {
			if (_showAsyncPromise) return _showAsyncPromise;
			_showAsyncPromise = new WinJS.Promise(function (resolve) {
				_showAsyncResolve = resolve;
				self.show();
			});
			return _showAsyncPromise;
		};

		// 初始化 options
		if (options) {
			if (options.title !== undefined) this.title = options.title;
			if (options.content !== undefined) this.content = options.content;
			if (options.commands && options.commands.length) {
				options.commands.forEach(function (c) {
					if (c instanceof ContentDialogCommand) {
						self._commands.push(c);
					} else {
						self._commands.push(new ContentDialogCommand(c.label, c.handler, c.commandId));
					}
				});
				renderCommands();
			}
			if (typeof options.primaryCommandIndex === "number") this.primaryCommandIndex = options.primaryCommandIndex;
			if (typeof options.onbeforeshow === "function") this.onbeforeshow = options.onbeforeshow;
			if (typeof options.onaftershow === "function") this.onaftershow = options.onaftershow;
			if (typeof options.onbeforehide === "function") this.onbeforehide = options.onbeforehide;
			if (typeof options.onafterhide === "function") this.onafterhide = options.onafterhide;
			if (options.autoShow === true) this.show();
		}
	}

	// 快速创建 ContentDialog
	ContentDialog.create = function (content, title) {
		var container = document.createElement("div");
		document.body.appendChild(container);
		return new ContentDialog(container, {
			title: title,
			content: content
		});
	};

	global.WinJS.UI.ContentDialogCommand = ContentDialogCommand;
	global.WinJS.UI.ContentDialog = ContentDialog;

})(this);