using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MiLoadWeb;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOrigin",
        policyBuilder => policyBuilder.WithOrigins() // 允许特定来源
            .AllowAnyMethod() // 允许任何HTTP方法
            .AllowAnyHeader()); // 允许任何HTTP头
});


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowOrigin");

app.UseHttpsRedirection();

var pluginManager = new PluginManager();

app.MapGet("unturnedPlugins/{id}", async (string id) =>
{
    // 这个是文件下载，（利用DownloadPool）

    var path = pluginManager.GetPoor()?.Pop(id);

    return path != null ? Results.File(await File.ReadAllBytesAsync(path), "application/octet-stream") : Results.NotFound();
});

app.MapGet("unturnedPlugins/pluginGet/all", async () =>
{
    var downloadIds = new ConcurrentDictionary<string, List<string?>>()
    {
        ["plugins"] = [],
        ["libs"] = []
    };
    var poor = pluginManager.GetPoor();
    await Parallel.ForEachAsync(pluginManager.GetPlugins() ?? throw new InvalidOperationException(), (item, _) =>
    {
        if (item.PluginLocalPath != null) downloadIds["plugins"].Add(poor?.Push(item.PluginLocalPath));
        return new ValueTask(Task.CompletedTask);
    });
    await Parallel.ForEachAsync(pluginManager.GetLibs() ?? throw new InvalidOperationException(), (item, token) =>
    {
        if (item.LibLocalPath != null) downloadIds["libs"].Add(poor?.Push(item.LibLocalPath));
        return new ValueTask(Task.CompletedTask);
    });
    return Results.Ok(JsonConvert.SerializeObject(downloadIds));
});

app.MapGet("unturnedPlugins/pluginGet/{id}", async (string id) =>
{
    var downloadIds = new ConcurrentDictionary<string, List<string?>>()
    {
        ["plugins"] = [],
        ["libs"] = []
    };
    var poor = pluginManager.GetPoor();
    var plugin = (pluginManager.GetPlugins() ?? throw new InvalidOperationException()).FirstOrDefault(x => x.PluginId == id);
    if (plugin == null) return Results.Ok();
    downloadIds["plugins"].Add(poor?.Push(plugin.PluginLocalPath ?? throw new InvalidOperationException()));
    await Parallel.ForEachAsync(plugin.PluginLibIds ?? throw new InvalidOperationException(), (s, _) =>
    {
        downloadIds["libs"].Add(
            poor?.Push((pluginManager.GetLibs() ?? throw new InvalidOperationException()).First(x => x.LibId == s).LibLocalPath ?? throw new InvalidOperationException())
            );
        return new ValueTask(Task.CompletedTask);
    });
    return Results.Ok(JsonConvert.SerializeObject(downloadIds));
});

app.Run();