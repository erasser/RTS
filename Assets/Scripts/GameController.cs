using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public GameObject tankPrefab;
    public GameObject tankEnemyPrefab;
    public GameObject targetPrefab;
    public Texture2D mouseCursorMove;
    public LayerMask groundLayer;  // Used to determine if a unit is on the ground
    public Texture mapPlayerTexture;
    public Texture mapEnemyTexture;
    Rect _mapEnemyRect = new (0, 0, 4, 4);
    Vector2Int _mapSize;
    Vector2Int _mapSizeHalf;
    Vector2Int _mapRatio;
    public static GameController instance;
    static GameObject _camera;
    static Camera _cameraComponent;
    static GameObject _selectedObject;
    static Unit _selectedObjectUnitComponent;
    static bool _moveUnit;
    public static int fixedFrameCount;
    RaycastHit _selectionHit;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        _camera = GameObject.Find("Camera");
        _cameraComponent = _camera.GetComponent<Camera>();

        GameObject.Find("buttonMove").GetComponent<Button>().onClick.AddListener(ProcessMoveButton);

        GenerateMap();
        Unit.GenerateSomeUnits();
        Unit.GenerateSomeEnemyUnits();
    }

    void Update()
    {
        ProcessTouch();

        // if (Input.GetKey(KeyCode.A))
        // {
        // }
    }

    void FixedUpdate()
    {
        ++fixedFrameCount;

        if (!_selectedObject) return;  // TODO: Remove
        
        // _camera.transform.position = _selectedObject.transform.position + CameraOffset;
        // _camera.transform.LookAt(_selectedObject.transform);
    }

    void OnGUI()
    {
        UpdateMap();
    }

    void UpdateMap()
    {
        // TODO: Is this more performant than to have gameObjects? (They could also be rendered each nth frame)

        foreach (var unit in Unit.EnemyUnits)
            DrawUnitOnMap(unit, mapEnemyTexture);

        foreach (var unit in Unit.PlayerUnits)
            DrawUnitOnMap(unit, mapPlayerTexture);
    }

    void DrawUnitOnMap(GameObject unit, Texture texture)  // This could be probably declared in UpdateMap(). IDK if it's not redeclared many times there.
    {
        var unitPosition = unit.transform.position;
        _mapEnemyRect.x = _mapSizeHalf.x + unitPosition.x * _mapRatio.x;    // 100 is map scene dimension
        _mapEnemyRect.y = _mapSizeHalf.y - unitPosition.z * _mapRatio.y;

        GUI.DrawTexture(_mapEnemyRect, texture);
    }

    void ProcessTouch()
    {
        if (Input.GetMouseButtonDown(1))
            ProcessMoveButton();

        if (/*_moveUnit || */ Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() &&
            Physics.Raycast(_cameraComponent.ScreenPointToRay(Input.mousePosition), out _selectionHit))
        {
            var unitTouched = _selectionHit.collider.CompareTag("Unit");

            /*  Move unit  */
            if (_moveUnit)
            {
                if (unitTouched)
                    _selectedObjectUnitComponent.SetTarget(_selectionHit.collider.gameObject);
                else
                    _selectedObjectUnitComponent.SetTarget(_selectionHit.point);

                // TODO: Redundant code, create a method.
                Cursor.SetCursor(null, Vector2.zero, CursorMode.ForceSoftware);
                _moveUnit = false;
                return;
            }

            /*  Select / unselect unit  */
            if (unitTouched)
                SelectObject(_selectionHit.collider.gameObject);
            else
                UnselectObject();
        }
    }

    void GenerateMap()  // Gets texture size from map image UI element rect transform
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

    void SelectObject(GameObject obj)
    {
        if (Equals(_selectedObject, obj)) return;

        UnselectObject();

        _selectedObject = obj;
        _selectedObjectUnitComponent = _selectedObject.GetComponent<Unit>();
        _selectedObjectUnitComponent.ToggleOutline(true);
    }

    void UnselectObject()
    {
        if (!_selectedObject) return;

        _selectedObjectUnitComponent.ToggleOutline(false);
        _selectedObject = null;
        _selectedObjectUnitComponent = null;
        _moveUnit = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.ForceSoftware);
    }

    void ProcessMoveButton()
    {
        if (!_selectedObject) return;

        _moveUnit = true;
        
        Cursor.SetCursor(mouseCursorMove, new Vector2(32, 32), CursorMode.ForceSoftware);
    }
}
