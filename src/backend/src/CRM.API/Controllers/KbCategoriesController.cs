using CRM.Domain.KnowledgeBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/kb/categories")]
[Authorize(Roles = "Admin,Manager,Agent")]
public class KbCategoriesController : ControllerBase
{
    private readonly IKbCategoryRepository _categories;

    public KbCategoriesController(IKbCategoryRepository categories) =>
        _categories = categories;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var cats = await _categories.ListActiveAsync(ct);
        return Ok(cats.Select(c => new { c.Id, c.Name }));
    }
}
