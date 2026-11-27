using LiveChat.DevTools;
using LiveChat.Repositories;
using LiveChat.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "LiveChat.Auth";
    });

// 未標記 [AllowAnonymous] 的端點一律需要登入
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Repository 註冊區
builder.Services.AddSingleton(typeof(IRepository<>), typeof(JsonFileRepository<>));

// Service 註冊區（builder.Services.AddScoped<IXxxService, XxxService>();）
builder.Services.AddScoped<IAuthService, AuthService>();

// Development 工具
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<SeedResetService>();
}

var app = builder.Build();

// 首次啟動時建立 App_Data
SeedDataInitializer.Initialize(app.Environment.ContentRootPath);

// dotnet run -- --seed：重新產生測試資料並驗證後結束（僅 Development）
if (app.Environment.IsDevelopment() && args.Contains("--seed"))
{
    var checks = app.Services.GetRequiredService<SeedResetService>().Reset();
    foreach (var check in checks)
    {
        Console.WriteLine($"[{(check.Passed ? "通過" : "失敗")}] {check.Name}：{check.Detail}");
    }

    var failedCount = checks.Count(x => !x.Passed);
    Console.WriteLine($"SeedData 驗證：{checks.Count - failedCount}/{checks.Count} 項通過");
    Environment.ExitCode = failedCount == 0 ? 0 : 1;
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
