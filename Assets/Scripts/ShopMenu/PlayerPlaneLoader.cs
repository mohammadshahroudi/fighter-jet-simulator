using UnityEngine;

public class PlayerPlaneLoader : MonoBehaviour
{
  [SerializeField] private PlaneList planeDatabase;
    [SerializeField] private PlayerStats playerStats;
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
        // Set player health to match equipped plane
        if (playerStats != null)
            playerStats.Initialise(equippedPlane.Health);
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.InitialiseSpeed(equippedPlane.Speed);
        }
    }
    else
    {
        Debug.LogError("Equipped plane not found or prefab missing!");
    }
}
}
