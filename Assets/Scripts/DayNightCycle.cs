using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controls day/night cycle with automatic building window lighting
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    [SerializeField] private float timeOfDay = 12f; // 0-24 hours
    [SerializeField] private float dayDurationSeconds = 120f; // How long a full day takes in real seconds
    [SerializeField] private bool autoProgress = true; // Automatically progress time

    [Header("Sun/Moon")]
    [SerializeField] private Light directionalLight; // Main sun/moon light
    [SerializeField] private bool autoFindLight = true;

    [Header("Building Lights")]
    [SerializeField] private float lightsOnTime = 18f; // 6 PM
    [SerializeField] private float lightsOffTime = 6f; // 6 AM
    [SerializeField] private float emissionIntensity = 1.5f; // How bright the windows glow
    [SerializeField] private Color emissionColor = Color.white;

    [Header("Sky Colors")]
    [SerializeField] private Gradient skyColorGradient;
    [SerializeField] private Gradient equatorColorGradient;

    private List<Material> buildingMaterials = new List<Material>();
    private bool lightsCurrentlyOn = false;

    void Start()
    {
        // Auto-find directional light
        if (autoFindLight && directionalLight == null)
        {
            directionalLight = FindObjectOfType<Light>();
        }

        // Find all building materials with emission
        FindBuildingMaterials();

        // Set initial state
        UpdateDayNight();
    }

    void Update()
    {
        if (autoProgress)
        {
            // Progress time
            timeOfDay += (24f / dayDurationSeconds) * Time.deltaTime;
            if (timeOfDay >= 24f)
            {
                timeOfDay -= 24f;
            }

            UpdateDayNight();
        }
    }

    private void UpdateDayNight()
    {
        // Calculate time as 0-1 value
        float timePercent = timeOfDay / 24f;

        // Update sun rotation
        if (directionalLight != null)
        {
            float sunAngle = timePercent * 360f - 90f; // Start at -90 (sunrise in east)
            directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 0, 0);

            // Adjust light intensity based on time
            float lightIntensity = CalculateSunIntensity(timeOfDay);
            directionalLight.intensity = lightIntensity;

            // Adjust light color (orange at sunset/sunrise, white at noon, blue at night)
            directionalLight.color = GetSunColor(timeOfDay);
        }

        // Update ambient lighting
        UpdateAmbientLight(timeOfDay);

        // Control building lights
        bool shouldLightsBeOn = ShouldLightsBeOn(timeOfDay);
        if (shouldLightsBeOn != lightsCurrentlyOn)
        {
            SetBuildingLights(shouldLightsBeOn);
            lightsCurrentlyOn = shouldLightsBeOn;
        }
    }

    private float CalculateSunIntensity(float time)
    {
        // Sunrise: 6 AM, Sunset: 6 PM
        // Full brightness: 8 AM - 4 PM
        // Night: 8 PM - 4 AM

        if (time >= 8f && time <= 16f) // Day
        {
            return 1.0f;
        }
        else if (time >= 6f && time < 8f) // Sunrise
        {
            float t = (time - 6f) / 2f; // 0 to 1
            return Mathf.Lerp(0.1f, 1.0f, t);
        }
        else if (time > 16f && time <= 18f) // Sunset
        {
            float t = (time - 16f) / 2f; // 0 to 1
            return Mathf.Lerp(1.0f, 0.1f, t);
        }
        else // Night
        {
            return 0.1f;
        }
    }

    private Color GetSunColor(float time)
    {
        if (time >= 8f && time <= 16f) // Daytime
        {
            return new Color(1f, 0.98f, 0.9f); // Slightly warm white
        }
        else if (time >= 6f && time < 8f) // Sunrise
        {
            float t = (time - 6f) / 2f;
            Color sunrise = new Color(1f, 0.6f, 0.3f); // Orange
            Color day = new Color(1f, 0.98f, 0.9f);
            return Color.Lerp(sunrise, day, t);
        }
        else if (time > 16f && time <= 18f) // Sunset
        {
            float t = (time - 16f) / 2f;
            Color day = new Color(1f, 0.98f, 0.9f);
            Color sunset = new Color(1f, 0.5f, 0.2f); // Orange-red
            return Color.Lerp(day, sunset, t);
        }
        else // Night
        {
            return new Color(0.3f, 0.4f, 0.6f); // Bluish moonlight
        }
    }

    private void UpdateAmbientLight(float time)
    {
        // Adjust ambient lighting based on time
        float ambientIntensity;

        if (time >= 6f && time <= 18f) // Day
        {
            ambientIntensity = 1.0f;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.6f, 0.7f);
        }
        else if (time > 18f && time < 20f) // Evening transition
        {
            float t = (time - 18f) / 2f;
            ambientIntensity = Mathf.Lerp(1.0f, 0.3f, t);
            RenderSettings.ambientSkyColor = Color.Lerp(
                new Color(0.5f, 0.6f, 0.7f),
                new Color(0.1f, 0.15f, 0.25f),
                t
            );
        }
        else if (time >= 4f && time < 6f) // Morning transition
        {
            float t = (time - 4f) / 2f;
            ambientIntensity = Mathf.Lerp(0.3f, 1.0f, t);
            RenderSettings.ambientSkyColor = Color.Lerp(
                new Color(0.1f, 0.15f, 0.25f),
                new Color(0.5f, 0.6f, 0.7f),
                t
            );
        }
        else // Night
        {
            ambientIntensity = 0.3f;
            RenderSettings.ambientSkyColor = new Color(0.1f, 0.15f, 0.25f);
        }

        RenderSettings.ambientIntensity = ambientIntensity;
    }

    private bool ShouldLightsBeOn(float time)
    {
        // Lights on from 6 PM to 6 AM
        if (lightsOnTime < lightsOffTime) // Normal case (e.g., 6 PM to 6 AM next day)
        {
            return time >= lightsOnTime || time < lightsOffTime;
        }
        else // Unusual case
        {
            return time >= lightsOnTime && time < lightsOffTime;
        }
    }

    private void FindBuildingMaterials()
    {
        buildingMaterials.Clear();

        // Find all renderers in the scene
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();

        foreach (Renderer renderer in allRenderers)
        {
            foreach (Material mat in renderer.materials)
            {
                // Check if material has emission enabled
                if (mat.IsKeywordEnabled("_EMISSION") || mat.HasProperty("_EmissionMap"))
                {
                    if (!buildingMaterials.Contains(mat))
                    {
                        buildingMaterials.Add(mat);
                    }
                }
            }
        }

        Debug.Log($"Found {buildingMaterials.Count} materials with emission maps");
    }

    public void SetBuildingLights(bool on)
    {
        foreach (Material mat in buildingMaterials)
        {
            if (on)
            {
                // Turn on emission
                mat.EnableKeyword("_EMISSION");

                // Set emission color and intensity
                Color finalEmission = emissionColor * emissionIntensity;
                mat.SetColor("_EmissionColor", finalEmission);
            }
            else
            {
                // Turn off emission (set to black)
                mat.SetColor("_EmissionColor", Color.black);
            }
        }

        Debug.Log($"Building lights: {(on ? "ON" : "OFF")} - {buildingMaterials.Count} materials updated");
    }

    public void SetTime(float hour)
    {
        timeOfDay = Mathf.Clamp(hour, 0f, 24f);
        UpdateDayNight();
    }

    public void SetTimeToNight()
    {
        SetTime(20f); // 8 PM
    }

    public void SetTimeToDay()
    {
        SetTime(12f); // Noon
    }

    public void SetTimeToDusk()
    {
        SetTime(18f); // 6 PM
    }

    public void SetTimeToDawn()
    {
        SetTime(6f); // 6 AM
    }

    // Refresh materials (call after building a city)
    public void RefreshBuildingMaterials()
    {
        FindBuildingMaterials();
        UpdateDayNight();
    }

    // Getters
    public float GetTimeOfDay() => timeOfDay;
    public string GetTimeString()
    {
        int hours = Mathf.FloorToInt(timeOfDay);
        int minutes = Mathf.FloorToInt((timeOfDay - hours) * 60f);
        return $"{hours:D2}:{minutes:D2}";
    }
}
