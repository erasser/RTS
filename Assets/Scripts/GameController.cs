using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static MiniMap;
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
    static Selectable _selectedObjectSelectableComponent;
    static Unit _selectedObjectUnitComponent;
    static bool _moveUnit;
    public static int fixedFrameCount;
    RaycastHit _selectionHit;
    static GameObject _overlayRenderTexture;
    static GameObject _unitCameraRenderTexture;
    public static bool updateHostilesInRange;

    void Awake()
    {
        gameController = this;
        new SquareRoot();

        #if !UNITY_EDITOR
        // Application.targetFrameRate = (int)(1 / Time.fixedDeltaTime);  // My try to sync rendering with fixed update
            Application.targetFrameRate = 60;
        #endif
    }

    void Start()
    {
        mainCamera = Find("Camera");
        _mainCameraComponent = mainCamera.GetComponent<Camera>();
        mainCameraTransform = mainCamera.transform;
        _unitCameraRenderTexture = Find("UnitCameraRenderTexture");
        _overlayRenderTexture = Find("UnitInfoRenderTextureOverlayImage");
        Find("_pathFinderHelper").SetActive(false);  // Disable pathFinderHelper rendering

        Find("buttonMove").GetComponent<Button>().onClick.AddListener(ProcessMoveButton);
        Find("map").GetComponent<Button>().onClick.AddListener(MiniMap.ProcessTouch);

        SetRenderTexture();
        Create();
        GenerateSomeUnits();
        GenerateSomeEnemyUnits();
    }

    void Update()
    {
        Performance.ShowFPS();
        ProcessTouch();
        ProcessKeys();
    }

    void FixedUpdate()
    {
        ++fixedFrameCount;

        updateHostilesInRange = fixedFrameCount % 5 == 0;  // Moved here from Unit.cs, so it's not calculated for every unit

        if (fixedFrameCount % 10 == 0)
            FindPaths();

        // if (!selectedObject) return;  // TODO: Remove

        // _camera.transform.position = _selectedObject.transform.position + CameraOffset;
        // _camera.transform.LookAt(_selectedObject.transform);
    }

    void OnGUI()
    {
        UpdateMap();
    }

    void ProcessTouch()
    {
        #if UNITY_EDITOR
            if (Input.GetMouseButtonDown(1))
                ProcessMoveButton();

            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() &&
        #else
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began && !EventSystem.current.IsPointerOverGameObject(0) &&
        #endif

        Physics.Raycast(_mainCameraComponent.ScreenPointToRay(Input.mousePosition), out _selectionHit))
        {
            var collidedObject = _selectionHit.collider.gameObject;

            // This affects what can be selected and targeted
            var unitTouched = collidedObject.CompareTag("Unit") || collidedObject.CompareTag("UnitEnemy");
            var buildingTouched = collidedObject.CompareTag("Building");

            /*  Move unit  */
            if (_moveUnit)  // TODO: Solve for building
            {
                if (unitTouched && !buildingTouched)
                {
                    _selectedObjectUnitComponent.SetTarget(collidedObject);
                    if (!_selectedObjectUnitComponent.IsFriendly(collidedObject))
                        _selectedObjectUnitComponent.targetToShootAt = collidedObject;
                }
                else
                    _selectedObjectUnitComponent.SetTarget(_selectionHit.point);

                SetMoveUnitState(false);
                return;
            }

            /*  Select / unselect unit  */
            if (unitTouched || buildingTouched)
                SelectObject(collidedObject);
            else
                UnselectObject();
        }
    }

    void ProcessKeys()
    {
        if (Input.GetKey(KeyCode.W))
            mainCameraTransform.Translate(Vector3.forward * .1f, Space.World);
        if (Input.GetKey(KeyCode.S))
            mainCameraTransform.Translate(Vector3.back * .1f, Space.World);
        if (Input.GetKey(KeyCode.A))
            mainCameraTransform.Translate(Vector3.left * .1f);
        if (Input.GetKey(KeyCode.D))
            mainCameraTransform.Translate(Vector3.right * .1f);
        var scroll = Input.mouseScrollDelta.y;

        if (scroll != 0)
        {
            mainCameraTransform.Translate(Vector3.forward * scroll);
            var position = mainCameraTransform.position;
            var y = position.y;
            y = Mathf.Clamp(y, 4, 40);
            position = new Vector3(position.x, y, position.z);
            mainCameraTransform.position = position;
        }
    }
    
    static void SelectObject(GameObject obj)
    {
        if (Equals(selectedObject, obj)) return;

        UnselectObject();

        selectedObject = obj;
        _selectedObjectSelectableComponent = obj.GetComponent<Selectable>();
        _selectedObjectSelectableComponent.ToggleOutline(true);
        _selectedObjectUnitComponent = obj.GetComponent<Unit>();
        if (_selectedObjectUnitComponent)
        {
            _selectedObjectUnitComponent.unitCamera.SetActive(true);

            if (_selectedObjectUnitComponent.IsTargetADummy())
                _selectedObjectUnitComponent.targetDummy.SetActive(true);
        }
        _unitCameraRenderTexture.SetActive(true);
    }

    public static void UnselectObject()
    {
        if (!selectedObject) return;

        _selectedObjectSelectableComponent.ToggleOutline(false);
        _selectedObjectSelectableComponent = null;

        if (_selectedObjectUnitComponent)
        {
            _selectedObjectUnitComponent.unitCamera.SetActive(false);
            _selectedObjectUnitComponent.targetDummy.SetActive(false);
            _selectedObjectUnitComponent = null;
        }
        _unitCameraRenderTexture.SetActive(false);
        selectedObject = null;
        SetMoveUnitState(false);
    }

    void ProcessMoveButton()
    {
        if (!selectedObject || selectedObject.CompareTag("Building")) return;

        SetMoveUnitState(true);
    }

    static void SetMoveUnitState(bool moveUnit)
    {
        _moveUnit = moveUnit;

        #if UNITY_EDITOR
            if (moveUnit)
                Cursor.SetCursor(gameController.mouseCursorMove, new (32, 32), CursorMode.ForceSoftware);
            else
                Cursor.SetCursor(null, Vector2.zero, CursorMode.ForceSoftware);
        #endif
    }

    static void SetRenderTexture()
    {
        _overlayRenderTexture.GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width, Screen.height);
        _overlayRenderTexture.GetComponent<Image>().material.mainTexture.width = Screen.width;
        _overlayRenderTexture.GetComponent<Image>().material.mainTexture.height = Screen.height;
    }
}
