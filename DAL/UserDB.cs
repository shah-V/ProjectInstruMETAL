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
                password: reader.GetString("PasswordHash"),
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
                return ReadUser(reader);
            }
            return null;
        }

        // 3. הוספת משתמש חדש (מחזירה את ה-ID שנוצר)
        public async Task<int> AddUserAsync(User user)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
                INSERT INTO Users (Username, Email, PasswordHash, RoleID, IsActive)
                VALUES (@username, @email, @password, @roleId, @isActive);
                SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", user.Username);
            cmd.Parameters.AddWithValue("@email", user.Email);
            cmd.Parameters.AddWithValue("@password", user.Password);
            cmd.Parameters.AddWithValue("@roleId", user.RoleId == 0 ? 2 : user.RoleId); // ברירת מחדל Student (2)
            cmd.Parameters.AddWithValue("@isActive", user.IsActive);

            object? result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
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

        // 6. בדיקת התחברות (שם משתמש וסיסמה)
        public async Task<User?> CheckLoginAsync(string username, string password)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
                SELECT u.UserID, u.Username, u.Email, u.PasswordHash, u.RoleID, u.IsActive, u.CreatedAt, r.RoleName
                FROM Users u
                JOIN Roles r ON u.RoleID = r.RoleID
                WHERE u.Username = @username AND u.PasswordHash = @password AND u.IsActive = TRUE;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@password", password);

            using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadUser(reader);
            }
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

            int rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        // 8. עדכון סיסמה
        public async Task<bool> UpdatePasswordAsync(int userId, string newPassword)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "UPDATE Users SET PasswordHash = @password WHERE UserID = @userId;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@password", newPassword);
            cmd.Parameters.AddWithValue("@userId", userId);

            int rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
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

            int rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        // 10. מחיקת משתמש לפי ID
        public async Task<bool> DeleteUserAsync(int userId)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "DELETE FROM Users WHERE UserID = @userId;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            int rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        // 11. ספירת משתמשים לפי תפקיד (GROUP BY עם JOIN)
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

            return result;
        }
    }
}