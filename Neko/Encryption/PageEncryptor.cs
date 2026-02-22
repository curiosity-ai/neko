using System;
using System.Security.Cryptography;
using System.Text;

namespace Neko.Encryption
{
    public class PageEncryptor
    {
        private const int SaltSize = 16;
        private const int KeySize = 32; // 256 bits
        private const int NonceSize = 12; // 96 bits
        private const int TagSize = 16; // 128 bits
        private const int Iterations = 100000;

        public record EncryptionResult(string Salt, string Iv, string Data);

        public static EncryptionResult Encrypt(string content, string password)
        {
            var plainBytes = Encoding.UTF8.GetBytes(content);

            // Generate Salt
            var salt = new byte[SaltSize];
            RandomNumberGenerator.Fill(salt);

            // Derive Key
            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var key = deriveBytes.GetBytes(KeySize);

            // Generate Nonce
            var nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            // Encrypt
            using var aes = new AesGcm(key, TagSize);
            var ciphertext = new byte[plainBytes.Length];
            var tag = new byte[TagSize];

            aes.Encrypt(nonce, plainBytes, ciphertext, tag);

            // Concatenate Ciphertext + Tag for Web Crypto API compatibility
            var combined = new byte[ciphertext.Length + tag.Length];
            Buffer.BlockCopy(ciphertext, 0, combined, 0, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, combined, ciphertext.Length, tag.Length);

            return new EncryptionResult(
                Convert.ToBase64String(salt),
                Convert.ToBase64String(nonce),
                Convert.ToBase64String(combined)
            );
        }
    }
}
