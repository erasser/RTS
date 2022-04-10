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
    static Unit _selectedObjectUnitComponent;
    static bool _moveUnit;
    public static int fixedFrameCount;
    RaycastHit _selectionHit;
    static GameObject _overlayRenderTexture;
    GameObject _unitCameraRenderTexture;
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
        if (fixedFrameCount % 20 == 0)  // % 20 corresponds with .5 s (with current 0.025 time step setting)
            UpdateMap();
    }

    void ProcessTouch()
    {
        if (Input.GetMouseButtonDown(1))
            ProcessMoveButton();

        #if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() &&
        #else
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began && !EventSystem.current.IsPointerOverGameObject(0) &&
        #endif

        Physics.Raycast(_mainCameraComponent.ScreenPointToRay(Input.mousePosition), out _selectionHit))
        {
            // This affects what can be selected and targeted
            var unitTouched = _selectionHit.collider.CompareTag("Unit") || _selectionHit.collider.CompareTag("UnitEnemy");

            /*  Move unit  */
            if (_moveUnit)
            {
                if (unitTouched)
                {
                    _selectedObjectUnitComponent.SetTarget(_selectionHit.collider.gameObject);
                    if (!_selectedObjectUnitComponent.IsFriendly(_selectionHit.collider.gameObject))
                        _selectedObjectUnitComponent.targetToShootAt = _selectionHit.collider.gameObject;
                }
                else
                    _selectedObjectUnitComponent.SetTarget(_selectionHit.point);

                SetMoveUnitState(false);
                return;
            }

            /*  Select / unselect unit  */
            if (unitTouched)
                SelectObject(_selectionHit.collider.gameObject);
            else
                UnselectObject();
        }
    }

    void ProcessKeys()
    {
        if (Input.GetKey(KeyCode.W))
            mainCamera.transform.Translate(Vector3.forward * .1f, Space.World);
        if (Input.GetKey(KeyCode.S))
            mainCamera.transform.Translate(Vector3.back * .1f, Space.World);
        if (Input.GetKey(KeyCode.A))
            mainCamera.transform.Translate(Vector3.left * .1f);
        if (Input.GetKey(KeyCode.D))
            mainCamera.transform.Translate(Vector3.right * .1f);
        var scroll = Input.mouseScrollDelta.y;

        if (scroll != 0)
        {
            mainCamera.transform.Translate(Vector3.forward * scroll);
            var y = mainCamera.transform.position.y;
            y = Mathf.Clamp(y, 4, 40);
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, y, mainCamera.transform.position.z);
        }
    }
    
    void SelectObject(GameObject obj)
    {
        if (Equals(selectedObject, obj)) return;

        UnselectObject();

        selectedObject = obj;
        _selectedObjectUnitComponent = obj.GetComponent<Unit>();
        if (_selectedObjectUnitComponent)
        {
            _selectedObjectUnitComponent.ToggleOutline(true);
            _selectedObjectUnitComponent.unitCamera.SetActive(true);

            if (_selectedObjectUnitComponent.IsTargetADummy())
                _selectedObjectUnitComponent.targetDummy.SetActive(true);
        }
        _unitCameraRenderTexture.SetActive(true);
    }

    void UnselectObject()
    {
        if (!selectedObject) return;

        if (_selectedObjectUnitComponent)
        {
            _selectedObjectUnitComponent.ToggleOutline(false);
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
        if (!selectedObject) return;

        SetMoveUnitState(true);
    }

    void SetMoveUnitState(bool moveUnit)
    {
        _moveUnit = moveUnit;

        #if UNITY_EDITOR
            if (moveUnit)
                Cursor.SetCursor(mouseCursorMove, new (32, 32), CursorMode.ForceSoftware);
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
