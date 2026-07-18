using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Figures.Tools
{
    [DisallowMultipleComponent]
    public sealed class FixativeSprayFeedback : MonoBehaviour
    {
        [Header("Particle Prefabs")]
        [SerializeField]
        private ParticleSystem sprayParticlePrefab;

        [SerializeField]
        private ParticleSystem impactParticlePrefab;

        [SerializeField, Range(1, 12)]
        private int maximumInstancesPerPool = 4;

        [Header("Audio")]
        [SerializeField]
        private AudioSource audioSource;

        [SerializeField]
        private AudioClip[] applicationClips;

        [SerializeField]
        private AudioClip[] rejectedClips;

        [SerializeField, Range(0f, 1f)]
        private float volume = 0.65f;

        [SerializeField, Min(0.05f)]
        private float audioInterval = 0.22f;

        private readonly List<ParticleSystem> sprayPool = new();
        private readonly List<ParticleSystem> impactPool = new();

        private int sprayCursor;
        private int impactCursor;
        private float nextApplicationAudioTime;
        private float nextRejectedAudioTime;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0.65f;
            audioSource.dopplerLevel = 0f;
            audioSource.minDistance = 0.5f;
            audioSource.maxDistance = 12f;
        }

        public void PlayApplied(
            Vector3 origin,
            Vector3 direction,
            Vector3 hitPoint,
            Vector3 surfaceNormal,
            float saturation,
            bool becameDry)
        {
            PlayParticle(
                sprayParticlePrefab,
                sprayPool,
                ref sprayCursor,
                origin,
                direction,
                1f);

            PlayParticle(
                impactParticlePrefab,
                impactPool,
                ref impactCursor,
                hitPoint,
                surfaceNormal,
                Mathf.Lerp(
                    0.7f,
                    becameDry ? 1.5f : 1.1f,
                    saturation));

            if (Time.unscaledTime >=
                nextApplicationAudioTime)
            {
                nextApplicationAudioTime =
                    Time.unscaledTime + audioInterval;

                PlayRandomClip(
                    applicationClips,
                    becameDry ? 1f : 0.75f,
                    Random.Range(0.95f, 1.08f));
            }
        }

        public void PlayMiss(
            Vector3 origin,
            Vector3 direction)
        {
            PlayParticle(
                sprayParticlePrefab,
                sprayPool,
                ref sprayCursor,
                origin,
                direction,
                0.8f);
        }

        public void PlayRejected(
            Vector3 position,
            Vector3 direction)
        {
            PlayParticle(
                sprayParticlePrefab,
                sprayPool,
                ref sprayCursor,
                position,
                direction,
                0.55f);

            if (Time.unscaledTime >= nextRejectedAudioTime)
            {
                nextRejectedAudioTime =
                    Time.unscaledTime + 0.35f;

                PlayRandomClip(
                    rejectedClips,
                    0.5f,
                    Random.Range(0.97f, 1.03f));
            }
        }

        private void PlayParticle(
            ParticleSystem prefab,
            List<ParticleSystem> pool,
            ref int cursor,
            Vector3 position,
            Vector3 direction,
            float scale)
        {
            if (prefab == null)
            {
                return;
            }

            ParticleSystem instance =
                GetParticleInstance(
                    prefab,
                    pool,
                    ref cursor);

            if (instance == null)
            {
                return;
            }

            Vector3 safeDirection =
                direction.sqrMagnitude > 0.0001f
                    ? direction.normalized
                    : Vector3.forward;

            Transform instanceTransform = instance.transform;
            instanceTransform.position = position;
            instanceTransform.rotation =
                Quaternion.FromToRotation(
                    Vector3.forward,
                    safeDirection);

            instanceTransform.localScale =
                Vector3.one * Mathf.Max(0.01f, scale);

            if (!instance.gameObject.activeSelf)
            {
                instance.gameObject.SetActive(true);
            }

            instance.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            instance.Play(true);
        }

        private ParticleSystem GetParticleInstance(
            ParticleSystem prefab,
            List<ParticleSystem> pool,
            ref int cursor)
        {
            foreach (ParticleSystem candidate in pool)
            {
                if (candidate != null &&
                    !candidate.IsAlive(true))
                {
                    return candidate;
                }
            }

            if (pool.Count < maximumInstancesPerPool)
            {
                ParticleSystem created =
                    Instantiate(prefab, transform);

                created.name =
                    $"{prefab.name}_Pooled_{pool.Count + 1:00}";

                pool.Add(created);
                return created;
            }

            if (pool.Count == 0)
            {
                return null;
            }

            cursor %= pool.Count;
            ParticleSystem reused = pool[cursor];
            cursor = (cursor + 1) % pool.Count;
            return reused;
        }

        private void PlayRandomClip(
            AudioClip[] clips,
            float volumeMultiplier,
            float pitch)
        {
            AudioClip clip = GetRandomClip(clips);

            if (clip == null || audioSource == null)
            {
                return;
            }

            audioSource.pitch = pitch;
            audioSource.PlayOneShot(
                clip,
                Mathf.Clamp01(
                    volume * volumeMultiplier));
        }

        private static AudioClip GetRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            int startIndex = Random.Range(0, clips.Length);

            for (int offset = 0;
                 offset < clips.Length;
                 offset++)
            {
                AudioClip candidate =
                    clips[(startIndex + offset) % clips.Length];

                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void OnValidate()
        {
            maximumInstancesPerPool =
                Mathf.Clamp(maximumInstancesPerPool, 1, 12);

            volume = Mathf.Clamp01(volume);
            audioInterval = Mathf.Max(0.05f, audioInterval);
        }
    }
}
