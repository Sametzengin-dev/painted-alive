using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.Figures.StainSupport
{
    [DisallowMultipleComponent]
    public sealed class StainGripMark : MonoBehaviour
    {
        private static readonly List<StainGripMark> Marks =
            new List<StainGripMark>();

        [SerializeField]
        private float expiresAt;

        [SerializeField]
        private Vector3 surfaceNormal = Vector3.up;

        [SerializeField]
        private bool createsWallLedge;

        private Transform markVisual;
        private Vector3 markVisualBaseScale;
        private bool initialized;

        public static IReadOnlyList<StainGripMark> ActiveMarks =>
            Marks;
        public float RemainingLifetime =>
            Mathf.Max(0f, expiresAt - Time.time);
        public bool CreatesWallLedge => createsWallLedge;

        private void OnEnable()
        {
            if (!Marks.Contains(this))
            {
                Marks.Add(this);
            }
        }

        private void OnDisable()
        {
            Marks.Remove(this);
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            float remaining = RemainingLifetime;

            if (remaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (markVisual != null && remaining <= 1.25f)
            {
                float pulse =
                    0.9f +
                    Mathf.Sin(Time.time * 12f) * 0.1f;
                markVisual.localScale =
                    markVisualBaseScale * pulse;
            }
        }

        public void Initialize(
            Vector3 point,
            Vector3 normal,
            StainGripImprintConfig config)
        {
            if (config == null)
            {
                Destroy(gameObject);
                return;
            }

            surfaceNormal = normal.sqrMagnitude > 0.001f
                ? normal.normalized
                : Vector3.up;
            transform.position = point;
            transform.rotation = Quaternion.identity;
            expiresAt = Time.time + config.MarkLifetime;
            gameObject.layer = 2;

            CreateMarkVisual(config);
            CreatePhysicalSupport(config);
            initialized = true;
        }

        public void ExpireNow()
        {
            expiresAt = Time.time;

            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }

            Destroy(gameObject);
        }

        private void CreateMarkVisual(
            StainGripImprintConfig config)
        {
            GameObject visual = GameObject.CreatePrimitive(
                PrimitiveType.Cylinder);
            visual.name = "GripMarkVisual";
            visual.layer = 2;
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition =
                surfaceNormal * 0.018f;
            visual.transform.rotation =
                Quaternion.FromToRotation(
                    Vector3.up,
                    surfaceNormal);
            visual.transform.localScale =
                new Vector3(
                    config.MarkDiameter * 0.5f,
                    0.018f,
                    config.MarkDiameter * 0.5f);

            Collider visualCollider =
                visual.GetComponent<Collider>();

            if (visualCollider != null)
            {
                Destroy(visualCollider);
            }

            Renderer renderer =
                visual.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial = config.MarkMaterial;
                renderer.shadowCastingMode =
                    ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            markVisual = visual.transform;
            markVisualBaseScale =
                markVisual.localScale;
        }

        private void CreatePhysicalSupport(
            StainGripImprintConfig config)
        {
            createsWallLedge =
                Mathf.Abs(
                    Vector3.Dot(
                        surfaceNormal,
                        Vector3.up)) < 0.55f;
            GameObject support = GameObject.CreatePrimitive(
                PrimitiveType.Cube);
            support.name = createsWallLedge
                ? "GripWallLedge"
                : "GripSurfacePad";
            support.layer = 2;
            support.transform.SetParent(transform, true);

            if (createsWallLedge)
            {
                Vector3 horizontalNormal =
                    Vector3.ProjectOnPlane(
                        surfaceNormal,
                        Vector3.up).normalized;

                if (horizontalNormal.sqrMagnitude < 0.001f)
                {
                    horizontalNormal = Vector3.forward;
                }

                support.transform.rotation =
                    Quaternion.LookRotation(
                        horizontalNormal,
                        Vector3.up);
                support.transform.position =
                    transform.position +
                    horizontalNormal *
                    (config.WallLedgeDepth * 0.5f) -
                    Vector3.up *
                    (config.PlatformThickness * 0.5f);
                support.transform.localScale =
                    new Vector3(
                        config.MarkDiameter,
                        config.PlatformThickness,
                        config.WallLedgeDepth);
            }
            else
            {
                support.transform.rotation =
                    Quaternion.FromToRotation(
                        Vector3.up,
                        surfaceNormal);
                support.transform.position =
                    transform.position +
                    surfaceNormal *
                    (config.PlatformThickness * 0.5f);
                support.transform.localScale =
                    new Vector3(
                        config.MarkDiameter,
                        config.PlatformThickness,
                        config.MarkDiameter);
            }

            Renderer renderer =
                support.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial =
                    config.SupportMaterial;
                renderer.shadowCastingMode =
                    ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }
    }
}
