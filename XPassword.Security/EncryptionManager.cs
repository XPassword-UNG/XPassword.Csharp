﻿using System.Security.Cryptography;

namespace XPassword.Security;

public static class EncryptionManager
{
    public static string GenerateRandomKey()
    {
        var key = new byte[32]; // 256 bits
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }

    public static string? Encrypt(this string? targetString, string passwordKey)
    {
        try
        {
            if (targetString == null)
                return targetString;

            var salt = new byte[16];

            RandomNumberGenerator.Fill(salt);

            var key = GenerateKey(passwordKey, salt);

            using var aes = Aes.Create();

            aes.Key = key;
            aes.GenerateIV(); // Vetor de inicialização

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();

            // Escreve o IV no início do stream
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var writer = new StreamWriter(cs))
            {
                writer.Write(targetString);
            }

            var encryptedData = ms.ToArray();
            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(encryptedData);
        }
        catch { return null; }
    }

    public static string? Decrypt(this string? targetString, string passwordKey)
    {
        try
        {
            if (targetString == null)
                return targetString;

            string[] parts = targetString.Split(':');
            var salt = Convert.FromBase64String(parts[0]);
            var encryptedBytes = Convert.FromBase64String(parts[1]);
            var key = GenerateKey(passwordKey, salt);

            using var aes = Aes.Create();
            aes.Key = key;

            // Extrai o IV dos primeiros 16 bytes
            var iv = new byte[16];
            Array.Copy(encryptedBytes, iv, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(encryptedBytes, iv.Length, encryptedBytes.Length - iv.Length);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cs);

            return reader.ReadToEnd();
        }
        catch { return null; }
    }

    private static byte[] GenerateKey(string password, byte[] salt)
    {
        var keySize = 32;
        var iterations = 10000;

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(keySize);
    }
}