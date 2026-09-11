using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PulmoTrainer.Training
{
    [DisallowMultipleComponent]
    public sealed class TrainingEndSequence : MonoBehaviour
    {
        [SerializeField] private AudioSource narrationSource;
        [SerializeField] private AudioClip endingNarration;
        [SerializeField, Min(0f)] private float postNarrationDelay = 0.5f;

        private static TrainingEndSequence activeSequence;
        private EventSystem blockedEventSystem;
        private bool restoreEventSystem;

        public bool IsRunning { get; private set; }
        public static bool IsInputBlocked => activeSequence != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { activeSequence = null; }

        public void Begin()
        {
            if (IsRunning || activeSequence != null) return;
            IsRunning = true;
            activeSequence = this;
            blockedEventSystem = EventSystem.current;
            restoreEventSystem = blockedEventSystem != null && blockedEventSystem.enabled;
            if (restoreEventSystem) blockedEventSystem.enabled = false;
            StartCoroutine(PlayAndQuit());
        }

        private IEnumerator PlayAndQuit()
        {
            if (endingNarration != null)
            {
                // Use a dedicated source if the optional assigned source is unavailable.
                if (narrationSource == null || !narrationSource.gameObject.activeInHierarchy)
                    narrationSource = gameObject.AddComponent<AudioSource>();
                narrationSource.enabled = true;
                narrationSource.playOnAwake = false;
                narrationSource.loop = false;
                narrationSource.spatialBlend = 0f;
                narrationSource.ignoreListenerPause = true;
                narrationSource.clip = endingNarration;
                narrationSource.Play();
                yield return null;
                while (narrationSource != null && narrationSource.isPlaying)
                    yield return null;
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, postNarrationDelay));
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void OnDestroy()
        {
            if (activeSequence != this) return;
            if (restoreEventSystem && blockedEventSystem != null) blockedEventSystem.enabled = true;
            activeSequence = null;
        }
    }
}
