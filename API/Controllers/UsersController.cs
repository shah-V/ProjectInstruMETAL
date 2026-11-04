using Microsoft.AspNetCore.Mvc;
using DAL;
using Model;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserDB _userDb;

        public UsersController(UserDB userDb)
        {
            _userDb = userDb;
        }

        // GET: api/users
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userDb.GetAllUsersAsync();

            // אבטחה: מניעת חשיפת הגיבובים ברשימת המשתמשים
            foreach (var user in users)
            {
                user.Password = string.Empty;
            }

            return Ok(users);
        }

        // GET: api/users/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userDb.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "המשתמש לא נמצא" });

            // אבטחה: מניעת חשיפת הגיבוב
            user.Password = string.Empty;

            return Ok(user);
        }

        // GET: api/users/exists/shredder
        [HttpGet("exists/{username}")]
        public async Task<IActionResult> UsernameExists(string username)
        {
            bool exists = await _userDb.UsernameExistsAsync(username);
            return Ok(exists);
        }

        // GET: api/users/email-exists/user@metal.com
        [HttpGet("email-exists/{email}")]
        public async Task<IActionResult> EmailExists(string email)
        {
            bool exists = await _userDb.EmailExistsAsync(email);
            return Ok(exists);
        }

        // POST: api/users (הרשמה)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] User user)
        {
            if (await _userDb.UsernameExistsAsync(user.Username))
                return BadRequest(new { message = "שם המשתמש כבר תפוס" });

            if (await _userDb.EmailExistsAsync(user.Email))
                return BadRequest(new { message = "כתובת האימייל כבר קיימת במערכת" });

            int newId = await _userDb.AddUserAsync(user);
            user.Id = newId;

            // אבטחה: מניעת חשיפת הגיבוב באובייקט המוחזר
            user.Password = string.Empty;

            return CreatedAtAction(nameof(GetById), new { id = newId }, user);
        }

        // POST: api/users/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userDb.CheckLoginAsync(request.Username, request.Password);
            if (user == null)
                return Unauthorized(new { message = "שם משתמש או סיסמה שגויים" });

            // אבטחה: מניעת חשיפת הגיבוב
            user.Password = string.Empty;

            return Ok(user);
        }

        // PATCH: api/users/5/role
        [HttpPatch("{id}/role")]
        public async Task<IActionResult> UpdateRole(int id, [FromQuery] int newRoleId)
        {
            bool success = await _userDb.UpdateUserRoleAsync(id, newRoleId);
            if (!success)
                return NotFound(new { message = "המשתמש לא נמצא" });

            return Ok(new { message = "תפקיד המשתמש עודכן בהצלחה" });
        }

        // PATCH: api/users/5/password
        [HttpPatch("{id}/password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] string newPassword)
        {
            bool success = await _userDb.UpdatePasswordAsync(id, newPassword);
            if (!success)
                return NotFound(new { message = "המשתמש לא נמצא" });

            return Ok(new { message = "הסיסמה עודכנה בהצלחה" });
        }

        // PATCH: api/users/5/email
        [HttpPatch("{id}/email")]
        public async Task<IActionResult> UpdateEmail(int id, [FromBody] string newEmail)
        {
            bool success = await _userDb.UpdateEmailAsync(id, newEmail);
            if (!success)
                return NotFound(new { message = "המשתמש לא נמצא" });

            return Ok(new { message = "האימייל עודכן בהצלחה" });
        }

        // DELETE: api/users/5 (Hard Delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            bool success = await _userDb.DeleteUserAsync(id);
            if (!success)
                return NotFound(new { message = "המשתמש לא נמצא" });

            return Ok(new { message = "המשתמש נמחק בהצלחה" });
        }

        // PATCH: api/users/5/soft-delete (Soft Delete - הפיכה ללא פעיל)
        [HttpPatch("{id}/soft-delete")]
        public async Task<IActionResult> SoftDelete(int id)
        {
            bool success = await _userDb.SoftDeleteUserAsync(id);
            if (!success)
                return NotFound(new { message = "המשתמש לא נמצא" });

            return Ok(new { message = "המשתמש הועבר למצב לא פעיל" });
        }

        // GET: api/users/count-by-role
        [HttpGet("count-by-role")]
        public async Task<IActionResult> GetCountByRole()
        {
            var counts = await _userDb.GetCountByRoleAsync();
            return Ok(counts);
        }
    }

    // מחלקת עזר לקבלת נתוני התחברות
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}