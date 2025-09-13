#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using System.Linq;

public static class ScaffoldProject
{
    [MenuItem("Tools/Scaffold/Generate Clean Structure")]
    public static void Generate()
    {
        string[] dirs = {
            "Assets/Scripts/Core/Input",
            "Assets/Scripts/Core/Files",
            "Assets/Scripts/Core/Utils",
            "Assets/Scripts/Features/ImageStage/Domain",
            "Assets/Scripts/Features/ImageStage/Application",
            "Assets/Scripts/Features/ImageStage/Presentation",
            "Assets/Scripts/DevOnly",
            "Assets/Prefabs",
            "Assets/ScriptableObjects"
        };
        foreach (var d in dirs) Directory.CreateDirectory(d);

        // ── 空C#作成（存在すればスキップ）
        CreateIfNone("Assets/Scripts/Core/Input/IInputService.cs",          "public interface IInputService {}");
        CreateIfNone("Assets/Scripts/Core/Input/UnityInputService.cs",      "using UnityEngine; public class UnityInputService : MonoBehaviour {}");
        CreateIfNone("Assets/Scripts/Core/Files/IImageMover.cs",            "public interface IImageMover {}");
        CreateIfNone("Assets/Scripts/Core/Files/LocalImageMover.cs",        "public class LocalImageMover {}");
        CreateIfNone("Assets/Scripts/Features/ImageStage/Domain/ConfirmQueue.cs",  "public class ConfirmQueue {}");
        CreateIfNone("Assets/Scripts/Features/ImageStage/Domain/TwoStageTimer.cs", "public class TwoStageTimer {}");
        CreateIfNone("Assets/Scripts/Features/ImageStage/Application/ConfirmFlowController.cs", "public class ConfirmFlowController {}");
        CreateIfNone("Assets/Scripts/Features/ImageStage/Application/ImageStageSettings.cs",
@"using UnityEngine;
[CreateAssetMenu(menuName=""Settings/ImageStage"")]
public class ImageStageSettings : ScriptableObject {}");
        CreateIfNone("Assets/Scripts/Features/ImageStage/Presentation/ImageStageDriver.cs",     "using UnityEngine; public class ImageStageDriver : MonoBehaviour {}");
        CreateIfNone("Assets/Scripts/Features/ImageStage/Presentation/PreviewView.cs",          "using UnityEngine; public class PreviewView : MonoBehaviour {}");
        CreateIfNone("Assets/Scripts/Features/ImageStage/Presentation/StatusView.cs",           "using UnityEngine; public class StatusView : MonoBehaviour {}");
        CreateIfNone("Assets/Scripts/Features/ImageStage/Presentation/TimelineSpawnerAdapter.cs","using UnityEngine; public class TimelineSpawnerAdapter : MonoBehaviour {}");
        CreateIfNone("Assets/Scripts/DevOnly/DebugOverlay.cs",                                  "using UnityEngine; public class DebugOverlay : MonoBehaviour {}");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── コンパイル完了後にSO/Prefabを作る（型がロードされてから）
        EditorApplication.delayCall += CreateAssetsAfterCompile;
        EditorUtility.DisplayDialog("Scaffold", "Files created. Compiling... The asset/prefab creation will run right after compile.", "OK");
    }

    static void CreateAssetsAfterCompile()
    {
        // ImageStageSettings.asset
        var assetPath = "Assets/ScriptableObjects/ImageStageSettings.asset";
        if (!File.Exists(assetPath))
        {
            var t = AppDomain.CurrentDomain.GetAssemblies()
                     .SelectMany(a => {
                         Type[] ts; try { ts = a.GetTypes(); } catch { ts = Type.EmptyTypes; }
                         return ts;
                     })
                     .FirstOrDefault(x => x.Name == "ImageStageSettings" && typeof(ScriptableObject).IsAssignableFrom(x));

            if (t != null)
            {
                var inst = ScriptableObject.CreateInstance(t);
                AssetDatabase.CreateAsset(inst, assetPath);
            }
            else
            {
                Debug.LogWarning("ImageStageSettings type is not loaded yet. Run Tools > Scaffold > Generate Clean Structure again after compile.");
            }
        }

        // 空Prefab（存在しなければ作る）
        CreateEmptyPrefab("Assets/Prefabs/ImageDisplaySub.prefab", "ImageDisplaySub");
        CreateEmptyPrefab("Assets/Prefabs/TimelineDisplay.prefab", "TimelineDisplay");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void CreateIfNone(string path, string body)
    {
        if (File.Exists(path)) return;
        File.WriteAllText(path, "// Auto-generated\n" + body);
    }

    static void CreateEmptyPrefab(string path, string goName)
    {
        if (File.Exists(path)) return;
        var go = new GameObject(goName);
        PrefabUtility.SaveAsPrefabAsset(go, path);
        GameObject.DestroyImmediate(go);
    }
}
#endif
