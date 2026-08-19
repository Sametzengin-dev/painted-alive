using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.Figures.CounterComposition
{
    [DisallowMultipleComponent]
    public sealed class PrototypeLiveCompositionAnchor :
        MonoBehaviour
    {
        [SerializeField]
        private PrototypeLiveCompositionAnchorId anchorId;

        [SerializeField]
        private Transform anchorPoint;

        [SerializeField]
        private bool latched;

        [SerializeField]
        private int latchCount;

        [SerializeField]
        private int releaseCount;

        [SerializeField]
        private string lastState = "Available";

        private LineRenderer ring;
        private Material runtimeMaterial;

        public PrototypeLiveCompositionAnchorId AnchorId =>
            anchorId;
        public Transform AnchorPoint => anchorPoint;
        public bool Latched => latched;
        public int LatchCount => latchCount;
        public int ReleaseCount => releaseCount;
        public string LastState => lastState;
        public bool IsConfigured => anchorPoint != null;

        public void Configure(
            PrototypeLiveCompositionAnchorId configuredId,
            Transform configuredAnchorPoint)
        {
            anchorId = configuredId;
            anchorPoint = configuredAnchorPoint;

            EnsureVisual();
            RefreshVisual();
        }

        public bool Latch(
            string source)
        {
            if (
                !IsConfigured ||
                latched
            )
            {
                return false;
            }

            latched = true;
            latchCount++;

            lastState =
                string.IsNullOrWhiteSpace(source)
                    ? "Latched"
                    : $"Latched: {source}";

            RefreshVisual();

            return true;
        }

        public void Release(
            string source)
        {
            if (!latched)
            {
                return;
            }

            latched = false;
            releaseCount++;

            lastState =
                string.IsNullOrWhiteSpace(source)
                    ? "Available"
                    : $"Released: {source}";

            RefreshVisual();
        }

        private void Awake()
        {
            EnsureVisual();
            RefreshVisual();
        }

        private void Update()
        {
            RefreshVisual();
        }

        private void EnsureVisual()
        {
            if (ring != null)
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
                            $"M53_0_CompositionAnchor_{anchorId}",
                        hideFlags =
                            HideFlags.DontSave
                    };
            }

            GameObject visual =
                new GameObject(
                    "AnchorRing");

            visual.transform.SetParent(
                transform,
                false);

            ring =
                visual.AddComponent<
                    LineRenderer>();

            ring.useWorldSpace = true;
            ring.loop = true;
            ring.alignment =
                LineAlignment.View;
            ring.textureMode =
                LineTextureMode.Stretch;

            ring.numCapVertices = 3;
            ring.numCornerVertices = 3;
            ring.shadowCastingMode =
                ShadowCastingMode.Off;
            ring.receiveShadows = false;
            ring.widthMultiplier = 0.075f;
            ring.positionCount = 28;
            ring.sharedMaterial =
                runtimeMaterial;
        }

        private void RefreshVisual()
        {
            if (
                ring == null ||
                anchorPoint == null
            )
            {
                return;
            }

            Vector3 center =
                anchorPoint.position +
                Vector3.up *
                0.055f;

            float radius =
                latched
                    ? 0.55f
                    : 0.38f;

            for (int index = 0;
                 index < ring.positionCount;
                 index++)
            {
                float angle =
                    index /
                    (float)ring.positionCount *
                    Mathf.PI *
                    2f;

                Vector3 offset =
                    new Vector3(
                        Mathf.Cos(angle),
                        0f,
                        Mathf.Sin(angle)) *
                    radius;

                ring.SetPosition(
                    index,
                    center +
                    offset);
            }

            Color available =
                new Color(
                    0.48f,
                    0.42f,
                    0.20f,
                    0.72f);

            Color active =
                new Color(
                    0.96f,
                    0.84f,
                    0.30f,
                    0.98f);

            Color color =
                latched
                    ? active
                    : available;

            ring.startColor = color;
            ring.endColor = color;
        }

        private void OnDestroy()
        {
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
