using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using SmartViewFilter.Revit.Models;

namespace SmartViewFilter.Revit.Services
{
    public class ConfigurationStore
    {
        private readonly string _filePath;

        public ConfigurationStore()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SmartViewFilter");
            _filePath = Path.Combine(folder, "saved-configurations.json");
        }

        public string FilePath => _filePath;

        public List<SavedConfiguration> Load()
        {
            if (!File.Exists(_filePath))
            {
                return new List<SavedConfiguration>();
            }

            using (FileStream stream = File.OpenRead(_filePath))
            {
                if (stream.Length == 0)
                {
                    return new List<SavedConfiguration>();
                }

                var serializer = new DataContractJsonSerializer(typeof(List<SavedConfiguration>));
                return serializer.ReadObject(stream) as List<SavedConfiguration> ?? new List<SavedConfiguration>();
            }
        }

        public void Save(List<SavedConfiguration> configurations)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
            using (FileStream stream = File.Create(_filePath))
            {
                var serializer = new DataContractJsonSerializer(typeof(List<SavedConfiguration>));
                serializer.WriteObject(stream, configurations ?? new List<SavedConfiguration>());
            }
        }
    }
}
