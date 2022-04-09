using System;
using System.Collections.Generic;
using cakeslice;
using K_PathFinder;
using UnityEngine;
using Random = UnityEngine.Random;
using static GameController;

// TODO: ► Remove Outline component from enemies

public class Unit : MonoBehaviour
{
    [Range(1, 1000)]
    public float speed = 150;
    static float _higherSpeedCoefficient = 1.3f;
    static int _hostileSqrDistanceLimit = 20;
    bool _isOnGround;
    bool _isOnUnit;
    float _bottomToCenterDistance;  // TODO: Implement for UpdateIsOnGround() method
    Rigidbody _rb;
    GameObject _target;  // TODO: Should be placed on the ground to correctly detect, when the target is reached
    public GameObject targetDummy;
    public GameObject targetToShootAt;  // It's cleared after unit too far or destroyed
    public Vector3 toShootTargetDirection;  // Cached, because it's used many times  // TODO: Consider moving to WeaponLaser, if it's used just for laser.
    public List<Outline> outlineComponents = new();
    float _initialDrag;         // 5 is fine
    float _initialAngularDrag;  // 5 is fine (10 before, but it prevented the unit to face a target precisely, the net force was not big enough)
    // bool _moveAfterFinishedOnTopOfAnotherUnit;
    public static readonly List<GameObject> PlayerUnits = new();
    public static readonly List<GameObject> EnemyUnits = new();
    List<GameObject> _hostilesInRange = new(); // For enemy, player is hostile. For player, enemy is hostile. TODO: Not used.
    public Transform cockpitTransform;
    public Transform cannonSocketTransform;
    public float armor = 100;
    public float currentArmor;
    public float shield = 100;
    public float currentShield;  // Regenerates over time
    public WeaponLaser laser;
    Transform _statusInfoTransform;
    HealthBar _armorBar;
    HealthBar _shieldBar;
    Transform _thisTransform;  // Cached transform of this
    float _lastDamagedTime;    // Time, when unit was last damaged - serves to determine shield regeneration.
    public GameObject unitCamera;
    /* pathfinding variables */
    PathFinderAgent _pathFinderAgent;
    Path _path;
    readonly List<Vector3> _pathPoints = new();
    static readonly List<Unit> UnitsThatNeedRegularPathUpdate = new();

    void Awake()
    {
        _thisTransform = transform;
        _rb = GetComponent<Rigidbody>();
        _pathFinderAgent = GetComponent<PathFinderAgent>();
        _pathFinderAgent.SetRecievePathDelegate(ReceivePathDelegate, AgentDelegateMode.ThreadSafe);  // ThreadSafe - executed in next update
        _rb.centerOfMass = Vector3.down * .1f;
        // _rb.centerOfMass = new Vector3(0, -.1f, .1f);
        _initialDrag = _rb.drag;
        _initialAngularDrag = _rb.angularDrag;
        targetDummy = Instantiate(gameController.targetPrefab);
        cockpitTransform = _thisTransform.Find("cockpit001");
        cannonSocketTransform = cockpitTransform.Find("cannonsSockets001");
        /***  Weapons  ***/
        laser = GetComponent<WeaponLaser>();
        laser.unit = this;
        currentArmor = armor;
        currentShield = shield;
        _statusInfoTransform = _thisTransform.Find("UnitStatus").transform;
        _armorBar = _statusInfoTransform.Find("armorBar").transform.GetComponent<HealthBar>();
        _shieldBar = _statusInfoTransform.Find("shieldBar").transform.GetComponent<HealthBar>();
        unitCamera = cockpitTransform.Find("Camera3rdPerson").gameObject;
        unitCamera.SetActive(false);

        SetOutlineComponents();

        SetBottomToCenterDistance();
    }

    void FixedUpdate()  // TODO: Refactor / optimize when it's done. It will be executed heavily
    {
        if (updateHostilesInRange)  // Update every nth frame
            UpdateHostilesInRange();

        MoveToTarget();
        AlignStatusInfo();
        RegenerateShield();

        // laser.UpdateLaserProps();
    }

