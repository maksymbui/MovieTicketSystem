using System.Text.Json;
using MovieTickets.Core.Entities;
using MovieTickets.Core.Logic;

namespace MovieTickets.Api.Services;

public sealed class JsonUserRepository : IUserRepository
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public JsonUserRepository(IWebHostEnvironment env)
    {
        var storage = Path.Combine(env.ContentRootPath, "../../../storage");
        Directory.CreateDirectory(storage);
        _filePath = Path.Combine(storage, "users.json");
        if (!File.Exists(_filePath)) File.WriteAllText(_filePath, "[]");
    }

    public async Task<bool> AnyAsync(CancellationToken ct = default)
    {
        var list = await ReadAsync(ct);
        return list.Count > 0;
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var list = await ReadAsync(ct);
        return list.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<User?> FindByIdAsync(string id, CancellationToken ct = default)
    {
        var list = await ReadAsync(ct);
        return list.FirstOrDefault(u => u.Id == id);
    }

    public async Task<User> CreateAsync(User user, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var list = await ReadAsync(ct);
            list.Add(user);
            await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(list, _json), ct);
            return user;
        }
        finally { _gate.Release(); }
    }

    private async Task<List<User>> ReadAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            using var fs = File.OpenRead(_filePath);
            var users = await JsonSerializer.DeserializeAsync<List<User>>(fs, cancellationToken: ct) ?? new();
            return users;
        }
        finally { _gate.Release(); }
    }
}
