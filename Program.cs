using libraryMVC.Data;
using libraryMVC.Data.Repositories;
using libraryMVC.Data.Storage;
using libraryMVC.Interfaces;
using libraryMVC.Security;
using libraryMVC.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));


builder.Services.AddIdentity<IdentityUser, IdentityRole>(
    options =>
    {
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    }
)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});


builder.Services.AddScoped<IAuthorsRepository, AuthorsRepository>();
builder.Services.AddScoped<IBooksRepository, BooksRepository>();
builder.Services.AddScoped<ICategoryRespository, CategoryRepository>();

builder.Services.AddScoped<IAuthorsService, AuthorsService>();
builder.Services.AddScoped<IBooksService, BooksService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
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
builder.Services.AddScoped<ICategoryBookRepository, CategoryBookRepository>();
builder.Services.AddScoped<ICategoryBookService, CategoryBookService>();
builder.Services.AddScoped<IAuthorBookRepository, AuthorBookRepository>();
builder.Services.AddScoped<IAuthorBookService, AuthorBookService>();
builder.Services.AddScoped<IBookImageRepository, BookImageRepository>();
builder.Services.AddScoped<IBookApplicationService, BookApplicationService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICategoryQueries>(provider =>
    (ICategoryQueries)provider.GetRequiredService<ICategoryService>());
builder.Services.AddScoped<ICategoryApplicationService>(provider =>
    (ICategoryApplicationService)provider.GetRequiredService<ICategoryService>());
builder.Services.AddScoped<ICategoryLifecycle>(provider =>
    (ICategoryLifecycle)provider.GetRequiredService<ICategoryService>());

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();

    RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    UserManager<IdentityUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    await IdentitySeed.EnsureRolesAsync(roleManager);

    if (args.Any(argument => string.Equals(argument, "--seed-identity", StringComparison.OrdinalIgnoreCase)))
    {
        await IdentitySeed.SeedAsync(
            roleManager,
            userManager,
            builder.Configuration);
        Console.WriteLine("Identity roles and seed users are configured.");
        return;
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

app.Run();
