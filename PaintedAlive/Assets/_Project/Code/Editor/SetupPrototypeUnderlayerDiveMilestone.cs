#if UNITY_EDITOR
using System;
using System.IO;
using PaintedAlive.Figures.CounterComposition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeUnderlayerDiveMilestone
    {
        private const string RootName =
            "M52_0_UnderlayerDiveRiskSpikeC1";

        private const string LabRootName =
            "M52_0_UnderlayerTraversalLab";

        private const string UnifiedCanvasName =
            "M43_UnifiedPlayerHUD";

        private const string UnifiedHudRootPath =
            "HUD_Root";

        private const string PanelName =
            "M52_0_UnderlayerDivePanel";

        private const string MaterialFolder =
            "Assets/_Project/Materials/Prototype/M52";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "52.0 - Apply Underlayer Dive Risk Spike C1")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M52.0 setup Play Mode dışında çalıştırılmalıdır.");
                }

                MonoBehaviour figureMotor =
                    FindRequiredMonoBehaviour(
                        "FigureMotor");

                MonoBehaviour paletteKnife =
                    FindRelatedMonoBehaviour(
                        figureMotor.transform,
                        "PaletteKnifeController");

                if (paletteKnife == null)
                {
                    throw new InvalidOperationException(
                        "FigureMotor ile ilişkili PaletteKnifeController bulunamadı.");
                }

                MonoBehaviour traceWeave =
                    FindOptionalMonoBehaviour(
                        "PrototypeTraceWeaveController");

                GameObject unifiedCanvas =
                    GameObject.Find(
                        UnifiedCanvasName);

                if (unifiedCanvas == null)
                {
                    throw new InvalidOperationException(
                        "M43_UnifiedPlayerHUD bulunamadı.");
                }

                Transform hudRoot =
                    unifiedCanvas.transform.Find(
                        UnifiedHudRootPath);

                if (hudRoot == null)
                {
                    throw new InvalidOperationException(
                        "M43 HUD_Root bulunamadı.");
                }

                EnsureFolder(
                    MaterialFolder);

                Material surfaceBarrierMaterial =
                    GetOrCreateMaterial(
                        MaterialFolder +
                        "/MAT_M52_SurfaceBarrier.mat",
                        new Color(
                            0.22f,
                            0.15f,
                            0.11f,
                            1f));

                Material underlayerFloorMaterial =
                    GetOrCreateMaterial(
                        MaterialFolder +
                        "/MAT_M52_UnderlayerFloor.mat",
                        new Color(
                            0.12f,
                            0.07f,
                            0.15f,
                            1f));

                Material underlayerRailMaterial =
                    GetOrCreateMaterial(
                        MaterialFolder +
                        "/MAT_M52_UnderlayerRail.mat",
                        new Color(
                            0.28f,
                            0.14f,
                            0.31f,
                            1f));

                Material seamMaterial =
                    GetOrCreateMaterial(
                        MaterialFolder +
                        "/MAT_M52_SeamMarker.mat",
                        new Color(
                            0.90f,
                            0.50f,
                            0.20f,
                            1f));

                GameObject root =
                    GetOrCreateRoot(
                        RootName);

                GameObject lab =
                    GetOrCreateChild(
                        root.transform,
                        LabRootName);

                ClearChildren(
                    lab.transform);

                Vector3 forward =
                    Vector3.ProjectOnPlane(
                        figureMotor.transform.forward,
                        Vector3.up);

                if (
                    forward.sqrMagnitude <
                    0.001f
                )
                {
                    forward =
                        Vector3.forward;
                }
                else
                {
                    forward.Normalize();
                }

                Vector3 right =
                    Vector3.Cross(
                        Vector3.up,
                        forward).normalized;

                Vector3 basePosition =
                    ResolveGroundPoint(
                        figureMotor.transform.position,
                        figureMotor.transform);

                Vector3 desiredA =
                    basePosition +
                    forward *
                    3.25f +
                    right *
                    3.25f;

                Vector3 surfaceA =
                    ResolveGroundPoint(
                        desiredA,
                        figureMotor.transform);

                Vector3 desiredB =
                    surfaceA +
                    forward *
                    7.50f;

                Vector3 surfaceB =
                    ResolveGroundPoint(
                        desiredB,
                        figureMotor.transform);

                Vector3 surfaceDirection =
                    Vector3.ProjectOnPlane(
                        surfaceB -
                        surfaceA,
                        Vector3.up);

                float surfaceLength =
                    surfaceDirection.magnitude;

                if (
                    surfaceLength <
                    4f
                )
                {
                    surfaceB =
                        surfaceA +
                        forward *
                        7.50f;

                    surfaceDirection =
                        surfaceB -
                        surfaceA;

                    surfaceLength =
                        surfaceDirection.magnitude;
                }

                surfaceDirection.Normalize();

                Vector3 surfaceRight =
                    Vector3.Cross(
                        Vector3.up,
                        surfaceDirection).normalized;

                Vector3 underlayerOffset =
                    surfaceRight *
                    22f +
                    Vector3.up *
                    0.25f;

                Vector3 underA =
                    surfaceA +
                    underlayerOffset;

                Vector3 underB =
                    surfaceB +
                    underlayerOffset;

                Transform surfaceAnchorA =
                    CreateAnchor(
                        lab.transform,
                        "SurfaceSeam_A",
                        surfaceA,
                        Quaternion.LookRotation(
                            surfaceDirection,
                            Vector3.up));

                Transform surfaceAnchorB =
                    CreateAnchor(
                        lab.transform,
                        "SurfaceSeam_B",
                        surfaceB,
                        Quaternion.LookRotation(
                            -surfaceDirection,
                            Vector3.up));

                Transform underAnchorA =
                    CreateAnchor(
                        lab.transform,
                        "UnderlayerSeam_A",
                        underA,
                        Quaternion.LookRotation(
                            surfaceDirection,
                            Vector3.up));

                Transform underAnchorB =
                    CreateAnchor(
                        lab.transform,
                        "UnderlayerSeam_B",
                        underB,
                        Quaternion.LookRotation(
                            -surfaceDirection,
                            Vector3.up));

                CreateMarker(
                    surfaceAnchorA,
                    "A_MARKER",
                    seamMaterial);

                CreateMarker(
                    surfaceAnchorB,
                    "B_MARKER",
                    seamMaterial);

                CreateMarker(
                    underAnchorA,
                    "A_UNDER_MARKER",
                    seamMaterial);

                CreateMarker(
                    underAnchorB,
                    "B_UNDER_MARKER",
                    seamMaterial);

                CreateWorldLabel(
                    surfaceAnchorA,
                    "M52 DİKİŞ A\n1 + E",
                    new Vector3(
                        0f,
                        1.25f,
                        0f));

                CreateWorldLabel(
                    surfaceAnchorB,
                    "M52 DİKİŞ B\n1 + E",
                    new Vector3(
                        0f,
                        1.25f,
                        0f));

                CreateWorldLabel(
                    underAnchorA,
                    "ALT A\n1 + E ÇIKIŞ",
                    new Vector3(
                        0f,
                        1.15f,
                        0f));

                CreateWorldLabel(
                    underAnchorB,
                    "ALT B\n1 + E ÇIKIŞ",
                    new Vector3(
                        0f,
                        1.15f,
                        0f));

                CreateSurfaceBarrier(
                    lab.transform,
                    surfaceA,
                    surfaceB,
                    surfaceRight,
                    surfaceBarrierMaterial);

                CreateUnderlayerCorridor(
                    lab.transform,
                    underA,
                    underB,
                    underlayerFloorMaterial,
                    underlayerRailMaterial);

                PrototypeUnderlayerSeam seamA =
                    GetOrAdd<
                        PrototypeUnderlayerSeam>(
                            surfaceAnchorA.gameObject);

                seamA.Configure(
                    PrototypeUnderlayerSeamId.A,
                    surfaceAnchorA,
                    underAnchorA);

                PrototypeUnderlayerSeam seamB =
                    GetOrAdd<
                        PrototypeUnderlayerSeam>(
                            surfaceAnchorB.gameObject);

                seamB.Configure(
                    PrototypeUnderlayerSeamId.B,
                    surfaceAnchorB,
                    underAnchorB);

                PrototypeUnderlayerDiveController controller =
                    GetOrAdd<
                        PrototypeUnderlayerDiveController>(
                            root);

                RectTransform panel =
                    CreatePanel(
                        hudRoot,
                        PanelName,
                        new Vector2(0f, 0f),
                        new Vector2(0f, 0f),
                        new Vector2(0f, 0f),
                        new Vector2(20f, 20f),
                        new Vector2(470f, 132f));

                CanvasGroup group =
                    GetOrAdd<CanvasGroup>(
                        panel.gameObject);

                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;

                Text title =
                    CreateText(
                        panel,
                        "TitleText",
                        "ALT KATMAN • DİKİŞ GİRİŞİ",
                        13,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.73f),
                        new Vector2(1f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -8f),
                        new Vector2(-24f, -4f));

                Text state =
                    CreateText(
                        panel,
                        "StateText",
                        "Alt Katman dikişi bekleniyor.",
                        10,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0.25f),
                        new Vector2(1f, 0.76f),
                        new Vector2(0f, 1f),
                        new Vector2(14f, -1f),
                        new Vector2(-24f, -3f));

                Text controls =
                    CreateText(
                        panel,
                        "ControlsText",
                        "1 • PALET BIÇAĞI   DİKİŞE YAKLAŞ   MEVCUT E",
                        9,
                        FontStyle.Normal,
                        TextAnchor.LowerLeft,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.28f),
                        new Vector2(0f, 0f),
                        new Vector2(14f, 6f),
                        new Vector2(-24f, -2f));

                controller.Configure(
                    figureMotor.transform,
                    figureMotor,
                    paletteKnife,
                    traceWeave,
                    seamA,
                    seamB,
                    group,
                    title,
                    state,
                    controls);

                EditorUtility.SetDirty(
                    controller);

                EditorUtility.SetDirty(
                    seamA);

                EditorUtility.SetDirty(
                    seamB);

                EditorSceneManager.MarkSceneDirty(
                    root.scene);

                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M52.0 Setup] Underlayer Dive Risk Spike C1 ready.\n" +
                    $"Controller={GetHierarchyPath(controller.transform)}\n" +
                    $"FigureMotor={GetHierarchyPath(figureMotor.transform)}\n" +
                    $"PaletteKnife={GetHierarchyPath(paletteKnife.transform)}\n" +
                    $"TraceWeaveRecorder={(traceWeave != null ? GetHierarchyPath(traceWeave.transform) : "OPTIONAL")}\n" +
                    $"SurfaceA={surfaceA:F3}\n" +
                    $"SurfaceB={surfaceB:F3}\n" +
                    $"UnderA={underA:F3}\n" +
                    $"UnderB={underB:F3}\n" +
                    "ControlledEntryOnly=True\n" +
                    "AnywhereWallPhaseEnabled=False\n" +
                    "UsesParallelTraversalGraph=True\n" +
                    "PaletteKnifeUsesExistingUseToolActionReadOnly=True\n" +
                    "AddsNewGameplayInputAction=False\n" +
                    "PainterCanSeeSurfaceSignal=True\n" +
                    "FigureFullyInvisibleToPainter=False\n" +
                    "SafeSeamRecoveryEnabled=True\n" +
                    "TraceWeavePortalJumpSuppressed=True\n" +
                    "NetworkAuthorityEnabled=False",
                    controller);

                EditorUtility.DisplayDialog(
                    "M52.0 Hazır",
                    "İki yüzey dikişi, paralel Alt Katman koridoru, mevcut Palet " +
                    "Bıçağı E ile giriş/çıkış, Ressam yüzey sinyali ve güvenli " +
                    "dikiş recovery prototipi kuruldu.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M52.0 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "52.0 - Force Enter Underlayer A (Play Mode)")]
        public static void ForceEnterA()
        {
            FindPlayModeController()?
                .ForceEnterFromA();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "52.0 - Force Surface Nearest (Play Mode)")]
        public static void ForceSurface()
        {
            FindPlayModeController()?
                .ForceSurfaceNearest();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "52.0 - Force Safety Recovery (Play Mode)")]
        public static void ForceRecovery()
        {
            FindPlayModeController()?
                .ForceSafetyRecovery();
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "52.0 - Diagnose Underlayer Dive")]
        public static void Diagnose()
        {
            PrototypeUnderlayerDiveController controller =
                FindOptional<
                    PrototypeUnderlayerDiveController>();

            PrototypeUnderlayerSeam[] seams =
                UnityEngine.Object.FindObjectsByType<
                    PrototypeUnderlayerSeam>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            string report =
                "[M52.0 Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"SeamCount={seams.Length}\n" +
                $"ResolvedRole={(controller != null ? controller.ResolvedRole : "N/A")}\n" +
                $"FigureRoleActive={(controller != null && controller.FigureRoleActive)}\n" +
                $"State={(controller != null ? controller.State.ToString() : "N/A")}\n" +
                $"InUnderlayer={(controller != null && controller.InUnderlayer)}\n" +
                $"NearestSeam={(controller != null ? controller.NearestSeam : "N/A")}\n" +
                $"NearestSeamDistance={(controller != null ? controller.NearestSeamDistance : -1f):F2}\n" +
                $"PaletteKnifeActive={(controller != null && controller.PaletteKnifeActive)}\n" +
                $"PaletteKnifeInputResolved={(controller != null && controller.PaletteKnifeInputResolved)}\n" +
                $"PaletteKnifePressed={(controller != null && controller.PaletteKnifePressed)}\n" +
                $"PaletteKnifeContract={(controller != null ? controller.PaletteKnifeInputContract : "N/A")}\n" +
                $"DiveCount={(controller != null ? controller.DiveCount : 0)}\n" +
                $"SurfaceCount={(controller != null ? controller.SurfaceCount : 0)}\n" +
                $"RejectedUseCount={(controller != null ? controller.RejectedUseCount : 0)}\n" +
                $"SafetyRecoveryCount={(controller != null ? controller.SafetyRecoveryCount : 0)}\n" +
                $"UnderlayerProgress={(controller != null ? controller.UnderlayerProgress : 0f):F2}\n" +
                $"GraphDistance={(controller != null ? controller.GraphDistance : 0f):F2}\n" +
                $"ProjectedSurfacePosition={(controller != null ? controller.ProjectedSurfacePosition.ToString("F3") : "N/A")}\n" +
                $"SurfaceSignalVisible={(controller != null && controller.SurfaceSignalVisible)}\n" +
                $"TraceRecorderSuppressed={(controller != null && controller.TraceRecorderSuppressed)}\n" +
                $"TraceSuppressCount={(controller != null ? controller.TraceSuppressCount : 0)}\n" +
                $"TraceRestoreCount={(controller != null ? controller.TraceRestoreCount : 0)}\n" +
                $"LastAction={(controller != null ? controller.LastAction : "N/A")}\n" +
                "ControlledEntryOnly=True\n" +
                "AnywhereWallPhaseEnabled=False\n" +
                "UsesParallelTraversalGraph=True\n" +
                "PaletteKnifeUsesExistingUseToolActionReadOnly=True\n" +
                "AddsNewGameplayInputAction=False\n" +
                "PainterCanSeeSurfaceSignal=True\n" +
                "FigureFullyInvisibleToPainter=False\n" +
                "SafeSeamRecoveryEnabled=True\n" +
                "TraceWeavePortalJumpSuppressed=True\n" +
                "NetworkAuthorityEnabled=False";

            Debug.Log(
                report,
                controller);

            EditorUtility.DisplayDialog(
                "M52.0 Diagnose",
                report,
                "Tamam");
        }

        private static PrototypeUnderlayerDiveController
            FindPlayModeController()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M52.0",
                    "Bu komut yalnız Play Mode'da çalışır.",
                    "Tamam");

                return null;
            }

            PrototypeUnderlayerDiveController controller =
                FindOptional<
                    PrototypeUnderlayerDiveController>();

            if (controller == null)
            {
                Debug.LogError(
                    "[M52.0] Controller bulunamadı.");
            }

            return controller;
        }

        private static Vector3 ResolveGroundPoint(
            Vector3 desired,
            Transform figureRoot)
        {
            Vector3 origin =
                desired +
                Vector3.up *
                8f;

            RaycastHit[] hits =
                Physics.RaycastAll(
                    origin,
                    Vector3.down,
                    24f,
                    ~0,
                    QueryTriggerInteraction.Ignore);

            Array.Sort(
                hits,
                (
                    left,
                    right) =>
                    left.distance.CompareTo(
                        right.distance));

            for (int index = 0;
                 index < hits.Length;
                 index++)
            {
                RaycastHit hit =
                    hits[index];

                if (
                    hit.collider == null ||
                    (
                        figureRoot != null &&
                        (
                            hit.collider.transform ==
                                figureRoot ||
                            hit.collider.transform.IsChildOf(
                                figureRoot)
                        )
                    )
                )
                {
                    continue;
                }

                if (
                    Vector3.Dot(
                        hit.normal,
                        Vector3.up) <
                    0.45f
                )
                {
                    continue;
                }

                return hit.point;
            }

            return new Vector3(
                desired.x,
                figureRoot != null
                    ? figureRoot.position.y
                    : desired.y,
                desired.z);
        }

        private static Transform CreateAnchor(
            Transform parent,
            string name,
            Vector3 position,
            Quaternion rotation)
        {
            GameObject anchor =
                new GameObject(
                    name);

            Undo.RegisterCreatedObjectUndo(
                anchor,
                $"Create {name}");

            anchor.transform.SetParent(
                parent,
                true);

            anchor.transform.SetPositionAndRotation(
                position,
                rotation);

            return anchor.transform;
        }

        private static void CreateMarker(
            Transform parent,
            string name,
            Material material)
        {
            GameObject marker =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cylinder);

            marker.name = name;

            Undo.RegisterCreatedObjectUndo(
                marker,
                $"Create {name}");

            marker.transform.SetParent(
                parent,
                false);

            marker.transform.localPosition =
                new Vector3(
                    0f,
                    0.055f,
                    0f);

            marker.transform.localScale =
                new Vector3(
                    0.62f,
                    0.035f,
                    0.62f);

            Renderer renderer =
                marker.GetComponent<
                    Renderer>();

            if (
                renderer != null &&
                material != null
            )
            {
                renderer.sharedMaterial =
                    material;
            }

            Collider collider =
                marker.GetComponent<
                    Collider>();

            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    collider);
            }
        }

        private static void CreateWorldLabel(
            Transform parent,
            string value,
            Vector3 localPosition)
        {
            GameObject labelObject =
                new GameObject(
                    "Label");

            Undo.RegisterCreatedObjectUndo(
                labelObject,
                "Create M52 Label");

            labelObject.transform.SetParent(
                parent,
                false);

            labelObject.transform.localPosition =
                localPosition;

            labelObject.transform.localRotation =
                Quaternion.identity;

            TextMesh label =
                labelObject.AddComponent<
                    TextMesh>();

            label.text = value;
            label.anchor =
                TextAnchor.MiddleCenter;

            label.alignment =
                TextAlignment.Center;

            label.characterSize =
                0.11f;

            label.fontSize = 42;

            label.color =
                new Color(
                    0.95f,
                    0.78f,
                    0.45f,
                    1f);
        }

        private static void CreateSurfaceBarrier(
            Transform parent,
            Vector3 surfaceA,
            Vector3 surfaceB,
            Vector3 surfaceRight,
            Material material)
        {
            Vector3 center =
                (
                    surfaceA +
                    surfaceB
                ) *
                0.5f +
                Vector3.up *
                1.55f;

            Vector3 direction =
                Vector3.ProjectOnPlane(
                    surfaceB -
                    surfaceA,
                    Vector3.up).normalized;

            GameObject barrier =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);

            barrier.name =
                "M52_SurfaceBarrier_TestOnly";

            Undo.RegisterCreatedObjectUndo(
                barrier,
                "Create M52 Surface Barrier");

            barrier.transform.SetParent(
                parent,
                true);

            barrier.transform.position =
                center;

            barrier.transform.rotation =
                Quaternion.LookRotation(
                    direction,
                    Vector3.up);

            barrier.transform.localScale =
                new Vector3(
                    5.2f,
                    3.1f,
                    0.62f);

            Renderer renderer =
                barrier.GetComponent<
                    Renderer>();

            if (
                renderer != null &&
                material != null
            )
            {
                renderer.sharedMaterial =
                    material;
            }
        }

        private static void CreateUnderlayerCorridor(
            Transform parent,
            Vector3 underA,
            Vector3 underB,
            Material floorMaterial,
            Material railMaterial)
        {
            Vector3 direction =
                Vector3.ProjectOnPlane(
                    underB -
                    underA,
                    Vector3.up);

            float length =
                direction.magnitude;

            direction.Normalize();

            Vector3 right =
                Vector3.Cross(
                    Vector3.up,
                    direction).normalized;

            Vector3 center =
                (
                    underA +
                    underB
                ) *
                0.5f;

            GameObject floor =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);

            floor.name =
                "M52_UnderlayerFloor";

            Undo.RegisterCreatedObjectUndo(
                floor,
                "Create M52 Underlayer Floor");

            floor.transform.SetParent(
                parent,
                true);

            floor.transform.position =
                center -
                Vector3.up *
                0.18f;

            floor.transform.rotation =
                Quaternion.LookRotation(
                    direction,
                    Vector3.up);

            floor.transform.localScale =
                new Vector3(
                    3.5f,
                    0.34f,
                    length +
                    2.0f);

            Renderer floorRenderer =
                floor.GetComponent<
                    Renderer>();

            if (
                floorRenderer != null &&
                floorMaterial != null
            )
            {
                floorRenderer.sharedMaterial =
                    floorMaterial;
            }

            CreateRail(
                parent,
                center +
                right *
                1.78f +
                Vector3.up *
                0.52f,
                direction,
                length,
                "M52_UnderlayerRail_Left",
                railMaterial);

            CreateRail(
                parent,
                center -
                right *
                1.78f +
                Vector3.up *
                0.52f,
                direction,
                length,
                "M52_UnderlayerRail_Right",
                railMaterial);

            for (int index = 1;
                 index <= 4;
                 index++)
            {
                float t =
                    index /
                    5f;

                Vector3 ghostPosition =
                    Vector3.Lerp(
                        underA,
                        underB,
                        t) +
                    Vector3.up *
                    0.035f;

                GameObject ghost =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Cube);

                ghost.name =
                    $"M52_ErasedRouteGhost_{index:00}";

                Undo.RegisterCreatedObjectUndo(
                    ghost,
                    "Create M52 Route Ghost");

                ghost.transform.SetParent(
                    parent,
                    true);

                ghost.transform.position =
                    ghostPosition;

                ghost.transform.rotation =
                    Quaternion.LookRotation(
                        direction,
                        Vector3.up);

                ghost.transform.localScale =
                    new Vector3(
                        0.62f,
                        0.025f,
                        0.95f);

                Renderer ghostRenderer =
                    ghost.GetComponent<
                        Renderer>();

                if (
                    ghostRenderer != null &&
                    railMaterial != null
                )
                {
                    ghostRenderer.sharedMaterial =
                        railMaterial;
                }

                Collider ghostCollider =
                    ghost.GetComponent<
                        Collider>();

                if (ghostCollider != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        ghostCollider);
                }
            }
        }

        private static void CreateRail(
            Transform parent,
            Vector3 position,
            Vector3 direction,
            float length,
            string name,
            Material material)
        {
            GameObject rail =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);

            rail.name = name;

            Undo.RegisterCreatedObjectUndo(
                rail,
                $"Create {name}");

            rail.transform.SetParent(
                parent,
                true);

            rail.transform.position =
                position;

            rail.transform.rotation =
                Quaternion.LookRotation(
                    direction,
                    Vector3.up);

            rail.transform.localScale =
                new Vector3(
                    0.18f,
                    1.05f,
                    length +
                    1.8f);

            Renderer renderer =
                rail.GetComponent<
                    Renderer>();

            if (
                renderer != null &&
                material != null
            )
            {
                renderer.sharedMaterial =
                    material;
            }
        }

        private static Material GetOrCreateMaterial(
            string path,
            Color color)
        {
            Material existing =
                AssetDatabase.LoadAssetAtPath<
                    Material>(
                        path);

            if (existing != null)
            {
                existing.color = color;
                EditorUtility.SetDirty(
                    existing);

                return existing;
            }

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit");

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Standard");
            }

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "M52 prototype materyali için shader bulunamadı.");
            }

            Material material =
                new Material(
                    shader)
                {
                    color = color,
                    name =
                        Path.GetFileNameWithoutExtension(
                            path)
                };

            AssetDatabase.CreateAsset(
                material,
                path);

            return material;
        }

        private static void EnsureFolder(
            string folderPath)
        {
            string[] parts =
                folderPath.Split('/');

            string current =
                parts[0];

            for (int index = 1;
                 index < parts.Length;
                 index++)
            {
                string next =
                    current +
                    "/" +
                    parts[index];

                if (
                    !AssetDatabase.IsValidFolder(
                        next)
                )
                {
                    AssetDatabase.CreateFolder(
                        current,
                        parts[index]);
                }

                current = next;
            }
        }

        private static void ClearChildren(
            Transform target)
        {
            for (int index =
                     target.childCount - 1;
                 index >= 0;
                 index--)
            {
                UnityEngine.Object.DestroyImmediate(
                    target.GetChild(index)
                        .gameObject);
            }
        }

        private static MonoBehaviour FindRelatedMonoBehaviour(
            Transform root,
            string typeName)
        {
            if (root == null)
            {
                return null;
            }

            MonoBehaviour[] related =
                root.GetComponentsInChildren<
                    MonoBehaviour>(
                        true);

            for (int index = 0;
                 index < related.Length;
                 index++)
            {
                MonoBehaviour candidate =
                    related[index];

                if (
                    candidate != null &&
                    candidate.GetType().Name ==
                        typeName
                )
                {
                    return candidate;
                }
            }

            related =
                root.GetComponentsInParent<
                    MonoBehaviour>(
                        true);

            for (int index = 0;
                 index < related.Length;
                 index++)
            {
                MonoBehaviour candidate =
                    related[index];

                if (
                    candidate != null &&
                    candidate.GetType().Name ==
                        typeName
                )
                {
                    return candidate;
                }
            }

            return FindOptionalMonoBehaviour(
                typeName);
        }

        private static MonoBehaviour FindRequiredMonoBehaviour(
            string typeName)
        {
            MonoBehaviour found =
                FindOptionalMonoBehaviour(
                    typeName);

            if (found == null)
            {
                throw new InvalidOperationException(
                    $"{typeName} bulunamadı.");
            }

            return found;
        }

        private static MonoBehaviour FindOptionalMonoBehaviour(
            string typeName)
        {
            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<
                    MonoBehaviour>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            for (int index = 0;
                 index < behaviours.Length;
                 index++)
            {
                MonoBehaviour candidate =
                    behaviours[index];

                if (
                    candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.gameObject.scene.IsValid() &&
                    candidate.GetType().Name ==
                        typeName
                )
                {
                    return candidate;
                }
            }

            return null;
        }

        private static GameObject GetOrCreateRoot(
            string name)
        {
            GameObject existing =
                GameObject.Find(
                    name);

            if (existing != null)
            {
                return existing;
            }

            GameObject root =
                new GameObject(
                    name);

            Undo.RegisterCreatedObjectUndo(
                root,
                $"Create {name}");

            return root;
        }

        private static GameObject GetOrCreateChild(
            Transform parent,
            string name)
        {
            Transform existing =
                parent.Find(
                    name);

            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject child =
                new GameObject(
                    name);

            Undo.RegisterCreatedObjectUndo(
                child,
                $"Create {name}");

            child.transform.SetParent(
                parent,
                false);

            return child;
        }

        private static RectTransform CreatePanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            RectTransform rect =
                GetOrCreateRect(
                    parent,
                    name,
                    anchorMin,
                    anchorMax,
                    pivot,
                    anchoredPosition,
                    sizeDelta);

            Image image =
                GetOrAdd<Image>(
                    rect.gameObject);

            image.color =
                new Color(
                    0.045f,
                    0.040f,
                    0.050f,
                    0.95f);

            image.raycastTarget = false;

            return rect;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            RectTransform rect =
                GetOrCreateRect(
                    parent,
                    name,
                    anchorMin,
                    anchorMax,
                    pivot,
                    anchoredPosition,
                    sizeDelta);

            Text text =
                GetOrAdd<Text>(
                    rect.gameObject);

            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;

            text.color =
                new Color(
                    0.93f,
                    0.86f,
                    0.72f,
                    1f);

            text.raycastTarget = false;
            text.supportRichText = true;

            text.horizontalOverflow =
                HorizontalWrapMode.Wrap;

            text.verticalOverflow =
                VerticalWrapMode.Truncate;

            if (text.font == null)
            {
                text.font =
                    Resources.GetBuiltinResource<Font>(
                        "LegacyRuntime.ttf");
            }

            return text;
        }

        private static RectTransform GetOrCreateRect(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            Transform existing =
                parent.Find(
                    name);

            GameObject child =
                existing != null
                    ? existing.gameObject
                    : new GameObject(
                        name,
                        typeof(RectTransform));

            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(
                    child,
                    $"Create {name}");

                child.transform.SetParent(
                    parent,
                    false);
            }

            RectTransform rect =
                child.GetComponent<
                    RectTransform>();

            if (rect == null)
            {
                throw new InvalidOperationException(
                    $"{name} RectTransform içermiyor.");
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;

            rect.anchoredPosition =
                anchoredPosition;

            rect.sizeDelta =
                sizeDelta;

            rect.localScale =
                Vector3.one;

            return rect;
        }

        private static T GetOrAdd<T>(
            GameObject target)
            where T : Component
        {
            T existing =
                target.GetComponent<T>();

            if (existing != null)
            {
                return existing;
            }

            T added =
                Undo.AddComponent<T>(
                    target);

            if (added == null)
            {
                throw new InvalidOperationException(
                    $"{typeof(T).Name} eklenemedi.");
            }

            return added;
        }

        private static T FindOptional<T>()
            where T : Component
        {
            T[] objects =
                UnityEngine.Object.FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int index = 0;
                 index < objects.Length;
                 index++)
            {
                T candidate =
                    objects[index];

                if (
                    candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.gameObject.scene.IsValid()
                )
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string GetHierarchyPath(
            Transform target)
        {
            if (target == null)
            {
                return "NULL";
            }

            string path =
                target.name;

            while (target.parent != null)
            {
                target =
                    target.parent;

                path =
                    target.name +
                    "/" +
                    path;
            }

            return path;
        }
    }
}
#endif
