using System;
using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    [DisallowMultipleComponent]
    public sealed class LivingGalleryGameplayBinder : MonoBehaviour
    {
        [SerializeField] private Transform mapRoot;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform collisionRoot;
        [SerializeField] private Transform gameplayRoot;
        [SerializeField] private Transform entryAnchor;
        [SerializeField] private Transform exitAnchor;
        [SerializeField] private Transform[] routeGuides = Array.Empty<Transform>();
        [SerializeField] private string[] authoredGaps = Array.Empty<string>();

        public Transform MapRoot => mapRoot;
        public Transform VisualRoot => visualRoot;
        public Transform CollisionRoot => collisionRoot;
        public Transform GameplayRoot => gameplayRoot;
        public Transform EntryAnchor => entryAnchor;
        public Transform ExitAnchor => exitAnchor;
        public Transform[] RouteGuides => routeGuides;
        public string[] AuthoredGaps => authoredGaps;

        public void Configure(
            Transform newMapRoot,
            Transform newVisualRoot,
            Transform newCollisionRoot,
            Transform newGameplayRoot,
            Transform newEntryAnchor,
            Transform newExitAnchor,
            Transform[] newRouteGuides,
            string[] newAuthoredGaps)
        {
            mapRoot = newMapRoot;
            visualRoot = newVisualRoot;
            collisionRoot = newCollisionRoot;
            gameplayRoot = newGameplayRoot;
            entryAnchor = newEntryAnchor;
            exitAnchor = newExitAnchor;
            routeGuides = newRouteGuides ?? Array.Empty<Transform>();
            authoredGaps = newAuthoredGaps ?? Array.Empty<string>();
        }
    }
}
