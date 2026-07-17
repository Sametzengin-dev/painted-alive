using PaintedAlive.Paint;
using UnityEngine;

namespace PaintedAlive.Figures
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FigurePaintContactSensor : MonoBehaviour
    {
        [SerializeField] private FigureClarityState clarityState;
        [SerializeField, Min(0.05f)] private float contactMemory = 0.15f;

        private CharacterController characterController;
        private OilStrokeRuntime contactedStroke;
        private FigurePaintRegion contactedRegion;
        private float contactRemaining;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (clarityState == null)
                clarityState = GetComponent<FigureClarityState>();
        }

        private void Update()
        {
            if (clarityState == null || contactedStroke == null)
                return;

            contactRemaining -= Time.deltaTime;

            if (contactRemaining <= 0f)
            {
                contactedStroke = null;
                return;
            }

            float exposurePerSecond =
                clarityState.GetExposurePerSecond(contactedStroke.State);

            if (exposurePerSecond <= 0f)
                return;

            clarityState.ApplyPaintExposure(
                exposurePerSecond * Time.deltaTime,
                contactedRegion);
        }

        private void OnControllerColliderHit(
            ControllerColliderHit hit)
        {
            OilStrokeRuntime stroke =
                hit.collider.GetComponentInParent<OilStrokeRuntime>();

            if (stroke == null)
                return;

            contactedStroke = stroke;
            contactedRegion = DetermineRegion(hit.point);
            contactRemaining = contactMemory;
        }

        private FigurePaintRegion DetermineRegion(Vector3 hitPoint)
        {
            Bounds bounds = characterController.bounds;

            float normalizedHeight = Mathf.InverseLerp(
                bounds.min.y,
                bounds.max.y,
                hitPoint.y);

            if (normalizedHeight < 0.35f)
                return FigurePaintRegion.Legs;

            if (normalizedHeight < 0.62f)
                return FigurePaintRegion.Torso;

            if (normalizedHeight < 0.84f)
                return FigurePaintRegion.Arms;

            return FigurePaintRegion.Head;
        }
    }
}
