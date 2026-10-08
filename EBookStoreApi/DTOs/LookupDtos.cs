namespace EBookStoreApi.DTOs;

public class CategoryLookupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AuthorLookupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
