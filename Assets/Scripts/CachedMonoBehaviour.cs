using UnityEngine;

public class CachedMonoBehaviour : MonoBehaviour
{
    public Transform transformCached;
    public GameObject gameObjectCached;
    public Rigidbody rigidBody;

    void Awake()
    {
        gameObjectCached = gameObject;
        transformCached = gameObjectCached.transform;
        rigidBody = gameObjectCached.GetComponent<Rigidbody>();  // Beware, could be null
    }
}
