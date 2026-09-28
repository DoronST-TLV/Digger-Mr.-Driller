using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Strata
{
    /// <summary>
    /// Title / HUD / Pause / Game Over panels. Listens to GameManager, GridManager and AirMeter events; never polls.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GridManager grid;
        [SerializeField] private AirMeter air;

        [Header("Panels")]
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("Title")]
        [SerializeField] private Text titleBestText;

        [Header("HUD")]
        [SerializeField] private Text depthText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text chainText;
        [SerializeField] private Image airBar;

        [Header("Pause")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button muteButton;
        [SerializeField] private Text muteLabel;

        [Header("Game over")]
        [SerializeField] private Text causeText;
        [SerializeField] private Text finalDepthText;
        [SerializeField] private Text finalScoreText;
        [SerializeField] private Text finalBestText;
        [SerializeField] private Text newBestText;
        [SerializeField] private Text retryText;

        private static readonly Color AirNormal = new Color(0.18f, 0.90f, 0.84f);
        private static readonly Color AirLow = new Color(0.90f, 0.28f, 0.30f);
        private static readonly Color AirLowDark = new Color(0.45f, 0.12f, 0.14f);

        private float airFraction = 1f;

        private void OnEnable()
        {
            gameManager.OnStateChanged += HandleState;
            gameManager.OnScoreChanged += HandleScore;
            gameManager.OnDepthChanged += HandleDepth;
            gameManager.OnGameOver += HandleGameOver;
            grid.OnChainChanged += HandleChain;
            air.OnChanged += HandleAir;
        }

        private void OnDisable()
        {
            gameManager.OnStateChanged -= HandleState;
            gameManager.OnScoreChanged -= HandleScore;
            gameManager.OnDepthChanged -= HandleDepth;
            gameManager.OnGameOver -= HandleGameOver;
            grid.OnChainChanged -= HandleChain;
            air.OnChanged -= HandleAir;
        }

        private void Start()
        {
            resumeButton.onClick.AddListener(gameManager.Resume);
            restartButton.onClick.AddListener(gameManager.Restart);
            muteButton.onClick.AddListener(ToggleMute);
            RefreshMuteLabel();
            HandleScore(0);
            HandleDepth(0);
            HandleChain(0);
            HandleAir(1f);
        }

        private void Update()
        {
            if (airFraction < config.lowAirFraction)
            {
                float pulse = Mathf.PingPong(Time.unscaledTime * 3f, 1f);
                airBar.color = Color.Lerp(AirLow, AirLowDark, pulse);
            }
        }

        private void HandleState(GameState state)
        {
            titlePanel.SetActive(state == GameState.Title);
            hudPanel.SetActive(state != GameState.Title);
            pausePanel.SetActive(state == GameState.Paused);
            if (state != GameState.GameOver) gameOverPanel.SetActive(false);
            if (state == GameState.Title) titleBestText.text = $"BEST {gameManager.BestDepth} m";
        }

        private void HandleScore(int score) => scoreText.text = score.ToString("N0");

        private void HandleDepth(int depth) => depthText.text = $"{depth} m";

        private void HandleChain(int chain)
        {
            chainText.gameObject.SetActive(chain >= 2);
            chainText.text = $"x{chain}";
        }

        private void HandleAir(float fraction)
        {
            airFraction = fraction;
            airBar.fillAmount = fraction;
            if (fraction >= config.lowAirFraction) airBar.color = AirNormal;
        }

        private void HandleGameOver(GameOverReason reason)
        {
            StartCoroutine(ShowGameOver(reason));
        }

        private IEnumerator ShowGameOver(GameOverReason reason)
        {
            yield return new WaitForSecondsRealtime(config.gameOverPanelDelay);
            causeText.text = reason == GameOverReason.Crushed ? "CRUSHED" : "OUT OF AIR";
            finalDepthText.text = $"DEPTH  {gameManager.Depth} m";
            finalScoreText.text = $"SCORE  {gameManager.Score:N0}";
            finalBestText.text = $"BEST  {gameManager.BestDepth} m  /  {gameManager.BestScore:N0}";
            newBestText.gameObject.SetActive(gameManager.IsNewBest);
            retryText.gameObject.SetActive(false);
            gameOverPanel.SetActive(true);
            yield return new WaitForSecondsRealtime(config.gameOverLockout);
            retryText.gameObject.SetActive(true);
        }

        private void ToggleMute()
        {
            if (AudioManager.Instance == null) return;
            AudioManager.Instance.ToggleMute();
            RefreshMuteLabel();
        }

        private void RefreshMuteLabel()
        {
            bool muted = AudioManager.Instance != null && AudioManager.Instance.Muted;
            muteLabel.text = muted ? "SOUND: OFF" : "SOUND: ON";
        }
    }
}
