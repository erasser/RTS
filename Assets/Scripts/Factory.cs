using System.Collections.Generic;
using UnityEngine;
using static Unit;

public class Factory : CachedMonoBehaviour
{
    public List<GameObject> buildableUnitsPrefabs;
    public UnitSide unitSide;
    [HideInInspector]
    public Vector3 rallyPoint;

    public enum UnitSide
    {
        Player,
        Enemy
    }
    
    void Start()
    {
        rallyPoint = transform.Find("rallyPoint").position;
    }

    public void BuildUnitType1()
    {
        var unit = Instantiate(buildableUnitsPrefabs[0]);
        unit.name = $"{buildableUnitsPrefabs[0].name}_{PlayerUnits.Count}";
        var unitComponent = unit.GetComponent<Unit>();
        unitComponent.isPlayerUnit = unitSide == UnitSide.Player;
        unitComponent.createdAtFactory = this;
        PlayerUnits.Add(unitComponent);
    }

}
