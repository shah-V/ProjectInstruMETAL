using DAL;

// יצירת הבואילדר (Builder) להגדרת שירותי האפליקציה והגדרות ההרצה
var builder = WebApplication.CreateBuilder(args);

// הוספת תמיכה בבקרים (Controllers) במערכת
builder.Services.AddControllers();

// הגדרת תמיכה בתיעוד OpenAPI (Swagger) עבור ה-API
builder.Services.AddOpenApi();

// שליפת מחרוזות החיבור מתוך הגדרות המערכת (User Secrets / appsettings)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var serverConnectionString = builder.Configuration.GetConnectionString("ServerConnection");

// הרכבת מחרוזת החיבור המלאה הכוללת את שם בסיס הנתונים instruMETALDB
string dbConnectionString = (builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."))
    + "Database=instruMETALDB;";

// רישום מחלקת ה-UserDB משכבת ה-DAL כ-Singleton כדי שניתן יהיה להזריק אותה לבקרים
builder.Services.AddSingleton(new DAL.UserDB(dbConnectionString));

// בניית אובייקט ה-WebApplication
var app = builder.Build();

// הגדרת צינור הטיפול בבקשות HTTP (Request Pipeline)
if (app.Environment.IsDevelopment())
{
    // הפעלת תיעוד OpenAPI בסביבת פיתוח בלבד
    app.MapOpenApi();
}

// הפניית בקשות לא מאובטחות ל-HTTPS
app.UseHttpsRedirection();

// הפעלת מנגנון הרשאות ואימות
app.UseAuthorization();

// מיפוי נתיבי ה-Controllers לטיפול בבקשות הנכנסות
app.MapControllers();

// הרצת אתחול בסיס הנתונים (יצירת מסד הנתונים והטבלאות אם אינם קיימים)
await DAL.DatabaseInitializer.InitializeAsync(connectionString, serverConnectionString);

// הרצת שרת ה-API והתחלת האזנה לבקשות
app.Run();