    public static void GenerateSomeUnits()
    {
        for (int i = 0; i < 2; ++i)
        {
            var tank = Instantiate(gameController.tankPrefab);
            tank.transform.position = new Vector3(Random.value * 10, 2, Random.value * 10);
            tank.transform.eulerAngles = new Vector3(0, Random.value * 360, 0);
            tank.name = $"Tank{PlayerUnits.Count}";
            PlayerUnits.Add(tank);
        }
    }

    public static void GenerateSomeEnemyUnits()
    {
        return;  // PathFinderAgent added just to Tank prefab for now

        for (int i = 0; i < 1; ++i)
        {
            var tankEnemy = Instantiate(gameController.tankEnemyPrefab);
            tankEnemy.transform.position = new Vector3(Random.value * 10, 2, Random.value * 10);
            tankEnemy.transform.eulerAngles = new Vector3(0, Random.value * 360, 0);
            tankEnemy.name = $"TankEnemy{EnemyUnits.Count}";
            EnemyUnits.Add(tankEnemy);
        }
    }
    
    void MoveToTarget()
    {
        if (_pathPoints.Count == 0) return;

        UpdateIsOnGround();

        if (!_isOnGround && !_isOnUnit) return;  // TODO: Remove, when units are initially placed on the ground (now they are dropped)

        // TODO: ► FIX: On down slope, unit is heading a bit to right. On up slope, unit is heading a bit to left. 

        // if (touchedGround && rb.velocity.sqrMagnitude < 36)  // TODO: Use instead of drag?
        // {
            // move forward
        // }

        ////  • option 1: Use Vector2.SignedAngle() & Rotate(Vector3.up)
        ////  • option 2: Use Vector3.SignedAngle() & Rotate(transform.up)  -> should be this IMHO (this is used now)

        // var toTargetDirection = _target.transform.position - _thisTransform.position;
        var toTargetDirection = _pathPoints[0] - _thisTransform.position;  // _pathPoints[0] is actual target location

        /***  Movement  ***/        
        var toTargetSqrMagnitude = toTargetDirection.sqrMagnitude;

        // TODO: Marge toTargetSqrMagnitude with declaration if "Slow down when near to target" is not used
        if (CheckTargetReached(toTargetSqrMagnitude))  // Also unsets target
        {
            // if (_isOnUnit)
            // {
            //     // TODO: This is not executing. Maybe it's not needed with a proper collision mesh. Try to make a thorn on top & on the bottom of collision mesh, so unit slides from another one. 
            //     // _moveAfterFinishedOnTopOfAnotherUnit = true;
            //     _rb.AddForce(_thisTransform.forward * speed * 2, ForceMode.Impulse);
            //     print("going to ground");
            // }
            return;
        }

        // Slow down when near to target - Not needed anymore
        var speedCoefficient = 1f;      // ↓ Don't slow if it's already slow enough
        // if (toTargetSqrMagnitude < 4 && _rb.velocity.sqrMagnitude > 1)
        //     speedCoefficient = toTargetSqrMagnitude / 4f;  // TODO: Try to make the motion more fluent. Try Sqr or other value to divide by.

        var forward = _thisTransform.forward;
        _rb.AddForce(forward * speed * speedCoefficient, ForceMode.Impulse);

        /***  Rotation  ***/
        var toTargetV3Flattened = _pathPoints[0] - _thisTransform.position;
        toTargetV3Flattened = new (toTargetV3Flattened.x, 0, toTargetV3Flattened.z);

        // var toTargetV2 = new Vector2(toTargetV3Flattened.x, toTargetV3Flattened.z);
        // var tankForwardV2 = new Vector2(transform.right.x, transform.right.z);  // It's 'right' to correctly get negative or positive value

        Vector3 tankForwardFlattenedV3 = new (forward.x, 0, forward.z);

        // var coefficient = Vector3.SignedAngle(transform.forward, toTargetV3, transform.up) / 4;  // I'm not sure about rotation axis
        // var coefficient = Vector3.SignedAngle(transform.forward, toTargetV3, Vector3.up) / 4;  // I'm not sure about rotation axis
        var angle = Vector3.SignedAngle(tankForwardFlattenedV3, toTargetV3Flattened, Vector3.up);  // The rotation axis is to determine the sign
        // var coefficient = Vector2.SignedAngle(tankForwardV2, toTargetV2) / 4;  // I'm not sure about rotation axis

        // TODO: ► Check also cross product, it involves an angle

        var angleCoefficient = Math.Clamp(angle, -60, 60);

        // if (Mathf.Abs(coefficient) < 1) return;

        // Rotate faster when initiating movement  // TODO: Try to rotate without movement. I don't know how to solve it because of wheel colliders.
        angleCoefficient *= Math.Abs(angle) > 10 ? .9f : .3f;

        _rb.AddTorque(_thisTransform.up * angleCoefficient, ForceMode.Impulse);
        // rb.transform.Rotate(Vector3.up * coefficient);  // TODO: What if collision will occur? - It seems good
        // rb.transform.Rotate(transform.up * coefficient);  // seká se
        
        // _camera3rdPerson.transform.eulerAngles = new Vector3(0, _camera3rdPerson.transform.eulerAngles.y, 0);

        // Debug.DrawRay(transform.position, tankForwardFlattenedV3.normalized * 20, Color.yellow);
        // Debug.DrawRay(transform.position, toTargetV3Flattened.normalized * 20, Color.red);
        // print(_rb.velocity.magnitude);  // With current settings it's 3
    }

