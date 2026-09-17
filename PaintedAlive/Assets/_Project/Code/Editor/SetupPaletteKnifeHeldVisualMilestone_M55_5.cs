#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PaintedAlive.Figures.Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.EditorTools
{
    /// <summary>
    /// M55.5 integrates the production Palette Knife as a presentation-only visual
    /// attached to the production Humanoid Figure's right hand.
    ///
    /// Deliberately untouched:
    /// - PaletteKnifeController targeting, reach and cutting logic
    /// - PaletteKnifeImpactFeedback
    /// - Ink / Oil / Counter-Composition bridges
    /// - FigureMotor / CharacterController / Animator controller
    /// </summary>
    public static class SetupPaletteKnifeHeldVisualMilestone_M55_5
    {
        private const string MenuRoot = "Tools/Painted Alive/Milestones/";

        private const string ModelPath =
            "Assets/_Project/Art/Models/Figures/Tools/PaletteKnife/Source/PA_PaletteKnife_Source.fbx";

        private const string TextureRoot =
            "Assets/_Project/Art/Textures/Figures/Tools/PaletteKnife";
        private const string BaseColorPath = TextureRoot + "/PA_PaletteKnife_BaseColor_2K.png";
        private const string NormalPath = TextureRoot + "/PA_PaletteKnife_Normal_2K.png";
        private const string MetallicPath = TextureRoot + "/PA_PaletteKnife_Metallic_2K.png";
        private const string RoughnessPath = TextureRoot + "/PA_PaletteKnife_Roughness_2K.png";
        private const string MetallicSmoothnessPath = TextureRoot + "/PA_PaletteKnife_MetallicSmoothness_2K.png";

        private const string MaterialFolder =
            "Assets/_Project/Art/Materials/Figures/Tools/PaletteKnife";
        private const string MaterialPath =
            MaterialFolder + "/MAT_PaletteKnife_Production.mat";

        private const string PrefabFolder =
            "Assets/_Project/Art/Models/Figures/Tools/PaletteKnife/Prefabs";
        private const string PrefabPath =
            PrefabFolder + "/PF_PaletteKnife_Visual.prefab";

        private const string SocketName = "ToolSocket_PaletteKnife_R";
        private const string HeldVisualName = "Held_PaletteKnife";

        // Source inspection: authored blade+handle length is approximately 0.25 m.
        private const float ExpectedMinimumLength = 0.18f;
        private const float ExpectedMaximumLength = 0.36f;
        private const float FallbackTargetLength = 0.25f;
        private const float GripFractionFromHandleEnd = 0.28f;

        [MenuItem(MenuRoot + "55.5 - Setup Palette Knife Held Visual")]
        public static void Setup()
        {
            try
            {
                RequireEditMode();
                ValidateSourceAssets();
                EnsureFolders();

                ConfigureModelImporter();
                ConfigureTextureImporter(BaseColorPath, false, true);
                ConfigureTextureImporter(NormalPath, true, false);
                ConfigureTextureImporter(MetallicPath, false, false);
                ConfigureTextureImporter(RoughnessPath, false, false);
                ConfigureTextureImporter(MetallicSmoothnessPath, false, false);

                Material material = CreateOrUpdateMaterial();
                GameObject visualPrefab = CreateOrUpdateVisualPrefab(material);

                FigureToolLoadoutController loadout =
                    FindExactlyOne<FigureToolLoadoutController>("FigureToolLoadoutController");
                Animator animator = FindProductionHumanoidAnimator(loadout.transform);
                Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (rightHand == null)
                {
                    throw new InvalidOperationException(
                        "Humanoid Animator geçerli fakat RightHand kemiği çözülemedi.");
                }

                Transform socket = rightHand.Find(SocketName);
                bool preserveExistingSocketPose = socket != null;

                if (socket == null)
                {
                    GameObject socketObject = new GameObject(SocketName);
                    Undo.RegisterCreatedObjectUndo(socketObject, "Create Palette Knife hand socket");
                    socket = socketObject.transform;
                    Undo.SetTransformParent(socket, rightHand, "Parent Palette Knife hand socket");
                    socket.localPosition = Vector3.zero;
                    socket.localRotation = Quaternion.identity;
                    socket.localScale = Vector3.one;
                }

                Transform oldHeld = socket.Find(HeldVisualName);
                if (oldHeld != null)
                {
                    Undo.DestroyObjectImmediate(oldHeld.gameObject);
                }

                GameObject held = PrefabUtility.InstantiatePrefab(visualPrefab) as GameObject;
                if (held == null)
                {
                    throw new InvalidOperationException(
                        "Palet Bıçağı görsel prefabı sahneye instantiate edilemedi.");
                }

                Undo.RegisterCreatedObjectUndo(held, "Create held Palette Knife visual");
                held.name = HeldVisualName;
                Undo.SetTransformParent(held.transform, socket, "Parent held Palette Knife visual");
                held.transform.localPosition = Vector3.zero;
                held.transform.localRotation = Quaternion.identity;
                held.transform.localScale = Vector3.one;
                held.SetActive(true);

                // First install receives an automatic hand fit. Rerunning Setup intentionally
                // preserves any manual socket tuning the user has already accepted.
                if (!preserveExistingSocketPose)
                {
                    AutoFitSocketToHand(socket, held, animator);
                }

                BindLoadoutPaletteKnifeVisual(loadout, socket.gameObject);

                EditorUtility.SetDirty(loadout);
                EditorUtility.SetDirty(socket.gameObject);
                EditorSceneManager.MarkSceneDirty(loadout.gameObject.scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                string message =
                    "M55.5 Palet Bıçağı görsel entegrasyonu tamamlandı.\n\n" +
                    "• Sağ Humanoid el kemiğine bağlandı.\n" +
                    "• PaletteKnifeController davranışı değiştirilmedi.\n" +
                    "• Collider / Rigidbody / ayrı Animator eklenmedi.\n" +
                    "• FigureToolLoadoutController.paletteKnifeVisual bağlandı.\n" +
                    "• 1 seçiliyken görünür, diğer ana aletlerde gizlenir.\n" +
                    "• M55.3 tool theft durumunda aynı loadout referansı üzerinden elden kaybolur.\n\n" +
                    "Şimdi 55.5 - Diagnose Palette Knife Held Visual çalıştır.";

                Debug.Log("[Painted Alive M55.5] " + message.Replace("\n", " | "));
                EditorUtility.DisplayDialog("Painted Alive M55.5", message, "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog(
                    "Painted Alive M55.5 - Setup Failed",
                    ex.Message +
                    "\n\nMevcut Palette Knife gameplay kodu bu Setup tarafından değiştirilmedi.",
                    "OK");
            }
        }

        [MenuItem(MenuRoot + "55.5 - Refit Palette Knife Grip Pose")]
        public static void RefitGripPose()
        {
            try
            {
                RequireEditMode();
                FigureToolLoadoutController loadout =
                    FindExactlyOne<FigureToolLoadoutController>("FigureToolLoadoutController");
                Animator animator = FindProductionHumanoidAnimator(loadout.transform);
                Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                Transform socket = rightHand != null ? rightHand.Find(SocketName) : null;
                Transform held = socket != null ? socket.Find(HeldVisualName) : null;

                if (socket == null || held == null)
                {
                    throw new InvalidOperationException(
                        "Önce 55.5 - Setup Palette Knife Held Visual çalıştırılmalı.");
                }

                Undo.RecordObject(socket, "Refit Palette Knife grip pose");
                AutoFitSocketToHand(socket, held.gameObject, animator);
                EditorUtility.SetDirty(socket);
                EditorSceneManager.MarkSceneDirty(loadout.gameObject.scene);

                Debug.Log(
                    "[Painted Alive M55.5] Grip pose otomatik yeniden hizalandı. " +
                    "Gerekirse yalnız ToolSocket_PaletteKnife_R Local Position/Rotation değerlerini ince ayarla.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        [MenuItem(MenuRoot + "55.5 - Diagnose Palette Knife Held Visual")]
        public static void Diagnose()
        {
            List<string> lines = new List<string>();
            bool pass = true;

            Action<bool, string, string> add = (ok, label, detail) =>
            {
                pass &= ok;
                lines.Add((ok ? "PASS" : "FAIL") + " | " + label + " | " + detail);
            };

            add(AssetExists(ModelPath), "SourceModel", ModelPath);
            add(AssetExists(BaseColorPath), "BaseColor", BaseColorPath);
            add(AssetExists(NormalPath), "Normal", NormalPath);
            add(AssetExists(MetallicPath), "Metallic", MetallicPath);
            add(AssetExists(RoughnessPath), "Roughness", RoughnessPath);
            add(AssetExists(MetallicSmoothnessPath), "MetallicSmoothness", MetallicSmoothnessPath);

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            add(material != null, "ProductionMaterial",
                material != null && material.shader != null ? material.shader.name : "missing");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            add(prefab != null, "VisualPrefab", PrefabPath);
            if (prefab != null)
            {
                add(prefab.GetComponentsInChildren<Collider>(true).Length == 0,
                    "VisualColliders", prefab.GetComponentsInChildren<Collider>(true).Length.ToString());
                add(prefab.GetComponentsInChildren<Rigidbody>(true).Length == 0,
                    "VisualRigidbodies", prefab.GetComponentsInChildren<Rigidbody>(true).Length.ToString());
                add(prefab.GetComponentsInChildren<Animator>(true).Length == 0,
                    "VisualAnimators", prefab.GetComponentsInChildren<Animator>(true).Length.ToString());
            }

            FigureToolLoadoutController[] loadouts =
                UnityEngine.Object.FindObjectsByType<FigureToolLoadoutController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            add(loadouts.Length == 1, "FigureToolLoadoutController", "count=" + loadouts.Length);

            if (loadouts.Length == 1)
            {
                FigureToolLoadoutController loadout = loadouts[0];
                Animator animator = TryFindProductionHumanoidAnimator(loadout.transform);
                add(animator != null, "HumanoidAnimator",
                    animator != null ? GetHierarchyPath(animator.transform) : "missing");

                Transform rightHand = animator != null
                    ? animator.GetBoneTransform(HumanBodyBones.RightHand)
                    : null;
                add(rightHand != null, "RightHandBone",
                    rightHand != null ? GetHierarchyPath(rightHand) : "missing");

                Transform socket = rightHand != null ? rightHand.Find(SocketName) : null;
                add(socket != null, "PaletteKnifeSocket",
                    socket != null ? GetHierarchyPath(socket) : "missing");

                Transform held = socket != null ? socket.Find(HeldVisualName) : null;
                add(held != null, "HeldPaletteKnife",
                    held != null ? GetHierarchyPath(held) : "missing");

                GameObject boundVisual = ReadLoadoutPaletteKnifeVisual(loadout);
                add(boundVisual != null && socket != null && boundVisual == socket.gameObject,
                    "LoadoutVisualBinding",
                    boundVisual != null ? GetHierarchyPath(boundVisual.transform) : "null");

                if (held != null)
                {
                    Bounds bounds = CalculateWorldRendererBounds(held.gameObject, out bool hasBounds);
                    float longest = hasBounds
                        ? Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z))
                        : 0f;
                    add(hasBounds, "HeldVisualBounds",
                        hasBounds ? bounds.size.ToString("F3") : "none");
                    add(hasBounds && longest >= ExpectedMinimumLength && longest <= ExpectedMaximumLength,
                        "PaletteKnifeWorldLength",
                        hasBounds ? longest.ToString("F3") + " m" : "unavailable");
                    add(held.GetComponentsInChildren<Collider>(true).Length == 0,
                        "SceneVisualColliders", held.GetComponentsInChildren<Collider>(true).Length.ToString());
                    add(held.GetComponentsInChildren<Rigidbody>(true).Length == 0,
                        "SceneVisualRigidbodies", held.GetComponentsInChildren<Rigidbody>(true).Length.ToString());
                }

                PaletteKnifeController[] knifeControllers =
                    UnityEngine.Object.FindObjectsByType<PaletteKnifeController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                add(knifeControllers.Length >= 1, "ExistingPaletteKnifeController",
                    "count=" + knifeControllers.Length);

                if (socket != null)
                {
                    lines.Add(
                        "INFO | SocketPose | LocalPosition=" + socket.localPosition.ToString("F4") +
                        " LocalEuler=" + socket.localEulerAngles.ToString("F2") +
                        " LocalScale=" + socket.localScale.ToString("F3"));
                }
            }

            lines.Insert(0, "Result=" + (pass ? "PASS" : "FAIL"));
            string report = string.Join("\n", lines);
            Debug.Log("[Painted Alive M55.5 Diagnose]\n" + report);
            EditorUtility.DisplayDialog("Painted Alive M55.5 Diagnose", report, "OK");
        }

        [MenuItem(MenuRoot + "55.5 - Select Palette Knife Socket")]
        public static void SelectSocket()
        {
            FigureToolLoadoutController[] loadouts =
                UnityEngine.Object.FindObjectsByType<FigureToolLoadoutController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            if (loadouts.Length != 1)
            {
                Debug.LogWarning("[Painted Alive M55.5] Tek FigureToolLoadoutController bulunamadı.");
                return;
            }

            Animator animator = TryFindProductionHumanoidAnimator(loadouts[0].transform);
            Transform hand = animator != null
                ? animator.GetBoneTransform(HumanBodyBones.RightHand)
                : null;
            Transform socket = hand != null ? hand.Find(SocketName) : null;
            if (socket == null)
            {
                Debug.LogWarning("[Painted Alive M55.5] Palette Knife socket bulunamadı.");
                return;
            }

            Selection.activeGameObject = socket.gameObject;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }

        private static void RequireEditMode()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "M55.5 Setup Play Mode dışında çalıştırılmalıdır.");
            }
        }

        private static void ValidateSourceAssets()
        {
            string[] required =
            {
                ModelPath,
                BaseColorPath,
                NormalPath,
                MetallicPath,
                RoughnessPath,
                MetallicSmoothnessPath
            };

            string[] missing = required.Where(path => !AssetExists(path)).ToArray();
            if (missing.Length > 0)
            {
                throw new FileNotFoundException(
                    "M55.5 gerekli kaynak assetleri bulamadı:\n" +
                    string.Join("\n", missing));
            }
        }

        private static bool AssetExists(string path)
        {
            return AssetDatabase.LoadMainAssetAtPath(path) != null || File.Exists(ToAbsolutePath(path));
        }

        private static void EnsureFolders()
        {
            EnsureAssetFolder(MaterialFolder);
            EnsureAssetFolder(PrefabFolder);
        }

        private static void EnsureAssetFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static void ConfigureModelImporter()
        {
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);
            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Palette Knife FBX ModelImporter bulunamadı.");
            }

            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.optimizeGameObjects = false;
            importer.preserveHierarchy = true;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        private static void ConfigureTextureImporter(
            string path,
            bool normalMap,
            bool sRgb)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("TextureImporter bulunamadı: " + path);
            }

            importer.textureType = normalMap
                ? TextureImporterType.NormalMap
                : TextureImporterType.Default;
            importer.sRGBTexture = normalMap ? false : sRgb;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.crunchedCompression = false;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        private static Material CreateOrUpdateMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Universal Render Pipeline/Lit shader bulunamadı.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "MAT_PaletteKnife_Production"
                };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorPath);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
            Texture2D metallicSmoothness =
                AssetDatabase.LoadAssetAtPath<Texture2D>(MetallicSmoothnessPath);

            material.SetTexture("_BaseMap", baseColor);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap", metallicSmoothness);
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");

            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateOrUpdateVisualPrefab(Material material)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (source == null)
            {
                throw new InvalidOperationException("Palette Knife FBX GameObject olarak yüklenemedi.");
            }

            GameObject root = new GameObject("PF_PaletteKnife_Visual");
            try
            {
                GameObject model = PrefabUtility.InstantiatePrefab(source) as GameObject;
                if (model == null)
                {
                    throw new InvalidOperationException("Palette Knife FBX instance oluşturulamadı.");
                }

                model.name = "Model";
                model.transform.SetParent(root.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;

                foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
                foreach (Rigidbody body in model.GetComponentsInChildren<Rigidbody>(true))
                {
                    UnityEngine.Object.DestroyImmediate(body);
                }
                foreach (Animator animator in model.GetComponentsInChildren<Animator>(true))
                {
                    UnityEngine.Object.DestroyImmediate(animator);
                }

                Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    throw new InvalidOperationException("Palette Knife FBX içinde Renderer bulunamadı.");
                }

                foreach (Renderer renderer in renderers)
                {
                    Material[] slots = renderer.sharedMaterials;
                    if (slots == null || slots.Length == 0)
                    {
                        renderer.sharedMaterial = material;
                        continue;
                    }

                    for (int i = 0; i < slots.Length; i++)
                    {
                        slots[i] = material;
                    }
                    renderer.sharedMaterials = slots;
                }

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (saved == null)
                {
                    throw new InvalidOperationException("Palette Knife visual prefab kaydedilemedi.");
                }
                return saved;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Animator FindProductionHumanoidAnimator(Transform loadoutTransform)
        {
            Animator animator = TryFindProductionHumanoidAnimator(loadoutTransform);
            if (animator == null)
            {
                throw new InvalidOperationException(
                    "Figure hierarchy içinde geçerli production Humanoid Animator bulunamadı.");
            }
            return animator;
        }

        private static Animator TryFindProductionHumanoidAnimator(Transform loadoutTransform)
        {
            Transform root = loadoutTransform != null ? loadoutTransform.root : null;
            Animator[] local = root != null
                ? root.GetComponentsInChildren<Animator>(true)
                : Array.Empty<Animator>();

            Animator localHumanoid = local.FirstOrDefault(IsValidHumanoidAnimator);
            if (localHumanoid != null)
            {
                return localHumanoid;
            }

            return UnityEngine.Object.FindObjectsByType<Animator>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(IsValidHumanoidAnimator);
        }

        private static bool IsValidHumanoidAnimator(Animator animator)
        {
            return animator != null &&
                   animator.avatar != null &&
                   animator.avatar.isValid &&
                   animator.avatar.isHuman &&
                   animator.GetBoneTransform(HumanBodyBones.RightHand) != null;
        }

        private static void AutoFitSocketToHand(
            Transform socket,
            GameObject heldVisual,
            Animator animator)
        {
            Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform middle = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
            Transform index = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
            Transform little = animator.GetBoneTransform(HumanBodyBones.RightLittleProximal);
            if (hand == null)
            {
                throw new InvalidOperationException("RightHand kemiği bulunamadı.");
            }

            Transform originalParent = socket.parent;

            // Put our own added socket at world identity temporarily. We never reparent or
            // modify the production bone itself.
            socket.SetParent(null, true);
            socket.position = Vector3.zero;
            socket.rotation = Quaternion.identity;
            socket.localScale = Vector3.one;
            heldVisual.transform.localPosition = Vector3.zero;
            heldVisual.transform.localRotation = Quaternion.identity;
            heldVisual.transform.localScale = Vector3.one;

            Bounds modelBounds = CalculateWorldRendererBounds(heldVisual, out bool hasBounds);
            if (!hasBounds)
            {
                socket.SetParent(originalParent, true);
                throw new InvalidOperationException("Palette Knife renderer bounds hesaplanamadı.");
            }

            int longIndex = LongestAxisIndex(modelBounds.size);
            int wideIndex = SecondLongestAxisIndex(modelBounds.size, longIndex);
            Vector3 longAxis = Axis(longIndex);
            Vector3 wideAxis = Axis(wideIndex);

            // Source origin is at the handle end. Choose the sign pointing from origin into
            // the main mesh volume so the opposite end is the blade tip.
            if (Vector3.Dot(modelBounds.center, longAxis) < 0f)
            {
                longAxis = -longAxis;
            }

            float currentLength = Component(modelBounds.size, longIndex);
            if (currentLength < ExpectedMinimumLength || currentLength > ExpectedMaximumLength)
            {
                float uniformScale = FallbackTargetLength / Mathf.Max(0.0001f, currentLength);
                heldVisual.transform.localScale = Vector3.one * uniformScale;
                modelBounds = CalculateWorldRendererBounds(heldVisual, out hasBounds);
                if (!hasBounds)
                {
                    socket.SetParent(originalParent, true);
                    throw new InvalidOperationException("Palette Knife scaled bounds hesaplanamadı.");
                }
                currentLength = Component(modelBounds.size, longIndex);
            }

            Vector3 fingerDirection = ResolveFingerDirection(hand, middle, index, little);
            Vector3 handWidth = ResolveHandWidth(hand, index, little, fingerDirection);

            Vector3 sourceUp = Vector3.Cross(longAxis, wideAxis).normalized;
            if (sourceUp.sqrMagnitude < 0.5f)
            {
                sourceUp = Vector3.up;
            }

            Vector3 targetUp = Vector3.Cross(fingerDirection, handWidth).normalized;
            if (targetUp.sqrMagnitude < 0.5f)
            {
                targetUp = Vector3.ProjectOnPlane(hand.up, fingerDirection).normalized;
            }
            if (targetUp.sqrMagnitude < 0.5f)
            {
                targetUp = Vector3.up;
            }

            Quaternion sourceBasis = Quaternion.LookRotation(longAxis, sourceUp);
            Quaternion targetBasis = Quaternion.LookRotation(fingerDirection, targetUp);
            socket.rotation = targetBasis * Quaternion.Inverse(sourceBasis);

            Vector3 center = modelBounds.center;
            float centerProjection = Vector3.Dot(center, longAxis);
            float halfLong = currentLength * 0.5f;
            float minProjection = centerProjection - halfLong;
            float gripProjection = minProjection + currentLength * GripFractionFromHandleEnd;
            Vector3 gripPoint = center + longAxis * (gripProjection - centerProjection);

            float palmLength = ResolvePalmLength(hand, middle, index);
            float palmOffset = Mathf.Clamp(palmLength * 0.45f, 0.025f, 0.055f);
            Vector3 targetGrip = hand.position + fingerDirection * palmOffset;

            socket.position = targetGrip - socket.rotation * gripPoint;
            socket.SetParent(originalParent, true);
        }

        private static int LongestAxisIndex(Vector3 size)
        {
            if (size.y >= size.x && size.y >= size.z) return 1;
            if (size.z >= size.x && size.z >= size.y) return 2;
            return 0;
        }

        private static int SecondLongestAxisIndex(Vector3 size, int longest)
        {
            List<int> indices = new List<int> { 0, 1, 2 };
            indices.Remove(longest);
            return Component(size, indices[0]) >= Component(size, indices[1])
                ? indices[0]
                : indices[1];
        }

        private static float Component(Vector3 value, int index)
        {
            return index == 0 ? value.x : index == 1 ? value.y : value.z;
        }

        private static Vector3 Axis(int index)
        {
            return index == 0 ? Vector3.right : index == 1 ? Vector3.up : Vector3.forward;
        }

        private static float ResolvePalmLength(
            Transform hand,
            Transform middle,
            Transform index)
        {
            if (middle != null)
            {
                return Vector3.Distance(hand.position, middle.position);
            }
            if (index != null)
            {
                return Vector3.Distance(hand.position, index.position);
            }
            return 0.08f;
        }

        private static Vector3 ResolveFingerDirection(
            Transform hand,
            Transform middle,
            Transform index,
            Transform little)
        {
            Vector3 target = Vector3.zero;
            int count = 0;
            if (middle != null) { target += middle.position; count++; }
            if (index != null) { target += index.position; count++; }
            if (little != null) { target += little.position; count++; }

            if (count > 0)
            {
                Vector3 direction = target / count - hand.position;
                if (direction.sqrMagnitude > 0.000001f)
                {
                    return direction.normalized;
                }
            }

            return hand.right.normalized;
        }

        private static Vector3 ResolveHandWidth(
            Transform hand,
            Transform index,
            Transform little,
            Vector3 fingerDirection)
        {
            if (index != null && little != null)
            {
                Vector3 width = Vector3.ProjectOnPlane(
                    index.position - little.position,
                    fingerDirection);
                if (width.sqrMagnitude > 0.000001f)
                {
                    return width.normalized;
                }
            }

            Vector3 fallback = Vector3.ProjectOnPlane(hand.forward, fingerDirection);
            if (fallback.sqrMagnitude < 0.000001f)
            {
                fallback = Vector3.ProjectOnPlane(hand.up, fingerDirection);
            }
            if (fallback.sqrMagnitude < 0.000001f)
            {
                fallback = Vector3.right;
            }
            return fallback.normalized;
        }

        private static Bounds CalculateWorldRendererBounds(
            GameObject root,
            out bool hasBounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            hasBounds = false;
            Bounds bounds = new Bounds(root.transform.position, Vector3.zero);

            foreach (Renderer renderer in renderers)
            {
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return bounds;
        }

        private static void BindLoadoutPaletteKnifeVisual(
            FigureToolLoadoutController loadout,
            GameObject socketObject)
        {
            SerializedObject serialized = new SerializedObject(loadout);
            SerializedProperty property = serialized.FindProperty("paletteKnifeVisual");
            if (property == null)
            {
                throw new InvalidOperationException(
                    "FigureToolLoadoutController içinde paletteKnifeVisual serialized alanı bulunamadı.");
            }

            property.objectReferenceValue = socketObject;
            serialized.ApplyModifiedProperties();
        }

        private static GameObject ReadLoadoutPaletteKnifeVisual(
            FigureToolLoadoutController loadout)
        {
            SerializedObject serialized = new SerializedObject(loadout);
            SerializedProperty property = serialized.FindProperty("paletteKnifeVisual");
            return property != null ? property.objectReferenceValue as GameObject : null;
        }

        private static T FindExactlyOne<T>(string label)
            where T : UnityEngine.Object
        {
            T[] found = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (found.Length != 1)
            {
                throw new InvalidOperationException(
                    $"M55.5 tek {label} bekliyor. Bulunan={found.Length}.");
            }

            return found[0];
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null) return "<null>";
            List<string> names = new List<string>();
            Transform current = transform;
            while (current != null)
            {
                names.Add(current.name);
                current = current.parent;
            }
            names.Reverse();
            return string.Join("/", names);
        }

        private static string ToAbsolutePath(string assetPath)
        {
            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Assets/ yolu bekleniyor: " + assetPath);
            }

            return Path.Combine(
                Application.dataPath,
                assetPath.Substring("Assets/".Length));
        }
    }
}
#endif
