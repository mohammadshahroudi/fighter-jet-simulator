using UnityEngine;

public class ParticleEffects : MonoBehaviour
{
    private ParticleSystem ps;

    private void Start()
    {
        ps = GetComponent<ParticleSystem>();
        Destroy(gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
    }
}
