using libraryMVC.Data;
using libraryMVC.Data.Repositories;
using libraryMVC.Data.Storage;
using libraryMVC.Interfaces;
using libraryMVC.Services;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));



builder.Services.AddScoped<IAuthorsRepository, AuthorsRepository>();
builder.Services.AddScoped<IBooksRepository, BooksRepository>();

builder.Services.AddScoped<IAuthorsService, AuthorsService>();
builder.Services.AddScoped<IBooksService, BooksService>();
builder.Services.AddScoped<IAuthorQueries>(provider =>
    (IAuthorQueries)provider.GetRequiredService<IAuthorsService>());
builder.Services.AddScoped<IAuthorApplicationService>(provider =>
    (IAuthorApplicationService)provider.GetRequiredService<IAuthorsService>());
builder.Services.AddScoped<IAuthorLifecycle>(provider =>
    (IAuthorLifecycle)provider.GetRequiredService<IAuthorsService>());
builder.Services.AddScoped<IBookQueries>(provider =>
    (IBookQueries)provider.GetRequiredService<IBooksService>());
builder.Services.AddScoped<IBookLifecycle>(provider =>
    (IBookLifecycle)provider.GetRequiredService<IBooksService>());
builder.Services.AddScoped<IBookImageService, BookImageService>();
builder.Services.AddScoped<IImageFileValidator, ImageFileValidator>();
builder.Services.AddScoped<IBookImageStorage, LocalBookImageStorage>();
builder.Services.AddScoped<IBookAuthorRepository, BookAuthorRepository>();
builder.Services.AddScoped<IBookAuthorService, BookAuthorService>();
builder.Services.AddScoped<IAuthorBookRepository, AuthorBookRepository>();
builder.Services.AddScoped<IAuthorBookService, AuthorBookService>();
builder.Services.AddScoped<IBookImageRepository, BookImageRepository>();
builder.Services.AddScoped<IBookApplicationService, BookApplicationService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

app.Run();
