namespace KikiAZZoo.Models.ViewModels
{
    public class AuthViewModel
    {
        public LoginViewModel Login { get; set; } = new();
        public RegisterViewModel Register { get; set; } = new();
    }
}
