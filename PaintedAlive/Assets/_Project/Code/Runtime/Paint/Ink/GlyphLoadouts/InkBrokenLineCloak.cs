using PaintedAlive.Paint.Ink.Possession;
using UnityEngine;

namespace PaintedAlive.Paint.Ink.GlyphLoadouts
{
    [DefaultExecutionOrder(12000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InkCreatureRuntime))]
    public sealed class InkBrokenLineCloak : MonoBehaviour
    {
        [SerializeField]
        private InkCreatureRuntime creature;

        [SerializeField, Min(0.2f)]
        private float cloakDuration = 0.7f;

        [SerializeField, Min(0.5f)]
        private float cloakCooldown = 4.5f;

        [SerializeField, Min(1f)]
        private float minimumTargetDistance = 6f;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private bool cloaked;

        [SerializeField]
        private float nextCloakTime;

        private Renderer[] cachedRenderers;
        private bool[] rendererStates;
        private InkPossessionController cachedPossession;
        private float cloakEndsAt;

        public bool IsCloaked => cloaked;

        private void Awake()
        {
            creature ??= GetComponent<InkCreatureRuntime>();
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            rendererStates = new bool[cachedRenderers.Length];
            nextCloakTime = Time.time + cloakCooldown;
        }

        private void OnDisable()
        {
            SetCloaked(false);
        }

        private void Update()
        {
            if (creature == null ||
                !creature.IsInitialized ||
                !creature.HasGlyph(InkGlyphType.BrokenLine))
            {
                SetCloaked(false);
                return;
            }

            if (cloaked)
            {
                if (Time.time >= cloakEndsAt ||
                    IsThreatClose() ||
                    IsPossessed())
                {
                    SetCloaked(false);
                    nextCloakTime = Time.time + cloakCooldown;
                }

                return;
            }

            if (Time.time >= nextCloakTime &&
                !IsThreatClose() &&
                !IsPossessed() &&
                !creature.IsFixed &&
                !creature.IsPinned)
            {
                SetCloaked(true);
                cloakEndsAt = Time.time + cloakDuration;
            }
        }

        private bool IsThreatClose()
        {
            if (creature.CurrentTarget == null)
            {
                return false;
            }

            return Vector3.SqrMagnitude(
                    creature.CurrentTarget.transform.position -
                    creature.transform.position) <
                minimumTargetDistance * minimumTargetDistance;
        }

        private bool IsPossessed()
        {
            if (cachedPossession == null)
            {
                cachedPossession =
                    Object.FindFirstObjectByType<InkPossessionController>(
                        FindObjectsInactive.Include);
            }

            return cachedPossession != null &&
                cachedPossession.IsPossessing &&
                cachedPossession.PossessedCreature == creature;
        }

        private void SetCloaked(bool active)
        {
            if (cloaked == active)
            {
                return;
            }

            cloaked = active;

            if (cachedRenderers == null)
            {
                cachedRenderers = GetComponentsInChildren<Renderer>(true);
                rendererStates = new bool[cachedRenderers.Length];
            }

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer target = cachedRenderers[i];

                if (target != null)
                {
                    if (active)
                    {
                        rendererStates[i] = target.enabled;
                        target.enabled = false;
                    }
                    else
                    {
                        target.enabled =
                            rendererStates != null &&
                            i < rendererStates.Length &&
                            rendererStates[i];
                    }
                }
            }

            if (!active && creature != null)
            {
                creature.RefreshGlyphHitZones();
            }
        }
    }
}
