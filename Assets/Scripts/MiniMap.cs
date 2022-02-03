using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MiniMap : MonoBehaviour
{
    static Rect _mapEnemyRect = new (0, 0, 4, 4);  // TODO: Images are resampled. Make them equal this size.
    static Vector2 _worldSize;
    // TODO: Consider if the following should be int or float
    static Vector2Int _mapSize;
    static Vector2Int _mapSizeHalf;
    static Vector2Int _mapRatio;  // Ratio of minimap size / world size
    static GameObject _cameraViewRect;

    public static void Create()  // Gets texture size from map image UI element rect transform
    {
        var mapImage = GameObject.Find("map");
        // var mapSizeV2 = mapImage.GetComponent<RectTransform>().rect;
        mapImage.GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width / 8, Screen.width / 8);
        var mapSizeV2 = mapImage.GetComponent<RectTransform>().sizeDelta;
        Debug.Log(mapSizeV2);
        _cameraViewRect = GameObject.Find("cameraViewRect");
        _worldSize = new (100, 100);  // TODO: Get dynamically from mesh
        _mapSize = new ((int)mapSizeV2.x, (int)mapSizeV2.y);
        _mapSizeHalf = _mapSize / 2;
        _mapRatio = new ((int)(_mapSize.x / _worldSize.x), (int)(_mapSize.y / _worldSize.y));

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

    void OnMouseOver()
    {
        print("mouse over");
        // print(EventSystem.current);
    }

    public static void UpdateMap()
    {
        foreach (var unit in Unit.EnemyUnits)
            DrawUnitOnMap(unit, GameController.gameController.minimapEnemyImage);

        foreach (var unit in Unit.PlayerUnits)
            DrawUnitOnMap(unit,
                unit == GameController.selectedObject
                    ? GameController.gameController.minimapSelectedUnitImage
                    : GameController.gameController.minimapPlayerImage);
    }

    static void DrawUnitOnMap(GameObject unit, Texture texture)  // This could be probably declared in UpdateMap(). IDK if it's not redeclared many times there.
    {
        var unitPosition = unit.transform.position;
        _mapEnemyRect.x = _mapSizeHalf.x + unitPosition.x * _mapRatio.x;
        _mapEnemyRect.y = _mapSizeHalf.y - unitPosition.z * _mapRatio.y;

        GUI.DrawTexture(_mapEnemyRect, texture);
    }

    // TODO: ► Implement orbit camera (from Car project)
    public static void ProcessTouch()
    {
        Vector2 pointerCoords = new(Input.mousePosition.x - 2, Screen.height - Input.mousePosition.y - 2);  // TODO: or +2 in y?
        // Vector2 worldCoords = new(pointerCoords.x / _mapRatio.x - _mapSize.x / 2, -(pointerCoords.y / _mapRatio.y - _mapSize.y / 2));
        Vector2 worldCoords = new((pointerCoords.x - _mapSizeHalf.x) * _mapRatio.x, -((pointerCoords.y - _mapSizeHalf.y) * _mapRatio.y));
        // TODO: ►  ↑ Zde jsem skončil, vyjasnit si násobení / dělení _mapRatio

        var cameraPosition = GameController.mainCamera.transform.position;
        cameraPosition.x = worldCoords.x;
        cameraPosition.z = worldCoords.y;
        GameController.mainCamera.transform.position = cameraPosition;

        var cameraViewRectTransform = _cameraViewRect.GetComponent<RectTransform>();
        cameraViewRectTransform.transform.position = new Vector3(pointerCoords.x, Screen.height - pointerCoords.y, 0);
        Debug.Log(pointerCoords);


    }
}
