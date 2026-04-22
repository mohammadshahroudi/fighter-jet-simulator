using UnityEngine;

public class PlayerPlaneLoader : MonoBehaviour
{
  [SerializeField] private PlaneDatabase planeDatabase;

void Start()
{
    string equippedPlaneId = ShopPersistence.GetSavedEquippedPlaneId();
    var allPlanes = planeDatabase.allPlanes;
    PlaneData equippedPlane = allPlanes.Find(plane => plane != null && plane.PlaneId == equippedPlaneId);

   
    if (transform.childCount > 0)
    {
    Destroy(transform.GetChild(0).gameObject);
    }
    // Add equipped plane as child
    if (equippedPlane != null && equippedPlane.PlanePrefab != null)
    {
        Instantiate(equippedPlane.PlanePrefab, transform);
    }
    else
    {
        Debug.LogError("Equipped plane not found or prefab missing!");
    }
}
}
