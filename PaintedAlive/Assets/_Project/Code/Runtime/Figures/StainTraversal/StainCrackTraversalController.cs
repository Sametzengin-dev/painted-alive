using PaintedAlive.Figures.StainMovement;
using PaintedAlive.Figures.StainSupport;
using PaintedAlive.Paint.Ink.StainHijack;
using PaintedAlive.Painters.Ink;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures.StainTraversal
{
    [DefaultExecutionOrder(14700)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FigureClarityState))]
    [RequireComponent(typeof(CharacterController))]
    public sealed class StainCrackTraversalController :
        MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private FigureClarityState clarityState;

        [SerializeField]
        private CharacterController characterController;

        [SerializeField]
        private InkPainterRoleAuthority roleAuthority;

        [SerializeField]
        private InkStainCreatureHijackController hijackController;

        [SerializeField]
        private StainSurfaceCrawlController crawlController;

        [SerializeField]
        private StainSpongeCarryController carryController;

        [SerializeField]
        private StainCrackTraversalConfig config;

        [SerializeField]
        private Transform transitVisual;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private StainCrackPassage nearbyPassage;

        [SerializeField]
        private StainCrackPassage activePassage;

        [SerializeField]
        private bool isTraversing;

        [SerializeField, Range(0f, 1f)]
        private float normalizedProgress;

        [SerializeField]
        private string lastResult = "Leke formu bekleniyor";

        private Vector3 traversalStart;
        private Vector3 traversalEnd;
        private Vector3 transitVisualBaseScale = Vector3.one;
        private float traversalElapsed;
        private float nextInputTime;
        private bool characterControllerWasEnabled;

        public StainCrackPassage NearbyPassage => nearbyPassage;
        public bool IsTraversing => isTraversing;
        public float NormalizedProgress => normalizedProgress;
        public string LastResult => lastResult;

        private bool IsStainRoleActive =>
            clarityState != null &&
            clarityState.CurrentLevel ==
                FigureClarityLevel.Stain &&
            roleAuthority != null &&
            !roleAuthority.IsInkPainter;

        private void Awake()
        {
            ResolveReferences();

            if (clarityState == null ||
                characterController == null ||
                roleAuthority == null ||
                hijackController == null ||
                crawlController == null ||
                carryController == null ||
                config == null ||
                transitVisual == null)
            {
                Debug.LogError(
                    "M31 Stain crack traversal references are " +
                    "incomplete. Run M31 Setup again.",
                    this);
                enabled = false;
                return;
            }

            transitVisualBaseScale = transitVisual.localScale;
            SetTransitVisualVisible(false);
        }

        private void OnDisable()
        {
            if (isTraversing)
            {
                FinishTraversal(
                    false,
                    "M31 denetleyicisi kapandı");
            }

            SetTransitVisualVisible(false);
        }

        private void Update()
        {
            if (isTraversing)
            {
                UpdateTraversal(Time.deltaTime);
                return;
            }

            if (!CanBeginTraversal() || IsEditingText())
            {
                nearbyPassage = null;
                lastResult = roleAuthority != null &&
                    roleAuthority.IsInkPainter
                        ? "Painter rolünde kapalı"
                        : "Leke formu ve serbest hareket bekleniyor";
                return;
            }

            nearbyPassage = FindNearestPassage();

            if (nearbyPassage == null)
            {
                lastResult = "Yakında bağlı ince çatlak yok";
                return;
            }

            lastResult = "E: ince çatlaktan geç";

            if (WasUsePressed() &&
                Time.unscaledTime >= nextInputTime)
            {
                BeginTraversal(nearbyPassage);
            }
        }

        public void Configure(
            FigureClarityState targetClarity,
            CharacterController targetCharacterController,
            InkPainterRoleAuthority targetRoleAuthority,
            InkStainCreatureHijackController targetHijackController,
            StainSurfaceCrawlController targetCrawlController,
            StainSpongeCarryController targetCarryController,
            StainCrackTraversalConfig targetConfig,
            Transform targetTransitVisual)
        {
            clarityState = targetClarity;
            characterController = targetCharacterController;
            roleAuthority = targetRoleAuthority;
            hijackController = targetHijackController;
            crawlController = targetCrawlController;
            carryController = targetCarryController;
            config = targetConfig;
            transitVisual = targetTransitVisual;

            if (transitVisual != null)
            {
                transitVisualBaseScale = transitVisual.localScale;
                SetTransitVisualVisible(false);
            }
        }

        private bool CanBeginTraversal()
        {
            return IsStainRoleActive &&
                !hijackController.IsHijacking &&
                !carryController.IsCarried;
        }

        private void BeginTraversal(
            StainCrackPassage passage)
        {
            if (passage == null ||
                !passage.CanTraverse ||
                passage.LinkedPassage == null ||
                !CanBeginTraversal())
            {
                lastResult = "Çatlak geçişi başlatılamadı";
                return;
            }

            activePassage = passage;
            nearbyPassage = null;
            traversalStart = transform.position;
            traversalEnd =
                passage.LinkedPassage.ExitPosition;
            traversalElapsed = 0f;
            normalizedProgress = 0f;
            characterControllerWasEnabled =
                characterController.enabled;

            crawlController.SetExternalSuspended(
                true,
                "M31 çatlak geçişi aktif");

            if (characterController.enabled)
            {
                characterController.enabled = false;
            }

            transitVisual.localScale =
                transitVisualBaseScale;
            SetTransitVisualVisible(true);
            isTraversing = true;
            lastResult = "İnce çatlakta akıyor";
        }

        private void UpdateTraversal(float deltaTime)
        {
            if (!IsStainRoleActive ||
                hijackController.IsHijacking ||
                carryController.IsCarried ||
                activePassage == null ||
                !activePassage.CanTraverse)
            {
                FinishTraversal(
                    false,
                    "Çatlak geçişi güvenli biçimde iptal edildi");
                return;
            }

            traversalElapsed += Mathf.Max(0f, deltaTime);
            normalizedProgress = Mathf.Clamp01(
                traversalElapsed /
                Mathf.Max(0.1f, config.TraversalDuration));
            float eased =
                normalizedProgress *
                normalizedProgress *
                (3f - 2f * normalizedProgress);
            transform.position = Vector3.LerpUnclamped(
                traversalStart,
                traversalEnd,
                eased);

            float compression =
                Mathf.Abs(normalizedProgress * 2f - 1f);
            float scaleFactor = Mathf.Lerp(
                config.MinimumTransitScale,
                1f,
                compression);
            transitVisual.localScale =
                Vector3.Scale(
                    transitVisualBaseScale,
                    new Vector3(
                        scaleFactor,
                        1f,
                        scaleFactor));

            if (normalizedProgress >= 1f)
            {
                FinishTraversal(
                    true,
                    "İnce çatlağın diğer tarafına geçildi");
            }
        }

        private void FinishTraversal(
            bool completed,
            string result)
        {
            if (!isTraversing)
            {
                return;
            }

            transform.position = completed
                ? traversalEnd
                : traversalStart;
            isTraversing = false;
            normalizedProgress = completed ? 1f : 0f;
            traversalElapsed = 0f;
            activePassage = null;
            SetTransitVisualVisible(false);

            if (characterController != null &&
                characterControllerWasEnabled &&
                characterController.gameObject.activeInHierarchy)
            {
                characterController.enabled = true;
            }

            if (crawlController != null)
            {
                crawlController.SetExternalSuspended(
                    false,
                    result);
            }

            nextInputTime =
                Time.unscaledTime +
                (config != null
                    ? config.InputCooldown
                    : 0.35f);
            lastResult = result;
        }

        private StainCrackPassage FindNearestPassage()
        {
            StainCrackPassage best = null;
            float maximumDistance =
                config.InteractionRange;
            float bestSqrDistance =
                maximumDistance * maximumDistance;
            var passages =
                StainCrackPassage.ActivePassages;

            for (int i = 0; i < passages.Count; i++)
            {
                StainCrackPassage candidate =
                    passages[i];

                if (candidate == null ||
                    !candidate.CanTraverse)
                {
                    continue;
                }

                float sqrDistance =
                    (candidate.EntryPosition -
                     transform.position).sqrMagnitude;

                if (sqrDistance >= bestSqrDistance)
                {
                    continue;
                }

                best = candidate;
                bestSqrDistance = sqrDistance;
            }

            return best;
        }

        private static bool WasUsePressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null &&
                keyboard.eKey.wasPressedThisFrame;
        }

        private static bool IsEditingText()
        {
            EventSystem eventSystem = EventSystem.current;
            GameObject selected = eventSystem != null
                ? eventSystem.currentSelectedGameObject
                : null;

            if (selected == null)
            {
                return false;
            }

            Component[] components =
                selected.GetComponents<Component>();

            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];

                if (component == null)
                {
                    continue;
                }

                string typeName =
                    component.GetType().Name;

                if (typeName == "InputField" ||
                    typeName == "TMP_InputField")
                {
                    return true;
                }
            }

            return false;
        }

        private void SetTransitVisualVisible(bool visible)
        {
            if (transitVisual != null &&
                transitVisual.gameObject.activeSelf != visible)
            {
                transitVisual.gameObject.SetActive(visible);
            }
        }

        private void ResolveReferences()
        {
            clarityState ??=
                GetComponent<FigureClarityState>();
            characterController ??=
                GetComponent<CharacterController>();
            roleAuthority ??=
                InkPainterRoleAuthority.ActiveInstance;
            hijackController ??=
                GetComponent<
                    InkStainCreatureHijackController>();
            crawlController ??=
                GetComponent<
                    StainSurfaceCrawlController>();
            carryController ??=
                GetComponent<
                    StainSpongeCarryController>();
        }
    }
}
