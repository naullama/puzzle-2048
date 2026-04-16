using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameBootstrap : MonoBehaviour
{
    // ── 定数 ──────────────────────────────────────────
    const int   SIZE = 4;
    const int   WIN  = 2048;
    const float CELL = 100f;
    const float GAP  = 10f;

    // ── 状態 ──────────────────────────────────────────
    int[,] board     = new int[SIZE, SIZE];
    int    score, best;
    bool   gameEnded, won;

    // ── UI 参照 ───────────────────────────────────────
    Text       scoreVal, bestVal;
    Image[]    cellBg   = new Image[SIZE * SIZE];
    Text[]     cellTxt  = new Text[SIZE * SIZE];
    GameObject goPanel, winPanel;

    // ── デバッグ ──────────────────────────────────────
    string debugMsg = "";

    // ── カラー定義 ────────────────────────────────────
    static Color C(string h) {
        h = h.TrimStart('#');
        byte r = Convert.ToByte(h.Substring(0,2),16);
        byte g = Convert.ToByte(h.Substring(2,2),16);
        byte b = Convert.ToByte(h.Substring(4,2),16);
        byte a = h.Length>=8 ? Convert.ToByte(h.Substring(6,2),16) : (byte)255;
        return new Color32(r,g,b,a);
    }
    static readonly Color BG_SCREEN = C("faf8ef");
    static readonly Color BG_BOARD  = C("bbada0");
    static readonly Color BG_EMPTY  = C("cdc1b4");
    static readonly Color DARK_TXT  = C("776e65");
    static readonly Color LITE_TXT  = C("f9f6f2");
    static readonly Color[] TILE = {
        C("eee4da"),C("ede0c8"),C("f2b179"),C("f59563"),
        C("f67c5f"),C("f65e3b"),C("edcf72"),C("edcc61"),
        C("edc850"),C("edc53f"),C("edc22e")
    };

    // ── Unity ライフサイクル ──────────────────────────

    void Awake()
    {
        try
        {
            best = PlayerPrefs.GetInt("Best2048", 0);
            BuildUI();
            NewGame();
            debugMsg = "OK";
        }
        catch (Exception e)
        {
            debugMsg = "ERR: " + e.Message + "\n" + e.StackTrace;
            Debug.LogError(debugMsg);
        }
    }

    void Update()
    {
        if (gameEnded) return;
        Vector2Int d = Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.UpArrow))    d = new Vector2Int( 0, 1);
        if (Input.GetKeyDown(KeyCode.DownArrow))  d = new Vector2Int( 0,-1);
        if (Input.GetKeyDown(KeyCode.LeftArrow))  d = new Vector2Int(-1, 0);
        if (Input.GetKeyDown(KeyCode.RightArrow)) d = new Vector2Int( 1, 0);
        if (d == Vector2Int.zero) return;
        if (Move(d)) AfterMove();
    }

    // デバッグ: WebGL でエラーが出たときに画面に表示
    void OnGUI()
    {
        if (debugMsg != "OK" && !string.IsNullOrEmpty(debugMsg))
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(10, 10, Screen.width - 20, Screen.height - 20), debugMsg);
        }
    }

    // ── ゲームロジック ────────────────────────────────

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
        bool[,] merged = new bool[SIZE,SIZE];

        int[] rs = Order(dir.y);
        int[] cs = Order(dir.x);

        foreach (int r in rs)
        foreach (int c in cs)
        {
            if (board[r,c] == 0) continue;

            int nr = r, nc = c;
            while (true)
            {
                int tr = nr + dir.y;
                int tc = nc + dir.x;
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
            {
                board[nr,nc]   = board[r,c] * 2;
                score         += board[nr,nc];
                merged[nr,nc]  = true;
            }
            else
            {
                board[nr,nc] = board[r,c];
            }
            board[r,c] = 0;
        }
        return moved;
    }

    int[] Order(int dir)
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

    bool Has(int v)
    { for (int r=0;r<SIZE;r++) for (int c=0;c<SIZE;c++) if (board[r,c]==v) return true; return false; }

    bool CanMove()
    {
        for (int r=0;r<SIZE;r++) for (int c=0;c<SIZE;c++) {
            if (board[r,c]==0) return true;
            if (r+1<SIZE && board[r,c]==board[r+1,c]) return true;
            if (c+1<SIZE && board[r,c]==board[r,c+1]) return true;
        }
        return false;
    }

    // ── 描画更新 ──────────────────────────────────────

    void Redraw()
    {
        for (int r=0;r<SIZE;r++)
        for (int c=0;c<SIZE;c++)
        {
            int idx = r*SIZE+c, v = board[r,c];
            cellBg[idx].color  = v==0 ? BG_EMPTY : TileCol(v);
            cellTxt[idx].text  = v==0 ? "" : v.ToString();
            cellTxt[idx].color = v<=4 ? DARK_TXT : LITE_TXT;
            cellTxt[idx].fontSize = v>=1000?22:v>=100?26:32;
        }
        if (scoreVal) scoreVal.text = score.ToString();
        if (bestVal)  bestVal.text  = best.ToString();
    }

    Color TileCol(int v)
    { return TILE[Mathf.Clamp((int)Mathf.Log(v,2)-1, 0, TILE.Length-1)]; }

    // ── UI 構築 ────────────────────────────────────────

    void BuildUI()
    {
        // EventSystem（ボタン操作に必須）
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // Canvas
        var cvGO = new GameObject("Canvas");
        var cv   = cvGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 0;
        var cs = cvGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution= new Vector2(480, 700);
        cs.screenMatchMode    = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight = 0.5f;
        cvGO.AddComponent<GraphicRaycaster>();

        var root = cvGO.GetComponent<RectTransform>();

        // 画面背景
        Img(root,"BG", BG_SCREEN, a=>Stretch(a));

        // タイトル
        Txt(root,"Title","2048",52,DARK_TXT,FontStyle.Bold,
            r=>{ Place(r, .5f,.86f, 0,0, 160,60); });

        // スコアボックス
        scoreVal = ScoreBox(root,"SCORE", .72f,.87f);
        bestVal  = ScoreBox(root,"BEST",  .88f,.87f);

        // New Game ボタン
        var btnRT = Img(root,"NewBtn",C("8f7a66"),
            r=>Place(r,.5f,.80f, 0,0, 150,38));
        Txt(btnRT,"L","New Game",16,LITE_TXT,FontStyle.Bold,r=>Stretch(r));
        Btn(btnRT.gameObject, NewGame);

        // グリッド背景
        float bp = SIZE*CELL+(SIZE+1)*GAP;
        var grid = Img(root,"Grid",BG_BOARD, r=>Place(r,.5f,.43f, 0,0, bp,bp));

        // セル
        for (int row=0;row<SIZE;row++)
        for (int col=0;col<SIZE;col++)
        {
            int idx = row*SIZE+col;
            float x = GAP+col*(CELL+GAP)+CELL*.5f-bp*.5f;
            float y = GAP+row*(CELL+GAP)+CELL*.5f-bp*.5f;

            Img(grid,"E"+idx,BG_EMPTY, r=>Place(r,.5f,.5f, x,y, CELL,CELL));

            var tRT = Img(grid,"T"+idx,BG_EMPTY, r=>Place(r,.5f,.5f, x,y, CELL,CELL));
            cellBg[idx]  = tRT.GetComponent<Image>();
            cellTxt[idx] = Txt(tRT,"N","",32,DARK_TXT,FontStyle.Bold, r=>Stretch(r));
        }

        // オーバーレイパネル
        goPanel  = Overlay(root,"GAME OVER","これ以上動けません","もう一度",
                           ()=>NewGame(), C("eee4daDD"));
        winPanel = Overlay(root,"YOU WIN!","2048 達成！","続ける",
                           ()=>winPanel.SetActive(false), C("edcf72DD"));
        goPanel.SetActive(false);
        winPanel.SetActive(false);
    }

    // ── UI ヘルパー ────────────────────────────────────

    RectTransform Img(RectTransform p, string n, Color col, Action<RectTransform> layout=null)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        layout?.Invoke(rt);
        return rt;
    }

    Text Txt(RectTransform p, string n, string txt, int sz, Color col, FontStyle fs,
             Action<RectTransform> layout=null)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var t  = go.AddComponent<Text>();
        t.text      = txt;
        t.fontSize  = sz;
        t.color     = col;
        t.fontStyle = fs;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        // 組み込みフォントを安全に取得
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null) t.font = font;
        var rt = go.GetComponent<RectTransform>();
        layout?.Invoke(rt);
        return t;
    }

    Text ScoreBox(RectTransform root, string label, float ax, float ay)
    {
        var box = Img(root, label+"Box", C("bbada0"), r=>Place(r,ax,ay, 0,0, 72,48));
        Txt(box,"L",label,11,LITE_TXT,FontStyle.Bold,
            r=>{ r.anchorMin=new Vector2(0,.55f); r.anchorMax=Vector2.one;
                 r.offsetMin=r.offsetMax=Vector2.zero; });
        return Txt(box,"V","0",18,LITE_TXT,FontStyle.Bold,
            r=>{ r.anchorMin=Vector2.zero; r.anchorMax=new Vector2(1,.6f);
                 r.offsetMin=r.offsetMax=Vector2.zero; });
    }

    GameObject Overlay(RectTransform root, string title, string sub,
                       string btnLbl, Action onClick, Color bg)
    {
        var panel = Img(root, title+"P", bg,
            r=>{ r.anchorMin=new Vector2(.08f,.28f); r.anchorMax=new Vector2(.92f,.62f);
                 r.offsetMin=r.offsetMax=Vector2.zero; });
        Txt(panel,"T",title,36,DARK_TXT,FontStyle.Bold,
            r=>{ r.anchorMin=new Vector2(0,.62f); r.anchorMax=Vector2.one;
                 r.offsetMin=r.offsetMax=Vector2.zero; });
        Txt(panel,"S",sub,15,DARK_TXT,FontStyle.Normal,
            r=>{ r.anchorMin=new Vector2(0,.38f); r.anchorMax=new Vector2(1,.64f);
                 r.offsetMin=r.offsetMax=Vector2.zero; });
        var btn = Img(panel,"Btn",C("8f7a66"),
            r=>{ r.anchorMin=new Vector2(.2f,.06f); r.anchorMax=new Vector2(.8f,.34f);
                 r.offsetMin=r.offsetMax=Vector2.zero; });
        Txt(btn,"BL",btnLbl,15,LITE_TXT,FontStyle.Bold,r=>Stretch(r));
        Btn(btn.gameObject, ()=>onClick());
        return panel.gameObject;
    }

    void Btn(GameObject go, Action action)
    {
        var btn  = go.AddComponent<Button>();
        var nav  = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;
        btn.onClick.AddListener(() => action());
    }

    void Stretch(RectTransform rt)
    { rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=rt.offsetMax=Vector2.zero; }

    void Place(RectTransform rt, float ax, float ay, float px, float py, float w, float h)
    { rt.anchorMin=rt.anchorMax=new Vector2(ax,ay); rt.anchoredPosition=new Vector2(px,py); rt.sizeDelta=new Vector2(w,h); }
}
