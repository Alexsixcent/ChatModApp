namespace ChatModApp.Models
{
    public class TextFragment:IMessageFragment
    {
        public string Text { get; set; }

        public TextFragment(string text)
        {
            Text = text;
        }
    }
}