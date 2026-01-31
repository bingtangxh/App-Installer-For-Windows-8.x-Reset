(function(global) {
    "use strict";

    /**
     * TransitionPanel
     * axis: 'x' | 'y' | 'both'
     * speed: 'fast' | 'medium' | 'slow'
     * el: DOM 元素
     */
    function TransitionPanel(el, options) {
        if (!el) throw new Error("TransitionPanel requires a DOM element.");

        this.el = el;
        this.opts = options || {};
        this.axis = this.opts.axis || 'y';
        this.speed = this.opts.speed || 'medium';

        // 初始化状态
        this._shown = false;
        this._events = {};

        // 确保基础类 statusbar
        if (!el.classList.contains('statusbar')) el.classList.add('statusbar');

        // 确保 axis 类存在（x/y/both）
        if (this.axis === 'x' && !el.classList.contains('x')) el.classList.add('x');
        else if (this.axis === 'y' && !el.classList.contains('y')) el.classList.add('y');
        else if (this.axis === 'both' && !el.classList.contains('both')) el.classList.add('both');

        // 添加速度类
        if (this.speed === 'fast') el.classList.add('fast');
        else if (this.speed === 'medium') el.classList.add('medium');
        else el.classList.add('slow');

        // 内容变化自动刷新
        this._bindContentChange();
    }

    function maxWidth(el) {
        var cw = 0;
        var ow = 0;
        var rw = 0;
        var sw = 0;
        try { cw = el.clientWidth; } catch (e) {}
        try { ow = el.offsetWidth; } catch (e) {}
        try { rw = el.getBoundingClientRect().width; } catch (e) {}
        try { sw = el.scrollWidth; } catch (e) {}
        return Math.max(cw, ow, rw, sw);
    }

    function maxHeight(el) {
        var ch = 0;
        var oh = 0;
        var rh = 0;
        var sh = 0;
        try { ch = el.clientHeight; } catch (e) {}
        try { oh = el.offsetHeight; } catch (e) {}
        try { rh = el.getBoundingClientRect().height; } catch (e) {}
        try { sh = el.scrollHeight; } catch (e) {}
        return Math.max(ch, oh, rh, sh);
    }
    // 显示
    TransitionPanel.prototype.show = function() {
        if (this._shown) return;
        this._emit('beforeshow');
        this._shown = true;

        var el = this.el;

        setTimeout(function() {
            this._emit('show');

            if (this.axis !== 'x') el.style.height = maxHeight(el) + 'px';
            if (this.axis !== 'y') el.style.width = maxWidth(el) + 'px';
            if (this.axis === 'both') {
                el.style.height = maxHeight(el) + 'px';
                el.style.width = maxWidth(el) + 'px';
            }
            this._afterTransition('aftershow');
        }.bind(this), 16);
    };

    // 隐藏
    TransitionPanel.prototype.hide = function() {
        if (!this._shown) return;
        this._emit('beforehide');
        this._shown = false;

        var el = this.el;

        // 锁定当前尺寸
        if (this.axis !== 'x') el.style.height = maxHeight(el) + 'px';
        if (this.axis !== 'y') el.style.width = maxWidth(el) + 'px';
        if (this.axis === 'both') {
            el.style.height = maxHeight(el) + 'px';
            el.style.width = maxWidth(el) + 'px';
        }
        setTimeout(function() {
            this._emit('hide');

            // 回到折叠状态尺寸（依赖 x/y/both 类）
            if (this.axis !== 'x') el.style.height = '';
            if (this.axis !== 'y') el.style.width = '';
            if (this.axis === 'both') {
                el.style.height = '';
                el.style.width = '';
            }
            this._afterTransition('afterhide');
        }.bind(this), 16);
    };

    // 刷新尺寸（显示中）
    TransitionPanel.prototype.refresh = function() {
        if (!this._shown) return;
        var el = this.el;
        if (this.axis !== 'x') el.style.height = el.scrollHeight + 'px';
        if (this.axis !== 'y') el.style.width = el.scrollWidth + 'px';
    };

    // 内容变化自动刷新
    TransitionPanel.prototype._bindContentChange = function() {
        if (!global.setTextChangeEvent) return;
        var self = this;
        global.setTextChangeEvent(this.el, function() {
            if (self._shown) self.refresh();
        });
    };

    // transitionend 回调处理
    TransitionPanel.prototype._afterTransition = function(evt) {
        var el = this.el;
        var called = false;
        var duration = this.speed === 'fast' ? 300 : (this.speed === 'medium' ? 500 : 700);

        function done() {
            if (called) return;
            called = true;
            el.removeEventListener('transitionend', done);
            if (evt) this._emit(evt);
        }
        el.addEventListener('transitionend', done.bind(this));
        setTimeout(done.bind(this), duration + 30);
    };

    // 生命周期事件绑定
    TransitionPanel.prototype.on = function(name, fn) {
        (this._events[name] || (this._events[name] = [])).push(fn);
    };

    // 事件触发
    TransitionPanel.prototype._emit = function(name) {
        var list = this._events[name];
        if (!list) return;
        for (var i = 0; i < list.length; i++) {
            try { list[i].call(this); } catch (e) { console.error(e); }
        }
    };

    // 只读属性 shown
    Object.defineProperty(TransitionPanel.prototype, 'shown', {
        get: function() { return this._shown; }
    });

    // 销毁
    TransitionPanel.prototype.dispose = function() {
        // 移除所有事件回调
        this._events = {};
        // 清理 el 内联样式
        if (this.el) {
            this.el.style.height = '';
            this.el.style.width = '';
        }
        this._shown = false;
        // 可选：删除内容变化监听（如果使用全局 setTextChangeEvent）
        if (global.Windows && global.Windows.UI && global.Windows.UI.Event && global.Windows.UI.Event.Monitor) {
            // 这里可以 detach 所有回调
            // 视具体实现可扩展
        }
    };

    // 全局暴露
    global.TransitionPanel = TransitionPanel;

})(this);