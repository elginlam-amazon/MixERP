using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MixERP.Net.Framework.Web;
using MixERP.Net.FrontEnd.Hubs;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddSignalR();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.Use(async (context, next) =>
{
    HttpContext.Current = new HttpContext(context);
    try
    {
        await next();
    }
    finally
    {
        HttpContext.Current = null;
    }
});
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();
app.MapMixErpHubs();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program { }
