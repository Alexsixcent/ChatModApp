namespace ChatModApp.Models.Chat.Fragments;

public class TextFragment:IMessageFragment
{
    public string Text { get; set; }

    public TextFragment(string text)
    {
        Text = text;
    }
}