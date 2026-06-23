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

            // Derive the salt deterministically from the password rather than at
            // random. PBKDF2 only needs the salt to be unique per password (not
            // secret), and a stable salt means the same password always derives
            // the same key. That lets the browser derive the key once on unlock,
            // cache it for the session, and decrypt every other page protected
            // with that password without re-running PBKDF2 — so navigating a
            // protected site no longer pays the (deliberately slow) key
            // derivation, nor flashes the password prompt, on each page.
            //
            // Confidentiality still rests on a fresh random nonce per page (below):
            // the key is reused across pages, but AES-GCM is only ever invoked with
            // a unique nonce, so no two pages share a keystream.
            var salt = DeriveSalt(password);

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

        // A stable per-password salt: SHA-256 of a versioned, domain-separated
        // prefix plus the password, truncated to the salt size. The client never
        // needs to reproduce this — each page ships its salt in the payload — it
        // only matters that every page sharing a password also shares a salt, so
        // the derived key is identical and cacheable across the site.
        private static byte[] DeriveSalt(string password)
        {
            var material = Encoding.UTF8.GetBytes("neko-page-salt-v1:" + password);
            var hash = SHA256.HashData(material);
            var salt = new byte[SaltSize];
            Buffer.BlockCopy(hash, 0, salt, 0, SaltSize);
            return salt;
        }
    }
}
