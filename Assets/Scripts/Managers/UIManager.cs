using UnityEngine;
using TMPro;

namespace Puzzle2048.Managers
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI gameOverScoreText;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private TextMeshProUGUI winScoreText;

        public void UpdateScore(int score, int best)
        {
            if (scoreText) scoreText.text = score.ToString();
            if (bestScoreText) bestScoreText.text = best.ToString();
        }

        public void ShowGameOverPanel(int score)
        {
            if (gameOverPanel) gameOverPanel.SetActive(true);
            if (gameOverScoreText) gameOverScoreText.text = $"Score: {score}";
        }

        public void ShowWinPanel(int score)
        {
            if (winPanel) winPanel.SetActive(true);
            if (winScoreText) winScoreText.text = $"Score: {score}";
        }

        public void HidePanels()
        {
            if (gameOverPanel) gameOverPanel.SetActive(false);
            if (winPanel) winPanel.SetActive(false);
        }

        public void OnRetryButton()
        {
            GameManager.Instance.StartNewGame();
        }

        public void OnContinueButton()
        {
            if (winPanel) winPanel.SetActive(false);
        }
    }
}
