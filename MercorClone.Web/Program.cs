using Ganss.Xss;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MercorClone.Web.Data;
using MercorClone.Web.Models.Entities;
using MercorClone.Web.Repositories;
using MercorClone.Web.Repositories.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ── MVC ───────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Database ──────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// ── Identity ──────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit          = true;
    options.Password.RequiredLength        = 8;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ── Repositories ──────────────────────────────────────────────────
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();


//Swager
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── HTML Sanitizer (stored XSS protection for Quill editor output) ─
builder.Services.AddScoped<IHtmlSanitizer>(_ =>
{
    var sanitizer = new HtmlSanitizer();

    sanitizer.AllowedTags.Clear();
    foreach (var tag in new[] { "p", "br", "b", "i", "u", "strong", "em",
                                 "ul", "ol", "li", "h2", "h3", "a", "span" })
    {
        sanitizer.AllowedTags.Add(tag);
    }

    sanitizer.AllowedAttributes.Clear();
    sanitizer.AllowedAttributes.Add("href");
    sanitizer.AllowedAttributes.Add("target");
    sanitizer.AllowedAttributes.Add("rel");
    sanitizer.AllowedAttributes.Add("class");

    sanitizer.AllowedSchemes.Clear();
    sanitizer.AllowedSchemes.Add("https");
    sanitizer.AllowedSchemes.Add("http");
    sanitizer.AllowedSchemes.Add("mailto");

    return sanitizer;
});

// ─────────────────────────────────────────────────────────────────
var app = builder.Build();

// 2. Add the Swagger middleware here:
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MercorClone API v1");
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


// ── Pipeline ──────────────────────────────────────────────────────
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();