using System;
using System.IO;
using G42Warehouse;
using Xunit;

namespace G42Warehouse.Tests
{
    public class EmployeeTests
    {
        [Fact]
        public void Constructor_ValidData_AddsToExtentAndSetsProperties()
        {
            var employmentDate = DateTime.Now.AddYears(-2);
            int beforeCount = Employee.Extent.Count;

            var emp = new Employee(
                "John Doe",
                employmentDate,
                2000,
                EmployeeCategory.WarehouseManager,
                WorkerSpecialization.None
            );

            Assert.Equal("John Doe", emp.Name);
            Assert.Equal(2000, emp.BaseSalary);
            Assert.Equal(EmployeeCategory.WarehouseManager, emp.Category);
            Assert.Equal(WorkerSpecialization.None, emp.Specialization);
            Assert.True(Employee.Extent.Contains(emp));
            Assert.Equal(beforeCount + 1, Employee.Extent.Count);
        }
        
        [Fact]
        public void ExperienceLevel_DefaultsToJunior_WhenNotSpecified()
        {
            var emp = new Employee("Test", DateTime.Now.AddYears(-1), 1000);
            Assert.Equal(ExperienceLevelType.Junior, emp.ExperienceLevel);
        }

        [Fact]
        public void ExperienceLevel_CanBeSetToSenior()
        {
            var emp = new Employee("Test", DateTime.Now.AddYears(-1), 1000,
                EmployeeCategory.Worker,
                WorkerSpecialization.None,
                ExperienceLevelType.Senior);

            Assert.Equal(ExperienceLevelType.Senior, emp.ExperienceLevel);
        }

        [Fact]
        public void Constructor_EmptyName_ThrowsArgumentException()
        {
            var employmentDate = DateTime.Now.AddYears(-1);

            Assert.Throws<ArgumentException>(() =>
                new Employee("", employmentDate, 1500)
            );
        }

        [Fact]
        public void EmploymentDate_InFuture_ThrowsArgumentException()
        {
            var futureDate = DateTime.Now.AddDays(1);

            Assert.Throws<ArgumentException>(() =>
                new Employee("Test", futureDate, 1500)
            );
        }

        [Fact]
        public void BaseSalary_NotPositive_ThrowsArgumentException()
        {
            var employmentDate = DateTime.Now.AddYears(-1);

            Assert.Throws<ArgumentException>(() =>
                new Employee("Test", employmentDate, 0)
            );

            Assert.Throws<ArgumentException>(() =>
                new Employee("Test", employmentDate, -10)
            );
        }

        [Fact]
        public void Salary_IsDerivedFromBaseSalaryAndGrowth()
        {
            Employee.YearlyGrowth = 0.20;
            var employmentDate = DateTime.Now.AddYears(-3);
            var emp = new Employee("Test", employmentDate, 1000);

            int years = emp.YearsSinceEmployment;
            double expectedSalary = 1000 * (1 + years * Employee.YearlyGrowth);

            Assert.Equal(expectedSalary, emp.Salary, 3);
        }

        [Fact]
        public void SaveAndLoad_PreservesExtent()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "_employees.xml");

            try
            {
                Employee.Load(path);

                var e1 = new Employee("Alice", DateTime.Now.AddYears(-1), 1500);
                var e2 = new Employee("Bob", DateTime.Now.AddYears(-2), 2000,
                                      EmployeeCategory.Worker,
                                      WorkerSpecialization.DeliveryDriver);

                Employee.Save(path);

                bool loaded = Employee.Load(path);

                Assert.True(loaded);
                Assert.Equal(2, Employee.Extent.Count);
                Assert.Contains(Employee.Extent, e => e.Name == "Alice");
                Assert.Contains(Employee.Extent, e => e.Name == "Bob");
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void Extent_IsEncapsulated_ModifyingCopyDoesNotAffectExtent()
        {
            Employee.Load("non_existing_employees.xml"); // clears extent

            var e1 = new Employee("Temp Emp", DateTime.Now.AddYears(-1), 1000);

            var copy = new System.Collections.Generic.List<Employee>(Employee.Extent);
            copy.Clear();

            Assert.Equal(1, Employee.Extent.Count);
            Assert.True(Employee.Extent.Contains(e1));
        }
    }
}
