using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopDomain.Model;
using ShopInfrastructure;
using ShopInfrastructure.ViewModels;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace ShopInfrastructure.Controllers
{
    public class ItemsController : Controller
    {
        private readonly DbshopContext _context;
        private readonly IWebHostEnvironment _env;


        public ItemsController(DbshopContext context)
        {
            _context = context;
        }

        // GET: Items
        public async Task<IActionResult> Index(int? id, string? name)
        {
            if (id == null) return RedirectToAction("Categories", "Index");
            ViewBag.CategoryId = id;
            ViewBag.CategoryName = name;
            var itemByCategory = _context.Items.Where(i => i.CategoryId == id).Include(i => i.Category).Include(i => i.Country);
            return View(await itemByCategory.ToListAsync());
        }

        // GET: Items/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Country)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // GET: Items/Landing/5
        [HttpGet("Items/{id:int}/landing")]
        public async Task<IActionResult> Landing(int id)
        {
            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Country)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null) return NotFound();

            // Схожі товари з тієї ж категорії
            var related = await _context.Items
                .Where(x => x.CategoryId == item.CategoryId && x.Id != id)
                .OrderByDescending(x => x.Id)
                .Take(8)
                .ToListAsync();

            var vm = new ItemLandingVm
            {
                Item = item,
                Related = related
            };

            return View("Landing", vm);
        }


        // GET: Items/Create
        public IActionResult Create(int categoryId)
        {

            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = _context.Categories.Where(c => c.Id == categoryId).FirstOrDefault().Name;
            return View();
        }

        // POST: Items/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int categoryId, ItemViewModel itemViewModel)
        {
            if (ModelState.IsValid)
            {
                var categoryExists = _context.Categories.Any(c => c.Id == categoryId);
                if (!categoryExists)
                {
                    ModelState.AddModelError("CategoryId", "Категорія не знайдена в базі даних.");
                    return View(itemViewModel);
                }

                var country = _context.OriginCountries
                    .FirstOrDefault(c => c.Name == itemViewModel.CountryName);

                if (country == null)
                {
                    ModelState.AddModelError("CountryName", "Такої країни немає в базі даних.");
                    ViewBag.CategoryId = categoryId;
                    ViewBag.CategoryName = _context.Categories
                        .Where(c => c.Id == categoryId)
                        .Select(c => c.Name)
                        .FirstOrDefault();
                    return View(itemViewModel);
                }


                if (country.Id == 0)
                {
                    ModelState.AddModelError("CountryName", "Некоректний ідентифікатор країни.");
                    return View(itemViewModel);
                }

                // Завантаження файла
                string? relativePath = null;
                if (itemViewModel.ImageFile is { Length: > 0 })
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                    var ext = Path.GetExtension(itemViewModel.ImageFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("ImageFile", "Дозволено лише зображення (.jpg, .jpeg, .png, .gif, .bmp, .webp).");
                        return View(itemViewModel);
                    }

                    if (!itemViewModel.ImageFile.ContentType.StartsWith("image/"))
                    {
                        ModelState.AddModelError("ImageFile", "Файл має бути зображенням.");
                        return View(itemViewModel);
                    }

                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    Directory.CreateDirectory(uploadsDir);

                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = System.IO.File.Create(filePath))
                    {
                        await itemViewModel.ImageFile.CopyToAsync(stream);
                    }

                    relativePath = $"/uploads/{fileName}";
                }



                var item = new Item
                {
                    Name = itemViewModel.Name,
                    Description = itemViewModel.Description,
                    CategoryId = categoryId,
                    CountryId = country.Id,
                    Price = itemViewModel.Price,
                    ImagePath = relativePath
                };
                _context.Add(item);
                await _context.SaveChangesAsync();

                //return RedirectToAction("Index", "Items", new { Id = categoryId, name = _context.Categories.Where(c => c.Id == categoryId).FirstOrDefault().Name });
                return RedirectToAction(nameof(Index), "Items", new { categoryId });

            }

            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = _context.Categories
                .Where(c => c.Id == categoryId)
                .Select(c => c.Name)
                .FirstOrDefault();

            return View(itemViewModel);
        }

        // GET: Items/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.Items
                .Include(i => i.Country)
                .Include(i => i.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) return NotFound();

            var vm = new ItemEditViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                CountryId = item.CountryId,
                CategoryId = item.CategoryId,
                Price = item.Price,
                ImagePath = item.ImagePath,
                CountryName = item.Country?.Name,
                CategoryName = item.Category?.Name
            };

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", vm.CategoryId);
            ViewData["CountryId"] = new SelectList(_context.OriginCountries, "Id", "Name", vm.CountryId);
            ModelState.Clear();
            return View(vm);
        }



        // POST: Items/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ItemEditViewModel vm)
        {
            if (id != vm.Id) return BadRequest();

            var countryName = vm.CountryName?.Trim();
            var categoryName = vm.CategoryName?.Trim();

            if (!string.IsNullOrWhiteSpace(countryName))
            {
                var country = await _context.OriginCountries
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Name.Trim().ToLower() == countryName.ToLower());

                if (country is null)
                {
                    vm.CountryId = 0;
                    ModelState.AddModelError(nameof(vm.CountryName), "Такої країни немає в базі. Оберіть зі списку.");
                }
                else
                {
                    vm.CountryId = country.Id;
                }
            }
            else
            {
                var exists = await _context.OriginCountries.AnyAsync(c => c.Id == vm.CountryId);
                if (!exists) ModelState.AddModelError(nameof(vm.CountryId), "Оберіть країну зі списку.");
            }

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                var category = await _context.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Name.Trim().ToLower() == categoryName.ToLower());

                if (category is null)
                {
                    vm.CategoryId = 0;
                    ModelState.AddModelError(nameof(vm.CategoryName), "Такої категорії немає в базі. Оберіть зі списку.");
                }
                else
                {
                    vm.CategoryId = category.Id;
                }
            }
            else
            {
                var exists = await _context.Categories.AnyAsync(c => c.Id == vm.CategoryId);
                if (!exists) ModelState.AddModelError(nameof(vm.CategoryId), "Оберіть категорію зі списку.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", vm.CategoryId);
                ViewData["CountryId"] = new SelectList(_context.OriginCountries, "Id", "Name", vm.CountryId);
                return View(vm);
            }

            var item = await _context.Items.FindAsync(id);
            if (item == null) return NotFound();

            item.Name = vm.Name;
            item.Description = vm.Description;
            item.Price = vm.Price;
            item.CategoryId = vm.CategoryId;
            item.CountryId = vm.CountryId;

            if (vm.ImageFile is { Length: > 0 })
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                Directory.CreateDirectory(uploadsDir);
                var ext = Path.GetExtension(vm.ImageFile.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);
                using var stream = System.IO.File.Create(filePath);
                await vm.ImageFile.CopyToAsync(stream);

                if (!string.IsNullOrEmpty(item.ImagePath))
                {
                    var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", item.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }
                item.ImagePath = $"/uploads/{fileName}";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }





        // GET: Items/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Country)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // POST: Items/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        /*public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item != null)
            {
                _context.Items.Remove(item);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        } */

        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }


            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item != null)
            {
                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }



        private bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.Id == id);
        }
    }
}