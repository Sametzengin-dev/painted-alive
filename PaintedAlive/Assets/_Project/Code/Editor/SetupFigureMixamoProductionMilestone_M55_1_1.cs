#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PaintedAlive.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SetupFigureMixamoProductionMilestone_M55_1_1
{
    private const string MenuRoot = "Tools/Painted Alive/Milestones/";

    private const string RigPath = "Assets/_Project/Art/Models/Figures/Source/PA_Figure_Rigged.fbx";
    private const string AnimationRoot = "Assets/_Project/Art/Animations/Figure/Locomotion";
    private const string BaseColorPath = "Assets/_Project/Art/Textures/Figures/PA_Figure_BaseColor_4K.jpg";
    private const string NormalPath = "Assets/_Project/Art/Textures/Figures/PA_Figure_Normal_4K.jpg";

    private const string ControllerPath = "Assets/_Project/Art/Animations/Figure/Controllers/AC_Figure_Production.controller";
    private const string MaterialPath = "Assets/_Project/Art/Materials/Figures/MAT_Figure_Production.mat";
    private const string PrefabPath = "Assets/_Project/Art/Models/Figures/Prefabs/PF_Figure_ProductionVisual.prefab";
    private const string ClipSetPath = "Assets/_Project/Data/Figures/Animation/DA_FigureMixamoClips_Production.asset";

    private sealed class ClipSpec
    {
        public string File;
        public string ClipName;
        public bool Loop;
        public bool LoopPose;
        public float ExpectedSeconds;

        public string Path => AnimationRoot + "/" + File;
    }

    private static readonly ClipSpec[] ClipSpecs =
    {
        new ClipSpec { File = "PA_Anim_Idle.fbx",      ClipName = "PA_Anim_Idle",      Loop = true,  LoopPose = true,  ExpectedSeconds = 14.333f },
        new ClipSpec { File = "PA_Anim_Walk.fbx",      ClipName = "PA_Anim_Walk",      Loop = true,  LoopPose = true,  ExpectedSeconds = 1.033f },
        new ClipSpec { File = "PA_Anim_Run.fbx",       ClipName = "PA_Anim_Run",       Loop = true,  LoopPose = true,  ExpectedSeconds = 0.633f },
        new ClipSpec { File = "PA_Anim_Sprint.fbx",    ClipName = "PA_Anim_Sprint",    Loop = true,  LoopPose = true,  ExpectedSeconds = 0.533f },
        new ClipSpec { File = "PA_Anim_JumpStart.fbx", ClipName = "PA_Anim_JumpStart", Loop = false, LoopPose = false, ExpectedSeconds = 0.833f },
        new ClipSpec { File = "PA_Anim_FallLoop.fbx",  ClipName = "PA_Anim_FallLoop",  Loop = true,  LoopPose = true,  ExpectedSeconds = 0.700f },
        new ClipSpec { File = "PA_Anim_Land.fbx",      ClipName = "PA_Anim_Land",      Loop = false, LoopPose = false, ExpectedSeconds = 1.067f },
    };

    [MenuItem(MenuRoot + "55.1.1 - Install Production Figure + Mixamo Locomotion")]
    public static void Install()
    {
        try
        {
            ValidateSourceAssetsOrThrow();
            EnsureOutputFolders();

            ConfigureTexture(BaseColorPath, false);
            ConfigureTexture(NormalPath, true);

            ConfigureRigImporter();
            Avatar avatar = LoadHumanAvatarOrThrow();

            foreach (ClipSpec spec in ClipSpecs)
                ConfigureAnimationImporter(spec, avatar);

            Dictionary<string, AnimationClip> clips = LoadClipsOrThrow();
            FigureMixamoClipSet_M55_1_1 clipSet = CreateOrUpdateClipSet(clips);
            Material material = CreateOrUpdateMaterial();
            AnimatorController controller = BuildProductionController(clipSet);
            GameObject prefab = CreateOrUpdateVisualPrefab(avatar, controller, material);

            bool sceneInstalled = InstallVisualIntoCurrentFigure(prefab, controller, avatar);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string summary =
                "M55.1.1 production Figure / Mixamo integration installed.\n\n" +
                "Humanoid Avatar: VALID\n" +
                "Clips: 7/7\n" +
                "Locomotion BlendTree: Idle / Walk / Run / Sprint\n" +
                "Air states: Jump_Start / Fall_Loop / Land\n" +
                "Root Motion: OFF\n" +
                "Textures: 4K BaseColor + 4K Normal\n" +
                "Current scene binding: " + (sceneInstalled ? "INSTALLED" : "SKIPPED (no FigureMotor found)") + "\n\n" +
                "Now run: 55.1.1 - Diagnose Production Figure + Mixamo Locomotion";

            Debug.Log("[Painted Alive M55.1.1] " + summary.Replace("\n", " | "));
            EditorUtility.DisplayDialog("Painted Alive M55.1.1", summary, "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog(
                "Painted Alive M55.1.1 - Install Failed",
                ex.Message + "\n\nNo gameplay code was intentionally modified.",
                "OK");
        }
    }

    [MenuItem(MenuRoot + "55.1.1 - Diagnose Production Figure + Mixamo Locomotion")]
    public static void Diagnose()
    {
        List<string> lines = new List<string>();
        bool pass = true;

        Action<bool, string, string> add = (ok, label, detail) =>
        {
            pass &= ok;
            lines.Add((ok ? "PASS" : "FAIL") + " | " + label + " | " + detail);
        };

        add(FileAssetExists(RigPath), "RigAsset", RigPath);
        add(FileAssetExists(BaseColorPath), "BaseColor", BaseColorPath);
        add(FileAssetExists(NormalPath), "NormalMap", NormalPath);

        Avatar avatar = LoadHumanAvatar();
        add(avatar != null, "AvatarPresent", avatar != null ? avatar.name : "missing");
        add(avatar != null && avatar.isValid, "AvatarValid", avatar != null ? avatar.isValid.ToString() : "false");
        add(avatar != null && avatar.isHuman, "AvatarHuman", avatar != null ? avatar.isHuman.ToString() : "false");

        foreach (ClipSpec spec in ClipSpecs)
        {
            AnimationClip clip = LoadImportedClip(spec.Path, spec.ClipName);
            bool durationOk = clip != null && Mathf.Abs(clip.length - spec.ExpectedSeconds) <= 0.20f;
            add(clip != null, spec.ClipName, clip != null ? (clip.length.ToString("0.000") + "s") : "missing");
            add(durationOk, spec.ClipName + "Duration", clip != null ? (clip.length.ToString("0.000") + "s expected≈" + spec.ExpectedSeconds.ToString("0.000") + "s") : "missing");
        }

        Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorPath);
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
        add(baseColor != null && baseColor.width == 4096 && baseColor.height == 4096,
            "BaseColorResolution",
            baseColor != null ? (baseColor.width + "x" + baseColor.height) : "missing");
        add(normal != null && normal.width == 4096 && normal.height == 4096,
            "NormalResolution",
            normal != null ? (normal.width + "x" + normal.height) : "missing");

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        add(controller != null, "AnimatorController", ControllerPath);
        if (controller != null)
        {
            string[] required = { "Speed", "Speed01", "ForwardSpeed", "SideSpeed", "VerticalSpeed", "Grounded", "Moving", "Sprinting", "Jump", "Land" };
            HashSet<string> found = new HashSet<string>(controller.parameters.Select(p => p.name));
            string[] missing = required.Where(p => !found.Contains(p)).ToArray();
            add(missing.Length == 0, "AnimatorParameters", missing.Length == 0 ? "all present" : ("missing=" + string.Join(",", missing)));
        }

        GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        add(visualPrefab != null, "VisualPrefab", PrefabPath);
        if (visualPrefab != null)
        {
            Animator animator = visualPrefab.GetComponentInChildren<Animator>(true);
            add(animator != null, "PrefabAnimator", animator != null ? animator.name : "missing");
            add(animator != null && animator.avatar != null && animator.avatar.isHuman,
                "PrefabHumanoidAvatar",
                animator != null && animator.avatar != null ? animator.avatar.name : "missing");
            add(animator != null && animator.runtimeAnimatorController != null,
                "PrefabController",
                animator != null && animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "missing");
            add(animator != null && !animator.applyRootMotion,
                "ApplyRootMotion",
                animator != null ? animator.applyRootMotion.ToString() : "missing animator");
            add(visualPrefab.GetComponentsInChildren<Collider>(true).Length == 0,
                "VisualGameplayColliders",
                visualPrefab.GetComponentsInChildren<Collider>(true).Length.ToString());
            add(visualPrefab.GetComponentsInChildren<Rigidbody>(true).Length == 0,
                "VisualRigidbodies",
                visualPrefab.GetComponentsInChildren<Rigidbody>(true).Length.ToString());
        }

        MonoBehaviour figureMotor = FindSceneBehaviourByTypeName("FigureMotor");
        add(figureMotor != null, "SceneFigureMotor", figureMotor != null ? GetHierarchyPath(figureMotor.transform) : "not found in current scene");
        if (figureMotor != null)
        {
            Transform visualRoot = figureMotor.transform.Find("CharacterVisualRoot");
            add(visualRoot != null, "CharacterVisualRoot", visualRoot != null ? GetHierarchyPath(visualRoot) : "missing");
            Transform installed = visualRoot != null ? visualRoot.Find("Figure_ProductionVisual") : null;
            add(installed != null, "ProductionVisualInstalled", installed != null ? GetHierarchyPath(installed) : "missing");

            Renderer[] visibleLegacy = figureMotor.GetComponentsInChildren<Renderer>(true)
                .Where(r => r != null && r.enabled &&
                            (visualRoot == null || !(r.transform == visualRoot || r.transform.IsChildOf(visualRoot))) &&
                            IsLegacyFigurePlaceholderRenderer(r))
                .ToArray();
            add(visibleLegacy.Length == 0,
                "LegacyPlaceholderHidden",
                visibleLegacy.Length == 0 ? "no visible Cylinder/Capsule placeholder renderer" :
                ("visible=" + string.Join(",", visibleLegacy.Select(r => GetHierarchyPath(r.transform)).ToArray())));

            MonoBehaviour driver = FindBehaviourInChildrenByTypeName(figureMotor.transform, "FigureAnimationDriver");
            add(driver != null, "FigureAnimationDriver", driver != null ? driver.GetType().FullName : "not found; run M55.0 rebind");

            if (installed != null)
            {
                float height = CalculateRendererBoundsHeight(installed.gameObject);
                add(height >= 1.50f && height <= 2.20f, "VisualHeight", height.ToString("0.000") + " m (target≈1.8m)");
            }
        }

        string report = "Painted Alive M55.1.1 Diagnostics\n" +
                        "Result=" + (pass ? "PASS" : "NEEDS_ATTENTION") + "\n\n" +
                        string.Join("\n", lines);

        Debug.Log("[Painted Alive M55.1.1]\n" + report);
        EditorUtility.DisplayDialog("M55.1.1 Diagnostics", report, "OK");
    }

    private static void ValidateSourceAssetsOrThrow()
    {
        List<string> missing = new List<string>();
        if (!FileAssetExists(RigPath)) missing.Add(RigPath);
        if (!FileAssetExists(BaseColorPath)) missing.Add(BaseColorPath);
        if (!FileAssetExists(NormalPath)) missing.Add(NormalPath);
        foreach (ClipSpec spec in ClipSpecs)
            if (!FileAssetExists(spec.Path)) missing.Add(spec.Path);

        if (missing.Count > 0)
            throw new FileNotFoundException("M55.1.1 source assets missing:\n" + string.Join("\n", missing));
    }

    private static bool FileAssetExists(string assetPath)
    {
        return AssetDatabase.LoadMainAssetAtPath(assetPath) != null || File.Exists(assetPath);
    }

    private static void EnsureOutputFolders()
    {
        EnsureFolderForAsset(ControllerPath);
        EnsureFolderForAsset(MaterialPath);
        EnsureFolderForAsset(PrefabPath);
        EnsureFolderForAsset(ClipSetPath);
    }

    private static void EnsureFolderForAsset(string assetPath)
    {
        string folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(folder)) return;

        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static void ConfigureTexture(string path, bool normalMap)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("TextureImporter missing for " + path);

        importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.maxTextureSize = 4096;
        importer.mipmapEnabled = true;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.crunchedCompression = false;
        if (!normalMap)
            importer.sRGBTexture = true;

        importer.SaveAndReimport();
    }

    private static void ConfigureRigImporter()
    {
        ModelImporter importer = AssetImporter.GetAtPath(RigPath) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException("ModelImporter missing for " + RigPath);

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = false; // main FBX contains only a one-frame Mixamo take; do not use it.
        importer.importBlendShapes = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.optimizeGameObjects = false; // keep bones available for later tools/attachments.
        importer.preserveHierarchy = true;
        importer.globalScale = 1.0f;
        importer.useFileScale = true;

        importer.SaveAndReimport();
    }

    private static Avatar LoadHumanAvatarOrThrow()
    {
        Avatar avatar = LoadHumanAvatar();
        if (avatar == null)
            throw new InvalidOperationException("No Avatar was generated from " + RigPath);
        if (!avatar.isValid || !avatar.isHuman)
            throw new InvalidOperationException("Generated Avatar is not a valid Humanoid. isValid=" + avatar.isValid + " isHuman=" + avatar.isHuman);
        return avatar;
    }

    private static Avatar LoadHumanAvatar()
    {
        return AssetDatabase.LoadAllAssetsAtPath(RigPath)
            .OfType<Avatar>()
            .FirstOrDefault(a => a != null && a.isValid)
            ?? AssetDatabase.LoadAllAssetsAtPath(RigPath).OfType<Avatar>().FirstOrDefault();
    }

    private static void ConfigureAnimationImporter(ClipSpec spec, Avatar sourceAvatar)
    {
        ModelImporter importer = AssetImporter.GetAtPath(spec.Path) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException("ModelImporter missing for " + spec.Path);

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        importer.sourceAvatar = sourceAvatar;
        importer.importAnimation = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.optimizeGameObjects = false;
        importer.globalScale = 1.0f;
        importer.useFileScale = true;

        ModelImporterClipAnimation[] defaults = importer.defaultClipAnimations;
        if (defaults == null || defaults.Length == 0)
            throw new InvalidOperationException("No animation take found in " + spec.Path);

        ModelImporterClipAnimation clip = defaults[0];
        clip.name = spec.ClipName;
        clip.loopTime = spec.Loop;
        clip.loopPose = spec.LoopPose;
        clip.cycleOffset = 0.0f;
        clip.mirror = false;

        // FigureMotor / CharacterController remain world-motion authority.
        // Bake Mixamo root motion into the pose so animation cannot drift the gameplay root.
        clip.lockRootRotation = true;
        clip.lockRootHeightY = true;
        clip.lockRootPositionXZ = true;
        clip.keepOriginalOrientation = true;
        clip.keepOriginalPositionY = true;
        clip.keepOriginalPositionXZ = true;
        clip.heightFromFeet = false;

        importer.clipAnimations = new[] { clip };
        importer.SaveAndReimport();
    }

    private static Dictionary<string, AnimationClip> LoadClipsOrThrow()
    {
        Dictionary<string, AnimationClip> result = new Dictionary<string, AnimationClip>();
        foreach (ClipSpec spec in ClipSpecs)
        {
            AnimationClip clip = LoadImportedClip(spec.Path, spec.ClipName);
            if (clip == null)
                throw new InvalidOperationException("Imported AnimationClip not found: " + spec.ClipName + " in " + spec.Path);
            result[spec.ClipName] = clip;
        }
        return result;
    }

    private static AnimationClip LoadImportedClip(string path, string preferredName)
    {
        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .Where(c => c != null && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return clips.FirstOrDefault(c => string.Equals(c.name, preferredName, StringComparison.OrdinalIgnoreCase))
               ?? clips.FirstOrDefault();
    }

    private static FigureMixamoClipSet_M55_1_1 CreateOrUpdateClipSet(Dictionary<string, AnimationClip> clips)
    {
        FigureMixamoClipSet_M55_1_1 set = AssetDatabase.LoadAssetAtPath<FigureMixamoClipSet_M55_1_1>(ClipSetPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<FigureMixamoClipSet_M55_1_1>();
            AssetDatabase.CreateAsset(set, ClipSetPath);
        }

        set.Idle = clips["PA_Anim_Idle"];
        set.Walk = clips["PA_Anim_Walk"];
        set.Run = clips["PA_Anim_Run"];
        set.Sprint = clips["PA_Anim_Sprint"];
        set.JumpStart = clips["PA_Anim_JumpStart"];
        set.FallLoop = clips["PA_Anim_FallLoop"];
        set.Land = clips["PA_Anim_Land"];
        EditorUtility.SetDirty(set);

        PopulateLegacyM55_1ClipSetIfPresent(set);
        return set;
    }

    private static void PopulateLegacyM55_1ClipSetIfPresent(FigureMixamoClipSet_M55_1_1 set)
    {
        const string legacyPath = "Assets/_Project/Data/Figures/Animation/DA_FigureAnimationClips_Production.asset";
        UnityEngine.Object legacy = AssetDatabase.LoadMainAssetAtPath(legacyPath);
        if (legacy == null) return;

        SerializedObject so = new SerializedObject(legacy);
        SetObjectReferenceIfPresent(so, new[] { "Idle", "idle" }, set.Idle);
        SetObjectReferenceIfPresent(so, new[] { "Walk", "walk" }, set.Walk);
        SetObjectReferenceIfPresent(so, new[] { "RunSprint", "runSprint", "Run", "run" }, set.Sprint);
        SetObjectReferenceIfPresent(so, new[] { "JumpStart", "jumpStart", "Jump_Start" }, set.JumpStart);
        SetObjectReferenceIfPresent(so, new[] { "FallLoop", "fallLoop", "Fall_Loop" }, set.FallLoop);
        SetObjectReferenceIfPresent(so, new[] { "Land", "land" }, set.Land);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(legacy);
    }

    private static void SetObjectReferenceIfPresent(SerializedObject so, IEnumerable<string> names, UnityEngine.Object value)
    {
        foreach (string name in names)
        {
            SerializedProperty property = so.FindProperty(name);
            if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
            {
                property.objectReferenceValue = value;
                return;
            }
        }
    }

    private static Material CreateOrUpdateMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("HDRP/Lit")
                        ?? Shader.Find("Standard");

        if (shader == null)
            throw new InvalidOperationException("No supported Lit shader found (URP/HDRP/Standard).");

        if (material == null)
        {
            material = new Material(shader) { name = "MAT_Figure_Production" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else if (material.shader == null)
        {
            material.shader = shader;
        }

        Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorPath);
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);

        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", baseColor);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", baseColor);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);

        if (material.HasProperty("_BumpMap"))
        {
            material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
        }
        if (material.HasProperty("_NormalMap")) material.SetTexture("_NormalMap", normal);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.0f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.18f);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.18f);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static AnimatorController BuildProductionController(FigureMixamoClipSet_M55_1_1 set)
    {
        if (!set.HasAllRequiredClips)
            throw new InvalidOperationException("Production clip set is incomplete.");

        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Speed01", AnimatorControllerParameterType.Float);
        controller.AddParameter("ForwardSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("SideSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Sprinting", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Land", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        sm.name = "Base Layer";

        BlendTree locomotionTree = new BlendTree
        {
            name = "BT_Figure_Locomotion",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed01",
            useAutomaticThresholds = false
        };
        AssetDatabase.AddObjectToAsset(locomotionTree, controller);
        locomotionTree.AddChild(set.Idle, 0.00f);
        locomotionTree.AddChild(set.Walk, 0.38f);
        locomotionTree.AddChild(set.Run, 0.72f);
        locomotionTree.AddChild(set.Sprint, 1.00f);

        AnimatorState locomotion = sm.AddState("Locomotion", new Vector3(260, 150, 0));
        locomotion.motion = locomotionTree;
        sm.defaultState = locomotion;

        AnimatorState jump = sm.AddState("Jump_Start", new Vector3(530, 60, 0));
        jump.motion = set.JumpStart;

        AnimatorState fall = sm.AddState("Fall_Loop", new Vector3(790, 60, 0));
        fall.motion = set.FallLoop;

        AnimatorState land = sm.AddState("Land", new Vector3(790, 210, 0));
        land.motion = set.Land;

        // Primary Jump trigger path.
        AnimatorStateTransition jumpByTrigger = locomotion.AddTransition(jump);
        ConfigureNoExitTransition(jumpByTrigger, 0.05f);
        jumpByTrigger.AddCondition(AnimatorConditionMode.If, 0.0f, "Jump");

        // Runtime fallback if CharacterController grounded timing misses the Jump trigger.
        AnimatorStateTransition jumpByVelocity = locomotion.AddTransition(jump);
        ConfigureNoExitTransition(jumpByVelocity, 0.05f);
        jumpByVelocity.AddCondition(AnimatorConditionMode.IfNot, 0.0f, "Grounded");
        jumpByVelocity.AddCondition(AnimatorConditionMode.Greater, 0.05f, "VerticalSpeed");

        // Walk/run off an edge without a jump.
        AnimatorStateTransition edgeFall = locomotion.AddTransition(fall);
        ConfigureNoExitTransition(edgeFall, 0.08f);
        edgeFall.AddCondition(AnimatorConditionMode.IfNot, 0.0f, "Grounded");
        edgeFall.AddCondition(AnimatorConditionMode.Less, -0.05f, "VerticalSpeed");

        // Jump_Start hands control to Fall_Loop at the apex.
        AnimatorStateTransition jumpToFall = jump.AddTransition(fall);
        ConfigureNoExitTransition(jumpToFall, 0.07f);
        jumpToFall.AddCondition(AnimatorConditionMode.Less, -0.05f, "VerticalSpeed");

        // Safety fallback if the vertical-speed signal is delayed.
        AnimatorStateTransition jumpFallback = jump.AddTransition(fall);
        jumpFallback.hasExitTime = true;
        jumpFallback.exitTime = 0.92f;
        jumpFallback.hasFixedDuration = true;
        jumpFallback.duration = 0.05f;

        AnimatorStateTransition landByTrigger = fall.AddTransition(land);
        ConfigureNoExitTransition(landByTrigger, 0.05f);
        landByTrigger.AddCondition(AnimatorConditionMode.If, 0.0f, "Land");

        AnimatorStateTransition landByGrounded = fall.AddTransition(land);
        ConfigureNoExitTransition(landByGrounded, 0.05f);
        landByGrounded.AddCondition(AnimatorConditionMode.If, 0.0f, "Grounded");

        AnimatorStateTransition landToLocomotion = land.AddTransition(locomotion);
        landToLocomotion.hasExitTime = true;
        landToLocomotion.exitTime = 0.62f;
        landToLocomotion.hasFixedDuration = true;
        landToLocomotion.duration = 0.08f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static void ConfigureNoExitTransition(AnimatorStateTransition transition, float duration)
    {
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = duration;
        transition.offset = 0.0f;
    }

    private static GameObject CreateOrUpdateVisualPrefab(Avatar avatar, RuntimeAnimatorController controller, Material material)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
        if (model == null)
            throw new InvalidOperationException("Rig model prefab could not be loaded: " + RigPath);

        GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (instance == null)
            instance = UnityEngine.Object.Instantiate(model);

        try
        {
            instance.name = "Figure_ProductionVisual";
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);
            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true))
                UnityEngine.Object.DestroyImmediate(body);

            foreach (SkinnedMeshRenderer renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                    renderer.sharedMaterial = material;
                else
                {
                    for (int i = 0; i < materials.Length; i++) materials[i] = material;
                    renderer.sharedMaterials = materials;
                }
            }

            Animator animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Failed to save production visual prefab at " + PrefabPath);
            return prefab;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static bool InstallVisualIntoCurrentFigure(GameObject visualPrefab, RuntimeAnimatorController controller, Avatar avatar)
    {
        MonoBehaviour figureMotor = FindSceneBehaviourByTypeName("FigureMotor");
        if (figureMotor == null)
        {
            Debug.LogWarning("[Painted Alive M55.1.1] No FigureMotor found in the current loaded scene. Assets were installed, but scene visual binding was skipped.");
            return false;
        }

        Transform gameplayRoot = figureMotor.transform;
        Transform visualRoot = gameplayRoot.Find("CharacterVisualRoot");
        if (visualRoot == null)
        {
            GameObject visualRootObject = new GameObject("CharacterVisualRoot");
            Undo.RegisterCreatedObjectUndo(visualRootObject, "Create CharacterVisualRoot");
            visualRoot = visualRootObject.transform;
            visualRoot.SetParent(gameplayRoot, false);
        }

        Transform old = visualRoot.Find("Figure_ProductionVisual");
        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject);

        GameObject visual = PrefabUtility.InstantiatePrefab(visualPrefab, visualRoot) as GameObject;
        if (visual == null)
            visual = UnityEngine.Object.Instantiate(visualPrefab, visualRoot);

        visual.name = "Figure_ProductionVisual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        // M55.1.1.0.2: the gameplay placeholder primitive must remain as the
        // CharacterController/FigureMotor root, but its renderer must not remain visible
        // behind the production character. Only known Cylinder/Capsule placeholder
        // renderers outside CharacterVisualRoot are disabled; gameplay colliders and
        // components are intentionally untouched.
        int hiddenPlaceholderRenderers = HideLegacyPlaceholderRenderers(gameplayRoot, visualRoot);
        if (hiddenPlaceholderRenderers > 0)
            Debug.Log("[Painted Alive M55.1.1.0.2] Hidden legacy Figure placeholder renderer(s): " + hiddenPlaceholderRenderers);

        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            Undo.DestroyObjectImmediate(collider);
        foreach (Rigidbody body in visual.GetComponentsInChildren<Rigidbody>(true))
            Undo.DestroyObjectImmediate(body);

        Animator animator = visual.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            TryBindExistingFigureAnimationDriver(gameplayRoot, animator);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return true;
    }

    private static int HideLegacyPlaceholderRenderers(Transform gameplayRoot, Transform visualRoot)
    {
        int hidden = 0;
        foreach (Renderer renderer in gameplayRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || !renderer.enabled)
                continue;

            // Never touch the production visual (or anything intentionally placed under
            // CharacterVisualRoot). This repair is only for the old primitive placeholder.
            if (renderer.transform == visualRoot || renderer.transform.IsChildOf(visualRoot))
                continue;

            if (!IsLegacyFigurePlaceholderRenderer(renderer))
                continue;

            Undo.RecordObject(renderer, "Hide Figure Placeholder Renderer");
            renderer.enabled = false;
            EditorUtility.SetDirty(renderer);
            hidden++;
        }
        return hidden;
    }

    private static bool IsLegacyFigurePlaceholderRenderer(Renderer renderer)
    {
        // The existing prototype Figure is a Unity primitive cylinder/capsule.
        // Restrict the repair to those known primitive visuals so unrelated scene art
        // under the gameplay root can never be disabled accidentally.
        MeshRenderer meshRenderer = renderer as MeshRenderer;
        if (meshRenderer == null)
            return false;

        MeshFilter filter = renderer.GetComponent<MeshFilter>();
        string meshName = filter != null && filter.sharedMesh != null
            ? filter.sharedMesh.name.ToLowerInvariant()
            : string.Empty;
        string objectName = renderer.gameObject.name.ToLowerInvariant();

        return meshName.Contains("cylinder") ||
               meshName.Contains("capsule") ||
               objectName == "cylinder" ||
               objectName == "capsule" ||
               objectName.Contains("figure_placeholder");
    }

    private static void TryBindExistingFigureAnimationDriver(Transform gameplayRoot, Animator animator)
    {
        MonoBehaviour driver = FindBehaviourInChildrenByTypeName(gameplayRoot, "FigureAnimationDriver");
        if (driver == null)
        {
            Debug.LogWarning("[Painted Alive M55.1.1] FigureAnimationDriver not found. M55.0 driver is required. If it exists elsewhere, run '55.0 - Apply or Rebind Figure Animation Foundation' after this installer.");
            return;
        }

        SerializedObject serialized = new SerializedObject(driver);
        string[] likelyNames = { "animator", "targetAnimator", "characterAnimator", "_animator" };
        foreach (string name in likelyNames)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
            {
                property.objectReferenceValue = animator;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(driver);
                Debug.Log("[Painted Alive M55.1.1] Bound existing FigureAnimationDriver to production Animator via serialized field '" + name + "'.");
                return;
            }
        }

        // M55.0 driver also supports safe child-Animator auto-binding; leave it intact if its field name differs.
        Debug.Log("[Painted Alive M55.1.1] FigureAnimationDriver found; explicit Animator field was not discovered. Driver child-Animator auto-binding will be used.");
    }

    private static MonoBehaviour FindSceneBehaviourByTypeName(string typeName)
    {
        return Resources.FindObjectsOfTypeAll<MonoBehaviour>()
            .FirstOrDefault(mb =>
                mb != null &&
                mb.gameObject.scene.IsValid() &&
                mb.gameObject.scene.isLoaded &&
                string.Equals(mb.GetType().Name, typeName, StringComparison.Ordinal));
    }

    private static MonoBehaviour FindBehaviourInChildrenByTypeName(Transform root, string typeName)
    {
        return root.GetComponentsInChildren<MonoBehaviour>(true)
            .FirstOrDefault(mb => mb != null && string.Equals(mb.GetType().Name, typeName, StringComparison.Ordinal));
    }

    private static float CalculateRendererBoundsHeight(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return 0.0f;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds.size.y;
    }

    private static string GetHierarchyPath(Transform t)
    {
        if (t == null) return "<null>";
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
#endif
