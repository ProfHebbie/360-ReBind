using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ReBind360.Models;

namespace ReBind360.Core
{
    public class ProfileManager
    {
        private readonly string _profilesDir;

        public ProfileManager()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _profilesDir = Path.Combine(appData, "360ReBind", "Profiles");
            Directory.CreateDirectory(_profilesDir);
        }

        public List<string> ListProfiles()
        {
            return Directory.GetFiles(_profilesDir, "*.json")
                            .Select(Path.GetFileNameWithoutExtension)
                            .ToList()!;
        }

        public void Save(MappingProfile profile, string? name = null)
        {
            var saveName = name ?? profile.Name;
            profile.Name = saveName;
            var path = Path.Combine(_profilesDir, $"{saveName}.json");
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(profile, options);
            File.WriteAllText(path, json);
        }

        public MappingProfile Load(string name)
        {
            var path = Path.Combine(_profilesDir, $"{name}.json");
            if (!File.Exists(path))
            {
                return new MappingProfile { Name = name };
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<MappingProfile>(json) ?? new MappingProfile { Name = name };
        }

        public bool Delete(string name)
        {
            var path = Path.Combine(_profilesDir, $"{name}.json");
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
            return false;
        }

        public bool Exists(string name) => File.Exists(Path.Combine(_profilesDir, $"{name}.json"));
    }
}
