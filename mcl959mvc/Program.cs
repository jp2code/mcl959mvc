using mcl959mvc.Classes;
using mcl959mvc.Data;
using mcl959mvc.Models;
using mcl959mvc.Services; // Add this using directive
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore; // Ensure this is included
using Microsoft.Extensions.DependencyInjection; // Ensure this is included
using Microsoft.OpenApi.Models;
using System.Security.Claims;

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
var mailgunApiKey = builder.Configuration["Key:MailGunApiKey"];
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
    options.MailGunApiKey = mailgunApiKey ?? throw new ArgumentNullException("Key:MailGunApiKey");
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
    options.Cookie.Name = ".mcl959.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ?
        Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest :
        Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
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

// Ensure the following line is present in your .csproj file to include the Swashbuckle.AspNetCore package
// <PackageReference Include="Swashbuckle.AspNetCore" Version="6.2.3" />
builder.Services.AddScoped<MembershipService>();
builder.Services.AddScoped<IFaqService, FaqService>();
builder.Services.AddScoped<IChatAuditService, ChatAuditService>();

var app = builder.Build();

// Register the logger provider after building the app
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<Mcl959DbContext>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    loggerFactory.AddProvider(new DatabaseLoggerProvider(dbContext));
}

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

app.UseStatusCodePagesWithReExecute("/Home/Error404", "?code={0}");

app.UseStaticFiles();

app.UseRouting();

// Authenticate first
app.UseAuthentication();

// OPTIONAL: inject transient claims here so they affect authorization too
app.Use(async (ctx, next) =>
{
    if (ctx.User.Identity?.IsAuthenticated ?? false)
    {
        var userMgr = ctx.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var svc = ctx.RequestServices.GetRequiredService<MembershipService>();
        var db = ctx.RequestServices.GetRequiredService<Mcl959DbContext>(); // same scoped DbContext

        var user = await userMgr.GetUserAsync(ctx.User);
        if (user != null)
        {
            // Recompute flags each request
            user.IsMember = false;
            user.IsAdmin = false;
            await svc.MapToRoster(user);

            // If MapToRoster set roster.Authenticated, persist it once
            await db.SaveChangesAsync();

            // isRegistered = EmailConfirmed
            var id = (ClaimsIdentity)ctx.User.Identity!;
            var stale = id.FindAll(c => c.Type is "isRegistered" or "isMember" or "isAdmin").ToList();
            foreach (var c in stale) id.RemoveClaim(c);

            id.AddClaim(new Claim("isRegistered", user.EmailConfirmed ? "true" : "false"));
            id.AddClaim(new Claim("isMember", user.IsMember ? "true" : "false"));
            id.AddClaim(new Claim("isAdmin", user.IsAdmin ? "true" : "false"));
        }
    }
    await next();
});

// Then authorize
app.UseAuthorization();

