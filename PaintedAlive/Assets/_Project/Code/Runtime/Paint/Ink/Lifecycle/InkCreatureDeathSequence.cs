using PaintedAlive.Paint.Ink.Possession;
using UnityEngine;

namespace PaintedAlive.Paint.Ink.Lifecycle
{
    public enum InkCreatureDeathCause
    {
        None,
        EyeGlyphLost,
        FootGlyphLost
    }

    [DefaultExecutionOrder(-20)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InkCreatureRuntime))]
    public sealed class InkCreatureDeathSequence : MonoBehaviour
    {
        private const string DeathLockName =
            "FrameGunAnchor_Runtime_M19DeathLock";

        [SerializeField]
        private InkCreatureRuntime creature;

        [SerializeField]
        private InkNestLifecycleConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private bool isDying;

        [SerializeField]
        private InkCreatureDeathCause deathCause;

        [SerializeField]
        private float deathStartedAt;

        private Collider[] cachedColliders;
        private Vector3 startPosition;
        private Vector3 startScale;
        private Quaternion startRotation;
        private Quaternion tippedRotation;

        public bool IsDying => isDying;
        public InkCreatureDeathCause DeathCause => deathCause;
        public float RemainingLifetime => isDying && config != null
            ? Mathf.Max(
                0f,
                config.TotalDeathDuration - (Time.time - deathStartedAt))
            : 0f;

        private void Awake()
        {
            creature ??= GetComponent<InkCreatureRuntime>();
            cachedColliders = GetComponentsInChildren<Collider>(true);
        }

        private void Update()
        {
            if (isDying)
            {
                return;
            }

            if (creature == null ||
                config == null ||
                !creature.IsInitialized)
            {
                return;
            }

            if (!creature.HasGlyph(InkGlyphType.Eye))
            {
                BeginDeath(InkCreatureDeathCause.EyeGlyphLost);
            }
            else if (!creature.HasGlyph(InkGlyphType.Foot))
            {
                BeginDeath(InkCreatureDeathCause.FootGlyphLost);
            }
        }

        private void LateUpdate()
        {
            if (isDying)
            {
                AnimateDeath(Time.time);
            }
        }

        public void Configure(
            InkCreatureRuntime targetCreature,
            InkNestLifecycleConfig lifecycleConfig)
        {
            creature = targetCreature;
            config = lifecycleConfig;
        }

        public bool BeginDeath(InkCreatureDeathCause cause)
        {
            if (isDying || creature == null || config == null)
            {
                return false;
            }

            isDying = true;
            deathCause = cause;
            deathStartedAt = Time.time;
            startPosition = transform.position;
            startScale = transform.localScale;
            startRotation = transform.rotation;

            float side = (GetInstanceID() & 1) == 0 ? 1f : -1f;
            tippedRotation = startRotation * Quaternion.AngleAxis(
                config.TipAngle * side,
                Vector3.forward);

            CreateDeathLock();
            ExitPossessionIfNeeded();
            DisableCollision();
            creature.enabled = false;
            return true;
        }

        private void AnimateDeath(float now)
        {
            if (creature != null && creature.enabled)
            {
                creature.enabled = false;
            }

            float elapsed = Mathf.Max(0f, now - deathStartedAt);
            float tipDuration = Mathf.Max(0.01f, config.TipDuration);

            if (elapsed < tipDuration)
            {
                float progress = Mathf.SmoothStep(
                    0f,
                    1f,
                    elapsed / tipDuration);
                transform.rotation = Quaternion.Slerp(
                    startRotation,
                    tippedRotation,
                    progress);
                transform.localScale = startScale;
                transform.position = startPosition;
                return;
            }

            transform.rotation = tippedRotation;
            float dissolveStart =
                tipDuration + config.TippedLingerDuration;

            if (elapsed < dissolveStart)
            {
                transform.localScale = startScale;
                transform.position = startPosition;
                return;
            }

            float dissolveProgress = Mathf.Clamp01(
                (elapsed - dissolveStart) /
                Mathf.Max(0.01f, config.DissolveDuration));
            float eased = dissolveProgress * dissolveProgress *
                (3f - 2f * dissolveProgress);
            Vector3 puddleScale = new Vector3(
                startScale.x * 1.15f,
                startScale.y * 0.04f,
                startScale.z * 1.15f);
            transform.localScale = Vector3.Lerp(
                startScale,
                puddleScale * 0.02f,
                eased);
            transform.position = startPosition -
                Vector3.up * (config.DissolveSinkDistance * eased);

            if (dissolveProgress >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private void ExitPossessionIfNeeded()
        {
            InkPossessionController[] controllers =
                Object.FindObjectsByType<InkPossessionController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int i = 0; i < controllers.Length; i++)
            {
                InkPossessionController controller = controllers[i];

                if (controller != null &&
                    controller.IsPossessing &&
                    controller.PossessedCreature == creature)
                {
                    controller.ExitPossession(
                        "Critical Lekebacak glyph was cut");
                }
            }
        }

        private void DisableCollision()
        {
            if (cachedColliders == null || cachedColliders.Length == 0)
            {
                cachedColliders = GetComponentsInChildren<Collider>(true);
            }

            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null)
                {
                    cachedColliders[i].enabled = false;
                }
            }

            Rigidbody body = GetComponent<Rigidbody>();

            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
        }

        private void CreateDeathLock()
        {
            Transform existing = transform.Find(DeathLockName);

            if (existing != null)
            {
                return;
            }

            var marker = new GameObject(DeathLockName);
            marker.transform.SetParent(transform, false);
        }
    }
}
