using PaintedAlive.Core.RoleAuthority;
using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.Environment.LivingGallery
{
    /// <summary>
    /// Lightweight Painter-only false reflection presentation. It is deliberately
    /// non-colliding and is not a duplicate Figure/network actor. Final art can
    /// replace this material/mesh without changing the authority-safe event data.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LivingGalleryInkDecoyPresenter : MonoBehaviour,
        ILivingGalleryResettable
    {
        [SerializeField] private InkReflectionDecoySystem system;
        [SerializeField] private Transform poolReference;
        [SerializeField, Min(0.2f)] private float baseLength = 1.5f;
        [SerializeField, Min(0.1f)] private float baseWidth = 0.62f;
        [SerializeField, Range(0.05f, 1f)] private float baseOpacity = 0.42f;

        private GameObject visualObject;
        private MeshRenderer visualRenderer;
        private Material runtimeMaterial;
        private int shownEventId = int.MinValue;

        public bool IsConfigured => system != null && poolReference != null;

        public void Configure(
            InkReflectionDecoySystem decoySystem,
            Transform pool,
            float opacity = 0.42f)
        {
            system = decoySystem;
            poolReference = pool;
            baseOpacity = Mathf.Clamp(opacity, 0.05f, 1f);
        }

        private void Awake() => EnsureVisual();

        private void LateUpdate()
        {
            EnsureVisual();
            if (visualObject == null)
                return;

            bool painterActive =
                PrototypeRoleAuthorityResolver.IsPainter(out _);

            if (!painterActive || system == null ||
                !system.HasActiveDecoy || poolReference == null)
            {
                visualObject.SetActive(false);
                return;
            }

            InkDecoyEventData data = system.ActiveDecoy;
            visualObject.SetActive(true);
            visualObject.transform.position =
                data.projectionPosition + poolReference.up * 0.025f;
            visualObject.transform.rotation =
                poolReference.rotation *
                Quaternion.AngleAxis(data.orientationDegrees, Vector3.up);

            float pulse = 1f +
                Mathf.Sin(Time.time * 4.2f + data.seed * 0.013f) *
                0.06f * Mathf.Max(0.1f, data.distortion);
            visualObject.transform.localScale = new Vector3(
                baseWidth * pulse,
                1f,
                baseLength / Mathf.Max(0.01f, baseWidth) / pulse);

            if (runtimeMaterial != null && shownEventId != data.eventId)
            {
                Color color = new Color(
                    0.16f,
                    0.18f,
                    0.22f,
                    Mathf.Clamp01(baseOpacity * data.opacity));
                SetMaterialColor(runtimeMaterial, color);
                shownEventId = data.eventId;
            }
        }

        private void EnsureVisual()
        {
            if (visualObject != null)
                return;

            visualObject = new GameObject("InkDecoy_PainterOnly_Runtime");
            visualObject.hideFlags = HideFlags.DontSave;
            visualObject.transform.SetParent(transform, false);

            MeshFilter filter = visualObject.AddComponent<MeshFilter>();
            visualRenderer = visualObject.AddComponent<MeshRenderer>();
            filter.sharedMesh = BuildSilhouetteMesh();

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                            Shader.Find("Unlit/Color");
            if (shader != null)
            {
                runtimeMaterial = new Material(shader)
                {
                    name = "MAT_LG_InkDecoy_Runtime",
                    hideFlags = HideFlags.DontSave
                };
                ConfigureTransparent(runtimeMaterial);
                SetMaterialColor(
                    runtimeMaterial,
                    new Color(0.16f, 0.18f, 0.22f, baseOpacity));
                visualRenderer.sharedMaterial = runtimeMaterial;
            }

            visualRenderer.shadowCastingMode = ShadowCastingMode.Off;
            visualRenderer.receiveShadows = false;
            visualObject.SetActive(false);
        }

        private static Mesh BuildSilhouetteMesh()
        {
            // Convex stylized reflected Figure footprint in the local XZ plane.
            Vector3[] ring =
            {
                new Vector3( 0.00f, 0f,  0.72f),
                new Vector3(-0.18f, 0f,  0.60f),
                new Vector3(-0.28f, 0f,  0.32f),
                new Vector3(-0.34f, 0f,  0.02f),
                new Vector3(-0.22f, 0f, -0.28f),
                new Vector3(-0.15f, 0f, -0.72f),
                new Vector3( 0.00f, 0f, -0.82f),
                new Vector3( 0.15f, 0f, -0.72f),
                new Vector3( 0.22f, 0f, -0.28f),
                new Vector3( 0.34f, 0f,  0.02f),
                new Vector3( 0.28f, 0f,  0.32f),
                new Vector3( 0.18f, 0f,  0.60f)
            };

            Vector3[] vertices = new Vector3[ring.Length + 1];
            vertices[0] = Vector3.zero;
            for (int index = 0; index < ring.Length; index++)
                vertices[index + 1] = ring[index];

            int[] triangles = new int[ring.Length * 3];
            for (int index = 0; index < ring.Length; index++)
            {
                int next = (index + 1) % ring.Length;
                triangles[index * 3 + 0] = 0;
                triangles[index * 3 + 1] = index + 1;
                triangles[index * 3 + 2] = next + 1;
            }

            Mesh mesh = new Mesh
            {
                name = "MESH_LG_InkDecoy_Runtime",
                hideFlags = HideFlags.DontSave
            };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void ConfigureTransparent(Material material)
        {
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_SrcBlend"))
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
                Destroy(runtimeMaterial);
            if (visualObject != null)
                Destroy(visualObject);
        }

        public void ResetLivingGalleryState()
        {
            shownEventId = int.MinValue;
            if (visualObject != null)
                visualObject.SetActive(false);
        }
    }
}
