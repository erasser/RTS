using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController instance;
    static GameObject _camera;
    static Camera _cameraComponent;
    static Vector3 _cameraOffset = new(2, 3, -2);
    public static GameObject selectedObject;
    static Unit _selectedObjectUnitComponent;
    public LayerMask groundLayer;  // Used to determine if a unit is on the ground

    void Start()
    {
        instance = this;
        _camera = GameObject.Find("Camera");
        _cameraComponent = _camera.GetComponent<Camera>();
        SelectObject(GameObject.Find("Tank"));
    }

    void Update()
    {
        CheckTouch();

        // if (Input.GetKey(KeyCode.A))
        // {
        // }
    }

    void FixedUpdate()
    {
        if (!selectedObject) return;  // TODO: Remove
        
        _camera.transform.position = selectedObject.transform.position + _cameraOffset;
        _camera.transform.LookAt(selectedObject.transform);
    }
    
    void CheckTouch()
    {
        if (Input.GetMouseButtonDown(0) &&
            Physics.Raycast(_cameraComponent.ScreenPointToRay(Input.mousePosition), out RaycastHit selectionHit, 1000 /*, selectableObjectsLayer*/))
        {
            if (selectionHit.collider.CompareTag("Unit"))
                SelectObject(selectionHit.collider.gameObject);
            else
                UnselectObject();
        }
    }

    void SelectObject(GameObject obj)
    {
        if (Equals(selectedObject, obj)) return;

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
    }
}
