using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using EBookStoreApi.Models;
using System.Data;

namespace EBookStoreApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdoBooksController : ControllerBase
{
    private readonly string _connectionString;

    public AdoBooksController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    // GET: api/AdoBooks
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdoBook>>> GetAllBooks()
    {
        var books = new List<AdoBook>();
        const string sql = "SELECT Id, Title, ISBN, Price, PublishedDate, FileUrl, AuthorId, CategoryId FROM dbo.Books";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            books.Add(new AdoBook
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                ISBN = reader.GetString(reader.GetOrdinal("ISBN")),
                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                PublishedDate = reader.GetDateTime(reader.GetOrdinal("PublishedDate")),
                FileUrl = reader.IsDBNull(reader.GetOrdinal("FileUrl")) ? null : reader.GetString(reader.GetOrdinal("FileUrl")),
                AuthorId = reader.GetInt32(reader.GetOrdinal("AuthorId")),
                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId"))
            });
        }

        return Ok(books);
    }

    // GET: api/AdoBooks/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdoBook>> GetBookById(int id)
    {
        const string sql = "SELECT Id, Title, ISBN, Price, PublishedDate, FileUrl, AuthorId, CategoryId FROM dbo.Books WHERE Id = @Id";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var book = new AdoBook
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                ISBN = reader.GetString(reader.GetOrdinal("ISBN")),
                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                PublishedDate = reader.GetDateTime(reader.GetOrdinal("PublishedDate")),
                FileUrl = reader.IsDBNull(reader.GetOrdinal("FileUrl")) ? null : reader.GetString(reader.GetOrdinal("FileUrl")),
                AuthorId = reader.GetInt32(reader.GetOrdinal("AuthorId")),
                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId"))
            };
            return Ok(book);
        }

        return NotFound(new { message = $"Book with ID {id} was not found." });
    }

    // POST: api/AdoBooks
    [HttpPost]
    public async Task<ActionResult<AdoBook>> CreateBook([FromBody] AdoBook book)
    {
        const string sql = @"
            INSERT INTO dbo.Books (Title, ISBN, Price, PublishedDate, FileUrl, AuthorId, CategoryId)
            OUTPUT INSERTED.Id
            VALUES (@Title, @ISBN, @Price, @PublishedDate, @FileUrl, @AuthorId, @CategoryId);";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = book.Title;
        command.Parameters.Add("@ISBN", SqlDbType.VarChar, 20).Value = book.ISBN;
        command.Parameters.Add("@Price", SqlDbType.Decimal).Value = book.Price;
        command.Parameters.Add("@PublishedDate", SqlDbType.Date).Value = book.PublishedDate;
        command.Parameters.Add("@FileUrl", SqlDbType.NVarChar, 500).Value = (object?)book.FileUrl ?? DBNull.Value;
        command.Parameters.Add("@AuthorId", SqlDbType.Int).Value = book.AuthorId;
        command.Parameters.Add("@CategoryId", SqlDbType.Int).Value = book.CategoryId;

        await connection.OpenAsync();
        var newId = Convert.ToInt32(await command.ExecuteScalarAsync());
        book.Id = newId;

        return CreatedAtAction(nameof(GetBookById), new { id = book.Id }, book);
    }

    // PUT: api/AdoBooks/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateBook(int id, [FromBody] AdoBook book)
    {
        const string sql = @"
            UPDATE dbo.Books 
            SET Title = @Title,
                ISBN = @ISBN,
                Price = @Price,
                PublishedDate = @PublishedDate,
                FileUrl = @FileUrl,
                AuthorId = @AuthorId,
                CategoryId = @CategoryId
            WHERE Id = @Id;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        command.Parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = book.Title;
        command.Parameters.Add("@ISBN", SqlDbType.VarChar, 20).Value = book.ISBN;
        command.Parameters.Add("@Price", SqlDbType.Decimal).Value = book.Price;
        command.Parameters.Add("@PublishedDate", SqlDbType.Date).Value = book.PublishedDate;
        command.Parameters.Add("@FileUrl", SqlDbType.NVarChar, 500).Value = (object?)book.FileUrl ?? DBNull.Value;
        command.Parameters.Add("@AuthorId", SqlDbType.Int).Value = book.AuthorId;
        command.Parameters.Add("@CategoryId", SqlDbType.Int).Value = book.CategoryId;

        await connection.OpenAsync();
        int rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            return NotFound(new { message = $"Book with ID {id} was not found." });
        }

        return NoContent();
    }

    // DELETE: api/AdoBooks/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBook(int id)
    {
        const string sql = "DELETE FROM dbo.Books WHERE Id = @Id;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        await connection.OpenAsync();
        int rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            return NotFound(new { message = $"Book with ID {id} was not found." });
        }

        return NoContent();
    }
}
