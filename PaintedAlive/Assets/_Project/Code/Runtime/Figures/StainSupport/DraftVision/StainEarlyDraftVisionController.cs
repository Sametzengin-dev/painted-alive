using System;
using System.Reflection;
using PaintedAlive.Figures;
using PaintedAlive.Painters.DraftVision;
using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.Figures.StainSupport.DraftVision
{
    [DefaultExecutionOrder(350)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FigureClarityState))]
    public sealed class StainEarlyDraftVisionController : MonoBehaviour
    {
        [Serializable]
        private sealed class DraftSlot
        {
            public int SourceId;
            public float StartedAt;
            public float NormalRevealAt;
            public float LastUpdateAt;
            public bool SourceEnded;
            public LineRenderer Line;
            public Vector3 Center;

            public bool IsAssigned => SourceId != 0;

            public void Reset()
            {
                SourceId = 0;
                StartedAt = 0f;
                NormalRevealAt = 0f;
                LastUpdateAt = 0f;
                SourceEnded = false;
                Center = Vector3.zero;

                if (Line != null)
                {
                    Line.positionCount = 0;
                    Line.enabled = false;
                }
            }
        }

        [Header("Configuration")]
        [SerializeField] private StainEarlyDraftVisionConfig config;

        [Header("Figure Dependencies")]
        [SerializeField] private FigureClarityState clarityState;
        [SerializeField] private Camera figureCamera;

        [Header("States That Hide Draft Sense")]
        [SerializeField] private Behaviour[] blockedStateBehaviours = Array.Empty<Behaviour>();

        [Header("Runtime - Read Only")]
        [SerializeField] private int visibleDraftCount;
        [SerializeField] private int receivedDraftCount;

        private DraftSlot[] slots = Array.Empty<DraftSlot>();
        private Material runtimeFallbackMaterial;

        public int VisibleDraftCount => visibleDraftCount;
        public int ReceivedDraftCount => receivedDraftCount;

        public void Configure(
            StainEarlyDraftVisionConfig visionConfig,
            FigureClarityState figureClarity,
            Camera roleCamera,
            Behaviour[] blockedBehaviours)
        {
            config = visionConfig;
            clarityState = figureClarity;
            figureCamera = roleCamera;
            blockedStateBehaviours = blockedBehaviours ?? Array.Empty<Behaviour>();
        }

        private void Awake()
        {
            clarityState ??= GetComponent<FigureClarityState>();
            if (figureCamera == null)
            {
                figureCamera = Camera.main;
            }

            if (config == null || clarityState == null)
            {
                Debug.LogError(
                    "StainEarlyDraftVisionController requires FigureClarityState and an M35 config.",
                    this);
                enabled = false;
                return;
            }

            BuildSlotPool();
        }

        private void OnEnable()
        {
            PainterDraftSignalHub.DraftUpdated += HandleDraftUpdated;
            PainterDraftSignalHub.DraftEnded += HandleDraftEnded;
        }

        private void OnDisable()
        {
            PainterDraftSignalHub.DraftUpdated -= HandleDraftUpdated;
            PainterDraftSignalHub.DraftEnded -= HandleDraftEnded;
            HideAllDrafts();
        }

        private void OnDestroy()
        {
            if (runtimeFallbackMaterial != null)
            {
                Destroy(runtimeFallbackMaterial);
            }
        }

        private void Update()
        {
            if (!CanSenseDrafts())
            {
                HideAllDrafts();
                return;
            }

            Camera activeCamera = ResolveActiveCamera();
            Vector3 viewerPosition = activeCamera != null
                ? activeCamera.transform.position
                : transform.position;

            visibleDraftCount = 0;
            float now = Time.time;

            for (int i = 0; i < slots.Length; i++)
            {
                DraftSlot slot = slots[i];
                if (!slot.IsAssigned)
                {
                    continue;
                }

                float fadeEnd = slot.NormalRevealAt + config.FadeOutDuration;
                bool expired = now >= fadeEnd;
                bool stale = now - slot.LastUpdateAt >
                    config.EarlyLeadDuration + config.FadeOutDuration + 0.35f;
                bool tooFar = Vector3.Distance(viewerPosition, slot.Center) >
                    config.MaximumVisibleDistance;

                if (expired || stale || tooFar)
                {
                    slot.Reset();
                    continue;
                }

                float alpha = ResolveAlpha(slot, now);
                ApplyLineColor(slot.Line, alpha);
                slot.Line.enabled = alpha > 0.005f && slot.Line.positionCount >= 2;

                if (slot.Line.enabled)
                {
                    visibleDraftCount++;
                }
            }
        }

        private bool CanSenseDrafts()
        {
            if (clarityState == null ||
                clarityState.CurrentLevel != FigureClarityLevel.Stain)
            {
                return false;
            }

            if (blockedStateBehaviours == null)
            {
                return true;
            }

            for (int i = 0; i < blockedStateBehaviours.Length; i++)
            {
                if (IsTransientStateActive(blockedStateBehaviours[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsTransientStateActive(Behaviour behaviour)
        {
            if (behaviour == null ||
                !behaviour.enabled ||
                !behaviour.gameObject.activeInHierarchy)
            {
                return false;
            }

            string typeName = behaviour.GetType().Name;
            string[] candidateMembers;

            if (typeName.IndexOf("Hijack", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                candidateMembers = new[]
                {
                    "IsHijacking",
                    "IsControlling",
                    "IsPossessing",
                    "HasHijackedCreature",
                    "IsActive"
                };
            }
            else if (typeName.IndexOf("SpongeCarry", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                candidateMembers = new[]
                {
                    "IsInsideSponge",
                    "IsBeingCarried",
                    "IsCarried",
                    "HasCarriedStain",
                    "IsActive"
                };
            }
            else if (typeName.IndexOf("CrackTraversal", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                candidateMembers = new[]
                {
                    "IsTraversing",
                    "IsInTraversal",
                    "IsInsideCrack",
                    "IsActive"
                };
            }
            else if (typeName.IndexOf("GripImprint", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                candidateMembers = new[]
                {
                    "IsPreparing",
                    "IsPlacing",
                    "IsCreatingImprint",
                    "IsActive"
                };
            }
            else
            {
                return false;
            }

            Type type = behaviour.GetType();
            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            for (int i = 0; i < candidateMembers.Length; i++)
            {
                string memberName = candidateMembers[i];

                PropertyInfo property = type.GetProperty(memberName, flags);
                if (property != null &&
                    property.PropertyType == typeof(bool) &&
                    property.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        return (bool)property.GetValue(behaviour);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }

                FieldInfo field = type.GetField(memberName, flags);
                if (field != null && field.FieldType == typeof(bool))
                {
                    try
                    {
                        return (bool)field.GetValue(behaviour);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }

            // Bu kontrolcüler normalde sahnede enabled kalır. Aktif durumlarını
            // açıklayan bir runtime üyesi bulunamazsa yalnız enabled olmaları
            // taslak görüşünü kalıcı biçimde engellememelidir.
            return false;
        }

        private void HandleDraftUpdated(PainterDraftSignal signal)
        {
            receivedDraftCount++;

            if (!signal.HasRenderableGeometry || !CanSenseDrafts())
            {
                return;
            }

            Vector3 center = CalculateCenter(signal.WorldPoints);
            Camera activeCamera = ResolveActiveCamera();
            Vector3 viewerPosition = activeCamera != null
                ? activeCamera.transform.position
                : transform.position;

            if (Vector3.Distance(viewerPosition, center) > config.MaximumVisibleDistance)
            {
                return;
            }

            DraftSlot slot = FindOrAllocateSlot(signal.SourceId);
            if (slot == null)
            {
                return;
            }

            slot.SourceId = signal.SourceId;
            slot.StartedAt = signal.StartedAt;
            slot.NormalRevealAt = signal.NormalRevealAt;
            slot.LastUpdateAt = Time.time;
            slot.SourceEnded = false;
            slot.Center = center;

            UpdateLineGeometry(slot.Line, signal.WorldPoints);
        }

        private void HandleDraftEnded(int sourceId)
        {
            DraftSlot slot = FindSlot(sourceId);
            if (slot == null)
            {
                return;
            }

            slot.SourceEnded = true;
            slot.LastUpdateAt = Time.time;
        }

        private DraftSlot FindOrAllocateSlot(int sourceId)
        {
            DraftSlot existing = FindSlot(sourceId);
            if (existing != null)
            {
                return existing;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsAssigned)
                {
                    return slots[i];
                }
            }

            DraftSlot oldest = null;
            for (int i = 0; i < slots.Length; i++)
            {
                if (oldest == null || slots[i].NormalRevealAt < oldest.NormalRevealAt)
                {
                    oldest = slots[i];
                }
            }

            oldest?.Reset();
            return oldest;
        }

        private DraftSlot FindSlot(int sourceId)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].SourceId == sourceId)
                {
                    return slots[i];
                }
            }

            return null;
        }

        private void BuildSlotPool()
        {
            int slotCount = Mathf.Max(1, config.MaximumActiveDrafts);
            slots = new DraftSlot[slotCount];

            Material material = ResolveDraftMaterial();
            for (int i = 0; i < slotCount; i++)
            {
                var visual = new GameObject($"M35_StainDraft_{i + 1:00}");
                visual.transform.SetParent(transform, false);

                LineRenderer line = visual.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.loop = false;
                line.alignment = LineAlignment.View;
                line.textureMode = LineTextureMode.Stretch;
                line.widthMultiplier = config.LineWidth;
                line.numCornerVertices = 4;
                line.numCapVertices = 4;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.sharedMaterial = material;
                line.positionCount = 0;
                line.enabled = false;

                slots[i] = new DraftSlot
                {
                    Line = line
                };
            }
        }

        private Material ResolveDraftMaterial()
        {
            if (config.DraftMaterial != null)
            {
                return config.DraftMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                return null;
            }

            runtimeFallbackMaterial = new Material(shader)
            {
                name = "M35_StainDraft_RuntimeMaterial",
                hideFlags = HideFlags.DontSave
            };

            if (runtimeFallbackMaterial.HasProperty("_BaseColor"))
            {
                runtimeFallbackMaterial.SetColor("_BaseColor", config.DraftColor);
            }

            return runtimeFallbackMaterial;
        }

        private void UpdateLineGeometry(LineRenderer line, Vector3[] sourcePoints)
        {
            if (line == null || sourcePoints == null)
            {
                return;
            }

            line.positionCount = sourcePoints.Length;
            for (int i = 0; i < sourcePoints.Length; i++)
            {
                line.SetPosition(
                    i,
                    sourcePoints[i] + Vector3.up * config.WorldLift);
            }
        }

        private float ResolveAlpha(DraftSlot slot, float now)
        {
            float baseAlpha = config.DraftColor.a;
            float pulse = config.PulseFrequency > 0f
                ? 0.5f + 0.5f * Mathf.Sin(
                    (now - slot.StartedAt) * config.PulseFrequency * Mathf.PI * 2f)
                : 1f;

            float pulseMultiplier = Mathf.Lerp(
                config.MinimumPulseAlpha,
                1f,
                pulse);

            float fade = 1f;
            if (config.FadeOutDuration > 0f && now > slot.NormalRevealAt)
            {
                fade = 1f - Mathf.Clamp01(
                    (now - slot.NormalRevealAt) / config.FadeOutDuration);
            }

            return baseAlpha * pulseMultiplier * fade;
        }

        private void ApplyLineColor(LineRenderer line, float alpha)
        {
            if (line == null)
            {
                return;
            }

            Color color = config.DraftColor;
            color.a = alpha;
            line.startColor = color;
            line.endColor = new Color(
                color.r,
                color.g,
                color.b,
                alpha * 0.72f);
        }

        private void HideAllDrafts()
        {
            visibleDraftCount = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i]?.Reset();
            }
        }

        private Camera ResolveActiveCamera()
        {
            if (figureCamera != null && figureCamera.isActiveAndEnabled)
            {
                return figureCamera;
            }

            Camera main = Camera.main;
            if (main != null && main.isActiveAndEnabled)
            {
                figureCamera = main;
                return main;
            }

            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera != null && camera.isActiveAndEnabled)
                {
                    figureCamera = camera;
                    return camera;
                }
            }

            return figureCamera;
        }

        private static Vector3 CalculateCenter(Vector3[] points)
        {
            Vector3 center = Vector3.zero;
            for (int i = 0; i < points.Length; i++)
            {
                center += points[i];
            }

            return points.Length > 0 ? center / points.Length : center;
        }
    }
}
