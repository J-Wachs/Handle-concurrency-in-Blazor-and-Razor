using HandleConcurrency.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Entity = HandleConcurrency.Data.Product;

namespace HandleConcurrencyRazorDemo.Pages;

public class ProductDeleteModel(
        IProductRepository repository
    ) : PageModel
{
    public const string Message = "Message";

    [BindProperty]
    public Entity Entity { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public bool DisplayEntityInfo { get; set; } = true;

    [BindProperty(SupportsGet = true)]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Event method for the GET http verb.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<IActionResult> OnGetAsync(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var result = await repository.GetByIdAsync(id.Value);
        if (result.IsSuccess)
        {
            Entity = result.Data!;

            // We calculate a signature for hidden fields, in order to protect against tampering:
            Signature = Utils.Signature.Calculate(Entity.Id, Entity.VersionInfo, Entity.VersionQuantities);
        }

        var messages = result.FormatMessages();
        if (!string.IsNullOrEmpty(messages))
        {
            TempData[Message] = messages;
        }

        return Page();
    }

    /// <summary>
    /// Event method for the POST http versb.
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult> OnPostAsync()
    {
        // Re-calculate the signature in order to detect if data has been tamperd with:
        var localSignature = Utils.Signature.Calculate(Entity.Id, Entity.VersionInfo, Entity.VersionQuantities);
        if (localSignature != Signature)
        {
            TempData[Message] = "Signature does not match.";
            return Page();
        }

        var result = await repository.DeleteAsync(Entity);
        if (result.IsSuccess)
        {
            DisplayEntityInfo = false;
            TempData[Message] = $"Product {Entity.Id} has been deleted.";
        }
        else
        {
            TempData[Message] = result.FormatMessages();
        }

        return Page();
    }
}
