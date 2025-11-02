using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopInfrastructure;
using ShopDomain.Model;
using ShopInfrastructure.Services;


namespace ShopInfrastructure.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")] 
    public class ItemsController : ControllerBase
    {
        private readonly DbshopContext _context;

        private readonly IItemSearchService _search;

        public ItemsController(DbshopContext context, IItemSearchService search)
        {
            _context = context;
            _search = search;
        }


        public class ItemApiDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = null!;
            public string? Description { get; set; }
            public decimal Price { get; set; }

            public int CategoryId { get; set; }
            public string? CategoryName { get; set; }

            public int CountryId { get; set; }
            public string? CountryName { get; set; }

            public string? ImagePath { get; set; }
        }

        public class ItemCreateDto
        {
            public string Name { get; set; } = null!;
            public string? Description { get; set; }
            public decimal Price { get; set; }
            public int CategoryId { get; set; }
            public int CountryId { get; set; }
            public string? ImagePath { get; set; } 
        }

        // api/items/reindex  (GET або POST) — разове переіндексування всіх товарів
        [HttpGet("reindex")]
        [HttpPost("reindex")]
        public async Task<IActionResult> Reindex()
        {
            var allItems = await _context.Items.ToListAsync();

            foreach (var item in allItems)
            {
                await _search.IndexItemAsync(item);
            }

            return Ok(new { status = "Ok", count = allItems.Count });
        }


        // GET: api/items
        [HttpGet]
        public async Task<ActionResult<object>> GetAll(
    [FromQuery] int skip = 0,
    [FromQuery] int limit = 10)
        {
            if (limit <= 0) limit = 10;
            if (limit > 50) limit = 50;

            var total = await _context.Items.CountAsync();

            var items = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Country)
                .AsNoTracking()
                .OrderBy(i => i.Id)
                .Skip(skip)
                .Take(limit)
                .Select(i => new ItemApiDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Description = i.Description,
                    Price = i.Price,
                    CategoryId = i.CategoryId,
                    CategoryName = i.Category != null ? i.Category.Name : null,
                    CountryId = i.CountryId,
                    CountryName = i.Country != null ? i.Country.Name : null,
                    ImagePath = i.ImagePath
                })
                .ToListAsync();

            string? nextLink = null;
            var nextSkip = skip + limit;
            if (nextSkip < total)
            {
                nextLink = Url.Action(
                    action: nameof(GetAll),
                    controller: "Items",
                    values: new { skip = nextSkip, limit = limit },
                    protocol: Request.Scheme
                );
            }

            return Ok(new
            {
                data = items,
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
    [FromQuery] int limit = 10,
    [FromQuery] string? category = null,
    [FromQuery] string? country = null)
        {
            if (limit <= 0) limit = 10;
            if (limit > 50) limit = 50;

            var (items, total) = await _search.SearchAsync(q, skip, limit, category, country);

            string? nextLink = null;
            var nextSkip = skip + limit;
            if (nextSkip < total)
            {
                nextLink = Url.Action(nameof(Search), "Items",
                    new { q, skip = nextSkip, limit, category, country }, Request.Scheme);
            }

            return Ok(new
            {
                data = items,
                total,
                skip,
                limit,
                nextLink
            });
        }





        // GET: api/items/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ItemApiDto>> GetOne(int id)
        {
            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Country)
                .AsNoTracking()
                .Where(i => i.Id == id)
                .Select(i => new ItemApiDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Description = i.Description,
                    Price = i.Price,
                    CategoryId = i.CategoryId,
                    CategoryName = i.Category != null ? i.Category.Name : null,
                    CountryId = i.CountryId,
                    CountryName = i.Country != null ? i.Country.Name : null,
                    ImagePath = i.ImagePath
                })
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { status = "Error", message = "Не знайдено" });

            return Ok(item);
        }

        // POST: api/items
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ItemCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { status = "Error", message = "Некоректні дані" });

            if (dto.Price < 0)
                return BadRequest(new { status = "Error", message = "Ціна не може бути від’ємною" });

            var catExists = await _context.Categories
                .AnyAsync(c => c.Id == dto.CategoryId);
            if (!catExists)
                return BadRequest(new { status = "Error", message = "Такої категорії немає" });

            var countryExists = await _context.OriginCountries
                .AnyAsync(c => c.Id == dto.CountryId);
            if (!countryExists)
                return BadRequest(new { status = "Error", message = "Такої країни немає" });

            var item = new Item
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                CategoryId = dto.CategoryId,
                CountryId = dto.CountryId,
                ImagePath = dto.ImagePath
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();
            await _search.IndexItemAsync(item);


            return Ok(new { status = "Ok", id = item.Id });
        }



        // PUT: api/items/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ItemCreateDto dto)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return NotFound(new { status = "Error", message = "Елемент не знайдено" });

            if (dto.Price < 0)
                return BadRequest(new { status = "Error", message = "Ціна не може бути від’ємною" });

            var catExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!catExists)
                return BadRequest(new { status = "Error", message = "Такої категорії немає" });

            var countryExists = await _context.OriginCountries.AnyAsync(c => c.Id == dto.CountryId);
            if (!countryExists)
                return BadRequest(new { status = "Error", message = "Такої країни немає" });

            item.Name = dto.Name;
            item.Description = dto.Description;
            item.Price = dto.Price;
            item.CategoryId = dto.CategoryId;
            item.CountryId = dto.CountryId;
            item.ImagePath = dto.ImagePath;

            await _context.SaveChangesAsync();
            await _search.IndexItemAsync(item);


            return Ok(new { status = "Ok" });
        }


        // DELETE: api/items/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return NotFound(new { status = "Error", message = "Елемент не знайдено" });

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
            await _search.DeleteItemAsync(id);


            return Ok(new { status = "Ok" });
        }
    }
}
