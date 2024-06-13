using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace GeeksCoreLibrary.Core.Helpers;

// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
//   Shared under the terms of the Microsoft Public License,
//   https://opensource.org/licenses/MS-PL
//
//   This has been copied from https://docs.microsoft.com/en-us/dotnet/standard/security/vulnerabilities-cbc-mode
//
// ==--==
public enum AeCipher : byte
{
    Unknown,
    Aes256CbcPkcs7,
}

public enum AeMac : byte
{
    Unknown,
    HMACSHA256,
    HMACSHA384,
}

/// <summary>
/// Provides extension methods to make HashAlgorithm look like .NET Core's
/// IncrementalHash
/// </summary>
internal static class IncrementalHashExtensions
{
    public static void AppendData(this HashAlgorithm hash, byte[] data)
    {
        hash.TransformBlock(data, 0, data.Length, null, 0);
    }

    public static void AppendData(this HashAlgorithm hash, byte[] data, int offset, int length)
    {
        hash.TransformBlock(data, offset, length, null, 0);
    }

    public static byte[] GetHashAndReset(this HashAlgorithm hash)
    {
        hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return hash.Hash;
    }
}

public static class CryptographyHelpers
{
    /// <summary>
    /// Use <paramref name="masterKey"/> to derive two keys (one cipher, one HMAC)
    /// to provide authenticated encryption for <paramref name="message"/>.
    /// </summary>
    /// <param name="masterKey">The master key from which other keys derive.</param>
    /// <param name="message">The message to encrypt</param>
    /// <returns>
    /// A concatenation of
    /// [cipher algorithm+chainmode+padding][mac algorithm][authtag][IV][ciphertext],
    /// suitable to be passed to <see cref="Decrypt"/>.
    /// </returns>
    /// <remarks>
    /// <paramref name="masterKey"/> should be a 128-bit (or bigger) value generated
    /// by a secure random number generator, such as the one returned from
    /// <see cref="RandomNumberGenerator.Create()"/>.
    /// This implementation chooses to block deficient inputs by length, but does not
    /// make any attempt at discerning the randomness of the key.
    ///
    /// If the master key is being input by a prompt (like a password/passphrase)
    /// then it should be properly turned into keying material via a Key Derivation
    /// Function like PBKDF2, represented by Rfc2898DeriveBytes. A 'password' should
    /// never be simply turned to bytes via an Encoding class and used as a key.
    /// </remarks>
    public static byte[] Encrypt(byte[] masterKey, byte[] message)
    {
        if (masterKey == null)
        {
            throw new ArgumentNullException(nameof(masterKey));
        }

        if (masterKey.Length < 16)
        {
            throw new ArgumentOutOfRangeException(nameof(masterKey), "Master Key must be at least 128 bits (16 bytes)");
        }

        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        // First, choose an encryption scheme.
        var aeCipher = AeCipher.Aes256CbcPkcs7;

        // Second, choose an authentication (message integrity) scheme.
        //
        // In this example we use the master key length to change from HMACSHA256 to
        // HMACSHA384, but that is completely arbitrary. This mostly represents a
        // "cryptographic needs change over time" scenario.
        var aeMac = masterKey.Length < 48 ? AeMac.HMACSHA256 : AeMac.HMACSHA384;

        // It's good to be able to identify what choices were made when a message was
        // encrypted, so that the message can later be decrypted. This allows for
        // future versions to add support for new encryption schemes, but still be
        // able to read old data. A practice known as "cryptographic agility".
        //
        // This is similar in practice to PKCS#7 messaging, but this uses a
        // private-scoped byte rather than a public-scoped Object IDentifier (OID).
        // Please note that the scheme in this example adheres to no particular
        // standard, and is unlikely to survive to a more complete implementation in
        // the .NET Framework.
        //
        // You may be well-served by prepending a version number byte to this
        // message, but may want to avoid the value 0x30 (the leading byte value for
        // DER-encoded structures such as X.509 certificates and PKCS#7 messages).
        byte[] algorithmChoices = { (byte)aeCipher, (byte)aeMac };
        byte[] iv;
        byte[] cipherText;
        byte[] tag;

        // Using our algorithm choices, open an HMAC (as an authentication tag
        // generator) and a SymmetricAlgorithm which use different keys each derived
        // from the same master key.
        //
        // A custom implementation may very well have distinctly managed secret keys
        // for the MAC and cipher, this example merely demonstrates the master to
        // derived key methodology to encourage key separation from the MAC and
        // cipher keys.
        using (var tagGenerator = GetMac(aeMac, masterKey))
        {
            using (var cipher = GetCipher(aeCipher, masterKey))
            using (var encryptor = cipher.CreateEncryptor())
            {
                // Since no IV was provided, a random one has been generated
                // during the call to CreateEncryptor.
                //
                // But note that it only does the auto-generation once. If the cipher
                // object were used again, a call to GenerateIV would have been
                // required.
                iv = cipher.IV;

                cipherText = Transform(encryptor, message, 0, message.Length);
            }

            // The IV and ciphertest both need to be included in the MAC to prevent
            // tampering.
            //
            // By including the algorithm identifiers, we have technically moved from
            // simple Authenticated Encryption (AE) to Authenticated Encryption with
            // Additional Data (AEAD). By including the algorithm identifiers in the
            // MAC, it becomes harder for an attacker to change them as an attempt to
            // perform a downgrade attack.
            //
            // If you've added a data format version field, it can also be included
            // in the MAC to further inhibit an attacker's options for confusing the
            // data processor into believing the tampered message is valid.
            tagGenerator.AppendData(algorithmChoices);
            tagGenerator.AppendData(iv);
            tagGenerator.AppendData(cipherText);
            tag = tagGenerator.GetHashAndReset();
        }

        // Build the final result as the concatenation of everything except the keys.
        var totalLength =
            algorithmChoices.Length +
            tag.Length +
            iv.Length +
            cipherText.Length;

        var output = new byte[totalLength];
        var outputOffset = 0;

        Append(algorithmChoices, output, ref outputOffset);
        Append(tag, output, ref outputOffset);
        Append(iv, output, ref outputOffset);
        Append(cipherText, output, ref outputOffset);

        Debug.Assert(outputOffset == output.Length);
        return output;
    }

