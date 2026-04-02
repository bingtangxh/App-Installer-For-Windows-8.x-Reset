function IsBlackLabel(text) {
    if (text === null || text === undefined) return true;
    var trimmed = String(text).replace(/^\s+|\s+$/g, '');
    return trimmed === '';
}

var MBFLAGS = {
    MB_OK: 0x00000000,
    MB_OKCANCEL: 0x00000001,
    MB_ABORTRETRYIGNORE: 0x00000002,
    MB_YESNOCANCEL: 0x00000003,
    MB_YESNO: 0x00000004,
    MB_RETRYCANCEL: 0x00000005,
    MB_CANCELTRYCONTINUE: 0x00000006,
    MB_HELP: 0x00004000,
    MB_DEFBUTTON1: 0x00000000,
    MB_DEFBUTTON2: 0x00000100,
    MB_DEFBUTTON3: 0x00000200,
    MB_DEFBUTTON4: 0x00000300,
    MB_ICONERROR: 0x00000010,
    MB_ICONWARNING: 0x00000030,
    MB_ICONINFORMATION: 0x00000040
};

var MBRET = {
    IDOK: 1,
    IDCANCEL: 2,
    IDABORT: 3,
    IDRETRY: 4,
    IDIGNORE: 5,
    IDYES: 6,
    IDNO: 7,
    IDCLOSE: 8,
    IDHELP: 9,
    IDTRYAGAIN: 10,
    IDCONTINUE: 11
};

Object.freeze(MBFLAGS);
Object.freeze(MBRET);

// ==================== 资源加载部分（保持不变） ====================
(function(global) {
    try {
        var storage = Bridge.External.Storage;
        var path = storage.path;
        var root = path.getDir(path.program);
        var respath = path.combine(root, "reslib.dll");
        var res = Bridge.Resources;
        global.respath = respath;
        global.getPublicRes = function(resId) {
            return res.fromfile(respath, resId);
        }
    } catch (e) {}
})(this);

function GetLocaleStringFromResId(resId) {
    try { return getPublicRes(resId); } catch (e) { return ""; }
}

// ==================== 全局结果存储 ====================
var msgboxResult = {};

function ClearAllMessageBoxResults() {
    msgboxResult = {};
}

// ==================== 辅助函数 ====================
var _msgboxIdCounter = 0;

function _generateMsgBoxId() {
    return "msgbox_" + new Date().getTime() + "_" + (++_msgboxIdCounter);
}

// 根据 MBFLAGS 生成命令列表（返回 { commands, defaultIndex }）
function _buildCommandsFromFlags(uType) {
    var commands = [];
    var baseType = uType & 0xF;
    var defaultIndex = 0;
    var hasHelp = (uType & MBFLAGS.MB_HELP) === MBFLAGS.MB_HELP;

    // 辅助添加命令
    function addCommand(resId, returnValue) {
        var label = GetLocaleStringFromResId(resId);
        if (!label) label = "Button";
        commands.push({ label: label, value: returnValue });
    }

    // 先处理帮助按钮（原实现中帮助按钮总是在最前面）
    if (hasHelp) {
        addCommand(808, MBRET.IDHELP);
    }

    // 根据基础类型添加按钮
    switch (baseType) {
        case MBFLAGS.MB_OK:
            addCommand(800, MBRET.IDOK);
            break;
        case MBFLAGS.MB_OKCANCEL:
            addCommand(800, MBRET.IDOK);
            addCommand(801, MBRET.IDCANCEL);
            break;
        case MBFLAGS.MB_ABORTRETRYIGNORE:
            addCommand(802, MBRET.IDABORT);
            addCommand(803, MBRET.IDRETRY);
            addCommand(804, MBRET.IDIGNORE);
            break;
        case MBFLAGS.MB_YESNOCANCEL:
            addCommand(805, MBRET.IDYES);
            addCommand(806, MBRET.IDNO);
            addCommand(801, MBRET.IDCANCEL);
            break;
        case MBFLAGS.MB_YESNO:
            addCommand(805, MBRET.IDYES);
            addCommand(806, MBRET.IDNO);
            break;
        case MBFLAGS.MB_RETRYCANCEL:
            addCommand(803, MBRET.IDRETRY);
            addCommand(801, MBRET.IDCANCEL);
            break;
        case MBFLAGS.MB_CANCELTRYCONTINUE:
            addCommand(801, MBRET.IDCANCEL);
            addCommand(803, MBRET.IDRETRY);
            addCommand(810, MBRET.IDCONTINUE);
            break;
        default:
            addCommand(800, MBRET.IDOK);
            break;
    }

    // 确定默认按钮索引（考虑帮助按钮占位）
    var defFlag = uType & 0x300; // MB_DEFBUTTON1~4
    var defButtonNumber = 0;
    if (defFlag === MBFLAGS.MB_DEFBUTTON2) defButtonNumber = 1;
    else if (defFlag === MBFLAGS.MB_DEFBUTTON3) defButtonNumber = 2;
    else if (defFlag === MBFLAGS.MB_DEFBUTTON4) defButtonNumber = 3;
    else defButtonNumber = 0;

    // 默认索引需要加上帮助按钮的偏移
    var finalDefaultIndex = (hasHelp ? 1 : 0) + defButtonNumber;
    if (finalDefaultIndex >= commands.length) finalDefaultIndex = commands.length - 1;
    if (finalDefaultIndex < 0) finalDefaultIndex = 0;

    return { commands: commands, defaultIndex: finalDefaultIndex };
}

