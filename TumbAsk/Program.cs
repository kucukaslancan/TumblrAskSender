using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using TumbAsk.Data; // `ApplicationDbContext` için gerekli
using TumbAsk.Hubs;
using TumbAsk.Repositories; // Repositories klasöründeki custom repository için
using TumbAsk.Services; // Services klasöründeki custom servisler için


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromMinutes(10); // Keep-alive mesajýný her 10 dakikada bir gönder
    options.HandshakeTimeout = TimeSpan.FromMinutes(5); // El sýkýþma zaman aþýmý süresi
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(30); // Ýstemci zaman aþýmý süresi
    options.MaximumReceiveMessageSize = 32 * 1024; // Maksimum alýnan mesaj boyutu
});

// PostgreSQL baðlantý dizesini alýn
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// PostgreSQL yapýlandýrmasýyla Hangfire’ý baþlatýn
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
    endpoints.MapHub<TumblrStatusHub>("/tumblrStatusHub"); // SignalR hub tanýmý
    endpoints.MapHub<BotStatusHub>("/botStatusHub"); // SignalR hub tanýmý
});

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Bot}/{action=Index}/{id?}");

//app.MapHub<BotStatusHub>("/botStatusHub"); // SignalR Hub ekleniyor


app.Run();
