using System;
using UnityEngine;

namespace Strata
{
    /// <summary>Air runs out over time; capsules refill it, hard blocks cost it. Empty air ends the run.</summary>
    public class AirMeter : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        public float Air { get; private set; } = 100f;
        public bool IsDraining { get; private set; }

        /// <summary>fraction 0..1</summary>
        public event Action<float> OnChanged;
        public event Action OnEmpty;

        private bool stopped;

        /// <summary>Drain starts with the player's first action, not on the title screen.</summary>
        public void Begin()
        {
            if (!stopped) IsDraining = true;
        }

        public void Stop()
        {
            IsDraining = false;
            stopped = true;
        }

        private void Update()
        {
            if (!IsDraining) return;
            Add(-config.airDrainPerSecond * Time.deltaTime);   // deltaTime is 0 while paused
        }

        public void Add(float amount)
        {
            if (stopped) return;
            Air = Mathf.Clamp(Air + amount, 0f, 100f);
            OnChanged?.Invoke(Air / 100f);
            if (Air <= 0f)
            {
                Stop();
                OnEmpty?.Invoke();
            }
        }
    }
}
