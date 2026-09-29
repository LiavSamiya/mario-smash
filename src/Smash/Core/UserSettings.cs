using System;
using System.IO;
using System.Text.Json;

namespace Smash
{
    //options the player changes on the settings screen, saved between sessions
    class UserSettings
    {
        public const int MaxVolume = 10;

        //0 to MaxVolume
        public int MusicVolume { get; set; } = 7;
        public int SfxVolume { get; set; } = 8;

        static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MarioSmash", "settings.json");

        public static UserSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(FilePath)) ?? new UserSettings();
            }
            catch (Exception e) when (e is IOException || e is JsonException || e is UnauthorizedAccessException)
            {
                //a broken or unreadable file just means default settings
            }
            return new UserSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                //settings are a convenience, the game keeps running without them
            }
        }

        public void ApplyTo(SoundBank audio)
        {
            audio.MusicVolume = MusicVolume / (float)MaxVolume;
            audio.SfxVolume = SfxVolume / (float)MaxVolume;
        }
    }
}
