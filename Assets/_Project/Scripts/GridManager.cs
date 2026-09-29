using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Strata
{
    /// <summary>
    /// Owns the cell data, the pooled block views, row generation / recycling and the fall-merge cascade.
    /// Coordinates: x = column (0..width-1), y = row, 0 is the surface and y grows DOWNWARD.
    /// World position of a cell: (x - halfWidthOffset, -y).
    /// Rules implemented here are the ones in GDD §3 "Moment-to-moment rules".
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GridManager : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private LevelGenerator generator;
        [SerializeField] private PlayerController player;
        [SerializeField] private Camera gameCamera;
        [SerializeField] private Sprite colorBlockSprite;
        [SerializeField] private Sprite hardBlockSprite;
        [SerializeField] private Sprite capsuleSprite;

        /// <summary>count of cleared blocks, current chain — used for score.</summary>
        public event Action<int, int> OnBlocksCleared;
        /// <summary>colour and cells of one cleared group — used for FX.</summary>
        public event Action<Color, List<Vector2Int>> OnGroupCleared;
        /// <summary>number of cells that just landed.</summary>
        public event Action<int> OnGroupLanded;
        public event Action<int> OnChainChanged;
        public event Action OnDig;

        public static readonly Vector2Int Down = new Vector2Int(0, 1);
        private static readonly Vector2Int[] Neighbours =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        };

        private static float halfWidthOffset;
        public static Vector3 CellToWorld(Vector2Int cell) => new Vector3(cell.x - halfWidthOffset, -cell.y, 0f);

        private readonly Dictionary<int, CellType[]> rows = new Dictionary<int, CellType[]>();
        private readonly Dictionary<Vector2Int, Block> views = new Dictionary<Vector2Int, Block>();
        // Identity of the colour group each cell belonged to before it moved. Two ids inside one group = a merge.
        private readonly Dictionary<Vector2Int, int> groupIds = new Dictionary<Vector2Int, int>();
        private int nextGroupId = 1;
        private int generatedUntil = -1;
        private int lastRecycleLine = int.MinValue;
        private bool frozen;
        private bool cascadeRequested;
        private Coroutine cascade;

        public int Chain { get; private set; }

        private void Awake()
        {
            halfWidthOffset = (config.gridWidth - 1) * 0.5f;
        }

        private void Start()
        {
            EnsureGeneratedForCamera();
        }

        private void LateUpdate()
        {
            if (frozen) return;
            EnsureGeneratedForCamera();
            RecycleAboveCamera();
        }

        // ------------------------------------------------------------------ queries

        public CellType GetCell(Vector2Int c)
        {
            if (c.x < 0 || c.x >= config.gridWidth) return CellType.Wall;
            if (c.y < 0) return CellType.Empty;                 // the air above the surface
            if (c.y > generatedUntil) return CellType.Solid;    // not generated yet: solid floor
            return rows.TryGetValue(c.y, out CellType[] row) ? row[c.x] : CellType.Empty;
        }

        private void SetCell(Vector2Int c, CellType type)
        {
            if (!rows.TryGetValue(c.y, out CellType[] row))
            {
                if (type == CellType.Empty) return;
                row = new CellType[config.gridWidth];
                rows[c.y] = row;
            }
            row[c.x] = type;
        }

        public Color ColorOf(CellType type) => type.IsColor() ? config.blockColors[type.ColorIndex()] : Color.white;

        /// <summary>4-neighbour connected cells of the same colour. Hard blocks and capsules are groups of one.</summary>
        public List<Vector2Int> GetGroup(Vector2Int start)
        {
            var group = new List<Vector2Int>();
            CellType type = GetCell(start);
            if (!type.IsColor())
            {
                group.Add(start);
                return group;
            }
            var visited = new HashSet<Vector2Int> { start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                group.Add(c);
                foreach (Vector2Int d in Neighbours)
                {
                    Vector2Int n = c + d;
                    if (visited.Contains(n) || GetCell(n) != type) continue;
                    visited.Add(n);
                    queue.Enqueue(n);
                }
            }
            return group;
        }

        // ------------------------------------------------------------------ generation & recycling

        private void EnsureGeneratedForCamera()
        {
            float bottomWorldY = gameCamera.transform.position.y - gameCamera.orthographicSize;
            int neededRow = Mathf.CeilToInt(-bottomWorldY) + config.bufferRows;
            while (generatedUntil < neededRow) GenerateRow(++generatedUntil);
        }

        private void GenerateRow(int y)
        {
            CellType[] row = generator.GenerateRow(y);
            rows[y] = row;
            for (int x = 0; x < row.Length; x++)
            {
                if (row[x] == CellType.Empty) continue;
                var c = new Vector2Int(x, y);
                SpawnView(c, row[x]);
                if (row[x].IsColor()) AssignGroupIdForNewCell(c);
            }
        }

        private void SpawnView(Vector2Int c, CellType type)
        {
            Block block = BlockPool.Instance.Get();
            block.Setup(type, c, SpriteFor(type), ColorOf(type));
            views[c] = block;
        }

        private Sprite SpriteFor(CellType type)
        {
            if (type == CellType.Hard) return hardBlockSprite;
            if (type == CellType.Capsule) return capsuleSprite;
            return colorBlockSprite;
        }

        /// <summary>A generated cell joins the id of a same-colour neighbour (generation extends groups, it never "merges" them).</summary>
        private void AssignGroupIdForNewCell(Vector2Int c)
        {
            CellType type = GetCell(c);
            int chosen = 0;
            foreach (Vector2Int d in Neighbours)
            {
                Vector2Int n = c + d;
                if (GetCell(n) != type || !groupIds.TryGetValue(n, out int id)) continue;
                if (chosen == 0) chosen = id;
                else if (id != chosen) Relabel(n, chosen);
            }
            groupIds[c] = chosen != 0 ? chosen : nextGroupId++;
        }

        private void Relabel(Vector2Int start, int newId)
        {
            foreach (Vector2Int cell in GetGroup(start)) groupIds[cell] = newId;
        }

        /// <summary>
        /// Returns blocks far above the camera to the pool. A colour group is recycled only when ALL of it
        /// is above the line, so a hanging group is never cut in a way that changes what supports it (pillar 1).
        /// </summary>
        private void RecycleAboveCamera()
        {
            float topWorldY = gameCamera.transform.position.y + gameCamera.orthographicSize;
            int topRow = Mathf.FloorToInt(-topWorldY);
            int line = topRow - config.keepRowsAbove;
            if (line <= lastRecycleLine) return;
            lastRecycleLine = line;

            var emptiedRows = new List<int>();
            foreach (KeyValuePair<int, CellType[]> kv in rows)
            {
                int y = kv.Key;
                if (y >= line) continue;
                CellType[] row = kv.Value;
                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x] == CellType.Empty) continue;
                    var c = new Vector2Int(x, y);
                    if (!row[x].IsColor())
                    {
                        RemoveCell(c);
                        continue;
                    }
                    List<Vector2Int> group = GetGroup(c);
                    bool allAboveLine = true;
                    foreach (Vector2Int g in group)
                    {
                        if (g.y >= line) { allAboveLine = false; break; }
                    }
                    if (!allAboveLine) continue;
                    foreach (Vector2Int g in group) RemoveCell(g);
                }
                if (IsRowEmpty(row)) emptiedRows.Add(y);
            }
            foreach (int y in emptiedRows) rows.Remove(y);
        }

        private static bool IsRowEmpty(CellType[] row)
        {
            foreach (CellType t in row) if (t != CellType.Empty) return false;
            return true;
        }

        private void RemoveCell(Vector2Int c)
        {
            SetCell(c, CellType.Empty);
            groupIds.Remove(c);
            if (views.TryGetValue(c, out Block block))
            {
                views.Remove(c);
                BlockPool.Instance.Release(block);
            }
        }

        // ------------------------------------------------------------------ digging

        /// <summary>
        /// The player digs a cell. A colour block takes its whole group with it; hard blocks and capsules go alone.
        /// Returns what was there, or Empty if nothing happened.
        /// </summary>
        public CellType Dig(Vector2Int c)
        {
            CellType type = GetCell(c);
            if (!type.IsBlock()) return CellType.Empty;

            if (type.IsColor())
            {
                foreach (Vector2Int g in GetGroup(c)) RemoveCell(g);
            }
            else
            {
                RemoveCell(c);
            }
            OnDig?.Invoke();
            RequestCascade();
            return type;
        }

        // ------------------------------------------------------------------ cascade

        private void RequestCascade()
        {
            if (frozen) return;
            cascadeRequested = true;
            if (cascade == null) cascade = StartCoroutine(CascadeRoutine());
        }

        /// <summary>Game over: stop everything where it is.</summary>
        public void Freeze()
        {
            frozen = true;
            if (cascade != null)
            {
                StopCoroutine(cascade);
                cascade = null;
            }
            StopAllShakes();
        }

        private void StopAllShakes()
        {
            foreach (Block block in views.Values) block.StopShake();
        }

        /// <summary>
        /// shake → drop one cell → re-check → (land) → repeat until settled → clear merged groups → repeat.
        /// Runs while the player keeps moving; dodging a shaking group is the core skill.
        /// </summary>
        private IEnumerator CascadeRoutine()
        {
            var moved = new HashSet<Vector2Int>();           // current cells of blocks that moved this cascade
            var alreadyFalling = new HashSet<Vector2Int>();  // cells that fell in the previous step (no new delay)

            while (cascadeRequested)
            {
                cascadeRequested = false;

                // ---- settle: drop until nothing is unsupported ----
                while (true)
                {
                    List<List<Vector2Int>> falling = FindUnsupportedGroups();
                    if (falling.Count == 0) break;

                    // newly unsupported groups hang for fallDelay first (pillar 1: every crush is telegraphed)
                    bool anyNew = false;
                    foreach (List<Vector2Int> group in falling)
                    {
                        if (ContainsAny(alreadyFalling, group)) continue;
                        anyNew = true;
                        foreach (Vector2Int c in group)
                        {
                            if (views.TryGetValue(c, out Block block)) block.StartShake(config.shakeAmplitude);
                        }
                    }
                    if (anyNew)
                    {
                        yield return new WaitForSeconds(config.fallDelay);
                        StopAllShakes();
                        falling = FindUnsupportedGroups();   // the player may have changed things meanwhile
                        if (falling.Count == 0) break;
                    }

                    var fallingCells = new HashSet<Vector2Int>();
                    foreach (List<Vector2Int> group in falling)
                        foreach (Vector2Int c in group) fallingCells.Add(c);

                    // crush: a block whose next cell is the player's cell ends the run
                    bool crush = false;
                    foreach (Vector2Int c in fallingCells)
                    {
                        if (c + Down == player.Cell) { crush = true; break; }
                    }
                    if (crush)
                    {
                        foreach (Vector2Int c in fallingCells)
                        {
                            if (views.TryGetValue(c, out Block block)) block.AnimateFall(c + Down, config.fallDurationPerCell);
                        }
                        player.Crush();
                        yield break;
                    }

                    // move data, lowest cells first so every target is free by the time its block moves
                    var ordered = new List<Vector2Int>(fallingCells);
                    ordered.Sort((a, b) => b.y.CompareTo(a.y));
                    foreach (Vector2Int c in ordered) MoveCellDown(c);

                    alreadyFalling.Clear();
                    foreach (Vector2Int c in ordered)
                    {
                        Vector2Int n = c + Down;
                        moved.Remove(c);
                        moved.Add(n);
                        alreadyFalling.Add(n);
                        if (views.TryGetValue(n, out Block block)) block.AnimateFall(n, config.fallDurationPerCell);
                    }
                    yield return new WaitForSeconds(config.fallDurationPerCell);

                    // landing feedback for cells that fell and are supported now
                    var stillFalling = new HashSet<Vector2Int>();
                    foreach (List<Vector2Int> group in FindUnsupportedGroups())
                        foreach (Vector2Int c in group) stillFalling.Add(c);
                    int landed = 0;
                    foreach (Vector2Int n in alreadyFalling)
                    {
                        if (stillFalling.Contains(n)) continue;
                        landed++;
                        if (views.TryGetValue(n, out Block block)) block.Squash(config.squashScale, config.squashDuration);
                    }
                    if (landed > 0) OnGroupLanded?.Invoke(landed);
                }

                // ---- merges: only groups that grew by merging clear (a big group that fell intact stays) ----
                if (moved.Count == 0) continue;
                List<List<Vector2Int>> toClear = FindMergedGroups(moved);
                moved.Clear();
                alreadyFalling.Clear();
                if (toClear.Count == 0) continue;

                Chain++;
                OnChainChanged?.Invoke(Chain);
                int cleared = 0;
                foreach (List<Vector2Int> group in toClear)
                {
                    cleared += group.Count;
                    OnGroupCleared?.Invoke(ColorOf(GetCell(group[0])), group);
                    foreach (Vector2Int c in group)
                    {
                        if (views.TryGetValue(c, out Block block)) block.Flash();
                    }
                }
                yield return new WaitForSeconds(config.clearFlashDuration);
                foreach (List<Vector2Int> group in toClear)
                    foreach (Vector2Int c in group) RemoveCell(c);
                OnBlocksCleared?.Invoke(cleared, Chain);
                cascadeRequested = true;   // the removal may have unsupported more groups
            }

            Chain = 0;
            OnChainChanged?.Invoke(Chain);
            cascade = null;
        }

        private static bool ContainsAny(HashSet<Vector2Int> set, List<Vector2Int> cells)
        {
            foreach (Vector2Int c in cells) if (set.Contains(c)) return true;
            return false;
        }

        private void MoveCellDown(Vector2Int c)
        {
            Vector2Int n = c + Down;
            CellType type = GetCell(c);
            SetCell(n, type);
            SetCell(c, CellType.Empty);
            if (groupIds.TryGetValue(c, out int id))
            {
                groupIds.Remove(c);
                groupIds[n] = id;
            }
            if (views.TryGetValue(c, out Block block))
            {
                views.Remove(c);
                views[n] = block;
            }
        }

        /// <summary>
        /// A group is supported when ANY of its cells has something that is not part of the group directly below it.
        /// The player is not support. The ungenerated floor (Solid) is.
        /// </summary>
        private List<List<Vector2Int>> FindUnsupportedGroups()
        {
            var result = new List<List<Vector2Int>>();
            var visited = new HashSet<Vector2Int>();
            foreach (KeyValuePair<int, CellType[]> kv in rows)
            {
                int y = kv.Key;
                CellType[] row = kv.Value;
                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x] == CellType.Empty) continue;
                    var c = new Vector2Int(x, y);
                    if (visited.Contains(c)) continue;
                    List<Vector2Int> group = GetGroup(c);
                    foreach (Vector2Int g in group) visited.Add(g);
                    if (!IsSupported(group)) result.Add(group);
                }
            }
            return result;
        }

        private bool IsSupported(List<Vector2Int> group)
        {
            foreach (Vector2Int c in group)
            {
                Vector2Int below = c + Down;
                if (group.Contains(below)) continue;
                if (GetCell(below) != CellType.Empty) return true;
            }
            return false;
        }

        /// <summary>
        /// Groups (containing a moved cell) of matchThreshold+ that hold two or more pre-fall ids have merged: clear them.
        /// Every other checked group gets a single fresh id, so a merge that was too small to clear is "spent".
        /// </summary>
        private List<List<Vector2Int>> FindMergedGroups(HashSet<Vector2Int> moved)
        {
            var result = new List<List<Vector2Int>>();
            var visited = new HashSet<Vector2Int>();
            foreach (Vector2Int m in moved)
            {
                if (visited.Contains(m) || !GetCell(m).IsColor()) continue;
                List<Vector2Int> group = GetGroup(m);
                foreach (Vector2Int g in group) visited.Add(g);

                var ids = new HashSet<int>();
                foreach (Vector2Int g in group)
                {
                    if (groupIds.TryGetValue(g, out int id)) ids.Add(id);
                }
                if (ids.Count >= 2 && group.Count >= config.matchThreshold)
                {
                    result.Add(group);
                }
                else
                {
                    int unified = nextGroupId++;
                    foreach (Vector2Int g in group) groupIds[g] = unified;
                }
            }
            return result;
        }

        // ------------------------------------------------------------------ gizmos (Session 6)

        private void OnDrawGizmos()
        {
            if (config == null) return;
            float halfWidth = config.gridWidth * 0.5f;
            Gizmos.color = Color.yellow;   // the walls
            Gizmos.DrawLine(new Vector3(-halfWidth, 2f, 0f), new Vector3(-halfWidth, -200f, 0f));
            Gizmos.DrawLine(new Vector3(halfWidth, 2f, 0f), new Vector3(halfWidth, -200f, 0f));
            if (!Application.isPlaying || gameCamera == null) return;

            float topWorldY = gameCamera.transform.position.y + gameCamera.orthographicSize;
            float recycleY = -(Mathf.FloorToInt(-topWorldY) - config.keepRowsAbove) + 0.5f;
            Gizmos.color = Color.red;      // recycle line
            Gizmos.DrawLine(new Vector3(-halfWidth, recycleY, 0f), new Vector3(halfWidth, recycleY, 0f));
            Gizmos.color = Color.cyan;     // generation line
            float generationY = -generatedUntil - 0.5f;
            Gizmos.DrawLine(new Vector3(-halfWidth, generationY, 0f), new Vector3(halfWidth, generationY, 0f));
        }
    }
}
