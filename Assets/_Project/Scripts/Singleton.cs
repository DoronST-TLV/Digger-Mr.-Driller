using UnityEngine;

namespace Strata
{
    /// <summary>
    /// One instance per scene, reachable through Instance. Duplicates destroy themselves in Awake.
    /// Override Persistent to keep the instance across scene reloads (AudioManager).
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        protected virtual bool Persistent => false;

        /// <summary>True for the surviving instance; subclasses skip their setup when this is false.</summary>
        protected bool IsPrimary => Instance == this;

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this as T;
            if (Persistent) DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
