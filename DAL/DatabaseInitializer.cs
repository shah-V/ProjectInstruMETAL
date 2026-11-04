using MySql.Data.MySqlClient;

namespace DAL
{
    public static class DatabaseInitializer
    {
        // מתודה ראשית להרצת כל שלבי האתחול
        public static async Task InitializeAsync(string connectionString, string serverConnectionString)
        {
            // 1. יצירת בסיס הנתונים מול השרת הכללי
            await CreateDatabaseAsync(serverConnectionString);

            // 2. הבטחה שמחרוזת החיבור לשלבים הבאים תכיל את instruMETALDB
            string dbConnectionString = connectionString.Contains("Database=", StringComparison.OrdinalIgnoreCase)
                ? connectionString
                : connectionString.TrimEnd(';') + ";Database=instruMETALDB;";

            // 3. הרצת יצירת הטבלאות והזנת נתוני הבדיקה בתוך instruMETALDB
            await CreateTablesAsync(dbConnectionString);
            await SeedRolesAsync(dbConnectionString);
            await SeedUsersAsync(dbConnectionString);
        }

        // יצירת בסיס הנתונים instruMETALDB במידה ואינו קיים בשרת
        private static async Task CreateDatabaseAsync(string serverConnectionString)
        {
            using var conn = new MySqlConnection(serverConnectionString);
            await conn.OpenAsync();

            string sql = "CREATE DATABASE IF NOT EXISTS instruMETALDB;";
            using var cmd = new MySqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // יצירת 7 הטבלאות של המערכת לפי סדר התלויות (מפתח זר)
        private static async Task CreateTablesAsync(string connectionString)
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();

            // מערך שאילתות יצירה - כל טבלה בנפרד
            string[] createTableQueries = new string[]
            {
                // 1. טבלת תפקידים (Roles)
                @"CREATE TABLE IF NOT EXISTS Roles (
                    RoleID INT AUTO_INCREMENT PRIMARY KEY,
                    RoleName VARCHAR(50) NOT NULL UNIQUE
                );",

                // 2. טבלת משתמשים (Users) - כולל IsActive ו-CreatedAt
                @"CREATE TABLE IF NOT EXISTS Users (
                    UserID INT AUTO_INCREMENT PRIMARY KEY,
                    Username VARCHAR(50) NOT NULL,
                    Email VARCHAR(100) NOT NULL UNIQUE,
                    PasswordHash VARCHAR(255) NOT NULL,
                    RoleID INT NOT NULL,
                    IsActive BOOLEAN DEFAULT TRUE,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
                );",

                // 3. טבלת להקות (Bands)
                @"CREATE TABLE IF NOT EXISTS Bands (
                    BandID INT AUTO_INCREMENT PRIMARY KEY,
                    BandName VARCHAR(100) NOT NULL,
                    Genre VARCHAR(50),
                    ImageUrl VARCHAR(255)
                );",

