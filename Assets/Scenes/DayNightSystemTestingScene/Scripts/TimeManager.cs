using System;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TimeManager : MonoBehaviour {
    [SerializeField] TextMeshProUGUI timeText;
    
    [SerializeField] Light sun;
    [SerializeField] Light moon;
    [SerializeField] AnimationCurve lightIntensityCurve;
    [SerializeField] float maxSunIntensity = 1;
    [SerializeField] float maxMoonIntensity = 0.5f;
    
    [SerializeField] Color dayAmbientLight;
    [SerializeField] Color nightAmbientLight;
    [SerializeField] Volume volume;
    [SerializeField] Material skyboxMaterial;
    
    // [SerializeField] RectTransform dial;
    float initialDialRotation;
    
    ColorAdjustments colorAdjustments;
    
    [SerializeField] TimeSettings timeSettings;
    
    public event Action OnSunrise {
        add => service.OnSunrise += value;
        remove => service.OnSunrise -= value;
    }
    
    public event Action OnSunset {
        add => service.OnSunset += value;
        remove => service.OnSunset -= value;
    }
    
    public event Action OnHourChange {
        add => service.OnHourChange += value;
        remove => service.OnHourChange -= value;
    }    

    TimeService service;

    void Start() {
        service = new TimeService(timeSettings);
        volume.profile.TryGet(out colorAdjustments);
        
        if (skyboxMaterial != null)
        {
            RenderSettings.skybox = skyboxMaterial;
            DynamicGI.UpdateEnvironment();
        }
        
        OnSunrise += () => Debug.Log("Sunrise");
        OnSunset += () => Debug.Log("Sunset");
        OnHourChange += () => Debug.Log("Hour change");
        
        // initialDialRotation = dial.rotation.eulerAngles.z;
    }

    void Update() {
        UpdateTimeOfDay();
        RotateSun();
        UpdateLightSettings();
        UpdateSkyBlend();
        
        if (Input.GetKeyDown(KeyCode.Space)) {
            timeSettings.timeMultiplier *= 2;
        }
        if (Input.GetKeyDown(KeyCode.LeftShift)) {
            timeSettings.timeMultiplier /= 2;
        }
    }

    void UpdateSkyBlend()
    {
        if (skyboxMaterial == null || sun == null)
        {
            return;
        }

        float dotProduct = Vector3.Dot(sun.transform.forward, Vector3.up);
        float blend = Mathf.Lerp(0, 1, lightIntensityCurve.Evaluate(dotProduct));

        skyboxMaterial.SetFloat("_Blend", blend);

        skyboxMaterial.SetVector("_SunDirection", -sun.transform.forward);

        if (moon != null)
        {
            skyboxMaterial.SetVector("_MoonDirection", -moon.transform.forward);
        }

        float normalizedTime = (service.CurrentTime.Hour + service.CurrentTime.Minute / 60f) / 24f;
        
        // 180 degrees over a full in-game day
        float starRotation = normalizedTime * 180f;

        skyboxMaterial.SetFloat("_StarRotation", starRotation);
    }
    
    void UpdateLightSettings()
    {
        float dotProduct = Vector3.Dot(sun.transform.forward, Vector3.down);
        float lightIntensity = lightIntensityCurve.Evaluate(dotProduct);

        sun.intensity = Mathf.Lerp(0, maxSunIntensity, lightIntensity);

        if (moon != null)
        {
            moon.intensity = Mathf.Lerp(maxMoonIntensity, 0, lightIntensity);
        }

        if (colorAdjustments == null)
        {
            return;
        }

        colorAdjustments.colorFilter.value = Color.Lerp(nightAmbientLight, dayAmbientLight, lightIntensity);
    }

    void RotateSun()
    {
        float rotation = service.CalculateSunAngle();

        sun.transform.rotation = Quaternion.AngleAxis(rotation, Vector3.right);

        if (moon != null)
        {
            moon.transform.rotation = Quaternion.AngleAxis(rotation + 180f, Vector3.right);
        }
    }

    void UpdateTimeOfDay() {
        service.UpdateTime(Time.deltaTime);
        if (timeText != null) {
            timeText.text = service.CurrentTime.ToString("hh:mm");
        }
    }
}