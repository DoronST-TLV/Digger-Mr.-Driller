using System.Collections;
using UnityEngine;

namespace Strata
{
    /// <summary>
    /// The view of one grid cell: sprite, tint, and the fall / shake / squash animations.
    /// Pooled by BlockPool; holds no game logic (GridManager owns the data).
    /// </summary>
    public class Block : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public Vector2Int Cell { get; private set; }
        public CellType Type { get; private set; }

        private Color baseColor;
        private Coroutine shakeRoutine;
        private bool shaking;

        public void Setup(CellType type, Vector2Int cell, Sprite sprite, Color color)
        {
            Type = type;
            baseColor = color;
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            transform.localScale = Vector3.one;
            name = $"Block {type}";
            SnapTo(cell);
        }

        public void SnapTo(Vector2Int cell)
        {
            Cell = cell;
            transform.position = GridManager.CellToWorld(cell);
        }

        /// <summary>Animate to a new cell. The cell becomes the logical position immediately.</summary>
        public void AnimateFall(Vector2Int cell, float duration)
        {
            StopShake();
            Cell = cell;
            StartCoroutine(Tweens.MoveTo(transform, GridManager.CellToWorld(cell), duration));
        }

        public void StartShake(float amplitude)
        {
            if (shaking) return;
            shaking = true;
            shakeRoutine = StartCoroutine(ShakeRoutine(amplitude));
        }

        public void StopShake()
        {
            if (!shaking) return;
            shaking = false;
            if (shakeRoutine != null) StopCoroutine(shakeRoutine);
            shakeRoutine = null;
            transform.position = GridManager.CellToWorld(Cell);
        }

        private IEnumerator ShakeRoutine(float amplitude)
        {
            Vector3 origin = GridManager.CellToWorld(Cell);
            while (true)
            {
                transform.position = origin + new Vector3(Random.Range(-amplitude, amplitude), 0f, 0f);
                yield return null;
            }
        }

        public void Squash(float scale, float duration)
        {
            StartCoroutine(Tweens.Squash(transform, scale, duration));
        }

        /// <summary>Brighten before being cleared.</summary>
        public void Flash()
        {
            spriteRenderer.color = Color.Lerp(baseColor, Color.white, 0.7f);
        }

        /// <summary>Called by the pool on release: stop everything and go back to a neutral state.</summary>
        public void ResetView()
        {
            StopAllCoroutines();
            shakeRoutine = null;
            shaking = false;
            transform.localScale = Vector3.one;
            spriteRenderer.color = Color.white;
        }
    }
}
