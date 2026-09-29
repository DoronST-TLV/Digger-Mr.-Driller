using UnityEngine;

namespace Strata
{
    public enum SfxId { Dig = 0, Land = 1, Clear = 2, Crush = 3, Capsule = 4, GameOver = 5, Milestone = 6 }

    /// <summary>One-shot sound effects and the mute setting. Persists across the restart reload (Session 3 pattern).</summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [SerializeField] private AudioSource source;
        [Tooltip("Second source so a pitched clip does not bend the ones already playing.")]
        [SerializeField] private AudioSource pitchedSource;
        [Tooltip("Index = SfxId: Dig, Land, Clear, Crush, Capsule, GameOver, Milestone")]
        [SerializeField] private AudioClip[] clips;

        private const string MuteKey = "Muted";

        protected override bool Persistent => true;

        public bool Muted { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (!IsPrimary) return;
            Muted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
            ApplyMute();
        }

        public void Play(SfxId id)
        {
            AudioClip clip = ClipFor(id);
            if (clip != null) source.PlayOneShot(clip);
        }

        /// <summary>Same clip, shifted pitch — used to make a chain audibly climb.</summary>
        public void Play(SfxId id, float pitch)
        {
            AudioClip clip = ClipFor(id);
            if (clip == null) return;
            pitchedSource.pitch = pitch;
            pitchedSource.PlayOneShot(clip);
        }

        private AudioClip ClipFor(SfxId id)
        {
            int index = (int)id;
            if (clips == null || index >= clips.Length) return null;
            return clips[index];
        }

        public void ToggleMute()
        {
            Muted = !Muted;
            ApplyMute();
            PlayerPrefs.SetInt(MuteKey, Muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ApplyMute()
        {
            source.mute = Muted;
            if (pitchedSource != null) pitchedSource.mute = Muted;
        }
    }
}
