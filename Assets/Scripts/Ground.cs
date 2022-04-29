using UnityEngine;

// ► NOT USED, I will use raycaster instead

// I'm not going to use this. Raycaster should be more performant.

// TODO: Cache gameObject.GetComponent<Unit>() if this approach is used. It could be cached in Unis.cs.

public class Ground : MonoBehaviour
{
    // Zde jsem skončil - wheel collider does not cause collision?
    
    void OnCollisionEnter(Collision other)
    {
        print("collision");
        other.collider.gameObject.GetComponent<Unit>().SetIsStayingOnSomething(true);
    }

    void OnCollisionExit(Collision other)
    {
        other.collider.gameObject.GetComponent<Unit>().SetIsStayingOnSomething(false);
    }
}
