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

function GetLocaleStringFromResId(resId) { try { return getPublicRes(resId); } catch (e) {} }
var msgboxResult = {};

function MessageBox(_lpText, _lpCaption, _uType, _objColor) {
    var id = MessageBoxForJS(_lpText, _lpCaption, _uType, _objColor, function(valueReturn) {
        getRes = valueReturn;
        msgboxResult[id] = valueReturn;
    });
    return id;
}

function GetMessageBoxResult(msgboxId) {
    var result = msgboxResult[msgboxId];
    if (result === undefined || result === null) return null;
    msgboxResult[msgboxId] = null;
    return result;
}

function ClearAllMessageBoxResults() {
    msgboxResult = null;
    msgboxResult = {};
}

// 注意：callback 函数无返回值，传入参数：整数型 按下的按钮值。
/*
    使用示例：
    MessageBoxForJS("请选择一个按钮", "", MBFLAGS.MB_OKCANCEL, "#0078d7", function(value) {
        console.log("MessageBoxForJS callback value: " + value);
    })
*/
function MessageBoxForJS(_lpText, _lpCaption, _uType, _objColor, _callback) {
    var msgbox = document.createElement("div");
    msgbox.classList.add("notice-back");
    msgbox.classList.add("win-ui-dark");
    var uniqueId = "msgbox_" + new Date().getTime();
    msgbox.id = uniqueId;
    var msgbody = document.createElement("div");
    msgbody.classList.add("notice-body");
    if (!IsBlackLabel(_objColor)) {
        msgbody.style.backgroundColor = _objColor;
    }
    msgbox.appendChild(msgbody);
    var msgcontainter = document.createElement("div");
    msgcontainter.style.height = "100%";
    msgcontainter.style.width = "100%";
    msgcontainter.style.maxHeight = "100%";
    msgcontainter.style.minHeight = "0px";
    msgcontainter.style.boxSizing = "border-box";
    msgbody.appendChild(msgcontainter);
    var msgcaption = document.createElement("div");
    msgcontainter.appendChild(msgcaption);
    msgcontainter.style.display = "flex";
    msgcontainter.style.flexDirection = "column";
    var msgcontent = document.createElement("div");
    msgcontent.style.flex = "1 1 auto";
    msgcontent.style.marginRight = "3px";
    msgcontent.style.overflowX = "hidden";
    msgcontent.style.overflowY = "auto";
    msgcontent.style.minHeight = "0px";
    msgcontainter.appendChild(msgcontent);
    if (_lpCaption instanceof HTMLElement) {
        msgcaption.appendChild(_lpCaption);
        msgcaption.classList.add("notice-title");
    } else {
        if (!IsBlackLabel(_lpCaption)) {
            var msgtitle = document.createElement("h2");
            msgtitle.textContent = _lpCaption;
            msgtitle.classList.add("notice-title");
            msgcaption.appendChild(msgtitle);
        } else {
            var msgtitle = document.createElement("h2");
            msgtitle.textContent = "";
            msgtitle.classList.add("notice-title");
            msgcaption.appendChild(msgtitle);
        }
    }
    if (_lpText instanceof HTMLElement || _lpText instanceof HTMLDivElement || typeof _lpText !== "string") {
        try {
            _lpText.classList.add("notice-text");
            msgcontent.appendChild(_lpText);
        } catch (e) {
            if (!IsBlackLabel(_lpText)) {
                var msgtext = document.createElement("p");
                msgtext.textContent = _lpText;
                msgtext.classList.add("notice-text");
                if (IsBlackLabel(_lpCaption)) {
                    msgtext.style.marginTop = "0";
                }
                msgcontent.appendChild(msgtext);
            } else {
                var msgtext = document.createElement("p");
                msgtext.innerText = "";
                msgtext.classList.add("notice-text");
                if (IsBlackLabel(_lpCaption)) {
                    msgtext.style.marginTop = "0";
                }
                msgcontent.appendChild(msgtext);
            }
        }
    } else {
        if (!IsBlackLabel(_lpText)) {
            var msgtext = document.createElement("p");
            msgtext.textContent = _lpText;
            msgtext.classList.add("notice-text");
            if (IsBlackLabel(_lpCaption)) {
                msgtext.style.marginTop = "0";
            }
            msgcontent.appendChild(msgtext);
        } else {
            var msgtext = document.createElement("p");
            msgtext.innerText = "";
            msgtext.classList.add("notice-text");
            if (IsBlackLabel(_lpCaption)) {
                msgtext.style.marginTop = "0";
            }
            msgcontent.appendChild(msgtext);
        }
    }
    var msgctrls = document.createElement("div");
    msgctrls.classList.add("notice-controls");
    msgcontainter.appendChild(msgctrls);
    var cnt = 0;
    var cbFuncPress = function(valueReturn) {
        getRes = valueReturn;
        msgbox.style.opacity = 0;
        setTimeout(function() {
            document.body.removeChild(msgbox);
        }, 500);
        if (_callback) {
            _callback(valueReturn);
        }
    };
    var pfCreateButton = function(displayNameResId, valueReturn) {
        var btn = document.createElement("button");
        btn.innerHTML = GetLocaleStringFromResId(displayNameResId);
        btn.classList.add("notice-btn");
        btn.addEventListener("click", function() {
            cbFuncPress(valueReturn);
        });
        msgctrls.appendChild(btn);
    };
    if ((_uType & MBFLAGS.MB_HELP) === MBFLAGS.MB_HELP) {
        pfCreateButton(808, MBRET.IDHELP);
    }
    for (cnt = 0; cnt <= MBFLAGS.MB_RETRYCANCEL; cnt++) {
        if ((_uType & 0xF) === cnt) {
            switch (cnt) {
                case MBFLAGS.MB_OK:
                    {
                        pfCreateButton(800, MBRET.IDOK);
                    }
                    break;
                case MBFLAGS.MB_OKCANCEL:
                    {
                        pfCreateButton(800, MBRET.IDOK);
                        pfCreateButton(801, MBRET.IDCANCEL);
                    }
                    break;
                case MBFLAGS.MB_ABORTRETRYIGNORE:
                    {
                        pfCreateButton(802, MBRET.IDABORT);
                        pfCreateButton(803, MBRET.IDRETRY);
                        pfCreateButton(804, MBRET.IDIGNORE);
                    }
                    break;
                case MBFLAGS.MB_YESNOCANCEL:
                    {
                        pfCreateButton(805, MBRET.IDYES);
                        pfCreateButton(806, MBRET.IDNO);
                        pfCreateButton(801, MBRET.IDCANCEL);
                    }
                    break;
                case MBFLAGS.MB_YESNO:
                    {
                        pfCreateButton(805, MBRET.IDYES);
                        pfCreateButton(806, MBRET.IDNO);
                    }
                    break;
                case MBFLAGS.MB_RETRYCANCEL:
                    {
                        pfCreateButton(803, MBRET.IDRETRY);
                        pfCreateButton(801, MBRET.IDCANCEL);
                    }
                    break;
                case MBFLAGS.MB_CANCELTRYCONTINUE:
                    {
                        pfCreateButton(801, MBRET.IDCANCEL);
                        pfCreateButton(803, MBRET.IDRETRY);
                        pfCreateButton(810, MBRET.IDCONTINUE);
                    }
                    break;
            }
        }
    }
    var btns = msgctrls.querySelectorAll("button");
    var defaultBtnCnt = 0;
    if ((_uType & MBFLAGS.MB_DEFBUTTON1) === MBFLAGS.MB_DEFBUTTON1) defaultBtnCnt = 0;
    if ((_uType & MBFLAGS.MB_DEFBUTTON2) === MBFLAGS.MB_DEFBUTTON2) defaultBtnCnt = 1;
    if ((_uType & MBFLAGS.MB_DEFBUTTON3) === MBFLAGS.MB_DEFBUTTON3) defaultBtnCnt = 2;
    if ((_uType & MBFLAGS.MB_DEFBUTTON4) === MBFLAGS.MB_DEFBUTTON4) defaultBtnCnt = 3;
    for (cnt = 0; cnt < btns.length; cnt++) {
        if (cnt === defaultBtnCnt) {
            btns[cnt].focus();
            break;
        }
    }
    document.body.appendChild(msgbox);
    setTimeout(function() {
        msgbox.style.opacity = 1;
    }, 1);
    return msgbox.id;
}

