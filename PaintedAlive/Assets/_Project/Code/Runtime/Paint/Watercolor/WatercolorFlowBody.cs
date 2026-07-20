using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Paint.Watercolor
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class WatercolorFlowBody : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody targetBody;

        [SerializeField, Range(0f, 2f)]
        private float forceMultiplier = 1f;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private bool isBeingCarried;

        public bool IsBeingCarried => isBeingCarried;

        private void Awake()
        {
            targetBody ??= GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            isBeingCarried = false;

            if (targetBody == null || targetBody.isKinematic)
            {
                return;
            }

            IReadOnlyList<WatercolorFlowSurface> surfaces =
                WatercolorFlowSurface.ActiveSurfaces;
            WatercolorFlowSurface bestSurface = null;
            Vector3 bestDirection = Vector3.zero;
            float bestInfluence = 0f;

            for (int i = surfaces.Count - 1; i >= 0; i--)
            {
                WatercolorFlowSurface surface = surfaces[i];

                if (surface == null ||
                    !surface.TrySampleFlow(
                        targetBody.worldCenterOfMass,
                        out Vector3 direction,
                        out float influence,
                        out _))
                {
                    continue;
                }

                if (influence <= bestInfluence)
                {
                    continue;
                }

                bestSurface = surface;
                bestDirection = direction;
                bestInfluence = influence;
            }

            if (bestSurface == null)
            {
                return;
            }

            targetBody.AddForce(
                bestDirection *
                bestSurface.RigidbodyFlowAcceleration *
                bestInfluence * forceMultiplier,
                ForceMode.Acceleration);
            isBeingCarried = true;
        }

        private void OnValidate()
        {
            forceMultiplier = Mathf.Clamp(forceMultiplier, 0f, 2f);
        }
    }
}