    /// <summary>
    /// Reads a message produced by <see cref="Encrypt"/> after verifying it hasn't
    /// been tampered with.
    /// </summary>
    /// <param name="masterKey">The master key from which other keys derive.</param>
    /// <param name="cipherText">
    /// The output of <see cref="Encrypt"/>: a concatenation of a cipher ID, mac ID,
    /// authTag, IV, and cipherText.
    /// </param>
    /// <returns>The decrypted content.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="masterKey"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="cipherText"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="cipherText"/> identifies unknown algorithms, is not long
    /// enough, fails a data integrity check, or fails to decrypt.
    /// </exception>
    /// <remarks>
    /// <paramref name="masterKey"/> should be a 128-bit (or larger) value
    /// generated by a secure random number generator, such as the one returned from
    /// <see cref="RandomNumberGenerator.Create()"/>. This implementation chooses to
    /// block deficient inputs by length, but doesn't make any attempt at
    /// discerning the randomness of the key.
    ///
    /// If the master key is being input by a prompt (like a password/passphrase),
    /// then it should be properly turned into keying material via a Key Derivation
    /// Function like PBKDF2, represented by Rfc2898DeriveBytes. A 'password' should
    /// never be simply turned to bytes via an Encoding class and used as a key.
    /// </remarks>
    public static byte[] Decrypt(byte[] masterKey, byte[] cipherText)
    {
        // This example continues the .NET practice of throwing exceptions for
        // failures. If you consider message tampering to be normal (and thus
        // "not exceptional") behavior, you may like the signature
        // bool Decrypt(byte[] messageKey, byte[] cipherText, out byte[] message)
        // better.
        if (masterKey == null)
        {
            throw new ArgumentNullException(nameof(masterKey));
        }

        if (masterKey.Length < 16)
        {
            throw new ArgumentOutOfRangeException(nameof(masterKey), "Master Key must be at least 128 bits (16 bytes)");
        }

        if (cipherText == null)
        {
            throw new ArgumentNullException(nameof(cipherText));
        }

        // The format of this message is assumed to be public, so there's no harm in
        // saying ahead of time that the message makes no sense.
        if (cipherText.Length < 2)
        {
            throw new CryptographicException();
        }

        // Use the message algorithm headers to determine what cipher algorithm and
        // MAC algorithm are going to be used. Since the same Key Derivation
        // Functions (KDFs) are being used in Decrypt as Encrypt, the keys are also
        // the same.
        var aeCipher = (AeCipher)cipherText[0];
        var aeMac = (AeMac)cipherText[1];

        using (var cipher = GetCipher(aeCipher, masterKey))
        using (var tagGenerator = GetMac(aeMac, masterKey))
        {
            var blockSizeInBytes = cipher.BlockSize / 8;
            var tagSizeInBytes = tagGenerator.HashSize / 8;
            var headerSizeInBytes = 2;
            var tagOffset = headerSizeInBytes;
            var ivOffset = tagOffset + tagSizeInBytes;
            var cipherTextOffset = ivOffset + blockSizeInBytes;
            var cipherTextLength = cipherText.Length - cipherTextOffset;
            var minLen = cipherTextOffset + blockSizeInBytes;

            // Again, the minimum length is still assumed to be public knowledge,
            // nothing has leaked out yet. The minimum length couldn't just be calculated
            // without reading the header.
            if (cipherText.Length < minLen)
            {
                throw new CryptographicException();
            }

            // It's very important that the MAC be calculated and verified before
            // proceeding to decrypt the ciphertext, as this prevents any sort of
            // information leaking out to an attacker.
            //
            // Don't include the tag in the calculation, though.

            // First, everything before the tag (the cipher and MAC algorithm ids)
            tagGenerator.AppendData(cipherText, 0, tagOffset);

            // Skip the data before the tag and the tag, then read everything that
            // remains.
            tagGenerator.AppendData(
                cipherText,
                tagOffset + tagSizeInBytes,
                cipherText.Length - tagSizeInBytes - tagOffset);

            byte[] generatedTag = tagGenerator.GetHashAndReset();

            // The time it took to get to this point has so far been a function only
            // of the length of the data, or of non-encrypted values (e.g. it could
            // take longer to prepare the *key* for the HMACSHA384 MAC than the
            // HMACSHA256 MAC, but the algorithm choice wasn't a secret).
            //
            // If the verification of the authentication tag aborts as soon as a
            // difference is found in the byte arrays then your program may be
            // acting as a timing oracle which helps an attacker to brute-force the
            // right answer for the MAC. So, it's very important that every possible
            // "no" (2^256-1 of them for HMACSHA256) be evaluated in as close to the
            // same amount of time as possible. For this, we call CryptographicEquals
            if (!CryptographicEquals(
                    generatedTag,
                    0,
                    cipherText,
                    tagOffset,
                    tagSizeInBytes))
            {
                // Assuming every tampered message (of the same length) took the same
                // amount of time to process, we can now safely say
                // "this data makes no sense" without giving anything away.
                throw new CryptographicException();
            }

            // Restore the IV into the symmetricAlgorithm instance.
            var iv = new byte[blockSizeInBytes];
            Buffer.BlockCopy(cipherText, ivOffset, iv, 0, iv.Length);
            cipher.IV = iv;

            using (var decryptor = cipher.CreateDecryptor())
            {
                return Transform(
                    decryptor,
                    cipherText,
                    cipherTextOffset,
                    cipherTextLength);
            }
        }
    }

