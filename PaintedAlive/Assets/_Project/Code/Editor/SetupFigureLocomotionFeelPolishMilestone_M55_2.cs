#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// M55.2 — Figure locomotion feel / transition polish.
/// Presentation-only pass: FigureMotor + CharacterController remain gameplay authority.
/// The user has accepted the current walk/run/sprint world speeds, so this milestone
/// deliberately DOES NOT retime locomotion clips or modify movement speeds.
/// </summary>
public static class SetupFigureLocomotionFeelPolishMilestone_M55_2
{
    private const string MenuRoot = "Tools/Painted Alive/Milestones/";

    private const string AnimationRoot = "Assets/_Project/Art/Animations/Figure/Locomotion";
    private const string ControllerPath = "Assets/_Project/Art/Animations/Figure/Controllers/AC_Figure_Production.controller";
    private const string BackupControllerPath = "Assets/_Project/Art/Animations/Figure/Controllers/Backups/AC_Figure_Production_PreM55_2.controller";
    private const string VisualPrefabPath = "Assets/_Project/Art/Models/Figures/Prefabs/PF_Figure_ProductionVisual.prefab";

    // Accepted M55.1.1 locomotion blend. These are intentionally locked in M55.2.
    private const float IdleThreshold = 0.00f;
    private const float WalkThreshold = 0.38f;
    private const float RunThreshold = 0.72f;
    private const float SprintThreshold = 1.00f;

    // Presentation-only transition timing. No gameplay speed changes.
    private const float JumpEnterDuration = 0.035f;
    private const float EdgeFallEnterDuration = 0.060f;
    private const float JumpToFallDuration = 0.055f;
    private const float JumpApexThreshold = 0.080f;
    private const float JumpFallbackExitTime = 0.78f;

    // Recovery guard for very short hops / low-ceiling contacts.
    // If Jump_Start becomes grounded again before its airborne exits can fire,
    // route directly into Land instead of leaving the Animator trapped.
    private const float JumpGroundRecoveryVerticalThreshold = 0.05f;

    private const float LandEnterDuration = 0.035f;
    private const float MovingLandExitTime = 0.28f;
    private const float MovingLandBlendDuration = 0.075f;
    private const float IdleLandExitTime = 0.55f;
    private const float IdleLandBlendDuration = 0.10f;

    private sealed class ClipSpec
    {
        public string File;
        public string PreferredName;
        public bool ExpectedLoop;
        public string Path => AnimationRoot + "/" + File;
    }

    private static readonly ClipSpec[] Clips =
    {
        new ClipSpec { File = "PA_Anim_Idle.fbx",      PreferredName = "PA_Anim_Idle",      ExpectedLoop = true  },
        new ClipSpec { File = "PA_Anim_Walk.fbx",      PreferredName = "PA_Anim_Walk",      ExpectedLoop = true  },
        new ClipSpec { File = "PA_Anim_Run.fbx",       PreferredName = "PA_Anim_Run",       ExpectedLoop = true  },
        new ClipSpec { File = "PA_Anim_Sprint.fbx",    PreferredName = "PA_Anim_Sprint",    ExpectedLoop = true  },
        new ClipSpec { File = "PA_Anim_JumpStart.fbx", PreferredName = "PA_Anim_JumpStart", ExpectedLoop = false },
        new ClipSpec { File = "PA_Anim_FallLoop.fbx",  PreferredName = "PA_Anim_FallLoop",  ExpectedLoop = true  },
        new ClipSpec { File = "PA_Anim_Land.fbx",      PreferredName = "PA_Anim_Land",      ExpectedLoop = false },
    };

