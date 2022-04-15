using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static MiniMap;
using static Unit;
using static UnityEngine.GameObject;

public class GameController : MonoBehaviour
{
    public static GameController gameController;  // Instance of singleton
    public GameObject targetPrefab;
    public Texture2D mouseCursorMove;
    public LayerMask groundLayer;  // Used to determine if a unit is on the ground
    public Texture minimapPlayerImage;
    public Texture minimapEnemyImage;
    public Texture minimapSelectedUnitImage;
    static GameObject _mainCamera;
    static readonly Dictionary<string, Vector3> CameraZoomLimit = new () {{"minY", Vector3.up * 4}, {"maxY", Vector3.up * 40}};
    static Camera _mainCameraComponent;
    public static Transform mainCameraTransform;
    public static GameObject selectedObject;
    static Selectable _selectedObjectSelectableComponent;
    static Unit _selectedObjectUnitComponent;
    static bool _moveUnit;
    static int _fixedFrameCount;
    RaycastHit _raycastHit;
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
        _mainCamera = Find("Camera");
        _mainCameraComponent = _mainCamera.GetComponent<Camera>();
        mainCameraTransform = _mainCamera.transform;
        _unitCameraRenderTexture = Find("UnitCameraRenderTexture");
        _overlayRenderTexture = Find("UnitInfoRenderTextureOverlayImage");
        Find("_pathFinderHelper").SetActive(false);  // Disable pathFinderHelper rendering

        Find("buttonMove").GetComponent<Button>().onClick.AddListener(ProcessMoveButton);
        Find("map").GetComponent<Button>().onClick.AddListener(MiniMap.ProcessTouch);

        SetRenderTexture();
        Create();
    }

    void Update()
    {
        Performance.ShowFPS();
        ProcessTouch();
        ProcessKeys();
    }

    void FixedUpdate()
    {
        ++_fixedFrameCount;

        updateHostilesInRange = _fixedFrameCount % 5 == 0;  // Moved here from Unit.cs, so it's not calculated for every unit

        if (_fixedFrameCount % 10 == 0)
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

        Physics.Raycast(_mainCameraComponent.ScreenPointToRay(Input.mousePosition), out _raycastHit))
        {
            var collidedObject = _raycastHit.collider.gameObject;

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
                    _selectedObjectUnitComponent.SetTarget(_raycastHit.point);

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
        #if UNITY_EDITOR
            var scroll = Input.mouseScrollDelta.y;
            if (Input.GetKey(KeyCode.W))
                mainCameraTransform.Translate(Vector3.forward * .1f, Space.World);
            if (Input.GetKey(KeyCode.S))
                mainCameraTransform.Translate(Vector3.back * .1f, Space.World);
            if (Input.GetKey(KeyCode.A))
                mainCameraTransform.Translate(Vector3.left * .1f);
            if (Input.GetKey(KeyCode.D))
                mainCameraTransform.Translate(Vector3.right * .1f);
        #endif

        if (scroll != 0)
        {
            var translateVector = mainCameraTransform.forward * scroll;
            mainCameraTransform.Translate(translateVector, Space.World);

            // Camera zoom clamp, simplified as fuck :D  (https://en.wikipedia.org/wiki/Line%E2%80%93plane_intersection)
            var cameraPosition = mainCameraTransform.position;
            var isLower = cameraPosition.y < CameraZoomLimit["minY"].y;
            if (isLower || cameraPosition.y > CameraZoomLimit["maxY"].y)
            {
                var planePoint = isLower ? CameraZoomLimit["minY"] : CameraZoomLimit["maxY"];
                mainCameraTransform.position = cameraPosition + translateVector * (planePoint - cameraPosition).y / translateVector.y;
            }

            // Physics.Raycast(cameraPosition, Vector3.down, out _raycastHit);
            //
            // if (cameraPosition.y - _raycastHit.point.y < CameraZoomLimit["minY"].y)
            // {
            //     var newCameraPosition = cameraPosition;
            //     newCameraPosition.y = _raycastHit.point.y + CameraZoomLimit["minY"].y;
            //     mainCameraTransform.position = newCameraPosition;
            // }
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
            _selectedObjectUnitComponent.followCamera.SetActive(true);

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
            _selectedObjectUnitComponent.followCamera.SetActive(false);
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
