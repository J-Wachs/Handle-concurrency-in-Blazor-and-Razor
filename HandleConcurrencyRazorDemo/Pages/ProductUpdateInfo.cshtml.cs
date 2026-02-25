using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HandleConcurrency.Repositories.Interfaces;
using Entity = HandleConcurrency.Data.DTOs.ProductInfoDTO;

namespace HandleConcurrencyRazorDemo.Pages;

public class ProductUpdateInfoModel(
    IProductRepository repository
    ) : PageModel
{
    public const string Message = "Message";
    private const string _userName = "Razor page";

    [BindProperty]
    public Entity Entity { get; set; } = default!;

    [BindProperty]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Event method for the GET http verb.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<IActionResult> OnGetAsync(long? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var result = await repository.GetByIdAsync(id.Value);
        if (result.IsSuccess)
        {
            Entity = result.Data!;

            // We calculate a signature for hidden fields, in order to protect against tampering:
            Signature = Utils.Signature.Calculate(Entity.Id);
        }

        var messages = result.FormatMessages();
        if (!string.IsNullOrEmpty(messages))
        {
            TempData[Message] = messages;
        }

        return Page();
    }

    /// <summary>
    /// Event method for the POST http verb.
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult> OnPostAsync()
    {
        // Re-calculate the signature in order to detect if data has been tamperd with:
        var localSignature = Utils.Signature.Calculate(Entity.Id);
        if (localSignature != Signature)
        {
            TempData[Message] = "Signature does not match.";
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await repository.UpdateInfoAsync(Entity!, _userName);
        if (result.IsSuccess is false)
        {
            TempData[Message] = result.FormatMessages();
            return Page();
        }

        TempData[Message] = $"Product {Entity.Id} updated.";
        return RedirectToPage(new { id = Entity.Id });
    }
}
