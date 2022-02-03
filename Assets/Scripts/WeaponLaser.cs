using UnityEngine;
using VolumetricLines;

public class WeaponLaser : MonoBehaviour
{
    GameObject _laserGameObject;  // Object holding VolumetricLines component to be able to toggle active state.
    VolumetricLineBehavior _laserComponent;
    public Unit unit;

    private void Start()
    {
        _laserGameObject = unit.cannonSocketTransform.Find("laser").gameObject;
        _laserGameObject.SetActive(false);
        _laserComponent = _laserGameObject.GetComponent<VolumetricLineBehavior>();
    }

    private void FixedUpdate()
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

        unit.toShootTargetDirection = unit.targetToShootAt.transform.position - unit.cockpitTransform.position + Vector3.up * .08f;

        unit.UpdateCockpitAndCannonRotation();
        
        var angle = Vector3.Angle(unit.toShootTargetDirection, unit.cannonSocketTransform.forward);
        if (angle > 5)
        {
            _laserGameObject.SetActive(false);
            return;
        }

        // TODO: ► If hits hostile non-targeted unit, shorten the laser
        _laserGameObject.SetActive(true);
        var distance = SquareRoot.GetValue(unit.toShootTargetDirection.sqrMagnitude);
        if (Physics.Raycast(unit.cockpitTransform.position, unit.toShootTargetDirection, out RaycastHit selectionHit, distance /*, GameController.instance.groundLayer*/))
        {
            // A friendly unit or the ground is in laser's way => disable the laser
            if (unit.IsFriendly(selectionHit.collider.gameObject) /*&& selectionHit.collider.gameObject != targetToShootAt*/ ||
                selectionHit.collider.name == "ground")
                _laserGameObject.SetActive(false);
            // else
            //     _laser.SetActive(true);
        }
        // else
        //     _laser.SetActive(true);

        _laserComponent.EndPos = Vector3.forward * distance; // laser length
    }
   
}
