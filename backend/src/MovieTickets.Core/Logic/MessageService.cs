using MovieTickets.Core.Entities;

namespace MovieTickets.Core.Logic;

public sealed class MessageService
{

    public IReadOnlyList<Message> GetAllMessagesForUser(string userId)
    {
        return DataStore.Messages
            .Where(m => m.ToUserId == userId)
            .OrderByDescending(m => m.SentUtc)
            .ToList();
    }

    public void SendMessageToUser(string email, string content)
    {
        var user = DataStore.GetUserByEmail(email);
        if (user == null)
        {
            Console.WriteLine($"User with email {email} not found. Message not sent.");
            return;
        }
        var message = new Message
        {
            ToUserId = user.Id,
            Content = content
        };

        DataStore.AddMessage(message);

    }
}