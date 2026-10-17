using DAL;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();



var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var serverConnectionString = builder.Configuration.GetConnectionString("ServerConnection");

string dbConnectionString = (builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."))
    + "Database=instruMETALDB;";

builder.Services.AddSingleton(new DAL.UserDB(dbConnectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await DAL.DatabaseInitializer.InitializeAsync(connectionString, serverConnectionString);

app.Run();