using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    public enum PigmentRouteEffect
    {
        None = 0,
        OpenRoute = 1,
        BlockRoute = 2,
        Slippery = 3,
        SpeedBoost = 4,
        Slow = 5,
        RevealPath = 6
    }

    /// <summary>
    /// Explicit policy holder. The Living Gallery source does not author any
    /// target/effect pair, so M55.8.3 intentionally resolves this gap as
    /// NO_EFFECT rather than inferring gameplay from pigment colour/proximity.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PigmentRouteModifier : MonoBehaviour
    {
        [SerializeField] private string bindingId;
        [SerializeField] private PigmentRouteEffect effect = PigmentRouteEffect.None;
        [SerializeField] private Transform affectedTarget;
        [SerializeField] private bool explicitNoEffectPolicy = true;
        [SerializeField, TextArea(1, 4)] private string policyReason;

        public string BindingId => bindingId;
        public PigmentRouteEffect Effect => effect;
        public Transform AffectedTarget => affectedTarget;
        public bool HasResolvedPolicy =>
            explicitNoEffectPolicy && effect == PigmentRouteEffect.None &&
            affectedTarget == null;

        public void ConfigureExplicitNoEffectPolicy(string id, string reason)
        {
            bindingId = id ?? string.Empty;
            effect = PigmentRouteEffect.None;
            affectedTarget = null;
            explicitNoEffectPolicy = true;
            policyReason = reason ?? string.Empty;
        }

        public void ConfigureEffect(
            string id,
            PigmentRouteEffect routeEffect,
            Transform target)
        {
            bindingId = id ?? string.Empty;
            effect = routeEffect;
            affectedTarget = target;
            explicitNoEffectPolicy = routeEffect == PigmentRouteEffect.None &&
                                     target == null;
            policyReason = string.Empty;
        }
    }
}
