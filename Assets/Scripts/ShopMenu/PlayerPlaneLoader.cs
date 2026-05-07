using UnityEngine;

public class PlayerPlaneLoader : MonoBehaviour
{
  [SerializeField] private PlaneList planeDatabase;
    [SerializeField] private PlayerStats playerStats;
        [SerializeField] private bool disableGunsOnSpawn;

void Start()
{
    string equippedPlaneId = ShopPersistence.GetSavedEquippedPlaneId();
    var allPlanes = planeDatabase.allPlanes;
    PlaneData equippedPlane = allPlanes.Find(plane => plane != null && plane.PlaneId == equippedPlaneId);
    PlaneData defaultPlane = allPlanes.Find(plane => plane != null && plane.PlaneId == "Falcon X1");

    if (transform.childCount > 0)
    {
        Destroy(transform.GetChild(0).gameObject);
    }
    // Add equipped plane as child
    if (equippedPlane != null && equippedPlane.PlanePrefab != null)
    {
        // Spawn the equipped plane and capture the instance
        GameObject spawned = Instantiate(equippedPlane.PlanePrefab, transform);
        ApplyGunState(spawned);

        // Set player health to match equipped plane
        if (playerStats != null)
            playerStats.Initialise(equippedPlane.Health);

        // Initialize controller speed
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.InitialiseSpeed(equippedPlane.Speed);
        }

        // Initialize gun damage on the spawned plane (if it has a GunLogic)
        if (spawned != null)
        {
            GunLogic gun = spawned.GetComponentInChildren<GunLogic>();
            if (gun != null)
            {
                gun.InitialiseDamage(equippedPlane.Damage);
            }
        }
    }
    else
    {
        Debug.LogError("Equipped plane not found or prefab missing! Using default plane instead.");
        GameObject spawned = Instantiate(defaultPlane.PlanePrefab, transform);
        ApplyGunState(spawned);
    }
}

    private void ApplyGunState(GameObject spawnedPlane)
    {
        if (spawnedPlane == null)
        {
            return;
        }

        foreach (Transform child in spawnedPlane.GetComponentsInChildren<Transform>(true))
        {
            if (child.CompareTag("Gun"))
            {
                child.gameObject.SetActive(!disableGunsOnSpawn);
            }
        }
    }
}
