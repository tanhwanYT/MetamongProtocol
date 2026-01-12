using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

public class SceneImportManager : MonoBehaviour 
{
    static string jsonFileName = "scene.json";

    static SceneDTO currentScene;
    static List<AssetDTO> assets;

    public static void ImportScene()
    {
        string path = Path.Combine(Application.streamingAssetsPath, jsonFileName);

        if (!File.Exists(path))
        {
            Debug.LogError($"[SceneImport] JSON not found: {path}");
            return;
        }

        string json = File.ReadAllText(path);
        GameDTO game = JsonConvert.DeserializeObject<GameDTO>(json);

        if (game == null)
        {
            Debug.LogError("[SceneImport] GameDTO deserialize failed");
            return;
        }

        currentScene = game.scenes.Find(s => s.sceneId == game.activeSceneId);
        assets = game.assets;

        if (currentScene == null)
        {
            Debug.LogError("[SceneImport] Active scene not found");
            return;
        }

        Debug.Log($"[SceneImport] Load Scene: {currentScene.sceneId}");

        Debug.Log($"[SceneImport] entity count = {currentScene.entities.Count}");
        foreach (var entity in currentScene.entities)
        {
            Debug.Log($"[SceneImport] create entity: {entity.name}");
            CreateEntity(entity);
        }

        Debug.Log("[SceneImport] Scene loaded");
    }

    static void CreateEntity(EntityDTO entity)
    {
        GameObject go = new GameObject(entity.name);
        go.transform.position = new Vector3(entity.x, entity.y, 0f);

        var sr = go.AddComponent<SpriteRenderer>();

        AssetDTO asset = FindAssetForEntity(entity);
        if (asset == null || string.IsNullOrEmpty(asset.url))
        {
            Debug.LogWarning($"[CreateEntity] Asset missing: {entity.name}");
            return;
        }

        ImageLoader.LoadSprite(asset.url, sprite =>
        {
            if (sprite == null)
            {
                Debug.LogError($"[CreateEntity] Sprite load failed: {asset.url}");
                return;
            }

            sr.sprite = sprite;
        });

        if (entity.variables != null && entity.variables.Count > 0)
        {
            CreateVariables(go, entity.variables);
        }

 //       if (entity.events != null && entity.events.Count > 0)
 //       {
  //          var events = go.AddComponent<RuntimeEvents>();
  //          events.Initialize(entity.events, go);
  //      }
    }

    static AssetDTO FindAssetForEntity(EntityDTO entity)
    {
        return assets.Find(a => a.name == entity.name);
    }

    static void CreateVariables(GameObject go, List<VariableDTO> vars)
    {
        if (vars == null || vars.Count == 0)
            return;

        var container = go.GetComponent<RuntimeVariables>();
        if (container == null)
        {
            container = go.AddComponent<RuntimeVariables>();
            if (container == null)
            {
                Debug.LogError("[CreateVariables] RuntimeVariables component add failed");
                return;
            }
        }

        foreach (var dto in vars)
        {
            VariableSO so = VariableSOFactory.Create(dto);
            if (so != null)
                container.AddVariable(so);
        }
    }
}
