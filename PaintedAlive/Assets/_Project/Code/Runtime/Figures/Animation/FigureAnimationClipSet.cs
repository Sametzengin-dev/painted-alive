using UnityEngine;

namespace PaintedAlive.Figures.Animation
{
    [CreateAssetMenu(
        fileName = "FigureAnimationClipSet",
        menuName = "Painted Alive/Figures/Animation Clip Set")]
    public sealed class FigureAnimationClipSet :
        ScriptableObject
    {
        [Header("Required Locomotion")]
        [SerializeField]
        private AnimationClip idle;

        [SerializeField]
        private AnimationClip walk;

        [SerializeField]
        private AnimationClip runSprint;

        [SerializeField]
        private AnimationClip jumpStart;

        [SerializeField]
        private AnimationClip fallLoop;

        [SerializeField]
        private AnimationClip land;

        [Header("Optional Future")]
        [SerializeField]
        private AnimationClip turnLeft;

        [SerializeField]
        private AnimationClip turnRight;

        [SerializeField]
        private AnimationClip stop;

        public AnimationClip Idle => idle;
        public AnimationClip Walk => walk;
        public AnimationClip RunSprint => runSprint;
        public AnimationClip JumpStart => jumpStart;
        public AnimationClip FallLoop => fallLoop;
        public AnimationClip Land => land;

        public AnimationClip TurnLeft => turnLeft;
        public AnimationClip TurnRight => turnRight;
        public AnimationClip Stop => stop;

        public bool RequiredLocomotionAssigned =>
            idle != null &&
            walk != null &&
            runSprint != null &&
            jumpStart != null &&
            fallLoop != null &&
            land != null;

        public int MissingRequiredClipCount
        {
            get
            {
                int missing = 0;

                if (idle == null)
                {
                    missing++;
                }

                if (walk == null)
                {
                    missing++;
                }

                if (runSprint == null)
                {
                    missing++;
                }

                if (jumpStart == null)
                {
                    missing++;
                }

                if (fallLoop == null)
                {
                    missing++;
                }

                if (land == null)
                {
                    missing++;
                }

                return missing;
            }
        }

        public string MissingRequiredClipNames
        {
            get
            {
                string result = string.Empty;

                AppendMissing(
                    ref result,
                    idle,
                    "Idle");

                AppendMissing(
                    ref result,
                    walk,
                    "Walk");

                AppendMissing(
                    ref result,
                    runSprint,
                    "RunSprint");

                AppendMissing(
                    ref result,
                    jumpStart,
                    "Jump_Start");

                AppendMissing(
                    ref result,
                    fallLoop,
                    "Fall_Loop");

                AppendMissing(
                    ref result,
                    land,
                    "Land");

                return
                    string.IsNullOrWhiteSpace(result)
                        ? "None"
                        : result;
            }
        }

        public bool RootMotionLocomotionRequired => false;
        public bool HumanoidRetargetingRequired => true;
        public bool ExternalLicensedClipsAllowed => true;
        public bool ToolAnimationsRequiredForThisMilestone => false;
        public bool AddsNewGameplayInputAction => false;

        private static void AppendMissing(
            ref string result,
            AnimationClip clip,
            string clipName)
        {
            if (clip != null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(result))
            {
                result += ", ";
            }

            result += clipName;
        }
    }
}
