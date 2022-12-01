using System.Configuration;
using GoogleTranslate.Common;
using GoogleTranslate.Translate;
using WebGoogleTranslate.Common;
using WebGoogleTranslate.Common.Impl;
using WebGoogleTranslate.Translate;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.WebHost.UseKestrel(options => options.AddServerHeader = false);

builder.WebHost.ConfigureLogging((context, loggingBuilder) =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddConfiguration(context.Configuration);
    loggingBuilder.AddLog4Net();

    if (context.HostingEnvironment.IsDevelopment())
    {
        loggingBuilder.AddConsole();
    }
});


builder.Services.AddScoped<IConvertFactory, ConvertFactory>();
builder.Services.AddScoped<IGoogleTranslate, WebGoogleTranslate.Translate.Impl.GoogleTranslate>();
builder.Services.Configure<Configuration>(_configuration.GetSection("RabbitMQConfiguration").Bind);
app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo Api v1");
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapPost("/translate", (string text, bool isHtml, IGoogleTranslate translate) =>
{
    var result = translate.Translate();
    Results.Ok();
});

app.Run();
