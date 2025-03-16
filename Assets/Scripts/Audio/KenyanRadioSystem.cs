using UnityEngine;
using System.Collections.Generic;
using System;

namespace NairobiHustle.Audio
{
    [System.Serializable]
    public class RadioStation
    {
        public string name;
        public float frequency;
        public AudioClip[] musicLibrary;
        public AudioClip[] jingles;
        public AudioClip[] newsClips;
        public AudioClip[] advertisements;
        public AudioClip[] presenters;
        public string[] presentersNames;
        public bool is24Hours;
        public Language[] supportedLanguages;
        public Schedule[] schedule;
    }

    [System.Serializable]
    public class Schedule
    {
        public DayOfWeek day;
        public ProgramSlot[] slots;
    }

    [System.Serializable]
    public class ProgramSlot
    {
        public string programName;
        public float startHour; // 24-hour format
        public float duration; // in hours
        public ProgramType type;
        public Language language;
        public AudioClip[] specificContent;
    }

    public enum ProgramType
    {
        Music,
        News,
        TalkShow,
        Religious,
        Sports,
        Traffic,
        Weather
    }

    public enum Language
    {
        English,
        Swahili,
        Sheng,
        Mixed
    }

    public class KenyanRadioSystem : MonoBehaviour
    {
        [Header("Radio Stations")]
        public RadioStation[] stations = new RadioStation[]
        {
            new RadioStation 
            { 
                name = "Classic 105",
                frequency = 105.2f,
                is24Hours = true,
                supportedLanguages = new Language[] { Language.English, Language.Swahili }
            },
            new RadioStation 
            { 
                name = "Kiss FM",
                frequency = 100.3f,
                is24Hours = true,
                supportedLanguages = new Language[] { Language.English, Language.Sheng }
            },
            new RadioStation 
            { 
                name = "Jambo FM",
                frequency = 97.5f,
                is24Hours = true,
                supportedLanguages = new Language[] { Language.Swahili, Language.Sheng }
            },
            new RadioStation 
            { 
                name = "Nation FM",
                frequency = 96.3f,
                is24Hours = true,
                supportedLanguages = new Language[] { Language.English, Language.Swahili }
            }
        };

        [Header("Audio Settings")]
        public float maxSignalStrength = 1f;
        public float minSignalStrength = 0.2f;
        public float staticVolume = 0.1f;
        public float tuningSpeed = 0.5f;
        public AnimationCurve signalFalloff;

        [Header("Content")]
        public AudioClip[] trafficUpdates;
        public AudioClip[] weatherReports;
        public AudioClip[] newsHeadlines;
        public AudioClip[] staticNoise;
        public AudioClip tuningSound;

        private AudioSource mainSource;
        private AudioSource staticSource;
        private AudioSource effectsSource;
        private float currentFrequency;
        private RadioStation currentStation;
        private float signalQuality;
        private float timeOfDay;
        private DayOfWeek currentDay;
        private ProgramSlot currentProgram;

        private void Awake()
        {
            SetupAudioSources();
            currentDay = DateTime.Now.DayOfWeek;
            timeOfDay = (float)DateTime.Now.Hour + (float)DateTime.Now.Minute / 60f;
        }

        private void SetupAudioSources()
        {
            // Main radio output
            mainSource = gameObject.AddComponent<AudioSource>();
            mainSource.spatialBlend = 0f; // 2D sound
            mainSource.priority = 0;

            // Static noise
            staticSource = gameObject.AddComponent<AudioSource>();
            staticSource.clip = staticNoise[0];
            staticSource.loop = true;
            staticSource.volume = 0f;
            staticSource.Play();

            // Effects (tuning, etc.)
            effectsSource = gameObject.AddComponent<AudioSource>();
            effectsSource.spatialBlend = 0f;
        }

        private void Update()
        {
            UpdateTime();
            UpdateSignalQuality();
            UpdateAudio();
            CheckProgramSchedule();
        }

        private void UpdateTime()
        {
            timeOfDay += Time.deltaTime / 3600f; // Real-time progression
            if (timeOfDay >= 24f)
            {
                timeOfDay = 0f;
                currentDay = (DayOfWeek)(((int)currentDay + 1) % 7);
            }
        }

