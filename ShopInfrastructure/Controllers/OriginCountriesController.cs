using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopDomain.Model;
using ShopInfrastructure;

namespace ShopInfrastructure.Controllers
{
    public class OriginCountriesController : Controller
    {
        private readonly DbshopContext _context;

        public OriginCountriesController(DbshopContext context)
        {
            _context = context;
        }

        // GET: OriginCountries
        public async Task<IActionResult> Index()
        {
            return View(await _context.OriginCountries.ToListAsync());
        }

        // GET: OriginCountries/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var originCountry = await _context.OriginCountries
                .FirstOrDefaultAsync(m => m.Id == id);
            if (originCountry == null)
            {
                return NotFound();
            }

            return View(originCountry);
        }

        // GET: OriginCountries/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: OriginCountries/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Id")] OriginCountry originCountry)
        {
            if (ModelState.IsValid)
            {
                _context.Add(originCountry);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(originCountry);
        }

        // GET: OriginCountries/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var originCountry = await _context.OriginCountries.FindAsync(id);
            if (originCountry == null)
            {
                return NotFound();
            }
            return View(originCountry);
        }

        // POST: OriginCountries/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Name,Id")] OriginCountry originCountry)
        {
            if (id != originCountry.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(originCountry);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OriginCountryExists(originCountry.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(originCountry);
        }

        // GET: OriginCountries/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var originCountry = await _context.OriginCountries
                .FirstOrDefaultAsync(m => m.Id == id);
            if (originCountry == null)
            {
                return NotFound();
            }

            return View(originCountry);
        }

        // POST: OriginCountries/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var originCountry = await _context.OriginCountries
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (originCountry == null)
            {
                return NotFound();
            }

            if (originCountry.Items != null && originCountry.Items.Any())
            {
                TempData["ErrorMessage"] = "Неможливо видалити країну, оскільки є товари, які до неї належать.";
                return RedirectToAction(nameof(Index));
            }

            _context.OriginCountries.Remove(originCountry);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        private bool OriginCountryExists(int id)
        {
            return _context.OriginCountries.Any(e => e.Id == id);
        }
    }
}
