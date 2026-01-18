using UnityEngine;
using UnityEditor;
using System.IO;

namespace Uniforge.FastTrack.Editor
{
    /// <summary>
    /// Easy import window - just drag and drop JSON file or paste JSON text.
    /// </summary>
    public class UniforgeImportWindow : EditorWindow
    {
        private string jsonText = "";
        private Vector2 scrollPos;

        [MenuItem("Uniforge/Import Window (Easy)", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<UniforgeImportWindow>("Uniforge Import");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }


        private void OnGUI()
        {
            GUILayout.Space(10);

            // Title
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 18;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("Uniforge Import", titleStyle);

            GUILayout.Space(10);

            // Drag and Drop Area
            DrawDragDropArea();

            GUILayout.Space(10);

            // OR separator
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("— OR —", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // JSON Text Area
            GUILayout.Label("Paste JSON here:", EditorStyles.boldLabel);
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(120));
            jsonText = EditorGUILayout.TextArea(jsonText, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Import Button
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("Import JSON", GUILayout.Height(40)))
            {
                if (!string.IsNullOrEmpty(jsonText))
                {
                    ImportJson(jsonText);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please paste JSON or drag a file first.", "OK");
                }
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);

            // Utility Buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Animations", GUILayout.Height(30)))
            {
                AnimationGenerator.ClearGeneratedAnimations();
            }
            if (GUILayout.Button("Browse File...", GUILayout.Height(30)))
            {
                BrowseAndImport();
            }
            GUILayout.EndHorizontal();

        }

        private void DrawDragDropArea()
        {
            // Create drag-drop area
            Rect dropArea = GUILayoutUtility.GetRect(0, 80, GUILayout.ExpandWidth(true));

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.alignment = TextAnchor.MiddleCenter;
            boxStyle.fontSize = 14;
            boxStyle.normal.textColor = Color.gray;

            GUI.Box(dropArea, "📁 Drag & Drop JSON File Here", boxStyle);

            // Handle drag and drop
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        break;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (string path in DragAndDrop.paths)
                        {
                            if (path.EndsWith(".json"))
                            {
                                string json = File.ReadAllText(path);
                                jsonText = json;
                                ImportJson(json);
                                break;
                            }
                        }
                    }
                    evt.Use();
                    break;
            }
        }

        private void BrowseAndImport()
        {
            string path = EditorUtility.OpenFilePanel("Select Uniforge JSON", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                string json = File.ReadAllText(path);
                jsonText = json;
                ImportJson(json);
            }
        }

        private void ImportJson(string json)
        {
            Debug.Log("<color=cyan>[UniforgeImport]</color> Starting import...");
            UniforgeImporter.ImportFromJson(json);
        }
    }
}
