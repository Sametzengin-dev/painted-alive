using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.Figures.CounterComposition
{
    [DisallowMultipleComponent]
    public sealed class PrototypeUnderlayerSeam :
        MonoBehaviour
    {
        [SerializeField]
        private PrototypeUnderlayerSeamId seamId;

        [SerializeField]
        private Transform surfaceAnchor;

        [SerializeField]
        private Transform underlayerAnchor;

        [SerializeField]
        private LineRenderer seamVisual;

        [SerializeField]
        private float openedUntil;

        [SerializeField]
        private int openCount;

        [SerializeField]
        private float sealedUntil;

        [SerializeField]
        private int sealCount;

        [SerializeField]
        private string lastSealSource = "None";

        [SerializeField]
        private float bracedUntil;

        [SerializeField]
        private int braceCount;

        [SerializeField]
        private string lastBraceSource = "None";

        [SerializeField]
        private string lastState = "Closed";

        private Material runtimeMaterial;

        public PrototypeUnderlayerSeamId SeamId => seamId;
        public Transform SurfaceAnchor => surfaceAnchor;
        public Transform UnderlayerAnchor => underlayerAnchor;
        public int OpenCount => openCount;
        public int SealCount => sealCount;
        public int BraceCount => braceCount;
        public string LastSealSource => lastSealSource;
        public string LastBraceSource => lastBraceSource;
        public string LastState => lastState;

        public bool IsSealed =>
            Time.unscaledTime <
            sealedUntil;

        public float SealedRemainingSeconds =>
            Mathf.Max(
                0f,
                sealedUntil -
                Time.unscaledTime);

        public bool IsBraced =>
            Time.unscaledTime <
            bracedUntil;

        public float BraceRemainingSeconds =>
            Mathf.Max(
                0f,
                bracedUntil -
                Time.unscaledTime);

        public bool BlocksTraversal =>
            IsSealed &&
            !IsBraced;

        public bool IsOpen =>
            Time.unscaledTime <
            openedUntil;

        public bool IsConfigured =>
            surfaceAnchor != null &&
            underlayerAnchor != null;

        public void Configure(
            PrototypeUnderlayerSeamId configuredId,
            Transform configuredSurfaceAnchor,
            Transform configuredUnderlayerAnchor)
        {
            seamId = configuredId;
            surfaceAnchor =
                configuredSurfaceAnchor;

            underlayerAnchor =
                configuredUnderlayerAnchor;

            EnsureVisual();
            RefreshVisual();
        }

        public void OpenForSeconds(
            float seconds)
        {
            openedUntil =
                Mathf.Max(
                    openedUntil,
                    Time.unscaledTime +
                    Mathf.Max(
                        0.25f,
                        seconds));

            openCount++;
            lastState = "Opened";

            RefreshVisual();
        }

        public bool SealForSeconds(
            float seconds,
            string source)
        {
            float effectiveSeconds =
                Mathf.Max(
                    0.25f,
                    seconds);

            sealedUntil =
                Mathf.Max(
                    sealedUntil,
                    Time.unscaledTime +
                    effectiveSeconds);

            sealCount++;

            lastSealSource =
                string.IsNullOrWhiteSpace(
                    source)
                    ? "Unknown"
                    : source;

            lastState =
                "Sealed";

            RefreshVisual();

            return true;
        }

        public void ClearSeal(
            string source)
        {
            sealedUntil = 0f;

            lastSealSource =
                string.IsNullOrWhiteSpace(
                    source)
                    ? "Cleared"
                    : source;

            lastState =
                IsBraced
                    ? "Braced"
                    : IsOpen
                        ? "Opened"
                        : "Closed";

            RefreshVisual();
        }

        public bool BraceForSeconds(
            float seconds,
            string source)
        {
            float effectiveSeconds =
                Mathf.Max(
                    0.35f,
                    seconds);

            bracedUntil =
                Mathf.Max(
                    bracedUntil,
                    Time.unscaledTime +
                    effectiveSeconds);

            braceCount++;

            lastBraceSource =
                string.IsNullOrWhiteSpace(
                    source)
                    ? "Unknown"
                    : source;

            lastState =
                IsSealed
                    ? "Braced / Seal bypassed"
                    : "Braced";

            OpenForSeconds(
                effectiveSeconds);

            RefreshVisual();

            return true;
        }

        public void ClearBrace(
            string source)
        {
            bracedUntil = 0f;

            lastBraceSource =
                string.IsNullOrWhiteSpace(
                    source)
                    ? "Cleared"
                    : source;

            lastState =
                IsSealed
                    ? "Sealed"
                    : IsOpen
                        ? "Opened"
                        : "Closed";

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

            if (IsBraced)
            {
                lastState =
                    IsSealed
                        ? "Braced / Seal bypassed"
                        : "Braced";
            }
            else if (IsSealed)
            {
                lastState =
                    "Sealed";
            }
            else if (
                !IsOpen &&
                (
                    lastState ==
                        "Opened" ||
                    lastState ==
                        "Sealed" ||
                    lastState ==
                        "Braced" ||
                    lastState ==
                        "Braced / Seal bypassed"
                )
            )
            {
                lastState =
                    "Closed";
            }
        }

        private void EnsureVisual()
        {
            if (seamVisual != null)
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
                    new Material(
                        shader)
                    {
                        name =
                            $"M52_0_Seam_{seamId}_Runtime",
                        hideFlags =
                            HideFlags.DontSave
                    };
            }

            GameObject visualObject =
                new GameObject(
                    "SeamVisual");

            visualObject.transform.SetParent(
                transform,
                false);

            seamVisual =
                visualObject.AddComponent<
                    LineRenderer>();

            seamVisual.useWorldSpace = true;
            seamVisual.loop = false;
            seamVisual.alignment =
                LineAlignment.View;

            seamVisual.textureMode =
                LineTextureMode.Stretch;

            seamVisual.numCapVertices = 4;
            seamVisual.numCornerVertices = 4;

            seamVisual.shadowCastingMode =
                ShadowCastingMode.Off;

            seamVisual.receiveShadows = false;
            seamVisual.positionCount = 5;

            seamVisual.sharedMaterial =
                runtimeMaterial;
        }

        private void RefreshVisual()
        {
            if (
                seamVisual == null ||
                surfaceAnchor == null
            )
            {
                return;
            }

            Vector3 center =
                surfaceAnchor.position +
                Vector3.up *
                0.06f;

            Vector3 right =
                surfaceAnchor.right;

            Vector3 forward =
                surfaceAnchor.forward;

            float width =
                IsBraced
                    ? 0.78f
                    : IsSealed
                        ? 0.58f
                        : IsOpen
                            ? 0.72f
                            : 0.46f;

            float length =
                IsBraced
                    ? 1.52f
                    : IsSealed
                        ? 1.18f
                        : IsOpen
                            ? 1.45f
                            : 1.05f;

            seamVisual.widthMultiplier =
                IsBraced
                    ? 0.105f
                    : IsSealed
                        ? 0.12f
                        : IsOpen
                            ? 0.09f
                            : 0.055f;

            seamVisual.SetPosition(
                0,
                center -
                forward *
                length *
                0.50f);

            seamVisual.SetPosition(
                1,
                center -
                forward *
                length *
                0.22f +
                right *
                width *
                0.42f);

            seamVisual.SetPosition(
                2,
                center +
                forward *
                0.05f -
                right *
                width *
                0.38f);

            seamVisual.SetPosition(
                3,
                center +
                forward *
                length *
                0.28f +
                right *
                width *
                0.34f);

            seamVisual.SetPosition(
                4,
                center +
                forward *
                length *
                0.50f);

            Color closed =
                new Color(
                    0.20f,
                    0.10f,
                    0.06f,
                    0.78f);

            Color open =
                new Color(
                    0.96f,
                    0.68f,
                    0.30f,
                    0.98f);

            Color sealedColor =
                new Color(
                    0.12f,
                    0.09f,
                    0.06f,
                    0.98f);

            Color bracedColor =
                new Color(
                    0.94f,
                    0.82f,
                    0.30f,
                    0.98f);

            Color color =
                IsBraced
                    ? bracedColor
                    : IsSealed
                        ? sealedColor
                        : IsOpen
                            ? open
                            : closed;

            seamVisual.startColor =
                color;

            seamVisual.endColor =
                color;
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
