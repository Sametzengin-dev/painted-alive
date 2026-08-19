#if UNITY_EDITOR
using System;
using PaintedAlive.Figures.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.Editor
{
    public static class SetupFigureLocomotionAnimatorMilestone
    {
        private const string DataFolder =
            "Assets/_Project/Data/Figures/Animation";

        private const string ClipSetPath =
            DataFolder +
            "/DA_FigureAnimationClips_Production.asset";

        private const string ControllerFolder =
            "Assets/_Project/Art/Animations/Figure/Controllers";

        private const string ControllerPath =
            ControllerFolder +
            "/AC_Figure_Production.controller";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "55.1 - Create or Select Figure Animation Clip Set")]
        public static void CreateOrSelectClipSet()
        {
            try
            {
                FigureAnimationClipSet clipSet =
                    GetOrCreateClipSet();

                Selection.activeObject =
                    clipSet;

                EditorGUIUtility.PingObject(
                    clipSet);

                Debug.Log(
                    "[M55.1] Figure animation clip-set ready.\n" +
                    $"Asset={ClipSetPath}\n" +
                    $"MissingRequiredClipCount={clipSet.MissingRequiredClipCount}\n" +
                    $"MissingRequiredClipNames={clipSet.MissingRequiredClipNames}\n" +
                    "Required=Idle, Walk, RunSprint, Jump_Start, Fall_Loop, Land",
                    clipSet);

                EditorUtility.DisplayDialog(
                    "M55.1 Clip Set Hazır",
                    "Clip Set seçildi. Inspector'da gerçek locomotion kliplerini " +
                    "ilgili alanlara sürükle. Altı zorunlu klip dolduktan sonra " +
                    "55.1 Build/Rebuild menüsünü çalıştır.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M55.1 Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "55.1 - Build or Rebuild Figure Locomotion Controller")]
        public static void BuildOrRebuildController()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "Animator Controller build Play Mode dışında çalıştırılmalıdır.");
                }

                FigureAnimationClipSet clipSet =
                    GetOrCreateClipSet();

                if (!clipSet.RequiredLocomotionAssigned)
                {
                    Selection.activeObject =
                        clipSet;

                    EditorGUIUtility.PingObject(
                        clipSet);

                    throw new InvalidOperationException(
                        "Zorunlu locomotion klipleri eksik: " +
                        clipSet.MissingRequiredClipNames);
                }

                FigureAnimationDriver driver =
                    FindRequired<
                        FigureAnimationDriver>();

                Animator animator =
                    driver.Animator;

                if (animator == null)
                {
                    throw new InvalidOperationException(
                        "Figure Animator henüz bağlı değil. Karakteri CharacterVisualRoot " +
                        "altına ekleyip önce 55.0 Rebind çalıştır.");
                }

                Avatar avatar =
                    animator.avatar;

                if (
                    avatar == null ||
                    !avatar.isValid ||
                    !avatar.isHuman
                )
                {
                    throw new InvalidOperationException(
                        "Figure Animator geçerli Humanoid Avatar taşımıyor.");
                }

                FigureAnimationProfile profile =
                    driver.Profile;

                if (profile == null)
                {
                    throw new InvalidOperationException(
                        "FigureAnimationProfile bağlı değil. Önce M55.0 setup/rebind çalıştır.");
                }

                EnsureFolder(
                    ControllerFolder);

                DeleteControllerIfExists();

                AnimatorController controller =
                    AnimatorController.CreateAnimatorControllerAtPath(
                        ControllerPath);

                if (controller == null)
                {
                    throw new InvalidOperationException(
                        "Animator Controller oluşturulamadı.");
                }

                AddParameters(
                    controller,
                    profile);

                AnimatorStateMachine stateMachine =
                    controller.layers[0].stateMachine;

                ClearStateMachine(
                    stateMachine);

                AnimatorState locomotion =
                    CreateLocomotionState(
                        controller,
                        stateMachine,
                        clipSet,
                        profile);

                AnimatorState jump =
                    CreateState(
                        stateMachine,
                        "Jump_Start",
                        clipSet.JumpStart,
                        new Vector3(
                            300f,
                            -40f,
                            0f));

                AnimatorState fall =
                    CreateState(
                        stateMachine,
                        "Fall_Loop",
                        clipSet.FallLoop,
                        new Vector3(
                            540f,
                            -40f,
                            0f));

                AnimatorState land =
                    CreateState(
                        stateMachine,
                        "Land",
                        clipSet.Land,
                        new Vector3(
                            780f,
                            -40f,
                            0f));

                stateMachine.defaultState =
                    locomotion;

                ConfigureTransitions(
                    stateMachine,
                    locomotion,
                    jump,
                    fall,
                    land,
                    profile);

                animator.runtimeAnimatorController =
                    controller;

                animator.applyRootMotion =
                    false;

                EditorUtility.SetDirty(
                    animator);

                EditorUtility.SetDirty(
                    driver);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                driver.Configure(
                    driver.FigureMotor,
                    driver.FigureMotor != null
                        ? driver.FigureMotor.GetComponent<CharacterController>()
                        : null,
                    animator,
                    profile);

                EditorSceneManager.MarkSceneDirty(
                    animator.gameObject.scene);

                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M55.1 Build] Figure locomotion Animator Controller ready.\n" +
                    $"Controller={ControllerPath}\n" +
                    $"Animator={GetHierarchyPath(animator.transform)}\n" +
                    "Locomotion=1D BlendTree(Speed01)\n" +
                    "Thresholds=Idle 0.00 / Walk 0.42 / RunSprint 1.00\n" +
                    "AirStates=Jump_Start -> Fall_Loop -> Land -> Locomotion\n" +
                    "ApplyRootMotion=False\n" +
                    "HumanoidAvatarValid=True\n" +
                    "GameplayMotorRemainsAuthoritative=True\n" +
                    "AddsNewGameplayInputAction=False\n" +
                    "NetworkAuthorityEnabled=False",
                    animator);

                EditorUtility.DisplayDialog(
                    "M55.1 Controller Hazır",
                    "Production locomotion Animator Controller oluşturuldu ve " +
                    "Figure Animator'a bağlandı. Şimdi Play Mode'da hareket/zıplama " +
                    "animasyonlarını test et.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M55.1 Build Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "55.1 - Diagnose Figure Locomotion Animator")]
        public static void Diagnose()
        {
            FigureAnimationClipSet clipSet =
                AssetDatabase.LoadAssetAtPath<
                    FigureAnimationClipSet>(
                        ClipSetPath);

            FigureAnimationDriver driver =
                FindOptional<
                    FigureAnimationDriver>();

            Animator animator =
                driver != null
                    ? driver.Animator
                    : null;

            RuntimeAnimatorController controller =
                animator != null
                    ? animator.runtimeAnimatorController
                    : null;

            Avatar avatar =
                animator != null
                    ? animator.avatar
                    : null;

            bool humanoidValid =
                avatar != null &&
                avatar.isValid &&
                avatar.isHuman;

            string report =
                "[M55.1 Diagnose]\n" +
                $"ClipSet={(clipSet != null ? "OK" : "MISSING")}\n" +
                $"RequiredClipsAssigned={(clipSet != null && clipSet.RequiredLocomotionAssigned)}\n" +
                $"MissingRequiredClipCount={(clipSet != null ? clipSet.MissingRequiredClipCount : 6)}\n" +
                $"MissingRequiredClipNames={(clipSet != null ? clipSet.MissingRequiredClipNames : "All")}\n" +
                $"AnimationDriver={(driver != null ? "OK" : "MISSING")}\n" +
                $"Animator={(animator != null ? animator.name : "MISSING")}\n" +
                $"HumanoidAvatarValid={humanoidValid}\n" +
                $"Controller={(controller != null ? controller.name : "MISSING")}\n" +
                $"ApplyRootMotion={(animator != null && animator.applyRootMotion)}\n" +
                $"ParametersReady={(driver != null && driver.ParametersReady)}\n" +
                $"MissingAnimatorParameterCount={(driver != null ? driver.MissingRequiredParameterCount : 10)}\n" +
                $"Speed={(driver != null ? driver.Speed : 0f):F3}\n" +
                $"Speed01={(driver != null ? driver.Speed01 : 0f):F3}\n" +
                $"VerticalSpeed={(driver != null ? driver.VerticalSpeed : 0f):F3}\n" +
                $"Grounded={(driver != null && driver.Grounded)}\n" +
                $"JumpTriggerCount={(driver != null ? driver.JumpTriggerCount : 0)}\n" +
                $"LandTriggerCount={(driver != null ? driver.LandTriggerCount : 0)}\n" +
                "LocomotionRootMotion=False\n" +
                "GameplayMotorRemainsAuthoritative=True\n" +
                "ExternalLicensedClipsAllowed=True\n" +
                "AddsNewGameplayInputAction=False\n" +
                "NetworkAuthorityEnabled=False";

            Debug.Log(
                report,
                driver != null
                    ? driver
                    : clipSet);

            EditorUtility.DisplayDialog(
                "M55.1 Diagnose",
                report,
                "Tamam");
        }

        private static FigureAnimationClipSet
            GetOrCreateClipSet()
        {
            EnsureFolder(
                DataFolder);

            FigureAnimationClipSet existing =
                AssetDatabase.LoadAssetAtPath<
                    FigureAnimationClipSet>(
                        ClipSetPath);

            if (existing != null)
            {
                return existing;
            }

            FigureAnimationClipSet clipSet =
                ScriptableObject.CreateInstance<
                    FigureAnimationClipSet>();

            AssetDatabase.CreateAsset(
                clipSet,
                ClipSetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return clipSet;
        }

        private static void AddParameters(
            AnimatorController controller,
            FigureAnimationProfile profile)
        {
            controller.AddParameter(
                profile.SpeedParameter,
                AnimatorControllerParameterType.Float);

            controller.AddParameter(
                profile.Speed01Parameter,
                AnimatorControllerParameterType.Float);

            controller.AddParameter(
                profile.ForwardSpeedParameter,
                AnimatorControllerParameterType.Float);

            controller.AddParameter(
                profile.SideSpeedParameter,
                AnimatorControllerParameterType.Float);

            controller.AddParameter(
                profile.VerticalSpeedParameter,
                AnimatorControllerParameterType.Float);

            controller.AddParameter(
                profile.GroundedParameter,
                AnimatorControllerParameterType.Bool);

            controller.AddParameter(
                profile.MovingParameter,
                AnimatorControllerParameterType.Bool);

            controller.AddParameter(
                profile.SprintingParameter,
                AnimatorControllerParameterType.Bool);

            controller.AddParameter(
                profile.JumpTriggerParameter,
                AnimatorControllerParameterType.Trigger);

            controller.AddParameter(
                profile.LandTriggerParameter,
                AnimatorControllerParameterType.Trigger);
        }

        private static AnimatorState CreateLocomotionState(
            AnimatorController controller,
            AnimatorStateMachine stateMachine,
            FigureAnimationClipSet clipSet,
            FigureAnimationProfile profile)
        {
            AnimatorState locomotion =
                stateMachine.AddState(
                    "Locomotion",
                    new Vector3(
                        60f,
                        -40f,
                        0f));

            BlendTree blendTree =
                new BlendTree
                {
                    name =
                        "BT_Figure_Locomotion",
                    blendType =
                        BlendTreeType.Simple1D,
                    blendParameter =
                        profile.Speed01Parameter,
                    useAutomaticThresholds =
                        false
                };

            blendTree.AddChild(
                clipSet.Idle,
                0f);

            blendTree.AddChild(
                clipSet.Walk,
                0.42f);

            blendTree.AddChild(
                clipSet.RunSprint,
                1f);

            AssetDatabase.AddObjectToAsset(
                blendTree,
                controller);

            locomotion.motion =
                blendTree;

            return locomotion;
        }

        private static AnimatorState CreateState(
            AnimatorStateMachine stateMachine,
            string name,
            AnimationClip clip,
            Vector3 position)
        {
            AnimatorState state =
                stateMachine.AddState(
                    name,
                    position);

            state.motion =
                clip;

            return state;
        }

        private static void ConfigureTransitions(
            AnimatorStateMachine stateMachine,
            AnimatorState locomotion,
            AnimatorState jump,
            AnimatorState fall,
            AnimatorState land,
            FigureAnimationProfile profile)
        {
            AnimatorStateTransition locomotionToJump =
                locomotion.AddTransition(
                    jump);

            ConfigureImmediateTransition(
                locomotionToJump,
                0.05f);

            locomotionToJump.AddCondition(
                AnimatorConditionMode.If,
                0f,
                profile.JumpTriggerParameter);

            AnimatorStateTransition locomotionToFall =
                locomotion.AddTransition(
                    fall);

            ConfigureImmediateTransition(
                locomotionToFall,
                0.08f);

            locomotionToFall.AddCondition(
                AnimatorConditionMode.IfNot,
                0f,
                profile.GroundedParameter);

            locomotionToFall.AddCondition(
                AnimatorConditionMode.Less,
                -0.05f,
                profile.VerticalSpeedParameter);

            AnimatorStateTransition jumpToFall =
                jump.AddTransition(
                    fall);

            ConfigureImmediateTransition(
                jumpToFall,
                0.08f);

            jumpToFall.AddCondition(
                AnimatorConditionMode.Less,
                0.05f,
                profile.VerticalSpeedParameter);

            AnimatorStateTransition jumpToLand =
                jump.AddTransition(
                    land);

            ConfigureImmediateTransition(
                jumpToLand,
                0.05f);

            jumpToLand.AddCondition(
                AnimatorConditionMode.If,
                0f,
                profile.LandTriggerParameter);

            AnimatorStateTransition fallToLand =
                fall.AddTransition(
                    land);

            ConfigureImmediateTransition(
                fallToLand,
                0.05f);

            fallToLand.AddCondition(
                AnimatorConditionMode.If,
                0f,
                profile.LandTriggerParameter);

            AnimatorStateTransition landToLocomotion =
                land.AddTransition(
                    locomotion);

            landToLocomotion.hasExitTime =
                true;

            landToLocomotion.exitTime =
                0.72f;

            landToLocomotion.hasFixedDuration =
                true;

            landToLocomotion.duration =
                0.08f;
        }

        private static void ConfigureImmediateTransition(
            AnimatorStateTransition transition,
            float duration)
        {
            transition.hasExitTime =
                false;

            transition.hasFixedDuration =
                true;

            transition.duration =
                duration;

            transition.canTransitionToSelf =
                false;
        }

        private static void ClearStateMachine(
            AnimatorStateMachine stateMachine)
        {
            ChildAnimatorState[] states =
                stateMachine.states;

            for (int index = states.Length - 1;
                 index >= 0;
                 index--)
            {
                AnimatorState state =
                    states[index].state;

                if (state != null)
                {
                    stateMachine.RemoveState(
                        state);
                }
            }

            ChildAnimatorStateMachine[] childMachines =
                stateMachine.stateMachines;

            for (int index = childMachines.Length - 1;
                 index >= 0;
                 index--)
            {
                AnimatorStateMachine child =
                    childMachines[index].stateMachine;

                if (child != null)
                {
                    stateMachine.RemoveStateMachine(
                        child);
                }
            }
        }

        private static void DeleteControllerIfExists()
        {
            AnimatorController existing =
                AssetDatabase.LoadAssetAtPath<
                    AnimatorController>(
                        ControllerPath);

            if (existing == null)
            {
                return;
            }

            if (
                !AssetDatabase.DeleteAsset(
                    ControllerPath)
            )
            {
                throw new InvalidOperationException(
                    "Eski AC_Figure_Production.controller silinemedi.");
            }
        }

        private static void EnsureFolder(
            string folderPath)
        {
            if (
                AssetDatabase.IsValidFolder(
                    folderPath)
            )
            {
                return;
            }

            int separator =
                folderPath.LastIndexOf(
                    '/');

            if (
                separator <= 0 ||
                separator >=
                    folderPath.Length - 1
            )
            {
                throw new InvalidOperationException(
                    $"Geçersiz Unity folder path: {folderPath}");
            }

            string parent =
                folderPath.Substring(
                    0,
                    separator);

            string folderName =
                folderPath.Substring(
                    separator + 1);

            if (
                !AssetDatabase.IsValidFolder(
                    parent)
            )
            {
                EnsureFolder(
                    parent);
            }

            AssetDatabase.CreateFolder(
                parent,
                folderName);
        }

        private static T FindRequired<T>()
            where T : Component
        {
            T found =
                FindOptional<T>();

            if (found == null)
            {
                throw new InvalidOperationException(
                    $"{typeof(T).Name} sahnede bulunamadı.");
            }

            return found;
        }

        private static T FindOptional<T>()
            where T : Component
        {
            T[] objects =
                UnityEngine.Object.FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (T candidate in objects)
            {
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
