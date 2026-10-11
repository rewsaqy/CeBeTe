namespace Yumiwa.Domain
{
    /// <summary>
    /// Player settings per GDD §03/07 — all feel options must be togglable (anti motion-sickness).
    /// Head bob / camera shake / FOV boost are presentation toggles, never stamina rules.
    /// Dialogue fields serve plan.json player_settings (text size, background opacity, reading speed);
    /// persisted via ISettingsRepository, independent from CheckpointData (transient queue).
    /// NOTE (struct trap): `new SettingsData()` on a struct zero-initializes and NEVER
    /// calls the optional-parameter constructor, so Defaults() must pass explicit values.
    /// The six legacy fields keep their historical live (zero) values to preserve behavior;
    /// only the three new dialogue fields get intended defaults here.
    /// </summary>
    public readonly struct SettingsData
    {
        public readonly bool HeadBobEnabled;
        public readonly bool CameraShakeEnabled;
        public readonly bool FovBoostEnabled;
        public readonly bool SubtitlesEnabled;
        public readonly float MouseSensitivity;
        public readonly float Fov;
        public readonly DialogueTextSize TextSize;
        public readonly float SubtitleBackgroundOpacity;
        public readonly DialogueReadingSpeed ReadingSpeed;

        public SettingsData(
            bool headBobEnabled = true,
            bool cameraShakeEnabled = true,
            bool fovBoostEnabled = true,
            bool subtitlesEnabled = true,
            float mouseSensitivity = 1f,
            float fov = 75f,
            DialogueTextSize textSize = DialogueTextSize.Medium,
            float subtitleBackgroundOpacity = 0.6f,
            DialogueReadingSpeed readingSpeed = DialogueReadingSpeed.Normal)
        {
            HeadBobEnabled = headBobEnabled;
            CameraShakeEnabled = cameraShakeEnabled;
            FovBoostEnabled = fovBoostEnabled;
            SubtitlesEnabled = subtitlesEnabled;
            MouseSensitivity = mouseSensitivity;
            Fov = fov;
            TextSize = textSize;
            SubtitleBackgroundOpacity = subtitleBackgroundOpacity < 0f ? 0f
                : subtitleBackgroundOpacity > 1f ? 1f : subtitleBackgroundOpacity;
            ReadingSpeed = readingSpeed;
        }

        public static SettingsData Defaults() => new SettingsData(
            headBobEnabled: false,
            cameraShakeEnabled: false,
            fovBoostEnabled: false,
            subtitlesEnabled: true,
            mouseSensitivity: 0f,
            fov: 0f,
            textSize: DialogueTextSize.Medium,
            subtitleBackgroundOpacity: 0.6f,
            readingSpeed: DialogueReadingSpeed.Normal);
    }
}
