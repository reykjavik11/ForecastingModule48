using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ForecastingModule.Helper
{
    public sealed class ConfigFileManager
    {
        public static readonly string KEY_HOST = "host";
        public static readonly string KEY_USER = "user";

        private static readonly Lazy<ConfigFileManager> _instance
            = new Lazy<ConfigFileManager>(() => new ConfigFileManager());

        private readonly string _filePath;
        private readonly object _fileLock = new object();

        // Private constructor for Singleton
        private ConfigFileManager()
        {
            // Define the file path
            _filePath = "config.json";

            // Ensure the file exists with default data
            EnsureFileExists();
        }

        // Public property to access the singleton instance
        public static ConfigFileManager Instance => _instance.Value;

        // Ensures the JSON file exists with default data
        private void EnsureFileExists()
        {
            if (!File.Exists(_filePath))
            {
                var defaultData = new Dictionary<string, object>
                {
                    { KEY_HOST, "{CONNECTION_STRING_HERE}" },
                    { KEY_USER, "" }
                };

                WriteAll(defaultData);
            }
        }

        // Reads a value by key
        public object Read(string key)
        {
            lock (_fileLock)
            {
                var data = ReadAll();
                return data.TryGetValue(key, out var value) ? value : null;
            }
        }

        // Writes a value by key
        public void Write(string key, object value)
        {
            lock (_fileLock)
            {
                var data = ReadAll();
                data[key] = value;
                WriteAll(data);
            }
        }

        // Reads all properties from the JSON file
        private Dictionary<string, object> ReadAll()
        {
            lock (_fileLock)
            {
                var json = File.ReadAllText(_filePath);
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(json)
                       ?? new Dictionary<string, object>();
            }
        }

        // Writes all properties to the JSON file
        private void WriteAll(Dictionary<string, object> data)
        {
            lock (_fileLock)
            {
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }
        }
    }
}
