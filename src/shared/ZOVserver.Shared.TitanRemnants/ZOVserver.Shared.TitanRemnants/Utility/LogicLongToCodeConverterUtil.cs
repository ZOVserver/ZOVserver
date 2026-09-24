using System.Text;

namespace ZOVserver.Shared.TitanRemnants.Utility;

public class LogicLongToCodeConverterUtil(string chars = LogicLongToCodeConverterUtil.DefaultChars)
{
    private const string DefaultChars = "0289PYLQGRJCUV";

    public string? ToCode(int hi, int lo)
    {
        if (hi >= 256)
            return null;

        var n = ToLong(lo >> 24, hi | (lo << 8));

        string ret;

        if (n != 0)
        {
            var sb = new StringBuilder();

            for (var b = chars.Length; n > 0; n /= b)
                sb.Insert(0, chars[(int)(n % b)]);

            ret = sb.ToString();
        }
        else
        {
            ret = "0";
        }

        return "#" + ret;
    }

    public long ToId(string code)
    {
        if (code.Length is < 2 or >= 14)
            return -1;

        long v = 0;

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var c in code[1..])
        {
            var i = chars.IndexOf(c);
            if (i < 0) return -1;

            v = v * chars.Length + i;
        }

        return ToLong((int)(v & 0xFF), (int)((v >> 8) & 0x7FFFFFFF));
    }

    public static long ToLong(int hi, int lo)
    {
        return ((long)hi << 32) | (lo & 0xFFFFFFFFL);
    }
}