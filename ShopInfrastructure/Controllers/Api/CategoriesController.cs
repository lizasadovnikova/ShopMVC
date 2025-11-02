using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopInfrastructure;
using ShopDomain.Model;
using ShopInfrastructure.Services; 

namespace ShopInfrastructure.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly DbshopContext _context;

        public CategoriesController(DbshopContext context)
        {
            _context = context;
        }

        // DTO для виводу
        public class CategoryDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = null!;
            public string? Description { get; set; }
            public int ItemsCount { get; set; }
        }

        // DTO для створення/оновлення
        public class CategoryCreateDto
        {
            public string Name { get; set; } = null!;
            public string? Description { get; set; }
        }

        // GET: api/categories
        [HttpGet]
        public async Task<ActionResult<object>> GetAll(
    [FromQuery] int skip = 0,
    [FromQuery] int limit = 10)
        {
            if (limit <= 0) limit = 10;
            if (limit > 50) limit = 50;

            var total = await _context.Categories.CountAsync();

            var cats = await _context.Categories
                .Include(c => c.Items)
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .Skip(skip)
                .Take(limit)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ItemsCount = c.Items.Count
                })
                .ToListAsync();

            string? nextLink = null;
            var nextSkip = skip + limit;
            if (nextSkip < total)
            {
                nextLink = Url.Action(
                    action: nameof(GetAll),
                    controller: "Categories",
                    values: new { skip = nextSkip, limit = limit },
                    protocol: Request.Scheme
                );
            }

            return Ok(new
            {
                data = cats,
                total,
                skip,
                limit,
                nextLink
            });
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string q,
            [FromQuery] int skip = 0,
            [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(q))
                q = string.Empty;

            if (limit <= 0) limit = 10;
            if (limit > 50) limit = 50;

            var query = _context.Categories
                .AsNoTracking()
                .Where(c =>
                    c.Name.Contains(q) ||
                    (c.Description != null && c.Description.Contains(q)));

            var total = await query.CountAsync();

            var data = await query
                .OrderBy(c => c.Id)
                .Skip(skip)
                .Take(limit)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    ItemsCount = c.Items.Count
                })
                .ToListAsync();

            string? nextLink = null;
            var nextSkip = skip + limit;
            if (nextSkip < total)
            {
                nextLink = Url.Action(nameof(Search), "Categories",
                    new { q, skip = nextSkip, limit }, Request.Scheme);
            }

            return Ok(new
            {
                data,
                total,
                skip,
                limit,
                nextLink
            });
        }




        // GET: api/categories/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CategoryDto>> GetOne(int id)
        {
            var cat = await _context.Categories
                .Include(c => c.Items)
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ItemsCount = c.Items.Count
                })
                .FirstOrDefaultAsync();

            if (cat == null)
                return NotFound(new { status = "Error", message = "Категорію не знайдено" });

            return Ok(cat);
        }

        // POST: api/categories
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { status = "Error", message = "Некоректні дані" });

            var entity = new Category
            {
                Name = dto.Name,
                Description = dto.Description
            };

            _context.Categories.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(new { status = "Ok", id = entity.Id });
        }

        // PUT: api/categories/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryCreateDto dto)
        {
            var entity = await _context.Categories.FindAsync(id);
            if (entity == null)
                return NotFound(new { status = "Error", message = "Категорію не знайдено" });

            entity.Name = dto.Name;
            entity.Description = dto.Description;

            await _context.SaveChangesAsync();

            return Ok(new { status = "Ok" });
        }

        // DELETE: api/categories/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, [FromServices] IItemSearchService search)
        {
            var category = await _context.Categories
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound(new { status = "Error", message = "Категорію не знайдено" });

            if (category.Items != null)
            {
                foreach (var item in category.Items)
                {
                    await search.DeleteItemAsync(item.Id);
                }
            }

            _context.Items.RemoveRange(category.Items);
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { status = "Ok" });
        }
    }
}
