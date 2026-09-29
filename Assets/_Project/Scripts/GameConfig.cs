using UnityEngine;

namespace Strata
{
    /// <summary>
    /// Every tunable value of the game in one asset (GDD §3).
    /// No gameplay number lives in a script; everything reads from here.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Strata/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Grid")]
        [Tooltip("Number of columns. The camera is sized so the grid always fills the screen width.")]
        public int gridWidth = 9;
        [Tooltip("How many block colours are in play (max 4). Fewer colours = bigger groups = easier.")]
        [Range(2, 4)] public int colorCount = 4;
        [Tooltip("A merged colour group of this size or more clears.")]
        public int matchThreshold = 4;
        [Tooltip("Rows kept generated below the bottom edge of the camera.")]
        public int bufferRows = 12;
        [Tooltip("Rows kept alive above the top edge of the camera before they are recycled.")]
        public int keepRowsAbove = 6;
        [Tooltip("No hard blocks in the first N rows.")]
        public int safeRowsAtStart = 10;
        [Tooltip("First row that can contain an air capsule.")]
        public int firstCapsuleRow = 6;
        public Color[] blockColors =
        {
            new Color(0.898f, 0.282f, 0.302f), // red
            new Color(0.243f, 0.482f, 0.980f), // blue
            new Color(0.961f, 0.773f, 0.094f), // yellow
            new Color(0.239f, 0.796f, 0.420f), // green
        };

        [Header("Falling (pillar 1: every crush is telegraphed)")]
        [Tooltip("Seconds an unsupported group shakes before it starts to drop.")]
        public float fallDelay = 0.35f;
        [Tooltip("Seconds a falling block (or the player) takes to drop one cell.")]
        public float fallDurationPerCell = 0.08f;
        [Tooltip("Seconds a cleared group flashes before it disappears.")]
        public float clearFlashDuration = 0.15f;

        [Header("Player")]
        public float moveDuration = 0.10f;
        public float digDuration = 0.12f;

        [Header("Air")]
        public float airDrainPerSecond = 1.5f;
        public float airCapsuleRestore = 20f;
        public float hardBlockAirCost = 20f;
        [Tooltip("On average one air capsule every N rows.")]
        public int capsuleEveryNRows = 12;
        [Tooltip("Probability (0..1) that a cell is a hard X-block, by depth in rows.")]
        public AnimationCurve hardBlockChanceByDepth = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(50f, 0.03f), new Keyframe(500f, 0.10f));

        [Header("Score")]
        public int depthScore = 10;
        public int clearScore = 10;

        [Header("Camera")]
        [Tooltip("The camera centre sits this many units below the player (player in the upper part of the screen).")]
        public float cameraOffsetBelowPlayer = 4f;
        [Tooltip("Never zoom in closer than this, so a landscape window still shows enough rows.")]
        public float minOrthoSize = 8f;
        public float cameraSmoothTime = 0.15f;

        [Header("Game flow")]
        public float gameOverPanelDelay = 0.5f;
        public float gameOverLockout = 0.75f;
        [Tooltip("Below this fraction the air bar pulses red.")]
        public float lowAirFraction = 0.25f;

        [Header("Milestones & chain feedback")]
        [Tooltip("A depth milestone pops every N rows (0 = off).")]
        public int milestoneEveryRows = 25;
        public float milestonePopDuration = 0.9f;
        [Tooltip("Each chain step raises the clear sound's pitch by this much.")]
        public float chainPitchStep = 0.12f;
        public float chainPitchMax = 1.8f;

        [Header("Juice")]
        public float shakeAmplitude = 0.06f;
        public float landShakeStrength = 0.05f;
        public float clearShakeStrength = 0.10f;
        public float squashScale = 0.8f;
        public float squashDuration = 0.12f;
    }
}
