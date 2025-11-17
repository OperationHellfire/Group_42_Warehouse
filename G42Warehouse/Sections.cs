using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace G42Warehouse
{
    public enum SectionStatus
    {
        Active,
        Maintenance,
        Closed
    }

    public enum SectionType
    {
        HazardousMaterials,
        AmbientStorage,
        RefrigeratedStorage,
        Other
    }

    [Serializable]
    public class Section
    {
        private static List<Section> _extent = new List<Section>();
        public static IReadOnlyList<Section> Extent => _extent.AsReadOnly();

        private static void AddToExtent(Section s)
        {
            if (s == null) throw new ArgumentException("Section cannot be null");
            _extent.Add(s);
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Section name cannot be empty.");
                _name = value;
            }
        }

        private string _location;
        public string Location
        {
            get => _location;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Location cannot be empty.");
                _location = value;
            }
        }
        
        private double _area;
        public double Area
        {
            get => _area;
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Area must be positive.");
                _area = value;
            }
        }

        public bool HasBackupGenerator { get; set; }

        private double? _temperature;
        public double? Temperature
        {
            get => _temperature;
            set
            {
                if (value != null && (value < -60 || value > 70))
                    throw new ArgumentException("Temperature out of valid range.");
                _temperature = value;
            }
        }

        private double? _humidity;
        public double? Humidity
        {
            get => _humidity;
            set
            {
                if (value != null && (value < 0 || value > 100))
                    throw new ArgumentException("Humidity must be between 0 and 100.");
                _humidity = value;
            }
        }
        
        private readonly List<string> _forbiddenMaterials = new List<string>();
        public IReadOnlyList<string> ForbiddenMaterials => _forbiddenMaterials.AsReadOnly();

        public void AddForbiddenMaterial(string material)
        {
            if (string.IsNullOrWhiteSpace(material))
                throw new ArgumentException("Material cannot be empty.");

            _forbiddenMaterials.Add(material);
        }

        public static int MaxSections { get; set; } = 200;

        public bool IsColdStorage => Temperature != null && Temperature < 5;

        public SectionStatus Status { get; set; }
        public SectionType Type { get; set; }

        public Section(
            string name,
            string location,
            double area,
            bool hasBackupGenerator,
            SectionType type = SectionType.AmbientStorage,
            SectionStatus status = SectionStatus.Active)
        {
            Name = name;
            Location = location;
            Area = area;
            HasBackupGenerator = hasBackupGenerator;
            Type = type;
            Status = status;

            AddToExtent(this);
        }

        private Section() { }

        public static void Save(string path = "section_extent.xml")
        {
            var serializer = new XmlSerializer(typeof(List<Section>));
            using var file = new StreamWriter(path);
            serializer.Serialize(file, _extent);
        }

        public static bool Load(string path = "section_extent.xml")
        {
            if (!File.Exists(path))
            {
                _extent.Clear();
                return false;
            }

            var serializer = new XmlSerializer(typeof(List<Section>));
            using var file = new StreamReader(path);

            try
            {
                _extent = (List<Section>)serializer.Deserialize(file);
            }
            catch
            {
                _extent.Clear();
                return false;
            }

            return true;
        }
    }
}
