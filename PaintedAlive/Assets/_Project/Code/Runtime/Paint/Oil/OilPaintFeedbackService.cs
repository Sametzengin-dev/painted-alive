using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace PaintedAlive.Paint
{
    [DisallowMultipleComponent]
    public sealed class OilPaintFeedbackService : MonoBehaviour
    {
        [Header("Particle Prefabs")]
        [SerializeField]
        private ParticleSystem knifeCutParticlePrefab;

        [SerializeField]
        private ParticleSystem fractureParticlePrefab;

        [SerializeField, Range(1, 32)]
        private int maximumParticlesPerPool = 10;

        [Header("Audio")]
        [SerializeField]
        private AudioClip[] knifeCutClips;

        [SerializeField]
        private AudioClip[] fractureClips;

        [SerializeField]
        private AudioClip[] blockedClips;

        [SerializeField]
        private AudioMixerGroup outputMixerGroup;

        [SerializeField, Range(1, 24)]
        private int maximumAudioSources = 8;

        [SerializeField, Range(0f, 1f)]
        private float masterFeedbackVolume = 0.9f;

        [SerializeField, Min(1f)]
        private float maximumHearingDistance = 18f;

        private readonly List<ParticleSystem> knifeParticlePool =
            new();

        private readonly List<ParticleSystem> fractureParticlePool =
            new();

        private readonly List<AudioSource> audioPool = new();

        private int knifeParticleCursor;
        private int fractureParticleCursor;
        private int audioCursor;

        public void PlayKnifeCut(
            Vector3 position,
            Vector3 surfaceNormal,
            float intensity)
        {
            float safeIntensity =
                Mathf.Clamp(intensity, 0.35f, 1.5f);

            PlayParticle(
                knifeCutParticlePrefab,
                knifeParticlePool,
                ref knifeParticleCursor,
                position,
                surfaceNormal,
                Mathf.Lerp(0.7f, 1.25f, safeIntensity));

            PlayRandomClip(
                knifeCutClips,
                position,
                Mathf.Lerp(0.55f, 1f, safeIntensity),
                Random.Range(0.94f, 1.08f));
        }

        public void PlayFracture(
            Vector3 position,
            float effectRadius,
            int fragmentCount)
        {
            float countScale =
                Mathf.InverseLerp(2f, 12f, fragmentCount);

            float scale =
                Mathf.Clamp(
                    effectRadius * 0.55f +
                    countScale * 0.5f,
                    0.8f,
                    2.25f);

            PlayParticle(
                fractureParticlePrefab,
                fractureParticlePool,
                ref fractureParticleCursor,
                position,
                Vector3.up,
                scale);

            PlayRandomClip(
                fractureClips,
                position,
                Mathf.Lerp(0.7f, 1f, countScale),
                Random.Range(0.88f, 1.02f));
        }

        public void PlayBlocked(Vector3 position)
        {
            PlayRandomClip(
                blockedClips,
                position,
                0.55f,
                Random.Range(0.96f, 1.04f));
        }

        private void PlayParticle(
            ParticleSystem prefab,
            List<ParticleSystem> pool,
            ref int cursor,
            Vector3 position,
            Vector3 surfaceNormal,
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

            Transform instanceTransform = instance.transform;
            instanceTransform.position = position;

            Vector3 safeNormal =
                surfaceNormal.sqrMagnitude > 0.0001f
                    ? surfaceNormal.normalized
                    : Vector3.up;

            instanceTransform.rotation =
                Quaternion.FromToRotation(
                    Vector3.forward,
                    safeNormal);

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
            for (int i = 0; i < pool.Count; i++)
            {
                ParticleSystem candidate = pool[i];

                if (candidate != null &&
                    !candidate.IsAlive(true))
                {
                    return candidate;
                }
            }

            if (pool.Count < maximumParticlesPerPool)
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
            Vector3 position,
            float volume,
            float pitch)
        {
            AudioClip clip = GetRandomClip(clips);

            if (clip == null)
            {
                return;
            }

            AudioSource source = GetAudioSource();

            if (source == null)
            {
                return;
            }

            source.transform.position = position;
            source.clip = clip;
            source.volume =
                Mathf.Clamp01(
                    volume * masterFeedbackVolume);

            source.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
            source.Play();
        }

        private AudioSource GetAudioSource()
        {
            foreach (AudioSource candidate in audioPool)
            {
                if (candidate != null && !candidate.isPlaying)
                {
                    return candidate;
                }
            }

            if (audioPool.Count < maximumAudioSources)
            {
                var sourceObject =
                    new GameObject(
                        $"FeedbackAudio_{audioPool.Count + 1:00}");

                sourceObject.transform.SetParent(transform, false);

                AudioSource created =
                    sourceObject.AddComponent<AudioSource>();

                created.playOnAwake = false;
                created.loop = false;
                created.spatialBlend = 1f;
                created.dopplerLevel = 0f;
                created.rolloffMode = AudioRolloffMode.Logarithmic;
                created.minDistance = 1f;
                created.maxDistance = maximumHearingDistance;
                created.outputAudioMixerGroup = outputMixerGroup;

                audioPool.Add(created);
                return created;
            }

            if (audioPool.Count == 0)
            {
                return null;
            }

            audioCursor %= audioPool.Count;
            AudioSource reused = audioPool[audioCursor];
            audioCursor = (audioCursor + 1) % audioPool.Count;

            reused.Stop();
            return reused;
        }

        private static AudioClip GetRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            int startIndex = Random.Range(0, clips.Length);

            for (int offset = 0; offset < clips.Length; offset++)
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
            maximumParticlesPerPool =
                Mathf.Clamp(maximumParticlesPerPool, 1, 32);

            maximumAudioSources =
                Mathf.Clamp(maximumAudioSources, 1, 24);

            masterFeedbackVolume =
                Mathf.Clamp01(masterFeedbackVolume);

            maximumHearingDistance =
                Mathf.Max(1f, maximumHearingDistance);

            foreach (AudioSource source in audioPool)
            {
                if (source != null)
                {
                    source.maxDistance = maximumHearingDistance;
                    source.outputAudioMixerGroup = outputMixerGroup;
                }
            }
        }
    }
}
