mergeInto(LibraryManager.library, {

    // 画面上に固定オーバーレイでログを表示
    JsLog: function(strPtr) {
        var str = UTF8ToString(strPtr);
        var el = document.getElementById('unity-dbg-overlay');
        if (!el) {
            el = document.createElement('div');
            el.id = 'unity-dbg-overlay';
            document.body.appendChild(el);
        }
        // display:none を解除して必ず表示
        el.style.display = 'block';
        var line = document.createElement('div');
        var ts = (performance.now()/1000).toFixed(2);
        line.textContent = '[' + ts + 's] ' + str;
        el.appendChild(line);
        el.scrollTop = el.scrollHeight;
        console.log('[Unity] ' + str);
    },

    // 致命的エラー用 alert
    JsAlert: function(strPtr) {
        var str = UTF8ToString(strPtr);
        console.error('[Unity FATAL] ' + str);
        alert('[Unity Error]\n' + str);
    }

});
