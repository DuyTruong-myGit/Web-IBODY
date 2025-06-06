using IBODY_WebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBODY_WebAPI.Controllers
{
    [ApiController]
    [Route("api/lich-hen")]
    public class LichHenController : ControllerBase
    {
        private readonly FinalIbodyContext _context;

        public LichHenController(FinalIbodyContext context)
        {
            _context = context;
        }

        //Lịch hẹn dành cho chuyên gia đang đăng nhập
        [HttpGet("chuyen-gia/{chuyenGiaId}")]
        public async Task<IActionResult> GetLichHenChuyenGia(int chuyenGiaId, [FromQuery] int taiKhoanId, [FromQuery] string? trangThai = null)
        {
            var tk = await _context.TaiKhoans.FindAsync(taiKhoanId);
            if (tk == null || tk.TrangThai == "khoa")
                return Forbid("Tài khoản của bạn đã bị khóa.");

            var lichQuery = _context.LichHens
                .Where(lh => lh.ChuyenGiaId == chuyenGiaId);

            if (!string.IsNullOrEmpty(trangThai))
            {
                lichQuery = lichQuery.Where(lh => lh.TrangThai == trangThai);
            }

            var lich = await lichQuery
                .Include(lh => lh.NguoiDung)
                .Include(lh => lh.HinhThuc)
                .Select(lh => new
                {
                    lh.Id,
                    lh.ThoiGianBatDau,
                    lh.ThoiGianKetThuc,
                    HinhThuc = lh.HinhThuc.Ten,
                    TomTat = lh.TomTat,
                    NguoiDatLich = new
                    {
                        lh.NguoiDung.Id,
                        lh.NguoiDung.HoTen
                    }
                })
                .OrderBy(lh => lh.ThoiGianBatDau)
                .ToListAsync();

            return Ok(lich);
        }

        [HttpDelete("huy-lich-chuyen-gia/{lichHenId}")]
        public async Task<IActionResult> HuyLichHenChuyenGia(int lichHenId, [FromQuery] int taiKhoanId)
        {
            var tk = await _context.TaiKhoans.FindAsync(taiKhoanId);
            if (tk == null || tk.TrangThai == "khoa")
                return Forbid("Tài khoản của bạn đã bị khóa.");

            var lich = await _context.LichHens.FindAsync(lichHenId);

            if (lich == null)
                return NotFound(new { message = "Lịch hẹn không tồn tại." });
            if (lich.TrangThai == "da_thanh_toan")
                return BadRequest(new { message = "Lịch đã thanh toán, không thể hủy." });

            var danhGiaLienQuan = _context.DanhGia.Where(dg => dg.LichHenId == lichHenId);
            _context.DanhGia.RemoveRange(danhGiaLienQuan);

            _context.LichHens.Remove(lich);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Chuyên gia đã hủy lịch hẹn thành công." });
        }

        // [HttpPost("duyet-lich/{lichHenId}")]
        // public async Task<IActionResult> DuyetLichHen(int lichHenId)
        // {
        //     var lich = await _context.LichHens
        //         .Include(l => l.NguoiDung)
        //         .FirstOrDefaultAsync(l => l.Id == lichHenId);

        //     if (lich == null || lich.TrangThai != "cho_duyet")
        //         return BadRequest("Lịch không hợp lệ.");

        //     var chuyenGiaId = lich.ChuyenGiaId;
        //     var nguoiDungId = lich.NguoiDungId;
        //     var start = lich.ThoiGianBatDau;
        //     var end = lich.ThoiGianKetThuc;

        //     var lichSau = await _context.LichHens
        //         .Where(l => l.ChuyenGiaId == chuyenGiaId && l.ThoiGianBatDau == end && l.Id != lich.Id)
        //         .OrderBy(l => l.ThoiGianBatDau)
        //         .FirstOrDefaultAsync();

        //     if (lichSau != null && lichSau.NguoiDungId != nguoiDungId)
        //     {
        //         return BadRequest("Có ca ngay sau và không cùng người đặt, phải cách nhau 30 phút.");
        //     }

        //     var lichTruoc = await _context.LichHens
        //         .Where(l => l.ChuyenGiaId == chuyenGiaId && l.ThoiGianKetThuc == start && l.Id != lich.Id)
        //         .OrderByDescending(l => l.ThoiGianBatDau)
        //         .FirstOrDefaultAsync();

        //     if (lichTruoc != null && lichTruoc.NguoiDungId != nguoiDungId)
        //     {
        //         return BadRequest("Có ca ngay trước và không cùng người đặt, phải cách nhau 30 phút.");
        //     }

        //     lich.TrangThai = "da_dien_ra";
        //     await _context.SaveChangesAsync();

        //     return Ok(new { message = "Đã duyệt lịch, sẵn sàng cho buổi tư vấn." });
        // }

        [HttpPost("duyet-lich/{lichHenId}")]
        public async Task<IActionResult> DuyetLichHen(int lichHenId)
        {
            var lichGoc = await _context.LichHens
                .Include(l => l.NguoiDung)
                .FirstOrDefaultAsync(l => l.Id == lichHenId);

            if (lichGoc == null || lichGoc.TrangThai != "cho_duyet")
                return BadRequest("Lịch không hợp lệ.");

            var nguoiDungId = lichGoc.NguoiDungId;
            var chuyenGiaId = lichGoc.ChuyenGiaId;

            var start = lichGoc.ThoiGianBatDau;

            // ✅ Tìm tất cả lịch liên tiếp cùng người đặt, chuyên gia, cùng ngày, chưa duyệt
            var lichCungDot = await _context.LichHens
                .Where(l =>
                    l.NguoiDungId == nguoiDungId &&
                    l.ChuyenGiaId == chuyenGiaId &&
                    l.TrangThai == "cho_duyet" &&
                    l.ThoiGianBatDau.Value.Date == start.Value.Date &&
                    l.ThoiGianBatDau >= start &&
                    l.ThoiGianBatDau < start.Value.AddHours(5)) // giới hạn 10 ca = 5h
                .OrderBy(l => l.ThoiGianBatDau)
                .ToListAsync();

            if (!lichCungDot.Any())
                return BadRequest("Không có lịch nào phù hợp để duyệt.");

            int soCa = lichCungDot.Count;

            // ✅ Tìm gói dịch vụ
            var taiKhoanId = lichGoc.NguoiDung.TaiKhoanId;
            var goi = await _context.GoiDangKies
                .Where(g => g.TaiKhoanId == taiKhoanId && g.TrangThai == "con_hieu_luc")
                .OrderByDescending(g => g.NgayKetThuc)
                .FirstOrDefaultAsync();

            if (goi == null)
                return BadRequest("Không tìm thấy gói hợp lệ.");

            // ⚠️ Không cần kiểm tra SoLuotConLai tại đây (đã check khi đặt), chỉ trừ
            goi.SoLuotConLai -= soCa;
            if (goi.SoLuotConLai <= 0)
                goi.TrangThai = "het_hieu_luc";

            // ✅ Cập nhật từng lịch
            foreach (var lich in lichCungDot)
            {
                lich.TrangThai = "da_dien_ra";
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã duyệt {soCa} ca và cập nhật lượt sử dụng." });
        }


        [HttpPost("tu-choi-lich/{lichHenId}")]
        public async Task<IActionResult> TuChoiLichHen(int lichHenId)
        {
            var lich = await _context.LichHens.FindAsync(lichHenId);
            if (lich == null || lich.TrangThai != "cho_duyet")
                return BadRequest("Lịch không hợp lệ.");

            lich.TrangThai = "da_huy";
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã từ chối lịch." });
        }

        [HttpPost("hoan-tat/{lichHenId}")]
        public async Task<IActionResult> HoanTatLichHen(int lichHenId)
        {
            var lich = await _context.LichHens.FindAsync(lichHenId);

            if (lich == null)
                return NotFound(new { message = "Không tìm thấy lịch hẹn." });

            if (lich.TrangThai != "da_dien_ra")
                return BadRequest(new { message = "Lịch chưa được duyệt hoặc đã hoàn tất." });

            lich.TrangThai = "da_hoan_tat";
            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ Đã hoàn tất buổi tư vấn." });
        }
        

        [HttpGet("lich-hoan-tat-gan-nhat")]
        public async Task<IActionResult> LayLichGanNhat([FromQuery] int nguoiDungId, [FromQuery] int chuyenGiaId)
        {
            var lich = await _context.LichHens
                .Where(l => l.NguoiDung.TaiKhoanId == nguoiDungId && l.ChuyenGia.TaiKhoanId == chuyenGiaId && l.TrangThai == "da_hoan_tat")
                .OrderByDescending(l => l.ThoiGianKetThuc)
                .Select(l => new { l.Id })
                .FirstOrDefaultAsync();

            if (lich == null)
                return NotFound("Không tìm thấy lịch hợp lệ.");

            return Ok(lich);
        }

    }
}
