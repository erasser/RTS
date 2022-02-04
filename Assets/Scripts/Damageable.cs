// TODO: Not used. Remove this class.

using UnityEngine;
// https://www.stevestreeting.com/2019/02/22/enemy-health-bars-in-1-draw-call-in-unity/

public class Damageable : MonoBehaviour
{
    public int maxHealth;
    public float damageForceThreshold = 1f;
    public float damageForceScale = 5f;
    public int CurrentHealth { get; private set; }

    void Start()
    {
        CurrentHealth = maxHealth;
    }

    // TODO: Come code changing currentHealth
}
