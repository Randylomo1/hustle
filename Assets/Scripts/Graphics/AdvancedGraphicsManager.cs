using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;
using System.Collections.Generic;

namespace NairobiHustle.Graphics
{
    public class AdvancedGraphicsManager : MonoBehaviour
    {
        [Header("Graphics Quality")]
        [SerializeField] private bool enableRayTracing = true;
        [SerializeField] private bool enableDLSS = true;
        [SerializeField] private bool enableFSR = true;
        [SerializeField] private bool enableHDRI = true;
        [SerializeField] private bool enableSSR = true;

        [Header("Lighting Settings")]
        [SerializeField] private bool enableGlobalIllumination = true;
        [SerializeField] private bool enableVolumetricLighting = true;
        [SerializeField] private bool enableScreenSpaceShadows = true;
        [SerializeField] private bool enableRayTracedShadows = true;
        [SerializeField] private int maxShadowCascades = 4;

        [Header("Post Processing")]
        [SerializeField] private bool enableBloom = true;
        [SerializeField] private bool enableDOF = true;
        [SerializeField] private bool enableMotionBlur = true;
        [SerializeField] private bool enableChromaticAberration = true;
        [SerializeField] private bool enableFilmGrain = true;

        [Header("Environmental Effects")]
        [SerializeField] private bool enableDynamicWeather = true;
        [SerializeField] private bool enableDynamicTimeOfDay = true;
        [SerializeField] private bool enableDynamicCrowds = true;
        [SerializeField] private bool enableDynamicTraffic = true;

        private HDRenderPipelineAsset hdrpAsset;
        private Volume postProcessVolume;
        private WeatherSystem weatherSystem;
        private TimeManager timeManager;
        private CrowdSystem crowdSystem;
        private TrafficSystem trafficSystem;

        private void Awake()
        {
            InitializeGraphics();
        }

        private void InitializeGraphics()
        {
            try
            {
                // Initialize HDRP settings
                SetupHDRP();

                // Initialize post-processing
                SetupPostProcessing();

                // Initialize environmental systems
                SetupEnvironmentalSystems();

                // Apply quality settings
                ApplyQualitySettings();

                Debug.Log("Graphics system initialized successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Graphics initialization failed: {e.Message}");
                throw;
            }
        }

        private void SetupHDRP()
        {
            hdrpAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            if (hdrpAsset == null)
            {
                throw new Exception("HDRP asset not found");
            }

            var settings = hdrpAsset.currentPlatformRenderPipelineSettings;

            // Configure ray tracing
            if (enableRayTracing && SystemInfo.supportsRayTracing)
            {
                settings.supportRayTracing = true;
                settings.supportedRayTracingMode = RayTracingMode.Both;
                settings.supportComputeRayTracing = true;
            }

            // Configure lighting
            settings.supportSSR = enableSSR;
            settings.supportSSAO = true;
            settings.supportSubsurfaceScattering = true;
            settings.supportVolumetrics = enableVolumetricLighting;

            // Configure shadows
            settings.hdShadowInitParams.maxShadowRequests = 512;
            settings.hdShadowInitParams.shadowFilteringQuality = HDShadowFilteringQuality.High;
            settings.hdShadowInitParams.supportScreenSpaceShadows = enableScreenSpaceShadows;

            // Apply settings
            GraphicsSettings.renderPipelineAsset = hdrpAsset;
        }

        private void SetupPostProcessing()
        {
            postProcessVolume = gameObject.AddComponent<Volume>();
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            postProcessVolume.profile = profile;

            // Add post-processing effects
            if (enableBloom)
                AddBloom(profile);

            if (enableDOF)
                AddDepthOfField(profile);

            if (enableMotionBlur)
                AddMotionBlur(profile);

            if (enableChromaticAberration)
                AddChromaticAberration(profile);

            if (enableFilmGrain)
                AddFilmGrain(profile);

            // Add color grading
            AddColorGrading(profile);
        }

        private void AddBloom(VolumeProfile profile)
        {
            var bloom = profile.Add<Bloom>();
            bloom.intensity.value = 1f;
            bloom.scatter.value = 0.7f;
            bloom.threshold.value = 1f;
            bloom.tint.value = Color.white;
        }

        private void AddDepthOfField(VolumeProfile profile)
        {
            var dof = profile.Add<DepthOfField>();
            dof.focusMode.value = DepthOfFieldMode.UsePhysicalCamera;
            dof.focusDistance.value = 10f;
            dof.focalLength.value = 50f;
            dof.aperture.value = 5.6f;
        }

        private void AddMotionBlur(VolumeProfile profile)
        {
            var blur = profile.Add<MotionBlur>();
            blur.intensity.value = 0.5f;
            blur.maximumVelocity.value = 32f;
            blur.minimumVelocity.value = 2f;
        }

        private void AddChromaticAberration(VolumeProfile profile)
        {
            var chromaticAberration = profile.Add<ChromaticAberration>();
            chromaticAberration.intensity.value = 0.1f;
        }