    [MenuItem(MenuRoot + "55.2 - Apply Locomotion Feel + Transition Polish")]
    public static void Apply()
    {
        try
        {
            ValidateInputsOrThrow();
            EnsureFolderForAsset(BackupControllerPath);
            BackupExistingControllerOnce();

            Dictionary<string, AnimationClip> clips = LoadClipsOrThrow();
            AnimatorController controller = RebuildPolishedController(clips);

            ApplyControllerToProductionPrefab(controller);
            int sceneAnimatorCount = ApplyControllerToLoadedSceneAnimators(controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string summary =
                "M55.2 locomotion feel / transition polish applied.\n\n" +
                "Walk/Run/Sprint world speeds: UNCHANGED / LOCKED\n" +
                "Locomotion thresholds: 0.00 / 0.38 / 0.72 / 1.00\n" +
                "Root Motion: OFF\n" +
                "Procedural Foot IK: NOT ADDED\n" +
                "Jump/Fall/Land transitions: POLISHED\n" +
                "Moving landing recovery: FASTER\n" +
                "Idle landing recovery: PRESERVED\n" +
                "Scene production Animators rebound: " + sceneAnimatorCount + "\n\n" +
                "Now run: 55.2 - Diagnose Locomotion Feel + Transition Polish";

            Debug.Log("[Painted Alive M55.2] " + summary.Replace("\n", " | "));
            EditorUtility.DisplayDialog("Painted Alive M55.2", summary, "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog(
                "Painted Alive M55.2 - Apply Failed",
                ex.Message + "\n\nNo gameplay movement speed or input was intentionally modified.",
                "OK");
        }
    }

    [MenuItem(MenuRoot + "55.2 - Diagnose Locomotion Feel + Transition Polish")]
    public static void Diagnose()
    {
        List<string> lines = new List<string>();
        bool pass = true;

        Action<bool, string, string> add = (ok, label, detail) =>
        {
            pass &= ok;
            lines.Add((ok ? "PASS" : "FAIL") + " | " + label + " | " + detail);
        };

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        add(controller != null, "Controller", controller != null ? ControllerPath : "missing");

        foreach (ClipSpec spec in Clips)
        {
            AnimationClip clip = LoadClip(spec.Path, spec.PreferredName);
            add(clip != null, spec.PreferredName, clip != null ? clip.length.ToString("0.000") + "s" : "missing");
            if (clip != null)
            {
                bool loopOk = clip.isLooping == spec.ExpectedLoop;
                add(loopOk,
                    spec.PreferredName + "Loop",
                    "isLooping=" + clip.isLooping + " expected=" + spec.ExpectedLoop);
            }
        }

        if (controller != null)
        {
            VerifyParameters(controller, add);
            VerifyControllerStructure(controller, add);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath);
        add(prefab != null, "ProductionVisualPrefab", prefab != null ? VisualPrefabPath : "missing");
        if (prefab != null)
        {
            Animator animator = prefab.GetComponentInChildren<Animator>(true);
            add(animator != null, "PrefabAnimator", animator != null ? animator.name : "missing");
            if (animator != null)
            {
                add(animator.runtimeAnimatorController == controller,
                    "PrefabControllerBound",
                    animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "null");
                add(!animator.applyRootMotion,
                    "PrefabRootMotionOff",
                    "applyRootMotion=" + animator.applyRootMotion);
            }
        }

        Animator[] sceneAnimators = FindLoadedSceneProductionAnimators();
        add(sceneAnimators.Length > 0,
            "SceneProductionAnimator",
            sceneAnimators.Length > 0 ? (sceneAnimators.Length + " found") : "none found in loaded scene");

        foreach (Animator animator in sceneAnimators)
        {
            add(animator.runtimeAnimatorController == controller,
                "SceneControllerBound:" + animator.name,
                animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "null");
            add(!animator.applyRootMotion,
                "SceneRootMotionOff:" + animator.name,
                "applyRootMotion=" + animator.applyRootMotion);
        }

        MonoBehaviour driver = FindLoadedSceneBehaviourByTypeName("FigureAnimationDriver");
        add(driver != null,
            "FigureAnimationDriver",
            driver != null ? GetHierarchyPath(driver.transform) : "not found in loaded scene");

        string report =
            "Painted Alive M55.2 Diagnostics\n" +
            "Result=" + (pass ? "PASS" : "NEEDS_ATTENTION") + "\n\n" +
            string.Join("\n", lines) +
            "\n\nManual Play Mode acceptance still required:\n" +
            "- Walk/Run/Sprint speed feels unchanged\n" +
            "- Idle<->movement has no visible hitch\n" +
            "- Jump_Start reaches Fall_Loop near apex\n" +
            "- Walking off an edge enters Fall_Loop directly\n" +
            "- Landing while holding movement does not feel sticky\n" +
            "- No world-space root drift / skating regression";

        Debug.Log("[Painted Alive M55.2]\n" + report);
        EditorUtility.DisplayDialog("M55.2 Diagnostics", report, "OK");
    }

    [MenuItem(MenuRoot + "55.2 - Restore Pre-M55.2 Animator Controller")]
    public static void RestoreBackup()
    {
        try
        {
            if (AssetDatabase.LoadMainAssetAtPath(BackupControllerPath) == null)
                throw new InvalidOperationException("M55.2 controller backup was not found: " + BackupControllerPath);

            if (AssetDatabase.LoadMainAssetAtPath(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);

            if (!AssetDatabase.CopyAsset(BackupControllerPath, ControllerPath))
                throw new InvalidOperationException("Could not restore controller backup.");

            AssetDatabase.ImportAsset(ControllerPath, ImportAssetOptions.ForceUpdate);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                throw new InvalidOperationException("Restored controller could not be loaded.");

            ApplyControllerToProductionPrefab(controller);
            int count = ApplyControllerToLoadedSceneAnimators(controller);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Painted Alive M55.2",
                "Pre-M55.2 Animator Controller restored.\nScene production Animators rebound: " + count,
                "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("M55.2 Restore Failed", ex.Message, "OK");
        }
    }

    private static void ValidateInputsOrThrow()
    {
        List<string> missing = new List<string>();
        foreach (ClipSpec spec in Clips)
        {
            if (AssetDatabase.LoadMainAssetAtPath(spec.Path) == null)
                missing.Add(spec.Path);
        }

        if (missing.Count > 0)
            throw new InvalidOperationException(
                "M55.2 requires the accepted M55.1.1 production animation assets. Missing:\n" +
                string.Join("\n", missing));
    }

    private static Dictionary<string, AnimationClip> LoadClipsOrThrow()
    {
        Dictionary<string, AnimationClip> result = new Dictionary<string, AnimationClip>();
        foreach (ClipSpec spec in Clips)
        {
            AnimationClip clip = LoadClip(spec.Path, spec.PreferredName);
            if (clip == null)
                throw new InvalidOperationException("AnimationClip missing: " + spec.PreferredName + " in " + spec.Path);
            result[spec.PreferredName] = clip;
        }
        return result;
    }

    private static AnimationClip LoadClip(string assetPath, string preferredName)
    {
        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<AnimationClip>()
            .Where(c => c != null && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return clips.FirstOrDefault(c => string.Equals(c.name, preferredName, StringComparison.OrdinalIgnoreCase))
               ?? clips.FirstOrDefault();
    }

    private static AnimatorController RebuildPolishedController(Dictionary<string, AnimationClip> clips)
    {
        EnsureFolderForAsset(ControllerPath);

        if (AssetDatabase.LoadMainAssetAtPath(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        if (controller == null)
            throw new InvalidOperationException("Could not create Animator Controller at " + ControllerPath);

        AddStandardParameters(controller);

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
        locomotionTree.AddChild(clips["PA_Anim_Idle"], IdleThreshold);
        locomotionTree.AddChild(clips["PA_Anim_Walk"], WalkThreshold);
        locomotionTree.AddChild(clips["PA_Anim_Run"], RunThreshold);
        locomotionTree.AddChild(clips["PA_Anim_Sprint"], SprintThreshold);

        AnimatorState locomotion = sm.AddState("Locomotion", new Vector3(250, 150, 0));
        locomotion.motion = locomotionTree;
        locomotion.speed = 1.0f;
        sm.defaultState = locomotion;

        AnimatorState jump = sm.AddState("Jump_Start", new Vector3(520, 55, 0));
        jump.motion = clips["PA_Anim_JumpStart"];
        jump.speed = 1.0f;

        AnimatorState fall = sm.AddState("Fall_Loop", new Vector3(785, 55, 0));
        fall.motion = clips["PA_Anim_FallLoop"];
        fall.speed = 1.0f;

        AnimatorState land = sm.AddState("Land", new Vector3(785, 220, 0));
        land.motion = clips["PA_Anim_Land"];
        land.speed = 1.0f;

        // Jump trigger: presentation reacts quickly without changing FigureMotor movement.
        AnimatorStateTransition jumpByTrigger = locomotion.AddTransition(jump);
        ConfigureImmediate(jumpByTrigger, JumpEnterDuration);
        jumpByTrigger.AddCondition(AnimatorConditionMode.If, 0.0f, "Jump");

        // Safety fallback for CharacterController grounded timing.
        AnimatorStateTransition jumpByVelocity = locomotion.AddTransition(jump);
        ConfigureImmediate(jumpByVelocity, JumpEnterDuration);
        jumpByVelocity.AddCondition(AnimatorConditionMode.IfNot, 0.0f, "Grounded");
        jumpByVelocity.AddCondition(AnimatorConditionMode.Greater, 0.05f, "VerticalSpeed");

        // Direct ledge fall path — no fake jump pose when walking off an edge.
        AnimatorStateTransition edgeFall = locomotion.AddTransition(fall);
        ConfigureImmediate(edgeFall, EdgeFallEnterDuration);
        edgeFall.AddCondition(AnimatorConditionMode.IfNot, 0.0f, "Grounded");
        edgeFall.AddCondition(AnimatorConditionMode.Less, -0.05f, "VerticalSpeed");

        // Hand control from Jump_Start to Fall_Loop around the apex.
        AnimatorStateTransition jumpToFall = jump.AddTransition(fall);
        ConfigureImmediate(jumpToFall, JumpToFallDuration);
        jumpToFall.AddCondition(AnimatorConditionMode.IfNot, 0.0f, "Grounded");
        jumpToFall.AddCondition(AnimatorConditionMode.Less, JumpApexThreshold, "VerticalSpeed");

        // Fallback prevents a malformed airborne state if velocity information is delayed.
        AnimatorStateTransition jumpFallback = jump.AddTransition(fall);
        jumpFallback.hasExitTime = true;
        jumpFallback.exitTime = JumpFallbackExitTime;
        jumpFallback.hasFixedDuration = true;
        jumpFallback.duration = JumpToFallDuration;
        jumpFallback.offset = 0.0f;
        jumpFallback.AddCondition(AnimatorConditionMode.IfNot, 0.0f, "Grounded");

        // Critical recovery path:
        // A short hop, step edge, low ceiling, or controller-grounding edge case
        // can make Jump_Start become grounded before either jump->fall path fires.
        // Both existing jump exits require !Grounded, so without this transition
        // Jump_Start can remain active indefinitely.
        //
        // VerticalSpeed must also be non-positive/near-zero so a one-frame
        // grounded report at the beginning of a real ascent does not cancel
        // the jump presentation.
        AnimatorStateTransition jumpGroundRecovery = jump.AddTransition(land);
        ConfigureImmediate(jumpGroundRecovery, LandEnterDuration);
        jumpGroundRecovery.AddCondition(
            AnimatorConditionMode.If,
            0.0f,
            "Grounded");
        jumpGroundRecovery.AddCondition(
            AnimatorConditionMode.Less,
            JumpGroundRecoveryVerticalThreshold,
            "VerticalSpeed");

        // Land trigger path plus grounded fallback. Both are presentation-only.
        AnimatorStateTransition landByTrigger = fall.AddTransition(land);
        ConfigureImmediate(landByTrigger, LandEnterDuration);
        landByTrigger.AddCondition(AnimatorConditionMode.If, 0.0f, "Land");

        AnimatorStateTransition landByGrounded = fall.AddTransition(land);
        ConfigureImmediate(landByGrounded, LandEnterDuration);
        landByGrounded.AddCondition(AnimatorConditionMode.If, 0.0f, "Grounded");

        // If movement is held, release the landing pose earlier so controls never feel sticky.
        AnimatorStateTransition movingLandExit = land.AddTransition(locomotion);
        movingLandExit.hasExitTime = true;
        movingLandExit.exitTime = MovingLandExitTime;
        movingLandExit.hasFixedDuration = true;
        movingLandExit.duration = MovingLandBlendDuration;
        movingLandExit.offset = 0.0f;
        movingLandExit.AddCondition(AnimatorConditionMode.If, 0.0f, "Moving");

        // If the player remains idle, allow more of the landing recovery to read.
        AnimatorStateTransition idleLandExit = land.AddTransition(locomotion);
        idleLandExit.hasExitTime = true;
        idleLandExit.exitTime = IdleLandExitTime;
        idleLandExit.hasFixedDuration = true;
        idleLandExit.duration = IdleLandBlendDuration;
        idleLandExit.offset = 0.0f;

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(locomotionTree);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static void AddStandardParameters(AnimatorController controller)
    {
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
    }

    private static void ConfigureImmediate(AnimatorStateTransition transition, float duration)
    {
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = duration;
        transition.offset = 0.0f;
    }

    private static void BackupExistingControllerOnce()
    {
        if (AssetDatabase.LoadMainAssetAtPath(ControllerPath) == null)
            return;

        if (AssetDatabase.LoadMainAssetAtPath(BackupControllerPath) != null)
            return;

        if (!AssetDatabase.CopyAsset(ControllerPath, BackupControllerPath))
            throw new InvalidOperationException("Could not create pre-M55.2 Animator Controller backup.");

        AssetDatabase.ImportAsset(BackupControllerPath, ImportAssetOptions.ForceUpdate);
    }

    private static void ApplyControllerToProductionPrefab(RuntimeAnimatorController controller)
    {
        if (AssetDatabase.LoadMainAssetAtPath(VisualPrefabPath) == null)
            return;

        GameObject root = PrefabUtility.LoadPrefabContents(VisualPrefabPath);
        try
        {
            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null)
                throw new InvalidOperationException("Production visual prefab has no Animator: " + VisualPrefabPath);

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.speed = 1.0f;
            EditorUtility.SetDirty(animator);
            PrefabUtility.SaveAsPrefabAsset(root, VisualPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static int ApplyControllerToLoadedSceneAnimators(RuntimeAnimatorController controller)
    {
        Animator[] animators = FindLoadedSceneProductionAnimators();
        foreach (Animator animator in animators)
        {
            Undo.RecordObject(animator, "Apply M55.2 Animator Polish");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.speed = 1.0f;
            EditorUtility.SetDirty(animator);
        }

        if (animators.Length > 0)
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        return animators.Length;
    }

    private static Animator[] FindLoadedSceneProductionAnimators()
    {
        return Resources.FindObjectsOfTypeAll<Animator>()
            .Where(a =>
                a != null &&
                a.gameObject.scene.IsValid() &&
                a.gameObject.scene.isLoaded &&
                (string.Equals(a.transform.name, "Figure_ProductionVisual", StringComparison.Ordinal) ||
                 GetHierarchyPath(a.transform).Contains("CharacterVisualRoot")))
            .ToArray();
    }

    private static MonoBehaviour FindLoadedSceneBehaviourByTypeName(string typeName)
    {
        return Resources.FindObjectsOfTypeAll<MonoBehaviour>()
            .FirstOrDefault(mb =>
                mb != null &&
                mb.gameObject.scene.IsValid() &&
                mb.gameObject.scene.isLoaded &&
                string.Equals(mb.GetType().Name, typeName, StringComparison.Ordinal));
    }

    private static void VerifyParameters(
        AnimatorController controller,
        Action<bool, string, string> add)
    {
        string[] required =
        {
            "Speed", "Speed01", "ForwardSpeed", "SideSpeed", "VerticalSpeed",
            "Grounded", "Moving", "Sprinting", "Jump", "Land"
        };

        HashSet<string> found = new HashSet<string>(controller.parameters.Select(p => p.name));
        string[] missing = required.Where(p => !found.Contains(p)).ToArray();
        add(missing.Length == 0,
            "AnimatorParameters",
            missing.Length == 0 ? "all present" : "missing=" + string.Join(",", missing));
    }

    private static void VerifyControllerStructure(
        AnimatorController controller,
        Action<bool, string, string> add)
    {
        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState locomotion = FindState(sm, "Locomotion");
        AnimatorState jump = FindState(sm, "Jump_Start");
        AnimatorState fall = FindState(sm, "Fall_Loop");
        AnimatorState land = FindState(sm, "Land");

        add(locomotion != null, "State:Locomotion", locomotion != null ? "present" : "missing");
        add(jump != null, "State:Jump_Start", jump != null ? "present" : "missing");
        add(fall != null, "State:Fall_Loop", fall != null ? "present" : "missing");
        add(land != null, "State:Land", land != null ? "present" : "missing");

        BlendTree tree = locomotion != null ? locomotion.motion as BlendTree : null;
        add(tree != null, "LocomotionBlendTree", tree != null ? tree.name : "missing");
        if (tree != null)
        {
            ChildMotion[] children = tree.children;
            add(children.Length == 4, "LocomotionClipCount", children.Length.ToString());
            if (children.Length == 4)
            {
                float[] expected = { IdleThreshold, WalkThreshold, RunThreshold, SprintThreshold };
                bool thresholdsOk = true;
                for (int i = 0; i < children.Length; i++)
                    thresholdsOk &= Mathf.Abs(children[i].threshold - expected[i]) <= 0.001f;

                add(thresholdsOk,
                    "AcceptedLocomotionThresholdsLocked",
                    string.Join(" / ", children.Select(c => c.threshold.ToString("0.00")).ToArray()));
            }
        }

        if (locomotion != null)
            add(locomotion.transitions.Length == 3,
                "LocomotionTransitions",
                locomotion.transitions.Length + " expected=3");

        if (jump != null)
            add(jump.transitions.Length == 3,
                "JumpTransitions",
                jump.transitions.Length + " expected=3 (includes grounded recovery)");

        if (fall != null)
            add(fall.transitions.Length == 2,
                "FallTransitions",
                fall.transitions.Length + " expected=2");

        if (land != null)
            add(land.transitions.Length == 2,
                "LandTransitions",
                land.transitions.Length + " expected=2");
    }

    private static AnimatorState FindState(AnimatorStateMachine sm, string name)
    {
        foreach (ChildAnimatorState child in sm.states)
        {
            if (child.state != null && string.Equals(child.state.name, name, StringComparison.Ordinal))
                return child.state;
        }
        return null;
    }

    private static void EnsureFolderForAsset(string assetPath)
    {
        string directory = System.IO.Path.GetDirectoryName(assetPath);
        if (string.IsNullOrEmpty(directory))
            return;

        string folder = directory.Replace('\\', '/');
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
