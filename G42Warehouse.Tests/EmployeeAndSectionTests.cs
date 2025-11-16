using G42Warehouse.Domain;
using Xunit;

namespace G42Warehouse.Tests
{
    public class EmployeeTests
    {
        [Fact]
        public void CreatingEmployee_WithEmptyName_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                new GeneralEmployee(string.Empty, DateTime.Today, 1000m, ExperienceLevel.Junior));
        }

        [Fact]
        public void CreatingEmployee_WithFutureEmploymentDate_Throws()
        {
            var futureDate = DateTime.Today.AddDays(1);

            Assert.Throws<ArgumentException>(() =>
                new GeneralEmployee("Alice", futureDate, 1000m, ExperienceLevel.Junior));
        }

        [Fact]
        public void CreatingEmployee_WithNonPositiveBaseSalary_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new GeneralEmployee("Alice", DateTime.Today, 0m, ExperienceLevel.Junior));
        }

        [Fact]
        public void Salary_IsDerivedFromBaseSalaryAndYears()
        {
            var twoYearsAgo = DateTime.Today.AddYears(-2);
            var emp = new GeneralEmployee("Bob", twoYearsAgo, 1000m, ExperienceLevel.Junior);

            var expectedYears = emp.YearsSinceEmployment;
            var expectedSalary = emp.BaseSalary * (1 + Employee.YearlySalaryGrowth * expectedYears);

            Assert.Equal(expectedSalary, emp.Salary);
        }

        [Fact]
        public void AssignToSection_AddsToBothSides()
        {
            var emp = new GeneralEmployee("Alice", DateTime.Today, 1200m, ExperienceLevel.Mid);
            var location = new SectionLocation("B1", "A", 1);
            var section = new AmbientSection("Ambient-1", location, 10, 20, SectionStatus.Active, false, null, 50);

            emp.AssignToSection(section);

            Assert.Contains(section, emp.AssignedSections);
            Assert.Contains(emp, section.Employees);
        }

        [Fact]
        public void EmployeeExtent_CanBeLoadedFromFile()
        {
            var path = Path.GetTempFileName();
            File.WriteAllLines(path, new[]
            {
                "GeneralEmployee;1;Charlie;2020-01-01;2000;1;test-note"
            });

            Employee.LoadExtent(path);

            var e = Assert.Single(Employee.Extent);
            Assert.IsType<GeneralEmployee>(e);
            Assert.Equal("Charlie", e.Name);
            Assert.Equal(ExperienceLevel.Junior, e.ExperienceLevel);
            Assert.Equal("test-note", e.Notes);
        }

        [Fact]
        public void SeniorExperienceLevel_IsUsable()
        {
            var emp = new GeneralEmployee("Diana", DateTime.Today, 3000m, ExperienceLevel.Senior);
            Assert.Equal(ExperienceLevel.Senior, emp.ExperienceLevel);
        }
    }

    public class SectionTests
    {
        [Fact]
        public void Section_AreaIsWidthTimesLength()
        {
            var loc = new SectionLocation("B1", "A", 1);
            var section = new AmbientSection("S1", loc, 10, 5, SectionStatus.Active, false, null, 40);

            Assert.Equal(50, section.Area, 2);
        }

        [Fact]
        public void CreatingSection_WithInvalidHumidity_Throws()
        {
            var loc = new SectionLocation("B1", "A", 1);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new AmbientSection("S1", loc, 10, 5, SectionStatus.Active, false, null, 150));
        }

        [Fact]
        public void CreatingSection_WithInvalidTemperature_Throws()
        {
            var loc = new SectionLocation("B1", "A", 1);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new AmbientSection("S1", loc, 10, 5, SectionStatus.Active, false, 100, 50));
        }

        [Fact]
        public void RefrigeratedSection_RecognizesTemperatureInRange()
        {
            var loc = new SectionLocation("B1", "A", 1);
            var section = new RefrigeratedSection("Cold-1", loc, 10, 5,
                SectionStatus.Maintenance, true, -5, 70, -10, 5);

            Assert.True(section.IsWithinOperationalTemperature);

            section.Temperature = 20;
            Assert.False(section.IsWithinOperationalTemperature);
        }

        [Fact]
        public void HazmatSection_RequiresAtLeastOneHazardType()
        {
            var loc = new SectionLocation("B1", "A", 1);

            Assert.Throws<ArgumentException>(() =>
                new HazmatSection("Haz-1", loc, 10, 5,
                    SectionStatus.Active, true, 20, 40,
                    Array.Empty<HazardType>(), true));
        }

        [Fact]
        public void SectionExtent_CanBeLoadedFromFile()
        {
            // CLEAR EXTENT
            Section.LoadExtent("___clean.tmp");

            var path = Path.GetTempFileName();
            File.WriteAllLines(path, new[]
            {
                "AmbientSection;S1;B1-A-1;10;5;0;1;25;40"
            });

            Section.LoadExtent(path);

            var s = Assert.Single(Section.Extent);
            Assert.IsType<AmbientSection>(s);
            Assert.Equal("S1", s.Name);
        }


        [Fact]
        public void SectionStatus_MaintenanceAndClosedAreUsable()
        {
            var loc = new SectionLocation("B2", "B", 2);
            var maintenanceSection = new AmbientSection("S-maint", loc, 8, 4, SectionStatus.Maintenance, false, null, 45);
            var closedSection = new AmbientSection("S-closed", loc, 6, 3, SectionStatus.Closed, true, null, 55);

            Assert.Equal(SectionStatus.Maintenance, maintenanceSection.Status);
            Assert.Equal(SectionStatus.Closed, closedSection.Status);
        }

        [Fact]
        public void HazmatSection_AllHazardTypesCanBeAdded()
        {
            var loc = new SectionLocation("B3", "C", 3);
            var allTypes = new[]
            {
                HazardType.Toxic,
                HazardType.Flammable,
                HazardType.Corrosive,
                HazardType.Irritant,
                HazardType.Sensitizer,
                HazardType.Asphyxiant
            };

            var section = new HazmatSection("Haz-all", loc, 12, 6,
                SectionStatus.Closed, true, 22, 35, allTypes, true);

            Assert.Equal(allTypes.Length, section.HazardTypes.Count);
            foreach (var hazard in allTypes)
            {
                Assert.Contains(hazard, section.HazardTypes);
            }
        }
    }
}
