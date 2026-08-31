using Parlays.Hubs;
using Parlays.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddSignalR();

// Register application singleton services
builder.Services.AddSingleton<IOddsDataService, OddsDataService>();
builder.Services.AddSingleton<IParlayCalculatorService, ParlayCalculatorService>();
builder.Services.AddHostedService<OddsEngineService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapHub<OddsHub>("/hubs/oddshub");

app.Run();
