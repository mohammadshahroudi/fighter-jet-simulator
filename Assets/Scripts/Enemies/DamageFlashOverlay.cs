using System.Collections;
using UnityEngine;

public class DamageFlashOverlay : MonoBehaviour
{
    [Header("Overlay Root")]
    [Tooltip("Duplicate visual-only hierarchy with transparent white material")]
    [SerializeField] private GameObject flashVisualRoot;

    [Header("Fade")]
    [SerializeField] private float flashPeakAlpha = 0.7f;
    [SerializeField] private float fadeInTime = 0.03f;
    [SerializeField] private float fadeOutTime = 0.12f;

    [Header("Shader Property Names")]
    [SerializeField] private string colorProperty = "_Color";
    [SerializeField] private string baseColorProperty = "_BaseColor";

    private Renderer[] _renderers;
    private Material[] _materials;
    private Color[] _baseColors;
    private Coroutine _flashRoutine;

    private int _colorId;
    private int _baseColorId;

    private void Awake()
    {
        _colorId = Shader.PropertyToID(colorProperty);
        _baseColorId = Shader.PropertyToID(baseColorProperty);

        if (flashVisualRoot == null)
        {
            Debug.LogWarning($"{name}: No flashVisualRoot assigned.", this);
            return;
        }

        _renderers = flashVisualRoot.GetComponentsInChildren<Renderer>(true);

        _materials = new Material[_renderers.Length];
        _baseColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;

            // material creates an instance for this prefab instance
            Material mat = _renderers[i].material;
            _materials[i] = mat;

            if (mat.HasProperty(_baseColorId))
                _baseColors[i] = mat.GetColor(_baseColorId);
            else if (mat.HasProperty(_colorId))
                _baseColors[i] = mat.GetColor(_colorId);
            else
                _baseColors[i] = new Color(1f, 1f, 1f, 0f);
        }

        SetOverlayAlpha(0f);
        flashVisualRoot.SetActive(false);
    }

    public void TriggerFlash()
    {
        if (flashVisualRoot == null || _materials == null || _materials.Length == 0)
            return;

        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);

        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        flashVisualRoot.SetActive(true);

        if (fadeInTime > 0f)
        {
            float timer = 0f;
            while (timer < fadeInTime)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / fadeInTime);
                SetOverlayAlpha(Mathf.Lerp(0f, flashPeakAlpha, t));
                yield return null;
            }
        }

        SetOverlayAlpha(flashPeakAlpha);

        if (fadeOutTime > 0f)
        {
            float timer = 0f;
            while (timer < fadeOutTime)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / fadeOutTime);
                SetOverlayAlpha(Mathf.Lerp(flashPeakAlpha, 0f, t));
                yield return null;
            }
        }

        SetOverlayAlpha(0f);
        flashVisualRoot.SetActive(false);
        _flashRoutine = null;
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (_materials == null) return;

        for (int i = 0; i < _materials.Length; i++)
        {
            Material mat = _materials[i];
            if (mat == null) continue;

            Color c = _baseColors[i];
            c.a = alpha;

            if (mat.HasProperty(_baseColorId))
                mat.SetColor(_baseColorId, c);

            if (mat.HasProperty(_colorId))
                mat.SetColor(_colorId, c);
        }
    }
}