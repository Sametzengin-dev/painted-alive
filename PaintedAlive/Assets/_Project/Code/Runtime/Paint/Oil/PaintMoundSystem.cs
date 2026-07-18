using System.Collections.Generic;
using PaintedAlive.Painters;
using UnityEngine;

namespace PaintedAlive.Paint
{
    public enum PaintMoundBlockReason
    {
        None,
        ConfigurationMissing,
        Cooldown,
        ActiveLimit,
        TotalLimit
    }

    [DisallowMultipleComponent]
    public sealed class PaintMoundSystem : MonoBehaviour
    {
        private const string MoundLayerName = "OilPaint";

        [Header("Configuration")]
        [SerializeField]
        private PainterPaintMoundConfig config;

        [SerializeField] private Material wetMaterial;
        [SerializeField] private Material dryMaterial;

        [Header("Runtime Hierarchy")]
        [SerializeField] private Transform moundsRoot;

        private readonly List<PaintMoundRuntime> mounds = new();
        private float nextAllowedPlacementTime;
        private int nextMoundId = 1;

        public PainterPaintMoundConfig Config => config;

        public IReadOnlyList<PaintMoundRuntime> Mounds => mounds;

        public float CooldownRemaining =>
            Mathf.Max(0f, nextAllowedPlacementTime - Time.time);

        public int TotalMoundCount
        {
            get
            {
                PruneMissingMounds();
                return mounds.Count;
            }
        }

        public int ActiveMoundCount
        {
            get
            {
                PruneMissingMounds();

                int count = 0;

                foreach (PaintMoundRuntime mound in mounds)
                {
                    if (mound != null && mound.IsActiveForBudget)
                        count++;
                }

                return count;
            }
        }

        public bool CanPlace(out PaintMoundBlockReason reason)
        {
            reason = PaintMoundBlockReason.None;

            if (config == null)
            {
                reason = PaintMoundBlockReason.ConfigurationMissing;
                return false;
            }

            if (CooldownRemaining > 0f)
            {
                reason = PaintMoundBlockReason.Cooldown;
                return false;
            }

            if (ActiveMoundCount >= config.MaximumActiveMounds)
            {
                reason = PaintMoundBlockReason.ActiveLimit;
                return false;
            }

            if (TotalMoundCount >= config.MaximumTotalMounds)
            {
                reason = PaintMoundBlockReason.TotalLimit;
                return false;
            }

            return true;
        }

        public bool TryCreateMound(
            Vector3 surfacePoint,
            Vector3 surfaceNormal,
            float chargeNormalized,
            out PaintMoundRuntime mound)
        {
            mound = null;

            if (!CanPlace(out _))
                return false;

            Transform parent =
                moundsRoot != null ? moundsRoot : transform;

            var moundObject =
                new GameObject($"PaintMound_{nextMoundId:0000}");

            moundObject.transform.SetParent(parent, true);

            int layer = LayerMask.NameToLayer(MoundLayerName);

            if (layer >= 0)
                moundObject.layer = layer;

            mound = moundObject.AddComponent<PaintMoundRuntime>();

            mound.Initialize(
                config,
                wetMaterial,
                dryMaterial,
                surfacePoint,
                surfaceNormal,
                chargeNormalized);

            if (!mound.enabled)
            {
                Destroy(moundObject);
                mound = null;
                return false;
            }

            mounds.Add(mound);
            nextMoundId++;

            nextAllowedPlacementTime =
                Time.time + config.PlacementCooldown;

            return true;
        }

        public void RemoveMound(PaintMoundRuntime mound)
        {
            if (mound == null)
                return;

            mounds.Remove(mound);
            Destroy(mound.gameObject);
        }

        public void RollbackLastPlacement(PaintMoundRuntime mound)
        {
            RemoveMound(mound);
            nextAllowedPlacementTime = 0f;
        }

        public void ClearAllMounds()
        {
            for (int i = mounds.Count - 1; i >= 0; i--)
            {
                if (mounds[i] != null)
                    Destroy(mounds[i].gameObject);
            }

            mounds.Clear();
            nextMoundId = 1;
            nextAllowedPlacementTime = 0f;
        }

        private void PruneMissingMounds()
        {
            for (int i = mounds.Count - 1; i >= 0; i--)
            {
                if (mounds[i] == null)
                    mounds.RemoveAt(i);
            }
        }
    }
}
