using UnityEngine;

namespace PaintedAlive.Figures.Tools
{
    [DisallowMultipleComponent]
    public sealed class SpongeBurstFeedback : MonoBehaviour
    {
        [SerializeField]
        private ParticleSystem burstParticlePrefab;

        [SerializeField]
        private AudioSource audioSource;

        [SerializeField]
        private AudioClip[] burstClips;

        [SerializeField, Range(0f, 1f)]
        private float volume = 0.9f;

        private void Awake()
        {
            if (audioSource == null)
            {
                var audioObject =
                    new GameObject("SpongeBurstAudio");
                audioObject.transform.SetParent(transform, false);
                audioSource =
                    audioObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.82f;
            audioSource.dopplerLevel = 0f;
            audioSource.minDistance = 0.7f;
            audioSource.maxDistance = 22f;
        }

        public void PlayBurst(
            Vector3 position,
            Vector3 normal,
            Color paintColor,
            float normalizedPower)
        {
            if (burstParticlePrefab != null)
            {
                ParticleSystem particle =
                    Instantiate(
                        burstParticlePrefab,
                        position,
                        Quaternion.FromToRotation(
                            Vector3.up,
                            normal.sqrMagnitude > 0.0001f
                                ? normal.normalized
                                : Vector3.up));

                particle.name = "VFX_SpongeBurst_Runtime";
                particle.transform.localScale =
                    Vector3.one *
                    Mathf.Lerp(
                        0.85f,
                        1.45f,
                        Mathf.Clamp01(normalizedPower));

                ParticleSystem.MainModule main = particle.main;
                main.startColor = paintColor;
                particle.Play(true);

                float lifetime =
                    main.duration +
                    main.startLifetime.constantMax + 0.25f;
                Destroy(particle.gameObject, lifetime);
            }

            if (audioSource != null)
            {
                audioSource.transform.position = position;
                PlayRandomClip();
            }
        }

        private void PlayRandomClip()
        {
            if (burstClips == null ||
                burstClips.Length == 0 ||
                audioSource == null)
            {
                return;
            }

            int startIndex =
                Random.Range(0, burstClips.Length);

            for (int i = 0; i < burstClips.Length; i++)
            {
                AudioClip clip =
                    burstClips[
                        (startIndex + i) % burstClips.Length];

                if (clip == null)
                {
                    continue;
                }

                audioSource.pitch = Random.Range(0.92f, 1.04f);
                audioSource.PlayOneShot(clip, volume);
                return;
            }
        }

        private void OnValidate()
        {
            volume = Mathf.Clamp01(volume);
        }
    }
}
