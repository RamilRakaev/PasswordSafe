using System.IO;
using System.Security.Cryptography;

namespace PasswordSafe.WPF.Services
{
    public class PasswordService
    {
        private readonly string _metaPath = "vault.meta";

        public bool IsInitialized() => File.Exists(_metaPath);

        public (byte[] salt, byte[] verifier) ReadMeta()
        {
            var bytes = File.ReadAllBytes(_metaPath);
            return (bytes[..16], bytes[16..]);
        }

        public void WriteMeta(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var verifier = DeriveVerifier(password, salt);
            File.WriteAllBytes(_metaPath, salt.Concat(verifier).ToArray());
        }

        public bool Verify(string password)
        {
            var (salt, verifier) = ReadMeta();
            var test = DeriveVerifier(password, salt);
            return CryptographicOperations.FixedTimeEquals(verifier, test);
        }

        private byte[] DeriveVerifier(string password, byte[] salt)
        {
            using var kdf = new Rfc2898DeriveBytes(password + "::verify", salt, 200_000, HashAlgorithmName.SHA256);
            return kdf.GetBytes(32);
        }
    }
}
