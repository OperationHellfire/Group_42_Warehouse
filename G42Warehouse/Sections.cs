using System.Globalization;

namespace G42Warehouse.Domain
{
    public enum SectionStatus
    {
        Active,
        Maintenance,
        Closed
    }

    public enum HazardType
    {
        Toxic,
        Flammable,
        Corrosive,
        Irritant,
        Sensitizer,
        Asphyxiant
    }
    public sealed class SectionLocation
    {
        public string Building { get; }
        public string Aisle { get; }
        public int Row { get; }

        public SectionLocation(string building, string aisle, int row)
        {
            if (string.IsNullOrWhiteSpace(building))
                throw new ArgumentException("Building cannot be empty.", nameof(building));
            if (string.IsNullOrWhiteSpace(aisle))
                throw new ArgumentException("Aisle cannot be empty.", nameof(aisle));
            if (row <= 0)
                throw new ArgumentOutOfRangeException(nameof(row), "Row must be positive.");

            Building = building.Trim();
            Aisle = aisle.Trim();
            Row = row;
        }

        public override string ToString() => $"{Building}-{Aisle}-{Row}";
    }

    public abstract class Section
    {
        private static readonly List<Section> _extent = new();
        public static IReadOnlyCollection<Section> Extent => _extent.AsReadOnly();

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Section name cannot be empty.", nameof(value));
                _name = value.Trim();
            }
        }

        public SectionLocation Location { get; }

        private double _width;
        public double Width
        {
            get => _width;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Width must be positive.");
                _width = value;
            }
        }

        private double _length;
        public double Length
        {
            get => _length;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Length must be positive.");
                _length = value;
            }
        }

        public double Area => Math.Round(Width * Length, 2);

        public SectionStatus Status { get; set; }

        public bool HasBackupGenerator { get; set; }

        private double? _temperature;
        public double? Temperature
        {
            get => _temperature;
            set
            {
                if (value is < -50 or > 80)
                    throw new ArgumentOutOfRangeException(nameof(value), "Temperature must be between -50 and 80 °C.");
                _temperature = value;
            }
        }

        private double _humidity;
        public double Humidity
        {
            get => _humidity;
            set
            {
                if (value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(value), "Humidity must be between 0 and 100%.");
                _humidity = value;
            }
        }

        private readonly ISet<Employee> _employees = new HashSet<Employee>();
        public IReadOnlyCollection<Employee> Employees => _employees.ToList().AsReadOnly();

        protected Section(
            string name,
            SectionLocation location,
            double width,
            double length,
            SectionStatus status,
            bool hasBackupGenerator,
            double? temperature,
            double humidity)
        {
            Location = location ?? throw new ArgumentNullException(nameof(location));
            Name = name;
            Width = width;
            Length = length;
            Status = status;
            HasBackupGenerator = hasBackupGenerator;
            Temperature = temperature;
            Humidity = humidity;

            _extent.Add(this);
        }

        internal void AddEmployeeInternal(Employee employee)
        {
            if (employee == null) throw new ArgumentNullException(nameof(employee));
            _employees.Add(employee);
        }

        internal void RemoveEmployeeInternal(Employee employee)
        {
            if (employee == null) throw new ArgumentNullException(nameof(employee));
            _employees.Remove(employee);
        }

        private const char Separator = ';';

        public static void SaveExtent(string filePath)
        {
            var lines = _extent.Select(s =>
                string.Join(Separator,
                    s.GetType().Name,
                    s.Name.Replace(Separator.ToString(), string.Empty),
                    s.Location.ToString().Replace(Separator.ToString(), string.Empty),
                    s.Width.ToString(CultureInfo.InvariantCulture),
                    s.Length.ToString(CultureInfo.InvariantCulture),
                    (int)s.Status,
                    s.HasBackupGenerator ? "1" : "0",
                    s.Temperature?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    s.Humidity.ToString(CultureInfo.InvariantCulture)));

            File.WriteAllLines(filePath, lines);
        }

        public static void LoadExtent(string filePath)
        {
            _extent.Clear();
            if (!File.Exists(filePath)) return;

            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(Separator);
                if (parts.Length < 9) continue;

                string typeName = parts[0];
                string name = parts[1];

                // --- SAFE parsedRow FIX ---
                var locationParts = parts[2].Split('-');

                string building = locationParts.Length > 0 ? locationParts[0] : "UNKNOWN";
                string aisle = locationParts.Length > 1 ? locationParts[1] : "X";

                int parsedRow = 1;
                if (locationParts.Length > 2)
                    int.TryParse(locationParts[2], out parsedRow);

                var location = new SectionLocation(building, aisle, parsedRow);
                // --- END FIX ---

                double width = double.Parse(parts[3], CultureInfo.InvariantCulture);
                double length = double.Parse(parts[4], CultureInfo.InvariantCulture);
                var status = (SectionStatus)int.Parse(parts[5], CultureInfo.InvariantCulture);

                bool hasBackup = parts[6] == "1";

                double? temperature =
                    string.IsNullOrWhiteSpace(parts[7])
                        ? null
                        : double.Parse(parts[7], CultureInfo.InvariantCulture);

                double humidity = double.Parse(parts[8], CultureInfo.InvariantCulture);

                Section section = typeName switch
                {
                    nameof(AmbientSection) =>
                        new AmbientSection(name, location, width, length, status, hasBackup, temperature, humidity),

                    nameof(RefrigeratedSection) =>
                        new RefrigeratedSection(name, location, width, length, status, hasBackup, temperature, humidity,
                            -10, 10),

                    nameof(HazmatSection) =>
                        new HazmatSection(name, location, width, length, status, hasBackup, temperature, humidity,
                            new[]
                            {
                                HazardType.Toxic
                            }, true),

                    _ =>
                        new AmbientSection(name, location, width, length, status, hasBackup, temperature, humidity)
                };
            }
        }
    }

    public sealed class AmbientSection : Section
    {
        public AmbientSection(
            string name,
            SectionLocation location,
            double width,
            double length,
            SectionStatus status,
            bool hasBackupGenerator,
            double? temperature,
            double humidity)
            : base(name, location, width, length, status, hasBackupGenerator, temperature, humidity)
        {
        }
    }

    public sealed class RefrigeratedSection : Section
    {
        public double MinOperationalTemperature { get; }
        public double MaxOperationalTemperature { get; }

        public bool IsWithinOperationalTemperature =>
            Temperature is double t && t >= MinOperationalTemperature && t <= MaxOperationalTemperature;

        public RefrigeratedSection(
            string name,
            SectionLocation location,
            double width,
            double length,
            SectionStatus status,
            bool hasBackupGenerator,
            double? temperature,
            double humidity,
            double minOperationalTemperature,
            double maxOperationalTemperature)
            : base(name, location, width, length, status, hasBackupGenerator, temperature, humidity)
        {
            if (minOperationalTemperature >= maxOperationalTemperature)
                throw new ArgumentException("Min temperature must be lower than max temperature.");

            MinOperationalTemperature = minOperationalTemperature;
            MaxOperationalTemperature = maxOperationalTemperature;
        }
    }

    public sealed class HazmatSection : Section
    {
        private readonly ISet<HazardType> _hazardTypes = new HashSet<HazardType>();
        public IReadOnlyCollection<HazardType> HazardTypes => _hazardTypes.ToList().AsReadOnly();

        public bool HasVentilationSystem { get; }

        public HazmatSection(
            string name,
            SectionLocation location,
            double width,
            double length,
            SectionStatus status,
            bool hasBackupGenerator,
            double? temperature,
            double humidity,
            IEnumerable<HazardType> hazardTypes,
            bool hasVentilationSystem)
            : base(name, location, width, length, status, hasBackupGenerator, temperature, humidity)
        {
            if (hazardTypes == null)
                throw new ArgumentNullException(nameof(hazardTypes));

            foreach (var h in hazardTypes)
                _hazardTypes.Add(h);

            if (_hazardTypes.Count == 0)
                throw new ArgumentException("Hazmat section must define at least one hazard type.");

            HasVentilationSystem = hasVentilationSystem;
        }
    }
}