// 创建 ContentDialog 并应用公共属性
function _createContentDialog(title, content, bgColor, commands, defaultIndex, callback) {
    var container = document.createElement("div");
    document.body.appendChild(container);

    var dialog = new WinJS.UI.ContentDialog(container, {
        //title: typeof title === "string" ? title : "",
        //content: typeof content === "string" ? content : "",
        autoShow: false
    });
    dialog.title = title;
    dialog.content = content;
    // 处理标题为 HTMLElement
    if (title instanceof HTMLElement) {
        var titleContainer = container.querySelector(".win-contentdialog-title");
        if (titleContainer) {
            while (titleContainer.firstChild) titleContainer.removeChild(titleContainer.firstChild);
            titleContainer.appendChild(title);
        }
    }

    // 处理内容为 HTMLElement
    if (content instanceof HTMLElement) {
        var contentContainer = container.querySelector(".win-contentdialog-content");
        if (contentContainer) {
            while (contentContainer.firstChild) contentContainer.removeChild(contentContainer.firstChild);
            contentContainer.appendChild(content);
        }
    }

    // 背景色
    if (!IsBlackLabel(bgColor)) {
        dialog.backgroundColor = bgColor;
    }

    // 添加命令
    for (var i = 0; i < commands.length; i++) {
        var cmd = commands[i];
        var dialogCmd = new WinJS.UI.ContentDialogCommand(cmd.label, function(evt) {
            // 阻止多次点击
            if (dialog._isClosing) return;
            dialog._isClosing = true;

            // 调用回调传递返回值
            if (callback) callback(cmd.value);

            // 关闭并移除对话框
            dialog.hide().then(function() {
                try { document.body.removeChild(container); } catch (e) {}
                dialog._isClosing = false;
            });

            // 返回 false 会阻止关闭，但我们手动关闭，所以返回 undefined 即可
        }, cmd.value.toString());
        dialog.commands.push(dialogCmd);
    }

    // 设置默认按钮
    if (defaultIndex >= 0 && defaultIndex < dialog.commands.length) {
        dialog.primaryCommandIndex = defaultIndex;
    }

    return dialog;
}

// ==================== 核心 API 实现 ====================

// 新版 MessageBoxForJS（完全基于 ContentDialog）
function MessageBoxForJS(_lpText, _lpCaption, _uType, _objColor, _callback) {
    var dialogId = _generateMsgBoxId();
    var commandsInfo = _buildCommandsFromFlags(_uType);

    var dialog = _createContentDialog(
        _lpCaption,
        _lpText,
        _objColor,
        commandsInfo.commands,
        commandsInfo.defaultIndex,
        function(returnValue) {
            if (_callback) _callback(returnValue);
        }
    );

    // 存储 dialog 引用以便可能的后续操作（如 GetMessageBoxResult 不需要，但保留）
    if (!window._activeDialogs) window._activeDialogs = {};
    window._activeDialogs[dialogId] = dialog;
    dialog.element.id = dialogId;
    // 显示对话框
    dialog.show().then(null, function(err) { console.error("ContentDialog show error:", err); });

    return dialogId;
}

// MessageBox：返回 ID，结果通过 GetMessageBoxResult 获取
function MessageBox(_lpText, _lpCaption, _uType, _objColor) {
    var msgboxId = _generateMsgBoxId();
    MessageBoxForJS(_lpText, _lpCaption, _uType, _objColor, function(valueReturn) {
        msgboxResult[msgboxId] = valueReturn;
        if (window._activeDialogs) delete window._activeDialogs[msgboxId];
    });
    return msgboxId;
}

function GetMessageBoxResult(msgboxId) {
    var result = msgboxResult[msgboxId];
    if (result === undefined || result === null) return null;
    msgboxResult[msgboxId] = null;
    return result;
}

