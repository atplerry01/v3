using Whycespace.Platform.UI.WhycePortal.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddHttpClient<GatewayClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Gateway:BaseUrl") ?? "http://localhost:5000");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();
