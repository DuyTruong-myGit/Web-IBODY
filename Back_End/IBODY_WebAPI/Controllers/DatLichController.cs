using IBODY_WebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBODY_WebAPI.Controllers
{
    [ApiController]
    [Route("api/dat-lich")]
    public class DatLichController : ControllerBase
    {
        private readonly FinalIbodyContext _context;
        public DatLichController(FinalIbodyContext context)
        {
            _context = context;
        }

        [HttpPost("tao-lich")]
        public async Task<IActionResult> TaoLichHen([FromBody] TaoLichHenDto dto)
        {
            try
            {
                var nguoiDung = await _context.NguoiDungs.FirstOrDefaultAsync(nd => nd.Id == dto.NguoiDungId);
                if (nguoiDung == null) return BadRequest("Không tìm thấy người dùng");

                var taiKhoanId = nguoiDung.TaiKhoanId;

                // Kiểm tra gói còn hiệu lực
                var goi = await _context.GoiDangKies
                    .Where(g => g.TaiKhoanId == taiKhoanId && g.TrangThai == "con_hieu_luc" && g.SoLuotConLai > 0)
                    .OrderByDescending(g => g.NgayKetThuc)
                    .FirstOrDefaultAsync();
                if (goi == null) return BadRequest("Không có gói còn hiệu lực hoặc hết lượt.");

                // Tính toán giờ bắt đầu và kết thúc cho mỗi ca
                int soCa = dto.SoCa <= goi.SoLuotConLai ? dto.SoCa : goi.SoLuotConLai;
                var thoiGianBatDau = dto.ThoiGianBatDau;

                // Kiểm tra xung đột
                for (int i = 0; i < soCa; i++)
                {
                    var thoiGianKetThuc = thoiGianBatDau.AddMinutes(30);

                    var trungLich = await _context.LichHens
                        .AnyAsync(lh =>
                            lh.ChuyenGiaId == dto.ChuyenGiaId &&
                            ((lh.ThoiGianBatDau < thoiGianKetThuc && lh.ThoiGianKetThuc > thoiGianBatDau))
                        );
                    if (trungLich)
                        return BadRequest($"Ca {i + 1} bị trùng với lịch đã có, vui lòng chọn thời gian khác.");

                    thoiGianBatDau = thoiGianKetThuc;
                }

                // Tạo lịch liên tiếp
                thoiGianBatDau = dto.ThoiGianBatDau;
                var soCaTrongNgay = await _context.LichHens
                    .Where(lh => lh.ChuyenGiaId == dto.ChuyenGiaId
                        && lh.ThoiGianBatDau.HasValue
                        && lh.ThoiGianBatDau.Value.Date == thoiGianBatDau.Date)
                    .CountAsync();

                if (soCaTrongNgay + dto.SoCa > 10)
                {
                    return BadRequest("Chuyên gia chỉ nhận tối đa 10 ca/ngày. Vui lòng chọn ngày khác hoặc số ca ít hơn.");
                }
                for (int i = 0; i < soCa; i++)
                {
                    var lichHen = new LichHen
                    {
                        NguoiDungId = dto.NguoiDungId,
                        ChuyenGiaId = dto.ChuyenGiaId,
                        HinhThucId = dto.HinhThucId,
                        ThoiGianBatDau = thoiGianBatDau,
                        ThoiGianKetThuc = thoiGianBatDau.AddMinutes(30),
                        TomTat = $"Ca {i + 1}: {dto.TomTat}",
                        TrangThai = "cho_duyet"
                    };

                    _context.LichHens.Add(lichHen);
                    thoiGianBatDau = thoiGianBatDau.AddMinutes(30);
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = $"Đặt {soCa} ca liên tiếp thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi tạo lịch: " + ex.Message });
            }
        }


        [HttpGet("hinh-thuc-tu-van")]
        public async Task<IActionResult> GetHinhThucTuVan()
        {
            var list = await _context.HinhThucTuVans.ToListAsync();
            return Ok(list);
        }
        [HttpGet("khoang-trong")]
        public async Task<IActionResult> GetKhoangTrongTheoNgay([FromQuery] int chuyenGiaId, [FromQuery] DateTime ngay)
        {
            // B1: Lấy thời gian rảnh của chuyên gia theo thứ
            int thu = (int)ngay.DayOfWeek;
            var timeRanges = await _context.ThoiGianRanhChuyenGia
                .Where(t => t.ChuyenGiaId == chuyenGiaId && t.ThuTrongTuan == thu)
                .ToListAsync();

            if (!timeRanges.Any())
                return Ok(new List<object>()); // không có thời gian rảnh

            // B2: Lấy lịch hẹn đã có trong ngày đó
            var lichDaCo = await _context.LichHens
                .Where(lh => lh.ChuyenGiaId == chuyenGiaId
                    && lh.ThoiGianBatDau.HasValue && lh.ThoiGianBatDau.Value.Date == ngay.Date)
                .ToListAsync();

            List<object> slotRanh = new();

            foreach (var range in timeRanges)
            {
                var start = ngay.Date.Add(range.Tu!.Value.ToTimeSpan());
                var end = ngay.Date.Add(range.Den!.Value.ToTimeSpan());

                while (start < end)
                {
                    var slotEnd = start.AddMinutes(30);

                    // Kiểm tra nếu trùng lịch thì bỏ qua
                    bool biTrung = lichDaCo.Any(lh =>
                        !(slotEnd <= lh.ThoiGianBatDau || start >= lh.ThoiGianKetThuc));

                    if (!biTrung)
                    {
                        slotRanh.Add(new
                        {
                            start = start.ToString("HH:mm"),
                            end = slotEnd.ToString("HH:mm")
                        });
                    }

                    start = slotEnd;
                }
            }

            return Ok(slotRanh);
        }

    }

        

public class TaoLichHenDto
{
    public int NguoiDungId { get; set; }
    public int ChuyenGiaId { get; set; }
    public int HinhThucId { get; set; }
    public DateTime ThoiGianBatDau { get; set; }
    public int SoCa { get; set; } = 1; // Số ca liên tiếp muốn đặt
    public string? TomTat { get; set; }
}


}

