using System;
using System.Collections.Generic;
using PaintedAlive.Paint.Ink.Economy;
using PaintedAlive.Painters.Ink;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.EditorTools
{
    public static class ApplyInkPainterBrushAimModeHotfix
    {
        private static readonly string[] SupportedControllerNames =
        {
            "PainterBrushController",
            "PainterPaintMoundController"
        };

        private static readonly string[] CameraPropertyNames =
        {
            "outputCamera",
            "sourceCamera",
            "painterCamera",
            "targetCamera",
            "controlledCamera"
        };

        [MenuItem(
            "Tools/Painted Alive/Milestones/21.2 - Fix Painter Brush Aim")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M21.2 düzeltmesini Play Mode dışında çalıştır.");
                }

                InkPainterRoleAuthority authority =
                    FindExactlyOne<InkPainterRoleAuthority>(
                        "InkPainterRoleAuthority");
                InkPainterIndependentCamera cameraController =
                    FindExactlyOne<InkPainterIndependentCamera>(
                        "InkPainterIndependentCamera");
                InkPainterNestController nestController =
                    FindExactlyOne<InkPainterNestController>(
                        "InkPainterNestController");
                Camera painterCamera = cameraController.ControlledCamera;

                if (painterCamera == null)
                {
                    throw new InvalidOperationException(
                        "M21 Ressam kamerası bulunamadı.");
                }

                MonoBehaviour[] painterControllers =
                    FindPainterAimControllers();

                if (painterControllers.Length == 0)
                {
                    throw new InvalidOperationException(
                        "PainterBrushController bulunamadı. " +
                        "Fırça milestone kurulumunu kontrol et.");
                }

                int reboundCount = 0;

                for (int i = 0; i < painterControllers.Length; i++)
                {
                    MonoBehaviour controller = painterControllers[i];

                    if (TrySetSerializedCamera(
                            controller,
                            painterCamera))
                    {
                        reboundCount++;
                    }
                }

                if (reboundCount == 0)
                {
                    throw new InvalidOperationException(
                        "Fırça denetleyicisinde desteklenen Camera alanı " +
                        "bulunamadı. İlk Diagnose satırını gönder.");
                }

                InkPainterBrushAimModeBridge bridge =
                    authority.GetComponent<InkPainterBrushAimModeBridge>();

                if (bridge == null)
                {
                    bridge =
                        Undo.AddComponent<InkPainterBrushAimModeBridge>(
                            authority.gameObject);
                }

                Undo.RecordObject(bridge, "Configure Painter Aim Bridge");
                bridge.Configure(
                    authority,
                    painterCamera,
                    nestController,
                    painterControllers);

                EditorUtility.SetDirty(bridge);
                EditorUtility.SetDirty(authority);
                EditorUtility.SetDirty(painterCamera);

                for (int i = 0; i < painterControllers.Length; i++)
                {
                    EditorUtility.SetDirty(painterControllers[i]);
                }

                EditorSceneManager.MarkSceneDirty(
                    authority.gameObject.scene);
                AssetDatabase.SaveAssets();

                Debug.Log(
                    "[M21.2] Painter brush aim fixed. " +
                    $"Camera={painterCamera.name}, " +
                    $"BoundControllers={reboundCount}. " +
                    "Default=Brush aim, hold F7=Nest aim. " +
                    "Press Ctrl+S to save the scene.",
                    bridge);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/21.2 - Diagnose Painter Brush Aim")]
        public static void Diagnose()
        {
            InkPainterRoleAuthority[] authorities =
                UnityEngine.Object.FindObjectsByType<
                    InkPainterRoleAuthority>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterIndependentCamera[] cameras =
                UnityEngine.Object.FindObjectsByType<
                    InkPainterIndependentCamera>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterBrushAimModeBridge[] bridges =
                UnityEngine.Object.FindObjectsByType<
                    InkPainterBrushAimModeBridge>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            MonoBehaviour[] controllers = FindPainterAimControllers();

            Debug.Log(
                "[M21.2 Diagnose] " +
                $"Authorities={authorities.Length}, " +
                $"PainterCameras={cameras.Length}, " +
                $"AimBridges={bridges.Length}, " +
                $"PainterAimControllers={controllers.Length}, " +
                $"CursorLock={Cursor.lockState}, " +
                $"CursorVisible={Cursor.visible}, " +
                $"Playing={Application.isPlaying}");

            for (int i = 0; i < controllers.Length; i++)
            {
                MonoBehaviour controller = controllers[i];
                SerializedObject serialized =
                    new SerializedObject(controller);
                string cameraDescription = "No supported Camera field";

                for (int j = 0; j < CameraPropertyNames.Length; j++)
                {
                    SerializedProperty property =
                        serialized.FindProperty(CameraPropertyNames[j]);

                    if (property == null)
                    {
                        continue;
                    }

                    UnityEngine.Object value =
                        property.objectReferenceValue;
                    cameraDescription =
                        $"{property.name}=" +
                        $"{(value != null ? value.name : "NULL")}";
                    break;
                }

                Debug.Log(
                    "[M21.2 Diagnose Controller] " +
                    $"Type={controller.GetType().FullName}, " +
                    $"Path={GetHierarchyPath(controller.transform)}, " +
                    cameraDescription,
                    controller);
            }

            for (int i = 0; i < bridges.Length; i++)
            {
                InkPainterBrushAimModeBridge bridge = bridges[i];
                Debug.Log(
                    "[M21.2 Diagnose Bridge] " +
                    $"Path={GetHierarchyPath(bridge.transform)}, " +
                    $"Mode={bridge.CurrentAimMode}, " +
                    $"BoundControllers={bridge.ReboundControllerCount}",
                    bridge);
            }
        }

        private static T FindExactlyOne<T>(string label)
            where T : UnityEngine.Object
        {
            T[] objects =
                UnityEngine.Object.FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (objects.Length != 1)
            {
                throw new InvalidOperationException(
                    $"M21.2 tam bir {label} bekliyor; " +
                    $"bulunan={objects.Length}.");
            }

            return objects[0];
        }

        private static MonoBehaviour[] FindPainterAimControllers()
        {
            MonoBehaviour[] all =
                UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            List<MonoBehaviour> matches = new List<MonoBehaviour>();

            for (int i = 0; i < all.Length; i++)
            {
                MonoBehaviour candidate = all[i];

                if (candidate == null)
                {
                    continue;
                }

                string typeName = candidate.GetType().Name;

                for (int j = 0;
                    j < SupportedControllerNames.Length;
                    j++)
                {
                    if (typeName != SupportedControllerNames[j])
                    {
                        continue;
                    }

                    matches.Add(candidate);
                    break;
                }
            }

            return matches.ToArray();
        }

        private static bool TrySetSerializedCamera(
            MonoBehaviour controller,
            Camera painterCamera)
        {
            SerializedObject serialized = new SerializedObject(controller);

            for (int i = 0; i < CameraPropertyNames.Length; i++)
            {
                SerializedProperty property =
                    serialized.FindProperty(CameraPropertyNames[i]);

                if (property == null ||
                    property.propertyType !=
                    SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                Undo.RecordObject(
                    controller,
                    "Bind Painter Controller Camera");
                property.objectReferenceValue = painterCamera;
                serialized.ApplyModifiedProperties();
                return true;
            }

            return false;
        }

        private static string GetHierarchyPath(Transform target)
        {
            string path = target.name;

            while (target.parent != null)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }

            return path;
        }
    }
}
