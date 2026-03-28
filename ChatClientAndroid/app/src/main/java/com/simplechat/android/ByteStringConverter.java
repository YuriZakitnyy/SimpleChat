package com.simplechat.android;

public class ByteStringConverter {
    private static final char[] Z85_ENCODER =
            "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ.-:+=^!/*?&<>()[]{}@%$#".toCharArray();
    private static final int[] Z85_DECODER = new int[256];
    static {
        for (int i = 0; i < Z85_DECODER.length; i++) Z85_DECODER[i] = -1;
        for (int i = 0; i < Z85_ENCODER.length; i++) Z85_DECODER[Z85_ENCODER[i]] = i;
    }

    // Encodes a byte array to a Z85 string
    public static String toZ85String(byte[] data) {
        if (data == null) throw new IllegalArgumentException("data is null");
        if (data.length == 0) return "0|";
        String z85 = encodeZ85(data);
        return data.length + "|" + z85;
    }

    // Decodes a Z85 string to a byte array
    public static byte[] fromZ85String(String s) {
        if (s == null) throw new IllegalArgumentException("input is null");
        if (s.length() == 0) return new byte[0];
        int sepIndex = s.indexOf('|');
        if (sepIndex <= 0) throw new IllegalArgumentException("Invalid format: missing length prefix.");
        int length = Integer.parseInt(s.substring(0, sepIndex));
        if (length < 0) throw new IllegalArgumentException("Invalid length prefix.");
        String payload = s.substring(sepIndex + 1);
        if (length == 0) return new byte[0];
        byte[] decoded = decodeZ85(payload);
        if (decoded.length < length) throw new IllegalArgumentException("Decoded data shorter than expected length.");
        if (decoded.length == length) return decoded;
        byte[] result = new byte[length];
        System.arraycopy(decoded, 0, result, 0, length);
        return result;
    }

    private static String encodeZ85(byte[] data) {
        int pad = (4 - (data.length % 4)) % 4;
        int total = data.length + pad;
        StringBuilder sb = new StringBuilder((total / 4) * 5);
        byte[] buf = new byte[total];
        System.arraycopy(data, 0, buf, 0, data.length);
        for (int i = 0; i < total; i += 4) {
            long value = ((long)buf[i] & 0xFF) << 24 |
                         ((long)buf[i + 1] & 0xFF) << 16 |
                         ((long)buf[i + 2] & 0xFF) << 8 |
                         ((long)buf[i + 3] & 0xFF);
            char[] block = new char[5];
            for (int j = 4; j >= 0; j--) {
                block[j] = Z85_ENCODER[(int)(value % 85)];
                value /= 85;
            }
            sb.append(block);
        }
        return sb.toString();
    }

    private static byte[] decodeZ85(String s) {
        if (s == null || s.isEmpty()) return new byte[0];
        if (s.length() % 5 != 0) throw new IllegalArgumentException("Invalid Z85 payload length.");
        byte[] outBytes = new byte[s.length() / 5 * 4];
        int outIndex = 0;
        for (int i = 0; i < s.length(); i += 5) {
            long value = 0;
            for (int j = 0; j < 5; j++) {
                char c = s.charAt(i + j);
                int v = c < 256 ? Z85_DECODER[c] : -1;
                if (v < 0) throw new IllegalArgumentException("Invalid character '" + c + "' in Z85 payload.");
                value = value * 85 + v;
            }
            outBytes[outIndex++] = (byte)((value >> 24) & 0xFF);
            outBytes[outIndex++] = (byte)((value >> 16) & 0xFF);
            outBytes[outIndex++] = (byte)((value >> 8) & 0xFF);
            outBytes[outIndex++] = (byte)(value & 0xFF);
        }
        return outBytes;
    }
}