// 异步版本（返回 Promise）
function messageBoxAsync(swText, swTitle, uType, swColor, pfCallback) {
    if (typeof Promise === "undefined") {
        console.error("Promise is not supported in this environment.");
        MessageBoxForJS(swText, swTitle, uType, swColor, pfCallback);
        return null;
    }
    return new Promise(function(resolve, reject) {
        try {
            MessageBoxForJS(swText, swTitle, uType, swColor, function(valueReturn) {
                if (pfCallback) pfCallback(valueReturn);
                resolve(valueReturn);
            });
        } catch (ex) {
            reject(ex);
        }
    });
}

// MessageBoxButton 类（保持不变）
function MessageBoxButton(swDisplayName, nValueReturn) {
    this.displayName = swDisplayName;
    this.value = nValueReturn;
}

// 高级自定义按钮对话框
function messageBoxAdvance(swText, swCaption, aCommands, swColor, pfCallback) {
    var dialogId = _generateMsgBoxId();

    // 转换 aCommands 为内部格式
    var commands = [];
    for (var i = 0; i < aCommands.length; i++) {
        var cmd = aCommands[i];
        commands.push({
            label: cmd.displayName,
            value: cmd.value
        });
    }
    if (commands.length === 0) {
        commands.push({ label: GetLocaleStringFromResId(800) || "OK", value: MBRET.IDOK });
    }

    var dialog = _createContentDialog(
        swCaption,
        swText,
        swColor,
        commands,
        0, // 默认第一个按钮为默认
        function(returnValue) {
            if (pfCallback) pfCallback(returnValue);
        }
    );
    dialog.element.id = dialogId;
    if (!window._activeDialogs) window._activeDialogs = {};
    window._activeDialogs[dialogId] = dialog;

    dialog.show();
    return dialogId;
}

function messageBoxAdvanceAsync(swText, swCaption, aCommands, swColor) {
    if (typeof Promise === "undefined") {
        console.error("Promise is not supported in this environment.");
        messageBoxAdvance(swText, swCaption, aCommands, swColor);
        return null;
    }
    return new Promise(function(resolve, reject) {
        try {
            messageBoxAdvance(swText, swCaption, aCommands, swColor, function(valueReturn) {
                resolve(valueReturn);
            });
        } catch (ex) {
            reject(ex);
        }
    });
}

// ==================== MsgBoxObj 类（保持原接口，内部已适配） ====================
function MsgBoxObj() {
    this.elementId = "";
    this.callback = null;
    this.type = MBFLAGS.MB_OK;
    this._boundCallback = this._internalCallback.bind(this);
    this._text = "";
    this._title = "";
    this._color = "#0078d7";

    this.show = function() {
        var self = this;
        this.elementId = MessageBoxForJS(
            this._text,
            this._title,
            this.type,
            this._color,
            function(valueReturn) {
                self._boundCallback(valueReturn);
            }
        );
        setTimeout(function() {
            var element = document.getElementById(self.elementId);
            if (element) {
                var bodyElement = element.querySelector(".notice-body");
                if (bodyElement) {
                    bodyElement.style.transition = "all 0.5s linear";
                }
            }
        }, 100);
    };

    Object.defineProperty(this, "text", {
        get: function() { return this._text; },
        set: function(value) {
            this._text = value;
            if (this.elementId) {
                var element = document.getElementById(this.elementId);
                if (element) {
                    var textElement = element.querySelector(".notice-text");
                    if (textElement) {
                        if (value instanceof HTMLElement) {
                            value.classList.add("notice-text");
                            textElement.parentNode.replaceChild(value, textElement);
                        } else {
                            textElement.innerHTML = value;
                        }
                    }
                }
            }
        }
    });

    Object.defineProperty(this, "title", {
        get: function() { return this._title; },
        set: function(value) {
            this._title = value;
            if (this.elementId) {
                var element = document.getElementById(this.elementId);
                if (element) {
                    var titleElement = element.querySelector(".notice-title");
                    if (titleElement) {
                        titleElement.innerHTML = value;
                    }
                }
            }
        }
    });

    Object.defineProperty(this, "color", {
        get: function() { return this._color; },
        set: function(value) {
            this._color = value;
            if (this.elementId) {
                var element = document.getElementById(this.elementId);
                if (element) {
                    var bodyElement = element.querySelector(".notice-body");
                    if (bodyElement) {
                        bodyElement.style.backgroundColor = value;
                    }
                }
            }
        }
    });

    this.getElement = function() {
        return document.getElementById(this.elementId);
    };
}

MsgBoxObj.prototype._internalCallback = function(returnValue) {
    if (typeof this.callback === "function") {
        this.callback(returnValue);
    }
};