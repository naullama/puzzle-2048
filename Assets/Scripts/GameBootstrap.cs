using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindObjectOfType<GameBootstrap>() != null) return;
        var go = new GameObject("GameBootstrap");
        DontDestroyOnLoad(go);
        go.AddComponent<GameBootstrap>();
    }

    const int SIZE = 4;
    const int WIN  = 2048;
    const float CELL = 100f;
    const float GAP  = 10f;

    int[,]  board = new int[SIZE, SIZE];
    int     score, best;
    bool    gameEnded, won;

    Text        scoreValueText, bestValueText;
    Image[]     cellImg  = new Image[SIZE * SIZE];
    Text[]      cellText = new Text[SIZE * SIZE];
    GameObject  gameOverPanel, winPanel;

    static readonly Color BG_BOARD  = Hex("bbada0");
    static readonly Color BG_EMPTY  = Hex("cdc1b4");
    static readonly Color BG_SCREEN = Hex("faf8ef");

    static readonly Color[] TILE_BG = {
        Hex("eee4da"), Hex("ede0c8"), Hex("f2b179"), Hex("f59563"),
        Hex("f67c5f"), Hex("f65e3b"), Hex("edcf72"), Hex("edcc61"),
        Hex("edc850"), Hex("edc53f"), Hex("edc22e"),
    };
    static readonly Color DARK_TEXT = Hex("776e65");
    static readonly Color LITE_TEXT = Hex("f9f6f2");

    // ── ライフサイクル ──────────────────────────────────

    void Awake()
    {
        best = PlayerPrefs.GetInt("Best2048", 0);
        BuildUI();
        NewGame();
    }

    void Update()
    {
        if (gameEnded) return;
        Vector2Int dir = Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.UpArrow))    dir = new Vector2Int( 0,  1);
        if (Input.GetKeyDown(KeyCode.DownArrow))  dir = new Vector2Int( 0, -1);
        if (Input.GetKeyDown(KeyCode.LeftArrow))  dir = new Vector2Int(-1,  0);
        if (Input.GetKeyDown(KeyCode.RightArrow)) dir = new Vector2Int( 1,  0);
        if (dir == Vector2Int.zero) return;
        if (DoMove(dir)) AfterMove();
    }

    // ── ゲームロジック ────────────────────────────────

    void NewGame()
    {
        score = 0; gameEnded = false; won = false;
        for (int r = 0; r < SIZE; r++)
            for (int c = 0; c < SIZE; c++)
                board[r, c] = 0;
        Spawn(); Spawn();
        Redraw();
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (winPanel)      winPanel.SetActive(false);
    }

    void Spawn()
    {
        var empty = new List<int>();
        for (int i = 0; i < SIZE * SIZE; i++)
            if (board[i / SIZE, i % SIZE] == 0) empty.Add(i);
        if (empty.Count == 0) return;
        int idx = empty[Random.Range(0, empty.Count)];
        board[idx / SIZE, idx % SIZE] = Random.value < 0.9f ? 2 : 4;
    }

    // 戻り値: タイルが動いたか
    bool DoMove(Vector2Int dir)
    {
        bool moved = false;

        // 走査順: 移動方向の先端から処理
        int[] rows = TraversalOrder(dir.y == 0 ? 0 : (dir.y > 0 ? 1 : -1), SIZE);
        int[] cols = TraversalOrder(dir.x == 0 ? 0 : (dir.x > 0 ? 1 : -1), SIZE);

        bool[,] merged = new bool[SIZE, SIZE];

        foreach (int r in rows)
        foreach (int c in cols)
        {
            if (board[r, c] == 0) continue;

            int tr = r, tc = c;
            // 移動先を探す
            while (true)
            {
                int nr = tr + dir.y;   // ←ここは dir.y を行方向に使う
                int nc = tc + dir.x;
                if (nr < 0 || nr >= SIZE || nc < 0 || nc >= SIZE) break;
                if (board[nr, nc] == 0) { tr = nr; tc = nc; }
                else if (board[nr, nc] == board[r, c] && !merged[nr, nc])
                {
                    tr = nr; tc = nc;
                    break;
                }
                else break;
            }

            if (tr == r && tc == c) continue;

            if (board[tr, tc] == board[r, c] && !merged[tr, tc])
            {
                int val = board[r, c] * 2;
                board[tr, tc] = val;
                board[r,  c]  = 0;
                merged[tr, tc] = true;
                score += val;
            }
            else
            {
                board[tr, tc] = board[r, c];
                board[r,  c]  = 0;
            }
            moved = true;
        }
        return moved;
    }

    int[] TraversalOrder(int direction, int size)
    {
        int[] arr = new int[size];
        for (int i = 0; i < size; i++) arr[i] = i;
        if (direction > 0) System.Array.Reverse(arr);
        return arr;
    }

    void AfterMove()
    {
        if (!won && HasValue(WIN))
        {
            won = true;
            if (winPanel) winPanel.SetActive(true);
        }
        Spawn();
        Redraw();
        if (score > best) { best = score; PlayerPrefs.SetInt("Best2048", best); }
        SetScoreText();
        if (!CanMove()) { gameEnded = true; if (gameOverPanel) gameOverPanel.SetActive(true); }
    }

    bool HasValue(int v)
    {
        for (int r = 0; r < SIZE; r++)
            for (int c = 0; c < SIZE; c++)
                if (board[r, c] == v) return true;
        return false;
    }

    bool CanMove()
    {
        for (int r = 0; r < SIZE; r++)
        for (int c = 0; c < SIZE; c++)
        {
            if (board[r, c] == 0) return true;
            if (r+1 < SIZE && board[r,c] == board[r+1,c]) return true;
            if (c+1 < SIZE && board[r,c] == board[r,c+1]) return true;
        }
        return false;
    }

    // ── 表示更新 ──────────────────────────────────────

    void Redraw()
    {
        for (int r = 0; r < SIZE; r++)
        for (int c = 0; c < SIZE; c++)
        {
            int idx = r * SIZE + c;
            int v   = board[r, c];
            cellImg[idx].color  = v == 0 ? BG_EMPTY : TileColor(v);
            cellText[idx].text  = v == 0 ? "" : v.ToString();
            cellText[idx].color = v <= 4 ? DARK_TEXT : LITE_TEXT;
            cellText[idx].fontSize = v >= 1000 ? 22 : v >= 100 ? 26 : 32;
        }
        SetScoreText();
    }

    void SetScoreText()
    {
        if (scoreValueText) scoreValueText.text = score.ToString();
        if (bestValueText)  bestValueText.text  = best.ToString();
    }

    Color TileColor(int v)
    {
        int i = Mathf.Clamp((int)Mathf.Log(v, 2) - 1, 0, TILE_BG.Length - 1);
        return TILE_BG[i];
    }

    // ── UI 構築 ────────────────────────────────────────

    void BuildUI()
    {
        // Canvas
        var cvGO = new GameObject("Canvas");
        var cv   = cvGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = cvGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(500, 700);
        cs.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight  = 0.5f;
        cvGO.AddComponent<GraphicRaycaster>();

        var root = cvGO.GetComponent<RectTransform>();

        // 画面背景
        {
            var bg = NewImg(root, "BG", BG_SCREEN);
            Stretch(bg);
        }

        // タイトル
        var titleT = NewText(root, "Title", "2048", 52, DARK_TEXT, FontStyle.Bold);
        Place(titleT, new Vector2(0, 0.88f), new Vector2(0, 0.88f), new Vector2(-100, 0), new Vector2(200, 60));

        // スコアボックス
        scoreValueText = BuildScoreBox(root, "SCORE",  new Vector2(0.75f, 0.88f));
        bestValueText  = BuildScoreBox(root, "BEST",   new Vector2(0.88f, 0.88f));

        // New Game ボタン
        {
            var btn = NewImg(root, "NewGameBtn", Hex("8f7a66"));
            Place(btn, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(140, 36));
            var t = NewText(btn, "L", "New Game", 16, LITE_TEXT, FontStyle.Bold);
            Stretch(t); CenterText(t.GetComponent<Text>());
            AddBtn(btn.gameObject, NewGame);
        }

        // ガイド
        {
            var guide = NewText(root, "Guide", "← → ↑ ↓ キーで操作", 14, Hex("bbada0"), FontStyle.Normal);
            Place(guide, new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(300, 24));
            CenterText(guide.GetComponent<Text>());
        }

        // グリッド背景
        float board_px = SIZE * CELL + (SIZE + 1) * GAP;
        var gridBG = NewImg(root, "Grid", BG_BOARD);
        Place(gridBG, new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f),
              Vector2.zero, new Vector2(board_px, board_px));
        var gridRT = gridBG;

        // 空セル＋タイル
        for (int r = 0; r < SIZE; r++)
        for (int c = 0; c < SIZE; c++)
        {
            int idx = r * SIZE + c;
            float x = GAP + c * (CELL + GAP) + CELL * 0.5f - board_px * 0.5f;
            float y = GAP + r * (CELL + GAP) + CELL * 0.5f - board_px * 0.5f;
            var pos = new Vector2(x, y);

            // 空セル背景
            var emptyRT = NewImg(gridRT, "E" + idx, BG_EMPTY);
            Place(emptyRT, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), pos, new Vector2(CELL, CELL));

            // タイル
            var tileRT = NewImg(gridRT, "T" + idx, BG_EMPTY);
            Place(tileRT, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), pos, new Vector2(CELL, CELL));
            cellImg[idx] = tileRT.GetComponent<Image>();

            var numText = NewText(tileRT, "N", "", 32, DARK_TEXT, FontStyle.Bold);
            Stretch(numText); CenterText(numText.GetComponent<Text>());
            cellText[idx] = numText.GetComponent<Text>();
        }

        // ゲームオーバーパネル
        gameOverPanel = BuildOverlay(root, "GAME OVER",
            "これ以上動けません", "もう一度", () => NewGame(), Hex("eee4daCC"));

        // クリアパネル
        winPanel = BuildOverlay(root, "YOU WIN!",
            "2048 達成！おめでとう！", "続ける", () => { winPanel.SetActive(false); }, Hex("edcf72CC"));
    }

    // ── UI ヘルパー ────────────────────────────────────

    RectTransform NewImg(RectTransform parent, string name, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        return go.GetComponent<RectTransform>();
    }

    RectTransform NewText(RectTransform parent, string name, string txt, int size, Color color, FontStyle style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text      = txt;
        t.fontSize  = size;
        t.color     = color;
        t.fontStyle = style;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.alignment = TextAnchor.MiddleCenter;
        return go.GetComponent<RectTransform>();
    }

    void Stretch(RectTransform rt)
    {
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
    }

    void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        rt.anchorMin       = anchorMin;
        rt.anchorMax       = anchorMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta       = size;
    }

    void CenterText(Text t)
    {
        t.alignment = TextAnchor.MiddleCenter;
    }

    void AddBtn(GameObject go, UnityEngine.Events.UnityAction action)
    {
        var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
        var nav = btn.navigation;
        nav.mode       = Navigation.Mode.None;
        btn.navigation = nav;
        btn.onClick.AddListener(action);
    }

    Text BuildScoreBox(RectTransform root, string label, Vector2 anchor)
    {
        var box = NewImg(root, label + "Box", Hex("bbada0"));
        Place(box, anchor, anchor, Vector2.zero, new Vector2(70, 46));

        var lbl = NewText(box, "L", label, 11, LITE_TEXT, FontStyle.Bold);
        Place(lbl, new Vector2(0,0.55f), new Vector2(1,1), Vector2.zero, Vector2.zero);
        CenterText(lbl.GetComponent<Text>());

        var val = NewText(box, "V", "0", 18, LITE_TEXT, FontStyle.Bold);
        Place(val, new Vector2(0,0), new Vector2(1,0.6f), Vector2.zero, Vector2.zero);
        CenterText(val.GetComponent<Text>());

        return val.GetComponent<Text>();
    }

    GameObject BuildOverlay(RectTransform root, string title, string sub,
                            string btnLabel, UnityEngine.Events.UnityAction onBtn, Color bgColor)
    {
        var panel = NewImg(root, title + "Panel", bgColor);
        Place(panel, new Vector2(0.1f, 0.25f), new Vector2(0.9f, 0.65f), Vector2.zero, Vector2.zero);
        panel.anchorMin = new Vector2(0.1f, 0.25f);
        panel.anchorMax = new Vector2(0.9f, 0.65f);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;

        var titleT = NewText(panel, "T", title, 38, DARK_TEXT, FontStyle.Bold);
        Place(titleT, new Vector2(0,0.6f), new Vector2(1,0.95f), Vector2.zero, Vector2.zero);
        CenterText(titleT.GetComponent<Text>());

        var subT = NewText(panel, "S", sub, 16, DARK_TEXT, FontStyle.Normal);
        Place(subT, new Vector2(0,0.38f), new Vector2(1,0.62f), Vector2.zero, Vector2.zero);
        CenterText(subT.GetComponent<Text>());

        var btn = NewImg(panel, "Btn", Hex("8f7a66"));
        Place(btn, new Vector2(0.25f,0.05f), new Vector2(0.75f,0.3f), Vector2.zero, Vector2.zero);
        btn.anchorMin = new Vector2(0.25f, 0.05f);
        btn.anchorMax = new Vector2(0.75f, 0.3f);
        btn.offsetMin = Vector2.zero;
        btn.offsetMax = Vector2.zero;

        var btnT = NewText(btn, "L", btnLabel, 16, LITE_TEXT, FontStyle.Bold);
        Stretch(btnT); CenterText(btnT.GetComponent<Text>());
        AddBtn(btn.gameObject, onBtn);

        return panel.gameObject;
    }

    // ── ユーティリティ ────────────────────────────────

    static Color Hex(string hex)
    {
        hex = hex.TrimStart('#');
        byte r = System.Convert.ToByte(hex.Substring(0, 2), 16);
        byte g = System.Convert.ToByte(hex.Substring(2, 2), 16);
        byte b = System.Convert.ToByte(hex.Substring(4, 2), 16);
        byte a = hex.Length >= 8 ? System.Convert.ToByte(hex.Substring(6, 2), 16) : (byte)255;
        return new Color32(r, g, b, a);
    }
}
