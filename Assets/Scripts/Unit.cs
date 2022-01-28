using System;
using System.Collections.Generic;
using cakeslice;
using UnityEngine;
using Random = UnityEngine.Random;

public class Unit : MonoBehaviour
{
    [Range(1, 1000)]
    public float speed = 150;
    static float _higherSpeedCoefficient = 1.3f;
    bool _isOnGround;
    bool _isOnUnit;
    float _bottomToCenterDistance;  // TODO: Implement for UpdateIsOnGround() method
    Rigidbody _rb;
    GameObject _target;  // TODO: Should be placed on the ground to correctly detect, when the target is reached
    GameObject _targetDummy;
    GameObject _camera3rdPerson;
    public List<Outline> outlineComponents = new();
    float _initialDrag;         // 5 is fine
    float _initialAngularDrag;  // 5 is fine (10 before, but it prevented the unit to face a target precisely, the net force was not big enough)

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.centerOfMass = Vector3.down * .1f;
        // _rb.centerOfMass = new Vector3(0, -.1f, .1f);
        // _target = GameObject.Find("target");
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
        for (int i = 0; i < 2; ++i)
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

        if (!_isOnGround && !_isOnUnit) return;  // TODO: Remove, when units are initially placed on the ground (now they are dropped)

        // TODO: ► FIX: On down slope, unit is heading a bit to right. On up slope, unit is heading a bit to left. 

        // if (touchedGround && rb.velocity.sqrMagnitude < 36)  // TODO: Use instead of drag?
        // {
            // move forward
        // }

        ////  • option 1: Use Vector2.SignedAngle() & Rotate(Vector3.up)
        ////  • option 2: Use Vector3.SignedAngle() & Rotate(transform.up)  -> should be this IMHO (this is used now)

        /***  Movement  ***/        
        var toTargetSqrMagnitude = (_target.transform.position - transform.position).sqrMagnitude;
        
        if (toTargetSqrMagnitude < 1)  // target reached
        {
            UnsetTarget();
            return;
        }

        // Slow when near to target
        var speedCoefficient = 1f;
        if (toTargetSqrMagnitude < 6)
            speedCoefficient = toTargetSqrMagnitude / 6f;  // TODO: Try to make the motion more fluent. Try Sqr or other value to divide by.

        _rb.AddForce(transform.forward * speed * speedCoefficient, ForceMode.Impulse);

        /***  Rotation  ***/
        var toTargetV3Flattened = _target.transform.position - transform.position;
        toTargetV3Flattened = new Vector3(toTargetV3Flattened.x, 0, toTargetV3Flattened.z);

        var toTargetV2 = new Vector2(toTargetV3Flattened.x, toTargetV3Flattened.z);
        var tankForwardV2 = new Vector2(transform.right.x, transform.right.z);  // It's 'right' to correctly get negative or positive value

        var tankForwardFlattenedV3 = new Vector3(transform.forward.x, 0, transform.forward.z);

        // var coefficient = Vector3.SignedAngle(transform.forward, toTargetV3, transform.up) / 4;  // I'm not sure about rotation axis
        // var coefficient = Vector3.SignedAngle(transform.forward, toTargetV3, Vector3.up) / 4;  // I'm not sure about rotation axis
        var angle = Vector3.SignedAngle(tankForwardFlattenedV3, toTargetV3Flattened, Vector3.up);  // I'm not sure about rotation axis
        // var coefficient = Vector2.SignedAngle(tankForwardV2, toTargetV2) / 4;  // I'm not sure about rotation axis

        // TODO: ► Check also cross product, it involves an angle

        var angleCoefficient = Math.Clamp(angle, -60, 60);

        // if (Mathf.Abs(coefficient) < 1) return;

        // Rotate faster when initiating movement  // TODO: Try to rotate without movement. I don't know how to solve it because of wheel colliders.
        if (Math.Abs(angle) > 10)
            angleCoefficient *= 1.2f;
        else
            angleCoefficient *= .3f;

        _rb.AddTorque(transform.up * angleCoefficient, ForceMode.Impulse);
        // rb.transform.Rotate(Vector3.up * coefficient);  // TODO: What if collision will occur? - It seems good
        // rb.transform.Rotate(transform.up * coefficient);  // seká se
        
        // _camera3rdPerson.transform.eulerAngles = new Vector3(0, _camera3rdPerson.transform.eulerAngles.y, 0);

        // Debug.DrawRay(transform.position, tankForwardFlattenedV3.normalized * 20, Color.yellow);
        // Debug.DrawRay(transform.position, toTargetV3Flattened.normalized * 20, Color.red);
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

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Unit"))
        {
            // other.gameObject.GetComponent<Rigidbody>().mass = 1000;
            print("on unit!");
            SetIsOnUnit(true);
            speed *= _higherSpeedCoefficient;
        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.CompareTag("Unit"))
        {
            // other.gameObject.GetComponent<Rigidbody>().mass = 300;
            print("not on unit!");
            SetIsOnUnit(false);
            speed /= _higherSpeedCoefficient;
        }
    }

    void SetIsOnUnit(bool onUnit)
    {
        if (_isOnUnit == onUnit) return;

        ToggleDrags(onUnit);
    }

    public void SetIsOnGround(bool onGround)
    {
        if (_isOnGround == onGround) return;

        ToggleDrags(onGround);
    }

    void ToggleDrags(bool enable)
    {
        if (enable)
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

    void UnsetTarget()
    {
        // Target is a point in the scene (i.e. unit is not following another unit)
        if (_targetDummy.activeSelf)
        {
            _targetDummy.SetActive(false);
            _target = null;
        }
    }
}
