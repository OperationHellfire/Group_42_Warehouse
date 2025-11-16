using System.Globalization;

namespace G42Warehouse.Domain
{
    public enum ExperienceLevel
    {
        Junior = 1,
        Mid = 2,
        Senior = 3
    }

    public abstract class Employee
    {
        private static int _nextId = 1;

        private static readonly List<Employee> _extent = new();
        public static IReadOnlyCollection<Employee> Extent => _extent.AsReadOnly();

        public static decimal YearlySalaryGrowth { get; } = 0.20m;

        public int Id { get; }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Name cannot be empty.", nameof(value));
                _name = value.Trim();
            }
        }

        private DateTime _employmentDate;
        public DateTime EmploymentDate
        {
            get => _employmentDate;
            set
            {
                if (value.Date > DateTime.Today)
                    throw new ArgumentException("Employment date cannot be in the future.", nameof(value));
                _employmentDate = value.Date;
            }
        }

        private decimal _baseSalary;
        public decimal BaseSalary
        {
            get => _baseSalary;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Base salary must be positive.");
                _baseSalary = value;
            }
        }

        public ExperienceLevel ExperienceLevel { get; set; }

        public int YearsSinceEmployment =>
            Math.Max(0, (int)((DateTime.Today - EmploymentDate).TotalDays / 365.25));

        public decimal Salary =>
            BaseSalary * (1 + YearlySalaryGrowth * YearsSinceEmployment);

        private readonly ISet<Section> _assignedSections = new HashSet<Section>();
        public IReadOnlyCollection<Section> AssignedSections => _assignedSections.ToList().AsReadOnly();

        public string? Notes { get; set; }

        protected Employee(string name, DateTime employmentDate, decimal baseSalary, ExperienceLevel experienceLevel)
        {
            Id = _nextId++;

            Name = name;
            EmploymentDate = employmentDate;
            BaseSalary = baseSalary;
            ExperienceLevel = experienceLevel;

            _extent.Add(this);
        }

        public void AssignToSection(Section section)
        {
            if (section == null) throw new ArgumentNullException(nameof(section));

            if (_assignedSections.Add(section))
            {
                section.AddEmployeeInternal(this);
            }
        }

        public void RemoveFromSection(Section section)
        {
            if (section == null) throw new ArgumentNullException(nameof(section));

            if (_assignedSections.Remove(section))
            {
                section.RemoveEmployeeInternal(this);
            }
        }

        private const char Separator = ';';

        public static void SaveExtent(string filePath)
        {
            var lines = _extent.Select(e =>
                string.Join(Separator,
                    e.GetType().Name,
                    e.Id,
                    e.Name.Replace(Separator.ToString(), string.Empty),
                    e.EmploymentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    e.BaseSalary.ToString(CultureInfo.InvariantCulture),
                    (int)e.ExperienceLevel,
                    e.Notes?.Replace(Separator.ToString(), string.Empty) ?? string.Empty));

            File.WriteAllLines(filePath, lines);
        }

        public static void LoadExtent(string filePath)
        {
            _extent.Clear();
            if (!File.Exists(filePath)) return;

            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(Separator);
                if (parts.Length < 7) continue;

                var typeName = parts[0];
                var name = parts[2];
                var employmentDate = DateTime.ParseExact(parts[3], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                var baseSalary = decimal.Parse(parts[4], CultureInfo.InvariantCulture);
                var experienceLevel = (ExperienceLevel)int.Parse(parts[5], CultureInfo.InvariantCulture);
                var notes = string.IsNullOrWhiteSpace(parts[6]) ? null : parts[6];

                Employee employee = typeName switch
                {
                    nameof(WarehouseManager) =>
                        new WarehouseManager(name, employmentDate, baseSalary, experienceLevel),
                    nameof(DeliveryDriver) =>
                        new DeliveryDriver(name, employmentDate, baseSalary, experienceLevel, null),
                    nameof(MachineOperator) =>
                        new MachineOperator(name, employmentDate, baseSalary, experienceLevel),
                    nameof(GeneralEmployee) =>
                        new GeneralEmployee(name, employmentDate, baseSalary, experienceLevel),
                    _ =>
                        new GeneralEmployee(name, employmentDate, baseSalary, experienceLevel)
                };

                employee.Notes = notes;
            }
        }
    }

    public sealed class GeneralEmployee : Employee
    {
        public GeneralEmployee(string name, DateTime employmentDate, decimal baseSalary, ExperienceLevel experienceLevel)
            : base(name, employmentDate, baseSalary, experienceLevel)
        {
        }
    }

    public abstract class Worker : Employee
    {
        protected Worker(string name, DateTime employmentDate, decimal baseSalary, ExperienceLevel experienceLevel)
            : base(name, employmentDate, baseSalary, experienceLevel)
        {
        }
    }

    public sealed class WarehouseManager : Employee
    {
        private readonly ISet<Worker> _managedWorkers = new HashSet<Worker>();
        public IReadOnlyCollection<Worker> ManagedWorkers => _managedWorkers.ToList().AsReadOnly();

        public WarehouseManager(string name, DateTime employmentDate, decimal baseSalary, ExperienceLevel experienceLevel)
            : base(name, employmentDate, baseSalary, experienceLevel)
        {
        }

        public void AddWorker(Worker worker)
        {
            if (worker == null) throw new ArgumentNullException(nameof(worker));
            _managedWorkers.Add(worker);
        }

        public void RemoveWorker(Worker worker)
        {
            if (worker == null) throw new ArgumentNullException(nameof(worker));
            _managedWorkers.Remove(worker);
        }
    }

    public sealed class DeliveryDriver : Worker
    {
        private string? _driverLicenseCategory;
        public string? DriverLicenseCategory
        {
            get => _driverLicenseCategory;
            set
            {
                if (value != null && string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("License category cannot be empty when provided.", nameof(value));

                if (value != null)
                {
                    var trimmed = value.Trim().ToUpperInvariant();
                    if (trimmed != "B" && trimmed != "C" && trimmed != "C1")
                        throw new ArgumentException("Driver license must be one of: B, C, C1.", nameof(value));

                    _driverLicenseCategory = trimmed;
                }
                else
                {
                    _driverLicenseCategory = null;
                }
            }
        }

        public DeliveryDriver(
            string name,
            DateTime employmentDate,
            decimal baseSalary,
            ExperienceLevel experienceLevel,
            string? driverLicenseCategory)
            : base(name, employmentDate, baseSalary, experienceLevel)
        {
            DriverLicenseCategory = driverLicenseCategory;
        }
    }

    public sealed class MachineOperator : Worker
    {
        public MachineOperator(string name, DateTime employmentDate, decimal baseSalary, ExperienceLevel experienceLevel)
            : base(name, employmentDate, baseSalary, experienceLevel)
        {
        }
    }
}

