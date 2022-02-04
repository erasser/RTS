using UnityEngine;
// https://www.stevestreeting.com/2019/02/22/enemy-health-bars-in-1-draw-call-in-unity/

/// <summary>
/// Displays a configurable health bar for any object with a Damageable as a parent
/// </summary>
class HealthBar : MonoBehaviour {
    MaterialPropertyBlock _matBlock;
    MeshRenderer _meshRenderer;
    static readonly int Fill = Shader.PropertyToID("_Fill");

    void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _matBlock = new MaterialPropertyBlock();
    }

    public void UpdateParams(float phase)
    {
        _meshRenderer.GetPropertyBlock(_matBlock);
        _matBlock.SetFloat(Fill, phase);
        _meshRenderer.SetPropertyBlock(_matBlock);
    }

}