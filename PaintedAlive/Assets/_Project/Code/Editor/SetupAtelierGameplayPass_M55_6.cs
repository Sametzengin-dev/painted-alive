#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using PaintedAlive.Core.Scoring;
using PaintedAlive.Environment.Atelier;
using PaintedAlive.Figures;
using PaintedAlive.Figures.StainSupport.FrameExit;
using PaintedAlive.MatchFlow;
using PaintedAlive.Paint.Watercolor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PaintedAlive.EditorTools
{
    /// <summary>
    /// M55.6 — Converts the already-integrated M55.4 Atelier FBX into a stable
    /// gameplay test map without rewriting Figure tools or Painter systems.
    ///
    /// Main responsibilities:
    /// - Preserve the original M55.4 stair collision that was already play-tested as stable.
    /// - Add lightweight environment semantics while preserving existing layer contracts.
    /// - Build platform route anchors.
    /// - Move M54/M36/M37 start/finish contracts onto the real Atelier route.
    /// - Author six reset-safe environmental M13 Watercolor flows from the basin visuals.
    /// - Diagnose traversal, route, water, and sightline readiness.
    /// </summary>
    public static class SetupAtelierGameplayPass_M55_6
    {
        private const string MenuRoot = "Tools/Painted Alive/Milestones/";
        private const string IntegrationRootName = "M55_4_AtelierTestScene";
        private const string ModelInstanceName = "MODEL_Atelier_Heights";
        private const string GeneratedRootName = "M55_6_AtelierGameplayPass";
        private const string TraversalRootName = "TraversalProxies";
        private const string RouteRootName = "RouteAnchors";
        private const string WatercolorRootName = "EnvironmentalWatercolor";
        private const string PaintSurfaceLayerName = "PaintSurface";

        private const string M55_4FigureSpawnName = "FigureSpawn_START";
        private const string FinalPlatformName = "PLATFORM | UPPER_GARDEN";
        private const string FinalFrameLintel = "FRAME | UPPER_GARDEN | lintel";
        private const string FinalFrameUprightA = "FRAME | UPPER_GARDEN | upright";
        private const string FinalFrameUprightB = "FRAME | UPPER_GARDEN | upright.001";

        private const string WatercolorConfigPath = "Assets/_Project/Data/Paint/WatercolorFlowConfig.asset";
        private const string WatercolorMaterialPath = "Assets/_Project/Materials/Environment/AtelierTest/MAT_Atelier_Watercolor.mat";
        private const string FrameExitConfigPath = "Assets/_Project/Data/Figures/DA_StainFrameExitRules.asset";
        private const string JourneyScoreConfigPath = "Assets/_Project/Data/Core/DA_PrototypeJourneyScore.asset";

        private const float StairProxyThickness = 0.10f;
        private const float StairProxyEntryOverlap = 0.28f;
        private const float StairProxyExitOverlap = 0.18f;
        private const float StairProxyEntryDrop = 0.09f;
        private const float StairProxySideInset = 0.10f;
        private const float RouteAnchorLift = 0.08f;
        private const float ExitTriggerDepth = 1.25f;

        [MenuItem(MenuRoot + "55.6 - Setup + Repair Atelier Gameplay Pass")]
        public static void SetupOrRepair()
        {
            try
            {
                if (Application.isPlaying)
                    throw new InvalidOperationException("M55.6 setup Play Mode dışında çalıştırılmalıdır.");

                Scene scene = SceneManager.GetActiveScene();
                if (!scene.IsValid() || !scene.isLoaded)
                    throw new InvalidOperationException("Aktif ve yüklü bir Scene bulunamadı.");

                GameObject integrationRoot = GameObject.Find(IntegrationRootName);
                if (integrationRoot == null)
                    throw new InvalidOperationException("M55.4 Atelier root bulunamadı. Önce çalışan M55.4 entegrasyonunun sahnede olduğundan emin ol.");

                Transform model = FindChildRecursive(integrationRoot.transform, ModelInstanceName);
                if (model == null)
                    throw new InvalidOperationException("M55.4 MODEL_Atelier_Heights bulunamadı.");

                int paintSurfaceLayer = LayerMask.NameToLayer(PaintSurfaceLayerName);
                if (paintSurfaceLayer < 0)
                    throw new InvalidOperationException("PaintSurface layer bulunamadı. M55.4 setup bu layer'ı oluşturmuş olmalı.");

                GameObject generatedRoot = GetOrCreateChild(integrationRoot.transform, GeneratedRootName).gameObject;
                Transform traversalRoot = GetOrCreateChild(generatedRoot.transform, TraversalRootName);
                Transform routeRoot = GetOrCreateChild(generatedRoot.transform, RouteRootName);
                Transform watercolorRoot = GetOrCreateChild(generatedRoot.transform, WatercolorRootName);

                // Idempotent repair: restore source stair colliders from the previous pass
                // before deleting/rebuilding generated proxies.
                RestorePriorStairColliders(integrationRoot.transform);
                ClearChildren(traversalRoot);
                ClearChildren(routeRoot);
                ClearChildren(watercolorRoot);

                SemanticStats semanticStats = ApplySurfaceSemantics(model, paintSurfaceLayer);
                // M55.6.4 regression rollback: the original M55.4 stair colliders were already
                // verified by playtest. Generated ramp proxies introduced blocking on later stairs,
                // so keep TraversalProxies empty and preserve the raw stair collision.
                int stairProxyCount = 0;
                Dictionary<string, Transform> routeAnchors = BuildRouteAnchors(model, routeRoot);

                Transform startAnchor = ResolveStartAnchor(integrationRoot.transform, routeAnchors);
                FinalFrameInfo finalFrame = ResolveFinalFrame(model, startAnchor.position);
                Transform finalAnchor = CreateOrUpdateAnchor(routeRoot, "Route_FINAL_FRAME", finalFrame.Center, finalFrame.Rotation);

                MatchBindingStats matchStats = BindExistingMatchContracts(startAnchor, finalAnchor, finalFrame);
                int watercolorZoneCount = BuildEnvironmentalWatercolorZones(model, watercolorRoot);

                EditorUtility.SetDirty(generatedRoot);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                string message =
                    "Atelier gameplay pass kuruldu.\n\n" +
                    $"Stair traversal proxies: {stairProxyCount} (original M55.4 stair collision preserved)\n" +
                    $"Route anchors: {routeAnchors.Count + 1}\n" +
                    $"Environmental M13 watercolor zones: {watercolorZoneCount}\n" +
                    $"Paintable semantics: {semanticStats.Paintable}\n" +
                    $"Solid semantics: {semanticStats.Solid}\n" +
                    $"Watercolor visual semantics: {semanticStats.Watercolor}\n\n" +
                    $"M54 anchors rebound: {matchStats.M54Anchors}\n" +
                    $"M36 frame gate rebound: {matchStats.M36Gate}\n" +
                    $"M37 route rebound: {matchStats.M37Tracker}\n\n" +
                    "Existing Figure/Painter tool controller files modified: 0\n" +
                    "Legacy prototype Ground automatically disabled: HAYIR\n\n" +
                    "Şimdi 55.6 - Diagnose Atelier Gameplay Pass çalıştır.";

                Debug.Log("[Painted Alive M55.6] " + message.Replace("\n", " | "), generatedRoot);
                EditorUtility.DisplayDialog("Painted Alive M55.6", message, "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("M55.6 Setup Hatası", exception.Message, "Tamam");
            }
        }

        [MenuItem(MenuRoot + "55.6.4 - Restore Original Stair Collision")]
        public static void RestoreOriginalStairCollision()
        {
            try
            {
                if (Application.isPlaying)
                    throw new InvalidOperationException("M55.6.4 rollback Play Mode dışında çalıştırılmalıdır.");

                Scene scene = SceneManager.GetActiveScene();
                if (!scene.IsValid() || !scene.isLoaded)
                    throw new InvalidOperationException("Aktif ve yüklü bir Scene bulunamadı.");

                GameObject integrationRoot = GameObject.Find(IntegrationRootName);
                if (integrationRoot == null)
                    throw new InvalidOperationException("M55.4 Atelier root bulunamadı.");

                Transform model = FindChildRecursive(integrationRoot.transform, ModelInstanceName);
                if (model == null)
                    throw new InvalidOperationException("M55.4 MODEL_Atelier_Heights bulunamadı.");

                int paintSurfaceLayer = LayerMask.NameToLayer(PaintSurfaceLayerName);
                if (paintSurfaceLayer < 0)
                    throw new InvalidOperationException("PaintSurface layer bulunamadı.");

                GameObject generatedRoot = GetOrCreateChild(integrationRoot.transform, GeneratedRootName).gameObject;
                Transform traversalRoot = GetOrCreateChild(generatedRoot.transform, TraversalRootName);

                RestorePriorStairColliders(integrationRoot.transform);
                ClearChildren(traversalRoot);
                ApplySurfaceSemantics(model, paintSurfaceLayer);

                int stairs = 0;
                int colliders = 0;
                int enabled = 0;
                foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter == null || !Starts(filter.name, "STAIR |")) continue;
                    stairs++;
                    Collider collider = filter.GetComponent<Collider>();
                    if (collider == null) continue;
                    colliders++;
                    if (collider.enabled) enabled++;
                    EditorUtility.SetDirty(collider);
                }

                EditorUtility.SetDirty(generatedRoot);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();

                string message =
                    "M55.6 merdiven proxy regresyonu geri alındı.\n\n" +
                    $"STAIR mesh: {stairs}\n" +
                    $"Raw collider: {colliders}\n" +
                    $"Enabled raw collider: {enabled}\n" +
                    "Traversal proxy: 0\n\n" +
                    "M55.4 sırasında çalışan orijinal merdiven collision yapısı geri getirildi.\n" +
                    "Watercolor / route / match / Figure tools değiştirilmedi.";

                Debug.Log("[Painted Alive M55.6.4] " + message.Replace("\n", " | "), generatedRoot);
                EditorUtility.DisplayDialog("Painted Alive M55.6.4", message, "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("M55.6.4 Stair Rollback Hatası", exception.Message, "Tamam");
            }
        }

        [MenuItem(MenuRoot + "55.6.4 - Diagnose Original Stair Collision")]
        public static void DiagnoseOriginalStairCollision()
        {
            GameObject integrationRoot = GameObject.Find(IntegrationRootName);
            Transform model = integrationRoot != null ? FindChildRecursive(integrationRoot.transform, ModelInstanceName) : null;
            Transform generated = integrationRoot != null ? FindChildRecursive(integrationRoot.transform, GeneratedRootName) : null;
            Transform traversal = generated != null ? generated.Find(TraversalRootName) : null;

            int stairs = 0;
            int rawColliders = 0;
            int enabledRaw = 0;
            if (model != null)
            {
                foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter == null || !Starts(filter.name, "STAIR |")) continue;
                    stairs++;
                    Collider collider = filter.GetComponent<Collider>();
                    if (collider == null) continue;
                    rawColliders++;
                    if (collider.enabled) enabledRaw++;
                }
            }

            int proxies = traversal != null ? traversal.GetComponentsInChildren<Collider>(true).Length : 0;
            bool pass = integrationRoot != null && model != null && stairs > 0 && rawColliders == stairs && enabledRaw == rawColliders && proxies == 0;

            string report =
                $"Result={(pass ? "PASS" : "CHECK")}\n" +
                $"SourceStairs={stairs}\n" +
                $"RawStairColliders={rawColliders}\n" +
                $"EnabledRawStairColliders={enabledRaw}\n" +
                $"TraversalProxies={proxies}";

            Debug.Log("[Painted Alive M55.6.4 Diagnose]\n" + report, integrationRoot);
            EditorUtility.DisplayDialog("M55.6.4 Stair Diagnose", report, "Tamam");
        }

        public static void RepairStairTraversalProxies()
        {
            try
            {
                if (Application.isPlaying)
                    throw new InvalidOperationException("M55.6.3 repair Play Mode dışında çalıştırılmalıdır.");

                Scene scene = SceneManager.GetActiveScene();
                if (!scene.IsValid() || !scene.isLoaded)
                    throw new InvalidOperationException("Aktif ve yüklü bir Scene bulunamadı.");

                GameObject integrationRoot = GameObject.Find(IntegrationRootName);
                if (integrationRoot == null)
                    throw new InvalidOperationException("M55.4 Atelier root bulunamadı.");

                Transform model = FindChildRecursive(integrationRoot.transform, ModelInstanceName);
                if (model == null)
                    throw new InvalidOperationException("M55.4 MODEL_Atelier_Heights bulunamadı.");

                int paintSurfaceLayer = LayerMask.NameToLayer(PaintSurfaceLayerName);
                if (paintSurfaceLayer < 0)
                    throw new InvalidOperationException("PaintSurface layer bulunamadı.");

                GameObject generatedRoot = GetOrCreateChild(integrationRoot.transform, GeneratedRootName).gameObject;
                Transform traversalRoot = GetOrCreateChild(generatedRoot.transform, TraversalRootName);

                // Only stair collision is repaired. Watercolor, routes, match anchors and tools are untouched.
                RestorePriorStairColliders(integrationRoot.transform);
                ClearChildren(traversalRoot);
                ApplySurfaceSemantics(model, paintSurfaceLayer);
                int stairProxyCount = BuildStairTraversalProxies(model, traversalRoot, paintSurfaceLayer);

                EditorUtility.SetDirty(generatedRoot);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();

                string message =
                    "Merdiven traversal proxy'leri yeniden kuruldu.\n\n" +
                    $"Yeni proxy sayısı: {stairProxyCount}\n" +
                    "Raw STAIR collider'ları yalnız başarılı proxy üretilen merdivenlerde kapatıldı.\n" +
                    "Watercolor / route / Figure tools değiştirilmedi.\n\n" +
                    "Şimdi Play Mode'da START -> LOWER_APPROACH merdivenini Walk ve Sprint ile test et.";

                Debug.Log("[Painted Alive M55.6.3] " + message.Replace("\n", " | "), generatedRoot);
                EditorUtility.DisplayDialog("Painted Alive M55.6.3", message, "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("M55.6.3 Stair Repair Hatası", exception.Message, "Tamam");
            }
        }

        public static void DiagnoseStairTraversalProxies()
        {
            GameObject integrationRoot = GameObject.Find(IntegrationRootName);
            Transform model = integrationRoot != null ? FindChildRecursive(integrationRoot.transform, ModelInstanceName) : null;
            Transform generated = integrationRoot != null ? FindChildRecursive(integrationRoot.transform, GeneratedRootName) : null;
            Transform traversal = generated != null ? generated.Find(TraversalRootName) : null;

            var lines = new List<string>();
            bool pass = integrationRoot != null && model != null && traversal != null;
            int sourceCount = 0;
            int sourceEnabled = 0;
            int proxyCount = 0;
            int suspicious = 0;

            if (model != null)
            {
                foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter == null || !Starts(filter.name, "STAIR |")) continue;
                    sourceCount++;
                    Collider collider = filter.GetComponent<Collider>();
                    if (collider != null && collider.enabled) sourceEnabled++;
                }
            }

            if (traversal != null)
            {
                BoxCollider[] boxes = traversal.GetComponentsInChildren<BoxCollider>(true);
                proxyCount = boxes.Length;
                foreach (BoxCollider box in boxes)
                {
                    if (box == null) continue;
                    Vector3 forward = box.transform.forward.normalized;
                    float horizontal = new Vector2(forward.x, forward.z).magnitude;
                    float slopeAngle = Mathf.Atan2(Mathf.Abs(forward.y), Mathf.Max(0.0001f, horizontal)) * Mathf.Rad2Deg;
                    if (slopeAngle < 1f || slopeAngle > 50f || box.size.z < 0.5f || box.size.x < 0.6f)
                    {
                        suspicious++;
                        lines.Add($"WARN | {box.name} | slope={slopeAngle:0.0}deg size={box.size}");
                    }
                }
            }

            pass &= sourceCount > 0 && proxyCount == sourceCount && sourceEnabled == 0 && suspicious == 0;
            lines.Insert(0, $"Result={(pass ? "PASS" : "CHECK")}");
            lines.Insert(1, $"SourceStairs={sourceCount}");
            lines.Insert(2, $"TraversalProxies={proxyCount}");
            lines.Insert(3, $"EnabledRawStairColliders={sourceEnabled}");
            lines.Insert(4, $"SuspiciousProxyFits={suspicious}");

            string report = string.Join("\n", lines);
            Debug.Log("[Painted Alive M55.6.3 Diagnose]\n" + report, integrationRoot);
            EditorUtility.DisplayDialog("M55.6.3 Stair Diagnose", report, "Tamam");
        }

        [MenuItem(MenuRoot + "55.6 - Diagnose Atelier Gameplay Pass")]
        public static void Diagnose()
        {
            GameObject integrationRoot = GameObject.Find(IntegrationRootName);
            Transform model = integrationRoot != null
                ? FindChildRecursive(integrationRoot.transform, ModelInstanceName)
                : null;
            Transform generated = integrationRoot != null
                ? FindChildRecursive(integrationRoot.transform, GeneratedRootName)
                : null;

            var lines = new List<string>();
            bool pass = true;

            AddCheck(lines, ref pass, integrationRoot != null, "M55.4IntegrationRoot", integrationRoot != null ? "OK" : "MISSING");
            AddCheck(lines, ref pass, model != null, "AtelierModel", model != null ? "OK" : "MISSING");
            AddCheck(lines, ref pass, generated != null, "M55.6GeneratedRoot", generated != null ? "OK" : "MISSING");

            int stairCount = 0;
            int stairProxyCount = 0;
            int enabledSourceStairColliders = 0;
            int routeAnchorCount = 0;
            int watercolorZones = 0;
            int waterTemplates = 0;

            if (model != null)
            {
                foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter == null) continue;
                    if (!filter.name.StartsWith("STAIR |", StringComparison.OrdinalIgnoreCase)) continue;
                    stairCount++;
                    Collider c = filter.GetComponent<Collider>();
                    if (c != null && c.enabled) enabledSourceStairColliders++;
                }
            }

            if (generated != null)
            {
                Transform traversal = generated.Find(TraversalRootName);
                if (traversal != null)
                    stairProxyCount = traversal.GetComponentsInChildren<BoxCollider>(true).Length;

                Transform route = generated.Find(RouteRootName);
                if (route != null)
                    routeAnchorCount = route.childCount;

                Transform water = generated.Find(WatercolorRootName);
                if (water != null)
                {
                    AtelierWatercolorFlowZone[] zones = water.GetComponentsInChildren<AtelierWatercolorFlowZone>(true);
                    watercolorZones = zones.Length;
                    for (int i = 0; i < zones.Length; i++)
                    {
                        if (zones[i] != null && zones[i].InactiveFlowTemplate != null)
                            waterTemplates++;
                    }
                }
            }

            AddCheck(lines, ref pass, stairCount > 0, "SourceStairs", stairCount.ToString());
            AddCheck(lines, ref pass, stairProxyCount == 0, "StairTraversalProxiesDisabled", stairProxyCount.ToString());
            AddCheck(lines, ref pass, enabledSourceStairColliders == stairCount, "RawStairCollisionRestored", $"enabled={enabledSourceStairColliders} source={stairCount}");
            AddCheck(lines, ref pass, routeAnchorCount >= 10, "RouteAnchors", routeAnchorCount.ToString());
            AddCheck(lines, ref pass, watercolorZones == 6, "EnvironmentalWatercolorZones", watercolorZones.ToString());
            AddCheck(lines, ref pass, waterTemplates == watercolorZones, "WatercolorTemplates", $"templates={waterTemplates} zones={watercolorZones}");

            PrototypeCoreMatchController coreMatch = FindSceneObject<PrototypeCoreMatchController>();
            PrototypeFrameExitGate legacyGate = FindSceneObject<PrototypeFrameExitGate>();
            PrototypeJourneyScoreTracker scoreTracker = FindSceneObject<PrototypeJourneyScoreTracker>();
            WatercolorFlowInteractor watercolorInteractor = FindSceneObject<WatercolorFlowInteractor>();

            AddCheck(lines, ref pass, coreMatch != null, "M54CoreMatch", coreMatch != null ? "OK" : "MISSING");
            AddCheck(lines, ref pass, legacyGate != null, "M36FrameExitGate", legacyGate != null ? "OK" : "MISSING");
            AddCheck(lines, ref pass, scoreTracker != null, "M37JourneyTracker", scoreTracker != null ? "OK" : "MISSING");
            AddCheck(lines, ref pass, watercolorInteractor != null, "M13FigureWatercolorInteractor", watercolorInteractor != null ? "OK" : "MISSING");

            Transform start = generated != null ? FindChildRecursive(generated, "Route_START") : null;
            Transform finish = generated != null ? FindChildRecursive(generated, "Route_FINAL_FRAME") : null;
            AddCheck(lines, ref pass, start != null, "RouteStart", start != null ? start.position.ToString("F2") : "MISSING");
            AddCheck(lines, ref pass, finish != null, "RouteFinish", finish != null ? finish.position.ToString("F2") : "MISSING");

            if (start != null && finish != null)
            {
                bool blocked = Physics.Linecast(
                    start.position + Vector3.up * 1.2f,
                    finish.position + Vector3.up * 1.2f,
                    out RaycastHit hit,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);

                lines.Add("INFO | StartToFinishDirectSightline | " +
                    (blocked ? "BLOCKED by " + (hit.collider != null ? hit.collider.name : "geometry") : "OPEN"));
            }

            lines.Add("INFO | FigureToolControllersModifiedByM55.6 | False");
            lines.Add("INFO | PainterToolControllersModifiedByM55.6 | False");
            lines.Add("INFO | UsesExistingM13Watercolor | True");
            lines.Add("INFO | LegacyGroundAutoDisabled | False");
            lines.Add("INFO | NewGameplayInputAdded | False");
            lines.Add("INFO | NetworkAuthorityAdded | False");

            string report =
                "Painted Alive M55.6 Atelier Gameplay Diagnostics\n" +
                "Result=" + (pass ? "PASS" : "NEEDS_ATTENTION") + "\n\n" +
                string.Join("\n", lines) +
                "\n\nPlay Mode traversal and tool checks are still required.";

            Debug.Log("[Painted Alive M55.6]\n" + report, generated != null ? generated.gameObject : null);
            EditorUtility.DisplayDialog("M55.6 Diagnostics", report, "Tamam");
        }

        [MenuItem(MenuRoot + "55.6 - Move Figure To Atelier START")]
        public static void MoveFigureToStart()
        {
            GameObject integrationRoot = GameObject.Find(IntegrationRootName);
            if (integrationRoot == null)
            {
                EditorUtility.DisplayDialog("M55.6", "M55.4 Atelier root bulunamadı.", "Tamam");
                return;
            }

            Transform generated = FindChildRecursive(integrationRoot.transform, GeneratedRootName);
            Transform start = generated != null ? FindChildRecursive(generated, "Route_START") : null;
            FigureMotor figure = FindSceneObject<FigureMotor>();

            if (start == null || figure == null)
            {
                EditorUtility.DisplayDialog("M55.6", "Route_START veya FigureMotor bulunamadı.", "Tamam");
                return;
            }

            CharacterController controller = figure.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null && wasEnabled) controller.enabled = false;
            figure.transform.SetPositionAndRotation(start.position, start.rotation);
            if (controller != null && wasEnabled) controller.enabled = true;

            EditorSceneManager.MarkSceneDirty(figure.gameObject.scene);
            Debug.Log("[Painted Alive M55.6] Figure Route_START noktasına taşındı.", figure);
        }

        [MenuItem(MenuRoot + "55.6 - Select Generated Gameplay Pass")]
        public static void SelectGeneratedRoot()
        {
            GameObject root = GameObject.Find(GeneratedRootName);
            if (root == null)
            {
                GameObject integration = GameObject.Find(IntegrationRootName);
                Transform found = integration != null ? FindChildRecursive(integration.transform, GeneratedRootName) : null;
                root = found != null ? found.gameObject : null;
            }

            Selection.activeGameObject = root;
            if (root != null) EditorGUIUtility.PingObject(root);
        }

        [MenuItem(MenuRoot + "55.6 - Remove Generated Gameplay Pass")]
        public static void RemoveGeneratedPass()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("M55.6", "Play Mode dışında çalıştır.", "Tamam");
                return;
            }

            GameObject integrationRoot = GameObject.Find(IntegrationRootName);
            Transform generated = integrationRoot != null
                ? FindChildRecursive(integrationRoot.transform, GeneratedRootName)
                : null;

            if (generated == null)
            {
                EditorUtility.DisplayDialog("M55.6", "Generated gameplay pass zaten yok.", "Tamam");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "M55.6 Remove",
                    "M55.6 tarafından üretilen proxy/anchor/water objeleri silinecek ve STAIR kaynak collider'ları geri açılacak. M55.4 map entegrasyonu korunacak.",
                    "Kaldır",
                    "İptal"))
                return;

            if (integrationRoot != null)
            {
                AtelierSurfaceSemantic[] semantics = integrationRoot.GetComponentsInChildren<AtelierSurfaceSemantic>(true);
                for (int i = 0; i < semantics.Length; i++)
                {
                    if (semantics[i] != null)
                    {
                        semantics[i].RestoreSourceColliderIfNeeded();
                        EditorUtility.SetDirty(semantics[i]);
                    }
                }
            }

            Undo.DestroyObjectImmediate(generated.gameObject);
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Painted Alive M55.6] Generated gameplay pass removed; M55.4 model preserved.");
        }


        private static void RestorePriorStairColliders(Transform integrationRoot)
        {
            if (integrationRoot == null) return;

            AtelierSurfaceSemantic[] semantics = integrationRoot.GetComponentsInChildren<AtelierSurfaceSemantic>(true);
            for (int i = 0; i < semantics.Length; i++)
            {
                AtelierSurfaceSemantic semantic = semantics[i];
                if (semantic == null || !semantic.TraversalProxyReplacedCollider) continue;
                semantic.RestoreSourceColliderIfNeeded();
                EditorUtility.SetDirty(semantic);

                Collider collider = semantic.GetComponent<Collider>();
                if (collider != null) EditorUtility.SetDirty(collider);
            }
        }

        private static SemanticStats ApplySurfaceSemantics(Transform model, int paintSurfaceLayer)
        {
            SemanticStats stats = new SemanticStats();

            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null || filter.sharedMesh == null) continue;
                string name = filter.name ?? string.Empty;

                AtelierSurfaceKind kind;
                if (Starts(name, "PLATFORM |") || Starts(name, "BRIDGE |") || Starts(name, "RAMP |") ||
                    Starts(name, "STAIR |") || Starts(name, "CANVAS_WALL |"))
                {
                    kind = AtelierSurfaceKind.PaintableTraversal;
                    filter.gameObject.layer = paintSurfaceLayer;
                    stats.Paintable++;
                }
                else if ((Starts(name, "CLIFF |") && name.IndexOf("fractured spur", StringComparison.OrdinalIgnoreCase) < 0) ||
                         Starts(name, "FRAME |") || Starts(name, "ARCHITECTURE |") || Starts(name, "COVER |") || Starts(name, "SUPPLY |"))
                {
                    kind = AtelierSurfaceKind.SolidObstacle;
                    stats.Solid++;
                }
                else if (Starts(name, "POOL |") || Starts(name, "WATERFALL |"))
                {
                    kind = AtelierSurfaceKind.WatercolorVisual;
                    stats.Watercolor++;
                }
                else
                {
                    continue;
                }

                AtelierSurfaceSemantic semantic = GetOrAddComponent<AtelierSurfaceSemantic>(filter.gameObject);
                if (!Starts(name, "STAIR |"))
                    semantic.Configure(kind, name);
                EditorUtility.SetDirty(semantic);
            }

            return stats;
        }

        private static int BuildStairTraversalProxies(Transform model, Transform proxyRoot, int paintSurfaceLayer)
        {
            MeshFilter[] stairs = model.GetComponentsInChildren<MeshFilter>(true)
                .Where(x => x != null && x.sharedMesh != null && Starts(x.name, "STAIR |"))
                .OrderBy(x => x.name)
                .ToArray();

            int created = 0;
            for (int i = 0; i < stairs.Length; i++)
            {
                MeshFilter source = stairs[i];
                if (!TryCalculateRamp(source, out RampFit fit))
                {
                    Debug.LogWarning("[M55.6] Stair proxy fit başarısız, raw collider korunuyor: " + source.name, source);
                    continue;
                }

                GameObject proxy = new GameObject("TRAVERSAL_PROXY | " + source.name);
                Undo.RegisterCreatedObjectUndo(proxy, "Create Atelier stair traversal proxy");
                proxy.transform.SetParent(proxyRoot, true);
                proxy.transform.SetPositionAndRotation(fit.Center, fit.Rotation);
                proxy.transform.localScale = Vector3.one;
                proxy.layer = paintSurfaceLayer;

                BoxCollider box = Undo.AddComponent<BoxCollider>(proxy);
                box.center = Vector3.zero;
                box.size = new Vector3(fit.Width, StairProxyThickness, fit.Length);

                AtelierSurfaceSemantic proxySemantic = Undo.AddComponent<AtelierSurfaceSemantic>(proxy);
                proxySemantic.Configure(AtelierSurfaceKind.PaintableTraversal, source.name + " [smooth proxy]");

                Collider sourceCollider = source.GetComponent<Collider>();
                if (sourceCollider != null)
                {
                    AtelierSurfaceSemantic sourceSemantic = GetOrAddComponent<AtelierSurfaceSemantic>(source.gameObject);
                    bool originalEnabled = sourceSemantic.TraversalProxyReplacedCollider
                        ? sourceSemantic.SourceColliderWasEnabled
                        : sourceCollider.enabled;
                    sourceSemantic.Configure(AtelierSurfaceKind.PaintableTraversal, source.name, true, originalEnabled);
                    sourceCollider.enabled = false;
                    EditorUtility.SetDirty(sourceCollider);
                    EditorUtility.SetDirty(sourceSemantic);
                }

                EditorUtility.SetDirty(box);
                EditorUtility.SetDirty(proxy);
                created++;
            }

            return created;
        }

        private static bool TryCalculateRamp(MeshFilter source, out RampFit fit)
        {
            fit = default;
            Mesh mesh = source.sharedMesh;
            Vector3[] vertices = mesh != null ? mesh.vertices : null;
            if (vertices == null || vertices.Length < 4) return false;

            var world = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                world[i] = source.transform.TransformPoint(vertices[i]);

            // M55.6 originally guessed the run direction from the largest local X/Z bound.
            // Several Atelier stair meshes are wider than they are long, so that can select
            // the stair WIDTH as the run axis and create a floating cross-wise box that feels
            // like an invisible wall. Pick the horizontal axis whose position correlates most
            // strongly with vertex height instead.
            Vector3 candidateA = Vector3.ProjectOnPlane(source.transform.right, Vector3.up);
            Vector3 candidateB = Vector3.ProjectOnPlane(source.transform.forward, Vector3.up);
            if (candidateA.sqrMagnitude < 0.001f) candidateA = Vector3.right;
            if (candidateB.sqrMagnitude < 0.001f) candidateB = Vector3.forward;
            candidateA.Normalize();
            candidateB.Normalize();

            float scoreA = CalculateHeightCorrelationScore(world, candidateA);
            float scoreB = CalculateHeightCorrelationScore(world, candidateB);
            Vector3 horizontalAxis = scoreA >= scoreB ? candidateA : candidateB;

            float minS = float.PositiveInfinity;
            float maxS = float.NegativeInfinity;
            for (int i = 0; i < world.Length; i++)
            {
                float sValue = Vector3.Dot(world[i], horizontalAxis);
                minS = Mathf.Min(minS, sValue);
                maxS = Mathf.Max(maxS, sValue);
            }

            float span = maxS - minS;
            if (span < 0.35f) return false;

            float edgeBand = Mathf.Max(0.10f, span * 0.10f);
            if (!TryGetEndTop(world, horizontalAxis, minS, edgeBand, true, out Vector3 endA) ||
                !TryGetEndTop(world, horizontalAxis, maxS, edgeBand, false, out Vector3 endB))
            {
                return false;
            }

            Vector3 lowEnd = endA;
            Vector3 highEnd = endB;
            if (lowEnd.y > highEnd.y)
            {
                Vector3 temp = lowEnd;
                lowEnd = highEnd;
                highEnd = temp;
            }

            Vector3 horizontalRun = Vector3.ProjectOnPlane(highEnd - lowEnd, Vector3.up);
            if (horizontalRun.sqrMagnitude < 0.04f) return false;
            horizontalRun.Normalize();

            float sourceRise = highEnd.y - lowEnd.y;
            if (sourceRise < 0.05f) return false;

            // Overlap both landings. The entry is intentionally buried below the first tread,
            // eliminating the small vertical lip CharacterController could hit like a wall.
            lowEnd -= horizontalRun * StairProxyEntryOverlap;
            lowEnd.y -= StairProxyEntryDrop;
            highEnd += horizontalRun * StairProxyExitOverlap;

            Vector3 slope = highEnd - lowEnd;
            if (slope.sqrMagnitude < 0.25f) return false;

            Vector3 slopeDirection = slope.normalized;
            Quaternion rotation = Quaternion.LookRotation(slopeDirection, Vector3.up);
            Vector3 sideAxis = rotation * Vector3.right;

            float minW = float.PositiveInfinity;
            float maxW = float.NegativeInfinity;
            for (int i = 0; i < world.Length; i++)
            {
                float w = Vector3.Dot(world[i], sideAxis);
                minW = Mathf.Min(minW, w);
                maxW = Mathf.Max(maxW, w);
            }

            float rawWidth = Mathf.Max(0.75f, maxW - minW);
            float width = Mathf.Max(0.65f, rawWidth - StairProxySideInset * 2f);
            float length = Mathf.Max(0.50f, slope.magnitude);

            // Place the top face on the calculated walking surface, not through the mesh center.
            Vector3 surfaceNormal = rotation * Vector3.up;
            Vector3 center = (lowEnd + highEnd) * 0.5f;
            center -= surfaceNormal * (StairProxyThickness * 0.5f + 0.005f);

            fit = new RampFit(center, rotation, width, length);
            return true;
        }

        private static float CalculateHeightCorrelationScore(Vector3[] world, Vector3 axis)
        {
            if (world == null || world.Length == 0) return 0f;

            float meanS = 0f;
            float meanY = 0f;
            for (int i = 0; i < world.Length; i++)
            {
                meanS += Vector3.Dot(world[i], axis);
                meanY += world[i].y;
            }
            meanS /= world.Length;
            meanY /= world.Length;

            float covariance = 0f;
            float varianceS = 0f;
            float minS = float.PositiveInfinity;
            float maxS = float.NegativeInfinity;
            for (int i = 0; i < world.Length; i++)
            {
                float sValue = Vector3.Dot(world[i], axis);
                float ds = sValue - meanS;
                covariance += ds * (world[i].y - meanY);
                varianceS += ds * ds;
                minS = Mathf.Min(minS, sValue);
                maxS = Mathf.Max(maxS, sValue);
            }

            if (varianceS < 0.0001f) return 0f;
            float regressionSlope = covariance / varianceS;
            float span = Mathf.Max(0f, maxS - minS);
            return Mathf.Abs(regressionSlope) * span;
        }

        private static bool TryGetEndTop(
            Vector3[] world,
            Vector3 axis,
            float edgeCoordinate,
            float edgeBand,
            bool minimumSide,
            out Vector3 endTop)
        {
            endTop = Vector3.zero;
            var candidates = new List<Vector3>();
            float topY = float.NegativeInfinity;

            for (int i = 0; i < world.Length; i++)
            {
                float sValue = Vector3.Dot(world[i], axis);
                bool inside = minimumSide
                    ? sValue <= edgeCoordinate + edgeBand
                    : sValue >= edgeCoordinate - edgeBand;
                if (!inside) continue;

                candidates.Add(world[i]);
                topY = Mathf.Max(topY, world[i].y);
            }

            if (candidates.Count == 0 || float.IsNegativeInfinity(topY)) return false;

            float topTolerance = Mathf.Max(0.025f, edgeBand * 0.12f);
            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].y < topY - topTolerance) continue;
                sum += candidates[i];
                count++;
            }

            if (count == 0) return false;
            endTop = sum / count;
            endTop.y = topY;
            return true;
        }

        private static Dictionary<string, Transform> BuildRouteAnchors(Transform model, Transform routeRoot)
        {
            var result = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
            Renderer[] platformRenderers = model.GetComponentsInChildren<Renderer>(true)
                .Where(x => x != null && Starts(x.name, "PLATFORM |"))
                .OrderBy(x => x.name)
                .ToArray();

            for (int i = 0; i < platformRenderers.Length; i++)
            {
                Renderer renderer = platformRenderers[i];
                string region = renderer.name.Substring("PLATFORM |".Length).Trim();
                Vector3 position = new Vector3(renderer.bounds.center.x, renderer.bounds.max.y + RouteAnchorLift, renderer.bounds.center.z);
                Transform anchor = CreateOrUpdateAnchor(routeRoot, "Route_" + Sanitize(region), position, Quaternion.identity);
                result[region] = anchor;
            }

            return result;
        }

        private static Transform ResolveStartAnchor(Transform integrationRoot, Dictionary<string, Transform> routeAnchors)
        {
            Transform existingSpawn = FindChildRecursive(integrationRoot, M55_4FigureSpawnName);
            Vector3 position;
            Quaternion rotation;

            if (existingSpawn != null)
            {
                position = existingSpawn.position;
                rotation = existingSpawn.rotation;
            }
            else if (routeAnchors.TryGetValue("START", out Transform routeStart))
            {
                position = routeStart.position + Vector3.up * 0.03f;
                rotation = routeStart.rotation;
            }
            else
            {
                throw new InvalidOperationException("Atelier START platform/spawn bulunamadı.");
            }

            Transform generated = FindChildRecursive(integrationRoot, GeneratedRootName);
            Transform routeRoot = generated != null ? generated.Find(RouteRootName) : null;
            if (routeRoot == null) throw new InvalidOperationException("M55.6 RouteAnchors root bulunamadı.");

            return CreateOrUpdateAnchor(routeRoot, "Route_START", position, rotation);
        }

        private static FinalFrameInfo ResolveFinalFrame(Transform model, Vector3 startPosition)
        {
            Renderer lintel = FindRendererByExactName(model, FinalFrameLintel);
            Renderer uprightA = FindRendererByExactName(model, FinalFrameUprightA);
            Renderer uprightB = FindRendererByExactName(model, FinalFrameUprightB);

            if (lintel == null || uprightA == null || uprightB == null)
                throw new InvalidOperationException("UPPER_GARDEN final frame geometry bulunamadı.");

            Renderer[] renderers = { lintel, uprightA, uprightB };
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            Vector3 right = uprightB.bounds.center - uprightA.bounds.center;
            right = Vector3.ProjectOnPlane(right, Vector3.up);
            if (right.sqrMagnitude < 0.01f) right = Vector3.right;
            right.Normalize();

            Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;
            Vector3 travel = Vector3.ProjectOnPlane(bounds.center - startPosition, Vector3.up).normalized;
            if (travel.sqrMagnitude > 0.001f && Vector3.Dot(forward, travel) < 0f)
                forward = -forward;

            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
            float width = Vector3.Distance(
                new Vector3(uprightA.bounds.center.x, 0f, uprightA.bounds.center.z),
                new Vector3(uprightB.bounds.center.x, 0f, uprightB.bounds.center.z));
            width = Mathf.Max(2.2f, width);
            float height = Mathf.Max(2.4f, bounds.size.y * 0.88f);

            return new FinalFrameInfo(bounds.center, rotation, width, height, renderers);
        }

        private static MatchBindingStats BindExistingMatchContracts(Transform start, Transform finalAnchor, FinalFrameInfo finalFrame)
        {
            MatchBindingStats stats = new MatchBindingStats();

            Transform m54Start = FindSceneTransformByName("M54_MatchStart");
            Transform m54Exit = FindSceneTransformByName("M54_FrameExit");
            if (m54Start != null && m54Exit != null)
            {
                m54Start.SetPositionAndRotation(start.position, start.rotation);
                m54Exit.SetPositionAndRotation(finalFrame.Center, finalFrame.Rotation);
                stats.M54Anchors = true;
                EditorUtility.SetDirty(m54Start);
                EditorUtility.SetDirty(m54Exit);
            }

            PrototypeFrameExitGate gate = FindSceneObject<PrototypeFrameExitGate>();
            if (gate != null)
            {
                gate.transform.SetPositionAndRotation(finalFrame.Center, finalFrame.Rotation);

                BoxCollider trigger = gate.GetComponent<BoxCollider>();
                if (trigger == null) trigger = Undo.AddComponent<BoxCollider>(gate.gameObject);
                trigger.isTrigger = true;
                trigger.center = Vector3.zero;
                trigger.size = new Vector3(finalFrame.Width * 0.82f, finalFrame.Height, ExitTriggerDepth);

                foreach (Renderer childRenderer in gate.GetComponentsInChildren<Renderer>(true))
                {
                    if (childRenderer != null &&
                        (childRenderer.name == "Frame_Left" || childRenderer.name == "Frame_Right" || childRenderer.name == "Frame_Top"))
                        childRenderer.enabled = false;
                }

                StainFrameExitConfig frameConfig = AssetDatabase.LoadAssetAtPath<StainFrameExitConfig>(FrameExitConfigPath);
                if (frameConfig != null)
                    gate.Configure(frameConfig, finalFrame.Renderers);

                EditorUtility.SetDirty(trigger);
                EditorUtility.SetDirty(gate);
                stats.M36Gate = true;
            }

            Transform journeyStart = FindSceneTransformByName("Journey_Start");
            if (journeyStart != null)
            {
                journeyStart.SetPositionAndRotation(start.position, start.rotation);
                EditorUtility.SetDirty(journeyStart);
            }

            PrototypeJourneyScoreTracker tracker = FindSceneObject<PrototypeJourneyScoreTracker>();
            FigureClarityState clarity = FindSceneObject<FigureClarityState>();
            PrototypeJourneyScoreConfig scoreConfig = AssetDatabase.LoadAssetAtPath<PrototypeJourneyScoreConfig>(JourneyScoreConfigPath);
            Transform trackerStart = journeyStart != null ? journeyStart : start;
            Transform trackerFinish = gate != null ? gate.transform : finalAnchor;

            if (tracker != null && clarity != null && scoreConfig != null)
            {
                tracker.Configure(clarity, trackerStart, trackerFinish, scoreConfig);
                EditorUtility.SetDirty(tracker);
                stats.M37Tracker = true;
            }

            return stats;
        }

        private static int BuildEnvironmentalWatercolorZones(Transform model, Transform watercolorRoot)
        {
            WatercolorFlowConfig config = AssetDatabase.LoadAssetAtPath<WatercolorFlowConfig>(WatercolorConfigPath);
            if (config == null)
            {
                Debug.LogWarning("[M55.6] Existing M13 WatercolorFlowConfig bulunamadı; environmental watercolor oluşturulmadı.");
                return 0;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(WatercolorMaterialPath);
            PrototypeCoreMatchController matchController = FindSceneObject<PrototypeCoreMatchController>();
            int created = 0;

            for (int i = 0; i < 6; i++)
            {
                Renderer pool = FindRendererByExactName(model, "POOL | turquoise sheet " + i);
                Renderer waterfall = FindRendererByExactName(model, "WATERFALL | basin " + i);
                if (pool == null || waterfall == null)
                {
                    Debug.LogWarning($"[M55.6] Basin {i}: pool veya waterfall visual bulunamadı.");
                    continue;
                }

                Vector3 sourcePosition = pool.bounds.center + Vector3.up * 0.04f;
                Vector3 direction = Vector3.ProjectOnPlane(pool.bounds.center - waterfall.bounds.center, Vector3.up);
                if (direction.sqrMagnitude < 0.01f)
                    direction = Vector3.ProjectOnPlane(pool.transform.forward, Vector3.up);
                if (direction.sqrMagnitude < 0.01f)
                    direction = Vector3.forward;
                direction.Normalize();

                GameObject zoneObject = new GameObject("WATERCOLOR_ZONE | Basin " + i);
                Undo.RegisterCreatedObjectUndo(zoneObject, "Create environmental watercolor zone");
                zoneObject.transform.SetParent(watercolorRoot, true);
                zoneObject.transform.SetPositionAndRotation(sourcePosition, Quaternion.LookRotation(direction, Vector3.up));

                AtelierWatercolorFlowZone zone = Undo.AddComponent<AtelierWatercolorFlowZone>(zoneObject);

                GameObject template = new GameObject("TEMPLATE_M13_WatercolorFlow");
                Undo.RegisterCreatedObjectUndo(template, "Create M13 watercolor flow template");
                template.transform.SetParent(zoneObject.transform, false);
                template.transform.localPosition = Vector3.zero;
                template.transform.localRotation = Quaternion.identity;
                template.transform.localScale = Vector3.one;

                MeshFilter filter = Undo.AddComponent<MeshFilter>(template);
                MeshRenderer renderer = Undo.AddComponent<MeshRenderer>(template);
                MeshCollider collider = Undo.AddComponent<MeshCollider>(template);
                WatercolorFlowSurface flow = Undo.AddComponent<WatercolorFlowSurface>(template);

                // M55.6.1: WatercolorFlowSurface keeps its component dependencies in
                // serialized fields. The existing M13 prefab has those references
                // authored explicitly, but our scene-authored template did not.
                // UnityEngine.Object's special null semantics can leave those fields
                // looking unassigned to Awake even though GetComponent would normally
                // find the required components. Bind the exact scene components now so
                // every inactive template clone inherits valid references before Awake.
                SerializedObject flowSerialized = new SerializedObject(flow);
                flowSerialized.FindProperty("config").objectReferenceValue = config;
                flowSerialized.FindProperty("meshFilter").objectReferenceValue = filter;
                flowSerialized.FindProperty("meshRenderer").objectReferenceValue = renderer;
                flowSerialized.FindProperty("meshCollider").objectReferenceValue = collider;
                flowSerialized.ApplyModifiedPropertiesWithoutUndo();

                // Keep the public M13 configuration contract as well.
                flow.Configure(config);

                if (material != null)
                    renderer.sharedMaterial = material;

                collider.convex = false;
                template.SetActive(false);
                zone.Configure("Basin_" + i, template, matchController);

                AtelierSurfaceSemantic poolSemantic = GetOrAddComponent<AtelierSurfaceSemantic>(pool.gameObject);
                poolSemantic.Configure(AtelierSurfaceKind.WatercolorVisual, pool.name);
                AtelierSurfaceSemantic waterfallSemantic = GetOrAddComponent<AtelierSurfaceSemantic>(waterfall.gameObject);
                waterfallSemantic.Configure(AtelierSurfaceKind.WatercolorVisual, waterfall.name);

                EditorUtility.SetDirty(filter);
                EditorUtility.SetDirty(renderer);
                EditorUtility.SetDirty(collider);
                EditorUtility.SetDirty(flow);
                EditorUtility.SetDirty(zone);
                created++;
            }

            return created;
        }

        private static Renderer FindRendererByExactName(Transform root, string exactName)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && string.Equals(renderers[i].name, exactName, StringComparison.OrdinalIgnoreCase))
                    return renderers[i];
            }
            return null;
        }

        private static Transform CreateOrUpdateAnchor(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            Transform anchor = parent.Find(name);
            if (anchor == null)
            {
                GameObject go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
                go.transform.SetParent(parent, true);
                anchor = go.transform;
            }
            anchor.SetPositionAndRotation(position, rotation);
            anchor.localScale = Vector3.one;
            EditorUtility.SetDirty(anchor.gameObject);
            return anchor;
        }

        private static Transform GetOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;
            GameObject go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null) return null;
            if (string.Equals(root.name, name, StringComparison.OrdinalIgnoreCase)) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static Transform FindSceneTransformByName(string name)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = FindChildRecursive(root.transform, name);
                if (found != null) return found;
            }
            return null;
        }

        private static T FindSceneObject<T>() where T : Component
        {
            T[] items = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < items.Length; i++)
            {
                T item = items[i];
                if (item != null && item.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(item))
                    return item;
            }
            return null;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component == null) component = Undo.AddComponent<T>(target);
            return component;
        }

        private static bool Starts(string value, string prefix)
        {
            return (value ?? string.Empty).StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "UNNAMED";
            char[] invalid = { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };
            string result = value;
            for (int i = 0; i < invalid.Length; i++) result = result.Replace(invalid[i], '_');
            return result.Trim().Replace(' ', '_');
        }

        private static void AddCheck(List<string> lines, ref bool pass, bool condition, string label, string detail)
        {
            if (!condition) pass = false;
            lines.Add((condition ? "PASS" : "FAIL") + " | " + label + " | " + detail);
        }

        private readonly struct RampFit
        {
            public RampFit(Vector3 center, Quaternion rotation, float width, float length)
            {
                Center = center;
                Rotation = rotation;
                Width = width;
                Length = length;
            }
            public Vector3 Center { get; }
            public Quaternion Rotation { get; }
            public float Width { get; }
            public float Length { get; }
        }

        private readonly struct FinalFrameInfo
        {
            public FinalFrameInfo(Vector3 center, Quaternion rotation, float width, float height, Renderer[] renderers)
            {
                Center = center;
                Rotation = rotation;
                Width = width;
                Height = height;
                Renderers = renderers;
            }
            public Vector3 Center { get; }
            public Quaternion Rotation { get; }
            public float Width { get; }
            public float Height { get; }
            public Renderer[] Renderers { get; }
        }

        private struct SemanticStats
        {
            public int Paintable;
            public int Solid;
            public int Watercolor;
        }

        private struct MatchBindingStats
        {
            public bool M54Anchors;
            public bool M36Gate;
            public bool M37Tracker;
        }
    }
}
#endif
