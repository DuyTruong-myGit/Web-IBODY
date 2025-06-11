using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using IBODY_WebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace IBODY_WebAPI.Controllers;
[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly FinalIbodyContext _context;

    public ProfileController(FinalIbodyContext context)
    {
        _context = context;
    }

    [HttpPost("request-upgrade")]
    public async Task<IActionResult> RequestUpgradeToExpert([FromBody] ExpertUpgradeDto dto)
    {
        // Kiểm tra tài khoản có tồn tại không
        var user = await _context.TaiKhoans.FindAsync(dto.TaiKhoanId);
        if (user == null)
            return NotFound(new { message = "Không tìm thấy tài khoản." });
        var expert = new ChuyenGium
        {
            TaiKhoanId = dto.TaiKhoanId,
            HoTen = dto.HoTen,
            SoNamKinhNghiem = dto.SoNamKinhNghiem,
            SoChungChi = dto.SoChungChi,
            AnhChungChi = dto.AnhChungChi,
            ChuyenMon = dto.ChuyenMon,
            GioiThieu = dto.GioiThieu,
            TrangThai = "cho_duyet"
        };

        _context.ChuyenGia.Add(expert);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã gửi yêu cầu nâng cấp. Vui lòng chờ xác nhận từ admin." });
    }

    [HttpPost("upload-certificate/{taiKhoanId}")]
    public async Task<IActionResult> UploadCertificate(int taiKhoanId, IFormFile file)
    {
        var chuyenGia = await _context.ChuyenGia.FirstOrDefaultAsync(cg => cg.TaiKhoanId == taiKhoanId);
        if (chuyenGia == null)
            return NotFound("Không tìm thấy chuyên gia.");

        if (file == null || file.Length == 0)
            return BadRequest("Vui lòng chọn file hợp lệ.");

        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "img");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var fileName = $"cert_{taiKhoanId}_{DateTime.Now.Ticks}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(folderPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        chuyenGia.AnhChungChi = $"/img/{fileName}";
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã tải ảnh chứng chỉ", url = chuyenGia.AnhChungChi });
    }

}

public class ExpertUpgradeDto
{
    public int TaiKhoanId { get; set; }
    public string HoTen { get; set; } = null!;
    public int SoNamKinhNghiem { get; set; }
    public string SoChungChi { get; set; } = null!;
    public string AnhChungChi { get; set; } = null!;
    public string ChuyenMon { get; set; } = null!;
    public string GioiThieu { get; set; } = null!;
}
