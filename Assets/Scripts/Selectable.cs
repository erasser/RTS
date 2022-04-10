using System.Collections.Generic;
using UnityEngine;
using cakeslice;
using static GameController;

public class Selectable : CachedMonoBehaviour
{
    public List<Outline> outlineComponents = new ();

    void Start()
    {
        SetOutlineComponents();
    }

    void SetOutlineComponents()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>())  // Get all children recursively
        {
            var outlineComponent = child.GetComponent<Outline>();
            if (!outlineComponent) continue;

            outlineComponents.Add(outlineComponent);
            outlineComponent.enabled = false;
        }
    }

    public void ToggleOutline(bool enable)
    {
        if (outlineComponents[0].enabled == enable) return;

        foreach (Outline outline in outlineComponents)
            outline.enabled = enable;
    }

    void OnDestroy()
    {
        if (selectedObject == gameObjectCached)
            UnselectObject();
    }
}
