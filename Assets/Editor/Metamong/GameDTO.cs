using System.Collections.Generic;

[System.Serializable]
public class GameDTO
{
    public int formatVersion;
    public string activeSceneId;
    public List<SceneDTO> scenes;
    public List<AssetDTO> assets;
}