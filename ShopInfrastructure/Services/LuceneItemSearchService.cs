using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Microsoft.EntityFrameworkCore;
using ShopDomain.Model;
using ShopInfrastructure; // <-- щоб був DbshopContext
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ShopInfrastructure.Services
{
    public interface IItemSearchService
    {
        Task IndexItemAsync(Item item);
        Task DeleteItemAsync(int id);
        Task<(IEnumerable<ItemSearchDto> items, int total)> SearchAsync(
            string query,
            int skip,
            int limit,
            string? category = null,
            string? country = null);
    }

    public class ItemSearchDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int CountryId { get; set; }
        public string? CountryName { get; set; }
    }

    public class LuceneItemSearchService : IItemSearchService
    {
        private const string IndexFolder = "lucene-index";
        private static readonly LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
        private readonly FSDirectory _dir;
        private readonly StandardAnalyzer _analyzer;
        private readonly DbshopContext _db;

        public LuceneItemSearchService(DbshopContext db)
        {
            _db = db;
            System.IO.Directory.CreateDirectory(IndexFolder);
            _dir = FSDirectory.Open(new DirectoryInfo(IndexFolder));
            _analyzer = new StandardAnalyzer(AppLuceneVersion);
        }

        public async Task IndexItemAsync(Item item)
        {
            // дістанемо назви одразу
            var categoryName = await _db.Categories
                .Where(c => c.Id == item.CategoryId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync() ?? string.Empty;

            var countryName = await _db.OriginCountries
                .Where(c => c.Id == item.CountryId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync() ?? string.Empty;

            var config = new IndexWriterConfig(AppLuceneVersion, _analyzer);
            using var writer = new IndexWriter(_dir, config);

            writer.DeleteDocuments(new Term("id", item.Id.ToString()));

            var doc = new Document
            {
                new StringField("id", item.Id.ToString(), Field.Store.YES),
                new TextField("name", item.Name ?? string.Empty, Field.Store.YES),
                new TextField("description", item.Description ?? string.Empty, Field.Store.YES),
                new StringField("categoryId", item.CategoryId.ToString(), Field.Store.YES),
                new TextField("categoryName", categoryName, Field.Store.YES),
                new StringField("countryId", item.CountryId.ToString(), Field.Store.YES),
                new TextField("countryName", countryName, Field.Store.YES),
                new StringField("price", item.Price.ToString(CultureInfo.InvariantCulture), Field.Store.YES)
            };

            writer.AddDocument(doc);
            writer.Flush(true, true);
        }

        public Task DeleteItemAsync(int id)
        {
            var config = new IndexWriterConfig(AppLuceneVersion, _analyzer);
            using var writer = new IndexWriter(_dir, config);
            writer.DeleteDocuments(new Term("id", id.ToString()));
            writer.Flush(true, true);
            return Task.CompletedTask;
        }

        public Task<(IEnumerable<ItemSearchDto> items, int total)> SearchAsync(
            string query,
            int skip,
            int limit,
            string? category = null,
            string? country = null)
        {
            if (limit <= 0) limit = 10;

            using var reader = DirectoryReader.Open(_dir);
            if (reader.MaxDoc == 0)
                return Task.FromResult((Enumerable.Empty<ItemSearchDto>(), 0));

            var searcher = new IndexSearcher(reader);

            // шукаємо одразу по 4 полях
            var fields = new[] { "name", "description", "categoryName", "countryName" };
            var parser = new MultiFieldQueryParser(AppLuceneVersion, fields, _analyzer);

            if (string.IsNullOrWhiteSpace(query))
                query = "*:*";

            Query luceneQuery;
            try
            {
                luceneQuery = parser.Parse(query);
            }
            catch
            {
                luceneQuery = parser.Parse("*:*");
            }

            // шукаємо побільше, а потім фільтруємо
            var topDocs = searcher.Search(luceneQuery, skip + limit + 100);

            var docs = topDocs.ScoreDocs
                .Select(hit => searcher.Doc(hit.Doc))
                .Select(d =>
                {
                    decimal? price = null;
                    var priceStr = d.Get("price");
                    if (decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var p))
                        price = p;

                    return new ItemSearchDto
                    {
                        Id = int.Parse(d.Get("id")),
                        Name = d.Get("name"),
                        Description = d.Get("description"),
                        CategoryId = int.Parse(d.Get("categoryId")),
                        CategoryName = d.Get("categoryName"),
                        CountryId = int.Parse(d.Get("countryId")),
                        CountryName = d.Get("countryName"),
                        Price = price
                    };
                });

            // додаткове фільтрування вже в C#
            if (!string.IsNullOrWhiteSpace(category))
                docs = docs.Where(x => x.CategoryName != null &&
                                       x.CategoryName.Contains(category, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(country))
                docs = docs.Where(x => x.CountryName != null &&
                                       x.CountryName.Contains(country, StringComparison.OrdinalIgnoreCase));

            var filtered = docs.ToList();
            var total = filtered.Count;

            var page = filtered
                .Skip(skip)
                .Take(limit)
                .ToList();

            return Task.FromResult(((IEnumerable<ItemSearchDto>)page, total));
        }
    }
}
