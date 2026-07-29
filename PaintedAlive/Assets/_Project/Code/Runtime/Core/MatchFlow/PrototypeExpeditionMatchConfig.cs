using UnityEngine;

namespace PaintedAlive.Core.MatchFlow
{
    [CreateAssetMenu(
        fileName = "PrototypeExpeditionMatchConfig",
        menuName = "Painted Alive/Core/Prototype Expedition Match Config")]
    public sealed class PrototypeExpeditionMatchConfig : ScriptableObject
    {
        [Header("Match Timing")]
        [SerializeField, Min(0f)] private float preparationDuration = 4f;
        [SerializeField, Min(5f)] private float activeDuration = 300f;
        [SerializeField] private bool autoStartOnPlay = true;

        [Header("World Lock")]
        [SerializeField] private bool pauseWorldDuringPreparation = true;
        [SerializeField] private bool pauseWorldWhenCompleted = true;

        [Header("Prototype HUD")]
        [SerializeField] private bool showPrototypeHud = true;
        [SerializeField, Min(420f)] private float resultPanelWidth = 560f;
        [SerializeField] private Color preparationColor = new(0.10f, 0.70f, 0.72f, 1f);
        [SerializeField] private Color activeColor = new(0.95f, 0.82f, 0.32f, 1f);
        [SerializeField] private Color completedColor = new(0.30f, 0.86f, 0.58f, 1f);
        [SerializeField] private Color timeoutColor = new(0.94f, 0.47f, 0.28f, 1f);

        public float PreparationDuration => preparationDuration;
        public float ActiveDuration => activeDuration;
        public bool AutoStartOnPlay => autoStartOnPlay;
        public bool PauseWorldDuringPreparation => pauseWorldDuringPreparation;
        public bool PauseWorldWhenCompleted => pauseWorldWhenCompleted;
        public bool ShowPrototypeHud => showPrototypeHud;
        public float ResultPanelWidth => resultPanelWidth;
        public Color PreparationColor => preparationColor;
        public Color ActiveColor => activeColor;
        public Color CompletedColor => completedColor;
        public Color TimeoutColor => timeoutColor;

        private void OnValidate()
        {
            preparationDuration = Mathf.Max(0f, preparationDuration);
            activeDuration = Mathf.Max(5f, activeDuration);
            resultPanelWidth = Mathf.Max(420f, resultPanelWidth);
        }
    }
}
