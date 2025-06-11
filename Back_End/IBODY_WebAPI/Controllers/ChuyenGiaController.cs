using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IBODY_WebAPI.Models;
using IBODY_WebAPI.Helpers;
using System.IO;
using Microsoft.Extensions.FileProviders;
using IBODY_WebAPI.Services;
namespace IBODY_WebAPI.Controllers
{
    [ApiController]
    [Route("api/chuyen-gia")]
    public class ChuyenGiaController : ControllerBase
    {
        private readonly FinalIbodyContext _context;
        private readonly ChatMessageService _chatMessageService;

        public ChuyenGiaController(FinalIbodyContext context,ChatMessageService chatMessageService)
        {
            _context = context;
             _chatMessageService = chatMessageService;
        }

        //  API cập nhật thông tin hồ sơ chuyên gia
        [HttpPut("cap-nhat/{chuyenGiaId}")]
        public async Task<IActionResult> CapNhatChuyenGia(int chuyenGiaId, [FromBody] CapNhatChuyenGiaDto dto)
        {
            var cg = await _context.ChuyenGia.FindAsync(chuyenGiaId);
            if (cg == null)
                return NotFound(new { message = "Không tìm thấy chuyên gia." });

           
            cg.HoTen = dto.HoTen;
            cg.SoNamKinhNghiem = dto.SoNamKinhNghiem;
            cg.SoChungChi = dto.SoChungChi;
            cg.ChuyenMon = dto.ChuyenMon;
            cg.GioiThieu = dto.GioiThieu;
            cg.SoTaiKhoan = dto.SoTaiKhoan;
            cg.TenNganHang = dto.TenNganHang;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật hồ sơ chuyên gia thành công." });
        }


        [HttpPost("upload-avatar/{accountId}")]
        public async Task<IActionResult> UploadAvatar(int accountId, IFormFile file)
        {
            var cg = await _context.ChuyenGia.FirstOrDefaultAsync(c => c.TaiKhoanId == accountId);
            if (cg == null)
                return NotFound("Không tìm thấy chuyên gia.");

            if (file == null || file.Length == 0)
                return BadRequest("Vui lòng chọn file hợp lệ.");

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "img");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fileName = $"cg_{accountId}_{DateTime.Now.Ticks}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            cg.AvatarUrl = $"/img/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã cập nhật avatar", avatarUrl = cg.AvatarUrl });
        }


