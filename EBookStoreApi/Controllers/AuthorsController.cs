using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EBookStoreApi.Data;
using EBookStoreApi.DTOs;

namespace EBookStoreApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorsController : ControllerBase
{
    private readonly EBookDbContext _context;

    public AuthorsController(EBookDbContext context)
    {
        _context = context;
    }

    // GET: api/Authors
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuthorLookupDto>>> GetAuthors()
    {
        var authors = await _context.Authors
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new AuthorLookupDto
            {
                Id = a.Id,
                Name = a.Name
            })
            .ToListAsync();

        return Ok(authors);
    }
}
