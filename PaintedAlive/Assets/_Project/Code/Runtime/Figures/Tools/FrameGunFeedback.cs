using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Figures.Tools
{
    [DisallowMultipleComponent]
    public sealed class FrameGunFeedback : MonoBehaviour
    {
        [Header("Particle Prefab")]
        [SerializeField]
        private ParticleSystem anchorImpactParticlePrefab;

        [SerializeField, Range(1, 12)]
        private int maximumParticleInstances = 4;

        [Header("Audio")]
        [SerializeField]
        private AudioSource audioSource;

        [SerializeField]
        private AudioClip[] fireClips;

        [SerializeField]
        private AudioClip[] attachClips;

        [SerializeField]
        private AudioClip[] releaseClips;

        [SerializeField]
        private AudioClip[] breakClips;

        [SerializeField]
        private AudioClip[] rejectedClips;

        [SerializeField, Range(0f, 1f)]
        private float volume = 0.75f;

        private readonly List<ParticleSystem> particlePool = new();
        private int particleCursor;

        private void Awake()
        {
            if (audioSource == null)
            {
                var audioObject =
                    new GameObject("FrameGunAudio");

                audioObject.transform.SetParent(transform, false);

                audioSource =
                    audioObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0.75f;
            audioSource.dopplerLevel = 0f;
            audioSource.minDistance = 0.5f;
            audioSource.maxDistance = 18f;
        }

        public void PlayFire(Vector3 position)
        {
            MoveAudioSource(position);
            PlayRandomClip(
                fireClips,
                0.85f,
                Random.Range(0.96f, 1.04f));
        }

        public void PlayAttached(
            Vector3 position,
            Vector3 surfaceNormal)
        {
            PlayParticle(position, surfaceNormal, 1f);
            MoveAudioSource(position);
            PlayRandomClip(
                attachClips,
                1f,
                Random.Range(0.94f, 1.06f));
        }

        public void PlayReleased(Vector3 position)
        {
            MoveAudioSource(position);
            PlayRandomClip(
                releaseClips,
                0.65f,
                Random.Range(0.97f, 1.05f));
        }

        public void PlayBroken(Vector3 position)
        {
            PlayParticle(position, Vector3.up, 1.35f);
            MoveAudioSource(position);
            PlayRandomClip(
                breakClips,
                1f,
                Random.Range(0.88f, 0.98f));
        }

        public void PlayRejected(Vector3 position)
        {
            MoveAudioSource(position);
            PlayRandomClip(
                rejectedClips,
                0.45f,
                Random.Range(0.98f, 1.03f));
        }

        private void PlayParticle(
            Vector3 position,
            Vector3 normal,
            float scale)
        {
            if (anchorImpactParticlePrefab == null)
            {
                return;
            }

            ParticleSystem particle = GetParticleInstance();

            if (particle == null)
            {
                return;
            }

            Vector3 safeNormal =
                normal.sqrMagnitude > 0.0001f
                    ? normal.normalized
                    : Vector3.up;

            particle.transform.position = position;
            particle.transform.rotation =
                Quaternion.FromToRotation(
                    Vector3.forward,
                    safeNormal);

            particle.transform.localScale =
                Vector3.one * Mathf.Max(0.01f, scale);

            particle.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            particle.Play(true);
        }

        private ParticleSystem GetParticleInstance()
        {
            foreach (ParticleSystem candidate in particlePool)
            {
                if (candidate != null &&
                    !candidate.IsAlive(true))
                {
                    return candidate;
                }
            }

            if (particlePool.Count < maximumParticleInstances)
            {
                ParticleSystem created =
                    Instantiate(
                        anchorImpactParticlePrefab,
                        transform);

                created.name =
                    $"{anchorImpactParticlePrefab.name}_Pooled_" +
                    $"{particlePool.Count + 1:00}";

                particlePool.Add(created);
                return created;
            }

            if (particlePool.Count == 0)
            {
                return null;
            }

            particleCursor %= particlePool.Count;
            ParticleSystem reused =
                particlePool[particleCursor];

            particleCursor =
                (particleCursor + 1) % particlePool.Count;

            return reused;
        }

        private void MoveAudioSource(Vector3 position)
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
            maximumParticleInstances =
                Mathf.Clamp(maximumParticleInstances, 1, 12);

            volume = Mathf.Clamp01(volume);
        }
    }
}
