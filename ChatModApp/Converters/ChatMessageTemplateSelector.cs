using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ChatModApp.Models;

namespace ChatModApp.Converters
{
    public class ChatMessageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Text { get; set; }
        public DataTemplate Emote { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            return item switch
            {
                EmoteFragment => Emote,
                TextFragment => Text,
                _ => Text
            };
        }
    }
}