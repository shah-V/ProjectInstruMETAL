using MySql.Data.MySqlClient;
using Model;

namespace DAL
{
    public class UserDB
    {
        private readonly string _connectionString;

        public UserDB(string connectionString)
        {
            _connectionString = connectionString;
        }

        // פעולה פרטית להמרת שורה מתוצאת השאילתה לאובייקט User
        private static User ReadUser(MySqlDataReader reader)
        {
            return new User(
                id: reader.GetInt32("UserID"),
                username: reader.GetString("Username"),
                email: reader.GetString("Email"),
                password: reader.GetString("PasswordHash"), // מכיל את מחרוזת הגיבוב המלאה
                roleId: reader.GetInt32("RoleID"),
                roleName: reader.GetString("RoleName"),
                isActive: reader.GetBoolean("IsActive"),
                createdAt: reader.GetDateTime("CreatedAt")
            );
        }

        // 1. שליפת כל המשתמשים כולל שם התפקיד (JOIN)
        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
                SELECT u.UserID, u.Username, u.Email, u.PasswordHash, u.RoleID, u.IsActive, u.CreatedAt, r.RoleName
                FROM Users u
                JOIN Roles r ON u.RoleID = r.RoleID;";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(ReadUser(reader));
            }

            Console.WriteLine($"[Users] Read {users.Count} users");
            return users;
        }

        // 2. שליפת משתמש בודד לפי ID
        public async Task<User?> GetUserByIdAsync(int id)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
                SELECT u.UserID, u.Username, u.Email, u.PasswordHash, u.RoleID, u.IsActive, u.CreatedAt, r.RoleName
                FROM Users u
                JOIN Roles r ON u.RoleID = r.RoleID
                WHERE u.UserID = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                Console.WriteLine($"[Users] Read user by id {id}");
                return ReadUser(reader);
            }

            Console.WriteLine($"[Users] No user with id {id}");
            return null;
        }

        // 3. הוספת משתמש חדש - שמירת גיבוב סיסמה בעזרת BCrypt
        public async Task<int> AddUserAsync(User user)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            int roleId = user.RoleId == 0 ? 2 : user.RoleId; // ברירת מחדל Student (2)

            string sql = @"
                INSERT INTO Users (Username, Email, PasswordHash, RoleID, IsActive)
                VALUES (@username, @email, @password, @roleId, @isActive);
                SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", user.Username);
            cmd.Parameters.AddWithValue("@email", user.Email);

            // גיבוב הסיסמה לפני שמירתה בבסיס הנתונים
            cmd.Parameters.AddWithValue("@password", BCrypt.Net.BCrypt.HashPassword(user.Password));

            cmd.Parameters.AddWithValue("@roleId", roleId);
            cmd.Parameters.AddWithValue("@isActive", user.IsActive);

            object? result = await cmd.ExecuteScalarAsync();
            int newId = Convert.ToInt32(result);

            string roleName = string.IsNullOrWhiteSpace(user.RoleName) ? $"RoleID {roleId}" : user.RoleName;
            Console.WriteLine($"[Register] New user added: {user.Username} (id {newId}, role {roleName})");

            return newId;
        }

        // 4. בדיקה אם שם משתמש כבר תפוס
        public async Task<bool> UsernameExistsAsync(string username)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "SELECT COUNT(*) FROM Users WHERE Username = @username;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", username);

            long count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        // 5. בדיקה אם אימייל כבר תפוס
        public async Task<bool> EmailExistsAsync(string email)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "SELECT COUNT(*) FROM Users WHERE Email = @email;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@email", email);

            long count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        // 6. בדיקת התחברות - אימות בעזרת BCrypt.Verify מול הגיבוב השמור
        public async Task<User?> CheckLoginAsync(string username, string password)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            // שולפים את המשתמש לפי שם משתמש בלבד (ואם הוא פעיל)
            string sql = @"
                SELECT u.UserID, u.Username, u.Email, u.PasswordHash, u.RoleID, u.IsActive, u.CreatedAt, r.RoleName
                FROM Users u
                JOIN Roles r ON u.RoleID = r.RoleID
                WHERE u.Username = @username AND u.IsActive = TRUE;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", username);

            using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                Console.WriteLine($"[Login] Failed, username not found: {username}");
                return null;
            }

            User user = ReadUser(reader);

            // בדיקה אם הסיסמה שהוקלדה תואמת לגיבוב השמור (כולל חילוץ המלח)
            if (BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                Console.WriteLine($"[Login] Success: {user.Username} ({user.RoleName})");
                return user;
            }

            Console.WriteLine($"[Login] Failed, wrong password: {username}");
            return null;
        }

        // 7. עדכון תפקיד משתמש
        public async Task<bool> UpdateUserRoleAsync(int userId, int newRoleId)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "UPDATE Users SET RoleID = @roleId WHERE UserID = @userId;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@roleId", newRoleId);
            cmd.Parameters.AddWithValue("@userId", userId);

            bool updated = await cmd.ExecuteNonQueryAsync() > 0;
            if (updated)
            {
                Console.WriteLine($"[Users] Role of user {userId} changed to {newRoleId}");
            }
            else
            {
                Console.WriteLine($"[Users] Role change failed: user {userId} or role {newRoleId} not found");
            }
            return updated;
        }

        // 8. עדכון סיסמה - גיבוב הסיסמה החדשה בעזרת BCrypt
        public async Task<bool> UpdatePasswordAsync(int userId, string newPassword)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "UPDATE Users SET PasswordHash = @password WHERE UserID = @userId;";
            using var cmd = new MySqlCommand(sql, conn);

            // גיבוב הסיסמה החדשה
            cmd.Parameters.AddWithValue("@password", BCrypt.Net.BCrypt.HashPassword(newPassword));
            cmd.Parameters.AddWithValue("@userId", userId);

            bool updated = await cmd.ExecuteNonQueryAsync() > 0;
            if (updated)
            {
                Console.WriteLine($"[Users] Password of user {userId} updated");
            }
            else
            {
                Console.WriteLine($"[Users] Password update failed: no user with id {userId}");
            }
            return updated;
        }

        // 9. עדכון אימייל
        public async Task<bool> UpdateEmailAsync(int userId, string newEmail)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "UPDATE Users SET Email = @email WHERE UserID = @userId;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@email", newEmail);
            cmd.Parameters.AddWithValue("@userId", userId);

            bool updated = await cmd.ExecuteNonQueryAsync() > 0;
            if (updated)
            {
                Console.WriteLine($"[Users] Email of user {userId} changed to {newEmail}");
            }
            else
            {
                Console.WriteLine($"[Users] Email update failed: no user with id {userId}");
            }
            return updated;
        }

        // 10. מחיקה קשה של משתמש לפי ID (Hard Delete)
        public async Task<bool> DeleteUserAsync(int userId)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "DELETE FROM Users WHERE UserID = @userId;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            bool deleted = await cmd.ExecuteNonQueryAsync() > 0;
            if (deleted)
            {
                Console.WriteLine($"[Users] User {userId} deleted");
            }
            else
            {
                Console.WriteLine($"[Users] Delete failed: no user with id {userId}");
            }
            return deleted;
        }

        // 11. מחיקה רכה של משתמש (Soft Delete) - שינוי סטטוס ל-IsActive = FALSE
        public async Task<bool> SoftDeleteUserAsync(int userId)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "UPDATE Users SET IsActive = FALSE WHERE UserID = @userId;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            bool deactivated = await cmd.ExecuteNonQueryAsync() > 0;
            if (deactivated)
            {
                Console.WriteLine($"[Users] User {userId} deactivated (soft deleted)");
            }
            else
            {
                Console.WriteLine($"[Users] Soft delete failed: no user with id {userId}");
            }
            return deactivated;
        }

        // 12. ספירת משתמשים לפי תפקיד (GROUP BY עם JOIN)
        public async Task<Dictionary<string, int>> GetCountByRoleAsync()
        {
            var result = new Dictionary<string, int>();

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
                SELECT r.RoleName, COUNT(u.UserID) AS Total
                FROM Users u
                JOIN Roles r ON u.RoleID = r.RoleID
                GROUP BY r.RoleName;";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string roleName = reader.GetString("RoleName");
                int count = reader.GetInt32("Total");
                result[roleName] = count;
            }

            Console.WriteLine($"[Users] Read user counts for {result.Count} roles");
            return result;
        }
    }
}