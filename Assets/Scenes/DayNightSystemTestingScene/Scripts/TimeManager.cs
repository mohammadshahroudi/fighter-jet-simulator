using System;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TimeManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TextMeshProUGUI timeText;

    [Header("Sun and Moon")]
    [SerializeField] Light sun;
    [SerializeField] Light moon;
    [SerializeField] AnimationCurve lightIntensityCurve;
    [SerializeField] float maxSunIntensity = 1f;
    [SerializeField] float maxMoonIntensity = 0.5f;

    [Header("Post Processing / Ambient")]
    [SerializeField] Color dayAmbientLight = Color.white;
    [SerializeField] Color nightAmbientLight = new Color(0.1f, 0.12f, 0.2f, 1f);
    [SerializeField] Volume volume;

    [Header("Skybox")]
    [SerializeField] Material skyboxMaterial;

    [Header("Fog")]
    [SerializeField] bool syncFogToSkybox = true;

    [Tooltip("If enabled, the fog uses the Day Horizon Color and Night Horizon Color from your skybox material.")]
    [SerializeField] bool useSkyboxHorizonColorsForFog = true;

    [SerializeField] Color dayFogColor = new Color(0.6f, 0.85f, 1.0f, 1f);
    [SerializeField] Color nightFogColor = new Color(0.02f, 0.04f, 0.10f, 1f);

    [Tooltip("How close the fog starts during daytime.")]
    [SerializeField] float dayFogStart = 220f;

    [Tooltip("How far the fog fully covers during daytime.")]
    [SerializeField] float dayFogEnd = 520f;

    [Tooltip("How close the fog starts during nighttime.")]
    [SerializeField] float nightFogStart = 100f;

    [Tooltip("How far the fog fully covers during nighttime.")]
    [SerializeField] float nightFogEnd = 320f;

    [Header("Time")]
    [SerializeField] TimeSettings timeSettings;

    ColorAdjustments colorAdjustments;
    TimeService service;

    public event Action OnSunrise
    {
        add => service.OnSunrise += value;
        remove => service.OnSunrise -= value;
    }

    public event Action OnSunset
    {
        add => service.OnSunset += value;
        remove => service.OnSunset -= value;
    }

    public event Action OnHourChange
    {
        add => service.OnHourChange += value;
        remove => service.OnHourChange -= value;
    }

    void Start()
    {
        service = new TimeService(timeSettings);

        if (volume != null && volume.profile != null)
        {
            volume.profile.TryGet(out colorAdjustments);
        }

        if (skyboxMaterial != null)
        {
            RenderSettings.skybox = skyboxMaterial;
            DynamicGI.UpdateEnvironment();
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;

        OnSunrise += () => Debug.Log("Sunrise");
        OnSunset += () => Debug.Log("Sunset");
        OnHourChange += () => Debug.Log("Hour change");
    }

    void Update()
    {
        UpdateTimeOfDay();
        RotateSun();

        float daylightAmount = UpdateLightSettings();

        UpdateSkyBlend(daylightAmount);
        UpdateFogSettings(daylightAmount);

        // For debug purposes. Speed up or slow down time.
        // Commented out for the final build (unless it's a feature at some point :eyes:)
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
        //     timeSettings.timeMultiplier *= 2;
        // }
        //
        // if (Input.GetKeyDown(KeyCode.LeftShift))
        // {
        //     timeSettings.timeMultiplier /= 2;
        // }
    }

    void UpdateTimeOfDay()
    {
        service.UpdateTime(Time.deltaTime);

        if (timeText != null)
        {
            timeText.text = service.CurrentTime.ToString("hh:mm");
        }
    }

    void RotateSun()
    {
        if (sun == null)
        {
            return;
        }

        float rotation = service.CalculateSunAngle();

        sun.transform.rotation = Quaternion.AngleAxis(rotation, Vector3.right);

        if (moon != null)
        {
            moon.transform.rotation = Quaternion.AngleAxis(rotation + 180f, Vector3.right);
        }
    }

    float UpdateLightSettings()
    {
        if (sun == null)
        {
            return 0f;
        }

        float dotProduct = Vector3.Dot(sun.transform.forward, Vector3.down);
        float lightIntensity = lightIntensityCurve.Evaluate(dotProduct);

        sun.intensity = Mathf.Lerp(0f, maxSunIntensity, lightIntensity);

        if (moon != null)
        {
            moon.intensity = Mathf.Lerp(maxMoonIntensity, 0f, lightIntensity);
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.colorFilter.value = Color.Lerp(
                nightAmbientLight,
                dayAmbientLight,
                lightIntensity
            );
        }

        return lightIntensity;
    }

    void UpdateSkyBlend(float daylightAmount)
    {
        if (skyboxMaterial == null || sun == null)
        {
            return;
        }

        /*
         * Your skybox shader uses _Blend like this:
         * 0 = day
         * 1 = night
         *
         * daylightAmount works like this:
         * 0 = night
         * 1 = day
         *
         * So we invert it before sending it to the skybox.
         */
        float skyBlend = 1f - daylightAmount;

        skyboxMaterial.SetFloat("_Blend", skyBlend);
        skyboxMaterial.SetVector("_SunDirection", -sun.transform.forward);

        if (moon != null)
        {
            skyboxMaterial.SetVector("_MoonDirection", -moon.transform.forward);
        }
    }

    void UpdateFogSettings(float daylightAmount)
    {
        if (!syncFogToSkybox)
        {
            return;
        }

        Color targetDayFog = dayFogColor;
        Color targetNightFog = nightFogColor;

        if (useSkyboxHorizonColorsForFog && skyboxMaterial != null)
        {
            if (skyboxMaterial.HasProperty("_DayHorizonColor"))
            {
                targetDayFog = skyboxMaterial.GetColor("_DayHorizonColor");
            }

            if (skyboxMaterial.HasProperty("_NightHorizonColor"))
            {
                targetNightFog = skyboxMaterial.GetColor("_NightHorizonColor");
            }
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;

        RenderSettings.fogColor = Color.Lerp(
            targetNightFog,
            targetDayFog,
            daylightAmount
        );

        RenderSettings.fogStartDistance = Mathf.Lerp(
            nightFogStart,
            dayFogStart,
            daylightAmount
        );

        RenderSettings.fogEndDistance = Mathf.Lerp(
            nightFogEnd,
            dayFogEnd,
            daylightAmount
        );
    }
}