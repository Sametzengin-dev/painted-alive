using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Core.MatchFlow
{
    [DisallowMultipleComponent]
    public sealed class PrototypeMatchInputLock : MonoBehaviour
    {
        [Serializable]
        private struct SavedBehaviourState
        {
            public Behaviour behaviour;
            public bool wasEnabled;
        }

        [Header("Direct Input Behaviours")]
        [SerializeField] private Behaviour[] controlledBehaviours =
            Array.Empty<Behaviour>();

        [Header("Runtime - Read Only")]
        [SerializeField] private bool inputsLocked;
        [SerializeField] private int capturedStateCount;

        private readonly List<SavedBehaviourState> savedStates = new();

        public bool InputsLocked => inputsLocked;
        public int ControlledBehaviourCount =>
            controlledBehaviours != null ? controlledBehaviours.Length : 0;
        public int CapturedStateCount => capturedStateCount;

        public void Configure(Behaviour[] behaviours)
        {
            controlledBehaviours = behaviours ?? Array.Empty<Behaviour>();
        }

        public void LockInputs()
        {
            if (inputsLocked)
            {
                return;
            }

            savedStates.Clear();

            if (controlledBehaviours != null)
            {
                for (int i = 0; i < controlledBehaviours.Length; i++)
                {
                    Behaviour behaviour = controlledBehaviours[i];
                    if (behaviour == null || behaviour == this)
                    {
                        continue;
                    }

                    savedStates.Add(new SavedBehaviourState
                    {
                        behaviour = behaviour,
                        wasEnabled = behaviour.enabled
                    });
                }
            }

            capturedStateCount = savedStates.Count;

            // Önce bütün durumları yakalayıp sonra kapatıyoruz. Böylece bir rol
            // yöneticisinin OnDisable'ı başka bileşenlerin kaydedilen durumunu bozmaz.
            for (int i = 0; i < savedStates.Count; i++)
            {
                Behaviour behaviour = savedStates[i].behaviour;
                if (behaviour != null)
                {
                    behaviour.enabled = false;
                }
            }

            inputsLocked = true;
        }

        public void UnlockInputs()
        {
            if (!inputsLocked)
            {
                return;
            }

            // Rol yöneticileri önce geri gelir; ardından diğer bileşenler tam olarak
            // kilit öncesindeki açık/kapalı durumuna döndürülür.
            RestoreStates(roleManagersOnly: true);
            RestoreStates(roleManagersOnly: false);

            savedStates.Clear();
            capturedStateCount = 0;
            inputsLocked = false;
        }

        private void RestoreStates(bool roleManagersOnly)
        {
            for (int i = 0; i < savedStates.Count; i++)
            {
                Behaviour behaviour = savedStates[i].behaviour;
                if (behaviour == null)
                {
                    continue;
                }

                bool isRoleManager = IsRoleManager(behaviour);
                if (isRoleManager != roleManagersOnly)
                {
                    continue;
                }

                behaviour.enabled = savedStates[i].wasEnabled;
            }
        }

        private static bool IsRoleManager(Behaviour behaviour)
        {
            string typeName = behaviour.GetType().Name;
            return typeName.IndexOf("RoleSwitcher", StringComparison.Ordinal) >= 0 ||
                   typeName.IndexOf("RoleAuthority", StringComparison.Ordinal) >= 0;
        }

        private void OnDisable()
        {
            UnlockInputs();
        }
    }
}
