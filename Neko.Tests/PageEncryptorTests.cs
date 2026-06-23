using NUnit.Framework;
using Neko.Encryption;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Neko.Tests
{
    [TestFixture]
    public class PageEncryptorTests
    {
        // The browser caches the key it derives on first unlock and reuses it for
        // every other page protected with the same password. That only works if
        // those pages share a salt (same salt + same password => same derived key),
        // so the salt must be stable for a given password.
        [Test]
        public void Salt_IsStable_ForSamePassword()
        {
            var a = PageEncryptor.Encrypt("page one", "letmein");
            var b = PageEncryptor.Encrypt("page two", "letmein");

            Assert.That(b.Salt, Is.EqualTo(a.Salt), "Same password must derive the same salt so the session key is reusable across pages.");
        }

        [Test]
        public void Salt_Differs_ForDifferentPasswords()
        {
            var a = PageEncryptor.Encrypt("content", "alpha");
            var b = PageEncryptor.Encrypt("content", "beta");

            Assert.That(b.Salt, Is.Not.EqualTo(a.Salt));
        }

        // The key is reused across pages, so confidentiality rests on a unique
        // nonce (IV) per encryption. Even identical content under the same password
        // must produce a fresh random nonce.
        [Test]
        public void Nonce_IsRandom_PerEncryption()
        {
            var a = PageEncryptor.Encrypt("same content", "letmein");
            var b = PageEncryptor.Encrypt("same content", "letmein");

            Assert.That(b.Iv, Is.Not.EqualTo(a.Iv), "Each page must use a fresh nonce even with the same key.");
            Assert.That(b.Data, Is.Not.EqualTo(a.Data), "A fresh nonce must yield different ciphertext for identical input.");
        }

        // Round-trip the C# ciphertext through the same PBKDF2 + AES-GCM parameters
        // the browser uses (password.js), proving what ships actually decrypts with
        // a key derived solely from the password and the embedded salt.
        [Test]
        public void Encrypt_RoundTrips_WithBrowserParameters()
        {
            const string plaintext = "<h1>Secret</h1>";
            const string password = "letmein";

            var result = PageEncryptor.Encrypt(plaintext, password);

            var salt = Convert.FromBase64String(result.Salt);
            var nonce = Convert.FromBase64String(result.Iv);
            var combined = Convert.FromBase64String(result.Data);

            // password.js concatenates ciphertext||tag (16-byte GCM tag) for Web Crypto.
            const int tagSize = 16;
            var ciphertext = new byte[combined.Length - tagSize];
            var tag = new byte[tagSize];
            Buffer.BlockCopy(combined, 0, ciphertext, 0, ciphertext.Length);
            Buffer.BlockCopy(combined, ciphertext.Length, tag, 0, tagSize);

            using var derive = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            var key = derive.GetBytes(32);

            var decrypted = new byte[ciphertext.Length];
            using var aes = new AesGcm(key, tagSize);
            aes.Decrypt(nonce, ciphertext, tag, decrypted);

            Assert.That(Encoding.UTF8.GetString(decrypted), Is.EqualTo(plaintext));
        }
    }
}
