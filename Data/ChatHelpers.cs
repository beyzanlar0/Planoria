using Microsoft.EntityFrameworkCore;
using Planoria.Models;

namespace Planoria.Data
{

    public static class GroupAuth
    {
        public static async Task<GroupRole?> GetRoleAsync(AppDbContext context, int groupId, int userId)
        {
            var gm = await context.GroupMembers
                .FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserId == userId);
            return gm?.Role;
        }
    }
    public static class FriendshipHelper
    {
        public static Task<bool> AreFriendsAsync(AppDbContext context, int a, int b) =>
            context.FriendRequests.AnyAsync(fr =>
                fr.Status == FriendRequestStatus.Accepted &&
                ((fr.SenderId == a && fr.ReceiverId == b) || (fr.SenderId == b && fr.ReceiverId == a)));

        public static Task<bool> IsBlockedAsync(AppDbContext context, int a, int b) =>
            context.FriendRequests.AnyAsync(fr =>
                fr.Status == FriendRequestStatus.Blocked &&
                ((fr.SenderId == a && fr.ReceiverId == b) || (fr.SenderId == b && fr.ReceiverId == a)));
    }

    public static class DirectChatService
    {
        // Başarılıysa ConversationId döner, değilse kullanıcıya gösterilecek hata mesajı.
        public static async Task<(int? ConversationId, string? Error)> OpenOrCreateAsync(int me, int other)
        {
            if (me == other) return (null, "Kendinle mesajlaşamazsın.");

            using var context = new AppDbContext();

            if (await FriendshipHelper.IsBlockedAsync(context, me, other))
                return (null, "Bu kullanıcıyla mesajlaşamazsın.");

            bool friends = await FriendshipHelper.AreFriendsAsync(context, me, other);

            var conv = await context.DirectConversations.FirstOrDefaultAsync(c =>
                (c.InitiatorId == me && c.RecipientId == other) ||
                (c.InitiatorId == other && c.RecipientId == me));

            if (conv == null)
            {
                conv = new DirectConversation
                {
                    InitiatorId = me,
                    RecipientId = other,
                    Status = friends ? ConversationStatus.Accepted : ConversationStatus.Pending
                };
                context.DirectConversations.Add(conv);
                await context.SaveChangesAsync();
                return (conv.Id, null);
            }

            if (friends && conv.Status != ConversationStatus.Accepted)
            {
                conv.Status = ConversationStatus.Accepted;
                await context.SaveChangesAsync();
            }
            else if (conv.Status == ConversationStatus.Declined)
            {
                if (conv.RecipientId == me)
                {
                    // İsteği reddeden taraf kendisi yazmak isterse sohbet açılır
                    conv.Status = ConversationStatus.Accepted;
                    await context.SaveChangesAsync();
                }
                else
                {
                    return (null, "Bu kişi mesaj isteğini reddetti.");
                }
            }

            return (conv.Id, null);
        }
    }
}