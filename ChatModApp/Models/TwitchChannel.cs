namespace ChatModApp.Models
{
    public class TwitchChannel : ITwitchChannel
    { 
        public string DisplayName { get; }
        public string Login { get; }

        public TwitchChannel(string displayName, string login)
        {
            DisplayName = displayName;
            Login = login;
        }
    }
}