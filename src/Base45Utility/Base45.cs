/*
 Copyright 2021 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
 Modifications copyright (C) 2021 Yari Melzani

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
 */

using System;

namespace Base45Utility
{
    /// <summary>
    /// Base45 encoding and decoding utility
    /// https://datatracker.ietf.org/doc/html/draft-faltstrom-base45-03
    /// </summary>
    public class Base45
    {
        private const int BaseSize = 45;
        private const int ChunkSize = 2;
        private const int EncodedChunkSize = 3;
        private const int SmallEncodedChunkSize = 2;
        private const int ByteSize = 256;

        private static readonly char[] Base45Digits =
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C',
            'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
            'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', ' ', '$', '%',
            '*', '+', '-', '.', '/', ':'
        };

        private static readonly int[] FromBase45;

        static Base45()
        {
            FromBase45 = new int[ByteSize];
            for (int i = 0; i < FromBase45.Length; i++) FromBase45[i] = -1;
            for (int i = 0; i < Base45Digits.Length; i++)
            {
                FromBase45[Base45Digits[i]] = i;
            }
        }

        /// <summary>
        /// Encode a string (treated as UTF-8) in Base45.
        /// </summary>
        /// <param name="src">Input string</param>
        /// <returns>Base45 encoded string</returns>
        public string Encode(string src)
        {
            if (src is null) throw new ArgumentNullException(nameof(src));
            return Encode(System.Text.Encoding.UTF8.GetBytes(src));
        }

        /// <summary>
        /// Encode a byte array in Base45.
        /// </summary>
        /// <param name="src">Input bytes</param>
        /// <returns>Base45 encoded string</returns>
        public string Encode(byte[] src)
        {
            if (src is null) throw new ArgumentNullException(nameof(src));

            int outLen = ComputeEncodedLength(src.Length);
            if (outLen == 0) return string.Empty;

#if NET8_0_OR_GREATER
            // string.Create writes straight into the final string buffer (no intermediate copy).
            return string.Create(outLen, src, static (dst, state) => EncodeInto(state, dst));
#else
            char[] result = new char[outLen];
            EncodeInto(src, result);
            return new string(result);
#endif
        }

#if NET8_0_OR_GREATER
        /// <summary>
        /// Encode a span of bytes in Base45 without copying the input.
        /// </summary>
        /// <param name="src">Input bytes</param>
        /// <returns>Base45 encoded string</returns>
        public string Encode(ReadOnlySpan<byte> src)
        {
            int outLen = ComputeEncodedLength(src.Length);
            if (outLen == 0) return string.Empty;

            char[] rented = System.Buffers.ArrayPool<char>.Shared.Rent(outLen);
            try
            {
                EncodeInto(src, rented.AsSpan(0, outLen));
                return new string(rented, 0, outLen);
            }
            finally
            {
                System.Buffers.ArrayPool<char>.Shared.Return(rented);
            }
        }
#endif

        /// <summary>
        /// Decode a Base45 string into a byte array.
        /// </summary>
        /// <param name="src">Base45 encoded string</param>
        /// <returns>Decoded bytes</returns>
        /// <exception cref="ArgumentNullException"><paramref name="src"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The input is not valid Base45 (bad length, illegal character or out-of-range group).</exception>
        public byte[] Decode(string src)
        {
            if (src is null) throw new ArgumentNullException(nameof(src));

            int outLen = ComputeDecodedLength(src.Length);
            byte[] result = new byte[outLen];
#if NET8_0_OR_GREATER
            DecodeInto(src.AsSpan(), result);
#else
            DecodeInto(src, result);
#endif
            return result;
        }

#if NET8_0_OR_GREATER
        /// <summary>
        /// Decode a Base45 char span into a caller-provided buffer.
        /// </summary>
        /// <param name="src">Base45 encoded characters</param>
        /// <param name="destination">Buffer that receives the decoded bytes</param>
        /// <returns>Number of bytes written to <paramref name="destination"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is too small.</exception>
        /// <exception cref="InvalidOperationException">The input is not valid Base45.</exception>
        public int Decode(ReadOnlySpan<char> src, Span<byte> destination)
        {
            int outLen = ComputeDecodedLength(src.Length);
            if (destination.Length < outLen)
                throw new ArgumentException("Destination buffer is too small", nameof(destination));

            DecodeInto(src, destination.Slice(0, outLen));
            return outLen;
        }
#endif

        /// <summary>
        /// Decode a Base45 string into a UTF-8 string.
        /// </summary>
        /// <param name="src">Base45 encoded string</param>
        /// <returns>Decoded UTF-8 string</returns>
        public string DecodeAsString(string src)
        {
            byte[] bytes = Decode(src);
            return System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        private static int ComputeEncodedLength(int byteCount)
        {
            int wholeChunkCount = byteCount / ChunkSize;
            int remainder = byteCount % ChunkSize;
            return wholeChunkCount * EncodedChunkSize + (remainder == 1 ? SmallEncodedChunkSize : 0);
        }

        private static int ComputeDecodedLength(int charCount)
        {
            int remainderSize = charCount % EncodedChunkSize;
            // Valid Base45 strings have length 3n or 3n+2; a leftover of one char is impossible.
            if (remainderSize == 1)
                throw new InvalidOperationException("Wrong input length");

            return (charCount / EncodedChunkSize) * ChunkSize + (remainderSize == ChunkSize ? 1 : 0);
        }

#if NET8_0_OR_GREATER
        private static void EncodeInto(ReadOnlySpan<byte> src, Span<char> result)
#else
        private static void EncodeInto(byte[] src, char[] result)
#endif
        {
            int n = src.Length;
            int ri = 0;
            int i = 0;
            while (i + 1 < n)
            {
                int value = (src[i++] * ByteSize) + src[i++]; // bytes are 0..255
                result[ri++] = Base45Digits[value % BaseSize];
                result[ri++] = Base45Digits[(value / BaseSize) % BaseSize];
                result[ri++] = Base45Digits[(value / (BaseSize * BaseSize)) % BaseSize];
            }

            if ((n & 1) == 1)
            {
                int b = src[n - 1];
                result[ri++] = Base45Digits[b % BaseSize];
                result[ri] = Base45Digits[(b / BaseSize) % BaseSize]; // b < 45 already yields '0'
            }
        }

#if NET8_0_OR_GREATER
        private static void DecodeInto(ReadOnlySpan<char> src, Span<byte> result)
#else
        private static void DecodeInto(string src, byte[] result)
#endif
        {
            int len = src.Length;
            int wholeChunkCount = len / EncodedChunkSize;

            int ri = 0;
            int bi = 0;
            for (int c = 0; c < wholeChunkCount; c++)
            {
                int val = Lookup(src[bi++]) + BaseSize * Lookup(src[bi++]) + BaseSize * BaseSize * Lookup(src[bi++]);
                if (val > 0xFFFF) throw new InvalidOperationException("Wrong input string");
                result[ri++] = (byte)(val / ByteSize);
                result[ri++] = (byte)(val % ByteSize);
            }

            if (len % EncodedChunkSize == ChunkSize)
            {
                int last = Lookup(src[bi++]) + BaseSize * Lookup(src[bi]);
                if (last > 0xFF) throw new InvalidOperationException("Wrong input string");
                result[ri] = (byte)last;
            }
        }

        private static int Lookup(char ch)
        {
            if (ch >= ByteSize || FromBase45[ch] == -1)
                throw new InvalidOperationException("Wrong input string");
            return FromBase45[ch];
        }
    }
}