                // 4. טבלת נגנים (Musicians) - מקושרת ל-Bands
                @"CREATE TABLE IF NOT EXISTS Musicians (
                    MusicianID INT AUTO_INCREMENT PRIMARY KEY,
                    BandID INT NOT NULL,
                    FullName VARCHAR(100) NOT NULL,
                    RoleInBand VARCHAR(50),
                    ImageUrl VARCHAR(255),
                    FOREIGN KEY (BandID) REFERENCES Bands(BandID) ON DELETE CASCADE
                );",

                // 5. טבלת שירים (Songs) - מקושרת ל-Bands
                @"CREATE TABLE IF NOT EXISTS Songs (
                    SongID INT AUTO_INCREMENT PRIMARY KEY,
                    BandID INT NOT NULL,
                    SongName VARCHAR(100) NOT NULL,
                    ReleaseYear INT,
                    FullAudioUrl VARCHAR(255),
                    FOREIGN KEY (BandID) REFERENCES Bands(BandID) ON DELETE CASCADE
                );",

                // 6. טבלת כלי נגינה (Instruments)
                @"CREATE TABLE IF NOT EXISTS Instruments (
                    InstrumentID INT AUTO_INCREMENT PRIMARY KEY,
                    InstrumentName VARCHAR(50) NOT NULL
                );",

                // 7. טבלת שיעורים (SongLessons) - טבלת איחוד
                @"CREATE TABLE IF NOT EXISTS SongLessons (
                    LessonID INT AUTO_INCREMENT PRIMARY KEY,
                    SongID INT NOT NULL,
                    InstrumentID INT NOT NULL,
                    MusicianID INT NULL,
                    CreatedByUserID INT NOT NULL,
                    TabsDataJson LONGTEXT,
                    MinusAudioUrl VARCHAR(255),
                    Difficulty VARCHAR(20),
                    IsPublished BOOLEAN DEFAULT TRUE,
                    FOREIGN KEY (SongID) REFERENCES Songs(SongID) ON DELETE CASCADE,
                    FOREIGN KEY (InstrumentID) REFERENCES Instruments(InstrumentID),
                    FOREIGN KEY (MusicianID) REFERENCES Musicians(MusicianID),
                    FOREIGN KEY (CreatedByUserID) REFERENCES Users(UserID)
                );"
            };

            // הרצת כל שאילתה בנפרד
            foreach (var query in createTableQueries)
            {
                using var cmd = new MySqlCommand(query, conn);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        // הכנסת תפקידי ברירת מחדל
        private static async Task SeedRolesAsync(string connectionString)
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();

            string checkSql = "SELECT COUNT(*) FROM Roles;";
            using var checkCmd = new MySqlCommand(checkSql, conn);
            long count = (long)await checkCmd.ExecuteScalarAsync();

            if (count == 0)
            {
                string insertSql = "INSERT INTO Roles (RoleName) VALUES ('Guest'), ('Student'), ('Teacher'), ('Admin');";
                using var insertCmd = new MySqlCommand(insertSql, conn);
                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        // הכנסת משתמשי בדיקה ראשוניים עם סיסמאות מגובבות ב-BCrypt
        private static async Task SeedUsersAsync(string connectionString)
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();

            string checkSql = "SELECT COUNT(*) FROM Users;";
            using var checkCmd = new MySqlCommand(checkSql, conn);
            long count = (long)await checkCmd.ExecuteScalarAsync();

            if (count == 0)
            {
                // הגדרת משתמשי הבדיקה כולל admin, teacher ו-user לפי המדריך
                var usersToSeed = new[]
                {
                    // משתמשי ליבה לפי הנחיות המדריך (עם סיסמה זהה לשם המשתמש)
                    new { Username = "admin", Email = "admin@instrumental.com", PlainPassword = "admin", RoleId = 4 },     // Admin
                    new { Username = "teacher", Email = "teacher@instrumental.com", PlainPassword = "teacher", RoleId = 3 }, // Teacher
                    new { Username = "user", Email = "user@instrumental.com", PlainPassword = "user", RoleId = 2 },         // Student

                    // משתמשי בדיקה נוספים
                    new { Username = "teacher1", Email = "teacher1@school.com", PlainPassword = "123456", RoleId = 3 },
                    new { Username = "teacher2", Email = "teacher2@school.com", PlainPassword = "123456", RoleId = 3 },
                    new { Username = "student1", Email = "student1@school.com", PlainPassword = "123456", RoleId = 2 },
                    new { Username = "student2", Email = "student2@school.com", PlainPassword = "123456", RoleId = 2 },
                    new { Username = "student3", Email = "student3@school.com", PlainPassword = "123456", RoleId = 2 },
                    new { Username = "student4", Email = "student4@school.com", PlainPassword = "123456", RoleId = 2 },
                    new { Username = "student5", Email = "student5@school.com", PlainPassword = "123456", RoleId = 2 }
                };

                string insertSql = @"
                    INSERT INTO Users (Username, Email, PasswordHash, RoleID, IsActive)
                    VALUES (@username, @email, @passwordHash, @roleId, TRUE);";

                foreach (var u in usersToSeed)
                {
                    using var cmd = new MySqlCommand(insertSql, conn);
                    cmd.Parameters.AddWithValue("@username", u.Username);
                    cmd.Parameters.AddWithValue("@email", u.Email);
                    // גיבוב הסיסמה באמצעות BCrypt לפני השמירה במסד הנתונים
                    cmd.Parameters.AddWithValue("@passwordHash", BCrypt.Net.BCrypt.HashPassword(u.PlainPassword));
                    cmd.Parameters.AddWithValue("@roleId", u.RoleId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}