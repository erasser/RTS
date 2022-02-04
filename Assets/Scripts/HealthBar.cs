using UnityEngine;
// https://www.stevestreeting.com/2019/02/22/enemy-health-bars-in-1-draw-call-in-unity/

/// <summary>
/// Displays a configurable health bar for any object with a Damageable as a parent
/// </summary>
class HealthBar : MonoBehaviour {
    MaterialPropertyBlock _matBlock;
    MeshRenderer _meshRenderer;
    // Unit _unit; 
    static readonly int Fill = Shader.PropertyToID("_Fill");

    void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _matBlock = new MaterialPropertyBlock();
        UpdateParams(1);
        // _damageable = GetComponentInParent<Damageable>();  // Get the damageable parent we're attached to
        // _unit = GetComponentInParent<Unit>();
    }

    void FixedUpdate()
    {
        // Only display on partial health
        // if (_damageable.CurrentHealth < _damageable.maxHealth) {
        //     _meshRenderer.enabled = true;
        //     AlignCamera();
        //     UpdateParams();
        // } else {
        //     _meshRenderer.enabled = false;
        // }

        AlignCamera();
    }

    public void UpdateParams(float phase)
    {
        _meshRenderer.GetPropertyBlock(_matBlock);
        _matBlock.SetFloat(Fill, phase);
        _meshRenderer.SetPropertyBlock(_matBlock);
    }

    void AlignCamera()
    {
        // transform.LookAt(GameController.mainCamera.transform);  // Works, but it's back-faced
        
        var cameraTransform = GameController.mainCamera.transform;
        var forward = transform.position - cameraTransform.position;
        forward.Normalize();
        var up = Vector3.Cross(forward, cameraTransform.right);
        transform.rotation = Quaternion.LookRotation(forward, up);
    }
}