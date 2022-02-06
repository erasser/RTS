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
    GameObject _unitCameraRenderTexture;

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
        _overlayRenderTexture = Find("UnitInfoRenderTextureOverlay");

        Find("buttonMove").GetComponent<Button>().onClick.AddListener(ProcessMoveButton);
        Find("map").GetComponent<Button>().onClick.AddListener(MiniMap.ProcessTouch);

        UpdateRenderTexture();
        MiniMap.Create();
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

        
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() &&
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began && !EventSystem.current.IsPointerOverGameObject(0) &&
#endif
        
        
        // if (/*_moveUnit || */ Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() &&
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
        _selectedObjectUnitComponent.ToggleOutline(true);
        _selectedObjectUnitComponent.unitCamera.SetActive(true);
        _unitCameraRenderTexture.SetActive(true);
    }

    void UnselectObject()
    {
        if (!selectedObject) return;

        _selectedObjectUnitComponent.ToggleOutline(false);
        _selectedObjectUnitComponent.unitCamera.SetActive(false);
        _unitCameraRenderTexture.SetActive(false);
        selectedObject = null;
        _selectedObjectUnitComponent = null;
        _moveUnit = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.ForceSoftware);
    }

    void ProcessMoveButton()
    {
        if (!selectedObject) return;

        _moveUnit = true;

#if UNITY_EDITOR
        Cursor.SetCursor(mouseCursorMove, new (32, 32), CursorMode.ForceSoftware);
#endif
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
