using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// シーン設定なしで2048ゲーム全体をコードから構築するブートストラップ。
/// RuntimeInitializeOnLoadMethod によりシーンのワイヤリング不要で動作する。
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var go = new GameObject("GameBootstrap");
        DontDestroyOnLoad(go);
        go.AddComponent<GameBootstrap>();
    }

    // ---- 定数 ----
    const int SIZE = 4;
    const int WIN = 2048;
    const float CELL = 110f;
    const float GAP = 12f;

    // ---- 状態 ----
    int[,] board = new int[SIZE, SIZE];
    int score = 0;
    int best = 0;

    // ---- UI ----
    TextMeshProUGUI scoreLabel, bestLabel;
    RectTransform[] cellRects = new RectTransform[SIZE * SIZE];
    Image[] cellBg = new Image[SIZE * SIZE];
    TextMeshProUGUI[] cellText = new TextMeshProUGUI[SIZE * SIZE];
    GameObject gameOverPanel, winPanel;
    bool gameEnded = false;
    bool won = false;

    static readonly Color COL_BG = new Color(0.74f, 0.68f, 0.63f);
    static readonly Color COL_EMPTY = new Color(0.80f, 0.75f, 0.70f);
    static readonly Color[] TILE_COLS = {
        new Color(0.93f,0.89f,0.85f), // 2
        new Color(0.93f,0.88f,0.78f), // 4
        new Color(0.95f,0.69f,0.47f), // 8
        new Color(0.96f,0.58f,0.39f), // 16
        new Color(0.96f,0.49f,0.37f), // 32
        new Color(0.96f,0.37f,0.23f), // 64
        new Color(0.93f,0.81f,0.45f), // 128
        new Color(0.93f,0.80f,0.38f), // 256
        new Color(0.93f,0.78f,0.31f), // 512
        new Color(0.93f,0.77f,0.25f), // 1024
        new Color(0.93f,0.76f,0.18f), // 2048+
    };

    void Awake()
    {
        best = PlayerPrefs.GetInt("Best", 0);
        BuildUI();
        NewGame();
    }

    void Update()
    {
        if (gameEnded) return;
        Vector2Int dir = Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.UpArrow))    dir = Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.DownArrow))  dir = Vector2Int.down;
        if (Input.GetKeyDown(KeyCode.LeftArrow))  dir = Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.RightArrow)) dir = Vector2Int.right;
        if (dir == Vector2Int.zero) return;
        if (TryMove(dir)) AfterMove();
    }

    // ======== ゲームロジック ========

    void NewGame()
    {
        score = 0;
        gameEnded = false;
        won = false;
        board = new int[SIZE, SIZE];
        SpawnRandom(); SpawnRandom();
        RefreshBoard();
        UpdateScore();
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);
    }

    void SpawnRandom()
    {
        var empties = new List<(int r, int c)>();
        for (int r = 0; r < SIZE; r++)
            for (int c = 0; c < SIZE; c++)
                if (board[r, c] == 0) empties.Add((r, c));
        if (empties.Count == 0) return;
        var (row, col) = empties[Random.Range(0, empties.Count)];
        board[row, col] = Random.value < 0.9f ? 2 : 4;
    }

    bool TryMove(Vector2Int dir)
    {
        bool moved = false;
        bool[,] merged = new bool[SIZE, SIZE];
        int dr = -dir.y, dc = dir.x;

        int rStart = dr > 0 ? SIZE - 1 : 0, rEnd = dr > 0 ? -1 : SIZE, rStep = dr > 0 ? -1 : 1;
        int cStart = dc > 0 ? SIZE - 1 : 0, cEnd = dc > 0 ? -1 : SIZE, cStep = dc > 0 ? -1 : 1;

        for (int r = rStart; r != rEnd; r += rStep)
        for (int c = cStart; c != cEnd; c += cStep)
        {
            if (board[r, c] == 0) continue;
            int nr = r + dr, nc = c + dc;
            while (nr >= 0 && nr < SIZE && nc >= 0 && nc < SIZE)
            {
                if (board[nr, nc] != 0)
                {
                    if (board[nr, nc] == board[nr - dr, nc - dc] && !merged[nr, nc])
                    {
                        int val = board[nr, nc] * 2;
                        board[nr, nc] = val;
                        board[nr - dr, nc - dc] = 0;
                        merged[nr, nc] = true;
                        score += val;
                        moved = true;
                    }
                    break;
                }
                board[nr, nc] = board[nr - dr, nc - dc];
                board[nr - dr, nc - dc] = 0;
                moved = true;
                nr += dr; nc += dc;
            }
        }
        return moved;
    }

    void AfterMove()
    {
        if (!won && HasValue(WIN))
        {
            won = true;
            if (winPanel) winPanel.SetActive(true);
        }
        SpawnRandom();
        RefreshBoard();
        if (score > best) { best = score; PlayerPrefs.SetInt("Best", best); }
        UpdateScore();
        if (!CanMove()) { gameEnded = true; if (gameOverPanel) gameOverPanel.SetActive(true); }
    }

    bool HasValue(int v) { for (int r = 0; r < SIZE; r++) for (int c = 0; c < SIZE; c++) if (board[r, c] == v) return true; return false; }

    bool CanMove()
    {
        for (int r = 0; r < SIZE; r++) for (int c = 0; c < SIZE; c++)
        {
            if (board[r, c] == 0) return true;
            if (r + 1 < SIZE && board[r, c] == board[r + 1, c]) return true;
            if (c + 1 < SIZE && board[r, c] == board[r, c + 1]) return true;
        }
        return false;
    }

    // ======== UI 構築 ========

    void BuildUI()
    {
        // Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        ((CanvasScaler)canvasGO.GetComponent<CanvasScaler>()).referenceResolution = new Vector2(600, 800);
        canvasGO.AddComponent<GraphicRaycaster>();

        // 背景
        var bg = MakeImage(canvasGO.transform, "BG", new Color(0.97f, 0.96f, 0.94f));
        bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one;
        bg.offsetMin = bg.offsetMax = Vector2.zero;

        // ヘッダー
        var header = MakePanel(canvasGO.transform, "Header", new Vector2(560, 80), new Vector2(0, 360));
        MakeLabel(header, "Title", "2048", 48, new Vector2(-150, 0), new Color(0.47f, 0.43f, 0.40f));
        var scoreBox = MakeScoreBox(header, "SCORE", new Vector2(60, 0));
        scoreLabel = scoreBox.GetComponentInChildren<TextMeshProUGUI>();
        var bestBox = MakeScoreBox(header, "BEST", new Vector2(150, 0));
        bestLabel = bestBox.GetComponentInChildren<TextMeshProUGUI>();

        // New Game ボタン
        var btnRT = MakePanel(canvasGO.transform, "NewBtn", new Vector2(140, 44), new Vector2(0, 305));
        var btnImg = btnRT.GetComponent<Image>() ?? btnRT.gameObject.AddComponent<Image>();
        btnImg.color = new Color(0.74f, 0.68f, 0.63f);
        AddRoundedButton(btnRT.gameObject, NewGame);
        MakeLabel(btnRT, "BtnLabel", "New Game", 18, Vector2.zero, Color.white);

        // グリッド背景
        float boardSize = SIZE * CELL + (SIZE + 1) * GAP;
        var gridBG = MakeImage(canvasGO.transform, "GridBG", COL_BG);
        gridBG.sizeDelta = new Vector2(boardSize, boardSize);
        gridBG.anchoredPosition = new Vector2(0, -20);

        // セル生成
        for (int r = 0; r < SIZE; r++)
        for (int c = 0; c < SIZE; c++)
        {
            int idx = r * SIZE + c;
            float x = GAP + c * (CELL + GAP) + CELL * 0.5f - boardSize * 0.5f;
            float y = GAP + r * (CELL + GAP) + CELL * 0.5f - boardSize * 0.5f;

            // 空セル背景
            var emptyCell = MakeImage(gridBG, "Cell_" + idx, COL_EMPTY);
            emptyCell.sizeDelta = new Vector2(CELL, CELL);
            emptyCell.anchoredPosition = new Vector2(x, y);

            // タイル
            var tileRT = MakeImage(gridBG, "Tile_" + idx, COL_EMPTY);
            tileRT.sizeDelta = new Vector2(CELL, CELL);
            tileRT.anchoredPosition = new Vector2(x, y);
            cellRects[idx] = tileRT;
            cellBg[idx] = tileRT.GetComponent<Image>();
            var lbl = MakeLabel(tileRT, "Num", "", 32, Vector2.zero, new Color(0.47f, 0.43f, 0.40f));
            cellText[idx] = lbl;
        }

        // ゲームオーバーパネル
        gameOverPanel = BuildOverlayPanel(canvasGO.transform, "GAME OVER", "もう動けません", "もう一度", gridBG, new Color(0.97f, 0.96f, 0.94f, 0.85f));
        gameOverPanel.SetActive(false);

        // クリアパネル
        winPanel = BuildOverlayPanel(canvasGO.transform, "YOU WIN!", "2048 達成！", "続ける", gridBG, new Color(0.93f, 0.81f, 0.45f, 0.85f));
        winPanel.SetActive(false);
    }

    // ---- ヘルパー ----

    RectTransform MakeImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        return rt;
    }

    RectTransform MakePanel(Transform parent, string name, Vector2 size, Vector2 pos)
    {
        var rt = MakeImage(parent, name, Color.clear);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return rt;
    }

    TextMeshProUGUI MakeLabel(RectTransform parent, string name, string text, int size, Vector2 pos, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = pos;
        return tmp;
    }

    RectTransform MakeScoreBox(RectTransform parent, string label, Vector2 pos)
    {
        var box = MakeImage(parent, label + "Box", new Color(0.74f, 0.68f, 0.63f));
        box.sizeDelta = new Vector2(80, 50);
        box.anchoredPosition = pos;
        MakeLabel(box, "Label", label, 12, new Vector2(0, 10), new Color(0.87f, 0.84f, 0.81f));
        MakeLabel(box, "Value", "0", 18, new Vector2(0, -8), Color.white);
        return box;
    }

    GameObject BuildOverlayPanel(Transform parent, string title, string sub, string btnLabel, RectTransform anchor, Color bgColor)
    {
        var panel = new GameObject(title + "Panel");
        panel.transform.SetParent(parent, false);
        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = anchor.sizeDelta;
        rt.anchoredPosition = anchor.anchoredPosition;
        var img = panel.AddComponent<Image>();
        img.color = bgColor;

        MakeLabel(rt, "Title", title, 42, new Vector2(0, 60), new Color(0.47f, 0.43f, 0.40f));
        MakeLabel(rt, "Sub", sub, 20, new Vector2(0, 10), new Color(0.47f, 0.43f, 0.40f));

        var btn = MakeImage(rt, "Btn", new Color(0.74f, 0.68f, 0.63f));
        btn.sizeDelta = new Vector2(150, 44);
        btn.anchoredPosition = new Vector2(0, -50);
        AddRoundedButton(btn.gameObject, () => { panel.SetActive(false); if (title == "GAME OVER" || title == "YOU WIN!") NewGame(); });
        MakeLabel(btn, "BtnLabel", btnLabel, 18, Vector2.zero, Color.white);

        return panel;
    }

    void AddRoundedButton(GameObject go, UnityEngine.Events.UnityAction action)
    {
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.85f, 0.79f, 0.73f);
        colors.pressedColor = new Color(0.65f, 0.60f, 0.56f);
        btn.colors = colors;
        btn.onClick.AddListener(action);
    }

    // ======== 表示更新 ========

    void RefreshBoard()
    {
        for (int r = 0; r < SIZE; r++)
        for (int c = 0; c < SIZE; c++)
        {
            int idx = r * SIZE + c;
            int v = board[r, c];
            cellBg[idx].color = v == 0 ? COL_EMPTY : TileColor(v);
            cellText[idx].text = v == 0 ? "" : v.ToString();
            cellText[idx].color = v <= 4 ? new Color(0.47f, 0.43f, 0.40f) : Color.white;
            cellText[idx].fontSize = v >= 1000 ? 24 : v >= 100 ? 28 : 32;
            if (v != 0) StartCoroutine(PopAnim(cellRects[idx]));
        }
    }

    Color TileColor(int v)
    {
        int idx = Mathf.Clamp((int)Mathf.Log(v, 2) - 1, 0, TILE_COLS.Length - 1);
        return TILE_COLS[idx];
    }

    void UpdateScore()
    {
        if (scoreLabel) scoreLabel.text = score.ToString();
        if (bestLabel) bestLabel.text = best.ToString();
        // スコアボックスの Value ラベルを更新
        UpdateScoreBox("SCOREBox", score);
        UpdateScoreBox("BESTBox", best);
    }

    void UpdateScoreBox(string boxName, int val)
    {
        var box = GameObject.Find(boxName);
        if (box == null) return;
        var labels = box.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var l in labels)
            if (l.gameObject.name == "Value") l.text = val.ToString();
    }

    IEnumerator PopAnim(RectTransform rt)
    {
        rt.localScale = Vector3.one * 0.8f;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / 0.12f;
            float s = Mathf.Lerp(0.8f, 1f, t < 0.7f ? t / 0.7f * 1.1f : 1f + (1f - t) / 0.3f * 0.1f);
            rt.localScale = Vector3.one * s;
            yield return null;
        }
        rt.localScale = Vector3.one;
    }
}
