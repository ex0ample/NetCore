using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EBookStoreApi.Data;
using EBookStoreApi.Entities;
using EBookStoreApi.DTOs;

namespace EBookStoreApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly EBookDbContext _context;
    private readonly ILogger<BooksController> _logger;

    public BooksController(EBookDbContext context, ILogger<BooksController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Books
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookResponseDto>>> GetBooks()
    {
        var books = await _context.Books
            .AsNoTracking() // เพิ่มประสิทธิภาพในการ Query อ่านอย่างเดียว
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Select(b => new BookResponseDto
            {
                Id = b.Id,
                Title = b.Title,
                ISBN = b.ISBN,
                Price = b.Price,
                PublishedDate = b.PublishedDate,
                FileUrl = b.FileUrl,
                AuthorId = b.AuthorId,
                AuthorName = b.Author != null ? b.Author.Name : string.Empty,
                CategoryId = b.CategoryId,
                CategoryName = b.Category != null ? b.Category.Name : string.Empty,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();

        return Ok(books);
    }

    // GET: api/Books/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookResponseDto>> GetBook(int id)
    {
        var book = await _context.Books
            .AsNoTracking()
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Where(b => b.Id == id)
            .Select(b => new BookResponseDto
            {
                Id = b.Id,
                Title = b.Title,
                ISBN = b.ISBN,
                Price = b.Price,
                PublishedDate = b.PublishedDate,
                FileUrl = b.FileUrl,
                AuthorId = b.AuthorId,
                AuthorName = b.Author != null ? b.Author.Name : string.Empty,
                CategoryId = b.CategoryId,
                CategoryName = b.Category != null ? b.Category.Name : string.Empty,
                CreatedAt = b.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (book == null)
        {
            return NotFound(new { message = $"Book with ID {id} was not found." });
        }

        return Ok(book);
    }

    // POST: api/Books
    [HttpPost]
    public async Task<ActionResult<BookResponseDto>> CreateBook([FromBody] CreateBookDto dto)
    {
        // 1. ตรวจสอบว่ามี AuthorId และ CategoryId อยู่จริงหรือไม่
        var authorExists = await _context.Authors.AnyAsync(a => a.Id == dto.AuthorId);
        if (!authorExists)
        {
            return BadRequest(new { message = $"Author with ID {dto.AuthorId} does not exist." });
        }

        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists)
        {
            return BadRequest(new { message = $"Category with ID {dto.CategoryId} does not exist." });
        }

        // 2. ตรวจสอบว่า ISBN ซ้ำหรือไม่
        var isbnExists = await _context.Books.AnyAsync(b => b.ISBN == dto.ISBN);
        if (isbnExists)
        {
            return Conflict(new { message = $"A book with ISBN '{dto.ISBN}' already exists." });
        }

        var book = new Book
        {
            Title = dto.Title,
            ISBN = dto.ISBN,
            Price = dto.Price,
            PublishedDate = dto.PublishedDate,
            FileUrl = dto.FileUrl,
            AuthorId = dto.AuthorId,
            CategoryId = dto.CategoryId,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // โหลดข้อมูล Navigation Property เพื่อคืนค่าใน Response
            await _context.Entry(book).Reference(b => b.Author).LoadAsync();
            await _context.Entry(book).Reference(b => b.Category).LoadAsync();

            var responseDto = new BookResponseDto
            {
                Id = book.Id,
                Title = book.Title,
                ISBN = book.ISBN,
                Price = book.Price,
                PublishedDate = book.PublishedDate,
                FileUrl = book.FileUrl,
                AuthorId = book.AuthorId,
                AuthorName = book.Author?.Name ?? string.Empty,
                CategoryId = book.CategoryId,
                CategoryName = book.Category?.Name ?? string.Empty,
                CreatedAt = book.CreatedAt
            };

            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, responseDto);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error creating book");
            return StatusCode(500, new { message = "An error occurred while saving the book to the database." });
        }
    }

    // PUT: api/Books/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateBook(int id, [FromBody] UpdateBookDto dto)
    {
        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound(new { message = $"Book with ID {id} was not found." });
        }

        // ตรวจสอบ ISBN ซ้ำกับเล่มอื่นหรือไม่
        var isbnConflict = await _context.Books.AnyAsync(b => b.ISBN == dto.ISBN && b.Id != id);
        if (isbnConflict)
        {
            return Conflict(new { message = $"Another book with ISBN '{dto.ISBN}' already exists." });
        }

        book.Title = dto.Title;
        book.ISBN = dto.ISBN;
        book.Price = dto.Price;
        book.PublishedDate = dto.PublishedDate;
        book.FileUrl = dto.FileUrl;
        book.AuthorId = dto.AuthorId;
        book.CategoryId = dto.CategoryId;

        try
        {
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Books.AnyAsync(b => b.Id == id))
                return NotFound();
            else
                throw;
        }
    }

    // DELETE: api/Books/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBook(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound(new { message = $"Book with ID {id} was not found." });
        }

        _context.Books.Remove(book);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/Books/search?search=Clean&categoryId=1&pageNumber=1&pageSize=10
    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<BookResponseDto>>> SearchBooks([FromQuery] BookQueryParameters queryParams)
    {
        // สร้าง IQueryable (ยังไม่มีการ Query ไปยัง SQL Server ณ บรรทัดนี้: Deferred Execution)
        var query = _context.Books
            .AsNoTracking()
            .Include(b => b.Author)
            .Include(b => b.Category)
            .AsQueryable();

        // 1. ค้นหาตามคำค้น (Title หรือ ISBN)
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var search = queryParams.Search.Trim().ToLower();
            query = query.Where(b => b.Title.ToLower().Contains(search) || b.ISBN.Contains(search));
        }

        // 2. กรองตาม CategoryId
        if (queryParams.CategoryId.HasValue)
        {
            query = query.Where(b => b.CategoryId == queryParams.CategoryId.Value);
        }

        // 3. กรองตาม AuthorId
        if (queryParams.AuthorId.HasValue)
        {
            query = query.Where(b => b.AuthorId == queryParams.AuthorId.Value);
        }

        // 4. นับจำนวนรายการทั้งหมดที่ตรงกับเงื่อนไข
        var totalCount = await query.CountAsync();

        // 5. ดึงข้อมูลเฉพาะหน้าที่ต้องการด้วย Skip & Take (SQL Server จะสร้างคำสั่ง OFFSET ... FETCH NEXT)
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .Select(b => new BookResponseDto
            {
                Id = b.Id,
                Title = b.Title,
                ISBN = b.ISBN,
                Price = b.Price,
                PublishedDate = b.PublishedDate,
                FileUrl = b.FileUrl,
                AuthorId = b.AuthorId,
                AuthorName = b.Author != null ? b.Author.Name : string.Empty,
                CategoryId = b.CategoryId,
                CategoryName = b.Category != null ? b.Category.Name : string.Empty,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();

        return Ok(new PagedResult<BookResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = queryParams.PageNumber,
            PageSize = queryParams.PageSize
        });
    }
}
