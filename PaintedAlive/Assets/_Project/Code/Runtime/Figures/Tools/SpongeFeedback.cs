using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Figures.Tools
{
    [DisallowMultipleComponent]
    public sealed class SpongeFeedback : MonoBehaviour
    {
        [SerializeField]
        private ParticleSystem absorbParticlePrefab;

        [SerializeField]
        private ParticleSystem dischargeParticlePrefab;

        [SerializeField]
        private AudioSource audioSource;

        [SerializeField]
        private AudioClip[] absorbClips;

        [SerializeField]
        private AudioClip[] dischargeClips;

        [SerializeField]
        private AudioClip[] rejectedClips;

        [SerializeField, Range(0f, 1f)]
        private float volume = 0.72f;

        [SerializeField, Min(0.05f)]
        private float absorbFeedbackInterval = 0.11f;

        [SerializeField, Range(1, 12)]
        private int maximumParticleInstances = 6;

        private readonly List<ParticleSystem> particlePool = new();
        private float nextAbsorbFeedbackTime;
        private int particleCursor;

        private void Awake()
        {
            if (audioSource == null)
            {
                var audioObject = new GameObject("SpongeAudio");
                audioObject.transform.SetParent(transform, false);
                audioSource = audioObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0.65f;
            audioSource.dopplerLevel = 0f;
            audioSource.minDistance = 0.5f;
            audioSource.maxDistance = 15f;
        }

        public void PlayAbsorb(
            Vector3 position,
            Vector3 normal,
            Color color,
            float normalizedFill)
        {
            if (Time.unscaledTime < nextAbsorbFeedbackTime)
            {
                return;
            }

            nextAbsorbFeedbackTime =
                Time.unscaledTime + absorbFeedbackInterval;

            PlayParticle(
                absorbParticlePrefab,
                position,
                normal,
                color,
                Mathf.Lerp(0.75f, 1.15f, normalizedFill));

            MoveAudio(position);
            PlayRandomClip(
                absorbClips,
                0.72f,
                Random.Range(0.96f, 1.05f));
        }

        public void PlayDischarge(
            Vector3 position,
            Vector3 normal,
            Color color,
            float normalizedAmount)
        {
            PlayParticle(
                dischargeParticlePrefab != null
                    ? dischargeParticlePrefab
                    : absorbParticlePrefab,
                position,
                normal,
                color,
                Mathf.Lerp(0.9f, 1.45f, normalizedAmount));

            MoveAudio(position);
            PlayRandomClip(
                dischargeClips,
                1f,
                Random.Range(0.92f, 1.03f));
        }

        public void PlayRejected(Vector3 position)
        {
            MoveAudio(position);
            PlayRandomClip(
                rejectedClips,
                0.45f,
                Random.Range(0.98f, 1.04f));
        }

        private void PlayParticle(
            ParticleSystem prefab,
            Vector3 position,
            Vector3 normal,
            Color color,
            float scale)
        {
            if (prefab == null)
            {
                return;
            }

            ParticleSystem particle = GetParticle(prefab);

            if (particle == null)
            {
                return;
            }

            Vector3 safeNormal =
                normal.sqrMagnitude > 0.0001f
                    ? normal.normalized
                    : Vector3.up;

            particle.transform.SetPositionAndRotation(
                position,
                Quaternion.FromToRotation(
                    Vector3.forward,
                    safeNormal));
            particle.transform.localScale =
                Vector3.one * Mathf.Max(0.01f, scale);

            ParticleSystem.MainModule main = particle.main;
            main.startColor = color;

            particle.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(true);
        }

        private ParticleSystem GetParticle(ParticleSystem prefab)
        {
            foreach (ParticleSystem candidate in particlePool)
            {
                if (candidate != null &&
                    !candidate.IsAlive(true) &&
                    candidate.name.StartsWith(prefab.name))
                {
                    return candidate;
                }
            }

            if (particlePool.Count < maximumParticleInstances)
            {
                ParticleSystem created =
                    Instantiate(prefab, transform);
                created.name =
                    $"{prefab.name}_Pooled_" +
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

        private void MoveAudio(Vector3 position)
        {
            if (audioSource != null)
            {
                audioSource.transform.position = position;
            }
        }

        private void PlayRandomClip(
            AudioClip[] clips,
            float volumeMultiplier,
            float pitch)
        {
            if (clips == null || clips.Length == 0 ||
                audioSource == null)
            {
                return;
            }

            int startIndex = Random.Range(0, clips.Length);

            for (int offset = 0;
                 offset < clips.Length;
                 offset++)
            {
                AudioClip clip =
                    clips[(startIndex + offset) % clips.Length];

                if (clip == null)
                {
                    continue;
                }

                audioSource.pitch = pitch;
                audioSource.PlayOneShot(
                    clip,
                    Mathf.Clamp01(volume * volumeMultiplier));
                return;
            }
        }

        private void OnValidate()
        {
            volume = Mathf.Clamp01(volume);
            absorbFeedbackInterval =
                Mathf.Max(0.05f, absorbFeedbackInterval);
            maximumParticleInstances =
                Mathf.Clamp(maximumParticleInstances, 1, 12);
        }
    }
}
