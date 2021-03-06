﻿using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace OpenGost.Security.Cryptography
{
    using static CryptoConstants;
    using static CryptoUtils;

    /// <summary>
    /// Computes a Hash-based Message Authentication Code (HMAC)
    /// by using the <see cref="Streebog256"/> hash function.
    /// </summary>
    [ComVisible(true)]
    public class HMACStreebog256 : HMAC
    {
        /// <summary>
        /// Gets the size, in bits, of the computed hash code.
        /// </summary>
        /// <value>
        /// The size, in bits, of the computed hash code.
        /// </value>
        public override int HashSize => 256;

        /// <summary>
        /// Initializes a new instance of the <see cref="HMACStreebog256"/>
        /// class with a randomly generated key.
        /// </summary>
        public HMACStreebog256()
            : this(GenerateRandomBytes(64))
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HMACStreebog256"/>
        /// class with the specified key data.
        /// </summary>
        /// <param name="key">
        /// The secret key for <see cref="HMACStreebog256"/> encryption.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="key"/> parameter is <c>null</c>. 
        /// </exception>
        public HMACStreebog256(byte[] key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

#if NET45
            HashSizeValue = 256; 
#endif
            HashName = Streebog256AlgorithmFullName;
            base.Key = key;
        }
    }
}
