var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("BackendApi", client =>
{
    var baseUrl = builder.Configuration["BackendApi:BaseUrl"] ?? "http://127.0.0.1:5001/api/public/";
    if (!baseUrl.EndsWith('/'))
        baseUrl += "/";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(15);

    var hostHeader = builder.Configuration["BackendApi:HostHeader"];
    if (!string.IsNullOrWhiteSpace(hostHeader))
        client.DefaultRequestHeaders.Host = hostHeader;
});

builder.Services.AddScoped<PublicWeb.Services.Public.LeaguePublicService>();
builder.Services.AddScoped<PublicWeb.Services.Public.TeamPublicService>();
builder.Services.AddScoped<PublicWeb.Services.Public.MatchPublicService>();

var app = builder.Build();

var pathBase = app.Configuration["PublicWeb:PathBase"];
if (!string.IsNullOrWhiteSpace(pathBase))
{
    app.UsePathBase(pathBase);
}

/// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

