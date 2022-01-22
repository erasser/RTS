using UnityEngine;

public class Tank : MonoBehaviour
{
    public static Tank instance;
    bool _touchedGround;
    public Rigidbody rb;
    [Range(1, 20)]
    public int speed = 9;
    
    void Start()
    {
        instance = this;
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (_touchedGround && rb.velocity.sqrMagnitude < 36)
        {
            rb.AddForce(transform.forward * speed, ForceMode.Impulse);
            // Debug.DrawRay(transform.position, transform.forward * 10, Color.yellow);
        }
    }

    private void OnCollisionEnter()
    {
        _touchedGround = true;
    }
}
