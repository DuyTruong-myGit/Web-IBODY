using IBODY_WebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IBODY_WebAPI.Helpers;
using System.IO;
using Microsoft.Extensions.FileProviders;
using IBODY_WebAPI.Services;
namespace IBODY_WebAPI.Controllers

{
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly FinalIbodyContext _context;
        private readonly ChatMessageService _chatMessageService;

        public UserController(FinalIbodyContext context,ChatMessageService chatMessageService)
        {
            _context = context;
            _chatMessageService = chatMessageService;
        }

        // ✅ LẤY THÔNG TIN HỒ SƠ
        [HttpGet("profile/{accountId}")]
        public async Task<IActionResult> GetUserProfile(int accountId)
        {
            var user = await _context.NguoiDungs
                .FirstOrDefaultAsync(nd => nd.TaiKhoanId == accountId);

            if (user == null)
                return NotFound(new { message = "Không tìm thấy hồ sơ người dùng." });

            var account = await _context.TaiKhoans.FindAsync(accountId);

            return Ok(new
            {
                id = user.Id,
                email = account?.Email,
                hoTen = user.HoTen,
                ngaySinh = user.NgaySinh,
                gioiTinh = user.GioiTinh,
                mucTieuTamLy = user.MucTieuTamLy,
                avatarUrl = user.AvatarUrl
            });
        }

        // ✅ CẬP NHẬT THÔNG TIN HỒ SƠ
        [HttpPut("profile/{accountId}")]
        public async Task<IActionResult> UpdateUserProfile(int accountId, [FromBody] UpdateUserProfileDto dto)
        {
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(nd => nd.TaiKhoanId == accountId);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy hồ sơ người dùng." });

            user.HoTen = dto.HoTen;
            user.NgaySinh = dto.NgaySinh;
            user.GioiTinh = dto.GioiTinh;
            user.MucTieuTamLy = dto.MucTieuTamLy;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật hồ sơ thành công." });
        }


