using HandleConcurrency.Data;
using HandleConcurrency.Repositories;
using HandleConcurrency.Repositories.Interfaces;
using HandleConcurrencyBlazorDemo.Components;
using HandleConcurrencyBlazorDemo.Controllers.Utils;
using HandleConcurrencyBlazorDemo.Controllers.Utils.Interfaces;
using HandleConcurrencyBlazorDemo.Data;
using HandleConcurrencyBlazorDemo.Utils;
using HandleConcurrencyBlazorDemo.Utils.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// In this demo project, the two lines are for giving the possibility to use the two Customer grids.
// Normally you would only have one of them.
builder.Services.AddKeyedScoped<IOptimisticConcurrencyRepository<Customer>, CustomerRepository>("customerrepository");
builder.Services.AddKeyedScoped<IOptimisticConcurrencyRepository<Customer>, CustomerRepositoryClient>("customerrepositoryclient");

builder.Services.AddScoped<IProductRepository, ProductRepository>();

// This way of adding the interface and the implementation of the class, makes runtime 
// create the specific instance of the class, when the DI system 'sees' that I have put
// e.g. IAPIQueryParser<Customer> or IAPIQueryParser<Product> in classes. They could be 
// written below explicit as:
// builder.Services.AddScoped<IAPIQueryParser<Customer>, APIQueryParser<Customer>>();
builder.Services.AddScoped(typeof(IAPIQueryParser<>), typeof(APIQueryParser<>));
builder.Services.AddScoped(typeof(IUrlQueryVisitor<>), typeof(UrlQueryVisitor<>));


builder.Services.AddDbContextFactory<DatabaseContext>(options =>
{
    options
     .UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();

app.Run();
