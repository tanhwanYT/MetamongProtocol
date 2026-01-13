using System.Collections.Generic;
namespace Metamong.Shared.DTO
{
    [System.Serializable]
    public class GameDTO
    {
        public int formatVersion;
        public string activeSceneId;
        public List<SceneDTO> scenes;
        public List<AssetDTO> assets;
    }
}