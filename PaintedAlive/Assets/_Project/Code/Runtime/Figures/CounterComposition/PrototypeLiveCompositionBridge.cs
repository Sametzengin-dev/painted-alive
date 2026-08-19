using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.Figures.CounterComposition
{
    [DisallowMultipleComponent]
    public sealed class PrototypeLiveCompositionBridge :
        MonoBehaviour
    {
        [SerializeField]
        private bool activeBridge;

        [SerializeField]
        private int colliderSegmentCount;

        [SerializeField]
        private float spanLength;

        [SerializeField]
        private Vector3 anchorPointA;

        [SerializeField]
        private Vector3 anchorPointB;

        [SerializeField]
        private Vector3 bodyPoint;

        [SerializeField]
        private Vector3 originalBodyPoint;

        [SerializeField]
        private int watercolorBendCount;

        [SerializeField]
        private string lastReaction = "None";

        [SerializeField]
        private string lastState = "Inactive";

        private readonly List<BoxCollider> colliders =
            new List<BoxCollider>();

        private LineRenderer bridgeLine;
        private LineRenderer bodyContour;
        private Material runtimeMaterial;

        public bool ActiveBridge => activeBridge;
        public int ColliderSegmentCount =>
            colliderSegmentCount;
        public float SpanLength => spanLength;
        public Vector3 AnchorPointA => anchorPointA;
        public Vector3 AnchorPointB => anchorPointB;
        public Vector3 BodyPoint => bodyPoint;
        public Vector3 OriginalBodyPoint => originalBodyPoint;
        public int WatercolorBendCount => watercolorBendCount;
        public string LastReaction => lastReaction;
        public string LastState => lastState;

        public bool UsesBoxSegmentProxies => true;
        public bool UsesMeshCollider => false;
        public bool UsesRagdollPhysics => false;
        public bool FreeformStructureEnabled => false;
        public bool TemplateBridgeOnly => true;

        public bool Build(
            Vector3 anchorA,
            Vector3 anchorB,
            Vector3 configuredBodyPoint)
        {
            ClearGeometry();

            Vector3 span =
                anchorB -
                anchorA;

            spanLength =
                span.magnitude;

            if (spanLength < 1.5f)
            {
                lastState =
                    "Span too short";

                return false;
            }

            anchorPointA =
                anchorA;

            anchorPointB =
                anchorB;

            bodyPoint =
                configuredBodyPoint;

            originalBodyPoint =
                configuredBodyPoint;

            watercolorBendCount = 0;
            lastReaction = "Built";

            return RebuildGeometry();
        }

        public bool OwnsCollider(
            Collider candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            Transform candidateTransform =
                candidate.transform;

            return
                candidateTransform == transform ||
                candidateTransform.IsChildOf(
                    transform);
        }

        public bool TryApplyWatercolorBend(
            Vector3 worldDelta,
            float maximumBodyDisplacement,
            string source,
            out string reason)
        {
            reason = "Unknown";

            if (!activeBridge)
            {
                reason =
                    "Bridge inactive";

                return false;
            }

            Vector3 planarDelta =
                Vector3.ProjectOnPlane(
                    worldDelta,
                    Vector3.up);

            if (
                planarDelta.sqrMagnitude <
                0.0001f
            )
            {
                reason =
                    "Bend delta too small";

                return false;
            }

            Vector3 candidate =
                bodyPoint +
                planarDelta;

            Vector3 offset =
                candidate -
                originalBodyPoint;

            float maxDisplacement =
                Mathf.Max(
                    0.15f,
                    maximumBodyDisplacement);

            if (
                offset.magnitude >
                maxDisplacement
            )
            {
                candidate =
                    originalBodyPoint +
                    offset.normalized *
                    maxDisplacement;
            }

            float leftLength =
                Vector3.Distance(
                    anchorPointA,
                    candidate);

            float rightLength =
                Vector3.Distance(
                    candidate,
                    anchorPointB);

            if (
                leftLength < 0.75f ||
                rightLength < 0.75f
            )
            {
                reason =
                    "Bend would collapse a segment";

                return false;
            }

            bodyPoint = candidate;

            if (!RebuildGeometry())
            {
                reason =
                    "Geometry rebuild failed";

                return false;
            }

            watercolorBendCount++;

            lastReaction =
                string.IsNullOrWhiteSpace(
                    source)
                    ? "Watercolor bend"
                    : $"Watercolor bend: {source}";

            lastState =
                "Active / Watercolor bent";

            reason = lastReaction;

            return true;
        }

        private bool RebuildGeometry()
        {
            ClearGeometry();

            Vector3 span =
                anchorPointB -
                anchorPointA;

            spanLength =
                span.magnitude;

            if (spanLength < 1.5f)
            {
                lastState =
                    "Span too short";

                return false;
            }

            EnsureVisuals();

            bridgeLine.positionCount = 3;

            bridgeLine.SetPosition(
                0,
                anchorPointA +
                Vector3.up *
                0.10f);

            bridgeLine.SetPosition(
                1,
                bodyPoint +
                Vector3.up *
                0.15f);

            bridgeLine.SetPosition(
                2,
                anchorPointB +
                Vector3.up *
                0.10f);

            bridgeLine.enabled = true;

            BuildSegment(
                anchorPointA,
                bodyPoint,
                0);

            BuildSegment(
                bodyPoint,
                anchorPointB,
                1);

            BuildBodyContour(
                bodyPoint,
                span.normalized);

            colliderSegmentCount =
                colliders.Count;

            activeBridge =
                colliderSegmentCount == 2;

            lastState =
                activeBridge
                    ? "Active"
                    : "Collider build failed";

            return activeBridge;
        }

        public void Deactivate(
            string reason)
        {
            if (!activeBridge)
            {
                return;
            }

            activeBridge = false;

            lastState =
                string.IsNullOrWhiteSpace(
                    reason)
                    ? "Released"
                    : reason;

            if (bridgeLine != null)
            {
                bridgeLine.enabled = false;
            }

            if (bodyContour != null)
            {
                bodyContour.enabled = false;
            }

            for (int index = 0;
                 index < colliders.Count;
                 index++)
            {
                BoxCollider collider =
                    colliders[index];

                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
        }

        private void BuildSegment(
            Vector3 start,
            Vector3 end,
            int index)
        {
            Vector3 delta =
                end -
                start;

            float length =
                delta.magnitude;

            if (length < 0.2f)
            {
                return;
            }

            GameObject segment =
                new GameObject(
                    $"CompositionBridgeCollider_{index:00}");

            segment.transform.SetParent(
                transform,
                false);

            segment.transform.position =
                (
                    start +
                    end
                ) *
                0.5f +
                Vector3.up *
                0.08f;

            segment.transform.rotation =
                Quaternion.LookRotation(
                    delta.normalized,
                    Vector3.up);

            BoxCollider collider =
                segment.AddComponent<
                    BoxCollider>();

            collider.isTrigger = false;

            collider.size =
                new Vector3(
                    0.92f,
                    0.16f,
                    length +
                    0.08f);

            collider.center =
                Vector3.zero;

            colliders.Add(
                collider);
        }

        private void BuildBodyContour(
            Vector3 center,
            Vector3 spanDirection)
        {
            if (bodyContour == null)
            {
                return;
            }

            Vector3 right =
                Vector3.Cross(
                    Vector3.up,
                    spanDirection);

            if (
                right.sqrMagnitude <
                0.001f
            )
            {
                right = Vector3.right;
            }
            else
            {
                right.Normalize();
            }

            bodyContour.positionCount = 5;

            bodyContour.SetPosition(
                0,
                center -
                spanDirection *
                0.80f +
                Vector3.up *
                0.16f);

            bodyContour.SetPosition(
                1,
                center -
                right *
                0.34f +
                Vector3.up *
                0.52f);

            bodyContour.SetPosition(
                2,
                center +
                Vector3.up *
                0.82f);

            bodyContour.SetPosition(
                3,
                center +
                right *
                0.34f +
                Vector3.up *
                0.52f);

            bodyContour.SetPosition(
                4,
                center +
                spanDirection *
                0.80f +
                Vector3.up *
                0.16f);

            bodyContour.enabled = true;
        }

        private void EnsureVisuals()
        {
            if (
                bridgeLine != null &&
                bodyContour != null
            )
            {
                return;
            }

            Shader shader =
                Shader.Find(
                    "Sprites/Default");

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Universal Render Pipeline/Unlit");
            }

            if (shader != null)
            {
                runtimeMaterial =
                    new Material(shader)
                    {
                        name =
                            "M53_0_LiveComposition_Runtime",
                        hideFlags =
                            HideFlags.DontSave
                    };
            }

            GameObject lineObject =
                new GameObject(
                    "CompositionBridgeVisual");

            lineObject.transform.SetParent(
                transform,
                false);

            bridgeLine =
                lineObject.AddComponent<
                    LineRenderer>();

            ConfigureLine(
                bridgeLine,
                0.16f);

            Color bridgeColor =
                new Color(
                    0.95f,
                    0.80f,
                    0.30f,
                    0.96f);

            bridgeLine.startColor =
                bridgeColor;
            bridgeLine.endColor =
                bridgeColor;

            GameObject bodyObject =
                new GameObject(
                    "CompositionBodyContour");

            bodyObject.transform.SetParent(
                transform,
                false);

            bodyContour =
                bodyObject.AddComponent<
                    LineRenderer>();

            ConfigureLine(
                bodyContour,
                0.10f);

            Color bodyColor =
                new Color(
                    0.96f,
                    0.93f,
                    0.72f,
                    0.98f);

            bodyContour.startColor =
                bodyColor;
            bodyContour.endColor =
                bodyColor;
        }

        private void ConfigureLine(
            LineRenderer line,
            float width)
        {
            line.useWorldSpace = true;
            line.loop = false;
            line.alignment =
                LineAlignment.View;

            line.textureMode =
                LineTextureMode.Stretch;

            line.numCapVertices = 4;
            line.numCornerVertices = 4;

            line.shadowCastingMode =
                ShadowCastingMode.Off;

            line.receiveShadows = false;
            line.widthMultiplier = width;

            line.sharedMaterial =
                runtimeMaterial;

            line.enabled = false;
        }

        private void ClearGeometry()
        {
            for (int index = 0;
                 index < colliders.Count;
                 index++)
            {
                BoxCollider collider =
                    colliders[index];

                if (collider == null)
                {
                    continue;
                }

                collider.enabled = false;

                GameObject objectToDestroy =
                    collider.gameObject;

                if (Application.isPlaying)
                {
                    Destroy(
                        objectToDestroy);
                }
                else
                {
                    DestroyImmediate(
                        objectToDestroy);
                }
            }

            colliders.Clear();
            colliderSegmentCount = 0;
            activeBridge = false;
        }

        private void OnDestroy()
        {
            ClearGeometry();

            if (runtimeMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(
                    runtimeMaterial);
            }
            else
            {
                DestroyImmediate(
                    runtimeMaterial);
            }

            runtimeMaterial = null;
        }
    }
}
