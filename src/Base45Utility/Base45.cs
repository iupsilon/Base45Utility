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
            FromBase45 = new int[256];
            for (int i = 0; i < FromBase45.Length; i++) FromBase45[i] = -1;
            for (int i = 0; i < Base45Digits.Length; i++)
            {
                FromBase45[Base45Digits[i]] = i;
            }
        }

        /// <summary>
        /// Computes base45 decoding table (thread safe)
        /// </summary>
        /// <returns></returns>
        public string Encode(string src)
        {
            if (src is null) throw new ArgumentNullException(nameof(src));
            return Encode(System.Text.Encoding.UTF8.GetBytes(src));
        }

        /// <summary>
        /// Encode input string in Base45
        /// </summary>
        /// <param name="src">Input string, utf8 encoded</param>
        /// <returns>utf8 Base45 encoded string</returns>
        public string Encode(byte[] src)
        {
            if (src is null) throw new ArgumentNullException(nameof(src));

            int wholeChunkCount = src.Length / ChunkSize;
            int remainder = src.Length % ChunkSize;
            char[] result = new char[wholeChunkCount * EncodedChunkSize + (remainder == 1 ? SmallEncodedChunkSize : 0)];

            int ri = 0;
            int i = 0;
            while (i + 1 < src.Length)
            {
                int value = (src[i++] * ByteSize) + src[i++]; // bytes are 0..255
                result[ri++] = Base45Digits[value % BaseSize];
                result[ri++] = Base45Digits[(value / BaseSize) % BaseSize];
                result[ri++] = Base45Digits[(value / (BaseSize * BaseSize)) % BaseSize];
            }

            if (remainder == 1)
            {
                int b = src[src.Length - 1];
                result[ri++] = Base45Digits[b % BaseSize];
                result[ri] = (b < BaseSize) ? Base45Digits[0] : Base45Digits[(b / BaseSize) % BaseSize];
            }

            return new string(result);
        }

        /// <summary>
        /// Decode encoded Base45 input string to byte array 
        /// </summary>
        /// <param name="src">input byte array</param>
        /// <returns>decoded byte array</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public byte[] Decode(string src)
        {
            if (src is null) throw new ArgumentNullException(nameof(src));
            int len = src.Length;
            int remainderSize = len % EncodedChunkSize;

            int[] buffer = new int[len];
            for (int i = 0; i < len; i++)
            {
                int idx = src[i];
                if (idx >= FromBase45.Length || FromBase45[idx] == -1)
                    throw new InvalidOperationException("Wrong input string");
                buffer[i] = FromBase45[idx];
            }

            int wholeChunkCount = len / EncodedChunkSize;
            byte[] result = new byte[wholeChunkCount * ChunkSize + (remainderSize == ChunkSize ? 1 : 0)];

            int ri = 0;
            int bi = 0;
            for (; bi < wholeChunkCount * EncodedChunkSize; bi += EncodedChunkSize)
            {
                int val = buffer[bi] + BaseSize * buffer[bi + 1] + BaseSize * BaseSize * buffer[bi + 2];
                if (val > 0xFFFF) throw new InvalidOperationException("Wrong input string");
                result[ri++] = (byte)(val / ByteSize);
                result[ri++] = (byte)(val % ByteSize);
            }

            if (remainderSize == ChunkSize)
            {
                int last = buffer[len - 2] + BaseSize * buffer[len - 1];
                result[ri] = (byte)last;
            }

            return result;
        }

        /// <summary>
        /// Decode encoded Base45 input string to a utf8 string 
        /// </summary>
        /// <param name="src">utf8 encoded Base45 string</param>
        /// <returns>utf8 decoded string</returns>
        public string DecodeAsString(string src)
        {
            var bytes = Decode(src);
            return System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }
    }
}