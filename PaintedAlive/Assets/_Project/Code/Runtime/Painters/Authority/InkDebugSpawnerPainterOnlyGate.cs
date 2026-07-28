using System;
using System.Reflection;
using UnityEngine;

namespace PaintedAlive.Painters.Authority
{
    [DefaultExecutionOrder(-9000)]
    [DisallowMultipleComponent]
    public sealed class InkDebugSpawnerPainterOnlyGate : MonoBehaviour
    {
        private const BindingFlags ReflectionFlags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        [Header("Role Source")]
        [SerializeField] private MonoBehaviour roleAuthority;
        [SerializeField] private GameObject[] painterRoleObjects = Array.Empty<GameObject>();

        [Header("Painter-Only F9 Debug Spawners")]
        [SerializeField] private Behaviour[] debugSpawnerBehaviours = Array.Empty<Behaviour>();

        [Header("Runtime - Read Only")]
        [SerializeField] private bool isPainterRole;
        [SerializeField] private int controlledSpawnerCount;

        public bool IsPainterRole => isPainterRole;
        public int ControlledSpawnerCount => controlledSpawnerCount;

        public void Configure(
            MonoBehaviour authority,
            Behaviour[] spawners,
            GameObject[] painterObjects)
        {
            roleAuthority = authority;
            debugSpawnerBehaviours = spawners ?? Array.Empty<Behaviour>();
            painterRoleObjects = painterObjects ?? Array.Empty<GameObject>();
            ApplyAuthority();
        }

        private void Awake()
        {
            ApplyAuthority();
        }

        private void OnEnable()
        {
            ApplyAuthority();
        }

        private void Update()
        {
            // Role switch input is usually evaluated during Update. This early pass
            // protects the normal frame; LateUpdate below seals the new role state.
            ApplyAuthority();
        }

        private void LateUpdate()
        {
            ApplyAuthority();
        }

        private void ApplyAuthority()
        {
            isPainterRole = ResolvePainterRole();
            controlledSpawnerCount = 0;

            if (debugSpawnerBehaviours == null)
            {
                return;
            }

            for (int i = 0; i < debugSpawnerBehaviours.Length; i++)
            {
                Behaviour spawner = debugSpawnerBehaviours[i];
                if (spawner == null || spawner == this)
                {
                    continue;
                }

                controlledSpawnerCount++;
                if (spawner.enabled != isPainterRole)
                {
                    spawner.enabled = isPainterRole;
                }
            }
        }

        private bool ResolvePainterRole()
        {
            if (roleAuthority != null)
            {
                if (TryReadBooleanRole(roleAuthority, out bool painterFromBoolean))
                {
                    return painterFromBoolean;
                }

                if (TryReadNamedRole(roleAuthority, out bool painterFromRoleName))
                {
                    return painterFromRoleName;
                }

                if (TryReadActiveRoleCamera(roleAuthority, out bool painterFromCamera))
                {
                    return painterFromCamera;
                }
            }

            if (painterRoleObjects != null)
            {
                for (int i = 0; i < painterRoleObjects.Length; i++)
                {
                    GameObject painterObject = painterRoleObjects[i];
                    if (painterObject != null && painterObject.activeInHierarchy)
                    {
                        return true;
                    }
                }
            }

            // Fail closed: if role cannot be identified, F9 must not create a
            // Painter-owned creature from Figure or Stain control.
            return false;
        }

        private static bool TryReadBooleanRole(
            MonoBehaviour authority,
            out bool isPainter)
        {
            string[] names =
            {
                "IsPainter",
                "IsPainterRole",
                "IsPainterActive",
                "PainterActive",
                "HasPainterAuthority"
            };

            Type type = authority.GetType();
            for (int i = 0; i < names.Length; i++)
            {
                if (TryReadBoolMember(authority, type, names[i], out isPainter))
                {
                    return true;
                }
            }

            isPainter = false;
            return false;
        }

        private static bool TryReadNamedRole(
            MonoBehaviour authority,
            out bool isPainter)
        {
            string[] names =
            {
                "CurrentRole",
                "currentRole",
                "ActiveRole",
                "activeRole",
                "Role",
                "role"
            };

            Type type = authority.GetType();
            for (int i = 0; i < names.Length; i++)
            {
                if (!TryReadMember(authority, type, names[i], out object value) ||
                    value == null)
                {
                    continue;
                }

                string roleName = value.ToString();
                if (string.IsNullOrEmpty(roleName))
                {
                    continue;
                }

                if (roleName.IndexOf("Painter", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isPainter = true;
                    return true;
                }

                if (roleName.IndexOf("Figure", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    roleName.IndexOf("Stain", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isPainter = false;
                    return true;
                }
            }

            isPainter = false;
            return false;
        }

        private static bool TryReadActiveRoleCamera(
            MonoBehaviour authority,
            out bool isPainter)
        {
            string[] names =
            {
                "ActiveRoleCamera",
                "activeRoleCamera",
                "CurrentCamera",
                "currentCamera"
            };

            Type type = authority.GetType();
            for (int i = 0; i < names.Length; i++)
            {
                if (!TryReadMember(authority, type, names[i], out object value) ||
                    value == null)
                {
                    continue;
                }

                UnityEngine.Object cameraObject = value as UnityEngine.Object;
                if (cameraObject == null)
                {
                    continue;
                }

                string objectName = cameraObject.name;
                isPainter = objectName.IndexOf(
                    "Painter",
                    StringComparison.OrdinalIgnoreCase) >= 0;
                return true;
            }

            isPainter = false;
            return false;
        }

        private static bool TryReadBoolMember(
            object target,
            Type type,
            string name,
            out bool value)
        {
            if (TryReadMember(target, type, name, out object raw) && raw is bool result)
            {
                value = result;
                return true;
            }

            value = false;
            return false;
        }

        private static bool TryReadMember(
            object target,
            Type type,
            string name,
            out object value)
        {
            value = null;

            try
            {
                PropertyInfo property = type.GetProperty(name, ReflectionFlags);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    value = property.GetValue(target);
                    return true;
                }

                FieldInfo field = type.GetField(name, ReflectionFlags);
                if (field != null)
                {
                    value = field.GetValue(target);
                    return true;
                }
            }
            catch (Exception)
            {
                value = null;
            }

            return false;
        }
    }
}
