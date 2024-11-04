using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using TumbAsk.Data; // `ApplicationDbContext` i�in gerekli
using TumbAsk.Hubs;
using TumbAsk.Repositories; // Repositories klas�r�ndeki custom repository i�in
using TumbAsk.Services; // Services klas�r�ndeki custom servisler i�in


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromMinutes(10); // Keep-alive mesaj�n� her 10 dakikada bir g�nder
    options.HandshakeTimeout = TimeSpan.FromMinutes(5); // El s�k��ma zaman a��m� s�resi
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(30); // �stemci zaman a��m� s�resi
    options.MaximumReceiveMessageSize = 32 * 1024; // Maksimum al�nan mesaj boyutu
});

// PostgreSQL ba�lant� dizesini al�n
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// PostgreSQL yap�land�rmas�yla Hangfire�� ba�lat�n
builder.Services.AddHangfire(config => config.UsePostgreSqlStorage(connectionString));
builder.Services.AddHangfireServer();
builder.Services.AddMemoryCache();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));


// Register custom services and repositories
builder.Services.AddScoped<IBotRepository, BotRepository>();
builder.Services.AddScoped<IBotService, BotService>();
builder.Services.AddScoped<IUserAccountRepository, UserAccountRepository>();
builder.Services.AddScoped<TumblrService>(); // TumblrService'i DI Container'a ekleyin
builder.Services.AddScoped<BotLogService>(); // TumblrService'i DI Container'a ekleyin
builder.Services.AddScoped<Dictionary<int, CancellationTokenSource>>(); // TumblrService'i DI Container'a ekleyin
builder.Services.AddHttpClient<TumblrService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseHangfireDashboard(); // Hangfire Dashboard ekleniyor

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Bot}/{action=Index}/{id?}");
    endpoints.MapHub<TumblrStatusHub>("/tumblrStatusHub"); // SignalR hub tan�m�
    endpoints.MapHub<BotStatusHub>("/botStatusHub"); // SignalR hub tan�m�
});

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Bot}/{action=Index}/{id?}");

//app.MapHub<BotStatusHub>("/botStatusHub"); // SignalR Hub ekleniyor


app.Run();
