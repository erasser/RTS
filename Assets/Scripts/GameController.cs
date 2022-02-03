using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    static Camera _cameraComponent;
    public static GameObject selectedObject;
    static Unit _selectedObjectUnitComponent;
    static bool _moveUnit;
    public static int fixedFrameCount;
    RaycastHit _selectionHit;

    private void Awake()
    {
        gameController = this;
        new SquareRoot();
    }

    void Start()
    {
        mainCamera = GameObject.Find("Camera");
        _cameraComponent = mainCamera.GetComponent<Camera>();

        GameObject.Find("buttonMove").GetComponent<Button>().onClick.AddListener(ProcessMoveButton);
        GameObject.Find("map").GetComponent<Button>().onClick.AddListener(MiniMap.ProcessTouch);

        MiniMap.Create();
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

        if (!selectedObject) return;  // TODO: Remove
        
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
        
        Cursor.SetCursor(mouseCursorMove, new Vector2(32, 32), CursorMode.ForceSoftware);
    }

    public static void ProcessDestroy(GameObject obj)
    {
        Destroy(obj);
    }
}