        private void AddFilmGrain(VolumeProfile profile)
        {
            var grain = profile.Add<FilmGrain>();
            grain.type.value = FilmGrainLookup.Thin1;
            grain.intensity.value = 0.3f;
            grain.response.value = 0.8f;
        }

        private void AddColorGrading(VolumeProfile profile)
        {
            var colorGrading = profile.Add<ColorAdjustments>();
            colorGrading.postExposure.value = 0f;
            colorGrading.contrast.value = 10f;
            colorGrading.saturation.value = 10f;
        }

        private void SetupEnvironmentalSystems()
        {
            if (enableDynamicWeather)
                InitializeWeatherSystem();

            if (enableDynamicTimeOfDay)
                InitializeTimeSystem();

            if (enableDynamicCrowds)
                InitializeCrowdSystem();

            if (enableDynamicTraffic)
                InitializeTrafficSystem();
        }

        private void InitializeWeatherSystem()
        {
            weatherSystem = gameObject.AddComponent<WeatherSystem>();
            weatherSystem.Initialize(new WeatherConfig
            {
                EnableRain = true,
                EnableSnow = false,
                EnableFog = true,
                EnableWind = true,
                EnableLightning = true,
                EnablePuddles = true,
                EnableWetSurfaces = true
            });
        }

        private void InitializeTimeSystem()
        {
            timeManager = gameObject.AddComponent<TimeManager>();
            timeManager.Initialize(new TimeConfig
            {
                EnableDayNightCycle = true,
                EnableSunMovement = true,
                EnableMoonPhases = true,
                EnableStars = true,
                DayLength = 24f, // 24 minutes = 1 game day
                StartTime = 8f // Start at 8 AM
            });
        }

        private void InitializeCrowdSystem()
        {
            crowdSystem = gameObject.AddComponent<CrowdSystem>();
            crowdSystem.Initialize(new CrowdConfig
            {
                MaxCrowdDensity = 100,
                EnableBehaviorVariation = true,
                EnableAppearanceVariation = true,
                EnableDynamicGrouping = true,
                EnableReactions = true
            });
        }

        private void InitializeTrafficSystem()
        {
            trafficSystem = gameObject.AddComponent<TrafficSystem>();
            trafficSystem.Initialize(new TrafficConfig
            {
                MaxVehicles = 100,
                EnableAIDrivers = true,
                EnableTrafficRules = true,
                EnableAccidents = true,
                EnableDynamicRouting = true,
                EnableVehicleVariation = true
            });
        }

        private void ApplyQualitySettings()
        {
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowDistance = 100f;
            QualitySettings.shadowCascades = maxShadowCascades;
            QualitySettings.shadowmaskMode = ShadowmaskMode.DistanceShadowmask;
            QualitySettings.skinWeights = SkinWeights.Unlimited;
            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 8;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.realtimeReflectionProbes = true;
            QualitySettings.billboardsFaceCameraPosition = true;
        }

        public void UpdateWeatherConditions(WeatherCondition condition)
        {
            if (!enableDynamicWeather || weatherSystem == null) return;
            weatherSystem.SetWeatherCondition(condition);
        }

        public void UpdateTimeOfDay(float time)
        {
            if (!enableDynamicTimeOfDay || timeManager == null) return;
            timeManager.SetTime(time);
        }

        public void UpdateCrowdDensity(float density)
        {
            if (!enableDynamicCrowds || crowdSystem == null) return;
            crowdSystem.SetDensity(density);
        }

        public void UpdateTrafficDensity(float density)
        {
            if (!enableDynamicTraffic || trafficSystem == null) return;
            trafficSystem.SetDensity(density);
        }

        public class WeatherConfig
        {
            public bool EnableRain { get; set; }
            public bool EnableSnow { get; set; }
            public bool EnableFog { get; set; }
            public bool EnableWind { get; set; }
            public bool EnableLightning { get; set; }
            public bool EnablePuddles { get; set; }
            public bool EnableWetSurfaces { get; set; }
        }

        public class TimeConfig
        {
            public bool EnableDayNightCycle { get; set; }
            public bool EnableSunMovement { get; set; }
            public bool EnableMoonPhases { get; set; }
            public bool EnableStars { get; set; }
            public float DayLength { get; set; }
            public float StartTime { get; set; }
        }

        public class CrowdConfig
        {
            public int MaxCrowdDensity { get; set; }
            public bool EnableBehaviorVariation { get; set; }
            public bool EnableAppearanceVariation { get; set; }
            public bool EnableDynamicGrouping { get; set; }
            public bool EnableReactions { get; set; }
        }

        public class TrafficConfig
        {
            public int MaxVehicles { get; set; }
            public bool EnableAIDrivers { get; set; }
            public bool EnableTrafficRules { get; set; }
            public bool EnableAccidents { get; set; }
            public bool EnableDynamicRouting { get; set; }
            public bool EnableVehicleVariation { get; set; }
        }

        public enum WeatherCondition
        {
            Clear,
            Cloudy,
            LightRain,
            HeavyRain,
            Storm,
            Foggy
        }
    }
} 