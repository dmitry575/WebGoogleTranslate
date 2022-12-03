using GoogleTranslate.Translate;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using WebGoogleTranslate.Common;
using WebGoogleTranslate.Common.Impl;
using WebGoogleTranslate.Translate;
using WebGoogleTranslate.Translate.Impl;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables("ASPNETCORE_");

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

var configuration = builder.Configuration;

builder.Services.AddScoped<IConvertFactory, ConvertFactory>();
builder.Services.AddScoped<IGoogleTranslate, WebGoogleTranslate.Translate.Impl.GoogleTranslate>();
builder.Services.AddScoped<IGoogleTranslateRequest, GoogleTranslateRequest>();
builder.Services.Configure<WebGoogleTranslate.Config.Configuration>(configuration.GetSection("config").Bind);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo Api v1"); });

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapPost("/translate", (string text, string fromLang, string toLang,  bool isHtml, bool convert, IGoogleTranslate translate) =>
{
    var result = translate.Translate(text, fromLang, toLang, isHtml,convert);
    Results.Ok(result);
});

app.Run();
