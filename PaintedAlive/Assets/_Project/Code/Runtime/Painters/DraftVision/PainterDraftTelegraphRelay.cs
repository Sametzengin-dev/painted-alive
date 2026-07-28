using System;
using System.Collections;
using System.Reflection;
using PaintedAlive.Figures.StainSupport.DraftVision;
using UnityEngine;

namespace PaintedAlive.Painters.DraftVision
{
    [DefaultExecutionOrder(300)]
    [DisallowMultipleComponent]
    public sealed class PainterDraftTelegraphRelay : MonoBehaviour
    {
        private const BindingFlags ReflectionFlags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        [SerializeField] private MonoBehaviour painterBrushController;
        [SerializeField] private StainEarlyDraftVisionConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField] private bool isRelaying;
        [SerializeField] private int relayedPointCount;
        [SerializeField] private float draftStartedAt;
        [SerializeField] private float normalRevealAt;

        private FieldInfo previewPointsField;
        private FieldInfo stateField;
        private FieldInfo telegraphElapsedField;
        private PropertyInfo previewPointsProperty;
        private PropertyInfo stateProperty;
        private float nextPublishTime;
        private int lastGeometryHash;
        private readonly int sourceIdSeed = 7919;

        public bool IsRelaying => isRelaying;
        public int RelayedPointCount => relayedPointCount;

        public void Configure(
            MonoBehaviour brushController,
            StainEarlyDraftVisionConfig visionConfig)
        {
            painterBrushController = brushController;
            config = visionConfig;
            CacheMembers();
        }

        private void Awake()
        {
            CacheMembers();
        }

        private void LateUpdate()
        {
            if (config == null ||
                painterBrushController == null ||
                !painterBrushController.isActiveAndEnabled ||
                !IsPreviewing())
            {
                EndCurrentDraft();
                return;
            }

            if (!TryReadPreviewPoints(out Vector3[] points) || points.Length < 2)
            {
                EndCurrentDraft();
                return;
            }

            if (!isRelaying)
            {
                isRelaying = true;
                draftStartedAt = Time.time;
                normalRevealAt = draftStartedAt + config.EarlyLeadDuration;
                lastGeometryHash = 0;
                nextPublishTime = 0f;
            }

            int geometryHash = CalculateGeometryHash(points, config.MinimumPointChange);
            bool geometryChanged = geometryHash != lastGeometryHash;
            if (!geometryChanged && Time.unscaledTime < nextPublishTime)
            {
                return;
            }

            lastGeometryHash = geometryHash;
            nextPublishTime = Time.unscaledTime + config.RelayPublishInterval;
            relayedPointCount = points.Length;

            PainterDraftSignalHub.Publish(
                new PainterDraftSignal(
                    ResolveSourceId(),
                    points,
                    draftStartedAt,
                    normalRevealAt));
        }

        private void OnDisable()
        {
            EndCurrentDraft();
        }

        private void OnDestroy()
        {
            EndCurrentDraft();
        }

        private void CacheMembers()
        {
            previewPointsField = null;
            stateField = null;
            telegraphElapsedField = null;
            previewPointsProperty = null;
            stateProperty = null;

            if (painterBrushController == null)
            {
                painterBrushController = FindBrushControllerOnObject();
            }

            if (painterBrushController == null)
            {
                return;
            }

            Type type = painterBrushController.GetType();
            previewPointsField = FindField(type, "previewPoints");
            stateField = FindField(type, "state");
            telegraphElapsedField = FindField(type, "telegraphElapsed");
            previewPointsProperty = FindProperty(type, "PreviewPoints");
            stateProperty = FindProperty(type, "State");
        }

        private MonoBehaviour FindBrushControllerOnObject()
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour != null &&
                    behaviour.GetType().Name.Equals(
                        "PainterBrushController",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return behaviour;
                }
            }

            return null;
        }

        private bool IsPreviewing()
        {
            object state = TryReadMember(stateField, stateProperty);
            if (state == null)
            {
                return TryReadPreviewPoints(out Vector3[] points) && points.Length >= 2;
            }

            string stateName = state.ToString();
            return stateName.IndexOf("Preview", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   stateName.IndexOf("Draft", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool TryReadPreviewPoints(out Vector3[] points)
        {
            points = Array.Empty<Vector3>();
            object collection = TryReadMember(previewPointsField, previewPointsProperty);

            if (collection is IList list)
            {
                int count = list.Count;
                if (count == 0)
                {
                    return false;
                }

                var result = new Vector3[count];
                int validCount = 0;
                for (int i = 0; i < count; i++)
                {
                    if (list[i] is Vector3 point)
                    {
                        result[validCount++] = point;
                    }
                }

                if (validCount == result.Length)
                {
                    points = result;
                }
                else if (validCount > 0)
                {
                    Array.Resize(ref result, validCount);
                    points = result;
                }

                return points.Length > 0;
            }

            if (collection is Vector3[] array)
            {
                points = (Vector3[])array.Clone();
                return points.Length > 0;
            }

            return false;
        }

        private object TryReadMember(FieldInfo field, PropertyInfo property)
        {
            try
            {
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    return property.GetValue(painterBrushController);
                }

                return field != null ? field.GetValue(painterBrushController) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void EndCurrentDraft()
        {
            if (isRelaying)
            {
                PainterDraftSignalHub.End(ResolveSourceId());
            }

            isRelaying = false;
            relayedPointCount = 0;
            draftStartedAt = 0f;
            normalRevealAt = 0f;
            lastGeometryHash = 0;
        }

        private int ResolveSourceId()
        {
            int controllerId = painterBrushController != null
                ? painterBrushController.GetInstanceID()
                : GetInstanceID();

            unchecked
            {
                return controllerId * sourceIdSeed;
            }
        }

        private static int CalculateGeometryHash(
            Vector3[] points,
            float minimumPointChange)
        {
            float quantization = Mathf.Max(minimumPointChange, 0.001f);
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + points.Length;
                for (int i = 0; i < points.Length; i++)
                {
                    Vector3 point = points[i];
                    hash = hash * 31 + Mathf.RoundToInt(point.x / quantization);
                    hash = hash * 31 + Mathf.RoundToInt(point.y / quantization);
                    hash = hash * 31 + Mathf.RoundToInt(point.z / quantization);
                }

                return hash;
            }
        }

        private static FieldInfo FindField(Type type, string name)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(name, ReflectionFlags);
                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            return null;
        }

        private static PropertyInfo FindProperty(Type type, string name)
        {
            while (type != null)
            {
                PropertyInfo property = type.GetProperty(name, ReflectionFlags);
                if (property != null)
                {
                    return property;
                }

                type = type.BaseType;
            }

            return null;
        }
    }
}
