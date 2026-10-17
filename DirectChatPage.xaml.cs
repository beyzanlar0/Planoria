using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Planoria.Data;
using Planoria.Models;

namespace Planoria
{
    public partial class DirectChatPage : ContentPage
    {
        private readonly int _conversationId;
        private readonly int _me;
        private int _otherUserId;
        private readonly ObservableCollection<ChatMessageItem> _messages = new();

        public DirectChatPage(int conversationId)
        {
            InitializeComponent();
            _conversationId = conversationId;
            _me = Preferences.Get("CurrentUserId", 0);
            MessagesCollectionView.ItemsSource = _messages;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadAll();
        }

        private async Task LoadAll()
        {
            using var context = new AppDbContext();

            var conv = await context.DirectConversations
                .Include(c => c.Initiator)
                .Include(c => c.Recipient)
                .FirstOrDefaultAsync(c => c.Id == _conversationId);

            if (conv == null || (conv.InitiatorId != _me && conv.RecipientId != _me))
            {
                await Navigation.PopToRootAsync();
                return;
            }

            var other = conv.InitiatorId == _me ? conv.Recipient : conv.Initiator;
            _otherUserId = other?.Id ?? 0;
            NameLabel.Text = other?.Username ?? "?";

            var msgs = await context.DirectMessages
                .Where(m => m.ConversationId == _conversationId)
                .Include(m => m.Sender)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            _messages.Clear();
            foreach (var m in msgs)
            {
                var avatar = AvatarCatalog.Get(m.Sender?.AvatarId ?? 0);
                _messages.Add(new ChatMessageItem
                {
                    SenderName = m.Sender?.Username ?? "?",
                    Content = m.Content,
                    TimeText = m.SentAt.ToLocalTime().ToString("HH:mm"),
                    IsMine = m.SenderId == _me,
                    ShowSenderName = false,
                    AvatarEmoji = avatar.Emoji,
                    AvatarColor = avatar.Color
                });
            }

            ApplyState(conv, msgs.Count(m => m.SenderId == _me));

            if (_messages.Any())
                MessagesCollectionView.ScrollTo(_messages.Last(), animate: false);
        }

        private void ApplyState(DirectConversation conv, int myMessageCount)
        {
            bool iAmInitiator = conv.InitiatorId == _me;

            InputBar.IsVisible = false;
            RequestBar.IsVisible = false;
            InfoLabel.IsVisible = false;

            switch (conv.Status)
            {
                case ConversationStatus.Accepted:
                    InputBar.IsVisible = true;
                    break;

                case ConversationStatus.Pending:
                    if (iAmInitiator)
                    {
                        if (myMessageCount == 0)
                        {
                            InputBar.IsVisible = true;
                            InfoLabel.Text = "Arkadaþ olmadýðýnýz için bu bir mesaj isteði olarak gönderilecek. Kabul edilene kadar tek mesaj gönderebilirsin.";
                        }
                        else
                        {
                            InfoLabel.Text = "Mesaj isteðin gönderildi. Kabul edilince sohbete devam edebilirsin.";
                        }
                    }
                    else
                    {
                        RequestBar.IsVisible = true;
                        InfoLabel.Text = $"{NameLabel.Text} sana mesaj isteði gönderdi.";
                    }
                    InfoLabel.IsVisible = true;
                    break;

                case ConversationStatus.Declined:
                    InfoLabel.Text = iAmInitiator ? "Mesaj isteðin reddedildi." : "Bu isteði reddettin.";
                    InfoLabel.IsVisible = true;
                    break;
            }
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            string text = MessageEntry.Text;
            if (string.IsNullOrWhiteSpace(text)) return;

            string? error = null;

            using (var context = new AppDbContext())
            {
                var conv = await context.DirectConversations.FirstOrDefaultAsync(c => c.Id == _conversationId);
                if (conv == null || (conv.InitiatorId != _me && conv.RecipientId != _me)) return;

                int other = conv.InitiatorId == _me ? conv.RecipientId : conv.InitiatorId;

                if (await FriendshipHelper.IsBlockedAsync(context, _me, other))
                {
                    error = "Bu kullanýcýyla mesajlaþamazsýn.";
                }
                else
                {
                    if (conv.Status != ConversationStatus.Accepted &&
                        await FriendshipHelper.AreFriendsAsync(context, _me, other))
                    {
                        conv.Status = ConversationStatus.Accepted;
                    }

                    if (conv.Status == ConversationStatus.Declined)
                    {
                        error = "Bu sohbete mesaj gönderemezsin.";
                    }
                    else if (conv.Status == ConversationStatus.Pending)
                    {
                        if (conv.InitiatorId != _me)
                            error = "Mesaj göndermek için önce isteði kabul etmelisin.";
                        else if (await context.DirectMessages.AnyAsync(m => m.ConversationId == conv.Id && m.SenderId == _me))
                            error = "Mesaj isteðin kabul edilene kadar yeni mesaj gönderemezsin.";
                    }
                }

                if (error == null)
                {
                    context.DirectMessages.Add(new DirectMessage
                    {
                        ConversationId = conv.Id,
                        SenderId = _me,
                        Content = text.Trim()
                    });
                    await context.SaveChangesAsync();
                }
            }

            if (error != null)
            {
                await DisplayAlert("Uyarý", error, "Tamam");
                await LoadAll();
                return;
            }

            MessageEntry.Text = string.Empty;
            await LoadAll();
        }

        private async void OnAcceptClicked(object sender, EventArgs e)
        {
            using (var context = new AppDbContext())
            {
                var conv = await context.DirectConversations
                    .FirstOrDefaultAsync(c => c.Id == _conversationId && c.RecipientId == _me);
                if (conv != null && conv.Status != ConversationStatus.Accepted)
                {
                    conv.Status = ConversationStatus.Accepted;
                    await context.SaveChangesAsync();
                }
            }
            await LoadAll();
        }

        private async void OnDeclineClicked(object sender, EventArgs e)
        {
            using (var context = new AppDbContext())
            {
                var conv = await context.DirectConversations
                    .FirstOrDefaultAsync(c => c.Id == _conversationId && c.RecipientId == _me);
                if (conv != null)
                {
                    conv.Status = ConversationStatus.Declined;
                    await context.SaveChangesAsync();
                }
            }
            await Navigation.PopAsync();
        }

        private async void OnHeaderTapped(object sender, EventArgs e)
        {
            if (_otherUserId != 0)
                await Navigation.PushAsync(new MemberProfilePage(_otherUserId));
        }
    }
}