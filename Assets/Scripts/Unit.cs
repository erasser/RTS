using System;
using System.Collections.Generic;
using cakeslice;
using UnityEngine;
using Random = UnityEngine.Random;

public class Unit : MonoBehaviour
{
    bool _isOnGround;
    float _bottomToCenterDistance;  // TODO: Implement for UpdateIsOnGround() method
    public Rigidbody rb;
    [Range(1, 1000)]
    public int speed = 150;
    static GameObject _target;  // TODO: Should be placed on the ground to correctly detect, when the target is reached
    private GameObject _camera3rdPerson;
    public List<Outline> outlineComponents = new();
    float _initialDrag;  // 5 is fine
    float _initialAngularDrag;  // 10 is fine

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.down * .1f;
        // rb.centerOfMass = new Vector3(0, -.1f, .2f);
        _target = GameObject.Find("target");
        Time.timeScale = 1f;
        _camera3rdPerson = GameObject.Find("Camera3rdPerson");
        _initialDrag = rb.drag;
        _initialAngularDrag = rb.angularDrag;
        SetOutlineComponents();
        SetBottomToCenterDistance();
    }

    void FixedUpdate()  // TODO: Refactor / optimize when it's done. It will be executed heavily
    {
        UpdateIsOnGround();
        
        if (!_isOnGround) return;  // TODO: Remove, when units are initially placed on the ground (now they are dropped)

        // TODO: ► Move and turn only if on the ground
        
        // TODO: Slow, if near to the target
        
        // if (touchedGround && rb.velocity.sqrMagnitude < 36)  // TODO: Use instead of drag?
        // {
            // move forward
        // }

        ////  • option 1: Use Vector2.SignedAngle() & Rotate(Vector3.up)
        ////  • option 2: Use Vector3.SignedAngle() & Rotate(transform.up)  -> should be this IMHO (this is used now)

        if ((_target.transform.position - transform.position).sqrMagnitude < 3)  // target reached
        {
            _target.transform.position = new Vector3(Random.value * 30, 0, Random.value * 30);
        }

        rb.AddForce(transform.forward * speed, ForceMode.Impulse);
        
        
        var toTargetV3 = _target.transform.position - transform.position;
        var toTargetV2 = new Vector2(toTargetV3.x, toTargetV3.z);
        var tankForwardV2 = new Vector2(transform.right.x, transform.right.z);  // It's 'right' to correctly get negative or positive value

        var tankForwardFlattenedV3 = new Vector3(transform.forward.x, 0, transform.forward.z);
        
        // var coefficient = Vector3.SignedAngle(transform.forward, toTargetV3, transform.up) / 4;  // I'm not sure about rotation axis
        // var coefficient = Vector3.SignedAngle(transform.forward, toTargetV3, Vector3.up) / 4;  // I'm not sure about rotation axis
        var angleCoefficient = Vector3.SignedAngle(tankForwardFlattenedV3, toTargetV3, Vector3.up);  // I'm not sure about rotation axis
        // var coefficient = Vector2.SignedAngle(tankForwardV2, toTargetV2) / 4;  // I'm not sure about rotation axis

        // TODO: ► Check also cross product, it involves and angle

        angleCoefficient = Math.Clamp(angleCoefficient, -60, 60);

        // if (Mathf.Abs(coefficient) < 1) return;

        angleCoefficient *= .3f;

        rb.AddTorque(transform.up * angleCoefficient, ForceMode.Impulse);
        // rb.transform.Rotate(Vector3.up * coefficient);  // TODO: What if collision will occur? - It seems good
        // rb.transform.Rotate(transform.up * coefficient);  // seká se


        // if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out var hit))
        // {
        //     hit.normal
        // }

        // _camera3rdPerson.transform.eulerAngles = new Vector3(0, _camera3rdPerson.transform.eulerAngles.y, 0);
        
        Debug.DrawRay(transform.position, transform.forward * 10, Color.yellow);
        Debug.DrawRay(transform.position, toTargetV3 * 10, Color.red);
        // print(angle);
    }

    void SetBottomToCenterDistance()
    {
        // _bottomToCenterDistance = GetComponent<MeshFilter>().sharedMesh.bounds.extents.y;  // extents are half of bounds
    }
    
    void UpdateIsOnGround()
    {
        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit groundHit, GameController.instance.groundLayer))
        {
            if (groundHit.distance < .2f)
            {
                _isOnGround = true;
                rb.drag = _initialDrag;
                _initialAngularDrag = rb.angularDrag;
                return;
            }
        }

        // ground not hit or too far
        rb.drag = 0;
        rb.angularDrag = 0;
        _isOnGround = false;
    }
    
    void SetOutlineComponents()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>())  // Get all children recursively
        {
            var outlineComponent = child.GetComponent<Outline>();
            if (!outlineComponent) continue;

            outlineComponents.Add(outlineComponent);
            outlineComponent.enabled = false;
        }
        print(outlineComponents.Count);
    }
    
    public void ToggleOutline(bool enable)
    {
        if (outlineComponents[0].enabled == enable) return;

        foreach (Outline outline in outlineComponents)
        {
            outline.enabled = enable;
        }
    }

}
