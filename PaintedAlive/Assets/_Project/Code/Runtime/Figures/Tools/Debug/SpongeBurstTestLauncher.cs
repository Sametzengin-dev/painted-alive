using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures.Tools
{
    [DisallowMultipleComponent]
    public sealed class SpongeBurstTestLauncher : MonoBehaviour
    {
        [SerializeField]
        private FigureMotor targetFigure;

        [SerializeField]
        private SpongeBurstImpactProjectile projectilePrefab;

        [SerializeField]
        private bool enableKeyboardShortcut = true;

        [SerializeField, Min(0.5f)]
        private float spawnDistance = 4.5f;

        [SerializeField, Min(0f)]
        private float spawnHeight = 1.15f;

        [SerializeField, Min(1f)]
        private float launchSpeed = 12f;

        private void Awake()
        {
            if (targetFigure == null)
            {
                targetFigure = GetComponentInParent<FigureMotor>();
            }
        }

        private void Update()
        {
            if (!enableKeyboardShortcut ||
                Keyboard.current == null ||
                !Keyboard.current.bKey.wasPressedThisFrame)
            {
                return;
            }

            LaunchTestProjectile();
        }

        [ContextMenu("Debug/Launch Burst Test Projectile")]
        public void LaunchTestProjectile()
        {
            if (!Application.isPlaying ||
                targetFigure == null ||
                projectilePrefab == null)
            {
                Debug.LogWarning(
                    "Burst test projectile requires Play Mode, " +
                    "Target Figure and Projectile Prefab.",
                    this);
                return;
            }

            Transform targetTransform = targetFigure.transform;
            Vector3 targetPoint =
                targetTransform.position + Vector3.up;
            Vector3 spawnPoint =
                targetTransform.position +
                targetTransform.forward * spawnDistance +
                Vector3.up * spawnHeight;
            Vector3 direction =
                (targetPoint - spawnPoint).normalized;

            SpongeBurstImpactProjectile projectile =
                Instantiate(
                    projectilePrefab,
                    spawnPoint,
                    Quaternion.identity);

            projectile.name =
                "SpongeBurstImpactProjectile_Runtime";
            projectile.Launch(direction * launchSpeed);
        }

        private void OnValidate()
        {
            spawnDistance = Mathf.Max(0.5f, spawnDistance);
            spawnHeight = Mathf.Max(0f, spawnHeight);
            launchSpeed = Mathf.Max(1f, launchSpeed);
        }
    }
}
