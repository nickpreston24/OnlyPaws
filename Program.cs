using System.Diagnostics;
using System.Text.Json;
using CodeMechanic.Diagnostics;
using CodeMechanic.FileSystem;
using CodeMechanic.Logging;
using CodeMechanic.Razorhat;
using CodeMechanic.Shargs;
using OnlyPaws.Pages;
using SerilogLoggerName = CodeMechanic.Logging.SerilogLoggerName;

var watch = Stopwatch.StartNew();
DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

var logger = new SerilogLoggerName(".onlypaws", "onlypaws").CreateLogger();


logger.Information("Testing the wwwroot from onlypaws");

var arguments = args.ToArgsMap();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton(logger);
builder.Services.AddSingleton(arguments);
builder.Services.AddSingleton<ImportMap>();
builder.Services.AddSingleton<PetUploaderService>();
builder.Services.AddSingleton<PetImagesService>();

builder.Services.AddScoped<OrganizationFinder>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
    .WithStaticAssets();

watch.LogTime(logger.Information);
app.Run();
