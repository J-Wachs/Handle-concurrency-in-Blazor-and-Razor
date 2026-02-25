using HandleConcurrency.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Entity = HandleConcurrency.Data.Product;

namespace HandleConcurrencyRazorDemo.Pages;

public class ProductCreateModel(
    IProductRepository repository
    ) : PageModel
{
    public const string Message = "Message";
    private const string _userName = "Razor page";

    [BindProperty]
    public Entity Entity { get; set; } = default!;

    /// <summary>
    /// Event method for the GET http verb.
    /// </summary>
    /// <returns></returns>
    public IActionResult OnGet()
    {
        return Page();
    }

    /// <summary>
    /// Event method for the POST http versb.
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await repository.AddAsync(Entity, _userName);
        if (result.IsSuccess is false)
        {
            TempData[Message] = result.FormatMessages();
            return Page();
        }

        TempData[Message] = $"Product {Entity.Id} created.";
        return RedirectToPage();
    }
}
