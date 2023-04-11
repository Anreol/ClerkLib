using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClerkLib.FileReader
{
    public class JsonFileReader<T> : IJsonFileReader<T> where T : class
    {
        public T Data { get; }

        private readonly string filePath;

        public JsonFileReader(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath), "The file path cannot be null or empty.");
            }

            this.filePath = filePath;

            try
            {
                var json = System.IO.File.ReadAllText(filePath);
                Data = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error deserializing JSON file {filePath}: {ex.Message}");
            }
        }

    }
}
