﻿using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace OpenGost.Security.Cryptography
{
    using static Buffer;
    using static SecurityCryptographyStrings;
    using static CryptoUtils;

    /// <summary>
    /// Represents the abstract class from which implementations of symmetric algorithm
    /// (<see cref="SymmetricAlgorithm"/>) can derive.
    /// </summary>
    [ComVisible(true)]
    public abstract class SymmetricTransform : ICryptoTransform
    {
        private readonly SymmetricTransformMode _transformMode;
        private readonly CipherMode _cipherMode;
        private readonly PaddingMode _paddingMode;
        private readonly int _blockSize;

        private byte[] _rgbKey;
        private byte[] _rgbIV;
        private byte[] _depadBuffer;
        private byte[] _stateBuffer;
        private byte[] _tempBuffer;
        private bool _keyExpanded;

        /// <summary>
        /// Gets a value indicating whether the current transform can be reused.
        /// </summary>
        /// <value>
        /// Always <c>true</c>.
        /// </value>
        public bool CanReuseTransform => true;

        /// <summary>
        /// Gets a value indicating whether multiple blocks can be transformed.
        /// </summary>
        /// <value>
        /// Always <c>true</c>.
        /// </value>
        public bool CanTransformMultipleBlocks => true;

        /// <summary>
        /// Gets the input block size.
        /// </summary>
        /// <value>
        /// The size of the input data blocks in bytes.
        /// </value>
        public int InputBlockSize => _blockSize;

        /// <summary>
        /// Gets the output block size.
        /// </summary>
        /// <value>
        /// The size of the output data blocks in bytes.
        /// </value>
        public int OutputBlockSize => _blockSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="SymmetricTransform" /> class.
        /// </summary>
        /// <param name="key">
        /// The secret key to be used for the symmetric algorithm.
        /// </param>
        /// <param name="iv">
        /// The initialization vector (<see cref="SymmetricAlgorithm.IV" />) to be used
        /// for the symmetric algorithm.
        /// </param>
        /// <param name="blockSize">
        /// The block size, in bits, of the cryptographic operation.
        /// </param>
        /// <param name="cipherMode">
        /// The mode for operation of the symmetric transform.
        /// </param>
        /// <param name="paddingMode">
        /// The padding mode used in the symmetric transform.
        /// </param>
        /// <param name="transformMode">
        /// The direction mode of the symmetric transform.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="key"/> parameter is <c>null</c>.
        /// -or-
        /// <paramref name="iv"/> parameter is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="blockSize"/> parameter is non-positive.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// <paramref name="cipherMode"/> parameter value is not supported.
        /// </exception>
        protected SymmetricTransform(byte[] key, byte[] iv, int blockSize, CipherMode cipherMode, PaddingMode paddingMode, SymmetricTransformMode transformMode)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (blockSize <= 0) throw new ArgumentOutOfRangeException(nameof(key), ArgumentOutOfRangeNeedPositiveNum);

            _transformMode = transformMode;
            _blockSize = blockSize / 8;
            _cipherMode = cipherMode;
            _paddingMode = paddingMode;

            switch (_cipherMode)
            {
                case CipherMode.ECB:
                    break;

                case CipherMode.CBC:
#if NET45
                case CipherMode.CFB:
                case CipherMode.OFB:
#endif
                    if (iv == null) throw new ArgumentNullException(nameof(iv));
                    _rgbIV = (byte[])iv.Clone();
                    _stateBuffer = new byte[_rgbIV.Length];
                    _tempBuffer = new byte[_blockSize];
                    Reset();
                    break;

                default:
                    throw new CryptographicException(CryptographicInvalidCipherMode);
            }

            _rgbKey = (byte[])key.Clone();
        }

        /// <summary>
        /// When overridden in a derived class, initializes the private key expansion.
        /// </summary>
        /// <param name="key">
        /// The private key to be used for the key expansion.
        /// </param>
        protected abstract void GenerateKeyExpansion(byte[] key);

        /// <summary>
        /// When overridden in a derived class, implements the block cipher encryption function.
        /// </summary>
        /// <param name="inputBuffer">
        /// The input to perform the operation on.
        /// </param>
        /// <param name="inputOffset">
        /// The offset into the input byte array to begin using data from.
        /// </param>
        /// <param name="outputBuffer">
        /// The output to write the data to.
        /// </param>
        /// <param name="outputOffset">
        /// The offset into the output byte array to begin writing data to.
        /// </param>
        protected abstract void EncryptBlock(byte[] inputBuffer, int inputOffset, byte[] outputBuffer, int outputOffset);

        /// <summary>
        /// When overridden in a derived class, implements the block cipher decryption function.
        /// </summary>
        /// <param name="inputBuffer">
        /// The input to perform the operation on.
        /// </param>
        /// <param name="inputOffset">
        /// The offset into the input byte array to begin using data from.
        /// </param>
        /// <param name="outputBuffer">
        /// The output to write the data to.
        /// </param>
        /// <param name="outputOffset">
        /// The offset into the output byte array to begin writing data to.
        /// </param>
        protected abstract void DecryptBlock(byte[] inputBuffer, int inputOffset, byte[] outputBuffer, int outputOffset);

        private void Reset()
        {
            EraseData(ref _depadBuffer);

#if NET45
            if (_cipherMode == CipherMode.CBC || _cipherMode == CipherMode.CFB || _cipherMode == CipherMode.OFB)
#elif NETCOREAPP1_0
            if (_cipherMode == CipherMode.CBC)
#endif
            {
                BlockCopy(_rgbIV, 0, _stateBuffer, 0, _rgbIV.Length);
            }
        }

        /// <summary>
        /// Computes the transformation for the specified region of the input byte array and
        /// copies the resulting transformation to the specified region of the output byte array.
        /// </summary>
        /// <param name="inputBuffer">
        /// The input to perform the operation on.
        /// </param>
        /// <param name="inputOffset">
        /// The offset into the input byte array to begin using data from.
        /// </param>
        /// <param name="inputCount">
        /// The number of bytes in the input byte array to use as data.
        /// </param>
        /// <param name="outputBuffer">
        /// The output to write the data to.
        /// </param>
        /// <param name="outputOffset">
        /// The offset into the output byte array to begin writing data to.
        /// </param>
        /// <returns>
        /// The number of bytes written.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="inputBuffer" /> parameter is <c>null</c>.
        /// -or-
        /// The <paramref name="outputBuffer" /> parameter is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value of the <paramref name="inputOffset" /> parameter is negative.
        /// -or-
        /// The value of the <paramref name="inputCount" /> parameter is non-positive.
        /// -or-
        /// The value of the <paramref name="outputOffset" /> parameter is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The length of the input buffer is less than the sum of the input offset and the input count.
        /// -or-
        /// The value of the <paramref name="inputCount" /> parameter is greater than the length of the <paramref name="inputBuffer" /> parameter.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// The length of the <paramref name="inputCount" /> parameter is not evenly devisable by input block size.
        /// </exception>
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            if (inputBuffer == null) throw new ArgumentNullException(nameof(inputBuffer));
            if (outputBuffer == null) throw new ArgumentNullException(nameof(outputBuffer));
            if (inputOffset < 0) throw new ArgumentOutOfRangeException(nameof(inputOffset), inputOffset, ArgumentOutOfRangeNeedNonNegNum);
            if (outputOffset < 0) throw new ArgumentOutOfRangeException(nameof(outputOffset), outputOffset, ArgumentOutOfRangeNeedNonNegNum);
            if (inputCount <= 0) throw new ArgumentOutOfRangeException(nameof(inputCount), inputCount, ArgumentOutOfRangeNeedPositiveNum);
            if (inputBuffer.Length - inputCount < inputOffset) throw new ArgumentException(ArgumentInvalidOffLen);
            if (inputCount % InputBlockSize != 0) throw new CryptographicException(CryptographicInvalidDataSize);

            EnsureKeyExpanded();

            if (_transformMode == SymmetricTransformMode.Encrypt)
                return EncryptData(inputBuffer, inputOffset, inputCount, ref outputBuffer, outputOffset, false);
            else
            {
                if (_paddingMode == PaddingMode.Zeros || _paddingMode == PaddingMode.None)
                    return DecryptData(inputBuffer, inputOffset, inputCount, ref outputBuffer, outputOffset, false);
                else
                {
                    if (_depadBuffer == null)
                    {
                        _depadBuffer = new byte[InputBlockSize];
                        // copy the last InputBlockSize bytes to _depadBuffer everything else gets processed and returned
                        int inputToProcess = inputCount - InputBlockSize;
                        BlockCopy(inputBuffer, inputOffset + inputToProcess, _depadBuffer, 0, InputBlockSize);

                        return DecryptData(inputBuffer, inputOffset, inputToProcess, ref outputBuffer, outputOffset, false);
                    }
                    else
                    {
                        // we already have a depad buffer, so we need to decrypt that info first & copy it out
                        DecryptData(_depadBuffer, 0, _depadBuffer.Length, ref outputBuffer, outputOffset, false);
                        outputOffset += OutputBlockSize;
                        int inputToProcess = inputCount - InputBlockSize;
                        BlockCopy(inputBuffer, inputOffset + inputToProcess, _depadBuffer, 0, InputBlockSize);
                        return OutputBlockSize + DecryptData(inputBuffer, inputOffset, inputToProcess, ref outputBuffer, outputOffset, false);
                    }
                }
            }
        }

        /// <summary>
        /// Computes the transformation for the specified region of the specified byte array.
        /// </summary>
        /// <param name="inputBuffer">
        /// The input to perform the operation on.
        /// </param>
        /// <param name="inputOffset">
        /// The offset into the input byte array to begin using data from.
        /// </param>
        /// <param name="inputCount">
        /// The number of bytes in the input byte array to use as data.
        /// </param>
        /// <returns>
        /// The computed transformation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="inputBuffer" /> parameter is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value of the <paramref name="inputOffset" /> parameter is negative.
        /// -or-
        /// The value of the <paramref name="inputCount" /> parameter is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The length of the input buffer is less than the sum of the input offset and the input count.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// The length of the <paramref name="inputCount" /> parameter is not evenly devisable by input block size.
        /// -or-
        /// The padding is invalid and cannot be removed.
        /// </exception>
        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            if (inputBuffer == null) throw new ArgumentNullException(nameof(inputBuffer));
            if (inputOffset < 0) throw new ArgumentOutOfRangeException(nameof(inputOffset), inputOffset, ArgumentOutOfRangeNeedNonNegNum);
            if (inputCount < 0) throw new ArgumentOutOfRangeException(nameof(inputCount), inputOffset, ArgumentOutOfRangeNeedNonNegNum);
            if (inputBuffer.Length - inputCount < inputOffset) throw new ArgumentException(ArgumentInvalidOffLen);

            EnsureKeyExpanded();

            byte[] transformedBytes = null;
            if (_transformMode == SymmetricTransformMode.Encrypt)
                EncryptData(inputBuffer, inputOffset, inputCount, ref transformedBytes, 0, true);
            else
            {
                if (inputCount % InputBlockSize != 0)
                    throw new CryptographicException(CryptographicInvalidDataSize);

                if (_depadBuffer == null)
                    DecryptData(inputBuffer, inputOffset, inputCount, ref transformedBytes, 0, true);
                else
                {
                    byte[] temp = new byte[_depadBuffer.Length + inputCount];
                    BlockCopy(_depadBuffer, 0, temp, 0, _depadBuffer.Length);
                    BlockCopy(inputBuffer, inputOffset, temp, _depadBuffer.Length, inputCount);
                    DecryptData(temp,
                                0,
                                temp.Length,
                                ref transformedBytes,
                                0,
                                true);
                }
            }
            Reset();
            return transformedBytes;
        }

        /// <summary>
        /// Releases all resources used by the current instance of the
        /// <see cref="SymmetricTransform"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="SymmetricTransform" /> class
        /// and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                EraseData(ref _rgbKey);
                EraseData(ref _rgbIV);
                EraseData(ref _depadBuffer);
                EraseData(ref _stateBuffer);
                EraseData(ref _tempBuffer);
            }
        }

        private void EnsureKeyExpanded()
        {
            if (!_keyExpanded)
            {
                GenerateKeyExpansion(_rgbKey);
                _keyExpanded = true;
            }
        }

        private int EncryptData(byte[] inputBuffer, int inputOffset, int inputCount, ref byte[] outputBuffer, int outputOffset, bool isFinalTransform)
        {
            int
                padSize = 0,
                lonelyBytes = inputCount % InputBlockSize;

            byte[] padBytes = null;

            if (isFinalTransform)
            {
                padBytes = CreatePadding(lonelyBytes);
                if (padBytes != null)
                    padSize = padBytes.Length;
            }

            if (outputBuffer == null)
            {
                outputBuffer = new byte[inputCount + padSize];
                outputOffset = 0;
            }
            else if ((outputBuffer.Length - outputOffset) < (inputCount + padSize))
                throw new CryptographicException(CryptographicInsufficientOutputBuffer);

            int shift;

            switch (_cipherMode)
            {
                case CipherMode.ECB:
                    for (shift = 0; shift < inputCount; shift += InputBlockSize)
                        EncryptBlock(inputBuffer, inputOffset + shift, outputBuffer, outputOffset + shift);
                    break;

                case CipherMode.CBC:
                    for (shift = 0; shift < inputCount; shift += InputBlockSize)
                    {
                        Xor(_stateBuffer, 0, inputBuffer, inputOffset + shift, _tempBuffer, 0, InputBlockSize);
                        EncryptBlock(_tempBuffer, 0, outputBuffer, outputOffset + shift);
                        BlockCopy(_stateBuffer, InputBlockSize, _stateBuffer, 0, _rgbIV.Length - InputBlockSize);
                        BlockCopy(outputBuffer, outputOffset + shift, _stateBuffer, _rgbIV.Length - InputBlockSize, InputBlockSize);
                    }
                    break;

#if NET45
                case CipherMode.CFB:
                    for (shift = 0; shift < inputCount; shift += InputBlockSize)
                    {
                        EncryptBlock(_stateBuffer, 0, _tempBuffer, 0);
                        Xor(_tempBuffer, 0, inputBuffer, inputOffset + shift, outputBuffer, outputOffset + shift, InputBlockSize);
                        BlockCopy(_stateBuffer, InputBlockSize, _stateBuffer, 0, _rgbIV.Length - InputBlockSize);
                        BlockCopy(outputBuffer, outputOffset + shift, _stateBuffer, _rgbIV.Length - InputBlockSize, InputBlockSize);
                    }
                    break;

                case CipherMode.OFB:
                    for (shift = 0; shift < inputCount; shift += InputBlockSize)
                    {
                        EncryptBlock(_stateBuffer, 0, _tempBuffer, 0);
                        Xor(_tempBuffer, 0, inputBuffer, inputOffset + shift, outputBuffer, outputOffset + shift, InputBlockSize);
                        BlockCopy(_stateBuffer, InputBlockSize, _stateBuffer, 0, _rgbIV.Length - InputBlockSize);
                        BlockCopy(_tempBuffer, 0, _stateBuffer, _rgbIV.Length - InputBlockSize, InputBlockSize);
                    }
                    break; 
#endif

                default:
                    throw new CryptographicException(CryptographicInvalidCipherMode);
            }

            if (padSize != 0)
                EncryptPaddedBlock(inputBuffer, inputOffset, outputBuffer, outputOffset, padSize, lonelyBytes, padBytes, shift);

            return inputCount + padSize;
        }

        private void EncryptPaddedBlock(byte[] inputBuffer, int inputOffset, byte[] outputBuffer, int outputOffset, int padSize, int lonelyBytes, byte[] padBytes, int shift)
        {
            byte[] tmpInputBuffer;

            if (padSize == InputBlockSize)
                tmpInputBuffer = padBytes;
            else
            {
                shift -= InputBlockSize;
                tmpInputBuffer = new byte[InputBlockSize];
                BlockCopy(inputBuffer, inputOffset + shift, tmpInputBuffer, 0, lonelyBytes);
                BlockCopy(padBytes, 0, tmpInputBuffer, lonelyBytes, padSize);
            }

            switch (_cipherMode)
            {
                case CipherMode.ECB:
                    EncryptBlock(tmpInputBuffer, 0, outputBuffer, outputOffset + shift);
                    break;

                case CipherMode.CBC:
                    Xor(_stateBuffer, 0, tmpInputBuffer, 0, _tempBuffer, 0, InputBlockSize);
                    EncryptBlock(_tempBuffer, 0, outputBuffer, outputOffset + shift);
                    break;

#if NET45
                case CipherMode.CFB:
                case CipherMode.OFB:
                    EncryptBlock(_stateBuffer, 0, _tempBuffer, 0);
                    Xor(_tempBuffer, 0, tmpInputBuffer, 0, outputBuffer, outputOffset + shift, InputBlockSize);
                    break; 
#endif

                default:
                    throw new CryptographicException(CryptographicInvalidCipherMode);
            }
        }

        private byte[] CreatePadding(int lonelyBytes)
        {
            int padSize = 0;
            byte[] padBytes = null;

            // check the padding mode and make sure we have enough outputBuffer to handle any padding we have to do
            switch (_paddingMode)
            {
                case PaddingMode.None:
                    if (lonelyBytes != 0)
                        throw new CryptographicException(CryptographicInvalidDataSize);
                    break;

                case PaddingMode.Zeros:
                    if (lonelyBytes != 0)
                        padSize = InputBlockSize - lonelyBytes;
                    break;

                case PaddingMode.PKCS7:
#if NET45
                case PaddingMode.ANSIX923:
                case PaddingMode.ISO10126: 
#endif
                    padSize = InputBlockSize - lonelyBytes;
                    break;
            }

            if (padSize != 0)
            {
                padBytes = new byte[padSize];

                switch (_paddingMode)
                {
                    case PaddingMode.None:
                        break;

                    case PaddingMode.Zeros:
                        // padBytes is already initialized with zeros
                        break;

                    case PaddingMode.PKCS7:
                        for (int index = 0; index < padSize; index++)
                            padBytes[index] = (byte)padSize;
                        break;

#if NET45
                    case PaddingMode.ANSIX923:
                        // padBytes is already initialized with zeros. Simply change the last byte
                        padBytes[padSize - 1] = (byte)padSize;
                        break;

                    case PaddingMode.ISO10126:
                        // generate random bytes
                        StaticRandomNumberGenerator.GetBytes(padBytes);
                        // and change the last byte
                        padBytes[padSize - 1] = (byte)padSize;
                        break; 
#endif
                }
            }

            return padBytes;
        }

        private int DecryptData(byte[] inputBuffer, int inputOffset, int inputCount, ref byte[] outputBuffer, int outputOffset, bool isFinalTransform)
        {
            if (outputBuffer == null)
            {
                outputBuffer = new byte[inputCount];
                outputOffset = 0;
            }
            else if ((outputBuffer.Length - outputOffset) < inputCount)
                throw new CryptographicException(CryptographicInsufficientOutputBuffer);

            switch (_cipherMode)
            {
                case CipherMode.ECB:
                    for (int shift = 0; shift < inputCount; shift += InputBlockSize)
                        DecryptBlock(inputBuffer, inputOffset + shift, outputBuffer, outputOffset + shift);
                    break;

                case CipherMode.CBC:
                    for (int shift = 0; shift < inputCount; shift += InputBlockSize)
                    {
                        DecryptBlock(inputBuffer, inputOffset + shift, _tempBuffer, 0);
                        Xor(_stateBuffer, 0, _tempBuffer, 0, outputBuffer, outputOffset + shift, InputBlockSize);
                        BlockCopy(_stateBuffer, InputBlockSize, _stateBuffer, 0, _rgbIV.Length - InputBlockSize);
                        BlockCopy(inputBuffer, inputOffset + shift, _stateBuffer, _rgbIV.Length - InputBlockSize, InputBlockSize);
                    }
                    break;

#if NET45
                case CipherMode.CFB:
                    for (int shift = 0; shift < inputCount; shift += InputBlockSize)
                    {
                        EncryptBlock(_stateBuffer, 0, _tempBuffer, 0);
                        Xor(_tempBuffer, 0, inputBuffer, inputOffset + shift, outputBuffer, outputOffset + shift, InputBlockSize);
                        BlockCopy(_stateBuffer, InputBlockSize, _stateBuffer, 0, _rgbIV.Length - InputBlockSize);
                        BlockCopy(inputBuffer, inputOffset + shift, _stateBuffer, _rgbIV.Length - InputBlockSize, InputBlockSize);
                    }
                    break;

                case CipherMode.OFB:
                    for (int shift = 0; shift < inputCount; shift += InputBlockSize)
                    {
                        EncryptBlock(_stateBuffer, 0, _tempBuffer, 0);
                        Xor(_tempBuffer, 0, inputBuffer, inputOffset + shift, outputBuffer, outputOffset + shift, InputBlockSize);
                        BlockCopy(_stateBuffer, InputBlockSize, _stateBuffer, 0, _rgbIV.Length - InputBlockSize);
                        BlockCopy(_tempBuffer, 0, _stateBuffer, _rgbIV.Length - InputBlockSize, InputBlockSize);
                    }
                    break; 
#endif

                default:
                    throw new CryptographicException(CryptographicInvalidCipherMode);
            }

            if (!isFinalTransform)
                return inputCount;

            // this is the last block, remove the padding.
            int padSize = 0;

            switch (_paddingMode)
            {
                case PaddingMode.None:
                case PaddingMode.Zeros:
                    break;

                case PaddingMode.PKCS7:
                    padSize = GetValidPadSize(inputCount, outputBuffer);

                    // additional check the validity of the padding
                    for (int index = 1; index <= padSize; index++)
                        if (outputBuffer[inputCount - index] != padSize)
                            throw new CryptographicException(CryptographicInvalidPadding);

                    RemovePadding(ref outputBuffer, padSize);
                    break;

#if NET45
                case PaddingMode.ANSIX923:
                    padSize = GetValidPadSize(inputCount, outputBuffer);

                    // additional check the validity of the padding
                    for (int index = 2; index <= padSize; index++)
                        if (outputBuffer[inputCount - index] != 0)
                            throw new CryptographicException(CryptographicInvalidPadding);

                    RemovePadding(ref outputBuffer, padSize);
                    break;

                case PaddingMode.ISO10126:
                    padSize = GetValidPadSize(inputCount, outputBuffer);
                    // no additional check, just ignore the random bytes
                    RemovePadding(ref outputBuffer, padSize);
                    break; 
#endif
            }

            return outputBuffer.Length;
        }

        private int GetValidPadSize(int inputCount, byte[] buffer)
        {
            int padSize;
            if (inputCount == 0)
                throw new CryptographicException(CryptographicInvalidPadding);
            padSize = buffer[inputCount - 1];
            if (padSize > buffer.Length || padSize > InputBlockSize || padSize <= 0)
                throw new CryptographicException(CryptographicInvalidPadding);
            return padSize;
        }

        private static void RemovePadding(ref byte[] buffer, int padSize)
        {
            var unpadded = new byte[buffer.Length - padSize];
            BlockCopy(buffer, 0, unpadded, 0, buffer.Length - padSize);
            buffer = unpadded;
        }
    }
}
