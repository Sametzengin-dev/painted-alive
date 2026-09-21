using System.Collections.Generic;
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

        [SerializeField, Range(1, 8)]
        private int maximumParticleInstances = 4;

        private readonly List<ParticleSystem> particlePool = new();
        private int particleCursor;

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
                ParticleSystem particle = GetParticle();

                if (particle != null)
                {
                    particle.transform.SetPositionAndRotation(
                        position,
                        Quaternion.FromToRotation(
                            Vector3.up,
                            normal.sqrMagnitude > 0.0001f
                                ? normal.normalized
                                : Vector3.up));

                    particle.transform.localScale =
                        Vector3.one *
                        Mathf.Lerp(
                            0.85f,
                            1.45f,
                            Mathf.Clamp01(normalizedPower));

                    ParticleSystem.MainModule main = particle.main;
                    main.startColor = paintColor;

                    particle.Stop(
                        true,
                        ParticleSystemStopBehavior.StopEmittingAndClear);
                    particle.Play(true);
                }
            }

            if (audioSource != null)
            {
                audioSource.transform.position = position;
                PlayRandomClip();
            }
        }

        private ParticleSystem GetParticle()
        {
            foreach (ParticleSystem candidate in particlePool)
            {
                if (candidate != null && !candidate.IsAlive(true))
                {
                    return candidate;
                }
            }

            if (particlePool.Count < maximumParticleInstances)
            {
                ParticleSystem created = Instantiate(
                    burstParticlePrefab,
                    transform);
                created.name =
                    $"{burstParticlePrefab.name}_Pooled_" +
                    $"{particlePool.Count + 1:00}";
                particlePool.Add(created);
                return created;
            }

            if (particlePool.Count == 0)
            {
                return null;
            }

            particleCursor %= particlePool.Count;
            ParticleSystem reused = particlePool[particleCursor];
            particleCursor =
                (particleCursor + 1) % particlePool.Count;
            return reused;
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
            maximumParticleInstances =
                Mathf.Clamp(maximumParticleInstances, 1, 8);
        }
    }
}
