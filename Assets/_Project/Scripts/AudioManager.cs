using UnityEngine;

namespace Strata
{
    public enum SfxId { Dig = 0, Land = 1, Clear = 2, Crush = 3, Capsule = 4, GameOver = 5 }

    /// <summary>One-shot sound effects and the mute setting. Persists across the restart reload (Session 3 pattern).</summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [SerializeField] private AudioSource source;
        [Tooltip("Index = SfxId: Dig, Land, Clear, Crush, Capsule, GameOver")]
        [SerializeField] private AudioClip[] clips;

        private const string MuteKey = "Muted";

        protected override bool Persistent => true;

        public bool Muted { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (!IsPrimary) return;
            Muted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
            source.mute = Muted;
        }

        public void Play(SfxId id)
        {
            int index = (int)id;
            if (clips == null || index >= clips.Length || clips[index] == null) return;
            source.PlayOneShot(clips[index]);
        }

        public void ToggleMute()
        {
            Muted = !Muted;
            source.mute = Muted;
            PlayerPrefs.SetInt(MuteKey, Muted ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
