using UnityEngine;

namespace PaintedAlive.Animation
{
    /// <summary>
    /// Production Mixamo locomotion clip set introduced by M55.1.1.
    /// This is presentation data only; FigureMotor remains gameplay authority.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DA_FigureMixamoClips_Production",
        menuName = "Painted Alive/Animation/Figure Mixamo Clip Set (M55.1.1)")]
    public sealed class FigureMixamoClipSet_M55_1_1 : ScriptableObject
    {
        [Header("Ground Locomotion")]
        public AnimationClip Idle;
        public AnimationClip Walk;
        public AnimationClip Run;
        public AnimationClip Sprint;

        [Header("Airborne")]
        public AnimationClip JumpStart;
        public AnimationClip FallLoop;
        public AnimationClip Land;

        public bool HasAllRequiredClips =>
            Idle != null &&
            Walk != null &&
            Run != null &&
            Sprint != null &&
            JumpStart != null &&
            FallLoop != null &&
            Land != null;
    }
}
