using IBODY_WebAPI.Data;
using IBODY_WebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBODY_WebAPI.Controllers
{
    [Route("api/goi-dich-vu")]
    [ApiController]
    public class GoiDangKyController : ControllerBase
    {
        private readonly FinalIbodyContext _context;

        public GoiDangKyController(FinalIbodyContext context)
        {
            _context = context;
        }

         [HttpGet("danh-sach")]
        public async Task<IActionResult> LayDanhSachGoi()
        {
            var result = await _context.GoiDichVus
                .Where(g => g.DanhCho == "nguoi_dung")
                .OrderBy(g => g.Id)
                .ToListAsync();
            return Ok(result);
        }

        [HttpPost("dang-ky")]
        public async Task<IActionResult> DangKyGoi([FromBody] GoiDangKy model)
        {
            if (model == null || model.GoiDichVuId <= 0)
                return BadRequest(new { message = "Thiếu thông tin gói cần đăng ký." });

            var goi = await _context.GoiDichVus.FirstOrDefaultAsync(g => g.Id == model.GoiDichVuId);
            if (goi == null)
                return NotFound(new { message = "Không tìm thấy gói dịch vụ." });

            // Huỷ gói cũ nếu đang còn hiệu lực
            var goiCu = await _context.GoiDangKies
                .Where(g => g.TaiKhoanId == model.TaiKhoanId && g.TrangThai == "con_hieu_luc")
                .ToListAsync();

            foreach (var g in goiCu)
            {
                g.TrangThai = "het_hieu_luc";
                g.NgayKetThuc = DateTime.Today;
            }

            await _context.SaveChangesAsync(); // lưu trạng thái cũ trước khi thêm mới

            var ngayBatDau = DateTime.Today;
            var ngayKetThuc = ngayBatDau.AddDays(goi.ThoiHanNgay.Value);

            var goiDangKy = new GoiDangKy
            {
                TaiKhoanId = model.TaiKhoanId,
                GoiDichVuId = goi.Id,
                NgayBatDau = ngayBatDau,
                NgayKetThuc = ngayKetThuc,
                SoLuotConLai = goi.SoLuot,
                TrangThai = "con_hieu_luc"
            };

            _context.GoiDangKies.Add(goiDangKy);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký gói thành công", goiDangKyId = goiDangKy.Id });
        }

        [HttpGet("dang-su-dung/{taiKhoanId}")]
        public async Task<IActionResult> LayGoiHienTai(int taiKhoanId)
        {
            var goi = await _context.GoiDangKies
                .Include(g => g.GoiDichVu)
                .Where(g => g.TaiKhoanId == taiKhoanId && g.NgayKetThuc >= DateTime.Today  && g.TrangThai == "con_hieu_luc")
                .OrderByDescending(g => g.NgayKetThuc)
                .Select(g => new
                {
                    id = g.Id,
                    taiKhoanId = g.TaiKhoanId,
                    goiDichVuId = g.GoiDichVuId,
                    soLuotConLai = g.SoLuotConLai,
                    ngayBatDau = g.NgayBatDau,
                    ngayKetThuc = g.NgayKetThuc,
                    tenGoi = g.GoiDichVu.Ten,
                    moTa = g.GoiDichVu.MoTa,
                    gia = g.GoiDichVu.Gia,
                    thoiHan = g.GoiDichVu.ThoiHanNgay
                })
                .FirstOrDefaultAsync();

            return Ok(goi);
        }

        [HttpPost("huy-goi/{taiKhoanId}")]
        public async Task<IActionResult> HuyGoi(int taiKhoanId)
        {
            var goi = await _context.GoiDangKies
                .Where(g => g.TaiKhoanId == taiKhoanId && g.TrangThai == "con_hieu_luc")
                .OrderByDescending(g => g.NgayKetThuc)
                .FirstOrDefaultAsync();

            if (goi == null)
                return BadRequest(new { message = "Không có gói nào đang sử dụng." });

            goi.TrangThai = "het_hieu_luc";
            goi.NgayKetThuc = DateTime.Today;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã hủy gói thành công." });
        }

        [HttpGet("chi-tiet/{id}")]
        public async Task<IActionResult> LayChiTietGoi(int id)
        {
            var goi = await _context.GoiDichVus.FindAsync(id);
            if (goi == null)
                return NotFound(new { message = "Không tìm thấy gói dịch vụ." });

            return Ok(new
            {
                id = goi.Id,
                ten = goi.Ten,
                moTa = goi.MoTa,
                gia = goi.Gia,
                thoiHanNgay = goi.ThoiHanNgay,
                soLuot = goi.SoLuot
            });
        }

    }
}
