using Model;

namespace View.Services
{
    public class UserSession
    {
        // פרטי המשתמש המחובר - קריאה חופשית, שינוי רק דרך מתודות המחלקה (כמוס)
        public int UserId { get; private set; }
        public string Username { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public int RoleId { get; private set; }
        public string RoleName { get; private set; } = string.Empty;

        // תכונה מחושבת לבדיקה אם המשתמש מחובר
        public bool IsLoggedIn => UserId > 0;

        // אירוע המודיע לרכיבי האתר (כמו הסרגל העליון) שהמצב השתנה
        public event Action? OnChange;

        public void Login(User user)
        {
            UserId = user.Id;
            Username = user.Username;
            Email = user.Email;
            RoleId = user.RoleId;
            RoleName = user.RoleName;

            OnChange?.Invoke();
        }

        public void Logout()
        {
            UserId = 0;
            Username = string.Empty;
            Email = string.Empty;
            RoleId = 0;
            RoleName = string.Empty;

            OnChange?.Invoke();
        }
    }
}