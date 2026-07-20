using System;
using PaintedAlive.Figures.Tools;
using PaintedAlive.Paint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.EditorTools
{
    public static class SetupFrameGunSurfaceAnchorsMilestone
    {
        private const string ConfigPath =
            "Assets/_Project/Data/Figures/FrameGunConfig.asset";

        [MenuItem(
            "Tools/Painted Alive/Milestones/10 - Upgrade Surface Aware Anchors")]
        public static void Run()
        {
            try
            {
                ValidateRequiredApi();

                FrameGunController[] frameGuns =
                    UnityEngine.Object.FindObjectsByType<
                        FrameGunController>(
                            FindObjectsInactive.Include,
                            FindObjectsSortMode.None);

                if (frameGuns.Length == 0)
                {
                    throw new InvalidOperationException(
                        "Sahnede FrameGunController bulunamadı. " +
                        "Önce çalışan Milestone 09 sahnesini aç.");
                }

                FrameGunConfig config =
                    AssetDatabase.LoadAssetAtPath<FrameGunConfig>(
                        ConfigPath);

                if (config == null)
                {
                    config = GetReference<FrameGunConfig>(
                        frameGuns[0],
                        "config");
                }

                if (config == null)
                {
                    throw new InvalidOperationException(
                        "FrameGunConfig bulunamadı. Milestone 09 " +
                        "kurulumunu yeniden çalıştır.");
                }

                config.EnsureSurfaceAnchorDefaults();
                EditorUtility.SetDirty(config);

                foreach (FrameGunController frameGun in frameGuns)
                {
                    ConfigureFrameGun(frameGun, config);
                    EditorSceneManager.MarkSceneDirty(
                        frameGun.gameObject.scene);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[Milestone 10] Yüzeye duyarlı ankrajlar " +
                    $"kuruldu. {frameGuns.Length} Çerçeve " +
                    "Tabancası güncellendi. Test: Islak stroke'a " +
                    "bağlan, halatı ger ve kaymayı gözlemle; " +
                    "Fiksatif ile tutuşu kilitle.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static void ConfigureFrameGun(
            FrameGunController frameGun,
            FrameGunConfig config)
        {
            FrameGunRopeVisual ropeVisual =
                frameGun.GetComponentInChildren<
                    FrameGunRopeVisual>(true);

            FrameGunFeedback feedback =
                frameGun.GetComponent<FrameGunFeedback>();

            if (ropeVisual == null || feedback == null)
            {
                throw new InvalidOperationException(
                    $"{frameGun.name} üzerinde Milestone 09 " +
                    "halat veya feedback bileşeni eksik. Önce " +
                    "09 - Setup Frame Gun Rope menüsünü çalıştır.");
            }

            SetReference(frameGun, "config", config);
            SetReference(frameGun, "ropeVisual", ropeVisual);
            SetReference(frameGun, "feedback", feedback);

            ropeVisual.Configure(config);
            ConfigureRopeMaterial(ropeVisual);

            if (!Application.isPlaying)
            {
                ropeVisual.SetVisible(false);
            }

            EditorUtility.SetDirty(frameGun);
            EditorUtility.SetDirty(ropeVisual);
            EditorUtility.SetDirty(feedback);
        }

        private static void ConfigureRopeMaterial(
            FrameGunRopeVisual ropeVisual)
        {
            LineRenderer lineRenderer =
                ropeVisual.GetComponent<LineRenderer>();

            if (lineRenderer == null ||
                lineRenderer.sharedMaterial == null)
            {
                return;
            }

            Material material = lineRenderer.sharedMaterial;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            EditorUtility.SetDirty(material);
        }

        private static void ValidateRequiredApi()
        {
            if (typeof(OilStrokeStructuralIntegrity).GetMethod(
                    "ApplyExternalDamage") == null)
            {
                throw new InvalidOperationException(
                    "OilStrokeStructuralIntegrity." +
                    "ApplyExternalDamage bulunamadı. M10 " +
                    "replacement dosyasını kopyala.");
            }

            if (typeof(FrameGunController).GetProperty(
                    "AnchorSurfaceType") == null)
            {
                throw new InvalidOperationException(
                    "Güncel FrameGunController bulunamadı. M10 " +
                    "replacement dosyasını kopyala.");
            }
        }

        private static T GetReference<T>(
            UnityEngine.Object target,
            string propertyName)
            where T : UnityEngine.Object
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{propertyName} " +
                    "alanı bulunamadı.");
            }

            return property.objectReferenceValue as T;
        }

        private static void SetReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{propertyName} " +
                    "alanı bulunamadı. Replacement dosyalarının " +
                    "tamamını kopyaladığını kontrol et.");
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