    bool CheckTargetReached(float toTargetSqrMagnitude)
    {
        float distanceLimit;
        var targetIsDummy = _target == targetDummy;

        // dummy target
        if (targetIsDummy)
            distanceLimit = .2f;
        // friendly target
        else if (IsFriendly(_target))
            distanceLimit = 1.1f;
        // hostile target
        else
        {
            // Heading for another hostile unit
            if (_target != targetToShootAt)
                return false;
            distanceLimit = _hostileSqrDistanceLimit;
        }

        var targetReached = toTargetSqrMagnitude < distanceLimit;

        // if (_pathPoints.Count)

        if (targetReached)
        {
            _pathPoints.RemoveAt(0);  // Delete the reached point from stack

            if (targetIsDummy && _pathPoints.Count == 0)  // Don't unset if the target is a unit
                UnsetDummyTarget();
        }

        return targetReached;
    }

    void SetBottomToCenterDistance()
    {
        // _bottomToCenterDistance = GetComponent<MeshFilter>().sharedMesh.bounds.extents.y;  // extents are half of bounds
    }
    
    void UpdateIsOnGround()
    {
        if (Physics.Raycast(_thisTransform.position, - _thisTransform.up, out RaycastHit groundHit, gameController.groundLayer))
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

    // Not used
    // TODO: ► What if collides with more that one unit? Maybe use OnCollisionStays() instead.
    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Unit") || other.gameObject.CompareTag("UnitEnemy"))
        {
            // other.gameObject.GetComponent<Rigidbody>().mass = 1000;  // other.rigidbody exists, no need for GetComponent
            // print("on unit!");
            SetIsOnUnit(true);
            speed *= _higherSpeedCoefficient;
        }
    }

    // Not used
    void OnCollisionExit(Collision other)
    {
        if (other.gameObject.CompareTag("Unit") || other.gameObject.CompareTag("UnitEnemy"))
        {
            // other.gameObject.GetComponent<Rigidbody>().mass = 300;
            // print("not on unit!");
            SetIsOnUnit(false);
            speed /= _higherSpeedCoefficient;
            // _moveAfterFinishedOnTopOfAnotherUnit = false;
        }
    }

    private void OnCollisionStay(Collision other)
    {
        // if (other.gameObject.CompareTag("Unit"))
        // {
        //     print("Applying another unit push");
        //     // This will push both colliding units
        //     other.rigidbody.AddForce((other.rigidbody.position - transform.position).normalized * 200, ForceMode.Impulse);
        // }
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
            outline.enabled = enable;
    }

