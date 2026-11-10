namespace RaceDay.Contracts
{
    public class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}


namespace RaceDay.Contracts
{
    public class CategoryCreateRequest
    {
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal EntryFee { get; set; }
        public int MaximumParticipants { get; set; } = 100;
    }
}
