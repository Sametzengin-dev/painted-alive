using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures
{
    [RequireComponent(typeof(FigureClarityState))]
    public sealed class StainMarkController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField]
        private InputActionProperty placeMarkAction;

        [Header("Placement")]
        [SerializeField] private Transform markOrigin;
        [SerializeField] private LayerMask groundMask;
        [SerializeField, Min(0.5f)] private float groundCheckDistance = 2f;

        [Header("Mark")]
        [SerializeField] private StainDirectionMark markPrefab;
        [SerializeField] private Transform markContainer;
        [SerializeField, Range(1, 8)] private int maximumMarks = 3;
        [SerializeField, Min(1f)] private float markLifetime = 10f;
        [SerializeField, Min(0f)] private float cooldown = 3f;

        private FigureClarityState clarityState;
        private StainDirectionMark[] markPool;

        private int nextMarkIndex;
        private float nextAllowedTime;
        private bool enabledInputHere;

        public bool IsInStainForm =>
            clarityState != null &&
            clarityState.CurrentLevel == FigureClarityLevel.Stain;

        public float CooldownDuration => cooldown;

        public float CooldownRemaining =>
            Mathf.Max(0f, nextAllowedTime - Time.time);

        public float CooldownNormalized
        {
            get
            {
                if (cooldown <= 0f)
                    return 0f;

                return Mathf.Clamp01(
                    CooldownRemaining / cooldown);
            }
        }

        public bool CanPlaceMark =>
            IsInStainForm &&
            CooldownRemaining <= 0f;

        private void Awake()
        {
            clarityState = GetComponent<FigureClarityState>();

            if (markOrigin == null)
                markOrigin = transform;

            CreateMarkPool();
        }

        private void OnEnable()
        {
            InputAction action = placeMarkAction.action;

            if (action != null && !action.enabled)
            {
                action.Enable();
                enabledInputHere = true;
            }
        }

        private void OnDisable()
        {
            if (enabledInputHere &&
                placeMarkAction.action != null)
            {
                placeMarkAction.action.Disable();
            }

            enabledInputHere = false;
        }

        private void Update()
        {
            if (!IsInStainForm)
                return;

            InputAction action = placeMarkAction.action;

            if (action == null ||
                !action.WasPressedThisFrame())
            {
                return;
            }

            if (!CanPlaceMark)
                return;

            TryPlaceMark();
        }

        private void CreateMarkPool()
        {
            if (markPrefab == null)
            {
                Debug.LogError(
                    "StainMarkController requires a mark prefab.",
                    this);

                markPool = new StainDirectionMark[0];
                return;
            }

            maximumMarks = Mathf.Max(1, maximumMarks);
            markPool = new StainDirectionMark[maximumMarks];

            for (int i = 0; i < maximumMarks; i++)
            {
                StainDirectionMark mark = Instantiate(
                    markPrefab,
                    markContainer);

                mark.name = $"StainDirectionMark_{i + 1:00}";
                mark.Deactivate();

                markPool[i] = mark;
            }
        }

        private void TryPlaceMark()
        {
            if (markPool == null || markPool.Length == 0)
                return;

            Vector3 rayOrigin =
                markOrigin.position + Vector3.up * 0.25f;

            if (!Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    groundCheckDistance,
                    groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                return;
            }

            Vector3 forward = Vector3.ProjectOnPlane(
                transform.forward,
                hit.normal);

            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.ProjectOnPlane(
                    Vector3.forward,
                    hit.normal);
            }

            forward.Normalize();

            Quaternion rotation =
                Quaternion.LookRotation(forward, hit.normal);

            Vector3 position =
                hit.point + hit.normal * 0.015f;

            StainDirectionMark mark =
                markPool[nextMarkIndex];

            mark.Activate(
                position,
                rotation,
                markLifetime);

            nextMarkIndex =
                (nextMarkIndex + 1) % markPool.Length;

            nextAllowedTime = Time.time + cooldown;
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin =
                markOrigin != null ? markOrigin : transform;

            Gizmos.color = new Color(
                0.7f,
                0.1f,
                0.25f,
                0.8f);

            Vector3 start =
                origin.position + Vector3.up * 0.25f;

            Gizmos.DrawLine(
                start,
                start + Vector3.down * groundCheckDistance);
        }
    }
}
