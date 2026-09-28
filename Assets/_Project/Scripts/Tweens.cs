using System.Collections;
using UnityEngine;

namespace Strata
{
    /// <summary>Small coroutine tweens shared by blocks and the player (Session 5: Lerp over time).</summary>
    public static class Tweens
    {
        public static IEnumerator Squash(Transform target, float scale, float duration)
        {
            float half = duration * 0.5f;
            Vector3 normal = Vector3.one;
            Vector3 squashed = new Vector3(2f - scale, scale, 1f);
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                target.localScale = Vector3.Lerp(normal, squashed, t / half);
                yield return null;
            }
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                target.localScale = Vector3.Lerp(squashed, normal, t / half);
                yield return null;
            }
            target.localScale = normal;
        }

        public static IEnumerator MoveTo(Transform target, Vector3 to, float duration)
        {
            Vector3 from = target.position;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                target.position = Vector3.Lerp(from, to, t / duration);
                yield return null;
            }
            target.position = to;
        }
    }
}
