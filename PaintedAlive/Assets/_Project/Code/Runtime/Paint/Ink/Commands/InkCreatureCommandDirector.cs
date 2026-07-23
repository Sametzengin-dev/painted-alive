using System.Collections.Generic;
using PaintedAlive.Figures;
using PaintedAlive.Paint.Ink.Economy;
using PaintedAlive.Painters.Ink;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PaintedAlive.Paint.Ink.Commands
{
    [DefaultExecutionOrder(-25000)]
    [DisallowMultipleComponent]
    public sealed class InkCreatureCommandDirector : MonoBehaviour
    {
        private const int MaximumSurfaceHits = 32;
        private const int MarkerSegments = 40;

        private readonly RaycastHit[] surfaceHits =
            new RaycastHit[MaximumSurfaceHits];
        private readonly List<FigureMotor> cachedFigures = new();

        [Header("References")]
        [SerializeField]
        private InkPainterRoleAuthority roleAuthority;

        [SerializeField]
        private InkSystemManager inkManager;

        [SerializeField]
        private InkPainterNestController nestController;

        [SerializeField]
        private InkPainterEconomy economy;

        [SerializeField]
        private Camera painterCamera;

        [Header("Targeting")]
        [SerializeField, Min(5f)]
        private float commandRange = 70f;

        [SerializeField]
        private LayerMask surfaceMask = Physics.DefaultRaycastLayers;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private Vector3 lastCommandPoint;

        [SerializeField]
        private int lastCommandedCount;

        [SerializeField]
        private int activeCommandCount;

        [SerializeField]
        private int commandSequence;

        [SerializeField]
        private string lastResult = "F5: issue brush-point order";

        private LineRenderer marker;
        private Material markerMaterial;
        private float markerHideAt;
        private float nextFigureRefreshTime;
        private float nextCountRefreshTime;

        public Vector3 LastCommandPoint => lastCommandPoint;
        public int LastCommandedCount => lastCommandedCount;
        public int ActiveCommandCount => activeCommandCount;
        public int CommandSequence => commandSequence;
        public string LastResult => lastResult;

        private void Awake()
        {
            if (inkManager == null)
            {
                inkManager = InkSystemManager.ActiveInstance;
            }

            if (economy == null)
            {
                economy = InkPainterEconomy.ActiveInstance;
            }

            if (painterCamera == null && nestController != null)
            {
                painterCamera = nestController.PainterCamera;
            }

            EnsureMarker();
            SetMarkerVisible(false);
        }

        private void OnDisable()
        {
            SetMarkerVisible(false);
        }

        private void OnDestroy()
        {
            if (markerMaterial != null)
            {
                Destroy(markerMaterial);
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (marker != null &&
                marker.gameObject.activeSelf &&
                Time.unscaledTime >= markerHideAt)
            {
                SetMarkerVisible(false);
            }

            if (Time.unscaledTime >= nextFigureRefreshTime)
            {
                RefreshFigureCache();
                nextFigureRefreshTime = Time.unscaledTime + 0.3f;
            }

            if (Time.unscaledTime >= nextCountRefreshTime)
            {
                RefreshActiveCommandCount();
                nextCountRefreshTime = Time.unscaledTime + 0.15f;
            }

            if (keyboard.f6Key.wasPressedThisFrame)
            {
                ReleaseAllCommands("Released for possession");
                return;
            }

            if (roleAuthority == null ||
                !roleAuthority.IsInkPainter ||
                IsEditingText())
            {
                return;
            }

            if (!keyboard.f5Key.wasPressedThisFrame)
            {
                return;
            }

            bool release =
                keyboard.leftShiftKey.isPressed ||
                keyboard.rightShiftKey.isPressed;

            if (release)
            {
                ReleaseAllCommands("Returned to autonomous AI");
                return;
            }

            if ((nestController != null && nestController.IsCasting) ||
                (economy != null && economy.PossessionActive))
            {
                lastResult = "Command blocked by active Ink action";
                return;
            }

            TryIssueCommand();
        }

        public void Configure(
            InkPainterRoleAuthority authority,
            InkSystemManager manager,
            InkPainterNestController painterNestController,
            InkPainterEconomy painterEconomy,
            Camera sourceCamera)
        {
            roleAuthority = authority;
            inkManager = manager;
            nestController = painterNestController;
            economy = painterEconomy;
            painterCamera = sourceCamera;
        }

        public FigureMotor FindNearestFigure(
            Vector3 point,
            float maximumDistance)
        {
            FigureMotor nearest = null;
            float nearestSquared =
                Mathf.Max(0f, maximumDistance) *
                Mathf.Max(0f, maximumDistance);

            for (int i = 0; i < cachedFigures.Count; i++)
            {
                FigureMotor figure = cachedFigures[i];

                if (figure == null || !figure.isActiveAndEnabled)
                {
                    continue;
                }

                float distanceSquared =
                    (figure.transform.position - point).sqrMagnitude;

                if (distanceSquared >= nearestSquared)
                {
                    continue;
                }

                nearestSquared = distanceSquared;
                nearest = figure;
            }

            return nearest;
        }

        public void ReleaseAllCommands(string reason)
        {
            InkCreatureCommandAgent[] agents =
                Object.FindObjectsByType<InkCreatureCommandAgent>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            int released = 0;

            for (int i = 0; i < agents.Length; i++)
            {
                InkCreatureCommandAgent agent = agents[i];

                if (agent != null && agent.IsCommanded)
                {
                    agent.CancelCommand(reason);
                    released++;
                }
            }

            activeCommandCount = 0;
            lastResult = released > 0
                ? $"{released} creature(s) autonomous"
                : "No active creature order";
            SetMarkerVisible(false);
        }

        private void TryIssueCommand()
        {
            if (inkManager == null)
            {
                inkManager = InkSystemManager.ActiveInstance;
            }

            if (painterCamera == null && nestController != null)
            {
                painterCamera = nestController.PainterCamera;
            }

            if (inkManager == null || painterCamera == null)
            {
                lastResult = "Ink manager or Painter camera missing";
                return;
            }

            if (!TryResolveTarget(
                    out Vector3 point,
                    out Vector3 normal))
            {
                lastResult = "No valid command surface";
                return;
            }

            commandSequence++;
            int assigned = 0;
            IReadOnlyList<InkCreatureRuntime> creatures =
                inkManager.ActiveCreatures;

            for (int i = 0; i < creatures.Count; i++)
            {
                InkCreatureRuntime creature = creatures[i];

                if (creature == null || !creature.IsInitialized)
                {
                    continue;
                }

                InkCreatureCommandAgent agent =
                    creature.GetComponent<InkCreatureCommandAgent>();

                if (agent == null)
                {
                    agent =
                        creature.gameObject.AddComponent<
                            InkCreatureCommandAgent>();
                }

                if (agent.AssignCommand(
                        this,
                        point,
                        normal,
                        commandSequence))
                {
                    assigned++;
                }
            }

            lastCommandPoint = point;
            lastCommandedCount = assigned;
            activeCommandCount = assigned;
            lastResult = assigned > 0
                ? $"{assigned} creature(s) ordered"
                : "No commandable Eye + Foot creature";

            if (assigned > 0)
            {
                ShowMarker(point, normal);
            }
        }

        private bool TryResolveTarget(
            out Vector3 point,
            out Vector3 normal)
        {
            point = Vector3.zero;
            normal = Vector3.up;
            Ray ray = new Ray(
                painterCamera.transform.position,
                painterCamera.transform.forward);
            int count = Physics.RaycastNonAlloc(
                ray,
                surfaceHits,
                commandRange,
                surfaceMask,
                QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            RaycastHit best = default;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = surfaceHits[i];
                Collider hitCollider = hit.collider;

                if (hitCollider == null ||
                    hit.distance >= nearest ||
                    hitCollider.GetComponentInParent<
                        InkCreatureRuntime>() != null ||
                    hitCollider.GetComponentInParent<
                        FigureMotor>() != null)
                {
                    continue;
                }

                nearest = hit.distance;
                best = hit;
            }

            if (best.collider == null)
            {
                return false;
            }

            point = best.point;
            normal = best.normal.sqrMagnitude > 0.001f
                ? best.normal.normalized
                : Vector3.up;
            return true;
        }

        private void RefreshFigureCache()
        {
            FigureMotor[] figures =
                Object.FindObjectsByType<FigureMotor>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            cachedFigures.Clear();

            for (int i = 0; i < figures.Length; i++)
            {
                FigureMotor figure = figures[i];

                if (figure != null && figure.isActiveAndEnabled)
                {
                    cachedFigures.Add(figure);
                }
            }
        }

        private void RefreshActiveCommandCount()
        {
            InkCreatureCommandAgent[] agents =
                Object.FindObjectsByType<InkCreatureCommandAgent>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            int count = 0;

            for (int i = 0; i < agents.Length; i++)
            {
                if (agents[i] != null && agents[i].IsCommanded)
                {
                    count++;
                }
            }

            activeCommandCount = count;
        }

        private void EnsureMarker()
        {
            if (marker != null)
            {
                return;
            }

            Transform existing = transform.Find("M23_CommandMarker");
            GameObject markerObject = existing != null
                ? existing.gameObject
                : new GameObject("M23_CommandMarker");

            if (existing == null)
            {
                markerObject.transform.SetParent(transform, false);
            }

            marker = markerObject.GetComponent<LineRenderer>();

            if (marker == null)
            {
                marker = markerObject.AddComponent<LineRenderer>();
            }

            Shader shader =
                Shader.Find("Sprites/Default");

            if (shader != null)
            {
                markerMaterial = new Material(shader);
                markerMaterial.color =
                    new Color(0.72f, 0.12f, 0.86f, 0.92f);
                marker.sharedMaterial = markerMaterial;
            }

            marker.useWorldSpace = false;
            marker.loop = true;
            marker.positionCount = MarkerSegments;
            marker.startWidth = 0.055f;
            marker.endWidth = 0.055f;
            marker.startColor =
                new Color(0.82f, 0.18f, 0.95f, 0.95f);
            marker.endColor =
                new Color(0.34f, 0.02f, 0.52f, 0.95f);
            marker.numCornerVertices = 2;

            for (int i = 0; i < MarkerSegments; i++)
            {
                float angle =
                    (Mathf.PI * 2f * i) / MarkerSegments;
                marker.SetPosition(
                    i,
                    new Vector3(
                        Mathf.Cos(angle) * 0.62f,
                        0f,
                        Mathf.Sin(angle) * 0.62f));
            }
        }

        private void ShowMarker(Vector3 point, Vector3 normal)
        {
            EnsureMarker();
            marker.transform.position = point + normal * 0.035f;
            marker.transform.rotation = Quaternion.FromToRotation(
                Vector3.up,
                normal);
            markerHideAt = Time.unscaledTime + 2.2f;
            SetMarkerVisible(true);
        }

        private void SetMarkerVisible(bool visible)
        {
            if (marker != null &&
                marker.gameObject.activeSelf != visible)
            {
                marker.gameObject.SetActive(visible);
            }
        }

        private static bool IsEditingText()
        {
            GameObject selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;

            return selected != null &&
                (selected.GetComponent("TMP_InputField") != null ||
                 selected.GetComponent("InputField") != null);
        }
    }
}
