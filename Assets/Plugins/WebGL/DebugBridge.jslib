mergeInto(LibraryManager.library, {

    // 画面上に固定オーバーレイでログを表示
    JsLog: function(strPtr) {
        var str = UTF8ToString(strPtr);
        var el = document.getElementById('unity-dbg-overlay');
        if (!el) {
            el = document.createElement('div');
            el.id = 'unity-dbg-overlay';
            el.style.cssText = [
                'position:fixed',
                'top:0','left:0','right:0',
                'max-height:45vh',
                'overflow-y:auto',
                'background:rgba(0,0,0,0.82)',
                'color:#39ff14',
                'font:12px/1.4 monospace',
                'padding:6px 8px',
                'z-index:999999',
                'pointer-events:none',
                'white-space:pre-wrap',
                'word-break:break-all'
            ].join(';');
            document.body.appendChild(el);
        }
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
