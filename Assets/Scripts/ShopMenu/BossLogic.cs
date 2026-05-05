using UnityEngine;

public class BossLogic : MonoBehaviour
{
    public Transform player;

    public float distanceInFront = 150f;
    public float strafeHeight = 50f;
    public float strafeSpeed = 25f;
    public float strafeWidth = 200f;
    public float chaseTurnSpeed = 4f;
    public float lookAheadDistance = 70f;

    void Start()
    {
        if (player == null)
        {
            enabled = false;
            return;
        }

        PlaceBossInFrontOfPlayer();
    }

    void Update()
    {
        ChasePlayerFromSky();
    }

    void PlaceBossInFrontOfPlayer()
    {
        if (player == null)
        {
            return;
        }

        Vector3 forward = player.forward;
        Vector3 right = player.right;

        float randomLateral = Random.Range(-strafeWidth * 0.5f, strafeWidth * 0.5f);
        Vector3 spawnPosition = player.position + forward * distanceInFront + right * randomLateral;
        spawnPosition.y = player.position.y + strafeHeight;

        transform.position = spawnPosition;
        transform.LookAt(player);
    }

    void ChasePlayerFromSky()
    {
        Vector3 forward = player.forward;
        Vector3 right = player.right;

        float strafePhase = Mathf.PingPong(Time.time * strafeSpeed, strafeWidth);
        float lateralOffset = strafePhase - (strafeWidth * 0.5f);

        Vector3 targetPosition = player.position + forward * distanceInFront + right * lateralOffset;
        targetPosition.y = player.position.y + strafeHeight;

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 2f);

        Vector3 lookTarget = player.position + forward * lookAheadDistance;
        lookTarget.y = player.position.y + strafeHeight;

        Vector3 lookDirection = (lookTarget - transform.position).normalized;

        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, chaseTurnSpeed * Time.deltaTime);
        }
    }
}