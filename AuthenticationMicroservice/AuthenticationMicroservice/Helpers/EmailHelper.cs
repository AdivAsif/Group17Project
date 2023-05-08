namespace AuthenticationMicroservice.Helpers;

public enum TableSize
{
    Small = 500,
    Medium = 800,
    Large = 1000
}

public static class EmailHelper
{
    public const string TableCell =
        " style='color:#000; padding-bottom:1%; vertical-align: middle; border-bottom: 1px solid #8D8AD8; border-left: 1px solid #8D8AD8; border-right: 1px solid #8D8AD8;'";

    public static string TableProperties(TableSize width)
    {
        return
            $"width={width} cellpadding=0 cellspacing=0 border=0 style='text-align: center; font-family: sans-serif'";
    }

    public static string TableHeader(int colspan)
    {
        return
            $"colspan='{colspan}' style='color:#000; background:#8D8AD8; text-align: center; border-bottom: 1px solid #414447; font-weight: 100; padding:8px; text-shadow: 0 1px 1px rgba(0, 0, 0, 0.1); border-top-left-radius:10px; border-top-right-radius:10px;'";
    }

    public static string TableFooter(int colspan)
    {
        return
            $"colspan='{colspan}' style='color:#000; background:#8D8AD8; text-align: center; font-weight: 100; padding:8px; text-shadow: 0 1px 1px rgba(0, 0, 0, 0.1); border-bottom-left-radius:10px; border-bottom-right-radius:10px;'";
    }
}