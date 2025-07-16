using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
#if !NET6_0_OR_GREATER
namespace SvRooij.ContentPrep.Tests
{

    internal static class FrameworkCompatibility
    {
        /// <summary>
        /// Netstandard 2.0 compatible extension method to compute the hash of a stream asynchronously.
        /// </summary>
        /// <param name="hash">The <see cref="HashAlgorithm"/> instance used to compute the hash.</param>
        /// <param name="inputStream">The input stream to compute the hash for. Cannot be <see langword="null"/>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation. Defaults to <see
        /// cref="CancellationToken.None"/>.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a byte array representing the
        /// computed hash value.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="hash"/> or <paramref name="inputStream"/> is <see langword="null"/>.</exception>
        public static Task<byte[]> ComputeHashAsync(this HashAlgorithm hash, Stream inputStream, CancellationToken cancellationToken = default)
        {
            if (hash == null) throw new ArgumentNullException(nameof(hash));
            if (inputStream == null) throw new ArgumentNullException(nameof(inputStream));
            return Task.FromResult(hash.ComputeHash(inputStream));
        }

        /// <summary>
        /// Netstandard 2.0 compatible extension method to write a byte array to a <see cref="FileStream"/> asynchronously.
        /// </summary>
        /// <param name="fileStream"></param>
        /// <param name="buffer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ValueTask WriteAsync(this FileStream fileStream, byte[] buffer, CancellationToken cancellationToken = default)
        {
            if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
            
            return new ValueTask(fileStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken));
        }
    }
}
#endif