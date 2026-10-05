using Microsoft.EntityFrameworkCore;
using ParrotAgent.Database;
using Microsoft.AspNetCore.Authentication.Cookies;
using ParrotAgent.Services;
using Hangfire;
using ParrotAgent.Utilities;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "ParrotAgent";
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddScoped<IJobQueue, HangfireJobQueue>();
builder.Services.AddScoped<IDocumentProcessor, DocumentProcessor>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IEmbedder, Embedder>();
builder.Services.AddScoped<ILLM, LLM>();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Account");
});

var connectionString = builder.Configuration["ExternalDbConnection"]??"";;

builder.Services. AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString, npgOpts => npgOpts.UseVector()));
builder.Services.AddHangfire(configuration => configuration 
.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
.UseSimpleAssemblyNameTypeSerializer()
.UseRecommendedSerializerSettings()
.UsePostgreSqlStorage(options =>
{
    options.UseNpgsqlConnection(connectionString);
}
));

builder.Services.AddHangfireServer();



var app = builder.Build();


app.UseHangfireDashboard();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapControllers();

app.Run();
