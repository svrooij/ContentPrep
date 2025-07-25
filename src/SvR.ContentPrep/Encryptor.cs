﻿using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using SvRooij.ContentPrep.Models;

namespace SvRooij.ContentPrep
{
    /// <summary>
    /// Encryptor class to encrypt and decrypt files
    /// </summary>
    internal static class Encryptor
    {
        private const string ProfileIdentifier = "ProfileVersion1";
        private const string FileDigestAlgorithm = "SHA256";
        internal const int DefaultBufferSize = 2097152;
        /// <summary>
        /// Encrypt a file in place and return the encryption information
        /// </summary>
        /// <param name="file">Input path</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal static async Task<FileEncryptionInfo> EncryptFileAsync(string file, CancellationToken cancellationToken = default)
        {
            // Create a temporary file to write the encrypted data to, in the same folder as the original file
            string tempLocation = Path.Combine(Path.GetDirectoryName(file)!, $"{Path.GetFileNameWithoutExtension(file)}.tmp");
            FileEncryptionInfo? encryptionInfo;
            using (var sourceStream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.Delete, bufferSize: DefaultBufferSize, useAsync: true))
            using (var targetStream = new FileStream(tempLocation, FileMode.Create, FileAccess.ReadWrite, FileShare.Delete, bufferSize: DefaultBufferSize, useAsync: true))
            {
                encryptionInfo = await EncryptStreamToStreamAsync(sourceStream, targetStream, cancellationToken);
            }

            File.Delete(file);
            File.Move(tempLocation, file);

            return encryptionInfo;
        }

