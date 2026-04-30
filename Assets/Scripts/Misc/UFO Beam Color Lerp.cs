using UnityEngine;

public class UFOBeamColorLerp : MonoBehaviour
{
    [SerializeField] public MeshRenderer[] meshRendererArray;
    [SerializeField] public Color[] colorList;
    [SerializeField] public float colorSwitchSpeed;

    
    private int currentColorIndex = 0;
    private int targetColorIndex = 1;
    private float interpolationVal;

    // color.lerp transitions you from colors on a linear basis. 0-1 represents such line, with the interpolation value representing transitional
    // colorLists between said line.

    void Start()
    {
        // transform.position = new Vector3(0, 2.0f, 0.5f);
        // transform.localScale = Vector3.one * 5.0f;
        colorSwitchSpeed = 1.0f;
    }

    void Update()
    {
        colorTransition();
        // transform.Rotate(0.0f, 10.0f * Time.deltaTime, 0.0f);
        
    }

    void colorTransition()
    {
        interpolationVal += Time.deltaTime / colorSwitchSpeed;
        // the interpolation value is now fps divded by the switchSpeed, increasing the transitional points between colors.

        Material[] materialList = new Material[meshRendererArray.Length];

        for (int i = 0; i < materialList.Length; i++)
        {
            materialList[i] = meshRendererArray[i].material;
        }
        
        // rendering the material, making material.color possible.
        // this time, each renderer for each wall is taken individually then rendered. 
        
        for (int i = 0; i < materialList.Length; i++)
        {
            materialList[i].color = Color.Lerp(colorList[currentColorIndex], colorList[targetColorIndex], interpolationVal);
            materialList[i].SetColor("_EmissionColor", materialList[i].color);
            materialList[i].EnableKeyword("_EMISSION");
        }
        
        
        // material.color = Color.Lerp(colorList[currentColorIndex], colorList[targetColorIndex], interpolationVal);
        // color lerp, currentcolor will be index 0, then targetcolor index will be 1

        if (interpolationVal >= 1)
        {
            // if the interpolation value is greater than 1, which is ALWAYS true, then proceed. this is all essentially just a big loop.
            interpolationVal = 0f;
            // reset the interpolationvalue 
            currentColorIndex = targetColorIndex;
            // the current color shifts to 1, making both our indexes 1.
            targetColorIndex++;
            // the current color is 1, then the target switches to 2. 
            if (targetColorIndex == colorList.Length)
            {
                targetColorIndex = 0;
                // if the index breaches the length of the index, reset the index. this will loop due to update happening every frame.
            }
        }
    }
    

}


