using UnityEngine;

public class Rotate : MonoBehaviour
{
	public GameObject plane;

	public void Update()
	{
		plane.transform.Rotate(0, 20 * Time.deltaTime, 0);
	}
}
