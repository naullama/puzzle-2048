using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[UnityEngine.Scripting.Preserve]
public class GameBootstrap : MonoBehaviour
{
    // ────────── 定数 ──────────────────────────────────
    const int   SIZE = 4;
    const int   WIN  = 2048;
    const float CELL = 100f;
    const float GAP  = 10f;

    // ────────── 状態 ──────────────────────────────────
    int[,] board = new int[SIZE, SIZE];
    int    score, best;
    bool   gameEnded, won;
    bool   uiReady;

    // ────────── UI 参照 ───────────────────────────────
    Text     scoreVal, bestVal;
    Image[]  cellBg  = new Image[SIZE * SIZE];
    Text[]   cellTxt = new Text[SIZE * SIZE];
    GameObject goPanel, winPanel;

    // ────────── デバッグ ──────────────────────────────
    string status = "Initializing...";

    // ────────── カラー ────────────────────────────────
    static Color Hex(string h) {
        h = h.TrimStart('#');
        return new Color32(
            Convert.ToByte(h.Substring(0,2),16),
            Convert.ToByte(h.Substring(2,2),16),
            Convert.ToByte(h.Substring(4,2),16),
            h.Length >= 8 ? Convert.ToByte(h.Substring(6,2),16) : (byte)255);
    }
    static readonly Color BG     = Hex("faf8ef");
    static readonly Color BOARD  = Hex("bbada0");
    static readonly Color EMPTY  = Hex("cdc1b4");
    static readonly Color DARK   = Hex("776e65");
    static readonly Color LITE   = Hex("f9f6f2");
    static readonly Color[] TC   = {
        Hex("eee4da"),Hex("ede0c8"),Hex("f2b179"),Hex("f59563"),
        Hex("f67c5f"),Hex("f65e3b"),Hex("edcf72"),Hex("edcc61"),
        Hex("edc850"),Hex("edc53f"),Hex("edc22e")
    };

    // ────────── ライフサイクル ─────────────────────────

    void Awake()
    {
        try
        {
            status = "Building UI...";
            best = PlayerPrefs.GetInt("Best2048", 0);
            BuildCamera();
            BuildUI();
            NewGame();
            uiReady = true;
            status = "Running";
        }
        catch (Exception ex)
        {
            status = "ERROR: " + ex.GetType().Name + ": " + ex.Message
                   + "\n" + ex.StackTrace;
            Debug.LogError(status);
        }
    }

