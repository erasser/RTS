using UnityEngine;
using VolumetricLines;

public class WeaponLaser : MonoBehaviour
{
    GameObject _laserGameObject;  // Object holding VolumetricLines component to be able to toggle active state.
    VolumetricLineBehavior _laserComponent;
    public Unit unit;
    GameObject _hostileHitUnitGameObject;   // Cached last hit hostile unit
    Unit _hostileHitUnit;                   // Cached last hit hostile unit's Unit component

    void Start()
    {
        _laserGameObject = unit.cannonSocketTransform.Find("laser").gameObject;
        _laserGameObject.SetActive(false);
        _laserComponent = _laserGameObject.GetComponent<VolumetricLineBehavior>();
    }

    void FixedUpdate()
    {
        UpdateLaserProps();
    }

    void UpdateLaserProps()  // Manages the direction and enabled state of laser
    {
        if (!unit.targetToShootAt)
        {
            _laserGameObject.SetActive(false);
            return;
        }
        
        SetHostileHitUnit(unit.targetToShootAt);

        unit.toShootTargetDirection = unit.targetToShootAt.transform.position - unit.cockpitTransform.position + Vector3.up * .08f;

        unit.UpdateCockpitAndCannonRotation();
        
        var angle = Vector3.Angle(unit.toShootTargetDirection, unit.cannonSocketTransform.forward);
        if (angle > 5)
        {
            _laserGameObject.SetActive(false);
            return;
        }

        // TODO: Consider to hit ground to satisfy the player?
        // var nonTargetedHostileHit = false;
        _laserGameObject.SetActive(true);
        var distance = SquareRoot.GetValue(unit.toShootTargetDirection.sqrMagnitude);  // laser length
        if (Physics.Raycast(unit.cockpitTransform.position, unit.toShootTargetDirection, out RaycastHit selectionHit, distance /*, GameController.instance.groundLayer*/))
        {
            // A friendly unit or the ground is in laser's way => disable the laser
            if (unit.IsFriendly(selectionHit.collider.gameObject) || selectionHit.collider.name == "ground")
            {
                _laserGameObject.SetActive(false);
                return;
            }
            // Enemy non-targeted unit => hit it
            /*else*/
            if (selectionHit.collider.gameObject != unit.targetToShootAt)
            {
                SetHostileHitUnit(selectionHit.collider.gameObject);
                SetLength(SquareRoot.GetValue((_hostileHitUnitGameObject.transform.position - unit.transform.position).sqrMagnitude) + .1f);

                _hostileHitUnit.TakeDamage(.1f);
                // nonTargetedHostileHit = true;
                return;
            }

            // Leave it here to test & prevent unit self casting
            // if (unit.gameObject == selectionHit.collider.gameObject)
            //     Debug.LogWarning("• unit self cast!");
        }

        // if (nonTargetedHostileHit) return;

        SetLength(distance);
        _hostileHitUnit.TakeDamage(.1f);
    }

    void SetHostileHitUnit(GameObject unitGameObject)
    {
        if (_hostileHitUnitGameObject && _hostileHitUnitGameObject == unitGameObject) return;

        _hostileHitUnitGameObject = unitGameObject;
        _hostileHitUnit = unitGameObject.GetComponent<Unit>();
    }

    /// <summary>
    /// Sets laser length.
    /// </summary>
    /// <param name="length">Laser length</param>
    void SetLength(float length)
    {
        _laserComponent.EndPos = Vector3.forward * length;
    }
}
