using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PaintedAlive.UI.Telegraph
{
    [DefaultExecutionOrder(650)]
    [DisallowMultipleComponent]
    public sealed class PrototypePainterTelegraphProbe : MonoBehaviour
    {
        private const BindingFlags Flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        [Header("Existing Gameplay Sources")]
        [SerializeField] private MonoBehaviour painterBrushController;
        [SerializeField] private MonoBehaviour painterStrokeBudget;
        [SerializeField] private MonoBehaviour painterPigmentReservoir;

        [Header("Sampling")]
        [SerializeField, Min(0.02f)] private float publishInterval = 0.05f;
        [SerializeField, Min(0.001f)] private float pointQuantization = 0.015f;
        [SerializeField, Min(0.0001f)] private float pigmentCommitEpsilon = 0.005f;

        [Header("Runtime Read Only")]
        [SerializeField] private bool sourceResolved;
        [SerializeField] private bool previewActive;
        [SerializeField] private string currentShape = string.Empty;
        [SerializeField] private PrototypePainterTelegraphPhase currentPhase;
        [SerializeField] private float currentProgress;
        [SerializeField] private int publishedSignalCount;
        [SerializeField] private int committedCount;
        [SerializeField] private int cancelledCount;
        [SerializeField] private string lastResolutionReason = "None";

        private PropertyInfo isPreviewingProperty;
        private PropertyInfo activePreviewShapeProperty;
        private PropertyInfo telegraphNormalizedProperty;
        private PropertyInfo isTelegraphCompleteProperty;
        private PropertyInfo previewCanAffordProperty;
        private PropertyInfo estimatedPigmentCostProperty;
        private FieldInfo previewPointsField;
        private FieldInfo currentTelegraphDurationField;

        private PropertyInfo activeStrokeCountProperty;
        private PropertyInfo currentPigmentProperty;

        private Vector3[] lastPoints = Array.Empty<Vector3>();
        private int lastGeometryHash;
        private float nextPublishAt;
        private float previewStartedAt;
        private int activeStrokeCountAtStart;
        private float pigmentAtStart;
        private bool previousPreviewing;

        public bool SourceResolved => sourceResolved;
        public bool PreviewActive => previewActive;
        public string CurrentShape => currentShape;
        public PrototypePainterTelegraphPhase CurrentPhase => currentPhase;
        public float CurrentProgress => currentProgress;
        public int PublishedSignalCount => publishedSignalCount;
        public int CommittedCount => committedCount;
        public int CancelledCount => cancelledCount;
        public string LastResolutionReason => lastResolutionReason;

        public void Configure(
            MonoBehaviour configuredBrushController,
            MonoBehaviour configuredStrokeBudget,
            MonoBehaviour configuredPigmentReservoir)
        {
            painterBrushController = configuredBrushController;
            painterStrokeBudget = configuredStrokeBudget;
            painterPigmentReservoir = configuredPigmentReservoir;
            CacheMembers();
        }

        private void Awake()
        {
            CacheMembers();
        }

        private void OnEnable()
        {
            previousPreviewing = false;
            previewActive = false;
            nextPublishAt = 0f;
        }

        private void LateUpdate()
        {
            if (!sourceResolved)
            {
                CacheMembers();
                if (!sourceResolved)
                {
                    return;
                }
            }

            bool isPreviewing =
                ReadBool(
                    painterBrushController,
                    isPreviewingProperty,
                    defaultValue: false);

            if (isPreviewing)
            {
                if (!previousPreviewing)
                {
                    BeginPreviewCapture();
                }

                UpdatePreviewCapture();
            }
            else if (previousPreviewing)
            {
                ResolvePreviewCapture();
            }

            previousPreviewing = isPreviewing;
            previewActive = isPreviewing;
        }

        private void OnDisable()
        {
            if (previousPreviewing)
            {
                ResolveCancelled("ProbeDisabled");
            }

            previousPreviewing = false;
            previewActive = false;
        }

        private void CacheMembers()
        {
            sourceResolved = false;

            if (painterBrushController == null)
            {
                return;
            }

            Type brushType = painterBrushController.GetType();

            isPreviewingProperty =
                FindProperty(brushType, "IsPreviewing");

            activePreviewShapeProperty =
                FindProperty(brushType, "ActivePreviewShape");

            telegraphNormalizedProperty =
                FindProperty(brushType, "TelegraphNormalized");

            isTelegraphCompleteProperty =
                FindProperty(brushType, "IsTelegraphComplete");

            previewCanAffordProperty =
                FindProperty(brushType, "PreviewCanAfford");

            estimatedPigmentCostProperty =
                FindProperty(brushType, "EstimatedPigmentCost");

            previewPointsField =
                FindField(brushType, "previewPoints");

            currentTelegraphDurationField =
                FindField(brushType, "currentTelegraphDuration");

            if (painterStrokeBudget != null)
            {
                activeStrokeCountProperty =
                    FindProperty(
                        painterStrokeBudget.GetType(),
                        "ActiveStrokeCount");
            }

            if (painterPigmentReservoir != null)
            {
                currentPigmentProperty =
                    FindProperty(
                        painterPigmentReservoir.GetType(),
                        "Current");
            }

            sourceResolved =
                isPreviewingProperty != null &&
                activePreviewShapeProperty != null &&
                telegraphNormalizedProperty != null &&
                previewCanAffordProperty != null &&
                previewPointsField != null;
        }

        private void BeginPreviewCapture()
        {
            previewStartedAt = Time.time;
            activeStrokeCountAtStart = ReadInt(
                painterStrokeBudget,
                activeStrokeCountProperty,
                0);

            pigmentAtStart = ReadFloat(
                painterPigmentReservoir,
                currentPigmentProperty,
                0f);

            lastGeometryHash = 0;
            lastPoints = Array.Empty<Vector3>();
            nextPublishAt = 0f;
            lastResolutionReason = "PreviewStarted";
        }

        private void UpdatePreviewCapture()
        {
            Vector3[] points = ReadPreviewPoints();
            if (points.Length < 2)
            {
                return;
            }

            string shape = ReadString(
                painterBrushController,
                activePreviewShapeProperty,
                "Unknown");

            float progress = ReadFloat(
                painterBrushController,
                telegraphNormalizedProperty,
                0f);

            bool canAfford = ReadBool(
                painterBrushController,
                previewCanAffordProperty,
                true);

            bool complete = ReadBool(
                painterBrushController,
                isTelegraphCompleteProperty,
                progress >= 0.999f);

            float estimatedCost = ReadFloat(
                painterBrushController,
                estimatedPigmentCostProperty,
                0f);

            float duration = ReadFloat(
                painterBrushController,
                currentTelegraphDurationField,
                0f);

            PrototypePainterTelegraphPhase phase =
                !canAfford
                    ? PrototypePainterTelegraphPhase.Blocked
                    : complete
                        ? PrototypePainterTelegraphPhase.Armed
                        : PrototypePainterTelegraphPhase.Drafting;

            int geometryHash =
                CalculateGeometryHash(points, pointQuantization);

            bool importantChange =
                geometryHash != lastGeometryHash ||
                phase != currentPhase ||
                Mathf.Abs(progress - currentProgress) >= 0.04f;

            if (!importantChange &&
                Time.unscaledTime < nextPublishAt)
            {
                return;
            }

            lastGeometryHash = geometryHash;
            lastPoints = points;
            currentShape = shape;
            currentPhase = phase;
            currentProgress = Mathf.Clamp01(progress);
            nextPublishAt =
                Time.unscaledTime + publishInterval;

            float remainingSeconds =
                duration > 0f
                    ? Mathf.Max(0f, duration * (1f - currentProgress))
                    : 0f;

            PrototypePainterTelegraphHub.Publish(
                new PrototypePainterTelegraphSignal(
                    ResolveSourceId(),
                    ClonePoints(lastPoints),
                    currentShape,
                    currentPhase,
                    currentProgress,
                    remainingSeconds,
                    canAfford,
                    estimatedCost,
                    previewStartedAt,
                    Time.time));

            publishedSignalCount++;
        }

        private void ResolvePreviewCapture()
        {
            int activeStrokeCountAfter = ReadInt(
                painterStrokeBudget,
                activeStrokeCountProperty,
                activeStrokeCountAtStart);

            float pigmentAfter = ReadFloat(
                painterPigmentReservoir,
                currentPigmentProperty,
                pigmentAtStart);

            bool strokeCountIncreased =
                activeStrokeCountAfter > activeStrokeCountAtStart;

            bool pigmentSpent =
                pigmentAtStart - pigmentAfter >
                pigmentCommitEpsilon;

            if (strokeCountIncreased || pigmentSpent)
            {
                committedCount++;
                lastResolutionReason =
                    strokeCountIncreased
                        ? "StrokeCountIncreased"
                        : "PigmentSpent";

                PrototypePainterTelegraphHub.Resolve(
                    new PrototypePainterTelegraphOutcome(
                        ResolveSourceId(),
                        ClonePoints(lastPoints),
                        currentShape,
                        PrototypePainterTelegraphResolution.Committed,
                        lastResolutionReason,
                        Time.time));
            }
            else
            {
                string reason =
                    currentPhase ==
                    PrototypePainterTelegraphPhase.Blocked
                        ? "PigmentOrBudgetBlocked"
                        : currentProgress < 0.999f
                            ? "ReleasedBeforeArmed"
                            : "CancelledOrRejected";

                ResolveCancelled(reason);
            }

            ResetCapture();
        }

        private void ResolveCancelled(string reason)
        {
            cancelledCount++;
            lastResolutionReason = reason;

            PrototypePainterTelegraphHub.Resolve(
                new PrototypePainterTelegraphOutcome(
                    ResolveSourceId(),
                    ClonePoints(lastPoints),
                    currentShape,
                    PrototypePainterTelegraphResolution.Cancelled,
                    reason,
                    Time.time));
        }

        private void ResetCapture()
        {
            currentShape = string.Empty;
            currentPhase = PrototypePainterTelegraphPhase.None;
            currentProgress = 0f;
            lastPoints = Array.Empty<Vector3>();
            lastGeometryHash = 0;
            nextPublishAt = 0f;
        }

        private Vector3[] ReadPreviewPoints()
        {
            object raw = ReadMember(
                painterBrushController,
                previewPointsField);

            if (raw is Vector3[] array)
            {
                return ClonePoints(array);
            }

            if (raw is IList list)
            {
                List<Vector3> result =
                    new List<Vector3>(list.Count);

                for (int index = 0;
                     index < list.Count;
                     index++)
                {
                    if (list[index] is Vector3 point)
                    {
                        result.Add(point);
                    }
                }

                return result.ToArray();
            }

            return Array.Empty<Vector3>();
        }

        private int ResolveSourceId()
        {
            return painterBrushController != null
                ? painterBrushController.GetInstanceID()
                : GetInstanceID();
        }

        private static Vector3[] ClonePoints(
            Vector3[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<Vector3>();
            }

            Vector3[] clone =
                new Vector3[source.Length];

            Array.Copy(
                source,
                clone,
                source.Length);

            return clone;
        }

        private static int CalculateGeometryHash(
            Vector3[] points,
            float quantization)
        {
            float safeQuantization =
                Mathf.Max(0.001f, quantization);

            unchecked
            {
                int hash = 17;
                hash = hash * 31 + points.Length;

                for (int index = 0;
                     index < points.Length;
                     index++)
                {
                    Vector3 point = points[index];
                    hash = hash * 31 +
                        Mathf.RoundToInt(
                            point.x / safeQuantization);
                    hash = hash * 31 +
                        Mathf.RoundToInt(
                            point.y / safeQuantization);
                    hash = hash * 31 +
                        Mathf.RoundToInt(
                            point.z / safeQuantization);
                }

                return hash;
            }
        }

        private static object ReadMember(
            object target,
            MemberInfo member)
        {
            if (target == null || member == null)
            {
                return null;
            }

            try
            {
                if (member is PropertyInfo property &&
                    property.GetIndexParameters().Length == 0)
                {
                    return property.GetValue(target);
                }

                if (member is FieldInfo field)
                {
                    return field.GetValue(target);
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        private static bool ReadBool(
            object target,
            MemberInfo member,
            bool defaultValue)
        {
            object value = ReadMember(target, member);
            return value is bool result
                ? result
                : defaultValue;
        }

        private static int ReadInt(
            object target,
            MemberInfo member,
            int defaultValue)
        {
            object value = ReadMember(target, member);
            return value is int result
                ? result
                : defaultValue;
        }

        private static float ReadFloat(
            object target,
            MemberInfo member,
            float defaultValue)
        {
            object value = ReadMember(target, member);

            if (value is float floatValue)
            {
                return floatValue;
            }

            if (value is double doubleValue)
            {
                return (float)doubleValue;
            }

            return defaultValue;
        }

        private static string ReadString(
            object target,
            MemberInfo member,
            string defaultValue)
        {
            object value = ReadMember(target, member);
            return value != null
                ? value.ToString()
                : defaultValue;
        }

        private static PropertyInfo FindProperty(
            Type type,
            string name)
        {
            while (type != null)
            {
                PropertyInfo property =
                    type.GetProperty(name, Flags);

                if (property != null)
                {
                    return property;
                }

                type = type.BaseType;
            }

            return null;
        }

        private static FieldInfo FindField(
            Type type,
            string name)
        {
            while (type != null)
            {
                FieldInfo field =
                    type.GetField(name, Flags);

                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            return null;
        }
    }
}
