namespace Model
{
    // מחלקה שמייצגת משתמש במערכת (שורה בטבלת Users)
    public class User : BaseEntity
    {
        // שם המשתמש, ייחודי במערכת
        public string Username { get; set; } = string.Empty;

        // כתובת האימייל של המשתמש
        public string Email { get; set; } = string.Empty;

        // הסיסמה (בשלב זה נשמרת ללא הצפנה)
        public string Password { get; set; } = string.Empty;

        // מזהה התפקיד (מפתח זר לטבלת Roles)
        public int RoleId { get; set; }

        // שם התפקיד (נשלף באמצעות JOIN מול טבלת Roles)
        public string RoleName { get; set; } = string.Empty;

        // סמן האם המשתמש פעיל במערכת (Soft Delete)
        public bool IsActive { get; set; } = true;

        // תאריך ושעת הרשמה (מיוצר אוטומטית בבסיס הנתונים)
        public DateTime CreatedAt { get; set; }

        // בנאי ריק להמרה אוטומטית של בקשות HTTP
        public User() { }

        // בנאי ליצירת משתמש חדש לפני שמירה בבסיס הנתונים (ללא מזהה)
        public User(string username, string email, string password)
        {
            Username = username;
            Email = email;
            Password = password;
        }

        // בנאי למשתמש שנשלף מבסיס הנתונים (מעביר את ה-Id למחלקת הבסיס BaseEntity)
        public User(int id, string username, string email, string password,
                    int roleId, string roleName, bool isActive, DateTime createdAt)
            : base(id)
        {
            Username = username;
            Email = email;
            Password = password;
            RoleId = roleId;
            RoleName = roleName;
            IsActive = isActive;
            CreatedAt = createdAt;
        }

        public override string ToString() => $"{Username} ({RoleName})";
    }
}