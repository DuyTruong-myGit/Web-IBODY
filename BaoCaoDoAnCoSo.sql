create database BaoCaoDoAnCoSo
use BaoCaoDoAnCoSo
CREATE TABLE tai_khoan (
    id INT IDENTITY(1,1) PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    mat_khau VARCHAR(255) NULL,
    vai_tro VARCHAR(20) CHECK (vai_tro IN ('nguoi_dung', 'chuyen_gia', 'quan_tri')),
    trang_thai VARCHAR(20) CHECK (trang_thai IN ('hoat_dong', 'khoa', 'cho_duyet')),
    reset_token NVARCHAR(255),
    reset_token_expiry DATETIME
);

CREATE TABLE nguoi_dung (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tai_khoan_id INT FOREIGN KEY REFERENCES tai_khoan(id),
    ho_ten NVARCHAR(255),
    ngay_sinh DATE,
    gioi_tinh VARCHAR(10) CHECK (gioi_tinh IN ('nam', 'nu', 'khac')),
    muc_tieu_tam_ly NVARCHAR(MAX),
    avatar_url NVARCHAR(255)
);

CREATE TABLE chuyen_gia (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tai_khoan_id INT FOREIGN KEY REFERENCES tai_khoan(id),
    ho_ten NVARCHAR(255),
    so_nam_kinh_nghiem INT,
    so_chung_chi NVARCHAR(100),
    chuyen_mon NVARCHAR(MAX),
    gioi_thieu NVARCHAR(MAX),
    trang_thai VARCHAR(20) CHECK (trang_thai IN ('cho_duyet', 'xac_thuc', 'tu_choi')),
    avatar_url NVARCHAR(255),
    so_tai_khoan NVARCHAR(50),
    ten_ngan_hang NVARCHAR(255),
    anh_chung_chi NVARCHAR(255)
);

CREATE TABLE hoc_van_chuyen_gia (
    id INT IDENTITY(1,1) PRIMARY KEY,
    chuyen_gia_id INT FOREIGN KEY REFERENCES chuyen_gia(id),
    truong NVARCHAR(255),
    bang_cap NVARCHAR(255),
    chuyen_nganh NVARCHAR(255),
    nam_tot_nghiep INT
);

CREATE TABLE thoi_gian_ranh_chuyen_gia (
    id INT IDENTITY(1,1) PRIMARY KEY,
    chuyen_gia_id INT FOREIGN KEY REFERENCES chuyen_gia(id),
    thu_trong_tuan INT CHECK (thu_trong_tuan BETWEEN 0 AND 6),
    tu TIME,
    den TIME
);

CREATE TABLE hinh_thuc_tu_van (
    id INT IDENTITY(1,1) PRIMARY KEY,
    ten VARCHAR(20) CHECK (ten IN ('chat', 'video', 'call')),
    thoi_luong_phut INT,
    gia_co_ban DECIMAL(10,2)
);

CREATE TABLE lich_hen (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nguoi_dung_id INT FOREIGN KEY REFERENCES nguoi_dung(id),
    chuyen_gia_id INT FOREIGN KEY REFERENCES chuyen_gia(id),
    hinh_thuc_id INT FOREIGN KEY REFERENCES hinh_thuc_tu_van(id),
    thoi_gian_bat_dau DATETIME,
    thoi_gian_ket_thuc DATETIME,
    tom_tat NVARCHAR(MAX),
    trang_thai VARCHAR(20) CHECK (trang_thai IN ('cho_duyet', 'cho_thanh_toan', 'da_thanh_toan', 'da_huy', 'da_dien_ra')),
    NgayTao DATETIME DEFAULT GETDATE()
);

CREATE TABLE danh_gia (
    id INT IDENTITY(1,1) PRIMARY KEY,
    lich_hen_id INT FOREIGN KEY REFERENCES lich_hen(id),
    nguoi_dung_id INT FOREIGN KEY REFERENCES nguoi_dung(id),
    chuyen_gia_id INT FOREIGN KEY REFERENCES chuyen_gia(id),
    diem_so INT CHECK (diem_so BETWEEN 1 AND 5),
    nhan_xet NVARCHAR(MAX)
);

CREATE TABLE tin_nhan (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nguoi_gui_id INT FOREIGN KEY REFERENCES tai_khoan(id),
    nguoi_nhan_id INT FOREIGN KEY REFERENCES tai_khoan(id),
    noi_dung NVARCHAR(MAX),
    thoi_gian DATETIME
);

CREATE TABLE thong_bao (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tai_khoan_id INT FOREIGN KEY REFERENCES tai_khoan(id),
    loai VARCHAR(20) CHECK (loai IN ('he_thong', 'lich_hen', 'bao_cao')),
    noi_dung NVARCHAR(MAX),
    da_doc_luc DATETIME
);

