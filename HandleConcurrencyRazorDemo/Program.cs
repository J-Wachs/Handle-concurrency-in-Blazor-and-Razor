using HandleConcurrency.Data;
using HandleConcurrency.Repositories;
using HandleConcurrency.Repositories.Interfaces;
using HandleConcurrencyRazorDemo.Data;
using HandleConcurrencyRazorDemo.Utils;
using HandleConcurrencyRazorDemo.Utils.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddKeyedScoped<IOptimisticConcurrencyRepository<Customer>, CustomerRepository>("customerrepository");
builder.Services.AddKeyedScoped<IOptimisticConcurrencyRepository<Customer>, CustomerRepositoryClient>("customerrepositoryclient");

// This way of added the interface and the implementation of the class, makes runtime 
// create the specific instance of the class, when the DI system 'sees' that I have put
// e.g. IAPIQueryParser<Customer> or IAPIQueryParser<Product> in classes. They could be 
// written below explicit as:
//builder.Services.AddScoped<IUrlQueryVisitor<Customer>, UrlQueryVisitor<Customer>>();
builder.Services.AddScoped(typeof(IUrlQueryVisitor<>), typeof(UrlQueryVisitor<>));

// We need support for sessions:
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    // Make sure this is allowed for your application!
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();


builder.Services.AddDbContextFactory<DatabaseContext>(options =>
{
    options
     .UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});

builder.Services.AddHttpClient();

var app = builder.Build();

// Apply any pending migrations
using (var serviceScope = app.Services.CreateScope())
{
    var context = serviceScope.ServiceProvider.GetRequiredService<DatabaseContext>();
    context.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.UseSession();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
