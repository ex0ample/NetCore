using System.ComponentModel.DataAnnotations;

namespace EBookStoreApi.DTOs;

public class CreateBookDto
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "ISBN is required.")]
    [StringLength(20, ErrorMessage = "ISBN must not exceed 20 characters.")]
    [RegularExpression(@"^(?:97[89][- ]?)?(?:[0-9][- ]?){9}[0-9X]$", ErrorMessage = "Invalid ISBN format.")]
    public string ISBN { get; set; } = string.Empty;

    [Range(0.0, 99999.99, ErrorMessage = "Price must be a positive value.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Published Date is required.")]
    public DateTime PublishedDate { get; set; }

    [Url(ErrorMessage = "FileUrl must be a valid URL.")]
    public string? FileUrl { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AuthorId must be a valid ID.")]
    public int AuthorId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be a valid ID.")]
    public int CategoryId { get; set; }
}

public class UpdateBookDto : CreateBookDto
{
}

public class BookResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime PublishedDate { get; set; }
    public string? FileUrl { get; set; }
    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
