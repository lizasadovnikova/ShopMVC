using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ShopInfrastructure.Controllers
{
    public class LookupController : Controller
    {
        private readonly DbshopContext _ctx;
        public LookupController(DbshopContext ctx) => _ctx = ctx;

        [HttpGet]
        public async Task<IActionResult> Countries(string term)
        {
            var q = (term ?? string.Empty).Trim();
            if (q.Length < 3) return Json(Array.Empty<object>());

            var countries = await _ctx.OriginCountries
                .AsNoTracking()
#if NET6_0_OR_GREATER
                .Where(c =>
                    EF.Functions.Like(
                        EF.Functions.Collate(c.Name, "Ukrainian_100_CI_AS"),
                        "%" + q + "%"))
#else
                .Where(c => EF.Functions.Like(c.Name.ToLower(), "%" + q.ToLower() + "%"))
#endif
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    id = c.Id,
                    label = (c.Name ?? string.Empty).Trim(),
                    value = (c.Name ?? string.Empty).Trim()
                })
                .Distinct()
                .Take(20)
                .ToListAsync();

            return Json(countries);
        }

        [HttpGet]
        public async Task<IActionResult> Categories(string term)
        {
            var q = (term ?? string.Empty).Trim();
            if (q.Length < 3) return Json(Array.Empty<object>());

            var categories = await _ctx.Categories
                .AsNoTracking()
#if NET6_0_OR_GREATER
                .Where(c =>
                    EF.Functions.Like(
                        EF.Functions.Collate(c.Name, "Ukrainian_100_CI_AS"),
                        "%" + q + "%"))
#else
                .Where(c => EF.Functions.Like(c.Name.ToLower(), "%" + q.ToLower() + "%"))
#endif
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    id = c.Id,
                    label = (c.Name ?? string.Empty).Trim(),
                    value = (c.Name ?? string.Empty).Trim()
                })
                .Distinct()
                .Take(20)
                .ToListAsync();

            return Json(categories);
        }
    }
}
