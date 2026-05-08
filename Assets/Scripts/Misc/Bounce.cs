using UnityEngine;

public class Bounce : MonoBehaviour
{
   [SerializeField] private float amp;
   [SerializeField] private float freq;

   private void Update()
   {
      transform.position = new Vector3(transform.position.x, Mathf.Sin(Time.time * freq) * amp, transform.position.z); 
   }
}
