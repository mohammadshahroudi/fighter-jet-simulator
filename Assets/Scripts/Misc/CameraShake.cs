using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Vector3 _originalLocalPosition;
    private Coroutine _shakeRoutine;

    private void Awake()
    {
        _originalLocalPosition = transform.localPosition;
    }

    public void Shake(float duration, float strength)
    {
        if (_shakeRoutine != null)
            StopCoroutine(_shakeRoutine);

        _shakeRoutine = StartCoroutine(ShakeRoutine(duration, strength));
    }

    private IEnumerator ShakeRoutine(float duration, float strength)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            Vector3 offset = Random.insideUnitSphere * strength;
            offset.z = 0f;

            transform.localPosition = _originalLocalPosition + offset;

            yield return null;
        }

        transform.localPosition = _originalLocalPosition;
        _shakeRoutine = null;
    }
}