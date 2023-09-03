using EvitaDB.QueryValidator.Serialization.Markdown.Structures;

namespace EvitaDB.QueryValidator.Utils;

public static class StringUtils
{
    public static string SurroundValueWith(string value, string surrounding)
    {
        return surrounding + value + surrounding;
    }

    public static string FillUpAligned(string value, string fill, int length, int alignment)
    {
        switch (alignment)
        {
            case Table<object>.AlignRight:
            {
                return FillUpRightAligned(value, fill, length);
            }
            case Table<object>.AlignCenter:
            {
                return FillUpCenterAligned(value, fill, length);
            }
            default:
            {
                return FillUpLeftAligned(value, fill, length);
            }
        }
    }

    public static string FillUpLeftAligned(string value, string fill, int length)
    {
        if (value.Length >= length)
        {
            return value;
        }

        while (value.Length < length)
        {
            value += fill;
        }

        return value;
    }

    public static string FillUpRightAligned(string value, string fill, int length)
    {
        if (value.Length >= length)
        {
            return value;
        }

        while (value.Length < length)
        {
            value = fill + value;
        }

        return value;
    }

    public static string FillUpCenterAligned(string value, string fill, int length)
    {
        if (value.Length >= length)
        {
            return value;
        }

        bool left = true;
        while (value.Length < length)
        {
            if (left)
            {
                value = FillUpLeftAligned(value, fill, value.Length + 1);
            }
            else
            {
                value = FillUpRightAligned(value, fill, value.Length + 1);
            }

            left = !left;
        }

        return value;
    }
}