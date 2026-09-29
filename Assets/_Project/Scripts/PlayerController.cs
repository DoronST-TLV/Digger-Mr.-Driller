using System;
using System.Collections;
using UnityEngine;

namespace Strata
{
    /// <summary>
    /// The digger. Lives in exactly one cell; steps or digs left/right/down, falls into gaps, dies when crushed.
    /// Holds a one-slot input buffer: the newest intent replaces the previous one and runs when the player is free.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private GridManager grid;
        [SerializeField] private AirMeter air;
        [SerializeField] private SpriteRenderer spriteRenderer;

        public Vector2Int Cell { get; private set; }

        public event Action OnCrushed;
        public event Action OnLanded;
        public event Action OnCapsuleCollected;
        /// <summary>depth in rows (cell.y + 1), fired only when a new maximum is reached.</summary>
        public event Action<int> OnDepthReached;

        private Vector2Int? bufferedIntent;
        private bool busy;
        private bool falling;
        private bool frozen;
        private int maxDepth;

        private void Awake()
        {
            Cell = new Vector2Int(config.gridWidth / 2, -1);   // standing on the surface row
            transform.position = GridManager.CellToWorld(Cell);
        }

        public void SetIntent(Vector2Int direction)
        {
            if (frozen) return;
            bufferedIntent = direction;
        }

        private void Update()
        {
            if (frozen || busy || falling) return;
            if (GameManager.Instance.State != GameState.Playing)
            {
                bufferedIntent = null;
                return;
            }
            if (grid.GetCell(Cell + GridManager.Down) == CellType.Empty)
            {
                StartCoroutine(FallRoutine());
                return;
            }
            if (bufferedIntent == null) return;
            Vector2Int direction = bufferedIntent.Value;
            bufferedIntent = null;
            StartCoroutine(ActRoutine(direction));
        }

        private IEnumerator ActRoutine(Vector2Int direction)
        {
            busy = true;
            Vector2Int target = Cell + direction;
            CellType targetType = grid.GetCell(target);

            if (direction.x != 0) spriteRenderer.flipX = direction.x < 0;

            if (targetType == CellType.Wall || targetType == CellType.Solid)
            {
                busy = false;       // edge of the world: ignored, no animation
                yield break;
            }

            air.Begin();            // the first real action starts the clock

            if (targetType == CellType.Empty)
            {
                if (direction != GridManager.Down)  // down into empty never happens: gravity already handled it
                {
                    SetCell(target);
                    yield return Tweens.MoveTo(transform, GridManager.CellToWorld(target), config.moveDuration);
                }
            }
            else
            {
                yield return BumpRoutine(direction);
                CellType dug = grid.Dig(target);
                if (dug == CellType.Hard) air.Add(-config.hardBlockAirCost);
                else if (dug == CellType.Capsule)
                {
                    air.Add(config.airCapsuleRestore);
                    OnCapsuleCollected?.Invoke();
                }
            }
            busy = false;
        }

        private IEnumerator FallRoutine()
        {
            falling = true;
            while (!frozen && grid.GetCell(Cell + GridManager.Down) == CellType.Empty)
            {
                Vector2Int target = Cell + GridManager.Down;
                SetCell(target);
                yield return Tweens.MoveTo(transform, GridManager.CellToWorld(target), config.fallDurationPerCell);
            }
            falling = false;
            if (!frozen) OnLanded?.Invoke();
        }

        /// <summary>A short push toward the block being dug.</summary>
        private IEnumerator BumpRoutine(Vector2Int direction)
        {
            Vector3 origin = GridManager.CellToWorld(Cell);
            Vector3 pushed = origin + new Vector3(direction.x, -direction.y, 0f) * 0.2f;
            float half = config.digDuration * 0.5f;
            yield return Tweens.MoveTo(transform, pushed, half);
            yield return Tweens.MoveTo(transform, origin, half);
        }

        private void SetCell(Vector2Int cell)
        {
            Cell = cell;
            int depth = cell.y + 1;
            if (depth > maxDepth)
            {
                maxDepth = depth;
                OnDepthReached?.Invoke(depth);
            }
        }

        /// <summary>Called by the grid when a falling block enters this cell.</summary>
        public void Crush()
        {
            if (frozen) return;
            Freeze();
            transform.localScale = new Vector3(1.3f, 0.4f, 1f);
            OnCrushed?.Invoke();
        }

        public void Freeze()
        {
            frozen = true;
            StopAllCoroutines();
            busy = false;
            falling = false;
        }
    }
}