        public void TuneToFrequency(float frequency)
        {
            currentFrequency = frequency;
            effectsSource.PlayOneShot(tuningSound);

            // Find closest station
            RadioStation closestStation = null;
            float minDiff = float.MaxValue;

            foreach (var station in stations)
            {
                float diff = Mathf.Abs(station.frequency - frequency);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closestStation = station;
                }
            }

            if (minDiff < 0.3f) // Within range
            {
                SwitchToStation(closestStation);
            }
            else
            {
                currentStation = null;
                signalQuality = 0f;
            }
        }

        private void SwitchToStation(RadioStation station)
        {
            if (station == currentStation) return;

            currentStation = station;
            signalQuality = 1f;
            PlayCurrentProgram();
        }

        private void UpdateSignalQuality()
        {
            if (currentStation == null) return;

            // Simulate signal interference based on various factors
            float baseSignal = signalFalloff.Evaluate(
                Mathf.Abs(currentFrequency - currentStation.frequency)
            );

            // Add random interference
            float interference = Mathf.PerlinNoise(Time.time * 0.1f, 0f) * 0.2f;
            signalQuality = Mathf.Clamp01(baseSignal - interference);

            // Update audio mix
            mainSource.volume = signalQuality * maxSignalStrength;
            staticSource.volume = (1f - signalQuality) * staticVolume;
        }

        private void UpdateAudio()
        {
            if (currentStation == null || currentProgram == null) return;

            // Check if current audio has finished
            if (!mainSource.isPlaying)
            {
                PlayNextContent();
            }
        }

        private void CheckProgramSchedule()
        {
            if (currentStation == null) return;

            Schedule todaySchedule = Array.Find(
                currentStation.schedule,
                s => s.day == currentDay
            );

            if (todaySchedule == null) return;

            // Find current program slot
            foreach (var slot in todaySchedule.slots)
            {
                float slotEnd = slot.startHour + slot.duration;
                if (timeOfDay >= slot.startHour && timeOfDay < slotEnd)
                {
                    if (currentProgram != slot)
                    {
                        currentProgram = slot;
                        PlayCurrentProgram();
                    }
                    break;
                }
            }
        }

        private void PlayCurrentProgram()
        {
            if (currentProgram == null || currentProgram.specificContent == null) return;

            mainSource.Stop();
            AudioClip content = currentProgram.specificContent[
                UnityEngine.Random.Range(0, currentProgram.specificContent.Length)
            ];
            mainSource.clip = content;
            mainSource.Play();
        }

        private void PlayNextContent()
        {
            switch (currentProgram.type)
            {
                case ProgramType.Music:
                    PlayRandomMusic();
                    break;
                case ProgramType.News:
                    PlayNews();
                    break;
                case ProgramType.Traffic:
                    PlayTrafficUpdate();
                    break;
                case ProgramType.Weather:
                    PlayWeatherReport();
                    break;
                default:
                    PlayRandomMusic();
                    break;
            }
        }

        private void PlayRandomMusic()
        {
            if (currentStation.musicLibrary == null || currentStation.musicLibrary.Length == 0) return;

            AudioClip music = currentStation.musicLibrary[
                UnityEngine.Random.Range(0, currentStation.musicLibrary.Length)
            ];
            mainSource.clip = music;
            mainSource.Play();
        }

        private void PlayNews()
        {
            if (newsHeadlines == null || newsHeadlines.Length == 0) return;

            AudioClip news = newsHeadlines[UnityEngine.Random.Range(0, newsHeadlines.Length)];
            mainSource.clip = news;
            mainSource.Play();
        }

        private void PlayTrafficUpdate()
        {
            if (trafficUpdates == null || trafficUpdates.Length == 0) return;

            AudioClip traffic = trafficUpdates[UnityEngine.Random.Range(0, trafficUpdates.Length)];
            mainSource.clip = traffic;
            mainSource.Play();
        }

        private void PlayWeatherReport()
        {
            if (weatherReports == null || weatherReports.Length == 0) return;

            AudioClip weather = weatherReports[UnityEngine.Random.Range(0, weatherReports.Length)];
            mainSource.clip = weather;
            mainSource.Play();
        }
    }
} 