        /// <summary>
        /// Encrypt a stream to a stream and return the encryption information
        /// </summary>
        /// <param name="inputStream"><see cref="Stream"/> that will be read from (requires Read and Seek), <see cref="MemoryStream"/> is advised</param>
        /// <param name="outputStream"><see cref="Stream"/> that will be written to (requires Read, Write and Seek), <see cref="MemoryStream"/> is advised</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal static async Task<FileEncryptionInfo> EncryptStreamToStreamAsync(Stream inputStream, Stream outputStream, CancellationToken cancellationToken = default)
        {
            byte[] encryptionKey = CreateAesKey();
            byte[] hmacKey = CreateAesKey();
            byte[] iv = GenerateAesIV();
            byte[]? inputHash;
            using (SHA256 hasher = SHA256.Create())
            {
                inputHash = await hasher.ComputeHashAsync(inputStream, cancellationToken);
            }

            // Rewind the input stream after hashing (which will read it to the end)
            inputStream.Seek(0, SeekOrigin.Begin);
            cancellationToken.ThrowIfCancellationRequested();

            // Encrypt the stream and write it to the output stream
            // The output stream will contain the hash (of the IV and the encrypted data), the IV and the encrypted data
            byte[] encryptedFileHash = await EncryptStreamWithIVAsync(inputStream, outputStream, encryptionKey, hmacKey, iv, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            FileEncryptionInfo fileEncryptionInfo = new FileEncryptionInfo
            {
                EncryptionKey = Convert.ToBase64String(encryptionKey),
                MacKey = Convert.ToBase64String(hmacKey),
                InitializationVector = Convert.ToBase64String(iv),
                Mac = Convert.ToBase64String(encryptedFileHash),
                ProfileIdentifier = ProfileIdentifier,
                FileDigest = Convert.ToBase64String(inputHash),
                FileDigestAlgorithm = FileDigestAlgorithm
            };

            return fileEncryptionInfo;
        }

        private static byte[] CreateAesKey()
        {
            using Aes aes = Aes.Create();
            aes.GenerateKey();
            return aes.Key;
        }

        private static byte[] GenerateAesIV()
        {
            using Aes aes = Aes.Create();
            return aes.IV;
        }

        /// <summary>
        /// Input stream will be encrypted and written to the target stream. The target stream will contain the hash (32 bytes) (of the IV and the encrypted data), the IV (16 bytes) and the encrypted data
        /// </summary>
        /// <param name="sourceStream">Input stream, needs Seek and Read</param>
        /// <param name="targetStream">Output stream, needs seek and Read and Write</param>
        /// <param name="encryptionKey">AES Encryption key</param>
        /// <param name="hmacKey">Some random data to compute an unique hash and validate the input afterwards</param>
        /// <param name="initializationVector">AES initialization vector</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <remarks>If the output stream is not 48 bytes bigger then the input stream, something is wrong</remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        private static async Task<byte[]> EncryptStreamWithIVAsync(
            Stream sourceStream,
            Stream targetStream,
            byte[] encryptionKey,
            byte[] hmacKey,
            byte[] initializationVector,
            CancellationToken cancellationToken)
        {
            if (sourceStream == null)
            {
                throw new ArgumentNullException(nameof(sourceStream));
            }
            if (targetStream == null)
            {
                throw new ArgumentNullException(nameof(targetStream));
            }
            if (sourceStream.CanRead == false)
            {
                throw new ArgumentException("The source stream must be readable", nameof(sourceStream));
            }
            if (targetStream.CanWrite == false || targetStream.CanSeek == false || targetStream.CanRead == false)
            {
                throw new ArgumentException("The target stream must support Read, Write and Seek", nameof(targetStream));
            }
            byte[]? encryptedFileHash;
            using Aes aes = Aes.Create();
            using HMACSHA256 hmac = new HMACSHA256(hmacKey);
            int hashSize = hmac.HashSize / 8; //32 bytes
            int ivLength = initializationVector.Length; // 16 bytes
            // Create an empty buffer for a specific length
            byte[] buffer = new byte[hashSize];
            // Write the empty hash (empty bytes) and IV to the targetFileStream 
            await targetStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
            await targetStream.WriteAsync(initializationVector, 0, ivLength, cancellationToken);
            await targetStream.FlushAsync(cancellationToken);
            // At this point the targetStream will contain the empty hash (32 bytes) and the IV (16 bytes)
            using (ICryptoTransform cryptoTransform = aes.CreateEncryptor(encryptionKey, initializationVector))
            // Create a CryptoStream to write the encrypted data to the targetStream
#if NET6_0_OR_GREATER
            using (CryptoStream cryptoStream = new CryptoStream(targetStream, cryptoTransform, CryptoStreamMode.Write, leaveOpen: true))
            {
                // Copy the sourceStream to the cryptoStream
                cancellationToken.ThrowIfCancellationRequested();
                await sourceStream.CopyToAsync(cryptoStream, DefaultBufferSize, cancellationToken);
                await cryptoStream.FlushFinalBlockAsync(cancellationToken);
                await cryptoStream.FlushAsync(cancellationToken);
                await targetStream.FlushAsync(cancellationToken);
            }
#else
            using (CryptoStream cryptoStream = new CryptoStream(targetStream, cryptoTransform, CryptoStreamMode.Write))
            {
                // Set the leaveOpen property of the CryptoStream to true
                // Hack found at https://stackoverflow.com/a/50878853
                // This property seems not available in the .NET Standard 2.0 version of CryptoStream
                //As of .NET 4.7.2, there is a second constructor with an added bool parameter called leaveOpen.
                //If this is set to true then the CryptoStream's dispose method will not call dispose on the underlying stream.
                var prop = cryptoStream.GetType().GetField("_leaveOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                prop?.SetValue(cryptoStream, true);

                // Copy the sourceStream to the cryptoStream
                cancellationToken.ThrowIfCancellationRequested();
                await sourceStream.CopyToAsync(cryptoStream, DefaultBufferSize, cancellationToken);

                cryptoStream.FlushFinalBlock();

                await cryptoStream.FlushAsync(cancellationToken);
                await targetStream.FlushAsync(cancellationToken);
            }
#endif

            cancellationToken.ThrowIfCancellationRequested();

            // Rewind the targetStream to the exact position where the IV should be written
            //targetStream.Seek(hashSize, SeekOrigin.Begin);
            //// Write the IV to the targetStream
            //await targetStream.WriteAsync(initializationVector, 0, initializationVector.Length, cancellationToken);
            //await targetStream.FlushAsync(cancellationToken);

            // Rewind the targetStream to the exact position of the start of the IV (which should be included in the hash)
            targetStream.Seek(hashSize, SeekOrigin.Begin);
            // Compute the hash of the targetStream
            byte[]? hash = await hmac.ComputeHashAsync(targetStream, cancellationToken);
            encryptedFileHash = hash;
            // Rewind the targetStream to the beginning
            targetStream.Seek(0L, SeekOrigin.Begin);
            // Write the hash to the targetStream
            await targetStream.WriteAsync(hash, 0, hash!.Length, cancellationToken);
            await targetStream.FlushAsync(cancellationToken);

            // At this point the targetStream will the hash (of the IV and the encrypted data), the IV and the encrypted data

            return encryptedFileHash!;
        }

        /// <summary>
        /// Validate and decrypt a stream using the encryptionKey and hmacKey
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="encryptionKey">Base64 representation of the encryption key</param>
        /// <param name="hmacKey">Base64 encoded representation of the hmacKey</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException">If the hash does not match it will throw an error</exception>
        internal static async Task<Stream> DecryptStreamAsync(Stream inputStream, string encryptionKey, string hmacKey, CancellationToken cancellationToken)
        {
            var resultStream = new MemoryStream();
            var encryptionKeyBytes = Convert.FromBase64String(encryptionKey);
            var hmacKeyBytes = Convert.FromBase64String(hmacKey);
            using Aes aes = Aes.Create();
            using HMACSHA256 hmac = new HMACSHA256(hmacKeyBytes);
            int offset = hmac.HashSize / 8;
            byte[] buffer = new byte[offset];
            await inputStream.ReadAsync(buffer, 0, offset, cancellationToken);
            byte[] hash = (await hmac.ComputeHashAsync(inputStream, cancellationToken))!;

            if (!buffer.CompareHashes(hash))
            {
                throw new InvalidDataException("Hashes do not match");
            }
            // Go to end of the hash
            inputStream.Seek(offset, SeekOrigin.Begin);

            // Read the IV from the stream (16 pytes)
            byte[] iv = new byte[aes.IV.Length];
            await inputStream.ReadAsync(iv, 0, iv.Length, cancellationToken);

            // At this point the inputStream is at the start of the encrypted data
            using ICryptoTransform cryptoTransform = aes.CreateDecryptor(encryptionKeyBytes, iv);
            using CryptoStream cryptoStream = new CryptoStream(inputStream, cryptoTransform, CryptoStreamMode.Read);
            await cryptoStream.CopyToAsync(resultStream, DefaultBufferSize, cancellationToken);

            resultStream.Seek(0, SeekOrigin.Begin);
            return resultStream;
        }
    }

    internal static class HashAlgorithmExtensions
    {
        internal static async Task<byte[]?> ComputeHashAsync(this HashAlgorithm hashAlgorithm, Stream stream, CancellationToken cancellationToken)
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

            return !input.Where((t, i) => t != compareTo[i]).Any();
        }
    }
}
