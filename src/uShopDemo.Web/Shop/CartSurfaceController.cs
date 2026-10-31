using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;
using Umbraco.Extensions;

namespace uShopDemo.Web.Shop;

/// <summary>Field names match the checkout form inputs.</summary>
public class CheckoutModel
{
    [Required(ErrorMessage = "Enter your name")]
    [StringLength(200)]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Enter your email")]
    [EmailAddress(ErrorMessage = "Enter a valid email")]
    [StringLength(320)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Enter your address")]
    [StringLength(1000)]
    public string? Address { get; set; }

    /// <summary>Checked for presence only, never stored.</summary>
    [Required(ErrorMessage = "Enter any card number (demo, nothing is charged)")]
    public string? CardNumber { get; set; }
}

/// <summary>Form posts for the cart. Forms are rendered with <c>Html.BeginUmbracoForm&lt;CartSurfaceController&gt;(action)</c>.</summary>
public class CartSurfaceController(
    IUmbracoContextAccessor umbracoContextAccessor,
    IUmbracoDatabaseFactory databaseFactory,
    ServiceContext services,
    AppCaches appCaches,
    IProfilingLogger profilingLogger,
    IPublishedUrlProvider publishedUrlProvider,
    ICartService cart)
    : SurfaceController(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Guid productKey, int quantity = 1)
    {
        await cart.AddAsync(productKey, quantity);
        return RedirectToShopPage("cart");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int lineId, int quantity)
    {
        await cart.UpdateAsync(lineId, quantity);
        return RedirectToShopPage("cart");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int lineId)
    {
        await cart.RemoveAsync(lineId);
        return RedirectToShopPage("cart");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutModel model)
    {
        if ((await cart.GetCurrentAsync()).IsEmpty)
        {
            ModelState.AddModelError(string.Empty, "Your cart is empty.");
        }
        if (!ModelState.IsValid)
        {
            return CurrentUmbracoPage();
        }

        var orderNumber = await cart.PlaceOrderAsync(model.Name!, model.Email!, model.Address!);
        if (orderNumber is null)
        {
            ModelState.AddModelError(string.Empty, "Your cart is empty.");
            return CurrentUmbracoPage();
        }

        TempData["orderNumber"] = orderNumber;
        var confirmation = ShopPage("orderConfirmation");
        return confirmation is null
            ? RedirectToUmbracoPage(CurrentPage!.Root())
            : Redirect($"{confirmation.Url()}?order={Uri.EscapeDataString(orderNumber)}");
    }

    private IActionResult RedirectToShopPage(string alias)
    {
        var page = ShopPage(alias);
        return page is null ? RedirectToCurrentUmbracoPage() : RedirectToUmbracoPage(page);
    }

    /// <summary>Singleton page (cart, checkout, orderConfirmation) directly under the shop root.</summary>
    private IPublishedContent? ShopPage(string alias) =>
        CurrentPage?.Root().Children()?.FirstOrDefault(c => c.ContentType.Alias == alias);
}