CREATE TABLE goi_dich_vu (
    id INT IDENTITY(1,1) PRIMARY KEY,
    ten NVARCHAR(255),
    mo_ta NVARCHAR(MAX),
    gia DECIMAL(10,2),
    thoi_han_ngay INT,
    danh_cho VARCHAR(20) CHECK (danh_cho IN ('nguoi_dung', 'chuyen_gia')),
    so_luot INT NOT NULL DEFAULT 1
);

UPDATE goi_dich_vu SET so_luot = 4 WHERE ten LIKE '%Cơ bản%';
UPDATE goi_dich_vu SET so_luot = 8 WHERE ten LIKE '%Nâng cao%';

CREATE TABLE hoa_don (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tai_khoan_id INT FOREIGN KEY REFERENCES tai_khoan(id),
    goi_dich_vu_id INT FOREIGN KEY REFERENCES goi_dich_vu(id),
    tong_tien DECIMAL(10,2),
    thoi_gian_tao DATETIME
);

CREATE TABLE phuong_thuc_chung (
    id INT IDENTITY(1,1) PRIMARY KEY,
    ten NVARCHAR(50) UNIQUE NOT NULL,
    mo_ta NVARCHAR(MAX),
    trang_thai VARCHAR(10) CHECK (trang_thai IN ('hien', 'an')) DEFAULT 'hien'
);

CREATE TABLE giao_dich (
    id INT IDENTITY(1,1) PRIMARY KEY,
    hoa_don_id INT FOREIGN KEY REFERENCES hoa_don(id),
    phuong_thuc_id INT FOREIGN KEY REFERENCES phuong_thuc_chung(id),
    so_tien DECIMAL(10,2),
    thoi_gian DATETIME
);

ALTER TABLE giao_dich
ADD CONSTRAINT FK_giao_dich_phuong_thuc_chung
FOREIGN KEY (phuong_thuc_id) REFERENCES phuong_thuc_chung(id);

SELECT *
FROM giao_dich
WHERE phuong_thuc_id NOT IN (SELECT id FROM phuong_thuc_chung);

CREATE TABLE bao_cao_vi_pham (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nguoi_bao_cao_id INT FOREIGN KEY REFERENCES tai_khoan(id),
    loai_doi_tuong VARCHAR(20) CHECK (loai_doi_tuong IN ('binh_luan', 'bai_viet', 'nguoi_dung')),
    doi_tuong_id INT,
    ly_do NVARCHAR(MAX),
    thoi_gian DATETIME
);

CREATE TABLE goi_dang_ky (
  id INT IDENTITY(1,1) PRIMARY KEY,
  tai_khoan_id INT FOREIGN KEY REFERENCES tai_khoan(id),
  goi_dich_vu_id INT FOREIGN KEY REFERENCES goi_dich_vu(id),
  ngay_bat_dau DATE NOT NULL,
  ngay_ket_thuc DATE NOT NULL,
  so_luot_con_lai INT NOT NULL,
  trang_thai VARCHAR(20) CHECK (trang_thai IN ('con_hieu_luc', 'het_hieu_luc')) DEFAULT 'con_hieu_luc'
);

CREATE TABLE YeuCauNhanLuong (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ChuyenGiaId INT NOT NULL,
    SoCa INT NOT NULL,
    SoTien DECIMAL(18,2) NOT NULL,
    NgayTao DATETIME DEFAULT GETDATE(),
    TrangThai NVARCHAR(20) DEFAULT 'dang_cho',
    FOREIGN KEY (ChuyenGiaId) REFERENCES chuyen_gia(id)
);

CREATE TABLE YeuCauXacNhanGoiDichVu (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TaiKhoanId INT NOT NULL,
    GoiDichVuId INT NOT NULL,
    NoiDungChuyenKhoan NVARCHAR(255),
    TrangThai NVARCHAR(20) DEFAULT 'cho_duyet',
    NgayTao DATETIME DEFAULT GETDATE(),
	FOREIGN KEY (TaiKhoanId) REFERENCES tai_khoan(id)
);

CREATE TABLE PhanTichTamLy (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nguoi_dung_id INT FOREIGN KEY REFERENCES nguoi_dung(id),
    thoi_gian DATETIME DEFAULT GETDATE(),
    ket_luan NVARCHAR(MAX),
    muc_do_nguy_co VARCHAR(50) CHECK (muc_do_nguy_co IN ('thap', 'trung_binh', 'cao'))
);

CREATE TABLE LichSuDangNhap (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tai_khoan_id INT FOREIGN KEY REFERENCES tai_khoan(id),
    dia_chi_ip NVARCHAR(100),
    thoi_gian DATETIME DEFAULT GETDATE(),
    thiet_bi NVARCHAR(100)
);