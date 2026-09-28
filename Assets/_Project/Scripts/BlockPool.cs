using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Strata
{
    /// <summary>
    /// Object pool for Block views (Session 6, Unity's ObjectPool&lt;T&gt;).
    /// Rows are rented and returned as the player descends; nothing is instantiated during play.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class BlockPool : Singleton<BlockPool>
    {
        [SerializeField] private Block prefab;
        [SerializeField] private Transform container;
        [SerializeField] private int prewarmCount = 300;

        private ObjectPool<Block> pool;

        public int CountActive => pool.CountActive;
        public int CountAll => pool.CountAll;

        protected override void Awake()
        {
            base.Awake();
            if (!IsPrimary) return;

            pool = new ObjectPool<Block>(
                createFunc: CreateBlock,
                actionOnGet: block => block.gameObject.SetActive(true),
                actionOnRelease: block =>
                {
                    block.ResetView();
                    block.gameObject.SetActive(false);
                },
                actionOnDestroy: block => Destroy(block.gameObject),
                collectionCheck: false,
                defaultCapacity: prewarmCount,
                maxSize: prewarmCount * 2);

            Prewarm();
        }

        private Block CreateBlock()
        {
            Block block = Instantiate(prefab, container);
            block.gameObject.SetActive(false);
            return block;
        }

        private void Prewarm()
        {
            var taken = new List<Block>(prewarmCount);
            for (int i = 0; i < prewarmCount; i++) taken.Add(pool.Get());
            foreach (Block block in taken) pool.Release(block);
        }

        public Block Get() => pool.Get();

        public void Release(Block block) => pool.Release(block);
    }
}
