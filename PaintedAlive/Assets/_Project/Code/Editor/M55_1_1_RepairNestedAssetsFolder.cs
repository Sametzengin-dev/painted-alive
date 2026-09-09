#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class M55_1_1_RepairNestedAssetsFolder
{
    private const string MenuPath = "Tools/Painted Alive/Milestones/55.1.1 - Repair Nested Assets Folder";
    private const string NestedRoot = "Assets/Assets/_Project";
    private const string CorrectRoot = "Assets/_Project";

    [MenuItem(MenuPath)]
    public static void Repair()
    {
        try
        {
            if (!AssetDatabase.IsValidFolder(NestedRoot))
            {
                EditorUtility.DisplayDialog(
                    "Painted Alive M55.1.1",
                    "Nested folder was not found:\n" + NestedRoot +
                    "\n\nNothing was changed.",
                    "OK");
                return;
            }

            EnsureFolder(CorrectRoot);

            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { NestedRoot });
            List<string> assetPaths = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !string.IsNullOrEmpty(p))
                .Where(p => p.StartsWith(NestedRoot + "/", StringComparison.Ordinal))
                .Where(p => !AssetDatabase.IsValidFolder(p))
                .Distinct()
                .OrderBy(p => p.Count(c => c == '/'))
                .ThenBy(p => p, StringComparer.Ordinal)
                .ToList();

            int moved = 0;
            List<string> conflicts = new List<string>();
            List<string> failures = new List<string>();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string source in assetPaths)
                {
                    string relative = source.Substring(NestedRoot.Length).TrimStart('/');
                    string destination = CorrectRoot + "/" + relative;
                    EnsureParentFolder(destination);

                    if (AssetDatabase.LoadMainAssetAtPath(destination) != null || File.Exists(destination))
                    {
                        conflicts.Add(destination);
                        continue;
                    }

                    string error = AssetDatabase.MoveAsset(source, destination);
                    if (string.IsNullOrEmpty(error))
                        moved++;
                    else
                        failures.Add(source + " -> " + destination + " | " + error);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh();

            // If every remaining item under the nested _Project is only folders, remove that accidental tree.
            string[] remaining = AssetDatabase.FindAssets(string.Empty, new[] { NestedRoot });
            bool hasRemainingFiles = remaining
                .Select(AssetDatabase.GUIDToAssetPath)
                .Any(p => !string.IsNullOrEmpty(p) &&
                          p.StartsWith(NestedRoot + "/", StringComparison.Ordinal) &&
                          !AssetDatabase.IsValidFolder(p));

            if (!hasRemainingFiles)
            {
                AssetDatabase.DeleteAsset(NestedRoot);
                AssetDatabase.Refresh();

                if (AssetDatabase.IsValidFolder("Assets/Assets"))
                {
                    string[] nestedChildren = AssetDatabase.FindAssets(string.Empty, new[] { "Assets/Assets" })
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Where(p => !string.IsNullOrEmpty(p) && p != "Assets/Assets")
                        .ToArray();

                    if (nestedChildren.Length == 0)
                        AssetDatabase.DeleteAsset("Assets/Assets");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string message =
                "Repair completed.\n\n" +
                "Moved assets: " + moved + "\n" +
                "Existing destination conflicts (left untouched): " + conflicts.Count + "\n" +
                "Move failures: " + failures.Count + "\n\n" +
                "Expected production source root:\nAssets/_Project/...\n\n" +
                "After Unity finishes recompiling, run:\n" +
                "Tools > Painted Alive > Milestones >\n" +
                "55.1.1 - Install Production Figure + Mixamo Locomotion";

            if (conflicts.Count > 0)
                message += "\n\nConflicts:\n" + string.Join("\n", conflicts.Take(12));
            if (failures.Count > 0)
                message += "\n\nFailures:\n" + string.Join("\n", failures.Take(12));

            Debug.Log("[Painted Alive M55.1.1 Repair] " + message.Replace("\n", " | "));
            EditorUtility.DisplayDialog("Painted Alive M55.1.1 - Repair", message, "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog(
                "Painted Alive M55.1.1 - Repair Failed",
                ex.Message + "\n\nNo gameplay code was intentionally modified.",
                "OK");
        }
    }

    private static void EnsureParentFolder(string assetPath)
    {
        string folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(folder))
            EnsureFolder(folder);
    }

    private static void EnsureFolder(string folder)
    {
        folder = folder.Replace('\\', '/').TrimEnd('/');
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string[] parts = folder.Split('/');
        if (parts.Length == 0 || parts[0] != "Assets")
            throw new InvalidOperationException("Folder must be inside Assets: " + folder);

        string current = "Assets";
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                string guid = AssetDatabase.CreateFolder(current, parts[i]);
                if (string.IsNullOrEmpty(guid))
                    throw new IOException("Could not create folder: " + next);
            }
            current = next;
        }
    }
}
#endif
