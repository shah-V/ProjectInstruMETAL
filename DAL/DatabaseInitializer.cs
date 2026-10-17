using MySql.Data.MySqlClient;

namespace DAL
{
    public static class DatabaseInitializer
    {
        // מתודה ראשית להרצת כל שלבי האתחול
        public static async Task InitializeAsync(string connectionString, string serverConnectionString)
        {
            await CreateDatabaseAsync(serverConnectionString);
            await CreateTablesAsync(connectionString);
            await SeedRolesAsync(connectionString);
            await SeedUsersAsync(connectionString);
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

        // הכנסת 10 משתמשי בדיקה
        private static async Task SeedUsersAsync(string connectionString)
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();

            string checkSql = "SELECT COUNT(*) FROM Users;";
            using var checkCmd = new MySqlCommand(checkSql, conn);
            long count = (long)await checkCmd.ExecuteScalarAsync();

            if (count == 0)
            {
                string insertSql = @"
                    INSERT INTO Users (Username, Email, PasswordHash, RoleID, IsActive) VALUES
                    ('admin', 'admin@school.com', '123456', 4, TRUE),
                    ('teacher1', 'teacher1@school.com', '123456', 3, TRUE),
                    ('teacher2', 'teacher2@school.com', '123456', 3, TRUE),
                    ('student1', 'student1@school.com', '123456', 2, TRUE),
                    ('student2', 'student2@school.com', '123456', 2, TRUE),
                    ('student3', 'student3@school.com', '123456', 2, TRUE),
                    ('student4', 'student4@school.com', '123456', 2, TRUE),
                    ('student5', 'student5@school.com', '123456', 2, TRUE),
                    ('student6', 'student6@school.com', '123456', 2, TRUE),
                    ('student7', 'student7@school.com', '123456', 2, TRUE);";

                using var insertCmd = new MySqlCommand(insertSql, conn);
                await insertCmd.ExecuteNonQueryAsync();
            }
        }
    }
}