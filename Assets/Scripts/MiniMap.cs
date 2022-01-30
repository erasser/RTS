using UnityEngine;
using UnityEngine.UI;

public class MiniMap
{
    static Rect _mapEnemyRect = new (0, 0, 4, 4);  // TODO: Images are resampled. Make them equal this size.
    static Vector2Int _mapSize;
    static Vector2Int _mapSizeHalf;
    static Vector2Int _mapRatio;

    public static void Create()  // Gets texture size from map image UI element rect transform
    {
        var mapImage = GameObject.Find("map");
        var mapSizeV2 = mapImage.GetComponent<RectTransform>().sizeDelta;
        _mapSize = new ((int)mapSizeV2.x, (int)mapSizeV2.y);
        _mapSizeHalf = _mapSize / 2;
        _mapRatio = _mapSize / 100;

        RenderTexture renderTexture = new(_mapSize.x, _mapSize.y, 16)
        {
            antiAliasing = 4
        };

        GameObject cameraMap = new("cameraThumbnail", typeof(Camera));
        var cameraMapCameraComponent = cameraMap.GetComponent<Camera>();
        cameraMapCameraComponent.targetTexture = renderTexture;

        cameraMap.transform.position = Vector3.up * 80;
        cameraMap.transform.eulerAngles = Vector3.right * 90;

        RenderTexture.active = cameraMapCameraComponent.targetTexture;
        cameraMapCameraComponent.Render();

        Texture2D texture = new(_mapSize.x, _mapSize.y);
        texture.ReadPixels(new Rect(0, 0, _mapSize.x, _mapSize.y), 0, 0);  // targetTexture must be assigned before ReadPixels()
        texture.Apply();

        var sprite = Sprite.Create(texture, new Rect(0, 0, _mapSize.x, _mapSize.y), Vector2.zero);
        mapImage.GetComponent<Image>().sprite = sprite;
    }

    public static void UpdateMap()
    {
        foreach (var unit in Unit.EnemyUnits)
            DrawUnitOnMap(unit, GameController.instance.minimapEnemyImage);

        foreach (var unit in Unit.PlayerUnits)
            DrawUnitOnMap(unit,
                unit == GameController.selectedObject
                    ? GameController.instance.minimapSelectedUnitImage
                    : GameController.instance.minimapPlayerImage);
    }

    static void DrawUnitOnMap(GameObject unit, Texture texture)  // This could be probably declared in UpdateMap(). IDK if it's not redeclared many times there.
    {
        var unitPosition = unit.transform.position;
        _mapEnemyRect.x = _mapSizeHalf.x + unitPosition.x * _mapRatio.x;    // 100 is map scene dimension
        _mapEnemyRect.y = _mapSizeHalf.y - unitPosition.z * _mapRatio.y;

        GUI.DrawTexture(_mapEnemyRect, texture);
    }
}
