using LiteDB;
using PasswordSafe.WPF.Models;

namespace PasswordSafe.WPF.Services
{
    public class DatabaseService
    {
        private readonly LiteDatabase _db;
        private readonly CryptoService _crypto;

        public DatabaseService(string password, byte[] salt, CryptoService crypto)
        {
            // Шифрование самого файла LiteDB
            var connStr = new ConnectionString($"Filename=vault.db;Password={password}");
            _db = new LiteDatabase(connStr);
            _crypto = crypto;
        }

        public List<SecretEntry> GetAll()
        {
            var entries = _db.GetCollection<SecretEntry>("entries").FindAll().ToList();
            var extras = _db.GetCollection<ExtraData>("extras").FindAll().ToList();

            foreach (var e in entries)
            {
                e.Title = _crypto.Decrypt(e.Title);
                e.Login = _crypto.Decrypt(e.Login);
                e.Secret = _crypto.Decrypt(e.Secret);
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
                Secret = _crypto.Encrypt(entry.Secret)
            };

            if (entry.Id == 0) col.Insert(copy);
            else col.Update(copy);

            // Перезаписываем extras
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

        public void Dispose() => _db?.Dispose();
    }
}
