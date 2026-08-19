using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.Figures.CounterComposition
{
    [DisallowMultipleComponent]
    public sealed class PrototypeUnderlayerInkRootThreat :
        MonoBehaviour
    {
        [SerializeField]
        private PrototypeUnderlayerDiveController underlayerDive;

        [SerializeField]
        private Transform figureRoot;

        [SerializeField]
        private Transform underlayerAnchorA;

        [SerializeField]
        private Transform underlayerAnchorB;

        [SerializeField, Min(0.1f)]
        private float patrolSpeed = 1.65f;

        [SerializeField, Range(0f, 0.45f)]
        private float endpointInset = 0.18f;

        [SerializeField, Min(0.2f)]
        private float contactRadius = 0.82f;

        [SerializeField, Min(0.1f)]
        private float contactCooldown = 1.5f;

        [SerializeField]
        private int contactRecoveryCount;

        [SerializeField]
        private float patrolProgress;

        [SerializeField]
        private string lastAction = "Patrol";

        private LineRenderer rootVisual;
        private Material rootMaterial;
        private float nextContactAt;

        public int ContactRecoveryCount =>
            contactRecoveryCount;
        public float PatrolProgress =>
            patrolProgress;
        public string LastAction => lastAction;

        public bool RawDamageEnabled => false;
        public bool UsesSafeSeamRecovery => true;
        public bool NetworkAuthorityEnabled => false;

        public void Configure(
            PrototypeUnderlayerDiveController configuredDive,
            Transform configuredFigureRoot,
            Transform configuredAnchorA,
            Transform configuredAnchorB)
        {
            underlayerDive = configuredDive;
            figureRoot = configuredFigureRoot;
            underlayerAnchorA = configuredAnchorA;
            underlayerAnchorB = configuredAnchorB;

            EnsureVisual();
            RefreshPosition();
        }

        private void Awake()
        {
            EnsureVisual();
        }

        private void Update()
        {
            RefreshPosition();
            CheckFigureContact();
        }

        private void RefreshPosition()
        {
            if (
                underlayerAnchorA == null ||
                underlayerAnchorB == null
            )
            {
                return;
            }

            float distance =
                Vector3.Distance(
                    underlayerAnchorA.position,
                    underlayerAnchorB.position);

            float duration =
                distance > 0.001f
                    ? distance /
                      Mathf.Max(
                          0.1f,
                          patrolSpeed)
                    : 1f;

            float raw =
                Mathf.PingPong(
                    Time.unscaledTime /
                    Mathf.Max(
                        0.1f,
                        duration),
                    1f);

            patrolProgress =
                Mathf.Lerp(
                    endpointInset,
                    1f - endpointInset,
                    raw);

            Vector3 position =
                Vector3.Lerp(
                    underlayerAnchorA.position,
                    underlayerAnchorB.position,
                    patrolProgress) +
                Vector3.up *
                0.35f;

            transform.position =
                position;

            UpdateVisual();
        }

        private void CheckFigureContact()
        {
            if (
                underlayerDive == null ||
                !underlayerDive.InUnderlayer ||
                figureRoot == null ||
                Time.unscaledTime <
                    nextContactAt
            )
            {
                return;
            }

            float distance =
                Vector3.Distance(
                    figureRoot.position,
                    transform.position);

            if (
                distance >
                contactRadius
            )
            {
                return;
            }

            nextContactAt =
                Time.unscaledTime +
                contactCooldown;

            contactRecoveryCount++;

            lastAction =
                "Figure temas etti; güvenli yüzeye geri gönderildi.";

            underlayerDive.ForceSafeSurfaceRecovery(
                "M52.1 Ink Root contact");

            Debug.LogWarning(
                "[M52.1 Ink Root Contact]\n" +
                $"ContactRecoveryCount={contactRecoveryCount}\n" +
                "RawDamageEnabled=False\n" +
                "UsesSafeSeamRecovery=True",
                this);
        }

        private void EnsureVisual()
        {
            if (rootVisual != null)
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
                rootMaterial =
                    new Material(
                        shader)
                    {
                        name =
                            "M52_1_InkRoot_Runtime",
                        hideFlags =
                            HideFlags.DontSave
                    };
            }

            GameObject visual =
                new GameObject(
                    "InkRootVisual");

            visual.transform.SetParent(
                transform,
                false);

            rootVisual =
                visual.AddComponent<
                    LineRenderer>();

            rootVisual.useWorldSpace = true;
            rootVisual.loop = false;
            rootVisual.alignment =
                LineAlignment.View;

            rootVisual.textureMode =
                LineTextureMode.Stretch;

            rootVisual.numCapVertices = 5;
            rootVisual.numCornerVertices = 5;

            rootVisual.shadowCastingMode =
                ShadowCastingMode.Off;

            rootVisual.receiveShadows = false;
            rootVisual.widthMultiplier =
                0.19f;

            rootVisual.positionCount = 5;
            rootVisual.sharedMaterial =
                rootMaterial;

            Color color =
                new Color(
                    0.04f,
                    0.02f,
                    0.07f,
                    0.98f);

            rootVisual.startColor = color;
            rootVisual.endColor = color;
        }

        private void UpdateVisual()
        {
            if (rootVisual == null)
            {
                return;
            }

            Vector3 center =
                transform.position;

            rootVisual.SetPosition(
                0,
                center +
                new Vector3(
                    -0.52f,
                    -0.22f,
                    0f));

            rootVisual.SetPosition(
                1,
                center +
                new Vector3(
                    -0.26f,
                    0.18f,
                    0.08f));

            rootVisual.SetPosition(
                2,
                center +
                new Vector3(
                    0f,
                    0.42f,
                    -0.06f));

            rootVisual.SetPosition(
                3,
                center +
                new Vector3(
                    0.26f,
                    0.14f,
                    0.07f));

            rootVisual.SetPosition(
                4,
                center +
                new Vector3(
                    0.52f,
                    -0.20f,
                    0f));
        }

        private void OnDestroy()
        {
            if (rootMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(
                    rootMaterial);
            }
            else
            {
                DestroyImmediate(
                    rootMaterial);
            }

            rootMaterial = null;
        }
    }
}
