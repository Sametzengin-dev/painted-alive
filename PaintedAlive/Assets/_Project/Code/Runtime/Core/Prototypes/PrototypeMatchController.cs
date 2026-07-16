using System;
using PaintedAlive.Figures;
using PaintedAlive.Paint;
using PaintedAlive.Painters;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Core.Prototypes
{
    public enum PrototypeMatchState
    {
        Waiting,
        Countdown,
        Running,
        FigureEscaped,
        TimeExpired
    }

    public sealed class PrototypeMatchController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PrototypeMatchConfig config;

        [Header("Flow")]
        [SerializeField] private PrototypeRoleSwitcher roleSwitcher;
        [SerializeField] private FigureProgressTracker progressTracker;

        [Header("Figure")]
        [SerializeField] private FigureMotor figureMotor;
        [SerializeField] private Transform spawnPoint;

        [Header("Paint")]
        [SerializeField] private OilStrokeSystem strokeSystem;
        [SerializeField] private PainterPigmentReservoir pigmentReservoir;

        public event Action<PrototypeMatchState> StateChanged;
        public event Action<float> CountdownChanged;
        public event Action<float> TimeChanged;

        public PrototypeMatchState State { get; private set; }
        public float CountdownRemaining { get; private set; }
        public float TimeRemaining { get; private set; }
        public float CompletionTime { get; private set; }

        private void Start()
        {
            BeginNewMatch();
        }

        private void Update()
        {
            if ((State == PrototypeMatchState.FigureEscaped ||
                 State == PrototypeMatchState.TimeExpired) &&
                Keyboard.current != null &&
                Keyboard.current.enterKey.wasPressedThisFrame)
            {
                BeginNewMatch();
                return;
            }

            switch (State)
            {
                case PrototypeMatchState.Countdown:
                    UpdateCountdown();
                    break;

                case PrototypeMatchState.Running:
                    UpdateRunningMatch();
                    break;
            }
        }

        public void BeginNewMatch()
        {
            if (config == null)
            {
                Debug.LogError(
                    $"{nameof(PrototypeMatchController)} requires a config.",
                    this);

                return;
            }

            roleSwitcher?.SelectFigure();
            roleSwitcher?.SetInteractionsLocked(true);

            strokeSystem?.ClearAllStrokes();
            pigmentReservoir?.Refill();
            progressTracker?.ResetProgress();

            if (figureMotor != null && spawnPoint != null)
            {
                figureMotor.Teleport(
                    spawnPoint.position,
                    spawnPoint.rotation);
            }

            TimeRemaining = config.MatchDuration;
            CountdownRemaining = config.CountdownDuration;
            CompletionTime = 0f;

            TimeChanged?.Invoke(TimeRemaining);

            if (CountdownRemaining <= 0f)
            {
                StartRunningMatch();
            }
            else
            {
                SetState(PrototypeMatchState.Countdown);
                CountdownChanged?.Invoke(CountdownRemaining);
            }
        }

        public void NotifyFigureReachedExit()
        {
            if (State != PrototypeMatchState.Running)
            {
                return;
            }

            CompletionTime =
                config.MatchDuration - TimeRemaining;

            roleSwitcher?.SetInteractionsLocked(true);
            SetState(PrototypeMatchState.FigureEscaped);
        }

        private void UpdateCountdown()
        {
            CountdownRemaining = Mathf.Max(
                0f,
                CountdownRemaining - Time.deltaTime);

            CountdownChanged?.Invoke(CountdownRemaining);

            if (CountdownRemaining <= 0f)
            {
                StartRunningMatch();
            }
        }

        private void StartRunningMatch()
        {
            roleSwitcher?.SetInteractionsLocked(false);
            SetState(PrototypeMatchState.Running);
        }

        private void UpdateRunningMatch()
        {
            TimeRemaining = Mathf.Max(
                0f,
                TimeRemaining - Time.deltaTime);

            TimeChanged?.Invoke(TimeRemaining);

            if (TimeRemaining <= 0f)
            {
                roleSwitcher?.SetInteractionsLocked(true);
                SetState(PrototypeMatchState.TimeExpired);
            }
        }

        private void SetState(PrototypeMatchState newState)
        {
            State = newState;
            StateChanged?.Invoke(State);
        }
    }
}
