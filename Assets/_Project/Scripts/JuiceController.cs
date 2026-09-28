using System.Collections.Generic;
using UnityEngine;

namespace Strata
{
    /// <summary>
    /// Turns grid and player events into feedback: sound, screen shake, particle bursts (pillar 3 juice spec, GDD §6).
    /// Keeps GridManager and PlayerController free of presentation code.
    /// </summary>
    public class JuiceController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private GridManager grid;
        [SerializeField] private PlayerController player;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ScreenShake screenShake;

        private void OnEnable()
        {
            grid.OnDig += HandleDig;
            grid.OnGroupLanded += HandleLanded;
            grid.OnGroupCleared += HandleCleared;
            player.OnCapsuleCollected += HandleCapsule;
            player.OnLanded += HandlePlayerLanded;
            gameManager.OnGameOver += HandleGameOver;
        }

        private void OnDisable()
        {
            grid.OnDig -= HandleDig;
            grid.OnGroupLanded -= HandleLanded;
            grid.OnGroupCleared -= HandleCleared;
            player.OnCapsuleCollected -= HandleCapsule;
            player.OnLanded -= HandlePlayerLanded;
            gameManager.OnGameOver -= HandleGameOver;
        }

        private static void Play(SfxId id)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.Play(id);
        }

        private void HandleDig() => Play(SfxId.Dig);

        private void HandleCapsule() => Play(SfxId.Capsule);

        private void HandlePlayerLanded()
        {
            Play(SfxId.Land);
            screenShake.Shake(config.landShakeStrength * 0.5f, 0.1f);
        }

        private void HandleLanded(int cellCount)
        {
            Play(SfxId.Land);
            float weight = Mathf.Clamp(cellCount, 1, 6) / 3f;
            screenShake.Shake(config.landShakeStrength * weight, 0.15f);
        }

        private void HandleCleared(Color color, List<Vector2Int> cells)
        {
            Play(SfxId.Clear);
            screenShake.Shake(config.clearShakeStrength, 0.2f);
            if (BurstPool.Instance == null) return;
            foreach (Vector2Int c in cells) BurstPool.Instance.Play(GridManager.CellToWorld(c), color);
        }

        private void HandleGameOver(GameOverReason reason)
        {
            Play(reason == GameOverReason.Crushed ? SfxId.Crush : SfxId.GameOver);
            screenShake.Shake(config.clearShakeStrength * 2f, 0.3f);
        }
    }
}
