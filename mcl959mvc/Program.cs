using mcl959mvc.Classes;
using mcl959mvc.Data;
using mcl959mvc.Models;
using mcl959mvc.Services; // Add this using directive
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore; // Ensure this is included
using Microsoft.Extensions.DependencyInjection; // Ensure this is included
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
// Explicitly load appsettings.Secrets.json
builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

// Now you can read your SMTP values
var smtpUser = builder.Configuration["Smtp:Username"];
var smtpPass = builder.Configuration["Smtp:Password"];
var smtpServer = builder.Configuration["Smtp:Server"];
var fromEmail = builder.Configuration["Smtp:FromEmail"];
var siteDomain = builder.Configuration["Smtp:SiteDomain"];
var siteLogo = builder.Configuration["Smtp:SiteLogo"];
// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, SmtpEmailSender>();
builder.Services.Configure<SmtpSettings>(options =>
{
    options.Server = smtpServer ?? throw new ArgumentNullException("Smtp:Server");
    options.Username = smtpUser ?? throw new ArgumentNullException("Smtp:Username");
    options.Password = smtpPass ?? throw new ArgumentNullException("Smtp:Password");
    options.FromEmail = fromEmail ?? throw new ArgumentNullException("Smtp:FromEmail");
    options.SiteDomain = siteDomain ?? throw new ArgumentNullException("Smtp:SiteDomain");
    options.SiteLogo = siteLogo ?? throw new ArgumentNullException("Smtp:SiteLogo");
});
builder.Services.AddDbContext<Mcl959DbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Mcl959Database")));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Mcl959Database")));

// Add services to the container.
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(90);
    options.SlidingExpiration = true; // Optional: extends the cookie if the user is active
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});
builder.Services.AddControllersWithViews();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Register the logger provider
builder.Logging.AddProvider(new DatabaseLoggerProvider(
     builder.Services.BuildServiceProvider().GetRequiredService<Mcl959DbContext>()));
builder.Services.AddScoped<MembershipService>();

// Ensure the following line is present in your .csproj file to include the Swashbuckle.AspNetCore package
// <PackageReference Include="Swashbuckle.AspNetCore" Version="6.2.3" />
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    var req = context.Request;
    var host = req.Host.Host.ToLower();

    // Redirect to www if not already www and not localhost
    if (!host.StartsWith("www.") && !host.Contains("localhost"))
    {
        var newHost = "www." + req.Host;
        var newUrl = $"{req.Scheme}://{newHost}{req.Path}{req.QueryString}";
        context.Response.Redirect(newUrl, permanent: true);
        return;
    }
    await next();
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
