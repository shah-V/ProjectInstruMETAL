using DAL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// שליפת מחרוזת החיבור היחידה מתוך secrets.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// הרכבת מחרוזת החיבור המלאה הכוללת את שם בסיס הנתונים instruMETALDB
string dbConnectionString = connectionString.EndsWith(";")
    ? connectionString + "Database=instruMETALDB;"
    : connectionString + ";Database=instruMETALDB;";

// רישום מחלקת ה-UserDB משכבת ה-DAL כ-Singleton
builder.Services.AddSingleton(new DAL.UserDB(dbConnectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// עטיפת אתחול בסיס הנתונים ב-try-catch כדי להדפיס שגיאה ברורה אם השרת כבוי או הסיסמה שגויה
try
{
    await DAL.DatabaseInitializer.InitializeAsync(connectionString, connectionString);
}
catch (Exception ex)
{
    Console.WriteLine($"[DB] Database connection failed: {ex.Message}");
    throw; // זורק מחדש את החריגה כדי שהשרת לא יעלה ללא בסיס נתונים תקין
}

app.Run();