    private static byte[] Transform(ICryptoTransform transform, byte[] input, int inputOffset, int inputLength)
    {
        // Many of the implementations of ICryptoTransform report true for
        // CanTransformMultipleBlocks, and when the entire message is available in
        // one shot this saves on the allocation of the CryptoStream and the
        // intermediate structures it needs to properly chunk the message into blocks
        // (since the underlying stream won't always return the number of bytes
        // needed).
        if (transform.CanTransformMultipleBlocks)
        {
            return transform.TransformFinalBlock(input, inputOffset, inputLength);
        }

        // If our transform couldn't do multiple blocks at once, let CryptoStream
        // handle the chunking.
        using (var messageStream = new MemoryStream())
        using (var cryptoStream = new CryptoStream(messageStream, transform, CryptoStreamMode.Write))
        {
            cryptoStream.Write(input, inputOffset, inputLength);
            cryptoStream.FlushFinalBlock();
            return messageStream.ToArray();
        }
    }

    /// <summary>
    /// Open a properly configured <see cref="SymmetricAlgorithm"/> conforming to the
    /// scheme identified by <paramref name="aeCipher"/>.
    /// </summary>
    /// <param name="aeCipher">The cipher mode to open.</param>
    /// <param name="masterKey">The master key from which other keys derive.</param>
    /// <returns>
    /// A SymmetricAlgorithm object with the right key, cipher mode, and padding
    /// mode; or <c>null</c> on unknown algorithms.
    /// </returns>
    private static SymmetricAlgorithm GetCipher(AeCipher aeCipher, byte[] masterKey)
    {
        SymmetricAlgorithm symmetricAlgorithm;

        switch (aeCipher)
        {
            case AeCipher.Aes256CbcPkcs7:
                symmetricAlgorithm = Aes.Create();
                // While 256-bit, CBC, and PKCS7 are all the default values for these
                // properties, being explicit helps comprehension more than it hurts
                // performance.
                symmetricAlgorithm.KeySize = 256;
                symmetricAlgorithm.Mode = CipherMode.CBC;
                symmetricAlgorithm.Padding = PaddingMode.PKCS7;
                break;
            default:
                // An algorithm we don't understand
                throw new CryptographicException();
        }

        // Instead of using the master key directly, derive a key for our chosen
        // HMAC algorithm based upon the master key.
        //
        // Since none of the symmetric encryption algorithms currently in .NET
        // support key sizes greater than 256-bit, we can use HMACSHA256 with
        // NIST SP 800-108 5.1 (Counter Mode KDF) to derive a value that is
        // no smaller than the key size, then Array.Resize to trim it down as
        // needed.

        using (HMAC hmac = new HMACSHA256(masterKey))
        {
            // i=1, Label=ASCII(cipher)
            var cipherKey = hmac.ComputeHash(new byte[] { 1, 99, 105, 112, 104, 101, 114 });

            // Resize the array to the desired keysize. KeySize is in bits,
            // and Array.Resize wants the length in bytes.
            Array.Resize(ref cipherKey, symmetricAlgorithm.KeySize / 8);

            symmetricAlgorithm.Key = cipherKey;
        }

        return symmetricAlgorithm;
    }

