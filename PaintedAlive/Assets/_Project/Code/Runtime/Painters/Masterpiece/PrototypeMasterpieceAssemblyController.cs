using System;
using System.Collections.Generic;
using PaintedAlive.Core.RoleAuthority;
using PaintedAlive.Painters.SideCanvas;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PaintedAlive.Painters.Masterpiece
{
    [DefaultExecutionOrder(760)]
    [DisallowMultipleComponent]
    public sealed class PrototypeMasterpieceAssemblyController :
        MonoBehaviour
    {
        private static readonly PrototypeMasterpiecePartKind[]
            DetachOrder =
            {
                PrototypeMasterpiecePartKind.Head,
                PrototypeMasterpiecePartKind.LeftAttack,
                PrototypeMasterpiecePartKind.RightAttack,
                PrototypeMasterpiecePartKind.LeftContact,
                PrototypeMasterpiecePartKind.RightContact
            };

        [Header("M45 Source")]
        [SerializeField]
        private PrototypeLivingSideCanvasController sideCanvas;

        [Header("M46 View")]
        [SerializeField]
        private PrototypeMasterpieceAssemblyRenderer assemblyRenderer;

        [SerializeField] private CanvasGroup statusGroup;
        [SerializeField] private Text statusText;
        [SerializeField] private Text capabilityText;
        [SerializeField] private Text controlsText;

        [Header("Proxy Contract")]
        [SerializeField, Range(0.04f, 0.18f)]
        private float minimumCoreRadius = 0.065f;

        [SerializeField, Range(0.06f, 0.24f)]
        private float maximumCoreRadius = 0.135f;

        [SerializeField, Range(0.025f, 0.10f)]
        private float minimumLimbRadius = 0.028f;

        [SerializeField, Range(0.03f, 0.12f)]
        private float maximumLimbRadius = 0.055f;

        [SerializeField, Range(0.08f, 0.60f)]
        private float maximumProxyLength = 0.44f;

        [Header("Runtime Read Only")]
        [SerializeField] private bool assemblyReady;
        [SerializeField] private bool proxyOverlayVisible = true;
        [SerializeField] private int assemblyRevision;
        [SerializeField] private int lastBuildHash;
        [SerializeField] private int rebuildCount;
        [SerializeField] private int detachedPartCount;
        [SerializeField] private int activeCapabilityCount;
        [SerializeField] private int detachActionCount;
        [SerializeField] private int restoreActionCount;
        [SerializeField] private bool painterRoleActive;
        [SerializeField] private int roleRejectedActionCount;
        [SerializeField] private string resolvedRole = "Unknown";
        [SerializeField] private string lastAction = "Waiting for valid M45 rig";

        [SerializeField]
        private List<PrototypeMasterpiecePartState> parts =
            new List<PrototypeMasterpiecePartState>();

        public bool AssemblyReady => assemblyReady;
        public PrototypeLivingSideCanvasController SideCanvasSource =>
            sideCanvas;
        public bool ProxyOverlayVisible => proxyOverlayVisible;
        public int AssemblyRevision => assemblyRevision;
        public int RebuildCount => rebuildCount;
        public int PartCount => parts.Count;
        public int ProxyCount => parts.Count;
        public int DetachedPartCount => detachedPartCount;
        public int ActiveCapabilityCount => activeCapabilityCount;
        public int DetachActionCount => detachActionCount;
        public int RestoreActionCount => restoreActionCount;
        public bool PainterRoleActive => painterRoleActive;
        public int RoleRejectedActionCount => roleRejectedActionCount;
        public string ResolvedRole => resolvedRole;
        public string LastAction => lastAction;
        public IReadOnlyList<PrototypeMasterpiecePartState> Parts => parts;

        public bool GameplayCollidersCreated => false;
        public bool WorldSpawnEnabled => false;
        public bool BossAIEnabled => false;
        public bool VisualShapeControlsPower => false;
        public bool FixedProxyContract => true;

        public void Configure(
            PrototypeLivingSideCanvasController configuredSideCanvas,
            PrototypeMasterpieceAssemblyRenderer configuredRenderer,
            CanvasGroup configuredStatusGroup,
            Text configuredStatusText,
            Text configuredCapabilityText,
            Text configuredControlsText)
        {
            sideCanvas = configuredSideCanvas;
            assemblyRenderer = configuredRenderer;
            statusGroup = configuredStatusGroup;
            statusText = configuredStatusText;
            capabilityText = configuredCapabilityText;
            controlsText = configuredControlsText;

            if (assemblyRenderer != null)
            {
                assemblyRenderer.Configure(
                    sideCanvas,
                    this);
            }

            RefreshState(forceRebuild: true);
        }

        private void Awake()
        {
            if (assemblyRenderer != null)
            {
                assemblyRenderer.Configure(
                    sideCanvas,
                    this);
            }

            RefreshState(forceRebuild: true);
        }

        private void Update()
        {
            RefreshRoleAuthority();

            if (!painterRoleActive)
            {
                SetStatusVisibility(false);
                return;
            }

            RefreshState(forceRebuild: false);

            if (!assemblyReady ||
                sideCanvas == null ||
                !sideCanvas.IsOpen ||
                sideCanvas.CurrentMode !=
                    PrototypeSideCanvasMode.Preview)
            {
                return;
            }

            Keyboard keyboard =
                Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (keyboard.bKey.wasPressedThisFrame)
            {
                DetachNextPart();
            }

            if (keyboard.nKey.wasPressedThisFrame)
            {
                RestoreAllParts();
            }

            if (keyboard.pKey.wasPressedThisFrame)
            {
                ToggleProxyOverlay();
            }
        }


        public bool TryPrepareForDeployment(
            out string reason)
        {
            reason = string.Empty;
            RefreshRoleAuthority();

            if (!painterRoleActive)
            {
                roleRejectedActionCount++;
                reason =
                    "Baş Yapıt assembly yalnız Painter rolünde hazırlanabilir.";

                return false;
            }

            if (sideCanvas == null)
            {
                reason =
                    "M46 Yan Tuval kaynağı bağlı değil.";
                return false;
            }

            if (!sideCanvas.IsOpen)
            {
                reason =
                    "Yan Tuval deploy sırasında açık olmalı.";
                return false;
            }

            if (sideCanvas.CurrentMode !=
                PrototypeSideCanvasMode.Preview)
            {
                reason =
                    "Yan Tuval Kukla Testi modunda değil.";
                return false;
            }

            if (!sideCanvas.HasCompleteRig)
            {
                reason =
                    "Altı rig marker tamamlanmadı.";
                return false;
            }

            if (!sideCanvas.RigValid)
            {
                reason =
                    $"M45 rig geçersiz: {sideCanvas.ValidationMessage}";
                return false;
            }

            // Deployment must never depend on a previous Update tick.
            // Rebuild and validate synchronously in the same V input call.
            RefreshState(
                forceRebuild: true);

            if (!assemblyReady)
            {
                reason =
                    $"M46 assembly üretilemedi: {lastAction}";
                return false;
            }

            if (parts.Count != 6)
            {
                reason =
                    $"M46 parça sözleşmesi eksik: {parts.Count}/6.";
                return false;
            }

            if (ProxyCount != 6)
            {
                reason =
                    $"M46 proxy sözleşmesi eksik: {ProxyCount}/6.";
                return false;
            }

            RecalculateCounts();

            if (detachedPartCount != 0)
            {
                reason =
                    "Deploy öncesi N ile bütün organları geri tak.";
                return false;
            }

            if (activeCapabilityCount != 6)
            {
                reason =
                    $"Aktif capability sözleşmesi eksik: " +
                    $"{activeCapabilityCount}/6.";
                return false;
            }

            reason =
                "M46 assembly deploy için hazır.";

            return true;
        }

        public void DetachNextPart()
        {
            if (!RequirePainterRole(
                    "Organ sökme"))
            {
                return;
            }

            if (!assemblyReady)
            {
                lastAction =
                    "Assembly is not ready.";

                RefreshText();
                return;
            }

            for (int orderIndex = 0;
                 orderIndex < DetachOrder.Length;
                 orderIndex++)
            {
                PrototypeMasterpiecePartState part =
                    FindPart(
                        DetachOrder[orderIndex]);

                if (part == null ||
                    part.Detached)
                {
                    continue;
                }

                part.SetDetached(
                    true,
                    Time.unscaledTime);

                detachActionCount++;
                assemblyRevision++;
                lastAction =
                    $"{TranslatePart(part.Kind)} detached • " +
                    $"{TranslateCapability(part.Capability)} disabled";

                RecalculateCounts();
                RefreshVisuals();
                RefreshText();
                return;
            }

            lastAction =
                "All detachable organs are already detached.";

            RefreshText();
        }

        public void RestoreAllParts()
        {
            if (!RequirePainterRole(
                    "Organ geri takma"))
            {
                return;
            }

            bool changed = false;

            for (int index = 0;
                 index < parts.Count;
                 index++)
            {
                PrototypeMasterpiecePartState part =
                    parts[index];

                if (part == null ||
                    !part.Detached)
                {
                    continue;
                }

                part.SetDetached(
                    false,
                    Time.unscaledTime);

                changed = true;
            }

            if (!changed)
            {
                lastAction =
                    "All organs are already attached.";

                RefreshText();
                return;
            }

            restoreActionCount++;
            assemblyRevision++;
            lastAction =
                "All detachable organs restored.";

            RecalculateCounts();
            RefreshVisuals();
            RefreshText();
        }

        public void ToggleProxyOverlay()
        {
            if (!RequirePainterRole(
                    "Assembly proxy görünümü"))
            {
                return;
            }

            proxyOverlayVisible =
                !proxyOverlayVisible;

            assemblyRevision++;
            lastAction =
                proxyOverlayVisible
                    ? "Fixed proxy overlay visible."
                    : "Fixed proxy overlay hidden.";

            RefreshVisuals();
            RefreshText();
        }

        public PrototypeMasterpiecePartState FindPart(
            PrototypeMasterpiecePartKind kind)
        {
            for (int index = 0;
                 index < parts.Count;
                 index++)
            {
                PrototypeMasterpiecePartState part =
                    parts[index];

                if (part != null &&
                    part.Kind == kind)
                {
                    return part;
                }
            }

            return null;
        }

        private void RefreshRoleAuthority()
        {
            painterRoleActive =
                PrototypeRoleAuthorityResolver.IsPainter(
                    out resolvedRole);
        }

        private bool RequirePainterRole(string action)
        {
            RefreshRoleAuthority();

            if (painterRoleActive)
            {
                return true;
            }

            roleRejectedActionCount++;
            lastAction =
                $"{action} yalnız Painter rolünde kullanılabilir.";

            SetStatusVisibility(false);
            RefreshText();
            return false;
        }

        private void RefreshState(bool forceRebuild)
        {
            RefreshRoleAuthority();
            bool shouldBeReady =
                sideCanvas != null &&
                sideCanvas.IsOpen &&
                sideCanvas.CurrentMode ==
                    PrototypeSideCanvasMode.Preview &&
                sideCanvas.HasCompleteRig &&
                sideCanvas.RigValid;

            SetStatusVisibility(
                painterRoleActive &&
                sideCanvas != null &&
                sideCanvas.IsOpen);

            if (!shouldBeReady)
            {
                if (assemblyReady)
                {
                    assemblyReady = false;
                    parts.Clear();
                    detachedPartCount = 0;
                    activeCapabilityCount = 0;
                    lastAction =
                        "Complete M45 rig and enter Puppet Preview.";

                    RefreshVisuals();
                    RefreshText();
                }
                else
                {
                    RefreshText();
                }

                return;
            }

            int buildHash =
                CalculateBuildHash();

            if (forceRebuild ||
                !assemblyReady ||
                buildHash != lastBuildHash)
            {
                BuildAssembly(
                    buildHash);
            }

            RefreshText();
        }

        private void BuildAssembly(int buildHash)
        {
            Dictionary<
                PrototypeRigMarkerKind,
                Vector2> markerMap =
                BuildMarkerMap();

            if (!TryGetRequiredMarkers(
                    markerMap,
                    out Vector2 core,
                    out Vector2 head,
                    out Vector2 leftAttack,
                    out Vector2 rightAttack,
                    out Vector2 leftContact,
                    out Vector2 rightContact))
            {
                assemblyReady = false;
                parts.Clear();
                lastAction =
                    "Required marker map is incomplete.";

                RefreshVisuals();
                RefreshText();
                return;
            }

            parts.Clear();

            float averageExtent =
                (
                    Vector2.Distance(core, head) +
                    Vector2.Distance(core, leftAttack) +
                    Vector2.Distance(core, rightAttack) +
                    Vector2.Distance(core, leftContact) +
                    Vector2.Distance(core, rightContact)
                ) / 5f;

            float coreRadius =
                Mathf.Clamp(
                    averageExtent * 0.24f,
                    minimumCoreRadius,
                    maximumCoreRadius);

            float limbRadius =
                Mathf.Clamp(
                    coreRadius * 0.38f,
                    minimumLimbRadius,
                    maximumLimbRadius);

            float headRadius =
                Mathf.Clamp(
                    coreRadius * 0.72f,
                    minimumCoreRadius * 0.72f,
                    maximumCoreRadius * 0.72f);

            parts.Add(
                new PrototypeMasterpiecePartState(
                    PrototypeMasterpiecePartKind.Core,
                    PrototypeMasterpieceCapability.CoreIntegrity,
                    PrototypeMasterpieceProxyKind.Circle,
                    core,
                    core,
                    coreRadius));

            parts.Add(
                new PrototypeMasterpiecePartState(
                    PrototypeMasterpiecePartKind.Head,
                    PrototypeMasterpieceCapability.Perception,
                    PrototypeMasterpieceProxyKind.Circle,
                    head,
                    head,
                    headRadius));

            parts.Add(
                CreateLimb(
                    PrototypeMasterpiecePartKind.LeftAttack,
                    PrototypeMasterpieceCapability.LeftAttack,
                    core,
                    leftAttack,
                    limbRadius));

            parts.Add(
                CreateLimb(
                    PrototypeMasterpiecePartKind.RightAttack,
                    PrototypeMasterpieceCapability.RightAttack,
                    core,
                    rightAttack,
                    limbRadius));

            parts.Add(
                CreateLimb(
                    PrototypeMasterpiecePartKind.LeftContact,
                    PrototypeMasterpieceCapability.LeftMovement,
                    core,
                    leftContact,
                    limbRadius));

            parts.Add(
                CreateLimb(
                    PrototypeMasterpiecePartKind.RightContact,
                    PrototypeMasterpieceCapability.RightMovement,
                    core,
                    rightContact,
                    limbRadius));

            assemblyReady = true;
            lastBuildHash = buildHash;
            rebuildCount++;
            assemblyRevision++;
            lastAction =
                "Six-part cutout assembly built from M45 stroke + markers.";

            RecalculateCounts();
            RefreshVisuals();
            RefreshText();
        }

        private PrototypeMasterpiecePartState CreateLimb(
            PrototypeMasterpiecePartKind kind,
            PrototypeMasterpieceCapability capability,
            Vector2 start,
            Vector2 requestedEnd,
            float radius)
        {
            Vector2 direction =
                requestedEnd - start;

            float length =
                direction.magnitude;

            if (length >
                maximumProxyLength)
            {
                requestedEnd =
                    start +
                    direction.normalized *
                    maximumProxyLength;
            }

            return new PrototypeMasterpiecePartState(
                kind,
                capability,
                PrototypeMasterpieceProxyKind.Capsule,
                start,
                requestedEnd,
                radius);
        }

        private void RecalculateCounts()
        {
            detachedPartCount = 0;
            activeCapabilityCount = 0;

            for (int index = 0;
                 index < parts.Count;
                 index++)
            {
                PrototypeMasterpiecePartState part =
                    parts[index];

                if (part == null)
                {
                    continue;
                }

                if (part.Detached)
                {
                    detachedPartCount++;
                }
                else
                {
                    activeCapabilityCount++;
                }
            }
        }

        private int CalculateBuildHash()
        {
            unchecked
            {
                int hash = 17;

                IReadOnlyList<
                    PrototypeRigMarkerPlacement> markers =
                    sideCanvas.Markers;

                for (int index = 0;
                     index < markers.Count;
                     index++)
                {
                    PrototypeRigMarkerPlacement marker =
                        markers[index];

                    if (marker == null)
                    {
                        continue;
                    }

                    hash = hash * 31 +
                        (int)marker.Kind;

                    hash = hash * 31 +
                        Mathf.RoundToInt(
                            marker.NormalizedPosition.x *
                            10000f);

                    hash = hash * 31 +
                        Mathf.RoundToInt(
                            marker.NormalizedPosition.y *
                            10000f);
                }

                IReadOnlyList<
                    PrototypeSideCanvasStroke> strokes =
                    sideCanvas.Strokes;

                hash = hash * 31 +
                    strokes.Count;

                for (int index = 0;
                     index < strokes.Count;
                     index++)
                {
                    PrototypeSideCanvasStroke stroke =
                        strokes[index];

                    if (stroke != null)
                    {
                        hash = hash * 31 +
                            stroke.Points.Count;
                    }
                }

                return hash;
            }
        }

        private Dictionary<
            PrototypeRigMarkerKind,
            Vector2> BuildMarkerMap()
        {
            var result =
                new Dictionary<
                    PrototypeRigMarkerKind,
                    Vector2>();

            IReadOnlyList<
                PrototypeRigMarkerPlacement> markers =
                sideCanvas.Markers;

            for (int index = 0;
                 index < markers.Count;
                 index++)
            {
                PrototypeRigMarkerPlacement marker =
                    markers[index];

                if (marker != null)
                {
                    result[marker.Kind] =
                        marker.NormalizedPosition;
                }
            }

            return result;
        }

        private static bool TryGetRequiredMarkers(
            Dictionary<
                PrototypeRigMarkerKind,
                Vector2> markerMap,
            out Vector2 core,
            out Vector2 head,
            out Vector2 leftAttack,
            out Vector2 rightAttack,
            out Vector2 leftContact,
            out Vector2 rightContact)
        {
            // Assign every out parameter before any early return or
            // short-circuit evaluation. This keeps the method valid under
            // Unity's C# compiler even when an earlier marker is missing.
            core = default;
            head = default;
            leftAttack = default;
            rightAttack = default;
            leftContact = default;
            rightContact = default;

            if (markerMap == null)
            {
                return false;
            }

            bool hasCore =
                markerMap.TryGetValue(
                    PrototypeRigMarkerKind.Core,
                    out core);

            bool hasHead =
                markerMap.TryGetValue(
                    PrototypeRigMarkerKind.Head,
                    out head);

            bool hasLeftAttack =
                markerMap.TryGetValue(
                    PrototypeRigMarkerKind.LeftHand,
                    out leftAttack);

            bool hasRightAttack =
                markerMap.TryGetValue(
                    PrototypeRigMarkerKind.RightHand,
                    out rightAttack);

            bool hasLeftContact =
                markerMap.TryGetValue(
                    PrototypeRigMarkerKind.LeftFoot,
                    out leftContact);

            bool hasRightContact =
                markerMap.TryGetValue(
                    PrototypeRigMarkerKind.RightFoot,
                    out rightContact);

            return hasCore &&
                hasHead &&
                hasLeftAttack &&
                hasRightAttack &&
                hasLeftContact &&
                hasRightContact;
        }

        private void RefreshVisuals()
        {
            if (assemblyRenderer != null)
            {
                assemblyRenderer.Refresh();
            }
        }

        private void SetStatusVisibility(bool visible)
        {
            if (statusGroup == null)
            {
                return;
            }

            statusGroup.alpha =
                visible ? 1f : 0f;

            statusGroup.interactable = false;
            statusGroup.blocksRaycasts = false;
        }

        private void RefreshText()
        {
            if (statusText != null)
            {
                statusText.text =
                    assemblyReady
                        ? "M46 • PARÇALI BAŞ YAPIT\n" +
                          $"PARÇA {parts.Count - detachedPartCount}/{parts.Count}  " +
                          $"PROXY {parts.Count}/{parts.Count}  " +
                          $"YETENEK {activeCapabilityCount}/{parts.Count}\n" +
                          $"REV {assemblyRevision} • {lastAction}"
                        : "M46 • PARÇALI BAŞ YAPIT\n" +
                          "M45 rigini tamamla ve KUKLA TESTİ moduna geç.\n" +
                          lastAction;
            }

            if (capabilityText != null)
            {
                capabilityText.text =
                    BuildCapabilityText();
            }

            if (controlsText != null)
            {
                controlsText.text =
                    "B • SONRAKİ ORGANI SÖK   " +
                    "N • TÜMÜNÜ GERİ TAK   " +
                    "P • PROXY GÖSTER/GİZLE\n" +
                    "WORLD SPAWN KAPALI • COLLIDER AUTHORITY YOK • AI YOK";
            }
        }

        private string BuildCapabilityText()
        {
            if (!assemblyReady)
            {
                return
                    "○ ÇEKİRDEK\n" +
                    "○ ALGI\n" +
                    "○ SOL SALDIRI\n" +
                    "○ SAĞ SALDIRI\n" +
                    "○ SOL HAREKET\n" +
                    "○ SAĞ HAREKET";
            }

            var lines =
                new List<string>();

            for (int index = 0;
                 index < parts.Count;
                 index++)
            {
                PrototypeMasterpiecePartState part =
                    parts[index];

                if (part == null)
                {
                    continue;
                }

                lines.Add(
                    $"{(part.Detached ? "×" : "●")} " +
                    TranslateCapability(
                        part.Capability));
            }

            return string.Join(
                "\n",
                lines);
        }

        private static string TranslatePart(
            PrototypeMasterpiecePartKind kind)
        {
            switch (kind)
            {
                case PrototypeMasterpiecePartKind.Core:
                    return "Core";

                case PrototypeMasterpiecePartKind.Head:
                    return "Head / Perception";

                case PrototypeMasterpiecePartKind.LeftAttack:
                    return "Left attack organ";

                case PrototypeMasterpiecePartKind.RightAttack:
                    return "Right attack organ";

                case PrototypeMasterpiecePartKind.LeftContact:
                    return "Left contact organ";

                default:
                    return "Right contact organ";
            }
        }

        private static string TranslateCapability(
            PrototypeMasterpieceCapability capability)
        {
            switch (capability)
            {
                case PrototypeMasterpieceCapability.CoreIntegrity:
                    return "ÇEKİRDEK";

                case PrototypeMasterpieceCapability.Perception:
                    return "ALGI";

                case PrototypeMasterpieceCapability.LeftAttack:
                    return "SOL SALDIRI";

                case PrototypeMasterpieceCapability.RightAttack:
                    return "SAĞ SALDIRI";

                case PrototypeMasterpieceCapability.LeftMovement:
                    return "SOL HAREKET";

                default:
                    return "SAĞ HAREKET";
            }
        }
    }
}
