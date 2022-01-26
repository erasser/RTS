using System;
using System.Collections.Generic;
using cakeslice;
using UnityEngine;
using Random = UnityEngine.Random;

public class Unit : MonoBehaviour
{
    [Range(1, 1000)]
    public int speed = 150;
    bool _isOnGround;
    float _bottomToCenterDistance;  // TODO: Implement for UpdateIsOnGround() method
    Rigidbody _rb;
    GameObject _target;  // TODO: Should be placed on the ground to correctly detect, when the target is reached
    GameObject _targetDummy;
    GameObject _camera3rdPerson;
    public List<Outline> outlineComponents = new();
    float _initialDrag;  // 5 is fine
    float _initialAngularDrag;  // 10 is fine

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // _rb.centerOfMass = Vector3.down * .1f;
        _rb.centerOfMass = new Vector3(0, -.1f, .2f);
        // _target = GameObject.Find("target");
        Time.timeScale = 1f;
        _camera3rdPerson = GameObject.Find("Camera3rdPerson");
        _initialDrag = _rb.drag;
        _initialAngularDrag = _rb.angularDrag;
        _targetDummy = Instantiate(GameController.instance.targetPrefab);

        SetOutlineComponents();

        SetBottomToCenterDistance();
    }

    void FixedUpdate()  // TODO: Refactor / optimize when it's done. It will be executed heavily
    {
        MoveToTarget();
    }

    public static void GenerateSomeUnits()
    {
        for (int i = 0; i < 10; ++i)
        {
            var tank = Instantiate(GameController.instance.tankPrefab);
            tank.transform.position = new Vector3(Random.value * 10, 1, Random.value * 10);
            tank.transform.eulerAngles = new Vector3(0, Random.value * 360, 0);
        }
    }
    
    void MoveToTarget()
    {
        if (!_target) return;

        UpdateIsOnGround();

        if (!_isOnGround) return;  // TODO: Remove, when units are initially placed on the ground (now they are dropped)

        // TODO: ► FIX: On down slope, unit is heading a bit to right. On up slope, unit is heading a bit to left. 

        // TODO: Slow, if near to the target

        // if (touchedGround && rb.velocity.sqrMagnitude < 36)  // TODO: Use instead of drag?
        // {
            // move forward
        // }

        ////  • option 1: Use Vector2.SignedAngle() & Rotate(Vector3.up)
        ////  • option 2: Use Vector3.SignedAngle() & Rotate(transform.up)  -> should be this IMHO (this is used now)

        if ((_target.transform.position - transform.position).sqrMagnitude < 3)  // target reached
        {
            UnsetTarget();
            return;
        }

        _rb.AddForce(transform.forward * speed, ForceMode.Impulse);

        var toTargetV3 = _target.transform.position - transform.position;
        toTargetV3 = new Vector3(toTargetV3.x, 0, toTargetV3.z);

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

        _rb.AddTorque(transform.up * angleCoefficient, ForceMode.Impulse);
        // rb.transform.Rotate(Vector3.up * coefficient);  // TODO: What if collision will occur? - It seems good
        // rb.transform.Rotate(transform.up * coefficient);  // seká se

        // if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out var hit))
        // {
        //     hit.normal
        // }

        // _camera3rdPerson.transform.eulerAngles = new Vector3(0, _camera3rdPerson.transform.eulerAngles.y, 0);

        // Debug.DrawRay(transform.position, transform.forward * 10, Color.yellow);
        // Debug.DrawRay(transform.position, toTargetV3 * 10, Color.red);
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
            if (groundHit.distance < .3f)
            {
                SetIsOnGround(true);
                return;
            }
        }

        // ground not hit or too far
        SetIsOnGround(false);
    }

    public void SetIsOnGround(bool onGround)
    {
        if (_isOnGround == onGround) return;

        if (onGround)
        {
            _isOnGround = true;
            _rb.drag = _initialDrag;
            _rb.angularDrag = _initialAngularDrag;
        }
        else
        {
            _isOnGround = false;
            _rb.drag = 0;
            _rb.angularDrag = 0;
        }
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
    }

    public void ToggleOutline(bool enable)
    {
        if (outlineComponents[0].enabled == enable) return;

        foreach (Outline outline in outlineComponents)
        {
            outline.enabled = enable;
        }
    }

    public void SetTarget(GameObject target)
    {
        _target = target;
    }

    public void SetTarget(Vector3 target)
    {
        _targetDummy.transform.position = target;
        _targetDummy.SetActive(true);
        _target = _targetDummy;
    }

    public void UnsetTarget()
    {
        if (_targetDummy.activeSelf)
            _targetDummy.SetActive(false);

        _target = null;
    }
}
