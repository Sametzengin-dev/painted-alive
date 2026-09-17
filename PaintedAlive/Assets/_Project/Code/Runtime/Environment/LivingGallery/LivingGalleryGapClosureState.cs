using System;
using System.Linq;
using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    public enum LivingGalleryGapResolutionKind
    {
        ExistingRuntimeVolume = 0,
        DerivedFromAuthoredGeometry = 1,
        RuntimePositionDetection = 2,
        ExplicitUnityRouteBinding = 3,
        ExplicitNoEffectPolicy = 4,
        GeneratedCoarseCollisionState = 5,
        ExplicitMotionPolicy = 6,
        ExistingCharacterAuthority = 7,
        CurrentJourneyTerminalPolicy = 8
    }

    [Serializable]
    public struct LivingGalleryGapResolutionRecord
    {
        public string gapId;
        public LivingGalleryGapResolutionKind kind;
        public bool resolved;
        [TextArea(1, 4)] public string note;

        public LivingGalleryGapResolutionRecord(
            string id,
            LivingGalleryGapResolutionKind resolutionKind,
            bool isResolved,
            string resolutionNote)
        {
            gapId = id ?? string.Empty;
            kind = resolutionKind;
            resolved = isResolved;
            note = resolutionNote ?? string.Empty;
        }
    }

    [DisallowMultipleComponent]
    public sealed class LivingGalleryGapClosureState : MonoBehaviour
    {
        [SerializeField] private string sourceManifestSchema =
            "painted_alive.living_gallery.unity_handoff.v1";
        [SerializeField] private string sourceManifestSha256 =
            "dca59e0ae771c5dfb297ce74aafbf6cdf3e43b00312afbee68742ba00520079a";
        [SerializeField] private LivingGalleryGapResolutionRecord[] resolutions =
            Array.Empty<LivingGalleryGapResolutionRecord>();

        public string SourceManifestSchema => sourceManifestSchema;
        public string SourceManifestSha256 => sourceManifestSha256;
        public LivingGalleryGapResolutionRecord[] Resolutions => resolutions;
        public int ResolutionCount => resolutions != null ? resolutions.Length : 0;
        public int ResolvedCount => resolutions == null
            ? 0
            : resolutions.Count(record => record.resolved);
        public bool AllResolved => ResolutionCount == 9 && ResolvedCount == 9;

        public void Configure(LivingGalleryGapResolutionRecord[] records)
        {
            resolutions = records ?? Array.Empty<LivingGalleryGapResolutionRecord>();
        }

        public bool IsResolved(string gapId)
        {
            if (string.IsNullOrEmpty(gapId) || resolutions == null)
                return false;

            for (int index = 0; index < resolutions.Length; index++)
            {
                if (string.Equals(
                        resolutions[index].gapId,
                        gapId,
                        StringComparison.Ordinal) &&
                    resolutions[index].resolved)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
