namespace G42Warehouse.Domain
{
    public enum SectionStatus
    {
        Active,
        Maintenance,
        Closed
    }

    public enum ExperienceLevel
    {
        Junior = 1,
        Mid = 2,
        Senior = 3
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
}