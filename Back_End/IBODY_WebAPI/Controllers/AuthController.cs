using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using IBODY_WebAPI.Models;
using System.Security.Claims;
using System.Text.Json;
using System.Net;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace IBODY_WebAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly FinalIbodyContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;


        public AuthController(UserManager<ApplicationUser> userManager,
                              SignInManager<ApplicationUser> signInManager,
                              RoleManager<IdentityRole> roleManager,
                              FinalIbodyContext context,
                              IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _emailService = emailService;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                Gender = dto.Gender,
                Dob = dto.Dob,
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            //  Gán role Identity
            if (!await _roleManager.RoleExistsAsync("nguoi_dung"))
                await _roleManager.CreateAsync(new IdentityRole("nguoi_dung"));

            await _userManager.AddToRoleAsync(user, "nguoi_dung");

            //  THÊM VÀO BẢNG tài khoản để đồng bộ
            var taiKhoan = new TaiKhoan
            {
                Email = user.Email,
                MatKhau = "hashed_by_identity",
                VaiTro = "nguoi_dung",
                TrangThai = "hoat_dong"
            };

            _context.TaiKhoans.Add(taiKhoan);
            await _context.SaveChangesAsync();
            //THÊM VÀO BẢNG người dùngdùng để đồng bộ
            var nguoiDung = new NguoiDung
            {
                TaiKhoanId = taiKhoan.Id,
                HoTen = user.FullName,
                GioiTinh = user.Gender,
                NgaySinh = user.Dob,
                MucTieuTamLy = null
            };

            _context.NguoiDungs.Add(nguoiDung);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đăng ký thành công" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var tk = await _context.TaiKhoans
                .FirstOrDefaultAsync(t => t.Email == dto.Email);

            if (tk == null)
                return Forbid("Tài khoản chưa được đăng ký đầy đủ trong hệ thống.");

            if (tk.TrangThai == "khoa")
                return Forbid("Tài khoản của bạn đã bị khóa.");

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized(new { message = "Email không tồn tại." });

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
                return Unauthorized(new { message = "Mật khẩu không đúng." });

            // Thực hiện đăng nhập (tạo cookie)
            await _signInManager.SignInAsync(user, isPersistent: false);

            var roles = await _userManager.GetRolesAsync(user);

            string? avatarUrl = null;
            if (roles.Contains("nguoi_dung"))
            {
                var nguoiDung = await _context.NguoiDungs.FirstOrDefaultAsync(nd => nd.TaiKhoanId == tk.Id);
                avatarUrl = nguoiDung?.AvatarUrl;
            }
            else if (roles.Contains("chuyen_gia"))
            {
                var chuyenGia = await _context.ChuyenGia.FirstOrDefaultAsync(cg => cg.TaiKhoanId == tk.Id);
                avatarUrl = chuyenGia?.AvatarUrl;
            }

            return Ok(new
            {
                message = "Đăng nhập thành công!",
                user = new
                {
                    taiKhoanId = tk.Id,
                    email = user.Email,
                    fullName = user.FullName,
                    roles = roles,
                    trangThai = tk.TrangThai,
                    avatarUrl = avatarUrl
                }
            });
        }
    
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest model)
        {
            var user = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.Email == model.Email);
            if (user == null)
                return NotFound("Email không tồn tại.");

            // Tạo token và lưu DB
            var token = Guid.NewGuid().ToString();
            user.ResetToken = token;
            user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            await _context.SaveChangesAsync();

            // Soạn nội dung email chỉ chứa token
            string emailContent = $@"
                <h3>Khôi phục mật khẩu</h3>
                <p>Mã xác nhận của bạn là:</p>
                <h2>{token}</h2>
                <p>Sao chép mã này và dán vào trang khôi phục mật khẩu để tiếp tục.</p>";

            await _emailService.SendEmailAsync(user.Email, "Mã xác nhận khôi phục mật khẩu", emailContent);

            return Ok("Mã xác nhận đã được gửi đến email của bạn.");
        }


        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest model)
        {
            var taiKhoan = await _context.TaiKhoans.FirstOrDefaultAsync(x =>
                x.ResetToken == model.Token && x.ResetTokenExpiry > DateTime.UtcNow);

            if (taiKhoan == null)
                return BadRequest("Token không hợp lệ hoặc đã hết hạn.");

            //Tìm user bên Identity
            var user = await _userManager.FindByEmailAsync(taiKhoan.Email);
            if (user == null)
                return BadRequest("Không tìm thấy người dùng trong Identity.");

            // Reset mật khẩu Identity
            var removePassword = await _userManager.RemovePasswordAsync(user);
            var addPassword = await _userManager.AddPasswordAsync(user, model.NewPassword);

            if (!addPassword.Succeeded)
                return BadRequest("Không thể cập nhật mật khẩu.");

            // Đồng bộ mật khẩu bảng riêng
            taiKhoan.MatKhau = "hashed_by_identity";
            taiKhoan.ResetToken = null;
            taiKhoan.ResetTokenExpiry = null;
            await _context.SaveChangesAsync();

            return Ok("Mật khẩu đã được thay đổi thành công.");
        }

        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            var props = new AuthenticationProperties
            {
                RedirectUri = "/api/auth/google-callback"
            };
            return Challenge(props, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            // Lấy kết quả xác thực từ Google
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!result.Succeeded)
            {
                var errorMsg = result.Failure?.Message ?? "Google login failed";
                return BadRequest(errorMsg);
            }

            //  Trích xuất thông tin từ claim
            var claims = result.Principal?.Identities?.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var fullName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Không nhận được email từ Google.");

            //  Kiểm tra tài khoản trong bảng 'tai_khoan'
            var existingAccount = await _context.TaiKhoans.FirstOrDefaultAsync(tk => tk.Email == email);

            if (existingAccount == null)
            {
                // Nếu chưa tồn tại → tạo mới tài khoản
                var newAccount = new TaiKhoan
                {
                    Email = email,
                    MatKhau = null,
                    VaiTro = "nguoi_dung",
                    TrangThai = "hoat_dong"
                };

                _context.TaiKhoans.Add(newAccount);
                await _context.SaveChangesAsync();
                existingAccount = newAccount;

                //  Có thể thêm vào bảng 'nguoi_dung' 
                var newNguoiDung = new NguoiDung
                {
                    TaiKhoanId = newAccount.Id,
                    HoTen = fullName ?? "Người dùng Google",
                    NgaySinh = null,
                    GioiTinh = null,
                    MucTieuTamLy = null,
                    AvatarUrl = null
                };

                _context.NguoiDungs.Add(newNguoiDung);
                await _context.SaveChangesAsync();
            }

            // Tạo JSON chứa thông tin người dùng để gửi về FE
            var googleUserJson = JsonSerializer.Serialize(new
            {
                userId = existingAccount.Id,
                email = existingAccount.Email,
                role = existingAccount.VaiTro,
                fullName = fullName ?? existingAccount.Email
            });

            // Redirect về frontend + đính kèm thông tin
            var redirectUrl = $"http://127.0.0.1:5500/Front_End/HTML/Index.html?googleUser={Uri.EscapeDataString(googleUserJson)}";

            return Redirect(redirectUrl);
        }




    }
    public class ForgotPasswordRequest
{
    public string Email { get; set; }
}
public class ResetPasswordRequest
{
    public string Token { get; set; }
    public string NewPassword { get; set; }
}



    public class LoginDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
    public class RegisterDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public DateTime? Dob { get; set; }

    public string VaiTro { get; set; } = "nguoi_dung"; 
}
}
