using HandleConcurrency.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Entity = HandleConcurrency.Data.Customer;

namespace HandleConcurrencyRazorDemo.Pages;

public class CustomersRepositoryGridModel(
    [FromKeyedServices("customerrepository")] IOptimisticConcurrencyRepository<Entity> repository
    ) : PageModel
{
    private const int FirstPage = 1;
    private const int PageSize100 = 100;

    public const string Message = "Message";

    public IList<Entity> Entity { get;set; } = default!;

    /// <summary>
    /// Event method for the GET http verb.
    /// </summary>
    /// <returns></returns>
    public async Task OnGetAsync()
    {
        var result = await repository.GetAsync(x => true, [], FirstPage, PageSize100);
        if (result is not null)
        {
            Entity = result.IsSuccess ? result.Data! : [];

            var messages = result.FormatMessages();
            if (!string.IsNullOrEmpty(messages))
            {
                TempData[Message] = messages;
            }
        }
        else
        {
            Entity = [];
            TempData[Message] = "'GetAll' did not return anything.";
        }
    }
}