        [HttpPost("upload-avatar/{accountId}")]
        public async Task<IActionResult> UploadAvatar(int accountId, IFormFile file)
        {
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(nd => nd.TaiKhoanId == accountId);
            if (user == null)
                return NotFound("Không tìm thấy người dùng.");

            if (file == null || file.Length == 0)
                return BadRequest("Vui lòng chọn file hợp lệ.");

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "img");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fileName = $"user_{accountId}_{DateTime.Now.Ticks}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            user.AvatarUrl = $"/img/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã cập nhật avatar", avatarUrl = user.AvatarUrl });
        }



        
            //Đánh giá chuyên gia chỉ sau khi kết thúc lịch hẹn
        [HttpPost("them_DanhGia")]
        public async Task<IActionResult> DanhGiaChuyenGia([FromBody] DanhGiaDto dto)
        {
            var nguoiDung = await _context.NguoiDungs.FirstOrDefaultAsync(x => x.TaiKhoanId == dto.NguoiDungId);
            if (nguoiDung == null)
                return NotFound("Người dùng không tồn tại.");

            var lich = await _context.LichHens
                .FirstOrDefaultAsync(l => l.NguoiDungId == nguoiDung.Id && l.ChuyenGiaId == dto.ChuyenGiaId);

            if (lich == null)
                return BadRequest("Bạn chưa từng có lịch hẹn với chuyên gia này.");

            var daDanhGia = await _context.DanhGia
                .AnyAsync(dg => dg.NguoiDungId == dto.NguoiDungId && dg.ChuyenGiaId == dto.ChuyenGiaId);
            if (daDanhGia)
                return BadRequest("Bạn đã đánh giá chuyên gia này rồi.");

            var danhGia = new DanhGium
            {
                LichHenId = lich.Id, // ✅ chỉ cần hợp lệ
                NguoiDungId = dto.NguoiDungId,
                ChuyenGiaId = dto.ChuyenGiaId,
                DiemSo = dto.DiemSo,
                NhanXet = dto.NhanXet
            };

            _context.DanhGia.Add(danhGia);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã gửi đánh giá!" });
        }


        [HttpPost("guiTinNhan")]
        public async Task<IActionResult> GuiTinNhan([FromBody] GuiTinNhanDto dto)
        {
            var tinNhan = new TinNhan
            {
                NguoiGuiId = dto.NguoiGuiId,
                NguoiNhanId = dto.NguoiNhanId,
                NoiDung = dto.NoiDung,
                ThoiGian = DateTime.Now
            };
            if (BadWordsFilter.ContainsBadWords(dto.NoiDung))
            {
                return BadRequest(new { message = "Tin nhắn chứa từ ngữ không phù hợp." });
            }

            await _chatMessageService.AddMessage(new ChatMessage {
                FromUserId = dto.NguoiGuiId,
                ToUserId = dto.NguoiNhanId,
                Content = dto.NoiDung
            });


            return Ok(new { message = "Đã gửi tin nhắn." });
        }

        [HttpGet("lichSuTinNhan")]
        public async Task<IActionResult> LichSuTinNhan([FromQuery] int taiKhoan1, [FromQuery] int taiKhoan2)
        {
            // var lichSu = await _context.TinNhans
            //     .Where(t =>
            //         (t.NguoiGuiId == taiKhoan1 && t.NguoiNhanId == taiKhoan2) ||
            //         (t.NguoiGuiId == taiKhoan2 && t.NguoiNhanId == taiKhoan1))
            //     .OrderBy(t => t.ThoiGian)
            //     .ToListAsync();
            var lichSu = await _chatMessageService.GetMessages(taiKhoan1, taiKhoan2);

            return Ok(lichSu);
        }


       [HttpGet("lichSuTuVan/{taiKhoanId}")]
        public async Task<IActionResult> LichSuTuVan(int taiKhoanId)
        {
            var nguoiDung = await _context.NguoiDungs.FirstOrDefaultAsync(x => x.TaiKhoanId == taiKhoanId);
            if (nguoiDung == null)
                return NotFound("Không tìm thấy người dùng.");

            var lichHen = await _context.LichHens
                .Include(l => l.ChuyenGia)
                .Include(l => l.HinhThuc)
                .Where(l => l.NguoiDungId == nguoiDung.Id && l.TrangThai == "da_dien_ra")
                .OrderByDescending(l => l.ThoiGianKetThuc)
                .Select(l => new {
                    id = l.Id,
                    chuyenGia = l.ChuyenGia.HoTen,
                    thoiGianBatDau = l.ThoiGianBatDau,
                    thoiGianKetThuc = l.ThoiGianKetThuc,
                    tomTat = l.TomTat,
                    ten = l.HinhThuc.Ten
                })
                .ToListAsync();

            return Ok(lichHen);
        }


        [HttpGet("danhSachChuyenGiaKetNoi/{taiKhoanId}")]
        public async Task<IActionResult> GetConnectedExperts(int taiKhoanId)
        {
            // Lấy danh sách tin nhắn có liên quan đến người dùng này
            var chuyenGiaIds = await _context.TinNhans
                .Where(t => t.NguoiGuiId == taiKhoanId || t.NguoiNhanId == taiKhoanId)
                .Select(t => t.NguoiGuiId == taiKhoanId ? t.NguoiNhanId : t.NguoiGuiId)
                .Distinct()
                .ToListAsync();

            var chuyenGiaList = await _context.ChuyenGia
                .Include(cg => cg.TaiKhoan)
                .Where(cg => chuyenGiaIds.Contains(cg.TaiKhoanId))
                .Select(cg => new
                {
                    cg.Id,
                    HoTen = cg.HoTen,
                    Email = cg.TaiKhoan.Email,
                    cg.TaiKhoanId
                })
                .ToListAsync();

            return Ok(chuyenGiaList);
        }

        //bỏ hàm trên

        [HttpGet("danhSachChuyenGiaDangTuVan/{taiKhoanId}")]
        public async Task<IActionResult> GetExpertsInProgress(int taiKhoanId)
        {
            var nguoiDung = await _context.NguoiDungs.FirstOrDefaultAsync(x => x.TaiKhoanId == taiKhoanId);
            if (nguoiDung == null)
                return NotFound("Không tìm thấy người dùng.");

            var chuyenGiaList = await _context.LichHens
                .Include(l => l.ChuyenGia).ThenInclude(cg => cg.TaiKhoan)
                .Where(l => l.NguoiDungId == nguoiDung.Id &&
                            (l.TrangThai == "da_dien_ra" || l.TrangThai == "cho_duyet")) // Lọc lịch hẹn đang diễn ra
                .Select(l => new
                {
                    l.ChuyenGia.Id,
                    HoTen = l.ChuyenGia.HoTen,
                    Email = l.ChuyenGia.TaiKhoan.Email,
                    l.ChuyenGia.TaiKhoanId,
                    TrangThai = l.TrangThai
                })
                .Distinct()
                .ToListAsync();

            return Ok(chuyenGiaList);
        }

        
        [HttpGet("phuong-thuc-thanh-toan")]
        public async Task<IActionResult> GetPhuongThucThanhToan()
        {
            var list = await _context.PhuongThucChungs
                .Where(p => p.TrangThai == "hien")
                .Select(p => new {
                    p.Id,
                    p.Ten
                })
                .ToListAsync();

            return Ok(list);
        }


        [HttpPost("yeu-cau-chuyen-khoan")]
        public async Task<IActionResult> TaoYeuCauThanhToan([FromBody] YeuCauXacNhanDto dto)
        {
            var existed = await _context.YeuCauXacNhanGoiDichVus
                .AnyAsync(x => x.TaiKhoanId == dto.TaiKhoanId && x.TrangThai == "cho_duyet");

            if (existed)
                return BadRequest(new { message = "Bạn đã có yêu cầu đang chờ xác nhận." });

            var yeuCau = new YeuCauXacNhanGoiDichVu
            {
                TaiKhoanId = dto.TaiKhoanId,
                GoiDichVuId = dto.GoiDichVuId,
                NoiDungChuyenKhoan = $"user_{dto.TaiKhoanId}_goi_{dto.GoiDichVuId}",
                TrangThai = "cho_duyet"
            };

            _context.YeuCauXacNhanGoiDichVus.Add(yeuCau);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lưu vào cơ sở dữ liệu.", error = ex.Message });
            }
            return Ok(new { message = "Đã gửi yêu cầu xác nhận chuyển khoản." });
        }



        [HttpGet("lich-su-goi/{taiKhoanId}")]
        public async Task<IActionResult> LichSuGoiDangKy(int taiKhoanId)
        {
            var lichSu = await _context.GoiDangKies
                .Include(g => g.GoiDichVu)
                .Where(g => g.TaiKhoanId == taiKhoanId)
                .OrderByDescending(g => g.NgayBatDau)
                .Select(g => new {
                    g.Id,
                    TenGoi = g.GoiDichVu.Ten,
                    Gia = g.GoiDichVu.Gia,
                    NgayBatDau = g.NgayBatDau,
                    NgayKetThuc = g.NgayKetThuc,
                    SoLuot = g.GoiDichVu.SoLuot,
                    SoLuotConLai = g.SoLuotConLai,
                    TrangThai = g.TrangThai
                })
                .ToListAsync();

            return Ok(lichSu);
        }

        // lấy chi tiết hóa đơn gói
        [HttpGet("goi-dang-ky/{id}")]
        public async Task<IActionResult> LayChiTietGoiDangKy(int id)
        {
            var goi = await _context.GoiDangKies
                .Include(g => g.GoiDichVu)
                .Include(g => g.TaiKhoan)
                .Where(g => g.Id == id)
                .Select(g => new
                {
                    g.Id,
                    TenGoi = g.GoiDichVu.Ten,
                    Gia = g.GoiDichVu.Gia,
                    NgayBatDau = g.NgayBatDau,
                    NgayKetThuc = g.NgayKetThuc,
                    SoLuot = g.GoiDichVu.SoLuot,
                    SoLuotConLai = g.SoLuotConLai,
                    TrangThai = g.TrangThai,
                    NguoiDung = new {
                        g.TaiKhoan.Id,
                        g.TaiKhoan.Email
                    }
                })
                .FirstOrDefaultAsync();

            if (goi == null)
                return NotFound(new { message = "Không tìm thấy gói đăng ký." });

            return Ok(goi);
        }



    }


    public class UpdateUserProfileDto
    {
        public string HoTen { get; set; } = null!;
        public DateTime? NgaySinh { get; set; }
        public string? GioiTinh { get; set; }
        public string? MucTieuTamLy { get; set; }
    }

    public class DanhGiaDto
    {
    public int LichHenId { get; set; }
    public int NguoiDungId { get; set; }
    public int ChuyenGiaId { get; set; }
    public int DiemSo { get; set; }
    public string? NhanXet { get; set; }
    }

    public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}



    public class ThanhToanDto
{
    public int LichHenId { get; set; }
    public int TaiKhoanId { get; set; }
    public int PhuongThucId { get; set; }
    public decimal SoTien { get; set; }
}

    public class GuiTinNhanDto
{
    public int NguoiGuiId { get; set; }
    public int NguoiNhanId { get; set; }
    public string NoiDung { get; set; } = null!;
}
public class YeuCauXacNhanDto
{
    public int TaiKhoanId { get; set; }
    public int GoiDichVuId { get; set; }
}


}



