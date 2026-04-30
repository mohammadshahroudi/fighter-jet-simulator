using UnityEngine;
using UnityEngine.Rendering;

public class Bullet : MonoBehaviour
{
    // How long bullets actually fly before despawning
    [SerializeField] private float lifetime = 5f;
    // Need this so that bullets dont hit the player during evasive maneouvers
    [SerializeField] private float collisionDelay = 0.5f;

    [Header("Tracer")]
    [SerializeField] private bool enableTracerByDefault = true;
    [SerializeField] private float tracerTime = 0.08f;
    [SerializeField] private float tracerStartWidth = 0.06f;
    [SerializeField] private float tracerEndWidth = 0f;
    [SerializeField] private Color tracerStartColor = new Color(1f, 0.9f, 0.3f, 1f);
    [SerializeField] private Color tracerEndColor = new Color(1f, 0.2f, 0f, 0f);
    [SerializeField] private Material tracerMaterial;
    [SerializeField] private bool autoCreateTracerMaterial = true;
    [SerializeField] private float tracerMinVertexDistance = 0.02f;
    [SerializeField] private int tracerNumCapVertices = 2;
    [SerializeField] private LineAlignment tracerAlignment = LineAlignment.View;
    [SerializeField] private bool tracerReceiveShadows = false;
    [SerializeField] private ShadowCastingMode tracerShadowCasting = ShadowCastingMode.Off;

    private bool canCollide = false;
    private TrailRenderer tracer;

    void Awake()
    {
        if (enableTracerByDefault)
        {
            EnableTracer();
        }
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
        Invoke(nameof(EnableCollision), collisionDelay);
    }

    public void EnableTracer()
    {
        if (tracer == null)
        {
            tracer = GetComponent<TrailRenderer>();
            if (tracer == null)
            {
                tracer = gameObject.AddComponent<TrailRenderer>();
            }
        }

        tracer.enabled = true;
        tracer.emitting = true;
        tracer.time = tracerTime;
        tracer.startWidth = tracerStartWidth;
        tracer.endWidth = tracerEndWidth;
        tracer.startColor = tracerStartColor;
        tracer.endColor = tracerEndColor;
        tracer.minVertexDistance = tracerMinVertexDistance;
        tracer.alignment = tracerAlignment;
        tracer.shadowCastingMode = tracerShadowCasting;
        tracer.receiveShadows = tracerReceiveShadows;
        tracer.numCapVertices = tracerNumCapVertices;

        if (tracer.sharedMaterial == null)
        {
            if (tracerMaterial != null)
            {
                tracer.sharedMaterial = tracerMaterial;
            }
            else if (autoCreateTracerMaterial)
            {
                Shader spriteShader = Shader.Find("Sprites/Default");
                if (spriteShader != null)
                {
                    tracer.sharedMaterial = new Material(spriteShader);
                }
            }
        }
    }

    void EnableCollision()
    {
        canCollide = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (canCollide)
        {
            Destroy(gameObject);
        }
    }
}
