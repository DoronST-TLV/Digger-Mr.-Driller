using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

namespace Strata
{
    /// <summary>Pooled particle bursts for cleared blocks (second use of ObjectPool&lt;T&gt;).</summary>
    public class BurstPool : Singleton<BurstPool>
    {
        [SerializeField] private ParticleSystem prefab;
        [SerializeField] private Transform container;

        private ObjectPool<ParticleSystem> pool;

        protected override void Awake()
        {
            base.Awake();
            if (!IsPrimary) return;
            pool = new ObjectPool<ParticleSystem>(
                createFunc: () => Instantiate(prefab, container),
                actionOnGet: ps => ps.gameObject.SetActive(true),
                actionOnRelease: ps => ps.gameObject.SetActive(false),
                actionOnDestroy: ps => Destroy(ps.gameObject),
                collectionCheck: false,
                defaultCapacity: 16,
                maxSize: 64);
        }

        public void Play(Vector3 position, Color color)
        {
            ParticleSystem ps = pool.Get();
            ps.transform.position = position;
            ParticleSystem.MainModule main = ps.main;
            main.startColor = color;
            ps.Clear();
            ps.Play();
            StartCoroutine(ReleaseWhenDone(ps));
        }

        private IEnumerator ReleaseWhenDone(ParticleSystem ps)
        {
            yield return new WaitForSeconds(ps.main.duration + ps.main.startLifetime.constantMax);
            pool.Release(ps);
        }
    }
}