    /// <summary>
    /// Can target friendly unit to follow or hostile unit to attack
    /// </summary>
    /// <param name="target">Unit to target</param>
    public void SetTarget(GameObject target)  // Unis is following another unit
    {
        _target = target;
        UnitsThatNeedRegularPathUpdate.Add(this);

        FindPath(target.transform.position);
    }

    public void SetTarget(Vector3 target)     // Target is a static point
    {
        if (IsTargetAUnit())
            UnitsThatNeedRegularPathUpdate.Remove(this);

        _target = targetDummy;
        targetDummy.transform.position = target;
        targetDummy.SetActive(true);

        FindPath(target);
    }

    void UnsetDummyTarget()  // Can be used to unset another targets as well, with a little change (it's not meaningful now).
    {
        // if (!IsTargetAUnit())
        // {
        targetDummy.SetActive(false);
        _target = null;
        // }
    }

    bool IsTargetAUnit()
    {
        return !targetDummy.activeSelf;
    }

    void UpdateHostilesInRange()
    {
        var hostilesList = CompareTag("Unit") ? EnemyUnits : PlayerUnits;

        float smallestSqrDistance = 1000000000;
        // _hostilesInRange.Clear();
        foreach (var hostile in hostilesList)
        {
            var sqrDistance = (hostile.transform.position - _thisTransform.position).sqrMagnitude;
            // Fills the list with hostiles in range
            if (sqrDistance < _hostileSqrDistanceLimit)
            {
                // _hostilesInRange.Add(hostile);

                // Unit is heading for this hostile. Hostile now in range => set it as a target to shoot at.
                if (_target == hostile)
                    targetToShootAt = hostile;

                // This hostile unit is closest hostile for now.
                // Do this only if unit doesn't have a shoot target, so the current target is not overwritten.
                else if (!targetToShootAt && sqrDistance < smallestSqrDistance)
                {
                    smallestSqrDistance = sqrDistance;
                    targetToShootAt = hostile;
                    // _laser.SetActive(true);  // Use this if ground raycast is not used in UpdateShootLaser() 
                }
            }
            else if (hostile == targetToShootAt)  // Clear shoot target if too far
            {
                targetToShootAt = null;
                // _laser.SetActive(false);
            }
        }
    }

    public void UpdateCockpitAndCannonRotation()
    {
        if (!laser.IsShooting()) return;

        var lookAtRotation = Quaternion.LookRotation(toShootTargetDirection);

        // TODO: The slerp is faster with higher angle. Fix it to linear behavior.
        var lookAtQuaternionCockpit = Quaternion.Slerp(cockpitTransform.rotation, lookAtRotation, Time.fixedDeltaTime * 16);
        var lookAtQuaternionCannon = Quaternion.Slerp(cannonSocketTransform.rotation, lookAtRotation, Time.fixedDeltaTime * 16);
        // var lookAtQuaternion2 = Quaternion.Slerp(_cockpitTransform.rotation, lookAtRotation, Time.fixedDeltaTime * 6);

        cockpitTransform.eulerAngles = new(cockpitTransform.eulerAngles.x, lookAtQuaternionCockpit.eulerAngles.y, cockpitTransform.eulerAngles.z);

        // TODO: This is a hotfix, because the cockpit also gets x & z rotation from above line >:-(
        cockpitTransform.localEulerAngles = new(0, cockpitTransform.localEulerAngles.y, 0);

        cannonSocketTransform.eulerAngles = new(lookAtQuaternionCannon.eulerAngles.x,cannonSocketTransform.eulerAngles.y,cannonSocketTransform.eulerAngles.z);

        // TODO: This is a hotfix, because the cockpit also gets x & z rotation from above line >:-(
        cannonSocketTransform.localEulerAngles = new(cannonSocketTransform.localEulerAngles.x, 0, 0);

/*        var cockpitRotation = lookAtQuaternion;
        cockpitRotation.x = 0;
        cockpitRotation.z = 0;
        cockpitRotation.Normalize();

        // // var cannonSocketRotation = lookAtQuaternion2;
        // // cannonSocketRotation.x = Mathf.Abs(cannonSocketRotation.x);  // TODO: Allow slight down rotation
        // // cannonSocketRotation.y = 0;
        // // cannonSocketRotation.z = 0;
        // // cannonSocketRotation.Normalize();
        // _cannonSocketTransform.rotation = cannonSocketRotation;
        // _cannonSocketTransform.eulerAngles = Vector3.right * lookAtQuaternion.eulerAngles.x;
        _cannonSocketTransform.eulerAngles = new Vector3(45,0,0);
        print(_cannonSocketTransform.localEulerAngles);
        
        // var euler = cockpitRotation.eulerAngles;

        // euler.x = 0;  // z is already 0

        // _cockpitTransform.eulerAngles = euler;
        _cockpitTransform.rotation = cockpitRotation;  // It's not localRotation, because LookAt() is in world coordinates
*/

        // _cockpitTransform.LookAt(_targetToShootAt.transform.position);  // Instant look at
    }

