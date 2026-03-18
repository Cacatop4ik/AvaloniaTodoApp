using System.Collections.Generic;
using System.Threading.Tasks;
using TodoApp.Models;

namespace TodoApp.Services;

public interface IDataService
{
    Task<List<TodoItem>> LoadTasksAsync();
    Task SaveTasksAsync(IEnumerable<TodoItem> tasks);
}