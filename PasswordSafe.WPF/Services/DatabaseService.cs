using LiteDB;
using PasswordSafe.WPF.Models;

namespace PasswordSafe.WPF.Services
{
    public class DatabaseService : IDisposable
    {
        private readonly LiteDatabase _db;
        private readonly CryptoService _crypto;

        public DatabaseService(string password, byte[] salt, CryptoService crypto)
        {
            var connStr = new ConnectionString($"Filename=vault.db;Password={password}");
            _db = new LiteDatabase(connStr);
            _crypto = crypto;
        }

        public List<SecretEntry> GetAll()
        {
            var entries = _db.GetCollection<SecretEntry>("entries").FindAll().ToList();
            var extras = _db.GetCollection<ExtraData>("extras").FindAll().ToList();
            var cats = GetCategories(); // уже расшифрованные

            var catDict = cats.ToDictionary(c => c.Id, c => c.Name);

            foreach (var e in entries)
            {
                e.Title = _crypto.Decrypt(e.Title);
                e.Login = _crypto.Decrypt(e.Login);
                e.Secret = _crypto.Decrypt(e.Secret);

                e.CategoryName = (e.CategoryId.HasValue && catDict.ContainsKey(e.CategoryId.Value))
                    ? catDict[e.CategoryId.Value]
                    : "Без категории";

                e.Extras = extras.Where(x => x.EntryId == e.Id).Select(x => new ExtraData
                {
                    Id = x.Id,
                    EntryId = x.EntryId,
                    Key = _crypto.Decrypt(x.Key),
                    Value = _crypto.Decrypt(x.Value)
                }).ToList();
            }
            return entries;
        }

        public void Save(SecretEntry entry)
        {
            var col = _db.GetCollection<SecretEntry>("entries");
            var ext = _db.GetCollection<ExtraData>("extras");

            var copy = new SecretEntry
            {
                Id = entry.Id,
                Title = _crypto.Encrypt(entry.Title),
                Login = _crypto.Encrypt(entry.Login),
                Secret = _crypto.Encrypt(entry.Secret),
                CategoryId = entry.CategoryId   // <-- сохраняем категорию
            };

            if (entry.Id == 0) col.Insert(copy);
            else col.Update(copy);

            ext.DeleteMany(x => x.EntryId == copy.Id);
            foreach (var ex in entry.Extras)
            {
                ext.Insert(new ExtraData
                {
                    EntryId = copy.Id,
                    Key = _crypto.Encrypt(ex.Key),
                    Value = _crypto.Encrypt(ex.Value)
                });
            }
        }

        public void Delete(int id)
        {
            _db.GetCollection<SecretEntry>("entries").Delete(id);
            _db.GetCollection<ExtraData>("extras").DeleteMany(x => x.EntryId == id);
        }

        public List<Category> GetCategories()
        {
            var col = _db.GetCollection<Category>("categories");
            return col.FindAll()
                      .Select(c => new Category
                      {
                          Id = c.Id,
                          Name = _crypto.Decrypt(c.Name)
                      })
                      .OrderBy(c => c.Name)
                      .ToList();
        }


        public void SaveCategory(Category category)
        {
            var col = _db.GetCollection<Category>("categories");
            var copy = new Category
            {
                Id = category.Id,
                Name = _crypto.Encrypt(category.Name)
            };

            if (category.Id == 0) col.Insert(copy);
            else col.Update(copy);
        }

        public void DeleteCategory(int id)
        {
            // Сбрасываем CategoryId у записей, привязанных к удаляемой категории
            var entries = _db.GetCollection<SecretEntry>("entries");
            var affected = entries.Find(e => e.CategoryId == id).ToList();
            foreach (var e in affected)
            {
                e.CategoryId = null;
                entries.Update(e);
            }

            _db.GetCollection<Category>("categories").Delete(id);
        }

        public void Dispose() => _db?.Dispose();
    }
}
