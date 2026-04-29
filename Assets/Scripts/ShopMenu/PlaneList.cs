using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Plane/PlaneDatabase")]
public class PlaneList : ScriptableObject
{
    public List<PlaneData> allPlanes;
}
