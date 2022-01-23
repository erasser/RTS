using UnityEngine;

public class Unit : MonoBehaviour
{
    public static bool touchedGround;
    public Rigidbody rb;
    [Range(1, 20)]
    public int speed = 9;
    static GameObject _target;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        _target = GameObject.Find("target");
        Time.timeScale = .5f;
    }

    void FixedUpdate()
    {
        if (!touchedGround) return;

        // if (touchedGround && rb.velocity.sqrMagnitude < 36)
        // {
            rb.AddForce(transform.forward * speed, ForceMode.Impulse);
        // }

        var toTargetV3 = _target.transform.position - transform.position;
        var toTargetV2 = new Vector2(toTargetV3.x, toTargetV3.z);
        var tankForwardV2 = new Vector2(transform.right.x, transform.right.z);  // IDFK why I must use 'right'

        // int sign = Vector2.Dot(tankForwardV2.normalized, toTargetV2.normalized) > 0 ? 1 : -1;
        float coefficient = Vector2.Dot(tankForwardV2.normalized, toTargetV2.normalized);
        // coefficient = 1 / coefficient;  // or 1 - coefficient 

        // rb.AddTorque(Vector3.up * coefficient * 2, ForceMode.Impulse);  // 400 in Update
        rb.transform.Rotate(Vector3.up * coefficient * 2);

        Debug.DrawRay(transform.position, transform.forward * 10, Color.yellow);
        Debug.DrawRay(transform.position, toTargetV3 * 10, Color.red);
        // print(Vector2.Dot(tankForwardV2, toTargetV2));
    }

    private void OnCollisionEnter()
    {
        touchedGround = true;
    }
}
