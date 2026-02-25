using HandleConcurrency.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Entity = HandleConcurrency.Data.Product;

namespace HandleConcurrencyRazorDemo.Pages;

public class ProductsGridModel(
    IProductRepository repository
    ) : PageModel
{
    public const string Message = "Message";

    private const int FirstPage = 1;
    private const int PageSize100 = 100;

    public IList<Entity> Entity { get; set; } = default!;

    /// <summary>
    /// Event method for the GET http verb.
    /// </summary>
    /// <returns></returns>
    public async Task OnGetAsync()
    {
        var result = await repository.GetAsync(x => true, [], FirstPage, PageSize100);
        if (result is not null)
        {
            if (result.IsSuccess)
            {
                Entity = result.Data!;
            }
            else
            {
                Entity = [];
            }
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
