using UnityEngine;

namespace PaintedAlive.Paint.Ink.StainHijack
{
    [DefaultExecutionOrder(15100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InkCreatureRuntime))]
    public sealed class InkStainCliffFallSequence : MonoBehaviour
    {
        [SerializeField]
        private InkCreatureRuntime creature;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private bool isFalling;

        [SerializeField]
        private float elapsedSeconds;

        [SerializeField]
        private Vector3 velocity;

        private Collider[] cachedColliders;
        private Rigidbody cachedBody;
        private Vector3 originalScale;
        private float gravity;
        private float spinDegreesPerSecond;
        private float fallDuration;
        private float dissolveDuration;

        public bool IsFalling => isFalling;
        public float ElapsedSeconds => elapsedSeconds;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (!isFalling)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            elapsedSeconds += deltaTime;
            velocity += Vector3.down * gravity * deltaTime;
            transform.position += velocity * deltaTime;
            transform.Rotate(
                Vector3.forward,
                spinDegreesPerSecond * deltaTime,
                Space.Self);

            if (elapsedSeconds <= fallDuration)
            {
                return;
            }

            float dissolveProgress = Mathf.Clamp01(
                (elapsedSeconds - fallDuration) /
                Mathf.Max(0.05f, dissolveDuration));
            float eased =
                dissolveProgress * dissolveProgress *
                (3f - 2f * dissolveProgress);
            transform.localScale = Vector3.Lerp(
                originalScale,
                originalScale * 0.02f,
                eased);

            if (dissolveProgress >= 1f)
            {
                Destroy(gameObject);
            }
        }

        public bool BeginFall(
            Vector3 initialHorizontalVelocity,
            float downwardGravity,
            float spinSpeed,
            float visibleFallDuration,
            float inkDissolveDuration)
        {
            if (isFalling)
            {
                return false;
            }

            ResolveReferences();

            if (creature == null)
            {
                return false;
            }

            isFalling = true;
            elapsedSeconds = 0f;
            velocity =
                Vector3.ProjectOnPlane(
                    initialHorizontalVelocity,
                    Vector3.up);
            gravity = Mathf.Max(1f, downwardGravity);
            spinDegreesPerSecond = Mathf.Max(0f, spinSpeed);
            fallDuration = Mathf.Max(0.25f, visibleFallDuration);
            dissolveDuration =
                Mathf.Max(0.05f, inkDissolveDuration);
            originalScale = transform.localScale;

            DisableAutonomyAndCollision();

            Debug.Log(
                "[M27] Ele geçirilen Mürekkep yaratığı " +
                "uçurumdan düştü.",
                this);
            return true;
        }

        private void DisableAutonomyAndCollision()
        {
            if (creature != null)
            {
                creature.enabled = false;
            }

            if (cachedColliders == null)
            {
                cachedColliders =
                    GetComponentsInChildren<Collider>(true);
            }

            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null)
                {
                    cachedColliders[i].enabled = false;
                }
            }

            if (cachedBody != null)
            {
                cachedBody.isKinematic = true;
                cachedBody.useGravity = false;
                cachedBody.linearVelocity = Vector3.zero;
                cachedBody.angularVelocity = Vector3.zero;
            }
        }

        private void ResolveReferences()
        {
            if (creature == null)
            {
                creature = GetComponent<InkCreatureRuntime>();
            }

            if (cachedBody == null)
            {
                cachedBody = GetComponent<Rigidbody>();
            }
        }
    }
}
