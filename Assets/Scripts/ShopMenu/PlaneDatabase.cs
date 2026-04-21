using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Plane/PlaneDatabase")]
public class PlaneDatabase : ScriptableObject
{
    public List<PlaneData> allPlanes;
}
