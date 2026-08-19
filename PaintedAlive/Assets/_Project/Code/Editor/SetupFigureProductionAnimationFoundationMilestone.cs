#if UNITY_EDITOR
using System;
using PaintedAlive.Figures;
using PaintedAlive.Figures.Animation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.Editor
{
    public static class SetupFigureProductionAnimationFoundationMilestone
    {
        private const string ProfileFolder =
            "Assets/_Project/Data/Figures/Animation";

        private const string ProfilePath =
            ProfileFolder +
            "/DA_FigureAnimation_Production.asset";

        private const string CharacterVisualRootName =
            "CharacterVisualRoot";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "55.0 - Apply or Rebind Figure Animation Foundation")]
        public static void ApplyOrRebind()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M55.0 setup/rebind Play Mode dışında çalıştırılmalıdır.");
                }

                FigureMotor figureMotor =
                    FindRequired<
                        FigureMotor>();

                CharacterController characterController =
                    figureMotor.GetComponent<
                        CharacterController>();

                if (characterController == null)
                {
                    throw new InvalidOperationException(
                        "FigureMotor kökünde CharacterController bulunamadı.");
                }

                Transform visualRoot =
                    GetOrCreateCharacterVisualRoot(
                        figureMotor.transform);

                FigureAnimationProfile profile =
                    GetOrCreateProfile();

                Animator animator =
                    FindBestAnimator(
                        visualRoot,
                        figureMotor.transform);

                FigureAnimationDriver driver =
                    GetOrAdd<
                        FigureAnimationDriver>(
                            figureMotor.gameObject);

                driver.Configure(
                    figureMotor,
                    characterController,
                    animator,
                    profile);

                if (animator != null)
                {
                    animator.applyRootMotion =
                        false;

                    EditorUtility.SetDirty(
                        animator);
                }

                EditorUtility.SetDirty(
                    driver);

                EditorUtility.SetDirty(
                    figureMotor);

                EditorSceneManager.MarkSceneDirty(
                    figureMotor.gameObject.scene);

                EditorSceneManager.SaveOpenScenes();

                string animatorState =
                    animator != null
                        ? animator.name
                        : "WAITING_FOR_CHARACTER";

                Debug.Log(
                    "[M55.0 Setup/Rebind] Figure Production Animation Foundation ready.\n" +
                    $"FigureMotor={GetHierarchyPath(figureMotor.transform)}\n" +
                    $"CharacterVisualRoot={GetHierarchyPath(visualRoot)}\n" +
                    $"Animator={animatorState}\n" +
                    $"AnimationProfile={ProfilePath}\n" +
                    "ModelRigTarget=Humanoid\n" +
                    "CharacterModelCreatedByProjectOwner=True\n" +
                    "CharacterRigCreatedByProjectOwner=True\n" +
                    "ExternalLicensedAnimationRetargetingAllowed=True\n" +
                    "GameplayMotorRemainsAuthoritative=True\n" +
                    "RootMotionAuthorityDisabled=True\n" +
                    "VisualAnimatorMayBeReplacedWithoutGameplayChanges=True\n" +
                    "AddsNewGameplayInputAction=False\n" +
                    "NetworkAuthorityEnabled=False",
                    driver);

                EditorUtility.DisplayDialog(
                    "M55.0 Hazır",
                    animator != null
                        ? "Figure Animator bulundu ve motor→Animator production sözleşmesine bağlandı."
                        : "Animation foundation kuruldu. Karakter/Animator henüz yoksa sorun değil; " +
                          "karakteri ekledikten sonra aynı M55.0 Rebind menüsünü tekrar çalıştır.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                EditorUtility.DisplayDialog(
                    "M55.0 Setup Hatası",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "55.0 - Diagnose Figure Animation Foundation")]
        public static void Diagnose()
        {
            FigureMotor figureMotor =
                FindOptional<
                    FigureMotor>();

            FigureAnimationDriver driver =
                FindOptional<
                    FigureAnimationDriver>();

            Animator animator =
                driver != null
                    ? driver.Animator
                    : figureMotor != null
                        ? figureMotor.GetComponentInChildren<
                            Animator>(
                                true)
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
                "[M55.0 Diagnose]\n" +
                $"FigureMotor={(figureMotor != null ? "OK" : "MISSING")}\n" +
                $"AnimationDriver={(driver != null ? "OK" : "MISSING")}\n" +
                $"Animator={(animator != null ? animator.name : "WAITING_FOR_CHARACTER")}\n" +
                $"AnimatorControllerAssigned={(animator != null && animator.runtimeAnimatorController != null)}\n" +
                $"HumanoidAvatarValid={humanoidValid}\n" +
                $"ApplyRootMotion={(animator != null && animator.applyRootMotion)}\n" +
                $"ParametersReady={(driver != null && driver.ParametersReady)}\n" +
                $"MissingRequiredParameterCount={(driver != null ? driver.MissingRequiredParameterCount : 10)}\n" +
                $"Speed={(driver != null ? driver.Speed : 0f):F3}\n" +
                $"Speed01={(driver != null ? driver.Speed01 : 0f):F3}\n" +
                $"ForwardSpeed={(driver != null ? driver.ForwardSpeed : 0f):F3}\n" +
                $"SideSpeed={(driver != null ? driver.SideSpeed : 0f):F3}\n" +
                $"VerticalSpeed={(driver != null ? driver.VerticalSpeed : 0f):F3}\n" +
                $"Grounded={(driver != null && driver.Grounded)}\n" +
                $"Moving={(driver != null && driver.Moving)}\n" +
                $"Sprinting={(driver != null && driver.Sprinting)}\n" +
                $"JumpTriggerCount={(driver != null ? driver.JumpTriggerCount : 0)}\n" +
                $"LandTriggerCount={(driver != null ? driver.LandTriggerCount : 0)}\n" +
                $"LastBindingState={(driver != null ? driver.LastBindingState : "N/A")}\n" +
                "ModelRigTarget=Humanoid\n" +
                "ExternalLicensedAnimationRetargetingAllowed=True\n" +
                "GameplayMotorRemainsAuthoritative=True\n" +
                "RootMotionAuthorityDisabled=True\n" +
                "VisualAnimatorMayBeReplacedWithoutGameplayChanges=True\n" +
                "AddsNewGameplayInputAction=False\n" +
                "NetworkAuthorityEnabled=False";

            Debug.Log(
                report,
                driver != null
                    ? driver
                    : figureMotor);

            EditorUtility.DisplayDialog(
                "M55.0 Diagnose",
                report,
                "Tamam");
        }

        private static FigureAnimationProfile
            GetOrCreateProfile()
        {
            EnsureFolder(
                "Assets/_Project/Data");

            EnsureFolder(
                "Assets/_Project/Data/Figures");

            EnsureFolder(
                ProfileFolder);

            FigureAnimationProfile existing =
                AssetDatabase.LoadAssetAtPath<
                    FigureAnimationProfile>(
                        ProfilePath);

            if (existing != null)
            {
                return existing;
            }

            FigureAnimationProfile profile =
                ScriptableObject.CreateInstance<
                    FigureAnimationProfile>();

            AssetDatabase.CreateAsset(
                profile,
                ProfilePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return profile;
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

            string parent =
                folderPath.Substring(
                    0,
                    folderPath.LastIndexOf(
                        '/'));

            string folderName =
                folderPath.Substring(
                    folderPath.LastIndexOf(
                        '/') +
                    1);

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

        private static Transform
            GetOrCreateCharacterVisualRoot(
                Transform figureRoot)
        {
            Transform existing =
                figureRoot.Find(
                    CharacterVisualRootName);

            if (existing != null)
            {
                return existing;
            }

            GameObject visualRoot =
                new GameObject(
                    CharacterVisualRootName);

            Undo.RegisterCreatedObjectUndo(
                visualRoot,
                "Create Figure CharacterVisualRoot");

            visualRoot.transform.SetParent(
                figureRoot,
                false);

            visualRoot.transform.localPosition =
                Vector3.zero;

            visualRoot.transform.localRotation =
                Quaternion.identity;

            visualRoot.transform.localScale =
                Vector3.one;

            return visualRoot.transform;
        }

        private static Animator FindBestAnimator(
            Transform preferredRoot,
            Transform figureRoot)
        {
            Animator best =
                FindBestAnimatorIn(
                    preferredRoot);

            if (best != null)
            {
                return best;
            }

            return
                FindBestAnimatorIn(
                    figureRoot);
        }

        private static Animator FindBestAnimatorIn(
            Transform root)
        {
            if (root == null)
            {
                return null;
            }

            Animator[] animators =
                root.GetComponentsInChildren<
                    Animator>(
                        true);

            Animator first =
                null;

            for (int index = 0;
                 index < animators.Length;
                 index++)
            {
                Animator candidate =
                    animators[index];

                if (candidate == null)
                {
                    continue;
                }

                if (first == null)
                {
                    first =
                        candidate;
                }

                Avatar avatar =
                    candidate.avatar;

                if (
                    avatar != null &&
                    avatar.isValid &&
                    avatar.isHuman
                )
                {
                    return candidate;
                }
            }

            return first;
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
