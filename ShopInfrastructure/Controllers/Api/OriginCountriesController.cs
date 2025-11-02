using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopInfrastructure;
using ShopDomain.Model;

namespace ShopInfrastructure.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class OriginCountriesController : ControllerBase
    {
        private readonly DbshopContext _context;

        public OriginCountriesController(DbshopContext context)
        {
            _context = context;
        }

        public class OriginCountryDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = null!;
            public int ItemsCount { get; set; }
        }

        public class OriginCountryCreateDto
        {
            public string Name { get; set; } = null!;
        }

        // GET: api/origincountries
        [HttpGet]
        public async Task<ActionResult<object>> GetAll(
    [FromQuery] int skip = 0,
    [FromQuery] int limit = 10)
        {
            if (limit <= 0) limit = 10;
            if (limit > 50) limit = 50;

            var total = await _context.OriginCountries.CountAsync();

            var countries = await _context.OriginCountries
                .Include(c => c.Items)
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .Skip(skip)
                .Take(limit)
                .Select(c => new OriginCountryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    ItemsCount = c.Items.Count
                })
                .ToListAsync();

            string? nextLink = null;
            var nextSkip = skip + limit;
            if (nextSkip < total)
            {
                nextLink = Url.Action(
                    action: nameof(GetAll),
                    controller: "OriginCountries",
                    values: new { skip = nextSkip, limit = limit },
                    protocol: Request.Scheme
                );
            }

            return Ok(new
            {
                data = countries,
                total,
                skip,
                limit,
                nextLink
            });
        }


        // GET: api/origincountries/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<OriginCountryDto>> GetOne(int id)
        {
            var c = await _context.OriginCountries
                .Include(x => x.Items)
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new OriginCountryDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    ItemsCount = x.Items.Count
                })
                .FirstOrDefaultAsync();

            if (c == null)
                return NotFound(new { status = "Error", message = "Країну не знайдено" });

            return Ok(c);
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

            var query = _context.OriginCountries
                .AsNoTracking()
                .Where(c => c.Name.Contains(q));

            var total = await query.CountAsync();

            var data = await query
                .OrderBy(c => c.Id)
                .Skip(skip)
                .Take(limit)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    ItemsCount = c.Items.Count
                })
                .ToListAsync();

            string? nextLink = null;
            var nextSkip = skip + limit;
            if (nextSkip < total)
            {
                nextLink = Url.Action(nameof(Search), "OriginCountries",
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

        // POST: api/origincountries
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OriginCountryCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { status = "Error", message = "Некоректні дані" });

            var entity = new OriginCountry
            {
                Name = dto.Name
            };

            _context.OriginCountries.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(new { status = "Ok", id = entity.Id });
        }

        // PUT: api/origincountries/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] OriginCountryCreateDto dto)
        {
            var entity = await _context.OriginCountries.FindAsync(id);
            if (entity == null)
                return NotFound(new { status = "Error", message = "Країну не знайдено" });

            entity.Name = dto.Name;
            await _context.SaveChangesAsync();

            return Ok(new { status = "Ok" });
        }

        // DELETE: api/origincountries/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var country = await _context.OriginCountries
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (country == null)
                return NotFound(new { status = "Error", message = "Країну не знайдено" });

            if (country.Items != null && country.Items.Any())
                return BadRequest(new { status = "Error", message = "Неможливо видалити країну, оскільки є товари, пов’язані з нею." });

            _context.OriginCountries.Remove(country);
            Console.WriteLine("Deleting country " + id);
            await _context.SaveChangesAsync();
            Console.WriteLine("Deleted.");


            return Ok(new { status = "Ok" });
        }


    }
}
