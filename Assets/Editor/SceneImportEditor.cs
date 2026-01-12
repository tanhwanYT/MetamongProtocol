using UnityEditor;
using UnityEngine;

public static class SceneImportEditor
{
    [MenuItem("UniForge/Import Scene From JSON")]
    static void ImportScene()
    {
        SceneImportManager.ImportScene();
    }
}
