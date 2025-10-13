using MovieTickets.Core.Entities;

namespace MovieTickets.Core.Logic;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> FindByIdAsync(string id, CancellationToken ct = default);
    Task<User> CreateAsync(User user, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
}
