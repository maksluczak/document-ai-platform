namespace DocumentService.Controllers;

using DocumentService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentDbContext _dbContext;

    public DocumentsController(DocumentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var docs = await _dbContext.Documents.ToListAsync();
        return Ok(docs);
    }
}