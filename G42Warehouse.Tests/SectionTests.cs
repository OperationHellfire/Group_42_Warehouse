using System;
using System.IO;
using G42Warehouse;
using Xunit;

namespace G42Warehouse.Tests
{
    public class SectionTests
    {
        [Fact]
        public void Constructor_ValidData_AddsToExtentAndSetsEnums()
        {
            int beforeCount = Section.Extent.Count;

            var section = new Section(
                "HazMat A",
                "North Wing",
                250.0,
                true,
                SectionType.HazardousMaterials,
                SectionStatus.Active
            );

            Assert.Equal("HazMat A", section.Name);
            Assert.Equal("North Wing", section.Location);
            Assert.Equal(250.0, section.Area);
            Assert.True(section.HasBackupGenerator);
            Assert.Equal(SectionType.HazardousMaterials, section.Type);
            Assert.Equal(SectionStatus.Active, section.Status);

            Assert.True(Section.Extent.Contains(section));
            Assert.Equal(beforeCount + 1, Section.Extent.Count);
        }

        [Fact]
        public void Name_Empty_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new Section("", "Loc", 10, false)
            );
        }

        [Fact]
        public void Location_Empty_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new Section("Sec1", "", 10, false)
            );
        }

        [Fact]
        public void Area_NotPositive_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new Section("Sec1", "Loc", 0, false)
            );

            Assert.Throws<ArgumentException>(() =>
                new Section("Sec1", "Loc", -5, false)
            );
        }

        [Fact]
        public void Temperature_InvalidRange_ThrowsArgumentException()
        {
            var section = new Section("Cold Room", "B2", 50, true);

            Assert.Throws<ArgumentException>(() => section.Temperature = -100);
            Assert.Throws<ArgumentException>(() => section.Temperature = 100);
        }

        [Fact]
        public void Humidity_InvalidRange_ThrowsArgumentException()
        {
            var section = new Section("Ambient 1", "A1", 100, false);

            Assert.Throws<ArgumentException>(() => section.Humidity = -1);
            Assert.Throws<ArgumentException>(() => section.Humidity = 101);
        }

        [Fact]
        public void TemperatureAndHumidity_Optional_AndIsColdStorageWorks()
        {
            var section = new Section("Fridge", "C1", 30, true);

            section.Temperature = null;
            Assert.False(section.IsColdStorage);

            section.Temperature = 2;
            Assert.True(section.IsColdStorage);

            section.Humidity = null;
            section.Humidity = 40;
            Assert.Equal(40, section.Humidity);
        }

        [Fact]
        public void AddForbiddenMaterial_Valid_AddsToCollection()
        {
            var section = new Section("HazMat A", "D1", 150, true);

            section.AddForbiddenMaterial("Explosives");
            section.AddForbiddenMaterial("Flammable Liquids");

            Assert.Equal(2, section.ForbiddenMaterials.Count);
            Assert.Contains("Explosives", section.ForbiddenMaterials);
            Assert.Contains("Flammable Liquids", section.ForbiddenMaterials);
        }

        [Fact]
        public void AddForbiddenMaterial_Empty_ThrowsArgumentException()
        {
            var section = new Section("HazMat A", "D1", 150, true);

            Assert.Throws<ArgumentException>(() => section.AddForbiddenMaterial(""));
        }

        [Fact]
        public void SaveAndLoad_PreservesExtent()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "_sections.xml");

            try
            {
                Section.Load(path);

                var s1 = new Section("Ambient 1", "A1", 100, false,
                                     SectionType.AmbientStorage, SectionStatus.Active);
                s1.Temperature = 20;

                var s2 = new Section("Fridge 1", "C1", 40, true,
                                     SectionType.RefrigeratedStorage, SectionStatus.Maintenance);
                s2.Temperature = 2;
                s2.AddForbiddenMaterial("Biological Waste");

                Section.Save(path);

                bool loaded = Section.Load(path);

                Assert.True(loaded);
                Assert.Equal(2, Section.Extent.Count);
                Assert.Contains(Section.Extent, s => s.Name == "Ambient 1");
                Assert.Contains(Section.Extent, s => s.Name == "Fridge 1");
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
            Section.Load("non_existing_sections.xml"); // clears extent

            var s = new Section("Test", "T1", 10, false);

            var copy = new System.Collections.Generic.List<Section>(Section.Extent);
            copy.Clear();

            Assert.Equal(1, Section.Extent.Count);
            Assert.True(Section.Extent.Contains(s));
        }
    }
}
