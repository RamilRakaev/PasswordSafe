using System.Security.Cryptography;
using System.Text;

namespace PasswordSafe.WPF.Services
{
    public class CryptoService
    {
        private readonly byte[] _key;

        public CryptoService(string password, byte[] salt)
        {
            // Производим ключ из пароля
            using var kdf = new Rfc2898DeriveBytes(password, salt, 200_000, HashAlgorithmName.SHA256);
            _key = kdf.GetBytes(32);
        }

        public string Encrypt(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return plain;

            var nonce = RandomNumberGenerator.GetBytes(12);
            var plainBytes = Encoding.UTF8.GetBytes(plain);
            var cipher = new byte[plainBytes.Length];
            var tag = new byte[16];

            using var aes = new AesGcm(_key, 16);
            aes.Encrypt(nonce, plainBytes, cipher, tag);

            // [nonce(12)][tag(16)][cipher]
            var result = new byte[nonce.Length + tag.Length + cipher.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, 12);
            Buffer.BlockCopy(tag, 0, result, 12, 16);
            Buffer.BlockCopy(cipher, 0, result, 28, cipher.Length);
            return Convert.ToBase64String(result);
        }

        public string Decrypt(string encrypted)
        {
            if (string.IsNullOrEmpty(encrypted)) return encrypted;

            var data = Convert.FromBase64String(encrypted);
            var nonce = data[..12];
            var tag = data[12..28];
            var cipher = data[28..];
            var plain = new byte[cipher.Length];

            using var aes = new AesGcm(_key, 16);
            aes.Decrypt(nonce, cipher, tag, plain);
            return Encoding.UTF8.GetString(plain);
        }

        public void Clear() => Array.Clear(_key, 0, _key.Length);
    }
}
