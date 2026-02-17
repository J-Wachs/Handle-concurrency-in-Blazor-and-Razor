using HandleConcurrency.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Buffers.Binary;
using Entity = HandleConcurrency.Data.Customer;

namespace HandleConcurrencyRazorDemo.Pages;

public class CustomerRepositoryDeleteModel(
    [FromKeyedServices("customerrepository")] IOptimisticConcurrencyRepository<Entity> repository
    ) : PageModel
{
    public const string Message = "Message";

    [BindProperty]
    public Entity Entity { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public bool DisplayCustomerInfo { get; set; } = true;

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
            Signature = Utils.Signature.Calculate(Entity.Id, BinaryPrimitives.ReadUInt64BigEndian(Entity.Version));
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
        var localSignature = Utils.Signature.Calculate(Entity.Id, BinaryPrimitives.ReadUInt64BigEndian(Entity.Version));
        if (localSignature != Signature)
        {
            // We need to refill the Entity class because we only got two fields in:
            var getByIdResult = await repository.GetByIdAsync(Entity.Id);
            if (getByIdResult.IsSuccess)
            {
                Entity = getByIdResult.Data!;
            }

            TempData[Message] = "Signature does not match.";
            return Page();
        }

        var result = await repository.DeleteAsync(Entity);
        if (result.IsSuccess)
        {
            DisplayCustomerInfo = false;
            TempData[Message] = $"Customer {Entity.Id} has been deleted.";
        }
        else
        {
            TempData[Message] = result.FormatMessages();
        }

        return Page();
    }
}
