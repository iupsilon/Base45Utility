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
using System.Linq;

namespace Base45Utility
{
    /// <summary>
    /// Base45 encoding and decoding utility
    /// https://datatracker.ietf.org/doc/html/draft-faltstrom-base45-03
    /// </summary>
    public class Base45
    {
        const int BaseSize = 45;
        const int ChunkSize = 2;
        const int EncodedChunkSize = 3;
        const int SmallEncodedChunkSize = 2;
        const int ByteSize = 256;

        static readonly char[] Base45Digits =
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C',
            'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
            'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', ' ', '$', '%',
            '*', '+', '-', '.', '/', ':'
        };

        /// <summary>
        /// Synchronization object
        /// </summary>
        private static readonly object SyncRoot = new object();
        private static int[] _internalFromBase45;

        /// <summary>
        /// Computes base45 decoding table (thread safe)
        /// </summary>
        /// <returns></returns>
        static int[] GetFromBase45()
        {
            if (_internalFromBase45 == null)
            {
                lock (SyncRoot)
                {
                    if (_internalFromBase45 == null)
                    {
                        int[] localFromBase45 = Enumerable.Repeat(-1, 256).ToArray();

                        for (int i = 0; i < Base45Digits.Length; i++)
                        {
                            localFromBase45[Base45Digits[i]] = i;
                        }

                        _internalFromBase45 = localFromBase45;
                    }
                }
            }

            return _internalFromBase45;
        }

        #region Encode

        /// <summary>
        /// Encode input string in Base45
        /// </summary>
        /// <param name="src">Input string, utf8 encoded</param>
        /// <returns>utf8 Base45 encoded string</returns>
        public string Encode(string src)
        {
            var srcBytes = System.Text.Encoding.UTF8.GetBytes(src);
            var result = Encode(srcBytes);
            return result;
        }

        /// <summary>
        /// Encode input byte array in Base45
        /// </summary>
        /// <param name="src">Input byte[]</param>
        /// <returns>utf8 Base45 encoded string</returns>
        public string Encode(byte[] src)
        {
            int wholeChunkCount = src.Length / ChunkSize;
            char[] resultChars = new char[wholeChunkCount * EncodedChunkSize + (src.Length % ChunkSize == 1 ? SmallEncodedChunkSize : 0)];

            int resultIndex = 0;
            int wholeChunkLength = wholeChunkCount * ChunkSize;
            for (int i = 0; i < wholeChunkLength;)
            {
                int value = (src[i++] & 0xff) * ByteSize + (src[i++] & 0xff);
                resultChars[resultIndex++] = Base45Digits[value % BaseSize];
                resultChars[resultIndex++] = Base45Digits[(value / BaseSize) % BaseSize];
                resultChars[resultIndex++] = Base45Digits[(value / (BaseSize * BaseSize)) % BaseSize];
            }

            if (src.Length % ChunkSize != 0)
            {
                resultChars[resultChars.Length - 2] = Base45Digits[(src[src.Length - 1] & 0xff) % BaseSize];
                resultChars[resultChars.Length - 1] = (src[src.Length - 1] & 0xff) < BaseSize
                    ? Base45Digits[0]
                    : Base45Digits[(src[src.Length - 1] & 0xff) / BaseSize % BaseSize];
            }


            var result = new string(resultChars);
            return result;
        }

        #endregion

        #region Decode

        /// <summary>
        /// Decode encoded Base45 input string to byte array 
        /// </summary>
        /// <param name="src">input byte array</param>
        /// <returns>decoded byte array</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public byte[] Decode(string src)
        {
            int remainderSize = src.Length % EncodedChunkSize;

            int[] buffer = new int[src.Length];
            var fromBase45 = GetFromBase45();
            for (int i = 0; i < src.Length; ++i)
            {
                buffer[i] = fromBase45[src[i]];
                if (buffer[i] == -1)
                {
                    throw new InvalidOperationException("Wrong input string");
                }
            }

            int wholeChunkCount = buffer.Length / EncodedChunkSize;
            byte[] result = new byte[wholeChunkCount * ChunkSize + (remainderSize == ChunkSize ? 1 : 0)];
            int resultIndex = 0;
            int wholeChunkLength = wholeChunkCount * EncodedChunkSize;
            for (int i = 0; i < wholeChunkLength;)
            {
                int val = buffer[i++] + BaseSize * buffer[i++] + BaseSize * BaseSize * buffer[i++];
                if (val > 0xFFFF)
                {
                    throw new InvalidOperationException("Wrong input string");
                }

                result[resultIndex++] = (byte) (val / ByteSize);
                result[resultIndex++] = (byte) (val % ByteSize);
            }

            if (remainderSize != 0)
            {
                result[resultIndex] = (byte) (buffer[buffer.Length - 2] + BaseSize * buffer[buffer.Length - 1]);
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
            var decodedBytes = Decode(src);
            var decodedString = System.Text.Encoding.UTF8.GetString(decodedBytes, 0, decodedBytes.Length);
            return decodedString;
        }

        #endregion
    }
}