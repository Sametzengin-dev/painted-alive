using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace PaintedAlive.UI.UnifiedHUD
{
    internal static class PrototypeUnifiedHudReflection
    {
        private const BindingFlags Flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        public static Component FindSceneComponentByTypeName(params string[] typeNames)
        {
            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int typeIndex = 0; typeIndex < typeNames.Length; typeIndex++)
            {
                string requested = typeNames[typeIndex];

                for (int index = 0; index < behaviours.Length; index++)
                {
                    MonoBehaviour behaviour = behaviours[index];
                    if (behaviour == null ||
                        !behaviour.gameObject.scene.IsValid())
                    {
                        continue;
                    }

                    Type type = behaviour.GetType();
                    if (string.Equals(type.FullName, requested, StringComparison.Ordinal) ||
                        string.Equals(type.Name, requested, StringComparison.Ordinal))
                    {
                        return behaviour;
                    }
                }
            }

            return null;
        }

        public static Component FindSceneComponentContaining(params string[] nameFragments)
        {
            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int fragmentIndex = 0; fragmentIndex < nameFragments.Length; fragmentIndex++)
            {
                string fragment = nameFragments[fragmentIndex];

                for (int index = 0; index < behaviours.Length; index++)
                {
                    MonoBehaviour behaviour = behaviours[index];
                    if (behaviour == null ||
                        !behaviour.gameObject.scene.IsValid())
                    {
                        continue;
                    }

                    string typeName = behaviour.GetType().Name;
                    if (typeName.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return behaviour;
                    }
                }
            }

            return null;
        }

        public static Component FindRelatedComponentByTypeName(
            Component anchor,
            params string[] typeNames)
        {
            if (anchor == null)
            {
                return null;
            }

            Component found = FindMatchingComponent(
                anchor.GetComponents<Component>(),
                typeNames,
                exactTypeName: true);

            if (found != null)
            {
                return found;
            }

            Transform parent = anchor.transform.parent;
            while (parent != null)
            {
                found = FindMatchingComponent(
                    parent.GetComponents<Component>(),
                    typeNames,
                    exactTypeName: true);

                if (found != null)
                {
                    return found;
                }

                parent = parent.parent;
            }

            return FindMatchingComponent(
                anchor.GetComponentsInChildren<Component>(true),
                typeNames,
                exactTypeName: true);
        }

        public static Component FindRelatedComponentContaining(
            Component anchor,
            params string[] nameFragments)
        {
            if (anchor == null)
            {
                return null;
            }

            Component found = FindMatchingComponent(
                anchor.GetComponents<Component>(),
                nameFragments,
                exactTypeName: false);

            if (found != null)
            {
                return found;
            }

            Transform parent = anchor.transform.parent;
            while (parent != null)
            {
                found = FindMatchingComponent(
                    parent.GetComponents<Component>(),
                    nameFragments,
                    exactTypeName: false);

                if (found != null)
                {
                    return found;
                }

                parent = parent.parent;
            }

            return FindMatchingComponent(
                anchor.GetComponentsInChildren<Component>(true),
                nameFragments,
                exactTypeName: false);
        }

        public static string GetHierarchyPath(Component component)
        {
            if (component == null)
            {
                return "MISSING";
            }

            Transform current = component.transform;
            string path = current.name;

            while (current.parent != null)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }

            return path;
        }

        private static Component FindMatchingComponent(
            IReadOnlyList<Component> components,
            IReadOnlyList<string> names,
            bool exactTypeName)
        {
            for (int nameIndex = 0; nameIndex < names.Count; nameIndex++)
            {
                string requested = names[nameIndex];

                for (int componentIndex = 0;
                     componentIndex < components.Count;
                     componentIndex++)
                {
                    Component component = components[componentIndex];
                    if (component == null)
                    {
                        continue;
                    }

                    Type type = component.GetType();
                    string fullName = type.FullName ?? string.Empty;
                    string shortName = type.Name;

                    bool matches = exactTypeName
                        ? string.Equals(
                              fullName,
                              requested,
                              StringComparison.Ordinal) ||
                          string.Equals(
                              shortName,
                              requested,
                              StringComparison.Ordinal)
                        : shortName.IndexOf(
                              requested,
                              StringComparison.OrdinalIgnoreCase) >= 0;

                    if (matches)
                    {
                        return component;
                    }
                }
            }

            return null;
        }

        public static string ReadString(
            Component component,
            string fallback,
            params string[] memberNames)
        {
            object value = ReadValue(component, memberNames);
            return value != null ? value.ToString() : fallback;
        }

        public static float ReadFloat(
            Component component,
            float fallback,
            params string[] memberNames)
        {
            object value = ReadValue(component, memberNames);
            if (value == null)
            {
                return fallback;
            }

            try
            {
                return Convert.ToSingle(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        public static int ReadInt(
            Component component,
            int fallback,
            params string[] memberNames)
        {
            object value = ReadValue(component, memberNames);
            if (value == null)
            {
                return fallback;
            }

            try
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        public static bool ReadBool(
            Component component,
            bool fallback,
            params string[] memberNames)
        {
            object value = ReadValue(component, memberNames);
            if (value == null)
            {
                return fallback;
            }

            try
            {
                return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        private static object ReadValue(
            Component component,
            IReadOnlyList<string> memberNames)
        {
            if (component == null)
            {
                return null;
            }

            Type type = component.GetType();

            for (int index = 0; index < memberNames.Count; index++)
            {
                string memberName = memberNames[index];

                PropertyInfo property = type.GetProperty(memberName, Flags);
                if (property != null &&
                    property.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        return property.GetValue(component);
                    }
                    catch
                    {
                        // Try the next compatible member.
                    }
                }

                FieldInfo field = type.GetField(memberName, Flags);
                if (field != null)
                {
                    try
                    {
                        return field.GetValue(component);
                    }
                    catch
                    {
                        // Try the next compatible member.
                    }
                }
            }

            return null;
        }
    }
}
