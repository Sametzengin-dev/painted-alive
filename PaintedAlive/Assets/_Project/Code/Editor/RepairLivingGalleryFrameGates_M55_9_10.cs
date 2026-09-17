#if UNITY_EDITOR
using System;
using System.Linq;
using PaintedAlive.Environment.LivingGallery;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RepairLivingGalleryFrameGates_M55_9_10
{
    private const string MenuRoot =
        "Tools/Painted Alive/Milestones/";

    [MenuItem(MenuRoot + "55.9.10 - Repair Living Gallery Frame Gate Bindings")]
    public static void Repair()
    {
        RequireEditMode();

        LivingGalleryGameplayBinder binder =
            FindExactlyOne<LivingGalleryGameplayBinder>();

        if (binder.MapRoot == null ||
            binder.VisualRoot == null ||
            binder.CollisionRoot == null ||
            binder.GameplayRoot == null)
        {
            throw new InvalidOperationException(
                "LivingGalleryGameplayBinder root reference eksik.");
        }

        float[] heights = { 6.0f, 6.1f, 5.7f };
        int repaired = 0;

        for (int index = 1; index <= 3; index++)
        {
            string suffix = "0" + index;
            string id = "FRAME_" + suffix;

            RotatingFrameGate gate =
                binder.MapRoot
                    .GetComponentsInChildren<RotatingFrameGate>(true)
                    .Single(candidate =>
                        candidate != null &&
                        string.Equals(
                            candidate.BindingId,
                            id,
                            StringComparison.Ordinal));

            Transform visual =
                FindExact(
                    binder.VisualRoot,
                    "INT_FrameGate_" + suffix);

            Transform collision =
                FindExact(
                    binder.CollisionRoot,
                    "INT_FrameGate_" + suffix);

            Transform pivot =
                FindExact(
                    binder.GameplayRoot,
                    "GP_FrameGate_" + suffix + "_Pivot");

            Transform clearance =
                FindExact(
                    binder.GameplayRoot,
                    "GP_FrameGate_" + suffix + "_Clearance");

            Transform movingCollision =
                FindExact(
                    binder.CollisionRoot,
                    "COLLISION_INT_FrameGate_" +
                    suffix +
                    "_OpaqueArtwork");

            Collider movingCollider =
                movingCollision.GetComponent<Collider>();

            if (movingCollider == null)
            {
                throw new InvalidOperationException(
                    id + " moving collision object has no Collider.");
            }

            Undo.RecordObject(
                gate,
                "Repair Living Gallery Frame Gate Binding");

            gate.Configure(
                id,
                visual,
                collision,
                pivot,
                clearance,
                movingCollider,
                0f,
                90f,
                1.5f,
                1.25f,
                2.54f,
                heights[index - 1]);

            gate.ConfigurePainterInitialState(
                RotatingFrameGateState.Open);

            EditorUtility.SetDirty(gate);
            repaired++;
        }

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        AssetDatabase.SaveAssets();

        Debug.Log(
            "[M55.9.10 Living Gallery Gate Repair]\n" +
            $"Repaired={repaired}/3\n" +
            "Exact visual/collision/pivot/clearance/moving-collider " +
            "references were rebound from authoritative imported roots.\n" +
            "Now rerun M55.9.9 Diagnose.");

        Diagnose();
    }

    [MenuItem(MenuRoot + "55.9.10 - Diagnose Living Gallery Frame Gate Bindings")]
    public static void Diagnose()
    {
        LivingGalleryGameplayBinder binder =
            FindOptional<LivingGalleryGameplayBinder>();

        if (binder == null || binder.MapRoot == null)
        {
            Debug.LogError(
                "[M55.9.10] LivingGalleryGameplayBinder/MapRoot missing.");
            return;
        }

        RotatingFrameGate[] gates =
            binder.MapRoot
                .GetComponentsInChildren<RotatingFrameGate>(true)
                .Where(gate => gate != null)
                .OrderBy(gate => gate.BindingId)
                .ToArray();

        int configured = 0;
        int pivotPreserving = 0;
        int hardErrors = 0;

        string[] details = new string[gates.Length];

        for (int index = 0; index < gates.Length; index++)
        {
            RotatingFrameGate gate = gates[index];
            SerializedObject serialized =
                new SerializedObject(gate);

            bool visual =
                HasReference(serialized, "visualGateRoot");
            bool collision =
                HasReference(serialized, "collisionGateRoot");
            bool pivot =
                HasReference(serialized, "pivot");
            bool clearance =
                HasReference(serialized, "clearanceReference");
            bool moving =
                HasReference(serialized, "movingCollider");

            if (gate.IsConfigured)
                configured++;
            else
                hardErrors++;

            if (gate.UsesPivotPreservingRotation)
                pivotPreserving++;
            else
                hardErrors++;

            details[index] =
                $"{gate.BindingId}: " +
                $"Configured={gate.IsConfigured}, " +
                $"PivotPreserving={gate.UsesPivotPreservingRotation}, " +
                $"Visual={visual}, Collision={collision}, " +
                $"Pivot={pivot}, Clearance={clearance}, " +
                $"MovingCollider={moving}";
        }

        if (gates.Length != 3)
            hardErrors++;

        string result =
            gates.Length == 3 &&
            configured == 3 &&
            pivotPreserving == 3 &&
            hardErrors == 0
                ? "PASS"
                : "NEEDS_ATTENTION";

        string report =
            "Painted Alive M55.9.10 — Living Gallery Frame Gate Bindings\n" +
            "Result=" + result + "\n\n" +
            $"FrameGates={gates.Length}/3\n" +
            $"Configured={configured}/3\n" +
            $"PivotPreserving={pivotPreserving}/3\n" +
            $"HardErrors={hardErrors}\n\n" +
            string.Join("\n", details);

        Debug.Log(
            "[M55.9.10 Living Gallery Gate Diagnose]\n" +
            report,
            binder);

        EditorUtility.DisplayDialog(
            "M55.9.10 Living Gallery Frame Gates",
            report,
            "Tamam");
    }

    private static bool HasReference(
        SerializedObject serialized,
        string propertyName)
    {
        SerializedProperty property =
            serialized.FindProperty(propertyName);

        return property != null &&
            property.objectReferenceValue != null;
    }

    private static Transform FindExact(
        Transform root,
        string exactName)
    {
        Transform[] matches =
            root.GetComponentsInChildren<Transform>(true)
                .Where(item =>
                    item != null &&
                    string.Equals(
                        item.name,
                        exactName,
                        StringComparison.Ordinal))
                .ToArray();

        if (matches.Length != 1)
        {
            throw new InvalidOperationException(
                exactName +
                " expected exactly 1, found " +
                matches.Length + ".");
        }

        return matches[0];
    }

    private static T FindExactlyOne<T>()
        where T : Component
    {
        T[] matches =
            UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .Where(item =>
                item != null &&
                !EditorUtility.IsPersistent(item) &&
                item.gameObject.scene.IsValid())
            .ToArray();

        if (matches.Length != 1)
        {
            throw new InvalidOperationException(
                typeof(T).Name +
                " expected exactly 1, found " +
                matches.Length + ".");
        }

        return matches[0];
    }

    private static T FindOptional<T>()
        where T : Component
    {
        return
            UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(item =>
                item != null &&
                !EditorUtility.IsPersistent(item) &&
                item.gameObject.scene.IsValid());
    }

    private static void RequireEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException(
                "M55.9.10 repair Play Mode kapalıyken çalıştırılmalıdır.");
        }
    }
}
#endif
