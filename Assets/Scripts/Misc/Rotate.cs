using UnityEngine;

public class Rotate : MonoBehaviour
{
	public GameObject Object;

	public void Update()
	{
		Object.transform.Rotate(0, 0,  20 * Time.deltaTime);
	}
}
