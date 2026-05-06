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

    public void Initialise(
        LineRenderer lineRenderer,
        Transform start,
        Transform targetTransform,
        float duration,
        bool shouldTrack,
        Vector3 endPoint,
        Vector3 offset
    )
    {
        line = lineRenderer;
        startPoint = start;
        target = targetTransform;
        lifeTimer = duration;

        trackTarget = shouldTrack;
        fixedEndPoint = endPoint;
        targetOffset = offset;

        if (line != null)
        {
            line.useWorldSpace = true;
            line.positionCount = 2;
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
        {
            endPos = target.position + targetOffset;
        }
        else
        {
            endPos = fixedEndPoint;
        }

        line.SetPosition(0, startPos);
        line.SetPosition(1, endPos);

        if (lifeTimer <= 0f)
            Destroy(gameObject);
    }
}