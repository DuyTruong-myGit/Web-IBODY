using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IBODY_WebAPI.Models;

namespace IBODY_WebAPI.Controllers
{
    [ApiController]
    [Route("api/thoi-gian-ranh")]
    public class ThoiGianRanhChuyenGiaController : ControllerBase
    {
        private readonly FinalIbodyContext _context;
        public ThoiGianRanhChuyenGiaController(FinalIbodyContext context)
        {
            _context = context;
        }

        [HttpGet("chuyen-gia/{chuyenGiaId}")]
        public async Task<IActionResult> GetByChuyenGia(int chuyenGiaId)
        {
            var list = await _context.ThoiGianRanhChuyenGia
                .Where(t => t.ChuyenGiaId == chuyenGiaId)
                .Select(t => new {
                    t.Id,
                    t.ThuTrongTuan,
                    Tu = t.Tu.HasValue ? t.Tu.Value.ToString("HH:mm") : null,
                    Den = t.Den.HasValue ? t.Den.Value.ToString("HH:mm") : null
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ThoiGianRanhDto dto)
        {
            var entry = new ThoiGianRanhChuyenGium
            {
                ChuyenGiaId = dto.ChuyenGiaId,
                ThuTrongTuan = dto.ThuTrongTuan,
                Tu = dto.Tu,
                Den = dto.Den
            };
            _context.ThoiGianRanhChuyenGia.Add(entry);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã thêm thời gian rảnh." });
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ThoiGianRanhDto dto)
        {
            var entry = await _context.ThoiGianRanhChuyenGia.FindAsync(id);
            if (entry == null)
                return NotFound(new { message = "Không tìm thấy bản ghi." });

            entry.ThuTrongTuan = dto.ThuTrongTuan;
            entry.Tu = dto.Tu;
            entry.Den = dto.Den;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã cập nhật thời gian rảnh." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entry = await _context.ThoiGianRanhChuyenGia.FindAsync(id);
            if (entry == null)
                return NotFound(new { message = "Không tìm thấy bản ghi." });

            _context.ThoiGianRanhChuyenGia.Remove(entry);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa thời gian rảnh." });
        }
    }

    public class ThoiGianRanhDto
    {
        public int ChuyenGiaId { get; set; }
        public int? ThuTrongTuan { get; set; }
        public TimeOnly? Tu { get; set; }
        public TimeOnly? Den { get; set; }
    }
}
