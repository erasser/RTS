using UnityEngine;

public class CachedMonoBehaviour : MonoBehaviour
{
    [HideInInspector]
    public Transform transformCached;
    [HideInInspector]
    public GameObject gameObjectCached;
    [HideInInspector]
    public Rigidbody rigidBody;

    void Awake()
    {
        gameObjectCached = gameObject;
        transformCached = gameObjectCached.transform;
        rigidBody = gameObjectCached.GetComponent<Rigidbody>();  // Beware, could be null
    }
}