    /// <summary>
    /// Open a properly configured <see cref="HMAC"/> conforming to the scheme
    /// identified by <paramref name="aeMac"/>.
    /// </summary>
    /// <param name="aeMac">The message authentication mode to open.</param>
    /// <param name="masterKey">The master key from which other keys derive.</param>
    /// <returns>
    /// An HMAC object with the proper key, or <c>null</c> on unknown algorithms.
    /// </returns>
    private static HMAC GetMac(AeMac aeMac, byte[] masterKey)
    {
        HMAC hmac;

        switch (aeMac)
        {
            case AeMac.HMACSHA256:
                hmac = new HMACSHA256();
                break;
            case AeMac.HMACSHA384:
                hmac = new HMACSHA384();
                break;
            default:
                // An algorithm we don't understand
                throw new CryptographicException();
        }

        // Instead of using the master key directly, derive a key for our chosen
        // HMAC algorithm based upon the master key.
        // Since the output size of the HMAC is the same as the ideal key size for
        // the HMAC, we can use the master key over a fixed input once to perform
        // NIST SP 800-108 5.1 (Counter Mode KDF):
        hmac.Key = masterKey;

        // i=1, Context=ASCII(MAC)
        var newKey = hmac.ComputeHash(new byte[] { 1, 77, 65, 67 });

        hmac.Key = newKey;
        return hmac;
    }

    // A simple helper method to ensure that the offset (writePos) always moves
    // forward with new data.
    private static void Append(byte[] newData, byte[] combinedData, ref int writePos)
    {
        Buffer.BlockCopy(newData, 0, combinedData, writePos, newData.Length);
        writePos += newData.Length;
    }

    /// <summary>
    /// Compare the contents of two arrays in an amount of time which is only
    /// dependent on <paramref name="length"/>.
    /// </summary>
    /// <param name="a">An array to compare to <paramref name="b"/>.</param>
    /// <param name="aOffset">
    /// The starting position within <paramref name="a"/> for comparison.
    /// </param>
    /// <param name="b">An array to compare to <paramref name="a"/>.</param>
    /// <param name="bOffset">
    /// The starting position within <paramref name="b"/> for comparison.
    /// </param>
    /// <param name="length">
    /// The number of bytes to compare between <paramref name="a"/> and
    /// <paramref name="b"/>.</param>
    /// <returns>
    /// <c>true</c> if both <paramref name="a"/> and <paramref name="b"/> have
    /// sufficient length for the comparison and all of the applicable values are the
    /// same in both arrays; <c>false</c> otherwise.
    /// </returns>
    /// <remarks>
    /// An "insufficient data" <c>false</c> response can happen early, but otherwise
    /// a <c>true</c> or <c>false</c> response take the same amount of time.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static bool CryptographicEquals(byte[] a, int aOffset, byte[] b, int bOffset, int length)
    {
        Debug.Assert(a != null);
        Debug.Assert(b != null);
        Debug.Assert(length >= 0);

        var result = 0;

        if (a.Length - aOffset < length || b.Length - bOffset < length)
        {
            return false;
        }

        unchecked
        {
            for (var i = 0; i < length; i++)
            {
                // Bitwise-OR of subtraction has been found to have the most
                // stable execution time.
                //
                // This cannot overflow because bytes are 1 byte in length, and
                // result is 4 bytes.
                // The OR propagates all set bytes, so the differences are only
                // present in the lowest byte.
                result = result | (a[i + aOffset] - b[i + bOffset]);
            }
        }

        return result == 0;
    }
}