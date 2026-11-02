using System.Globalization;

namespace uShopDemo.Web.Shop;

public static class Money
{
    private static readonly CultureInfo GbCulture = CultureInfo.GetCultureInfo("en-GB");

    public static string Format(decimal amount) => amount.ToString("C", GbCulture);
}