    public bool IsFriendly(GameObject unit)
    {
        // return CompareTag("Unit") && unit.CompareTag("UnitEnemy") || CompareTag("UnitEnemy") && unit.CompareTag("Unit");  // hostile logic
        return CompareTag(unit.tag);
    }

    public void TakeDamage(float damage)
    {
        _lastDamagedTime = Time.time;
        
        if (currentShield > 0)
        {
            currentShield -= damage; // Apply damage to shield
            UpdateShieldBar();

            if (currentShield < 0)
            {
                currentArmor += currentShield; // Apply damage to armor, if damage exceeds the shield level (this actually subtracts)
                currentShield = 0;
                UpdateArmorBar();
            }
        }
        else
        {
            currentArmor -= damage;
            UpdateArmorBar();
        }

        if (currentArmor <= 0)
            ProcessDestroy(gameObject);
    }

    void RegenerateShield()
    {
        if (currentShield == shield || Time.time - _lastDamagedTime < 4) return;  // Fuck the inspection warning, should be ok.

        currentShield += .15f;
        currentShield = Mathf.Min(currentShield, shield);

        UpdateShieldBar();
    }
    
    // TODO: ► Needed only when unit or camera transforms or shield is regenerated
    void AlignStatusInfo()
    {
        _statusInfoTransform.LookAt(mainCameraTransform);
        var eulerAngles = _statusInfoTransform.eulerAngles;
        eulerAngles = new (eulerAngles.x, mainCameraTransform.eulerAngles.y + 180, eulerAngles.z);
        _statusInfoTransform.eulerAngles = eulerAngles;

        // This also works
        // var forward = mainCameraTransform.position - _statusInfoTransform.position;
        // forward.Normalize();
        // var up = Vector3.Cross(forward, - mainCameraTransform.right);
        // _statusInfoTransform.rotation = Quaternion.LookRotation(forward, up);
    }

    void UpdateShieldBar()
    {
        _shieldBar.UpdateParams(currentShield / shield);
    }

    void UpdateArmorBar()
    {
        _armorBar.UpdateParams(currentArmor / armor);
    }

    void FindPath(Vector3 targetPosition)
    {
        _pathFinderAgent.Update();  //this function called cause agent cache it position
        _pathFinderAgent.SetGoalMoveHere(targetPosition);  //here we requesting path
    }

    public static void FindPaths()  // Called regularly, just for units that need it.
    {
        foreach (Unit unit in UnitsThatNeedRegularPathUpdate)
            unit.FindPath(unit._target.transform.position);
    }

    // If I understand it right, this is called when unit path is computed.
    void ReceivePathDelegate(Path path)
    {
        _pathPoints.Clear();

        for (int i = 0; i < path.count; ++i)
            _pathPoints.Add(path[i + path.currentIndex]);

         /*// For debug:
        print("════ PATH POINTS: ════");
        var pathCubesParent = Find("pathCubesParent");
        if (pathCubesParent) DestroyImmediate(pathCubesParent);
        pathCubesParent = new GameObject("pathCubesParent");
        foreach (Vector3 point in _pathPoints) {
            var cube = CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(pathCubesParent.transform);
            cube.transform.position = point;
            cube.transform.localScale = new (.2f, 4, .2f);
            Destroy(cube.GetComponent<BoxCollider>());
        }*/
    }
}