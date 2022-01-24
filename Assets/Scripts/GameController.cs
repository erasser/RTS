using System;
using UnityEngine;

public class GameController : MonoBehaviour
{
    static GameObject _camera;
    static Vector3 _cameraOffset = new(2, 3, -2);
    static GameObject _selectedObject;
    
    Quaternion _quaternion = new ();


    void Start()
    {
        _camera = GameObject.Find("Camera");
        _selectedObject = GameObject.Find("Tank");
    }

    void Update()
    {
        // if (Input.GetKey(KeyCode.A))
        // {
        
        // }
    }

    void FixedUpdate()
    {

        _camera.transform.position = _selectedObject.transform.position + _cameraOffset;
        _camera.transform.LookAt(_selectedObject.transform);
    }
}
