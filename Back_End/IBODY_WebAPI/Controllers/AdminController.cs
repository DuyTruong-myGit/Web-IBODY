using IBODY_WebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using IBODY_WebAPI.Data;
using Microsoft.AspNetCore.Identity;


namespace IBODY_WebAPI.Controllers
{
    [AllowAnonymous]
    // [Authorize(Roles = "quan_tri")]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        [HttpGet("dashboard")]
    public IActionResult GetAdminInfo()
    {
        return Ok("Xin chào quản trị viên!");
    }
        private readonly FinalIbodyContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(FinalIbodyContext context,UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        [HttpGet("accounts")]
        public async Task<IActionResult> GetAllAccounts()
        {
            var identityUsers = _userManager.Users.ToList(); 
            var taiKhoans = await _context.TaiKhoans.ToListAsync();

            var result = taiKhoans.Select(tk =>
            {
                var identity = identityUsers.FirstOrDefault(u => u.Email == tk.Email);

                return new
                {
                    id = tk.Id,
                    email = tk.Email,
                    vaiTro = tk.VaiTro,
                    trangThai = tk.TrangThai,
                    identityId = identity?.Id,
                    fullName = identity?.FullName ?? ""
                };
            }).ToList();

            return Ok(new
            {
                count = result.Count,
                data = result
            });
        }



        // Xóa tài khoản 
        [HttpDelete("account/{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var taiKhoan = await _context.TaiKhoans.FindAsync(id);
            if (taiKhoan == null)
                return NotFound(new { message = "Không tìm thấy tài khoản." });

            // Nếu là người dùng: xoá bản ghi liên quan trong bảng nguoi_dung
            var nguoiDung = await _context.NguoiDungs
                .FirstOrDefaultAsync(nd => nd.TaiKhoanId == id);
            if (nguoiDung != null)
                _context.NguoiDungs.Remove(nguoiDung);

            
            var chuyenGia = await _context.ChuyenGia
                .FirstOrDefaultAsync(cg => cg.TaiKhoanId == id);
            if (chuyenGia != null)
                _context.ChuyenGia.Remove(chuyenGia);

            // xóa ở bảng identity
            var identityUser = await _userManager.FindByEmailAsync(taiKhoan.Email);
            if (identityUser != null)
            {
                await _userManager.DeleteAsync(identityUser);
            }

            // Xoá tài khoản
            _context.TaiKhoans.Remove(taiKhoan);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa tài khoản và dữ liệu liên quan." });
        }


        [HttpGet("expert-requests")]
        public async Task<IActionResult> GetPendingExpertRequests()
        {
        var pending = await _context.ChuyenGia
        .Where(cg => cg.TrangThai == "cho_duyet")
        .Select(cg => new
        {
            cg.Id,
            cg.TaiKhoanId,
            cg.HoTen,
            cg.SoNamKinhNghiem,
            cg.SoChungChi,
            cg.AnhChungChi,
            cg.ChuyenMon,
            cg.GioiThieu,
            cg.TrangThai
        }).ToListAsync();

         return Ok(new
            {
                count = pending.Count,
                data = pending
            });
        }   


        [HttpPost("expert-approve/{id}")]
        public async Task<IActionResult> ApproveExpert(int id)
        {
            var expert = await _context.ChuyenGia.FindAsync(id);
            if (expert == null)
                return NotFound(new { message = "Không tìm thấy hồ sơ chuyên gia cần duyệt." });

            if (expert.TrangThai != "cho_duyet")
                return BadRequest(new { message = "Hồ sơ đã được xử lý." });

            expert.TrangThai = "xac_thuc";

            //  Cập nhật tài khoản
            var account = await _context.TaiKhoans.FindAsync(expert.TaiKhoanId);
            if (account != null)
            {
                account.VaiTro = "chuyen_gia";

                // Xóa bản ghi người dùng nếu có (nâng cấp)
                var nguoiDung = await _context.NguoiDungs
                    .FirstOrDefaultAsync(nd => nd.TaiKhoanId == account.Id);
                if (nguoiDung != null)
                    _context.NguoiDungs.Remove(nguoiDung);

                
                var identityUser = await _userManager.FindByEmailAsync(account.Email);
                if (identityUser != null)
                {
                    var roles = await _userManager.GetRolesAsync(identityUser);
                    if (!roles.Contains("chuyen_gia"))
                    {
                        await _userManager.AddToRoleAsync(identityUser, "chuyen_gia");
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã duyệt nâng cấp thành chuyên gia." });
        }




        [HttpPost("expert-reject/{id}")]
        public async Task<IActionResult> RejectExpert(int id)
        {
            var expert = await _context.ChuyenGia.FindAsync(id);
            if (expert == null)
                return NotFound(new { message = "Không tìm thấy chuyên gia." });

            if (expert.TrangThai != "cho_duyet")
                return BadRequest(new { message = "Chuyên gia đã được xử lý. Không thể từ chối nữa." });

            expert.TrangThai = "tu_choi";

            // ✅ Xoá role "chuyen_gia" khỏi hệ thống Identity nếu có
            var taiKhoan = await _context.TaiKhoans.FindAsync(expert.TaiKhoanId);
            if (taiKhoan != null)
            {
                var identityUser = await _userManager.FindByEmailAsync(taiKhoan.Email);
                if (identityUser != null)
                {
                    var roles = await _userManager.GetRolesAsync(identityUser);
                    if (roles.Contains("chuyen_gia"))
                    {
                        await _userManager.RemoveFromRoleAsync(identityUser, "chuyen_gia");
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã từ chối yêu cầu nâng cấp." });
        }   


        // hiển thị toàn bộ lịch hẹn đang có trên hệ thống
        [HttpGet("lich-hen")]
        public async Task<IActionResult> GetAllLichHen()
        {
            var lich = await _context.LichHens
                .Include(lh => lh.NguoiDung)
                .Include(lh => lh.ChuyenGia)
                .Include(lh => lh.HinhThuc)
                .Select(lh => new
                {
                    lh.Id,
                    lh.ThoiGianBatDau,
                    lh.ThoiGianKetThuc,
                    lh.TomTat,
                    NguoiDung = new
                    {
                        lh.NguoiDung.Id,
                        lh.NguoiDung.HoTen
                    },
                    ChuyenGia = new
                    {
                        lh.ChuyenGia.Id,
                        lh.ChuyenGia.HoTen,
                        lh.ChuyenGia.ChuyenMon
                    },
                    HinhThuc = lh.HinhThuc.Ten
                })
                .OrderByDescending(lh => lh.ThoiGianBatDau)
                .ToListAsync();

             return Ok(new
                {
                    count = lich.Count,
                    data = lich
                });
        }

        // Cập nhật lịch hẹn trên hệ thống
        [HttpPut("lich-hen/{id}")]
        public async Task<IActionResult> UpdateLichHen(int id, [FromBody] UpdateLichHenDto dto)
        {
            var lich = await _context.LichHens.FindAsync(id);
            if (lich == null)
                return NotFound(new { message = "Không tìm thấy lịch hẹn." });
            // if (lich.TrangThai == "da_thanh_toan" || lich.TrangThai == "da_dien_ra")
            // {
            //     return BadRequest(new { message = "Lịch đã thanh toán hoặc đã kết thúc, không thể chỉnh sửa." });
            // }
            lich.ThoiGianBatDau = dto.ThoiGianBatDau;
            lich.ThoiGianKetThuc = dto.ThoiGianKetThuc;
            lich.TomTat = dto.TomTat;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật lịch hẹn thành công." });
        }



        [HttpDelete("lich-hen/{id}")]
        public async Task<IActionResult> DeleteLichHen(int id)
        {
            var lich = await _context.LichHens.FindAsync(id);
            if (lich == null)
                return NotFound(new { message = "Không tìm thấy lịch hẹn." });

            // Kiểm tra nếu lịch đã qua thời điểm hiện tại thì không cho xóa
            var now = DateTime.Now;
            if (lich.ThoiGianKetThuc.HasValue && lich.ThoiGianKetThuc.Value < now)
            {
                return BadRequest(new { message = "Không thể xóa lịch hẹn đã kết thúc." });
            }

            _context.LichHens.Remove(lich);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa lịch hẹn thành công." });
        }

        // Hiển thị danh sách báo cáo vi phạm của người dùng
        [HttpGet("bao-cao-chuyen-gia")]
        public async Task<IActionResult> GetBaoCaoChuyenGia()
        {
            var danhSach = await _context.BaoCaoViPhams
                .Where(bc => bc.LoaiDoiTuong == "chuyen_gia")
                .Select(bc => new
                {
                    bc.Id,
                    bc.NguoiBaoCaoId,
                    DoiTuongId = bc.DoiTuongId,
                    bc.LyDo,
                    bc.ThoiGian,
                    EmailNguoiBiBaoCao = _context.TaiKhoans
                        .Where(tk => tk.Id == bc.DoiTuongId)
                        .Select(tk => tk.Email)
                        .FirstOrDefault(),
                    EmailNguoiBaoCao = _context.TaiKhoans
                        .Where(tk => tk.Id == bc.NguoiBaoCaoId)
                        .Select(tk => tk.Email)
                        .FirstOrDefault()
                })
                .OrderByDescending(bc => bc.ThoiGian)
                .ToListAsync();

            return Ok(new
            {
                data = danhSach
            });
        }

        // khóa tài khoản chuyên gia
        [HttpPost("khoa-tai-khoan/{id}")]
        public async Task<IActionResult> KhoaTaiKhoan(int id)
        {
            var tk = await _context.TaiKhoans.FindAsync(id);
            if (tk == null)
                return NotFound(new { message = "Không tìm thấy tài khoản." });

            tk.TrangThai = "khoa";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã khóa tài khoản chuyên gia." });
        }
        // mở khóa tài khoản chuyên gia
        [HttpPost("mo-khoa-tai-khoan/{id}")]
        public async Task<IActionResult> MoKhoaTaiKhoan(int id)
        {
            var tk = await _context.TaiKhoans.FindAsync(id);
            if (tk == null)
                return NotFound(new { message = "Không tìm thấy tài khoản." });

            if (tk.TrangThai != "khoa")
                return BadRequest(new { message = "Tài khoản không bị khóa." });

            tk.TrangThai = "hoat_dong";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã mở khóa tài khoản thành công." });
        }

        // Lấy danh sách tất cả phương thức thanh toán hệ thống
        [HttpGet("he-thong-phuong-thuc")]
        public async Task<IActionResult> GetAllSystemMethods()
        {
            var list = await _context.PhuongThucChungs
                .OrderBy(p => p.Id)
                .Select(p => new
                {
                    p.Id,
                    p.Ten,
                    p.MoTa,
                    p.TrangThai
                })
                .ToListAsync();

            return Ok(list);
        }

        //  Thêm phương thức mới
        [HttpPost("themThanhToan")]
        public async Task<IActionResult> AddSystemMethod([FromBody] ThemPhuongThucDto dto)
        {
            if (await _context.PhuongThucChungs.AnyAsync(p => p.Ten == dto.Ten))
                return BadRequest(new { message = "Phương thức đã tồn tại." });

            var newMethod = new PhuongThucChung
            {
                Ten = dto.Ten,
                MoTa = dto.MoTa,
                TrangThai = dto.TrangThai ?? "hien"
            };

            _context.PhuongThucChungs.Add(newMethod);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã thêm phương thức mới." });
        }

        //  Cập nhật phương thức
        [HttpPut("capNhatPhuongThucThanhToan/{id}")]
        public async Task<IActionResult> UpdateSystemMethod(int id, [FromBody] ThemPhuongThucDto dto)
        {
            var pt = await _context.PhuongThucChungs.FindAsync(id);
            if (pt == null)
                return NotFound(new { message = "Không tìm thấy phương thức." });

            pt.Ten = dto.Ten;
            pt.MoTa = dto.MoTa;
            pt.TrangThai = dto.TrangThai ?? pt.TrangThai;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã cập nhật phương thức." });
        }

        //Xóa phương thức
        [HttpDelete("xoaPhuongThucThanhToan/{id}")]
        public async Task<IActionResult> DeleteSystemMethod(int id)
        {
            var pt = await _context.PhuongThucChungs.FindAsync(id);
            if (pt == null)
                return NotFound(new { message = "Không tìm thấy phương thức." });

            _context.PhuongThucChungs.Remove(pt);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa phương thức thành công." });
        }



        
        // Lấy lịch sử chat
        [HttpGet("chat/lich-su")]
        public async Task<IActionResult> LichSuChatAdmin([FromQuery] int taiKhoan1, [FromQuery] int taiKhoan2)
        {
            var tinNhanList = await _context.TinNhans
                .Where(t =>
                    (t.NguoiGuiId == taiKhoan1 && t.NguoiNhanId == taiKhoan2) ||
                    (t.NguoiGuiId == taiKhoan2 && t.NguoiNhanId == taiKhoan1))
                .OrderBy(t => t.ThoiGian)
                .Select(t => new
                {
                    t.Id,
                    t.NguoiGuiId,
                    t.NguoiNhanId,
                    t.NoiDung,
                    t.ThoiGian
                })
                .ToListAsync();

            return Ok(tinNhanList);
        }

        //Xóa 1 tin nhắn
        [HttpDelete("chat/xoa1TinNhan/{id}")]
        public async Task<IActionResult> XoaTinNhan(int id)
        {
            var tinNhan = await _context.TinNhans.FindAsync(id);
            if (tinNhan == null)
                return NotFound(new { message = "Không tìm thấy tin nhắn." });

            _context.TinNhans.Remove(tinNhan);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa tin nhắn." });
        }

        //Xóa toàn bộ đoạn chat giữa 2 tài khoản
        [HttpDelete("chat/xoaToanBo")]
        public async Task<IActionResult> XoaDoanChat([FromQuery] int taiKhoan1, [FromQuery] int taiKhoan2)
        {
            var tinNhanList = await _context.TinNhans
                .Where(t =>
                    (t.NguoiGuiId == taiKhoan1 && t.NguoiNhanId == taiKhoan2) ||
                    (t.NguoiGuiId == taiKhoan2 && t.NguoiNhanId == taiKhoan1))
                .ToListAsync();

            _context.TinNhans.RemoveRange(tinNhanList);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa toàn bộ đoạn chat giữa 2 tài khoản." });
        }


       
        [HttpGet("thong-ke-luong-chuyen-gia")]
        public async Task<IActionResult> TinhLuongChuyenGia(int thang, int nam, decimal luongMotBuoi = 100000)
        {
            var lichHen = await _context.LichHens
                .Where(lh => lh.TrangThai == "da_dien_ra"
                          && lh.ThoiGianKetThuc.HasValue
                          && lh.ThoiGianKetThuc.Value.Month == thang
                          && lh.ThoiGianKetThuc.Value.Year == nam)
                .GroupBy(lh => lh.ChuyenGiaId)
                .Select(g => new
                {
                    chuyenGiaId = g.Key,
                    soBuoi = g.Count(),
                    luongMotBuoi,
                    tongLuong = g.Count() * luongMotBuoi
                })
                .ToListAsync();

            return Ok(lichHen);
        }
       
        // Lấy danh sách đánh giá của chuyên gia
        [HttpGet("danhGiaCuaChuyenGia")]
        public async Task<IActionResult> GetAllDanhGiaChuyenGia()
        {
            var list = await _context.DanhGia
                .Include(dg => dg.NguoiDung).ThenInclude(nd => nd.TaiKhoan)
                .Include(dg => dg.ChuyenGia).ThenInclude(cg => cg.TaiKhoan)
                .Select(dg => new
                {
                    dg.Id,
                    NguoiDanhGia = dg.NguoiDung.HoTen,
                    EmailNguoiDung = dg.NguoiDung.TaiKhoan.Email,
                    ChuyenGia = dg.ChuyenGia.HoTen,
                    EmailChuyenGia = dg.ChuyenGia.TaiKhoan.Email,
                    DiemSo = dg.DiemSo,
                    NhanXet = dg.NhanXet
                })
                .OrderByDescending(d => d.Id)
                .ToListAsync();

            return Ok( new { count = list.Count, data = list });
        }

        // Xóa đánh giá sai sự thật
        [HttpDelete("xoaDanhGia/{id}")]
        public async Task<IActionResult> XoaDanhGiaChuyenGia(int id)
        {
            var danhGia = await _context.DanhGia.FindAsync(id);
            if (danhGia == null)
                return NotFound(new { message = "Không tìm thấy đánh giá." });

            _context.DanhGia.Remove(danhGia);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xoá đánh giá thành công." });
        }

        [HttpGet("yeu-cau-nhan-luong")]
        public async Task<IActionResult> DanhSachYeuCauNhanLuong()
        {
            var list = await _context.YeuCauNhanLuongs
                .Include(y => y.ChuyenGia)
                .ThenInclude(cg => cg.TaiKhoan)
                .OrderByDescending(y => y.NgayTao)
                .ToListAsync();

            return Ok(list.Select(y => new {
                y.Id,
                y.ChuyenGia.HoTen,
                Email = y.ChuyenGia.TaiKhoan.Email,
                y.SoCa,
                y.SoTien,
                y.NgayTao,
                y.TrangThai,
                y.ChuyenGia.SoTaiKhoan,
                y.ChuyenGia.TenNganHang

            }));
        }

        [HttpPost("duyet-yeu-cau-luong/{id}")]
        public async Task<IActionResult> DuyetYeuCau(int id)
        {
            var yc = await _context.YeuCauNhanLuongs.FindAsync(id);
            if (yc == null) return NotFound();

            yc.TrangThai = "da_duyet";
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã duyệt yêu cầu nhận lương." });
        }

        [HttpPost("tu-choi-yeu-cau-luong/{id}")]
        public async Task<IActionResult> TuChoiYeuCau(int id)
        {
            var yc = await _context.YeuCauNhanLuongs.FindAsync(id);
            if (yc == null) return NotFound();

            yc.TrangThai = "tu_choi";
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã từ chối yêu cầu nhận lương." });
        }



        [HttpGet("yeu-cau-chuyen-khoan")]
        public async Task<IActionResult> DanhSachYeuCauChuyenKhoan()
        {
            var list = await _context.YeuCauXacNhanGoiDichVus
                .Where(y => y.TrangThai == "cho_duyet")
                .OrderByDescending(y => y.NgayTao)
                .Select(y => new {
                    y.Id,
                    y.TaiKhoanId,
                    TenGoi = _context.GoiDichVus.FirstOrDefault(g => g.Id == y.GoiDichVuId)!.Ten,
                    Email = _context.TaiKhoans.FirstOrDefault(t => t.Id == y.TaiKhoanId)!.Email,
                    y.NoiDungChuyenKhoan,
                    y.NgayTao
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost("duyet-chuyen-khoan/{id}")]
        public async Task<IActionResult> DuyetChuyenKhoan(int id)
        {
            var yc = await _context.YeuCauXacNhanGoiDichVus.FindAsync(id);
            if (yc == null || yc.TrangThai != "cho_duyet")
                return NotFound(new { message = "Yêu cầu không tồn tại hoặc đã xử lý." });

            // Check: User đã có gói con_hieu_luc chưa?
            var goiCon = await _context.GoiDangKies
                .AnyAsync(g => g.TaiKhoanId == yc.TaiKhoanId && g.TrangThai == "con_hieu_luc");

            if (goiCon)
                return BadRequest(new { message = "Người dùng hiện đã có gói đang hoạt động." });

            // Lấy gói gốc
            var goi = await _context.GoiDichVus.FindAsync(yc.GoiDichVuId);
            if (goi == null || goi.ThoiHanNgay == null || goi.SoLuot == null)
                return BadRequest(new { message = "Gói dịch vụ không hợp lệ hoặc thiếu cấu hình." });

            // Tạo gói mới
            var now = DateTime.Now;
            var dangKy = new GoiDangKy
            {
                TaiKhoanId = yc.TaiKhoanId,
                GoiDichVuId = yc.GoiDichVuId,
                NgayBatDau = now,
                NgayKetThuc = now.AddDays(goi.ThoiHanNgay.Value),
                SoLuotConLai = goi.SoLuot,
                TrangThai = "con_hieu_luc"
            };
            _context.GoiDangKies.Add(dangKy);

            yc.TrangThai = "da_duyet";

            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ Đã duyệt và kích hoạt gói dịch vụ." });
        }


        [HttpPost("tu-choi-chuyen-khoan/{id}")]
        public async Task<IActionResult> TuChoiChuyenKhoan(int id)
        {
            var yc = await _context.YeuCauXacNhanGoiDichVus.FindAsync(id);
            if (yc == null)
                return NotFound(new { message = "Không tìm thấy yêu cầu." });

            if (yc.TrangThai != "cho_duyet")
                return BadRequest(new { message = "Yêu cầu này đã được xử lý trước đó." });

            yc.TrangThai = "tu_choi";
            await _context.SaveChangesAsync();

            return Ok(new { message = "❌ Đã từ chối yêu cầu xác nhận chuyển khoản." });
        }

        [HttpGet("lich-su-chuyen-khoan")]
        public async Task<IActionResult> GetLichSuChuyenKhoan()
        {
            var lichSuList = await _context.YeuCauXacNhanGoiDichVus
                .Where(y => y.TrangThai == "da_duyet")
                .OrderByDescending(y => y.NgayTao)
                .Select(y => new
                {
                    y.Id,
                    y.TaiKhoanId,
                    TenGoi = _context.GoiDichVus.FirstOrDefault(g => g.Id == y.GoiDichVuId).Ten,
                    Email = _context.TaiKhoans.FirstOrDefault(t => t.Id == y.TaiKhoanId).Email,
                    y.NoiDungChuyenKhoan,
                    y.NgayTao
                })
                .ToListAsync();

            return Ok(lichSuList);
        }
        
        [HttpGet("dashboard/thong-ke-tong-quan")]
        public async Task<IActionResult> GetThongKeTongQuan()
        {
            var soTaiKhoan = await _context.TaiKhoans.CountAsync();
            var soChuyenGia = await _context.TaiKhoans.CountAsync(tk => tk.VaiTro == "chuyen_gia");
            var soYeuCauNangCap = await _context.ChuyenGia.CountAsync(cg => cg.TrangThai == "cho_duyet");
            var soLichHen = await _context.LichHens.CountAsync();
            var soBaoCao = await _context.BaoCaoViPhams.CountAsync(bc => bc.LoaiDoiTuong == "chuyen_gia");

            return Ok(new
            {
                taiKhoan = soTaiKhoan,
                chuyenGia = soChuyenGia,
                yeuCauNangCap = soYeuCauNangCap,
                lichHen = soLichHen,
                baoCao = soBaoCao
            });
        }




    }



    public class UpdateAccountDto
    {
        public string VaiTro { get; set; } = null!;
        public string TrangThai { get; set; } = null!;
    }

    public class UpdateLichHenDto
    {
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianKetThuc { get; set; }
        public string? TomTat { get; set; }
    }
        public class ThemPhuongThucDto
    {
        public string Ten { get; set; } = null!;
        public string? MoTa { get; set; }
        public string? TrangThai { get; set; } 
    }
    
}
