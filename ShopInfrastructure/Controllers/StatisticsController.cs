using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopInfrastructure;
using System;

public class StatisticsController : Controller
{
    private readonly DbshopContext _context;
    public StatisticsController(DbshopContext ctx) => _context = ctx;

    [HttpGet]
    public async Task<IActionResult> Data()
    {
        var itemsByCategory = await _context.Items
            .GroupBy(i => i.Category.Name)
            .Select(g => new { label = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToListAsync();

        var itemsByCountry = await _context.Items
            .GroupBy(i => i.Country.Name)
            .Select(g => new { label = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToListAsync();

        var avgPriceByCategory = await _context.Items
            .GroupBy(i => i.Category.Name)
            .Select(g => new { label = g.Key, avg = Math.Round(g.Average(x => x.Price), 2) })
            .OrderByDescending(x => x.avg)
            .ToListAsync();

        return Json(new { itemsByCategory, itemsByCountry, avgPriceByCategory });
    }

    public IActionResult Overview() => View();
}
