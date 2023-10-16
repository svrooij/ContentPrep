using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using SvR.ContentPrep.Models;

namespace SvR.ContentPrep
{
    internal static class Encryptor
    {
        private const string ProfileIdentifier = "ProfileVersion1";
        private const string FileDigestAlgorithm = "SHA256";
        internal static async Task<FileEncryptionInfo> EncryptFileAsync(string file, CancellationToken cancellationToken = default)
        {
            byte[] encryptionKey = CreateAesKey();
            byte[] hmacKey = CreateAesKey();
            byte[] iv = GenerateAesIV();
            string fileWithGuid = Path.Combine(Path.GetDirectoryName(file), Guid.NewGuid().ToString());
            cancellationToken.ThrowIfCancellationRequested();
            byte[] encryptedFileHash = await EncryptFileWithIVAsync(file, fileWithGuid, encryptionKey, hmacKey, iv, cancellationToken);

            byte[]? filehash = null;
            using (SHA256 hasher = SHA256.Create())
            // using (FileStream fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None))
            using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                filehash = await hasher.ComputeHashAsync(fileStream, cancellationToken);
                fileStream.Close();
            }
            cancellationToken.ThrowIfCancellationRequested();

            FileEncryptionInfo fileEncryptionInfo = new FileEncryptionInfo
            {
                EncryptionKey = Convert.ToBase64String(encryptionKey),
                MacKey = Convert.ToBase64String(hmacKey),
                InitializationVector = Convert.ToBase64String(iv),
                Mac = Convert.ToBase64String(encryptedFileHash),
                ProfileIdentifier = ProfileIdentifier,
                FileDigest = Convert.ToBase64String(filehash),
                FileDigestAlgorithm = FileDigestAlgorithm
            };
            await MoveFileAsync(fileWithGuid, file, cancellationToken);
            return fileEncryptionInfo;
        }

        private static byte[] CreateAesKey()
        {
            using (AesCryptoServiceProvider cryptoServiceProvider = new AesCryptoServiceProvider())
            {
                cryptoServiceProvider.GenerateKey();
                return cryptoServiceProvider.Key;
            }
        }

        private static byte[] GenerateAesIV()
        {
            using (Aes aes = Aes.Create())
            {
                return aes.IV;
            }
        }

        private static async Task<byte[]> EncryptFileWithIVAsync(
            string sourceFile,
            string targetFile,
            byte[] encryptionKey,
            byte[] hmacKey,
            byte[] initializationVector,
            CancellationToken cancellationToken)
        {
            byte[]? encryptedFileHash = null;
            using (Aes aes = Aes.Create())
            using (HMACSHA256 hmac = new HMACSHA256(hmacKey))
            using (FileStream targetFileStream = new FileStream(targetFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                int offset = hmac.HashSize / 8;
                //byte[] buffer = new byte[2097152];
                // Create an empty buffer for a specific length
                byte[] buffer = new byte[offset + initializationVector.Length];
                // Write the empty IV to the targetFileStream
                await targetFileStream.WriteAsync(buffer, 0, offset + initializationVector.Length, cancellationToken);
                using (ICryptoTransform cryptoTransform = aes.CreateEncryptor(encryptionKey, initializationVector))
                using (FileStream inputFileStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize: 4096, useAsync: true))
                using (CryptoStream cryptoStream = new CryptoStream(targetFileStream, cryptoTransform, CryptoStreamMode.Write))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await inputFileStream.CopyToAsync(cryptoStream, 2097152, cancellationToken);
                    cryptoStream.FlushFinalBlock();
                }
                cancellationToken.ThrowIfCancellationRequested();

                // Re-open the file to write the hash and the IV
                using (FileStream encryptedFileStream = new FileStream(targetFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    encryptedFileStream.Seek(offset, SeekOrigin.Begin);
                    await encryptedFileStream.WriteAsync(initializationVector, 0, initializationVector.Length, cancellationToken);
                    encryptedFileStream.Seek(offset, SeekOrigin.Begin);
                    byte[] hash = await hmac.ComputeHashAsync(encryptedFileStream, cancellationToken);
                    encryptedFileHash = hash;
                    encryptedFileStream.Seek(0L, SeekOrigin.Begin);
                    await encryptedFileStream.WriteAsync(hash, 0, hash.Length, cancellationToken);
                    encryptedFileStream.Close();
                }
            }
            return encryptedFileHash;
        }

        private static async Task MoveFileAsync(
            string inputFile,
            string targetFile,
            CancellationToken cancellationToken)
        {
            using (FileStream sourceStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize: 4096, useAsync: true))
            using (FileStream destStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await sourceStream.CopyToAsync(destStream, 2097152, cancellationToken);
            }
            File.Delete(inputFile);
        }

        internal static async Task<Stream> DecryptFileAsync(Stream inputStream, string encryptionKey, string hmacKey, CancellationToken cancellationToken)
        {
            var resultStream = new MemoryStream();
            var encryptionKeyBytes = Convert.FromBase64String(encryptionKey);
            var hmacKeyBytes = Convert.FromBase64String(hmacKey);
            using (Aes aes = Aes.Create())
            using (HMACSHA256 hmac = new HMACSHA256(hmacKeyBytes))
            {
                int offset = hmac.HashSize / 8;
                byte[] buffer = new byte[offset];
                await inputStream.ReadAsync(buffer, 0, offset, cancellationToken);
                byte[] hash = await hmac.ComputeHashAsync(inputStream, cancellationToken);

                if (!buffer.CompareHashes(hash))
                {
                    throw new InvalidDataException("Hashes do not match");
                }
                inputStream.Seek(offset, SeekOrigin.Begin);
                byte[] iv = new byte[aes.IV.Length];
                await inputStream.ReadAsync(iv, 0, iv.Length, cancellationToken);
                using (ICryptoTransform cryptoTransform = aes.CreateDecryptor(encryptionKeyBytes, iv))
                using (CryptoStream cryptoStream = new CryptoStream(inputStream, cryptoTransform, CryptoStreamMode.Read))
                {
                    await cryptoStream.CopyToAsync(resultStream, 2097152, cancellationToken);
                    resultStream.Seek(0, SeekOrigin.Begin);
                }
            }

            return resultStream;
        }
    }

    internal static class HashAlgorithmExtensions
    {
        internal static async Task<byte[]> ComputeHashAsync(this HashAlgorithm hashAlgorithm, Stream stream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) != 0)
            {
                hashAlgorithm.TransformBlock(buffer, 0, bytesRead, null, 0);
            }
            hashAlgorithm.TransformFinalBlock(buffer, 0, 0);
            return hashAlgorithm.Hash;
        }

        internal static bool CompareHashes(this byte[] input, byte[] compareTo)
        {
            if (input.Length != compareTo.Length)
            {
                return false;
            }

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] != compareTo[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
