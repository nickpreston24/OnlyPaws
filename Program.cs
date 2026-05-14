using CodeMechanic.Logging;
using CodeMechanic.Razorhat;
using CodeMechanic.Shargs;

var builder = WebApplication.CreateBuilder(args);

var logger = new SerilogLoggerName(".onlypaws", "onlypaws").CreateLogger();
var arguments = args.ToArgsMap();

// Add services to the container.
builder.Services.AddRazorPages();
builder.UseImportMap();

builder.Services.AddSingleton(logger);
builder.Services.AddSingleton(arguments);
builder.Services.AddSingleton<PetImagesService>();

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

app.Run();