        [HttpPost("upload-chung-chi/{accountId}")]
        public async Task<IActionResult> UploadChungChi(int accountId, IFormFile file)
        {
            var cg = await _context.ChuyenGia.FirstOrDefaultAsync(c => c.TaiKhoanId == accountId);
            if (cg == null) return NotFound("Không tìm thấy chuyên gia.");

            if (file == null || file.Length == 0)
                return BadRequest("Vui lòng chọn file.");

            var folder = Path.Combine(Directory.GetCurrentDirectory(), "img");
            Directory.CreateDirectory(folder);

            var fileName = $"chungchi_{accountId}_{DateTime.Now.Ticks}{Path.GetExtension(file.FileName)}";
            var path = Path.Combine(folder, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            cg.AnhChungChi = $"/img/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã tải ảnh chứng chỉ", url = cg.AnhChungChi });
        }



        [HttpPost("guiTinNhan")]
        public async Task<IActionResult> ExpertGuiTinNhan([FromBody] GuiTinNhanDto dto)
        {
            var coLichHen = await _context.LichHens.AnyAsync(lh =>
                lh.ChuyenGia.TaiKhoanId == dto.NguoiGuiId &&
                lh.NguoiDung.TaiKhoanId == dto.NguoiNhanId &&
                lh.TrangThai == "da_dien_ra");

            if (!coLichHen)
            {
                return Forbid("Bạn chỉ có thể nhắn với người dùng đã đặt lịch và đã thanh toán.");
            }

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


            return Ok(new { message = "Đã gửi tin nhắn từ chuyên gia." });
        }

        [HttpGet("lichSuTinNhan")]
        public async Task<IActionResult> LichSuTinNhan([FromQuery] int taiKhoan1, [FromQuery] int taiKhoan2)
        {
            var lichSu = await _chatMessageService.GetMessages(taiKhoan1, taiKhoan2);
            return Ok(lichSu);
        }

        [HttpGet("danhGia/{taiKhoanId}")]
        public async Task<IActionResult> GetDanhGiaCuaToi(int taiKhoanId)
        {
            // Tìm chuyên gia theo tài khoản ID
            var chuyenGia = await _context.ChuyenGia
                .FirstOrDefaultAsync(cg => cg.TaiKhoanId == taiKhoanId);

            if (chuyenGia == null)
                return NotFound(new { message = "Không tìm thấy chuyên gia tương ứng với tài khoản." });

            var danhGiaList = await _context.DanhGia
                .Where(dg => dg.ChuyenGiaId == chuyenGia.Id)
                .Include(dg => dg.NguoiDung)
                    .ThenInclude(nd => nd.TaiKhoan)
                .Include(dg => dg.LichHen)
                .OrderByDescending(dg => dg.LichHen.ThoiGianBatDau)
                .Select(dg => new
                {
                    dg.Id,
                    HoTenNguoiDung = dg.NguoiDung.HoTen,
                    EmailNguoiDung = dg.NguoiDung.TaiKhoan.Email,
                    ThoiGianTuvan = dg.LichHen.ThoiGianBatDau,
                    DiemSo = dg.DiemSo,
                    NhanXet = dg.NhanXet
                })
                .ToListAsync();

            return Ok(danhGiaList);
        }

        [HttpGet("khach-hang-tu-van/{chuyenGiaId}")]
        public async Task<IActionResult> GetKhachHangTungTuVan(int chuyenGiaId)
        {
            // Lấy danh sách lịch hẹn đã thanh toán hoặc đã diễn ra
            var lichHenList = await _context.LichHens
                .Include(lh => lh.NguoiDung).ThenInclude(nd => nd.TaiKhoan)
                .Include(lh => lh.HinhThuc)
                .Where(lh => lh.ChuyenGiaId == chuyenGiaId 
                        && (lh.TrangThai == "da_thanh_toan" || lh.TrangThai == "da_dien_ra"))
                .Select(lh => new
                {
                    LichHenId = lh.Id,
                    HoTenKhachHang = lh.NguoiDung.HoTen,
                    Email = lh.NguoiDung.TaiKhoan.Email,
                    TaiKhoanIdNguoiDung = lh.NguoiDung.TaiKhoan.Id,
                    AvatarUrl = lh.NguoiDung.AvatarUrl, 
                    Ngay = lh.ThoiGianBatDau.Value.Date,
                    GioBatDau = lh.ThoiGianBatDau,
                    GioKetThuc = lh.ThoiGianKetThuc,
                    TomTat = lh.TomTat,
                    HinhThuc = lh.HinhThuc.Ten,
                    TrangThai = lh.TrangThai
                    
                })
                .OrderByDescending(lh => lh.GioBatDau)
                .ToListAsync();

            return Ok(lichHenList);
        }

        [HttpGet("thongTin/{taiKhoanId}")]
        public async Task<IActionResult> GetThongTinTheoTaiKhoan(int taiKhoanId)
        {
            var chuyenGia = await _context.ChuyenGia
                .FirstOrDefaultAsync(cg => cg.TaiKhoanId == taiKhoanId);

            if (chuyenGia == null)
                return NotFound();

            return Ok(new {
                id = chuyenGia.Id,
                hoTen = chuyenGia.HoTen,
                soNamKinhNghiem = chuyenGia.SoNamKinhNghiem,
                chuyenMon = chuyenGia.ChuyenMon,
                gioiThieu = chuyenGia.GioiThieu,
                soChungChi = chuyenGia.SoChungChi,
                anhChungChi = chuyenGia.AnhChungChi,
                avatarUrl = chuyenGia.AvatarUrl,
                soTaiKhoan = chuyenGia.SoTaiKhoan,
                tenNganHang = chuyenGia.TenNganHang

            });
        }

        [HttpGet("nhan-luong/{taiKhoanId}")]
        public async Task<IActionResult> ThongTinLuongChuyenGia(int taiKhoanId)
        {
            var chuyenGia = await _context.ChuyenGia.FirstOrDefaultAsync(c => c.TaiKhoanId == taiKhoanId);
            if (chuyenGia == null)
                return NotFound("Không tìm thấy chuyên gia.");

            const decimal donGiaCa = 500000;

            var soCa = await _context.LichHens
                .CountAsync(lh => lh.ChuyenGiaId == chuyenGia.Id && lh.TrangThai == "da_hoan_tat");

            var lich = await _context.LichHens
                .Where(lh => lh.ChuyenGiaId == chuyenGia.Id && lh.TrangThai == "da_hoan_tat")
                .Select(lh => new {
                    lh.Id,
                    lh.ThoiGianBatDau,
                    lh.ThoiGianKetThuc,
                    lh.TomTat,
                    lh.NgayTao // bổ sung để frontend lọc đúng
                }).ToListAsync();

            var luongLichSu = await _context.YeuCauNhanLuongs
                .Where(x => x.ChuyenGiaId == chuyenGia.Id)
                .OrderByDescending(x => x.NgayTao)
                .Select(x => new {
                    x.Id,
                    x.SoCa,
                    x.SoTien,
                    x.NgayTao,
                    x.TrangThai
                }).ToListAsync();

            return Ok(new {
                hoTen = chuyenGia.HoTen,
                soTaiKhoan = chuyenGia.SoTaiKhoan,
                tenNganHang = chuyenGia.TenNganHang,
                tongCa = soCa,
                donGia = donGiaCa,
                tongTien = soCa * donGiaCa,
                chiTietCa = lich,
                lichSuNhanLuong = luongLichSu
            });
        }

        [HttpPost("gui-yeu-cau-nhan-luong")]
        public async Task<IActionResult> GuiYeuCauNhanLuong([FromBody] int taiKhoanId)
        {
            var chuyenGia = await _context.ChuyenGia.FirstOrDefaultAsync(c => c.TaiKhoanId == taiKhoanId);
            if (chuyenGia == null) return NotFound("Không tìm thấy chuyên gia.");

            const decimal donGiaCa = 500000;

            // Lấy thời gian của yêu cầu lương gần nhất
            var lastRequest = await _context.YeuCauNhanLuongs
                .Where(y => y.ChuyenGiaId == chuyenGia.Id && y.TrangThai == "da_duyet")
                .OrderByDescending(y => y.NgayTao)
                .FirstOrDefaultAsync();

            DateTime? lastPaidTime = lastRequest?.NgayTao;

            // Lấy các lịch hẹn hoàn tất sau thời điểm được trả lương trước đó
            var lichChuaTinhLuong = await _context.LichHens
                .Where(lh => lh.ChuyenGiaId == chuyenGia.Id &&
                            lh.TrangThai == "da_hoan_tat" &&
                            lh.ThoiGianKetThuc != null &&
                            (lastPaidTime == null || lh.ThoiGianKetThuc > lastPaidTime))
                .ToListAsync();

            if (lichChuaTinhLuong.Count == 0)
                return BadRequest("Không có ca nào chưa thanh toán.");

            var soCa = lichChuaTinhLuong.Count;
            var tongTien = soCa * donGiaCa;

            var yeuCau = new YeuCauNhanLuong {
                ChuyenGiaId = chuyenGia.Id,
                SoCa = soCa,
                SoTien = tongTien,
                TrangThai = "dang_cho",
                NgayTao = DateTime.Now
            };

            _context.YeuCauNhanLuongs.Add(yeuCau);
            await _context.SaveChangesAsync();

            return Ok(new {
                message = "Đã gửi yêu cầu nhận lương. Vui lòng chờ xét duyệt.",
                soTaiKhoan = chuyenGia.SoTaiKhoan,
                tenNganHang = chuyenGia.TenNganHang
            });
        }

        [HttpGet("chi-tiet-luong/{id}")]
        public async Task<IActionResult> ChiTietLuong(int id)
        {
            var record = await _context.YeuCauNhanLuongs.FindAsync(id);
            if (record == null)
                return NotFound(new { message = "Không tìm thấy yêu cầu lương." });

            return Ok(new {
                record.SoCa,
                DonGia = 500000,
                SoTien = record.SoTien,
                record.NgayTao,
                record.TrangThai
            });
        }

    }


public class CapNhatChuyenGiaDto
{
    public string HoTen { get; set; } = null!;
    public int SoNamKinhNghiem { get; set; }
    public string SoChungChi { get; set; } = null!;
    public string ChuyenMon { get; set; } = null!;
    public string GioiThieu { get; set; } = null!;
    public string? SoTaiKhoan { get; set; }
    public string? TenNganHang { get; set; }
}

    public class DoiMatKhauDto
{
    public string MatKhauCu { get; set; } = null!;
    public string MatKhauMoi { get; set; } = null!;
}


}
