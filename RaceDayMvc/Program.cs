using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // Every POST must carry an antiforgery token (the forms add it automatically)
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddHttpContextAccessor();

// MVC-side session: remembers who is logged in and the API session cookie that goes with them
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Web";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(2);
});

// The ONLY way this app touches data: HTTP calls to the RaceDay API.
// UseCookies = false stops one shared cookie jar mixing different users' API sessions;
// ApiSessionHandler attaches the right user's API cookie on every request instead.
builder.Services.AddTransient<ApiSessionHandler>();
builder.Services
    .AddHttpClient<IRaceDayApi, RaceDayApiClient>(client =>
    {
        var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5000/";
        client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseCookies = false,
        AllowAutoRedirect = false
    })
    .AddHttpMessageHandler<ApiSessionHandler>();

var app = builder.Build();

// Friendly error pages in every environment: users never see a stack trace
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");

// Always parse and print numbers and dates the invariant way. A South African Windows PC uses a
// comma as the decimal separator, which would otherwise reject "21.1" km typed into a number field.
var invariant = System.Globalization.CultureInfo.InvariantCulture;
app.UseRequestLocalization(new Microsoft.AspNetCore.Builder.RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(invariant),
    SupportedCultures = new List<System.Globalization.CultureInfo> { invariant },
    SupportedUICultures = new List<System.Globalization.CultureInfo> { invariant }
});

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
