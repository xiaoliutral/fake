namespace System;

public static class FakeByteArrayExtensions
{
    /// <summary>
    /// Converts a byte array to a hexadecimal string representation.
    /// Each byte is represented by two hexadecimal characters.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string ToHex(this byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes), "Byte array cannot be null.");
        }

        char[] hexChars = new char[bytes.Length * 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            int byteValue = bytes[i] & 0xFF;
            hexChars[i * 2] = GetHexChar(byteValue >> 4);
            hexChars[i * 2 + 1] = GetHexChar(byteValue & 0x0F);
        }
        return new string(hexChars);
    }


    /// <summary>
    /// Converts a hexadecimal string representation to a byte array.
    /// The string must have an even length, and each pair of characters represents one byte.
    /// </summary>
    /// <param name="hex"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static byte[] FromHex(this string hex)
    {
        if (string.IsNullOrEmpty(hex))
        {
            throw new ArgumentNullException(nameof(hex), "Hex string cannot be null or empty.");
        }

        if (hex.Length % 2 != 0)
        {
            throw new ArgumentException("Hex string must have an even length.", nameof(hex));
        }

        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)((GetHexValue(hex[i * 2]) << 4) | GetHexValue(hex[i * 2 + 1]));
        }
        return bytes;
    }

    private static char GetHexChar(int value)
    {
        if (value < 0 || value > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 0 and 15.");
        }
        return (char)(value < 10 ? '0' + value : 'A' + (value - 10));
    }

    private static int GetHexValue(char hexChar)
    {
        if (hexChar >= '0' && hexChar <= '9')
        {
            return hexChar - '0';
        }
        if (hexChar >= 'A' && hexChar <= 'F')
        {
            return hexChar - 'A' + 10;
        }
        if (hexChar >= 'a' && hexChar <= 'f')
        {
            return hexChar - 'a' + 10;
        }
        throw new ArgumentException($"Invalid hex character: {hexChar}", nameof(hexChar));
    }


    /// <summary>
    /// Converts a byte array to a Base64 string representation.
    /// The Base64 string can be used for data transfer or storage.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string ToBase64(this byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes), "Byte array cannot be null.");
        }
        return Convert.ToBase64String(bytes);
    }


    /// <summary>
    /// Converts a Base64 string representation back to a byte array.
    /// The Base64 string must be a valid Base64 encoded string.
    /// </summary>
    /// <param name="base64"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static byte[] FromBase64(this string base64)
    {
        if (string.IsNullOrEmpty(base64))
        {
            throw new ArgumentNullException(nameof(base64), "Base64 string cannot be null or empty.");
        }
        return Convert.FromBase64String(base64);
    }
}