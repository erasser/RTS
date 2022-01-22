using UnityEngine;

public class GameController : MonoBehaviour
{
    static GameObject _camera;
    static Vector3 _cameraOffset = new(2, 3, -2);
    static GameObject _selectedObject;
    static GameObject _target;
    
    Quaternion _quaternion = new ();


    void Start()
    {
        _camera = GameObject.Find("Camera");
        _selectedObject = GameObject.Find("Tank");
        _target = GameObject.Find("target");
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.A))
        {
            var toTargetV3 = _target.transform.position - _selectedObject.transform.position;
            var toTargetV2 = new Vector2(toTargetV3.x, toTargetV3.z);
            var tankForwardV2 = new Vector2(Tank.instance.transform.right.x, Tank.instance.transform.right.z);  // IDFK why I must use 'right'

            int sign = Vector2.Dot(tankForwardV2, toTargetV2) > 0 ? 1 : -1;

            Tank.instance.rb.AddTorque(Vector3.up * sign * 40);
        }
    }

    void FixedUpdate()
    {
        


        _camera.transform.position = _selectedObject.transform.position + _cameraOffset;
        _camera.transform.LookAt(_selectedObject.transform);
    }
}
