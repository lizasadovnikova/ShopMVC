using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopInfrastructure;
using ShopDomain.Model;
using ShopInfrastructure.Services;
using Microsoft.Extensions.Caching.Memory;


namespace ShopInfrastructure.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")] 
    public class ItemsController : ControllerBase
    {
        private readonly DbshopContext _context;

        private readonly IItemSearchService _search;
        private readonly IMemoryCache _cache;

        private static string _itemsCacheVersion = "v1";
        private static string Version => _itemsCacheVersion;
        private static void BumpVersion() => _itemsCacheVersion = Guid.NewGuid().ToString("N");

        public ItemsController(DbshopContext context, IItemSearchService search, IMemoryCache cache)
        {
            _context = context;
            _search = search;
            _cache = cache;
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

        public class ItemMapDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = null!;
            public string? CategoryName { get; set; }
            public string? CountryName { get; set; }
            public decimal Price { get; set; }
            public double Lat { get; set; }
            public double Lng { get; set; }
        }

        private static readonly Dictionary<string, (double lat, double lng)> CountryCentroids =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Україна"] = (49.0, 31.0),
                ["США"] = (39.5, -98.35),
                ["Польща"] = (52.1, 19.4),
                ["Німеччина"] = (51.1, 10.4),
            };

        private static bool TryCountryCoords(string? countryName, out (double lat, double lng) p)
        {
            if (!string.IsNullOrWhiteSpace(countryName) && CountryCentroids.TryGetValue(countryName.Trim(), out p))
                return true;
            p = default;
            return false;
        }

        // GET: api/items/map?q=&categoryId=&countryId=&limit=
        [HttpGet("map")]
        public async Task<IActionResult> MapData(
            [FromQuery] string? q = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] int? countryId = null,
            [FromQuery] int limit = 1000)
        {
            if (limit <= 0) limit = 500;
            if (limit > 2000) limit = 2000;

            List<int> itemIds;
            if (!string.IsNullOrWhiteSpace(q))
            {
                var (items, total) = await _search.SearchAsync(q!, 0, limit);
                itemIds = items.Select(i => i.Id).ToList();
            }
            else
            {
                var queryEf = _context.Items.AsNoTracking();
                if (categoryId.HasValue) queryEf = queryEf.Where(i => i.CategoryId == categoryId);
                if (countryId.HasValue) queryEf = queryEf.Where(i => i.CountryId == countryId);
                itemIds = await queryEf
                    .OrderBy(i => i.Id)
                    .Select(i => i.Id)
                    .Take(limit)
                    .ToListAsync();
            }

            if (itemIds.Count == 0)
                return Ok(new { data = Array.Empty<ItemMapDto>() });

            var data = await _context.Items
                .Where(i => itemIds.Contains(i.Id))
                .Include(i => i.Category)
                .Include(i => i.Country)
                .AsNoTracking()
                .Select(i => new
                {
                    i.Id,
                    i.Name,
                    CategoryName = i.Category != null ? i.Category.Name : null,
                    CountryName = i.Country != null ? i.Country.Name : null,
                    i.Price,
                })
                .ToListAsync();

            var points = new List<ItemMapDto>(data.Count);
            foreach (var x in data)
            {
                (double lat, double lng) coords;

                if (TryCountryCoords(x.CountryName, out var c)) coords = c;
                else coords = (48.3794, 31.1656); // fallback: центр України

                points.Add(new ItemMapDto
                {
                    Id = x.Id,
                    Name = x.Name.Trim(),
                    CategoryName = x.CategoryName?.Trim(),
                    CountryName = x.CountryName?.Trim(),
                    Price = x.Price,
                    Lat = coords.lat,
                    Lng = coords.lng
                });
            }

            return Ok(new { data = points });
        }



        // api/items/reindex
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
        public async Task<ActionResult<object>> GetAll([FromQuery] int skip = 0, [FromQuery] int limit = 10)
        {
            if (limit <= 0) limit = 10;
            if (limit > 50) limit = 50;

            var cacheKey = $"items:{Version}:skip={skip}:limit={limit}";

            if (_cache.TryGetValue(cacheKey, out object? cached) && cached is not null)
            {
                Response.Headers["X-Cache"] = "HIT";
                Console.WriteLine($"[CACHE HIT] {cacheKey}");
                return Ok(cached);
            }

            var total = await _context.Items.CountAsync();
            var items = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Country)
                .AsNoTracking()
                .OrderBy(i => i.Id)
                .Skip(skip).Take(limit)
                .Select(i => new ItemApiDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Description = i.Description,
                    Price = i.Price,
                    CategoryId = i.CategoryId,
                    CategoryName = i.Category!.Name,
                    CountryId = i.CountryId,
                    CountryName = i.Country!.Name,
                    ImagePath = i.ImagePath
                })
                .ToListAsync();

            string? nextLink = null;
            var nextSkip = skip + limit;
            if (nextSkip < total)
                nextLink = Url.Action(nameof(GetAll), "Items", new { skip = nextSkip, limit }, Request.Scheme);

            var payload = new { data = items, total, skip, limit, nextLink };

            Response.Headers["X-Cache"] = "MISS";
            Console.WriteLine($"[CACHE MISS] {cacheKey}");
            _cache.Set(cacheKey, payload, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30),
                SlidingExpiration = TimeSpan.FromSeconds(15)
            });

            return Ok(payload);
        }



        // GET: api/items/search
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

            var normQ = (q ?? "").Trim().ToLowerInvariant();
            var normCat = (category ?? "").Trim().ToLowerInvariant();
            var normCty = (country ?? "").Trim().ToLowerInvariant();

            var cacheKey = $"items-search:{Version}:q={normQ}:cat={normCat}:cty={normCty}:s={skip}:l={limit}";

            if (_cache.TryGetValue(cacheKey, out object? cached) && cached is not null)
            {
                Response.Headers["X-Cache"] = "HIT";
                Console.WriteLine($"[CACHE HIT] {cacheKey}");
                return Ok(cached);
            }

            var (items, total) = await _search.SearchAsync(q, skip, limit, category, country);

            string? nextLink = null;
            var nextSkip = skip + limit;
            if (nextSkip < total)
                nextLink = Url.Action(nameof(Search), "Items",
                    new { q, skip = nextSkip, limit, category, country }, Request.Scheme);

            var payload = new { data = items, total, skip, limit, nextLink };

            Response.Headers["X-Cache"] = "MISS";
            Console.WriteLine($"[CACHE MISS] {cacheKey}");
            _cache.Set(cacheKey, payload, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30),
                SlidingExpiration = TimeSpan.FromSeconds(15)
            });

            return Ok(payload);
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
            BumpVersion(); 
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
            BumpVersion();


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
            BumpVersion();


            return Ok(new { status = "Ok" });
        }
    }
}
