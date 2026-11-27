using LiveChat.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Repository 註冊區
builder.Services.AddSingleton(typeof(IRepository<>), typeof(JsonFileRepository<>));

// Service 註冊區（builder.Services.AddScoped<IXxxService, XxxService>();）

var app = builder.Build();

// 首次啟動時建立 App_Data
SeedDataInitializer.Initialize(app.Environment.ContentRootPath);

// Configure the HTTP request pipeline.
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