function MsgBoxObj() {
    this.elementId = "";
    this.callback = null;
    this.type = MBFLAGS.MB_OK;
    this._boundCallback = this._internalCallback.bind(this);
    this._text = "";
    this._title = "";
    this._color = "#0078d7";
    this.show = function() {
        this.elementId = MessageBoxForJS(
            this._text,
            this._title,
            this.type,
            this._color,
            this._boundCallback
        );
        setTimeout(function() {
            var element = document.getElementById(this.elementId);
            if (element) {
                var bodyElement = element.querySelector(".notice-body");
                if (bodyElement) {
                    bodyElement.style.transition = "all 0.5s linear";
                }
            }
        }.bind(this), 100);
    }
    Object.defineProperty(this, "text", {
        get: function() {
            return this._text;
        },
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
        get: function() {
            return this._title;
        },
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
        get: function() {
            return this._color;
        },
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
/**
 * 异步显示消息框，返回一个 Promise 对象。
 * @param {string | HTMLElement} swText 内容
 * @param {string} swTitle 标题
 * @param {MBFLAGS} uType 标志，使用 MBFLAGS 常量
 * @param {string} swColor 背景颜色文本。
 * @returns {Promise}
 */
function messageBoxAsync(swText, swTitle, uType, swColor, pfCallback) {
    if (typeof Promise === "undefined") {
        console.error("Promise is not supported in this environment.");
        MessageBoxForJS(swText, swTitle, uType, swColor, pfCallback);
    }
    return new Promise(function(resolve, reject) {
        try {
            MessageBoxForJS(swText, swTitle, uType, swColor, function(valueReturn) {
                if (resolve) resolve(valueReturn);
            });
        } catch (ex) {
            if (reject) reject(ex);
        }
    });
}

function MessageBoxButton(swDisplayName, nValueReturn) {
    this.displayName = swDisplayName;
    this.value = nValueReturn;
}

function messageBoxAdvance(swText, swCaption, aCommands, swColor, pfCallback) {
    var _lpText = swText;
    var _lpCaption = swCaption;
    var msgbox = document.createElement("div");
    msgbox.classList.add("notice-back");
    msgbox.classList.add("win-ui-dark");
    var uniqueId = "msgbox_" + new Date().getTime();
    msgbox.id = uniqueId;
    var msgbody = document.createElement("div");
    msgbody.classList.add("notice-body");
    if (!IsBlackLabel(swColor)) {
        msgbody.style.backgroundColor = swColor;
    }
    msgbox.appendChild(msgbody);
    var msgcontainter = document.createElement("div");
    msgcontainter.style.height = "100%";
    msgcontainter.style.width = "100%";
    msgcontainter.style.maxHeight = "100%";
    msgcontainter.style.minHeight = "0px";
    msgcontainter.style.boxSizing = "border-box";
    msgbody.appendChild(msgcontainter);
    var msgcaption = document.createElement("div");
    msgcontainter.appendChild(msgcaption);
    msgcontainter.style.display = "flex";
    msgcontainter.style.flexDirection = "column";
    var msgcontent = document.createElement("div");
    msgcontent.style.flex = "1 1 auto";
    msgcontent.style.marginRight = "3px";
    msgcontent.style.overflowX = "hidden";
    msgcontent.style.overflowY = "auto";
    msgcontent.style.minHeight = "0px";
    msgcontainter.appendChild(msgcontent);
    if (_lpCaption instanceof HTMLElement) {
        msgcaption.appendChild(_lpCaption);
        msgcaption.classList.add("notice-title");
    } else {
        if (!IsBlackLabel(_lpCaption)) {
            var msgtitle = document.createElement("h2");
            msgtitle.textContent = _lpCaption;
            msgtitle.classList.add("notice-title");
            msgcaption.appendChild(msgtitle);
        } else {
            var msgtitle = document.createElement("h2");
            msgtitle.textContent = "";
            msgtitle.classList.add("notice-title");
            msgcaption.appendChild(msgtitle);
        }
    }
    if (_lpText instanceof HTMLElement || _lpText instanceof HTMLDivElement || typeof _lpText !== "string") {
        try {
            _lpText.classList.add("notice-text");
            msgcontent.appendChild(_lpText);
        } catch (e) {
            if (!IsBlackLabel(_lpText)) {
                var msgtext = document.createElement("p");
                msgtext.textContent = _lpText;
                msgtext.classList.add("notice-text");
                if (IsBlackLabel(_lpCaption)) {
                    msgtext.style.marginTop = "0";
                }
                msgcontent.appendChild(msgtext);
            } else {
                var msgtext = document.createElement("p");
                msgtext.innerText = "";
                msgtext.classList.add("notice-text");
                if (IsBlackLabel(_lpCaption)) {
                    msgtext.style.marginTop = "0";
                }
                msgcontent.appendChild(msgtext);
            }
        }
    } else {
        if (!IsBlackLabel(_lpText)) {
            var msgtext = document.createElement("p");
            msgtext.textContent = _lpText;
            msgtext.classList.add("notice-text");
            if (IsBlackLabel(_lpCaption)) {
                msgtext.style.marginTop = "0";
            }
            msgcontent.appendChild(msgtext);
        } else {
            var msgtext = document.createElement("p");
            msgtext.innerText = "";
            msgtext.classList.add("notice-text");
            if (IsBlackLabel(_lpCaption)) {
                msgtext.style.marginTop = "0";
            }
            msgcontent.appendChild(msgtext);
        }
    }
    var msgctrls = document.createElement("div");
    msgctrls.classList.add("notice-controls");
    msgcontainter.appendChild(msgctrls);
    if (aCommands.length <= 0) {
        aCommands.push(new MessageBoxButton(GetLocaleStringFromResId(800) || "OK", MBRET.IDOK));
    }
    for (var i = 0; i < aCommands.length; i++) {
        var cmd = aCommands[i];
        var btn = document.createElement("button");
        btn.textContent = cmd.displayName;
        btn.setAttribute("data-msgbox-value", cmd.value);
        Windows.UI.Event.Util.addEvent(btn, "click", function(event) {
            var btns = this.parentNode.querySelectorAll("button");
            var lastbtnstatus = [];
            for (var j = 0; j < btns.length; j++) {
                lastbtnstatus.push(btns[j].disabled);
                btns[j].disabled = true;
            }
            try {
                pfCallback(this.getAttribute("data-msgbox-value"));
            } catch (e) {}
            msgbox.style.opacity = 0;
            setTimeout(function(nodes, laststatus) {
                for (var k = 0; k < nodes.length; k++) {
                    nodes[k].disabled = laststatus[k];
                }
                document.body.removeChild(msgbox);
            }, 500, btns, lastbtnstatus);
        });
        msgctrls.appendChild(btn);
    }
    document.body.appendChild(msgbox);
    setTimeout(function() {
        msgbox.style.opacity = 1;
    }, 1);
    return msgbox.id;
}

function messageBoxAdvanceAsync(swText, swCaption, aCommands, swColor) {
    if (typeof Promise === "undefined") {
        console.error("Promise is not supported in this environment.");
        messageBoxAdvance(swText, swCaption, aCommands, swColor);
    }
    return new Promise(function(resolve, reject) {
        try {
            messageBoxAdvance(swText, swCaption, aCommands, swColor, function(valueReturn) {
                if (resolve) resolve(valueReturn);
            });
        } catch (ex) {
            if (reject) reject(ex);
        }
    });
}