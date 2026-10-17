using Planoria.Models;

namespace Planoria
{
    public class ChatBubbleSelector : DataTemplateSelector
    {
        public DataTemplate? MineTemplate { get; set; }
        public DataTemplate? OtherTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            var msg = (ChatMessageItem)item;
            return (msg.IsMine ? MineTemplate : OtherTemplate)!;
        }
    }
}