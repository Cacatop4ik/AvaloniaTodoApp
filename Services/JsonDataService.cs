using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TodoApp.Models;

namespace TodoApp.Services;

public class JsonDataService : IDataService
{
    private readonly string _filePath;

    public JsonDataService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TodoApp");

        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "tasks.json");
    }

    public async Task<List<TodoItem>> LoadTasksAsync()
    {
        if (!File.Exists(_filePath))
            return new List<TodoItem>();

        await using var stream = File.OpenRead(_filePath);

        var tasks = await JsonSerializer.DeserializeAsync<List<TodoItem>>(stream);

        return tasks ?? new List<TodoItem>();
    }

    public async Task SaveTasksAsync(IEnumerable<TodoItem> tasks)
    {
        await using var stream = File.Create(_filePath);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        await JsonSerializer.SerializeAsync(stream, tasks, options);
    }
}