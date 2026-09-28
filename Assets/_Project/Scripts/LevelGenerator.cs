using UnityEngine;

namespace Strata
{
    /// <summary>
    /// Produces one row of cells at a time from depth-based rules:
    /// colours everywhere, hard X-blocks after the safe rows (chance grows with depth), an air capsule now and then.
    /// Big same-colour groups are allowed on purpose — they are what the player digs.
    /// </summary>
    public class LevelGenerator : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("0 = random every run. Set a number to reproduce a run while debugging.")]
        [SerializeField] private int seed = 0;

        private System.Random rng;

        private void Awake()
        {
            rng = seed == 0 ? new System.Random() : new System.Random(seed);
        }

        public CellType[] GenerateRow(int y)
        {
            int width = config.gridWidth;
            var row = new CellType[width];

            float hardChance = y < config.safeRowsAtStart ? 0f : config.hardBlockChanceByDepth.Evaluate(y);
            float capsuleChance = y < config.firstCapsuleRow ? 0f : 1f / (config.capsuleEveryNRows * width);

            for (int x = 0; x < width; x++)
            {
                double roll = rng.NextDouble();
                if (roll < capsuleChance) row[x] = CellType.Capsule;
                else if (roll < capsuleChance + hardChance) row[x] = CellType.Hard;
                else row[x] = CellTypeExtensions.FromColorIndex(rng.Next(config.colorCount));
            }
            return row;
        }
    }
}
