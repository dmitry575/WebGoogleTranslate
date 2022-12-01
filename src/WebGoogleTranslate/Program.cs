using GoogleTranslate.Translate;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var configuration = builder.Build();

builder.WebHost.UseKestrel(options => options.AddServerHeader = false);

builder.Services.AddScoped<IConvert, Convert>();
builder.Services.AddScoped<IGoogleTranslate, GoogleTranslate>();

app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo Api v1");
});
app.MapPost("/translate", (string text, bool isHtml) => );

app.Run();
