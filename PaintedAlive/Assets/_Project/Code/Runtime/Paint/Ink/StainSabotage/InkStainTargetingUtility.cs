using System.Collections.Generic;
using PaintedAlive.Paint.Ink.Counterplay;
using PaintedAlive.Paint.Ink.GlyphLoadouts;
using UnityEngine;

namespace PaintedAlive.Paint.Ink.StainSabotage
{
    public enum InkStainTargetMode
    {
        VulnerableForSabotage,
        SabotagedForHijack
    }

    public static class InkStainTargetingUtility
    {
        private const float MaximumViewportOffset = 0.34f;
        private const float FallbackRangePadding = 0.65f;

        public static InkCreatureRuntime FindBestTarget(
            Camera configuredCamera,
            Transform interactionOrigin,
            float aimAssistRadius,
            float interactionRange,
            LayerMask targetMask,
            RaycastHit[] hitBuffer,
            int maximumComplexity,
            InkStainTargetMode mode)
        {
            Camera aimCamera = ResolveAimCamera(configuredCamera);

            if (aimCamera == null ||
                hitBuffer == null ||
                hitBuffer.Length == 0)
            {
                return null;
            }

            Vector3 originPosition = interactionOrigin != null
                ? interactionOrigin.position
                : aimCamera.transform.position;
            InkCreatureRuntime directTarget = FindDirectTarget(
                aimCamera,
                originPosition,
                aimAssistRadius,
                interactionRange,
                targetMask,
                hitBuffer,
                maximumComplexity,
                mode);

            if (directTarget != null)
            {
                return directTarget;
            }

            return FindAssistedTarget(
                aimCamera,
                originPosition,
                interactionRange,
                targetMask,
                hitBuffer,
                maximumComplexity,
                mode);
        }

        public static bool IsValidTarget(
            InkCreatureRuntime candidate,
            int maximumComplexity,
            InkStainTargetMode mode)
        {
            if (candidate == null ||
                !candidate.gameObject.activeInHierarchy ||
                !candidate.IsInitialized ||
                candidate.IsFixed ||
                candidate.IsPinned)
            {
                return false;
            }

            int complexity =
                InkGlyphComplexityUtility.GetCreatureCost(
                    candidate,
                    maximumComplexity + 1);

            if (complexity > maximumComplexity)
            {
                return false;
            }

            InkStainSabotageStatus sabotage =
                candidate.GetComponent<InkStainSabotageStatus>();

            if (mode == InkStainTargetMode.SabotagedForHijack)
            {
                return candidate.HasGlyph(InkGlyphType.Foot) &&
                    !candidate.HasGlyph(InkGlyphType.Shell) &&
                    sabotage != null &&
                    sabotage.IsSabotaged;
            }

            if (sabotage != null && sabotage.IsSabotaged)
            {
                return false;
            }

            InkCommandDisruptionStatus disruption =
                candidate.GetComponent<InkCommandDisruptionStatus>();

            return disruption == null || !disruption.IsDisrupted;
        }

        private static InkCreatureRuntime FindDirectTarget(
            Camera aimCamera,
            Vector3 interactionOrigin,
            float aimAssistRadius,
            float interactionRange,
            LayerMask targetMask,
            RaycastHit[] hitBuffer,
            int maximumComplexity,
            InkStainTargetMode mode)
        {
            Ray aimRay = aimCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f));
            float cameraToFigureDistance = Vector3.Distance(
                aimRay.origin,
                interactionOrigin);
            float castDistance = Mathf.Max(
                interactionRange,
                cameraToFigureDistance + interactionRange + 1f);
            int hitCount = Physics.SphereCastNonAlloc(
                aimRay.origin,
                Mathf.Max(0.02f, aimAssistRadius),
                aimRay.direction,
                hitBuffer,
                castDistance,
                targetMask,
                QueryTriggerInteraction.Collide);
            InkCreatureRuntime best = null;
            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hitBuffer[i];
                InkCreatureRuntime candidate =
                    ResolveCreature(hit.collider);

                if (!IsValidTarget(
                        candidate,
                        maximumComplexity,
                        mode) ||
                    DistanceToCreature(
                        interactionOrigin,
                        candidate) > interactionRange ||
                    hit.distance >= nearestDistance)
                {
                    continue;
                }

