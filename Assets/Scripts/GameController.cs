using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Unit;
using static UnityEngine.GameObject;

public class GameController : MonoBehaviour
{
    public static GameController gameController;  // Instance of singleton
    public GameObject tankPrefab;
    public GameObject tankEnemyPrefab;
    public GameObject targetPrefab;
    public Texture2D mouseCursorMove;
    public LayerMask groundLayer;  // Used to determine if a unit is on the ground
    public Texture minimapPlayerImage;
    public Texture minimapEnemyImage;
    public Texture minimapSelectedUnitImage;
    public static GameObject mainCamera;
    static Camera _mainCameraComponent;
    public static Transform mainCameraTransform;
    public static GameObject selectedObject;
    static Unit _selectedObjectUnitComponent;
    static bool _moveUnit;
    public static int fixedFrameCount;
    RaycastHit _selectionHit;
    GameObject _overlayRenderTexture;

    void Awake()
    {
        gameController = this;
        new SquareRoot();
    }

    void Start()
    {
        mainCamera = Find("Camera");
        _mainCameraComponent = mainCamera.GetComponent<Camera>();
        mainCameraTransform = mainCamera.transform;
        _overlayRenderTexture = Find("OverlayCameraRenderTexture");

        Find("buttonMove").GetComponent<Button>().onClick.AddListener(ProcessMoveButton);
        Find("map").GetComponent<Button>().onClick.AddListener(MiniMap.ProcessTouch);

        UpdateRenderTexture();
        MiniMap.Create();
        GenerateSomeUnits();
        GenerateSomeEnemyUnits();
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

        // if (!selectedObject) return;  // TODO: Remove

        // _camera.transform.position = _selectedObject.transform.position + CameraOffset;
        // _camera.transform.LookAt(_selectedObject.transform);
    }

    void OnGUI()
    {
        MiniMap.UpdateMap();
    }

    void ProcessTouch()
    {
        if (Input.GetMouseButtonDown(1))
            ProcessMoveButton();

        if (/*_moveUnit || */ Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() &&
            Physics.Raycast(_mainCameraComponent.ScreenPointToRay(Input.mousePosition), out _selectionHit))
        {
            // This affects what can be selected and targeted
            var unitTouched = _selectionHit.collider.CompareTag("Unit") || _selectionHit.collider.CompareTag("UnitEnemy");

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

    void SelectObject(GameObject obj)
    {
        if (Equals(selectedObject, obj)) return;

        UnselectObject();

        selectedObject = obj;
        _selectedObjectUnitComponent = selectedObject.GetComponent<Unit>();
        _selectedObjectUnitComponent.ToggleOutline(true);
    }

    void UnselectObject()
    {
        if (!selectedObject) return;

        _selectedObjectUnitComponent.ToggleOutline(false);
        selectedObject = null;
        _selectedObjectUnitComponent = null;
        _moveUnit = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.ForceSoftware);
    }

    void ProcessMoveButton()
    {
        if (!selectedObject) return;

        _moveUnit = true;
        
        Cursor.SetCursor(mouseCursorMove, new (32, 32), CursorMode.ForceSoftware);
    }

    public static void ProcessDestroy(GameObject obj)
    {
        if (obj.CompareTag("Unit"))
        {
            Destroy(obj.GetComponent<Unit>().targetDummy);
            PlayerUnits.Remove(obj);
        }
        else if (obj.CompareTag("UnitEnemy"))
        {
            Destroy(obj.GetComponent<Unit>().targetDummy);
            EnemyUnits.Remove(obj);
        }

        Destroy(obj);
    }

    void UpdateRenderTexture()
    {
        _overlayRenderTexture.GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width, Screen.height);
        _overlayRenderTexture.GetComponent<Image>().material.mainTexture.width = Screen.width;
        _overlayRenderTexture.GetComponent<Image>().material.mainTexture.height = Screen.height;
    }
}
