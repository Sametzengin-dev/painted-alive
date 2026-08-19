using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Figures.Animation
{
    [DefaultExecutionOrder(160)]
    [DisallowMultipleComponent]
    public sealed class FigureAnimationDriver :
        MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField]
        private FigureMotor figureMotor;

        [SerializeField]
        private CharacterController characterController;

        [SerializeField]
        private Animator animator;

        [SerializeField]
        private FigureAnimationProfile profile;

        [Header("Runtime Read Only")]
        [SerializeField]
        private bool animatorBound;

        [SerializeField]
        private bool animatorControllerAssigned;

        [SerializeField]
        private bool humanoidAvatarValid;

        [SerializeField]
        private bool parametersReady;

        [SerializeField]
        private int missingRequiredParameterCount;

        [SerializeField]
        private float speed;

        [SerializeField]
        private float speed01;

        [SerializeField]
        private float forwardSpeed;

        [SerializeField]
        private float sideSpeed;

        [SerializeField]
        private float verticalSpeed;

        [SerializeField]
        private bool grounded;

        [SerializeField]
        private bool moving;

        [SerializeField]
        private bool sprinting;

        [SerializeField]
        private int jumpTriggerCount;

        [SerializeField]
        private int landTriggerCount;

        [SerializeField]
        private string lastBindingState =
            "Waiting for character Animator.";

        private readonly HashSet<int> availableFloatParameters =
            new HashSet<int>();

        private readonly HashSet<int> availableBoolParameters =
            new HashSet<int>();

        private readonly HashSet<int> availableTriggerParameters =
            new HashSet<int>();

        private RuntimeAnimatorController cachedAnimatorController;
        private int cachedAnimatorParameterCount = -1;

        private bool previousGrounded;
        private float previousVerticalSpeed;
        private bool previousStateInitialized;
        private float nextAutoBindTime;

        public FigureMotor FigureMotor => figureMotor;
        public Animator Animator => animator;
        public FigureAnimationProfile Profile => profile;

        public bool AnimatorBound => animatorBound;
        public bool AnimatorControllerAssigned =>
            animatorControllerAssigned;
        public bool HumanoidAvatarValid =>
            humanoidAvatarValid;
        public bool ParametersReady =>
            parametersReady;
        public int MissingRequiredParameterCount =>
            missingRequiredParameterCount;

        public float Speed => speed;
        public float Speed01 => speed01;
        public float ForwardSpeed => forwardSpeed;
        public float SideSpeed => sideSpeed;
        public float VerticalSpeed => verticalSpeed;
        public bool Grounded => grounded;
        public bool Moving => moving;
        public bool Sprinting => sprinting;
        public int JumpTriggerCount => jumpTriggerCount;
        public int LandTriggerCount => landTriggerCount;
        public string LastBindingState => lastBindingState;

        public bool GameplayMotorRemainsAuthoritative => true;
        public bool RootMotionAuthorityDisabled => true;
        public bool HumanoidRetargetingTarget => true;
        public bool ExternalAnimationClipsSupported => true;
        public bool VisualAnimatorMayBeReplacedWithoutGameplayChanges => true;
        public bool AddsNewGameplayInputAction => false;
        public bool NetworkAuthorityEnabled => false;
        public bool LocalAnimationPresentationOnly => true;

        public void Configure(
            FigureMotor configuredFigureMotor,
            CharacterController configuredCharacterController,
            Animator configuredAnimator,
            FigureAnimationProfile configuredProfile)
        {
            figureMotor =
                configuredFigureMotor;

            characterController =
                configuredCharacterController;

            animator =
                configuredAnimator;

            profile =
                configuredProfile;

            RefreshBindingState(
                forceParameterRefresh: true);
        }

        private void Awake()
        {
            if (figureMotor == null)
            {
                figureMotor =
                    GetComponent<
                        FigureMotor>();
            }

            if (characterController == null)
            {
                characterController =
                    GetComponent<
                        CharacterController>();
            }

            if (animator == null)
            {
                TryAutoBindAnimator();
            }

            RefreshBindingState(
                forceParameterRefresh: true);
        }

        private void OnEnable()
        {
            nextAutoBindTime = 0f;

            RefreshBindingState(
                forceParameterRefresh: true);
        }

        private void LateUpdate()
        {
            if (
                figureMotor == null ||
                characterController == null ||
                profile == null
            )
            {
                return;
            }

            if (
                animator == null &&
                Time.unscaledTime >=
                    nextAutoBindTime
            )
            {
                nextAutoBindTime =
                    Time.unscaledTime +
                    0.75f;

                TryAutoBindAnimator();

                RefreshBindingState(
                    forceParameterRefresh: true);
            }

            Vector3 worldVelocity =
                figureMotor.Velocity;

            Vector3 planarVelocity =
                Vector3.ProjectOnPlane(
                    worldVelocity,
                    Vector3.up);

            Vector3 localVelocity =
                transform.InverseTransformDirection(
                    planarVelocity);

            speed =
                planarVelocity.magnitude;

            speed01 =
                Mathf.Clamp01(
                    speed /
                    Mathf.Max(
                        0.1f,
                        profile.LocomotionReferenceSpeed));

            forwardSpeed =
                localVelocity.z;

            sideSpeed =
                localVelocity.x;

            verticalSpeed =
                worldVelocity.y;

            grounded =
                characterController.isGrounded;

            moving =
                speed >=
                profile.MovingSpeedThreshold;

            sprinting =
                moving &&
                speed >=
                    profile.SprintSpeedThreshold;

            RefreshBindingState(
                forceParameterRefresh: false);

            if (animator != null)
            {
                if (animator.applyRootMotion)
                {
                    animator.applyRootMotion =
                        false;
                }

                WriteContinuousParameters();
                WriteTransitionTriggers();
            }

            previousGrounded =
                grounded;

            previousVerticalSpeed =
                verticalSpeed;

            previousStateInitialized =
                true;
        }

        public bool TryAutoBindAnimator()
        {
            Animator[] animators =
                GetComponentsInChildren<
                    Animator>(
                        true);

            if (
                animators == null ||
                animators.Length == 0
            )
            {
                animator = null;

                lastBindingState =
                    "Waiting for character Animator.";

                return false;
            }

            Animator best =
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

                Avatar avatar =
                    candidate.avatar;

                if (
                    avatar != null &&
                    avatar.isValid &&
                    avatar.isHuman
                )
                {
                    best =
                        candidate;

                    break;
                }

                if (best == null)
                {
                    best =
                        candidate;
                }
            }

            animator =
                best;

            if (animator != null)
            {
                animator.applyRootMotion =
                    false;

                lastBindingState =
                    $"Animator bound: {animator.name}.";

                return true;
            }

            lastBindingState =
                "Waiting for character Animator.";

            return false;
        }

        private void RefreshBindingState(
            bool forceParameterRefresh)
        {
            animatorBound =
                animator != null;

            animatorControllerAssigned =
                animator != null &&
                animator.runtimeAnimatorController !=
                    null;

            Avatar avatar =
                animator != null
                    ? animator.avatar
                    : null;

            humanoidAvatarValid =
                avatar != null &&
                avatar.isValid &&
                avatar.isHuman;

            if (animator == null)
            {
                parametersReady = false;
                missingRequiredParameterCount = 10;
                ClearParameterCache();

                return;
            }

            RuntimeAnimatorController currentController =
                animator.runtimeAnimatorController;

            int currentParameterCount =
                animator.parameters != null
                    ? animator.parameters.Length
                    : 0;

            if (
                forceParameterRefresh ||
                currentController !=
                    cachedAnimatorController ||
                currentParameterCount !=
                    cachedAnimatorParameterCount
            )
            {
                cachedAnimatorController =
                    currentController;

                cachedAnimatorParameterCount =
                    currentParameterCount;

                RebuildParameterCache();
            }
        }

        private void RebuildParameterCache()
        {
            ClearParameterCache();

            if (
                animator == null ||
                profile == null
            )
            {
                missingRequiredParameterCount =
                    10;

                parametersReady =
                    false;

                return;
            }

            AnimatorControllerParameter[] parameters =
                animator.parameters;

            for (int index = 0;
                 index < parameters.Length;
                 index++)
            {
                AnimatorControllerParameter parameter =
                    parameters[index];

                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Float:
                        availableFloatParameters.Add(
                            parameter.nameHash);
                        break;

                    case AnimatorControllerParameterType.Bool:
                        availableBoolParameters.Add(
                            parameter.nameHash);
                        break;

                    case AnimatorControllerParameterType.Trigger:
                        availableTriggerParameters.Add(
                            parameter.nameHash);
                        break;
                }
            }

            int missing = 0;

            missing +=
                CountMissing(
                    profile.SpeedParameter,
                    availableFloatParameters);

            missing +=
                CountMissing(
                    profile.Speed01Parameter,
                    availableFloatParameters);

            missing +=
                CountMissing(
                    profile.ForwardSpeedParameter,
                    availableFloatParameters);

            missing +=
                CountMissing(
                    profile.SideSpeedParameter,
                    availableFloatParameters);

            missing +=
                CountMissing(
                    profile.VerticalSpeedParameter,
                    availableFloatParameters);

            missing +=
                CountMissing(
                    profile.GroundedParameter,
                    availableBoolParameters);

            missing +=
                CountMissing(
                    profile.MovingParameter,
                    availableBoolParameters);

            missing +=
                CountMissing(
                    profile.SprintingParameter,
                    availableBoolParameters);

            missing +=
                CountMissing(
                    profile.JumpTriggerParameter,
                    availableTriggerParameters);

            missing +=
                CountMissing(
                    profile.LandTriggerParameter,
                    availableTriggerParameters);

            missingRequiredParameterCount =
                missing;

            parametersReady =
                missing == 0;

            if (!animatorControllerAssigned)
            {
                lastBindingState =
                    humanoidAvatarValid
                        ? "Humanoid Animator found; Animator Controller waiting."
                        : "Animator found; valid Humanoid Avatar and Controller waiting.";
            }
            else if (!humanoidAvatarValid)
            {
                lastBindingState =
                    "Animator Controller found; Humanoid Avatar invalid/missing.";
            }
            else if (!parametersReady)
            {
                lastBindingState =
                    $"Animator ready; {missing} required parameters still missing.";
            }
            else
            {
                lastBindingState =
                    "Production locomotion animation contract ready.";
            }
        }

        private void ClearParameterCache()
        {
            availableFloatParameters.Clear();
            availableBoolParameters.Clear();
            availableTriggerParameters.Clear();
        }

        private void WriteContinuousParameters()
        {
            float damp =
                profile.FloatParameterDampTime;

            float deltaTime =
                Mathf.Max(
                    0f,
                    Time.deltaTime);

            SetFloatIfAvailable(
                profile.SpeedParameter,
                speed,
                damp,
                deltaTime);

            SetFloatIfAvailable(
                profile.Speed01Parameter,
                speed01,
                damp,
                deltaTime);

            SetFloatIfAvailable(
                profile.ForwardSpeedParameter,
                forwardSpeed,
                damp,
                deltaTime);

            SetFloatIfAvailable(
                profile.SideSpeedParameter,
                sideSpeed,
                damp,
                deltaTime);

            SetFloatIfAvailable(
                profile.VerticalSpeedParameter,
                verticalSpeed,
                damp,
                deltaTime);

            SetBoolIfAvailable(
                profile.GroundedParameter,
                grounded);

            SetBoolIfAvailable(
                profile.MovingParameter,
                moving);

            SetBoolIfAvailable(
                profile.SprintingParameter,
                sprinting);
        }

        private void WriteTransitionTriggers()
        {
            if (!previousStateInitialized)
            {
                return;
            }

            bool jumped =
                previousGrounded &&
                !grounded &&
                verticalSpeed >=
                    profile.JumpVerticalThreshold;

            bool landed =
                !previousGrounded &&
                grounded &&
                previousVerticalSpeed <=
                    -profile.LandingMinimumFallSpeed;

            if (jumped)
            {
                if (
                    SetTriggerIfAvailable(
                        profile.JumpTriggerParameter)
                )
                {
                    jumpTriggerCount++;
                }
            }

            if (landed)
            {
                if (
                    SetTriggerIfAvailable(
                        profile.LandTriggerParameter)
                )
                {
                    landTriggerCount++;
                }
            }
        }

        private void SetFloatIfAvailable(
            string parameterName,
            float value,
            float dampTime,
            float deltaTime)
        {
            if (
                animator == null ||
                string.IsNullOrWhiteSpace(
                    parameterName)
            )
            {
                return;
            }

            int hash =
                Animator.StringToHash(
                    parameterName);

            if (
                !availableFloatParameters.Contains(
                    hash)
            )
            {
                return;
            }

            animator.SetFloat(
                hash,
                value,
                dampTime,
                deltaTime);
        }

        private void SetBoolIfAvailable(
            string parameterName,
            bool value)
        {
            if (
                animator == null ||
                string.IsNullOrWhiteSpace(
                    parameterName)
            )
            {
                return;
            }

            int hash =
                Animator.StringToHash(
                    parameterName);

            if (
                !availableBoolParameters.Contains(
                    hash)
            )
            {
                return;
            }

            animator.SetBool(
                hash,
                value);
        }

        private bool SetTriggerIfAvailable(
            string parameterName)
        {
            if (
                animator == null ||
                string.IsNullOrWhiteSpace(
                    parameterName)
            )
            {
                return false;
            }

            int hash =
                Animator.StringToHash(
                    parameterName);

            if (
                !availableTriggerParameters.Contains(
                    hash)
            )
            {
                return false;
            }

            animator.SetTrigger(
                hash);

            return true;
        }

        private static int CountMissing(
            string parameterName,
            HashSet<int> available)
        {
            if (
                string.IsNullOrWhiteSpace(
                    parameterName)
            )
            {
                return 1;
            }

            int hash =
                Animator.StringToHash(
                    parameterName);

            return
                available.Contains(
                    hash)
                    ? 0
                    : 1;
        }
    }
}
