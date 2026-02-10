using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CharCommon
{
    public static class ByteStringConverter
    {
        private const string Z85Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ.-:+=^!/*?&<>()[]{}@%$#";

        private static readonly int[] s_decodeMap = CreateDecodeMap();

        public static string ToZ85String(byte[] data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));

            if (data.Length == 0)
                return "0|";

            string z85 = EncodeZ85(data);
            return data.Length + "|" + z85;
        }

        public static byte[] FromZ85String(string s)
        {
            if (s is null) throw new ArgumentNullException(nameof(s));
            if (s.Length == 0) return Array.Empty<byte>();

            var sepIndex = s.IndexOf('|');
            if (sepIndex <= 0) throw new FormatException("Invalid format: missing length prefix.");

            if (!int.TryParse(s.Substring(0, sepIndex), out var length) || length < 0)
                throw new FormatException("Invalid length prefix.");

            var payload = s.Substring(sepIndex + 1);
            if (length == 0) return Array.Empty<byte>();

            var decoded = DecodeZ85(payload);
            if (decoded.Length < length)
                throw new FormatException("Decoded data shorter than expected length.");

            if (decoded.Length == length) return decoded;
            var result = new byte[length];
            Array.Copy(decoded, 0, result, 0, length);
            return result;
        }

        private static string EncodeZ85(byte[] data)
        {
            int pad = (4 - (data.Length % 4)) % 4;
            int total = data.Length + pad;
            var dst = new StringBuilder((total / 4) * 5);

            var buf = new byte[total];
            Array.Copy(data, buf, data.Length);

            for (int i = 0; i < total; i += 4)
            {
                uint value = ((uint)buf[i] << 24) | ((uint)buf[i + 1] << 16) | ((uint)buf[i + 2] << 8) | buf[i + 3];
                var block = new char[5];
                for (int j = 4; j >= 0; j--)
                {
                    block[j] = Z85Alphabet[(int)(value % 85)];
                    value /= 85;
                }
                dst.Append(block);
            }

            return dst.ToString();
        }

        private static byte[] DecodeZ85(string s)
        {
            if (string.IsNullOrEmpty(s)) return Array.Empty<byte>();
            if (s.Length % 5 != 0) throw new FormatException("Invalid Z85 payload length.");

            var outBytes = new List<byte>(s.Length / 5 * 4);

            for (int i = 0; i < s.Length; i += 5)
            {
                ulong value = 0;
                for (int j = 0; j < 5; j++)
                {
                    char c = s[i + j];
                    int v = c < 256 ? s_decodeMap[c] : -1;
                    if (v < 0) throw new FormatException($"Invalid character '{c}' in Z85 payload.");
                    value = value * 85 + (uint)v;
                }

                outBytes.Add((byte)((value >> 24) & 0xFF));
                outBytes.Add((byte)((value >> 16) & 0xFF));
                outBytes.Add((byte)((value >> 8) & 0xFF));
                outBytes.Add((byte)(value & 0xFF));
            }

            return outBytes.ToArray();
        }

        private static int[] CreateDecodeMap()
        {
            var map = Enumerable.Repeat(-1, 256).ToArray();
            for (int i = 0; i < Z85Alphabet.Length; i++)
            {
                map[(int)Z85Alphabet[i]] = i;
            }
            return map;
        }
    }
}