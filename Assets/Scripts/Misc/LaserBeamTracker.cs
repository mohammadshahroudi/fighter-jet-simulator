using UnityEngine;

public class LaserBeamTracker : MonoBehaviour
{
    private LineRenderer line;
    private Transform startPoint;
    private Transform target;
    private float lifeTimer;

    private bool trackTarget;
    private Vector3 fixedEndPoint;
    private Vector3 targetOffset;

    private float bendAmount;
    private float bendWobbleSpeed;

    public void Initialise(
        LineRenderer lineRenderer,
        Transform start,
        Transform targetTransform,
        float duration,
        bool shouldTrack,
        Vector3 endPoint,
        Vector3 offset,
        float bend = 8f,
        float wobbleSpeed = 12f
    )
    {
        line = lineRenderer;
        startPoint = start;
        target = targetTransform;
        lifeTimer = duration;

        trackTarget = shouldTrack;
        fixedEndPoint = endPoint;
        targetOffset = offset;

        bendAmount = bend;
        bendWobbleSpeed = wobbleSpeed;

        if (line != null)
        {
            line.useWorldSpace = true;
            line.positionCount = 3;
            line.enabled = true;
        }
    }

    private void LateUpdate()
    {
        if (line == null || startPoint == null)
        {
            Destroy(gameObject);
            return;
        }

        lifeTimer -= Time.deltaTime;

        Vector3 startPos = startPoint.position;

        Vector3 endPos;
        if (trackTarget && target != null)
            endPos = target.position + targetOffset;
        else
            endPos = fixedEndPoint;

        Vector3 midPos = (startPos + endPos) * 0.5f;

        Vector3 beamDir = (endPos - startPos).normalized;
        Vector3 bendDir = Vector3.Cross(beamDir, Vector3.up);

        if (bendDir.sqrMagnitude < 0.001f)
            bendDir = Vector3.Cross(beamDir, Vector3.right);

        bendDir.Normalize();

        float wobble = Mathf.Sin(Time.time * bendWobbleSpeed) * bendAmount;
        midPos += bendDir * wobble;

        line.SetPosition(0, startPos);
        line.SetPosition(1, midPos);
        line.SetPosition(2, endPos);

        if (lifeTimer <= 0f)
            Destroy(gameObject);
    }
}