app.Use(async (ctx, next) =>
{
    if (ctx.User.Identity?.IsAuthenticated ?? false)
    {
        var userMgr = ctx.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var svc = ctx.RequestServices.GetRequiredService<MembershipService>();
        var user = await userMgr.GetUserAsync(ctx.User);
        if (user != null)
        {
            await svc.MapToRoster(user);

            var id = (ClaimsIdentity)ctx.User.Identity!;

            // Remove previous transient claims (always re-sync)
            var stale = id.FindAll(c => c.Type is "isRegistered" or "isMember" or "isAdmin").ToList();
            foreach (var c in stale)
            {
                id.RemoveClaim(c);
            }
            id.AddClaim(new Claim("isRegistered", "true"));
            id.AddClaim(new Claim("isMember", user.IsMember ? "true" : "false"));
            id.AddClaim(new Claim("isAdmin", user.IsAdmin ? "true" : "false"));
        }
    }
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapControllers();

app.MapPost("/api/chat/ask", async (
    ChatRequest req,
    IFaqService faq,
    MembershipService membershipService,
    UserManager<ApplicationUser> userManager,
    IChatAuditService audit,
    HttpContext http) =>
{
    if (!http.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }
    var user = await userManager.GetUserAsync(http.User);
    var answer = "";
    var isRegHelp = false;

    // Registration intent detection
    var q = req.Question?.ToLowerInvariant() ?? "";
    bool announceIntent = q.Contains("announce") || q.Contains("hover") || q.Contains("speak");
    bool thanks = q.Contains("thank you") || q.Contains("thanks") || q.Contains("thx") || q.Contains("ty");
    if (q.Contains("register") || q.Contains("create account") || q.Contains("sign up"))
    {
        isRegHelp = true;
        answer = """
Steps to create an account:
<ul style="margin:0;padding-left:18px;">
<li>Click Register (top right).</li>
<li>Fill required fields and submit.</li>
<li>Check your email and confirm.</li>
<li>Log in.</li>
<li>If you are a paid member of MCL959, be sure that the email you provide here matches with the email you registered with.</li>
<li>After login refresh Members page; you will be marked Authenticated if matched.</li>
</ul>
""";
    }
    else if (announceIntent)
    {
        isRegHelp = true;
        answer = @"
To get Announce on Hover to work, enable the checkbox.<br/><br/>
Occasionally, the browser will load with the checkbox already checked, but the feature does not work. If this is your case, uncheck the box, and then recheck it.<br/><br/>
To have parts of the page read aloud, either hover your mouse over the text or tap the text with your finger if using a touchscreen device.
";
    }
    else if (thanks)
    {
        isRegHelp = true;
        answer = "You are welcome! I hope I have answered everything to your satisfaction. If I have left anything out, please ask shorter questions. This format is limited.";
    }
    else
    {
        var match = faq.FindBest(req.Question);
        answer = match?.Answer ?? "I do not have an answer yet. Please rephrase or contact us via the Contact page.";
    }

    // Log & email the Q/A
    await audit.LogAndEmailAsync(user?.Email, req.Question ?? "", answer, isRegHelp);

    var suggestions = !string.IsNullOrEmpty(answer) ? Array.Empty<string>() :
        isRegHelp ? new[] { "How do I reset my password?", "What are membership requirements?" }
        : new[] { "How do I create an account?", "Where is the application form?" };

    return Results.Ok(new ChatResponse {
        Answer = answer,
        IsRegistrationHelp = isRegHelp,
        Suggestions = suggestions,
        IsHtml = true
    });
})
.RequireAuthorization();

// Public (unauthenticated) limited endpoint
app.MapPost("/api/chat/public", async (
    ChatRequest req,
    IChatAuditService audit) =>
{
    var q = (req.Question ?? "").ToLowerInvariant();
    bool loginIntent = q.Contains("login") || q.Contains("log in");
    bool registerIntent = q.Contains("register") || q.Contains("sign up") || q.Contains("create account");
    bool announceIntent = q.Contains("announce") || q.Contains("hover") || q.Contains("speak");
    bool thanks = q.Contains("thank you") || q.Contains("thanks") || q.Contains("thx") || q.Contains("ty");

    string answer;
    bool isRegHelp = false;

    if (!(loginIntent || registerIntent || announceIntent || thanks))
    {
        answer = "Please log in for full answers. You can ask only about how to register or log in here.";
        await audit.LogAndEmailAsync(null, req.Question ?? "(empty)", answer, false);
        return Results.Ok(new ChatResponse {
            Answer = answer,
            Suggestions = new[] { "How do I register?","How do I log in?" },
            IsHtml = true
        });
    }

    if (registerIntent)
    {
        isRegHelp = true;
        answer = """
Registration steps:
<ul style="margin:0;padding-left:18px;">
<li>Click Register (top-right).</li>
<li>Provide your email & password (email must be yours).</li>
<li>Go to your email and confirm your identity by clicking on the link sent to you by this website.</li>
<li>Return here, and log in with your email & password.</li>
<li>Ensure the email matches a roster PersonalEmail or WorkEmail.</li>
<li>After login refresh Members page; you will be marked Authenticated if matched.</li>
</ul>
To be recognized as member of MCL959, ensure your confirmed email matches the roster record.
""";
    }
    else if (announceIntent)
    {
        isRegHelp = true;
        answer = @"
To get Announce on Hover to work, enable the checkbox.<br/><br/>
Occasionally, the browser will load with the checkbox already checked, but the feature does not work. If this is your case, uncheck the box, and then recheck it.<br/><br/>
To have parts of the page read aloud, either hover your mouse over the text or tap the text with your finger if using a touchscreen device.
";
    }
    else if (thanks)
    {
        isRegHelp = true;
        answer = "You are welcome! I hope I have answered everything to your satisfaction. If I have left anything out, please ask shorter questions. This format is limited.";
    }
    else
    {
        answer = """
Login steps:
<ul style="margin:0;padding-left:18px;">
<li>Click Login (top-right).</li>
<li>Enter your registered email & password.</li>
<li>If you just registered, be sure you confirmed the email link first.</li>
</ul>
If your email is recognized as a member of MCL959, you will gain member status automatically after login.
""";
    }

    await audit.LogAndEmailAsync(null, req.Question ?? "(empty)", answer, isRegHelp);

    var suggestions = !string.IsNullOrEmpty(answer) ? Array.Empty<string>() :
        isRegHelp ? new[] { "How do I reset my password?", "What are membership requirements?" }
        : new[] { "How do I create an account?", "Where is the application form?" };

    return Results.Ok(new ChatResponse {
        Answer = answer,
        Suggestions = suggestions,
        IsHtml = true
    });
});

try
{
    // app.UseDeveloperExceptionPage(); // remove this after figuring out the login issue
    app.Run();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(ex, "Application failed to start.");
    throw;
}
