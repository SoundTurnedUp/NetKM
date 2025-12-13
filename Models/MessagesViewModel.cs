namespace NetKM.Models
{
    public class MessagesViewModel
    {
        public List<ConversationViewModel> Conversations { get; set; } = new List<ConversationViewModel>();
        public List<Message> Messages { get; set; } = new List<Message>();
        public string CurrentUserId { get; set; }
        public string CurrentUserAvatar { get; set; }
        public string ActiveUserId { get; set; }
        public string ActiveUserName { get; set; }
        public string ActiveUserAvatar { get; set; }
    }
}