using Microsoft.EntityFrameworkCore;
using VectorRagDemo.BLL;
using VectorRagDemo.DAL;
using VectorRagDemo.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<VectorDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<LogboekDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LogboekConnection")));

builder.Services.AddScoped<ChatService>(provider =>
{
    var context = provider.GetRequiredService<VectorDbContext>();
    var logboekContext = provider.GetRequiredService<LogboekDbContext>();
    return new ChatService(context, logboekContext, builder.Configuration);
});

// Register LinkPreviewService with HttpClient
builder.Services.AddHttpClient<LinkPreviewService>();

builder.Services.AddGrpc();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGrpcService<GrpcService>();

app.Run();