    void Update()
    {
        if (!uiReady || gameEnded) return;
        Vector2Int d = Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.UpArrow))    d = new Vector2Int( 0, 1);
        if (Input.GetKeyDown(KeyCode.DownArrow))  d = new Vector2Int( 0,-1);
        if (Input.GetKeyDown(KeyCode.LeftArrow))  d = new Vector2Int(-1, 0);
        if (Input.GetKeyDown(KeyCode.RightArrow)) d = new Vector2Int( 1, 0);
        if (d == Vector2Int.zero) return;
        if (Move(d)) AfterMove();
    }

    // 常時ステータスを右下に表示（起動確認用）
    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label);
        style.fontSize  = 12;
        style.alignment = TextAnchor.LowerRight;

        if (status == "Running")
        {
            style.normal.textColor = new Color(0,0,0,0.3f);
            GUI.Label(new Rect(0, Screen.height-24, Screen.width-4, 20), "v1.0", style);
        }
        else
        {
            style.normal.textColor = Color.red;
            style.fontSize = 14;
            style.wordWrap = true;
            GUI.Label(new Rect(8, 8, Screen.width-16, Screen.height-16), status, style);
        }
    }

    // ────────── カメラ ────────────────────────────────

    void BuildCamera()
    {
        var camGO = new GameObject("MainCamera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = BG;
        cam.orthographic     = true;
        cam.depth            = -1;
    }

    // ────────── ゲームロジック ────────────────────────

    public void NewGame()
    {
        score = 0; gameEnded = false; won = false;
        Array.Clear(board, 0, board.Length);
        Spawn(); Spawn();
        Redraw();
        goPanel?.SetActive(false);
        winPanel?.SetActive(false);
    }

    void Spawn()
    {
        var empty = new List<int>();
        for (int i = 0; i < SIZE*SIZE; i++)
            if (board[i/SIZE, i%SIZE] == 0) empty.Add(i);
        if (empty.Count == 0) return;
        int idx = empty[UnityEngine.Random.Range(0, empty.Count)];
        board[idx/SIZE, idx%SIZE] = UnityEngine.Random.value < 0.9f ? 2 : 4;
    }

    bool Move(Vector2Int dir)
    {
        bool moved = false;
        bool[,] merged = new bool[SIZE, SIZE];
        int[] rs = MakeOrder(dir.y);
        int[] cs = MakeOrder(dir.x);

        foreach (int r in rs)
        foreach (int c in cs)
        {
            if (board[r,c] == 0) continue;
            int nr = r, nc = c;
            while (true)
            {
                int tr = nr + dir.y, tc = nc + dir.x;
                if (tr < 0 || tr >= SIZE || tc < 0 || tc >= SIZE) break;
                if (board[tr,tc] != 0)
                {
                    if (board[tr,tc] == board[r,c] && !merged[tr,tc])
                        { nr = tr; nc = tc; }
                    break;
                }
                nr = tr; nc = tc;
            }
            if (nr == r && nc == c) continue;
            moved = true;
            if (board[nr,nc] == board[r,c] && !merged[nr,nc])
            { board[nr,nc] = board[r,c]*2; score += board[nr,nc]; merged[nr,nc] = true; }
            else
            { board[nr,nc] = board[r,c]; }
            board[r,c] = 0;
        }
        return moved;
    }

    int[] MakeOrder(int dir)
    {
        var a = new int[SIZE];
        for (int i = 0; i < SIZE; i++) a[i] = i;
        if (dir > 0) Array.Reverse(a);
        return a;
    }

    void AfterMove()
    {
        if (!won && Has(WIN)) { won = true; winPanel?.SetActive(true); }
        Spawn(); Redraw();
        if (score > best) { best = score; PlayerPrefs.SetInt("Best2048", best); }
        if (!CanMove()) { gameEnded = true; goPanel?.SetActive(true); }
    }

    bool Has(int v) {
        for (int r=0;r<SIZE;r++) for (int c=0;c<SIZE;c++) if (board[r,c]==v) return true;
        return false;
    }
    bool CanMove() {
        for (int r=0;r<SIZE;r++) for (int c=0;c<SIZE;c++) {
            if (board[r,c]==0) return true;
            if (r+1<SIZE && board[r,c]==board[r+1,c]) return true;
            if (c+1<SIZE && board[r,c]==board[r,c+1]) return true;
        }
        return false;
    }

    // ────────── 描画 ──────────────────────────────────

    void Redraw()
    {
        for (int r=0;r<SIZE;r++)
        for (int c=0;c<SIZE;c++)
        {
            int i = r*SIZE+c, v = board[r,c];
            cellBg[i].color  = v==0 ? EMPTY : TC[Mathf.Clamp((int)Mathf.Log(v,2)-1,0,TC.Length-1)];
            cellTxt[i].text  = v==0 ? "" : v.ToString();
            cellTxt[i].color = v<=4 ? DARK : LITE;
            cellTxt[i].fontSize = v>=1000?20:v>=100?24:30;
        }
        if (scoreVal) scoreVal.text = score.ToString();
        if (bestVal)  bestVal.text  = best.ToString();
    }

    // ────────── UI 構築 ───────────────────────────────

    void BuildUI()
    {
        // EventSystem
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        // Canvas
        var cvGO = new GameObject("Canvas");
        var cv   = cvGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 0;
        var scaler = cvGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(480, 700);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;
        cvGO.AddComponent<GraphicRaycaster>();
        var root = cvGO.GetComponent<RectTransform>();

        // 画面背景
        AddImg(root,"BG",BG, r => Fill(r));

        // タイトル
        AddTxt(root,"Title","2048",50,DARK,FontStyle.Bold,
            r => Pos(r,.5f,.88f,0,0,180,60));

        // スコア
        scoreVal = ScoreBox(root,"SCORE",.72f,.88f);
        bestVal  = ScoreBox(root,"BEST", .88f,.88f);

        // New Game ボタン
        var nb = AddImg(root,"NewBtn",Hex("8f7a66"), r=>Pos(r,.5f,.81f,0,0,140,36));
        AddTxt(nb,"L","New Game",15,LITE,FontStyle.Bold, r=>Fill(r));
        MakeBtn(nb.gameObject, NewGame);

        // グリッド
        float bp = SIZE*CELL+(SIZE+1)*GAP;
        var grid = AddImg(root,"Grid",BOARD, r=>Pos(r,.5f,.44f,0,0,bp,bp));

        for (int row=0; row<SIZE; row++)
        for (int col=0; col<SIZE; col++)
        {
            int idx = row*SIZE+col;
            float x = GAP+col*(CELL+GAP)+CELL*.5f-bp*.5f;
            float y = GAP+row*(CELL+GAP)+CELL*.5f-bp*.5f;
            AddImg(grid,"E"+idx,EMPTY, r=>Pos(r,.5f,.5f,x,y,CELL,CELL));
            var tRT = AddImg(grid,"T"+idx,EMPTY, r=>Pos(r,.5f,.5f,x,y,CELL,CELL));
            cellBg[idx]  = tRT.GetComponent<Image>();
            cellTxt[idx] = AddTxt(tRT,"N","",30,DARK,FontStyle.Bold,r=>Fill(r));
        }

        // オーバーレイ
        goPanel  = MakeOverlay(root,"GAME OVER","これ以上動けません","もう一度",
                               ()=>NewGame(),Hex("eee4daDD"));
        winPanel = MakeOverlay(root,"YOU WIN!","2048 達成！おめでとう","続ける",
                               ()=>winPanel.SetActive(false),Hex("edcf72DD"));
        goPanel.SetActive(false);
        winPanel.SetActive(false);
    }

    // ────────── UI ヘルパー ───────────────────────────

    Font GetFont() {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
             ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }

    RectTransform AddImg(RectTransform p, string name, Color col, Action<RectTransform> lay=null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(p, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        lay?.Invoke(rt);
        return rt;
    }

    Text AddTxt(RectTransform p, string name, string txt, int sz, Color col,
                FontStyle fs, Action<RectTransform> lay=null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(p, false);
        var t = go.AddComponent<Text>();
        t.text      = txt;
        t.fontSize  = sz;
        t.color     = col;
        t.fontStyle = fs;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        var f = GetFont();
        if (f != null) t.font = f;
        var rt = go.GetComponent<RectTransform>();
        lay?.Invoke(rt);
        return t;
    }

    Text ScoreBox(RectTransform root, string lbl, float ax, float ay)
    {
        var box = AddImg(root,lbl+"Box",Hex("bbada0"), r=>Pos(r,ax,ay,0,0,70,46));
        AddTxt(box,"L",lbl,11,LITE,FontStyle.Bold,
            r=>{ r.anchorMin=new Vector2(0,.55f); r.anchorMax=Vector2.one;
                 r.offsetMin=r.offsetMax=Vector2.zero; });
        return AddTxt(box,"V","0",18,LITE,FontStyle.Bold,
            r=>{ r.anchorMin=Vector2.zero; r.anchorMax=new Vector2(1,.62f);
                 r.offsetMin=r.offsetMax=Vector2.zero; });
    }

    GameObject MakeOverlay(RectTransform root, string title, string sub,
                           string btnTxt, Action cb, Color bg)
    {
        var p = AddImg(root,title+"P",bg, r=>{
            r.anchorMin=new Vector2(.05f,.30f); r.anchorMax=new Vector2(.95f,.62f);
            r.offsetMin=r.offsetMax=Vector2.zero; });
        AddTxt(p,"T",title,34,DARK,FontStyle.Bold,
            r=>{ r.anchorMin=new Vector2(0,.62f); r.anchorMax=Vector2.one;
                 r.offsetMin=r.offsetMax=Vector2.zero; });
        AddTxt(p,"S",sub,14,DARK,FontStyle.Normal,
            r=>{ r.anchorMin=new Vector2(0,.38f); r.anchorMax=new Vector2(1,.65f);
                 r.offsetMin=r.offsetMax=Vector2.zero; });
        var btn = AddImg(p,"Btn",Hex("8f7a66"),
            r=>{ r.anchorMin=new Vector2(.2f,.06f); r.anchorMax=new Vector2(.8f,.33f);
                 r.offsetMin=r.offsetMax=Vector2.zero; });
        AddTxt(btn,"BL",btnTxt,14,LITE,FontStyle.Bold, r=>Fill(r));
        MakeBtn(btn.gameObject, cb);
        return p.gameObject;
    }

    void MakeBtn(GameObject go, Action action)
    {
        var btn = go.AddComponent<Button>();
        var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
        btn.onClick.AddListener(()=>action());
    }

    void Fill(RectTransform rt)
    { rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=rt.offsetMax=Vector2.zero; }

    void Pos(RectTransform rt, float ax, float ay, float px, float py, float w, float h)
    { rt.anchorMin=rt.anchorMax=new Vector2(ax,ay); rt.anchoredPosition=new Vector2(px,py); rt.sizeDelta=new Vector2(w,h); }
}
