namespace PasswordSafe.WPF.Models
{
    public class SecretEntry
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Login { get; set; }
        public string Secret { get; set; }
        public List<ExtraData> Extras { get; set; } = new();
    }
}
