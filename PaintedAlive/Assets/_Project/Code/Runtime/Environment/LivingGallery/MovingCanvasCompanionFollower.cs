using System;
using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    /// <summary>
    /// Keeps the authored paint-wash overlay and its own straps coherent with
    /// one of the two moving canvases without adding cloth simulation or a hard
    /// collider. The fixed overhead hanger is deliberately not included.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MovingCanvasCompanionFollower : MonoBehaviour,
        ILivingGalleryResettable
    {
        [SerializeField] private MovingCanvasObstacle source;
        [SerializeField] private Transform pivot;
        [SerializeField] private Transform[] followers = Array.Empty<Transform>();

        private Vector3[] initialPositions = Array.Empty<Vector3>();
        private Quaternion[] initialRotations = Array.Empty<Quaternion>();
        private bool initialized;

        public MovingCanvasObstacle Source => source;
        public int FollowerCount => followers != null ? followers.Length : 0;
        public bool IsConfigured => source != null && pivot != null && FollowerCount > 0;

        public void Configure(
            MovingCanvasObstacle sourceCanvas,
            Transform pivotReference,
            Transform[] followerTransforms)
        {
            source = sourceCanvas;
            pivot = pivotReference;
            followers = followerTransforms ?? Array.Empty<Transform>();
            initialized = false;
            CaptureInitial();
        }

        private void Awake() => CaptureInitial();

        private void CaptureInitial()
        {
            if (initialized)
                return;

            initialPositions = new Vector3[followers.Length];
            initialRotations = new Quaternion[followers.Length];
            for (int index = 0; index < followers.Length; index++)
            {
                Transform follower = followers[index];
                if (follower == null)
                    continue;
                initialPositions[index] = follower.position;
                initialRotations[index] = follower.rotation;
            }

            initialized = true;
        }

        private void LateUpdate()
        {
            if (source == null || pivot == null)
                return;

            CaptureInitial();
            ApplyAngle(source.CurrentAngleDegrees);
        }

        private void ApplyAngle(float angle)
        {
            Quaternion delta = Quaternion.AngleAxis(angle, pivot.right);
            for (int index = 0; index < followers.Length; index++)
            {
                Transform follower = followers[index];
                if (follower == null)
                    continue;

                Vector3 offset = initialPositions[index] - pivot.position;
                follower.position = pivot.position + delta * offset;
                follower.rotation = delta * initialRotations[index];
            }
        }

        public void ResetLivingGalleryState()
        {
            if (!initialized)
                return;

            for (int index = 0; index < followers.Length; index++)
            {
                Transform follower = followers[index];
                if (follower != null)
                    follower.SetPositionAndRotation(
                        initialPositions[index],
                        initialRotations[index]);
            }
        }
    }
}
