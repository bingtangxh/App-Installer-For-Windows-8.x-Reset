(function() {
    "use strict";

    function getAllNodesHasResource() {
        var allElements = document.body.getElementsByTagName('*');
        var result = [];
        for (var i = 0; i < allElements.length; i++) {
            var el = allElements[i];
            var tag = el.tagName.toLowerCase();
            if (tag === 'script' || tag === 'style' || tag === 'link') {
                continue;
            }
            var byName = el.getAttribute('data-res-byname');
            var byId = el.getAttribute('data-res-byid');
            var fromFile = el.getAttribute('data-res-fromfile');
            if ((byName && !Bridge.NString.empty(byName)) || (byId && parseInt(byId, 10) > 0) || (fromFile && !Bridge.NString.empty(fromFile))) {
                result.push(el);
            }
        }

        return result; // 返回符合条件的元素数组
    }
    module.exports = {
        getFileResPair: function(filepath, resid) {
            return {
                filepath: filepath,
                resid: resid
            };
        }
    };
    module.exports = {
        Resources: {
            processAll: function() {
                var nodes = getAllNodesHasResource();
                var resources = Bridge.Resources;
                for (var i = 0; i < nodes.length; i++) {
                    if (nodes[i].hasAttribute('data-res-byname')) {
                        var resName = nodes[i].getAttribute('data-res-byname');
                        try { nodes[i].textContent = Bridge.Resources.byname(resName); } catch (e) {}
                    } else if (nodes[i].hasAttribute('data-res-byid')) {
                        var resId = parseInt(nodes[i].getAttribute('data-res-byid'), 10);
                        try { nodes[i].textContent = Bridge.Resources.byid(resId); } catch (e) {}
                    } else if (nodes[i].hasAttribute('data-res-fromfile')) {
                        try {
                            var obj = eval(nodes[i].getAttribute('data-res-fromfile'));
                            nodes[i].textContent = resources.fromfile(obj.filepath, obj.resid);
                        } catch (e) {
                            nodes[i].textContent = "";
                        }
                    } else {
                        nodes[i].textContent = "";
                    }
                }
            }
        }
    };
})();