using UnityEngine;
using Puzzle2048.Core;

namespace Puzzle2048.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GridManager gridManager;
        [SerializeField] private UIManager uiManager;

        public int Score { get; private set; }
        public int BestScore { get; private set; }

        private const string BEST_SCORE_KEY = "BestScore";
        private bool gameOver = false;
        private bool won = false;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BestScore = PlayerPrefs.GetInt(BEST_SCORE_KEY, 0);
        }

        void Start()
        {
            StartNewGame();
        }

        void Update()
        {
            if (gameOver) return;
            HandleInput();
        }

        private void HandleInput()
        {
            Vector2Int direction = Vector2Int.zero;
            if (Input.GetKeyDown(KeyCode.UpArrow))    direction = Vector2Int.up;
            if (Input.GetKeyDown(KeyCode.DownArrow))  direction = Vector2Int.down;
            if (Input.GetKeyDown(KeyCode.LeftArrow))  direction = Vector2Int.left;
            if (Input.GetKeyDown(KeyCode.RightArrow)) direction = Vector2Int.right;

            if (direction == Vector2Int.zero) return;

            int earned = gridManager.Move(direction);
            if (!gridManager.HasMoved) return;

            AddScore(earned);

            if (!won && gridManager.HasTileWithValue(GameConfig.WIN_VALUE))
            {
                won = true;
                uiManager.ShowWinPanel(Score);
            }

            gridManager.SpawnRandomTile();

            if (!gridManager.HasAvailableMoves())
            {
                gameOver = true;
                uiManager.ShowGameOverPanel(Score);
            }
        }

        private void AddScore(int points)
        {
            Score += points;
            if (Score > BestScore)
            {
                BestScore = Score;
                PlayerPrefs.SetInt(BEST_SCORE_KEY, BestScore);
            }
            uiManager.UpdateScore(Score, BestScore);
        }

        public void StartNewGame()
        {
            Score = 0;
            gameOver = false;
            won = false;
            gridManager.ClearGrid();
            for (int i = 0; i < GameConfig.INITIAL_TILES; i++)
                gridManager.SpawnRandomTile();
            uiManager.UpdateScore(Score, BestScore);
            uiManager.HidePanels();
        }
    }
}
