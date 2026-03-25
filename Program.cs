using Microsoft.EntityFrameworkCore;
using EnterBridge.Components;
using EnterBridge.Data;
using EnterBridge.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=enterbridge.db"));

// API client
builder.Services.AddHttpClient<EnterBridgeApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.casestudy.enterbridge.com/");
});

// Cart is scoped per-circuit (per user session in Blazor Server)
builder.Services.AddScoped<CartService>();

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
