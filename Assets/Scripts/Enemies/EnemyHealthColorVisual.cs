using UnityEngine;

public class EnemyHealthColorVisual : MonoBehaviour
{
    [Header("Health Source")]
    [SerializeField] private EnemyHealth enemyHealth;

    [Header("Colors")]
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color midHealthColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float colorLerpSpeed = 10f;

    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();

        if (enemyHealth == null)
            enemyHealth = GetComponentInParent<EnemyHealth>();

        if (_renderer == null)
            Debug.LogWarning($"{name}: No Renderer found for EnemyHealthColorVisual.", this);

        if (enemyHealth == null)
            Debug.LogWarning($"{name}: No EnemyHealth found in parents.", this);
    }

    private void Update()
    {
        if (_renderer == null || enemyHealth == null)
            return;

        float t = Mathf.Clamp01(enemyHealth.HealthFraction);
        Color targetColor = EvaluateHealthColor(t);

        _renderer.GetPropertyBlock(_mpb);

        Color currentColor = targetColor;

        Material sharedMat = _renderer.sharedMaterial;
        if (sharedMat != null)
        {
            if (sharedMat.HasProperty(BaseColorId))
                currentColor = _mpb.isEmpty ? sharedMat.GetColor(BaseColorId) : _mpb.GetColor(BaseColorId);
            else if (sharedMat.HasProperty(ColorId))
                currentColor = _mpb.isEmpty ? sharedMat.GetColor(ColorId) : _mpb.GetColor(ColorId);
        }

        Color nextColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorLerpSpeed);

        if (sharedMat != null)
        {
            if (sharedMat.HasProperty(BaseColorId))
                _mpb.SetColor(BaseColorId, nextColor);

            if (sharedMat.HasProperty(ColorId))
                _mpb.SetColor(ColorId, nextColor);
        }

        _renderer.SetPropertyBlock(_mpb);
    }

    private Color EvaluateHealthColor(float t)
{
    Color orange = new Color(1f, 0.5f, 0f);

    if (t > 0.5f)
    {
        return Color.Lerp(orange, fullHealthColor, (t - 0.5f) * 2f);
    }
    else
    {
       return Color.Lerp(lowHealthColor, midHealthColor, t * 2f);
    }
}
}