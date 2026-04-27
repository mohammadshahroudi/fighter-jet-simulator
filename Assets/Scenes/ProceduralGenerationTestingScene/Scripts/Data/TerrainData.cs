using UnityEngine;
using System.Collections;

[CreateAssetMenu()]
public class TerrainData : UpdatableData {
	
    public float uniformScale = 2.5f; // Tutorial made this public now. Careful when changing this.

    public bool useFlatShading;
    public bool useFalloff;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
}