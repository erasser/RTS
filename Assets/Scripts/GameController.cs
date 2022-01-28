using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public GameObject tankPrefab;
    public GameObject targetPrefab;
    public Texture2D mouseCursorMove;
    public LayerMask groundLayer;  // Used to determine if a unit is on the ground
    public static GameController instance;
    static GameObject _camera;
    static Camera _cameraComponent;
    static readonly Vector3 CameraOffset = new(3, 4, -3);
    static GameObject _selectedObject;
    static Unit _selectedObjectUnitComponent;
    static bool _moveUnit;

    // enum PlayerState
    // {
    //     MoveUnit
    // }

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        _camera = GameObject.Find("Camera");
        _cameraComponent = _camera.GetComponent<Camera>();

        GameObject.Find("buttonMove").GetComponent<Button>().onClick.AddListener(ProcessMoveButton);
        
        Unit.GenerateSomeUnits();
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
        if (!_selectedObject) return;  // TODO: Remove
        
        // _camera.transform.position = _selectedObject.transform.position + CameraOffset;
        // _camera.transform.LookAt(_selectedObject.transform);
    }
    
    void ProcessTouch()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() &&
            Physics.Raycast(_cameraComponent.ScreenPointToRay(Input.mousePosition), out RaycastHit selectionHit))
        {
            var unitTouched = selectionHit.collider.CompareTag("Unit");

            /*  Move unit  */
            if (_moveUnit)
            {
                if (unitTouched)
                    _selectedObjectUnitComponent.SetTarget(selectionHit.collider.gameObject);
                else
                    _selectedObjectUnitComponent.SetTarget(selectionHit.point);

                // TODO: Redundant code, create a method.
                Cursor.SetCursor(null, Vector2.zero, CursorMode.ForceSoftware);
                _moveUnit = false;
                return;
            }

            /*  Select / unselect unit  */
            if (unitTouched)
            {
                SelectObject(selectionHit.collider.gameObject);
            }
            else
                UnselectObject();
        }
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