                best = candidate;
                nearestDistance = hit.distance;
            }

            return best;
        }

        private static InkCreatureRuntime FindAssistedTarget(
            Camera aimCamera,
            Vector3 interactionOrigin,
            float interactionRange,
            LayerMask visibilityMask,
            RaycastHit[] hitBuffer,
            int maximumComplexity,
            InkStainTargetMode mode)
        {
            InkSystemManager manager = InkSystemManager.ActiveInstance;
            IReadOnlyList<InkCreatureRuntime> creatures =
                manager != null ? manager.ActiveCreatures : null;

            if (creatures == null || creatures.Count == 0)
            {
                creatures =
                    Object.FindObjectsByType<InkCreatureRuntime>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None);
            }

            InkCreatureRuntime best = null;
            float bestScore = float.PositiveInfinity;

            for (int i = 0; i < creatures.Count; i++)
            {
                InkCreatureRuntime candidate = creatures[i];

                if (!IsValidTarget(
                        candidate,
                        maximumComplexity,
                        mode))
                {
                    continue;
                }

                Vector3 aimPoint = ResolveAimPoint(candidate);
                float distance = DistanceToCreature(
                    interactionOrigin,
                    candidate);

                if (distance >
                    interactionRange + FallbackRangePadding)
                {
                    continue;
                }

                Vector3 viewport =
                    aimCamera.WorldToViewportPoint(aimPoint);

                if (viewport.z <= 0f)
                {
                    continue;
                }

                float viewportOffset = Vector2.Distance(
                    new Vector2(viewport.x, viewport.y),
                    new Vector2(0.5f, 0.5f));

                if (viewportOffset > MaximumViewportOffset ||
                    !HasLineOfSight(
                        aimCamera,
                        candidate,
                        aimPoint,
                        visibilityMask,
                        hitBuffer))
                {
                    continue;
                }

                float score =
                    viewportOffset * 4f +
                    distance /
                    Mathf.Max(0.1f, interactionRange) *
                    0.2f;

                if (score >= bestScore)
                {
                    continue;
                }

                best = candidate;
                bestScore = score;
            }

            return best;
        }

        private static bool HasLineOfSight(
            Camera aimCamera,
            InkCreatureRuntime candidate,
            Vector3 aimPoint,
            LayerMask visibilityMask,
            RaycastHit[] hitBuffer)
        {
            Vector3 origin = aimCamera.transform.position;
            Vector3 toTarget = aimPoint - origin;
            float distance = toTarget.magnitude;

            if (distance <= 0.05f)
            {
                return true;
            }

            int hitCount = Physics.RaycastNonAlloc(
                origin,
                toTarget / distance,
                hitBuffer,
                distance + 0.15f,
                visibilityMask,
                QueryTriggerInteraction.Collide);
            float nearestBlocker = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hitBuffer[i];
                Collider hitCollider = hit.collider;

                if (hitCollider == null)
                {
                    continue;
                }

                InkCreatureRuntime hitCreature =
                    ResolveCreature(hitCollider);

                if (hitCreature == candidate ||
                    hitCollider.transform.IsChildOf(
                        candidate.transform))
                {
                    return true;
                }

                if (hit.distance > 0.05f &&
                    hit.distance < nearestBlocker)
                {
                    nearestBlocker = hit.distance;
                }
            }

            return nearestBlocker >= distance - 0.15f;
        }

        private static InkCreatureRuntime ResolveCreature(
            Collider hitCollider)
        {
            if (hitCollider == null)
            {
                return null;
            }

            InkCreatureRuntime candidate =
                hitCollider.GetComponentInParent<InkCreatureRuntime>();

            if (candidate != null)
            {
                return candidate;
            }

            InkGlyphHitZone hitZone =
                hitCollider.GetComponent<InkGlyphHitZone>();

            if (hitZone != null && hitZone.Creature != null)
            {
                return hitZone.Creature;
            }

            Rigidbody attachedBody = hitCollider.attachedRigidbody;
            return attachedBody != null
                ? attachedBody.GetComponentInParent<InkCreatureRuntime>()
                : null;
        }

        private static Vector3 ResolveAimPoint(
            InkCreatureRuntime candidate)
        {
            Bounds bounds = candidate.WorldBounds;
            return bounds.size.sqrMagnitude > 0.001f
                ? bounds.center
                : candidate.transform.position +
                    candidate.transform.up * 0.35f;
        }

        private static float DistanceToCreature(
            Vector3 interactionOrigin,
            InkCreatureRuntime candidate)
        {
            if (candidate == null)
            {
                return float.PositiveInfinity;
            }

            Bounds bounds = candidate.WorldBounds;
            Vector3 closestPoint = bounds.size.sqrMagnitude > 0.001f
                ? bounds.ClosestPoint(interactionOrigin)
                : candidate.transform.position;
            return Vector3.Distance(
                interactionOrigin,
                closestPoint);
        }

        private static Camera ResolveAimCamera(
            Camera configuredCamera)
        {
            if (configuredCamera != null &&
                configuredCamera.isActiveAndEnabled)
            {
                return configuredCamera;
            }

            Camera mainCamera = Camera.main;
            return mainCamera != null
                ? mainCamera
                : configuredCamera;
        }
    }
}
