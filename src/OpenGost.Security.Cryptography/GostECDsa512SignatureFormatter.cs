﻿#if NET45
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace OpenGost.Security.Cryptography
{
    using static CryptoConfig;
    using static CryptoConstants;
    using static SecurityCryptographyStrings;

    /// <summary>
    /// Creates a <see cref="GostECDsa512"/> signature.
    /// </summary>
    [ComVisible(true)]
    public class GostECDsa512SignatureFormatter : AsymmetricSignatureFormatter
    {
        private GostECDsa512 _key;
        private string _oid;

        /// <summary>
        /// Initializes a new instance of the <see cref="GostECDsa512SignatureFormatter"/> class.
        /// </summary>
        public GostECDsa512SignatureFormatter()
        {
            _oid = MapNameToOID(Streebog512AlgorithmFullName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GostECDsa512SignatureFormatter"/> class with the specified key.
        /// </summary>
        /// <param name="key">
        /// The instance of <see cref="GostECDsa512"/> that holds the key.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="key"/> is <c>null</c>.
        /// </exception>
        public GostECDsa512SignatureFormatter(AsymmetricAlgorithm key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            _key = (GostECDsa512)key;
        }

        /// <summary>
        /// Creates the <see cref="GostECDsa512"/> signature for the specified data.
        /// </summary>
        /// <param name="rgbHash">
        /// The data to be signed.
        /// </param>
        /// <returns>
        /// The digital signature for the specified data.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="rgbHash"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="CryptographicUnexpectedOperationException">
        /// The OID is <c>null</c>.
        /// -or-
        /// The <see cref="GostECDsa512"/> key is <c>null</c>.
        /// </exception>
        public override byte[] CreateSignature(byte[] rgbHash)
        {
            if (rgbHash == null) throw new ArgumentNullException(nameof(rgbHash));

            if (_oid == null)
                throw new CryptographicUnexpectedOperationException(CryptographicMissingOid);

            if (_key == null)
                throw new CryptographicUnexpectedOperationException(CryptographicMissingKey);

            return _key.SignHash(rgbHash);
        }

        /// <summary>
        /// Specifies the hash algorithm for the <see cref="GostECDsa512"/> signature formatter.
        /// </summary>
        /// <param name="strName">
        /// The name of the hash algorithm to use for the signature formatter. 
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="strName"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="CryptographicUnexpectedOperationException">
        /// The <paramref name="strName"/> parameter does not map to the <see cref="Streebog512"/>
        /// hash algorithm.
        /// </exception>
        public override void SetHashAlgorithm(string strName)
        {
            if (strName == null) throw new ArgumentNullException(nameof(strName));

            if (MapNameToOID(strName) != _oid)
                throw new CryptographicUnexpectedOperationException(CryptographicInvalidOperation);
        }

        /// <summary>
        /// Specifies the key to be used for the <see cref="GostECDsa512"/> signature formatter.
        /// </summary>
        /// <param name="key">
        /// The instance of <see cref="GostECDsa512"/> that holds the key. 
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="key"/> is <c>null</c>.
        /// </exception>
        public override void SetKey(AsymmetricAlgorithm key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            _key = (GostECDsa512)key;
        }
    }
}
#endif
