// Notice: This file may contain project-specific implementation details.
// Do not upload or share with external AI tools unless you have permission.

using UnityEngine;
using UnityEngine.Rendering;

// All of these probe settings were based off of a probe I had already made beforehand
// This approach is so that the reflection probe is realtime and updates constantly.
// As of 4/16/2026, the probe updates realtime. But later I will update it only when a 
// new chunk is generated into the world scene to hopefully cut down on how expensive
// realtime updates are.

// Here is where the Event Listener stuff should be added

// As of 4-22-2026 this entire will be unused. Going to come back to it for the third milestone to make stuff look "good."

public class ProbleHandler : MonoBehaviour
{
    [Header("Probe Settings")]
    public Vector3 probePosition = new Vector3(0f, 50f, 0f);
    public Vector3 probeSize = new Vector3(10f, 10f, 10f);
    public Vector3 boxOffset = Vector3.zero;

    public int importance = 1;
    public float intensity = 1f;
    public bool boxProjection = true;
    public float blendDistance = 1f;

    public int resolution = 64;
    public bool hdr = true;
    public float shadowDistance = 100f;
    public ReflectionProbeClearFlags clearFlags = ReflectionProbeClearFlags.Skybox;
    public Color backgroundColor = Color.black;
    public LayerMask cullingMask = ~0;
    public float nearClip = 0.1f;
    public float farClip = 1000f;

    public ReflectionProbeTimeSlicingMode timeSlicing =
        ReflectionProbeTimeSlicingMode.NoTimeSlicing;

    private ReflectionProbe probeComponent;

    void Awake()
    {
        CreateRealtimeProbe();
    }

    void CreateRealtimeProbe()
    {
        GameObject probeGameObject = new GameObject("Realtime Reflection Probe");
        probeGameObject.transform.SetParent(transform);
        probeGameObject.transform.position = probePosition;

        probeComponent = probeGameObject.AddComponent<ReflectionProbe>();

        probeComponent.size = probeSize;
        probeComponent.center = boxOffset;

        probeComponent.mode = ReflectionProbeMode.Realtime;
        probeComponent.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        probeComponent.timeSlicingMode = timeSlicing;

        probeComponent.importance = importance;
        probeComponent.intensity = intensity;
        probeComponent.boxProjection = boxProjection;
        probeComponent.blendDistance = blendDistance;
        
        // This is the important line, it lets the probe update every frame.
        // This definitely does not sound ideal, I will change this later on.
        probeComponent.refreshMode = ReflectionProbeRefreshMode.EveryFrame;

        probeComponent.resolution = resolution;
        probeComponent.hdr = hdr;
        probeComponent.shadowDistance = shadowDistance;
        probeComponent.clearFlags = clearFlags;
        probeComponent.backgroundColor = backgroundColor;
        probeComponent.cullingMask = cullingMask;
        probeComponent.nearClipPlane = nearClip;
        probeComponent.farClipPlane = farClip;

        probeComponent.RenderProbe();
    }

    // Unused as of 4/18/2026
    public void RefreshProbeNow()
    {
        if (probeComponent == null)
            return;

        probeComponent.RenderProbe();
    }
}