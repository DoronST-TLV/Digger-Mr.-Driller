using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Strata
{
    /// <summary>
    /// The state machine (Title → Playing ⇄ Paused → GameOver), score, depth and best results.
    /// Everything else subscribes to its events instead of polling it.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public class GameManager : Singleton<GameManager>
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private GridManager grid;
        [SerializeField] private PlayerController player;
        [SerializeField] private AirMeter air;

        public GameState State { get; private set; } = GameState.Title;
        public int StateChangedFrame { get; private set; } = -1;
        public int Score { get; private set; }
        public int Depth { get; private set; }
        public int BestDepth { get; private set; }
        public int BestScore { get; private set; }
        public bool IsNewBest { get; private set; }
        public bool CanRestart { get; private set; }

        public event Action<GameState> OnStateChanged;
        public event Action<int> OnScoreChanged;
        public event Action<int> OnDepthChanged;
        public event Action<GameOverReason> OnGameOver;

        private const string BestDepthKey = "BestDepth";
        private const string BestScoreKey = "BestScore";

        protected override void Awake()
        {
            base.Awake();
            if (!IsPrimary) return;
            Application.targetFrameRate = 60;
            Time.timeScale = 1f;
            BestDepth = PlayerPrefs.GetInt(BestDepthKey, 0);
            BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        }

        private void Start()
        {
            air.OnEmpty += HandleAirEmpty;
            player.OnCrushed += HandleCrushed;
            player.OnDepthReached += HandleDepthReached;
            grid.OnBlocksCleared += HandleBlocksCleared;
            SetState(GameState.Title);
        }

        private void OnDisable()
        {
            if (air != null) air.OnEmpty -= HandleAirEmpty;
            if (player != null)
            {
                player.OnCrushed -= HandleCrushed;
                player.OnDepthReached -= HandleDepthReached;
            }
            if (grid != null) grid.OnBlocksCleared -= HandleBlocksCleared;
        }

        private void Update()
        {
            switch (State)
            {
                case GameState.Title:
                    if (AnyTap()) StartGame();
                    break;
                case GameState.Playing:
                    if (Input.GetKeyDown(KeyCode.Escape)) Pause();   // Escape is also the Android Back button
                    break;
                case GameState.Paused:
                    if (Input.GetKeyDown(KeyCode.Escape)) Resume();
                    break;
                case GameState.GameOver:
                    if (CanRestart && AnyTap()) Restart();
                    break;
            }
        }

        private static bool AnyTap() => Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);

        // ------------------------------------------------------------------ flow

        public void StartGame()
        {
            if (State != GameState.Title) return;
            Score = 0;
            Depth = 0;
            IsNewBest = false;
            OnScoreChanged?.Invoke(Score);
            OnDepthChanged?.Invoke(Depth);
            SetState(GameState.Playing);
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void GameOver(GameOverReason reason)
        {
            if (State == GameState.GameOver) return;
            grid.Freeze();
            player.Freeze();
            air.Stop();
            SaveBest();
            SetState(GameState.GameOver);
            OnGameOver?.Invoke(reason);
            StartCoroutine(GameOverRoutine());
        }

        private IEnumerator GameOverRoutine()
        {
            CanRestart = false;
            yield return new WaitForSecondsRealtime(config.gameOverPanelDelay + config.gameOverLockout);
            CanRestart = true;
        }

        private void SetState(GameState newState)
        {
            State = newState;
            StateChangedFrame = Time.frameCount;
            OnStateChanged?.Invoke(newState);
        }

        // ------------------------------------------------------------------ mobile: backgrounding pauses the run

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) Pause();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && !Application.isEditor) Pause();
        }

        // ------------------------------------------------------------------ score

        private void HandleDepthReached(int depth)
        {
            if (depth <= Depth) return;
            AddScore((depth - Depth) * config.depthScore);
            Depth = depth;
            OnDepthChanged?.Invoke(Depth);
        }

        private void HandleBlocksCleared(int count, int chain)
        {
            AddScore(count * config.clearScore * Mathf.Max(chain, 1));
        }

        private void AddScore(int amount)
        {
            Score += amount;
            OnScoreChanged?.Invoke(Score);
        }

        private void HandleCrushed() => GameOver(GameOverReason.Crushed);

        private void HandleAirEmpty() => GameOver(GameOverReason.OutOfAir);

        private void SaveBest()
        {
            if (Depth > BestDepth)
            {
                BestDepth = Depth;
                IsNewBest = true;
                PlayerPrefs.SetInt(BestDepthKey, BestDepth);
            }
            if (Score > BestScore)
            {
                BestScore = Score;
                IsNewBest = true;
                PlayerPrefs.SetInt(BestScoreKey, BestScore);
            }
            PlayerPrefs.Save();
        }
    }
}
