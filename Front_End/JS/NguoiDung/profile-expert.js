// profile-expert.js hoàn chỉnh: đánh giá nếu đã từng có lịch, không cần "hoàn tất"
let expertTaiKhoanId = null;

function switchTab(tabIndex) {
  document.querySelectorAll(".tab-content").forEach((tab, index) => {
    tab.style.display = index === tabIndex ? "block" : "none";
  });
  document.querySelectorAll(".tab-button").forEach((btn, index) => {
    btn.classList.toggle("active", index === tabIndex);
  });
}

document.addEventListener("DOMContentLoaded", async () => {
  const currentUser = JSON.parse(localStorage.getItem("user"));
  if (!currentUser) {
    alert("Bạn cần đăng nhập để xem hồ sơ chuyên gia.");
    window.location.href = "../index.html";
    return;
  }
  const loginLink = document.getElementById("loginLink");
const userMenu = document.getElementById("userMenu");
const usernameDisplay = document.getElementById("usernameDisplay");
const avatarImg = document.querySelector(".user-button img");

if (loginLink && userMenu && usernameDisplay) {
  loginLink.style.display = "none";
  userMenu.style.display = "inline-block";
  usernameDisplay.innerText = currentUser.fullName || currentUser.username;

  if (avatarImg) {
    try {
      const res = await fetch(`http://localhost:5221/api/user/profile/${currentUser.taiKhoanId}`);
      const data = await res.json();
      avatarImg.src = data.avatarUrl
        ? `http://localhost:5221${data.avatarUrl}`
        : "/img/default-avatar.png";
    } catch (err) {
      console.error("❌ Lỗi khi tải avatar user:", err);
      avatarImg.src = "/img/default-avatar.png";
    }
  }
}
  
  const chuyenGiaId = new URLSearchParams(window.location.search).get("id");
  if (!chuyenGiaId) {
    alert("Không tìm thấy chuyên gia.");
    window.location.href = "search-expert.html";
    return;
  }

  
  try {
    const res = await fetch(`http://localhost:5221/api/tu-van/chuyen-gia/${chuyenGiaId}`);
    const data = await res.json();

    document.getElementById("expertName").textContent = data.hoTen;
    document.getElementById("expertEmail").textContent = data.email;
    document.getElementById("expertChuyenMon").textContent = data.chuyenMon;
    document.getElementById("expertKinhNghiem").textContent = data.soNamKinhNghiem;
    document.getElementById("expertChungChi").textContent = data.soChungChi;
    document.getElementById("expertGioiThieu").textContent = data.gioiThieu;
    expertTaiKhoanId = data.taiKhoanId;

    const avatarData = await (await fetch(`http://localhost:5221/api/chuyen-gia/thongTin/${expertTaiKhoanId}`)).json();
    document.querySelector(".expert-avatar").src = avatarData.avatarUrl ? `http://localhost:5221${avatarData.avatarUrl}` : "https://cdn-icons-png.flaticon.com/512/149/149071.png";

    const thongKe = await (await fetch(`http://localhost:5221/api/tu-van/avgDanhGia/${chuyenGiaId}`)).json();
    document.querySelector(".expert-rating").textContent = `⭐ ${thongKe.diemTrungBinh ?? 0} (${thongKe.soLuongDanhGia ?? 0} đánh giá)`;

    const list = document.getElementById("expertThoiGianRanh");
    const thuLabel = ["Chủ nhật", "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7"];

    data.thoiGianRanh?.forEach(t => {
      const li = document.createElement("li");
      const thuText = thuLabel[t.thuTrongTuan] ?? `Thứ ${t.thuTrongTuan}`;
      li.textContent = `${thuText}: ${t.tu} - ${t.den}`;
      list.appendChild(li);
    });

    // const dgList = document.getElementById("expertDanhGiaList");
    // dgList.innerHTML = "";
    // if (!data.danhGia || data.danhGia.length === 0) {
    //   dgList.innerHTML = "<li>Chưa có đánh giá nào.</li>";
    // } else {
    //   data.danhGia.forEach(dg => {
    //     const li = document.createElement("li");
    //     li.innerHTML = `<strong>${dg.nguoiDanhGia}</strong>: ${dg.diemSo}/5<br><em>\"${dg.nhanXet}\"</em>`;
    //     dgList.appendChild(li);
    //   });
    // }
  } catch (err) {
    console.error("Lỗi tải thông tin:", err);
    alert("Lỗi khi tải thông tin chuyên gia.");
  }
});
async function guiDanhGia() {
  const currentUser = JSON.parse(localStorage.getItem("user"));
  const diem = selectedRating;
  const nhanXet = document.getElementById("danhGiaNhanXet").value.trim();
  const chuyenGiaId = parseInt(new URLSearchParams(window.location.search).get("id"));
  
  // Hiển thị thông tin người dùng
  
  

  if (!diem || diem < 1 || diem > 5) return alert("Vui lòng chọn số sao từ 1 đến 5.");
  if (!nhanXet) return alert("Vui lòng nhập nhận xét.");

  try {
    const res = await fetch("http://localhost:5221/api/user/them_DanhGia", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        lichHenId: 0,
        nguoiDungId: currentUser.taiKhoanId,
        chuyenGiaId,
        diemSo: diem,
        nhanXet
      })
    });

    const text = await res.text();
    let result;
    try {
      result = JSON.parse(text);
    } catch {
      result = { message: text };
    }

    if (res.ok) {
      alert("✅ Đã gửi đánh giá!");
      location.reload();
    } else {
      alert("❌ " + (result.message || "Không thể gửi đánh giá."));
    }
  } catch (err) {
    console.error("Lỗi gửi đánh giá:", err);
    alert("Không thể gửi đánh giá.");
  }
}


const modal = document.getElementById("reportModal");
document.getElementById("openReportModalBtn")?.addEventListener("click", () => modal.style.display = "block");
document.querySelector(".close")?.addEventListener("click", () => modal.style.display = "none");
window.addEventListener("click", (e) => { if (e.target === modal) modal.style.display = "none"; });

document.getElementById("submitReportBtn")?.addEventListener("click", async () => {
  const currentUser = JSON.parse(localStorage.getItem("user"));
  const reason = document.getElementById("reportReason").value.trim();
  if (!reason || !expertTaiKhoanId) return alert("Thiếu thông tin tố cáo.");

  try {
    const res = await fetch("http://localhost:5221/api/bao-cao/gui", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        nguoiBaoCaoId: currentUser.taiKhoanId,
        chuyenGiaTaiKhoanId: expertTaiKhoanId,
        lyDo: reason
      })
    });
    const text = await res.text();
    const result = JSON.parse(text);
    if (res.ok) {
      alert(result.message || "Đã gửi tố cáo.");
      modal.style.display = "none";
      document.getElementById("reportReason").value = '';
    } else {
      alert("❌ " + (result.message || "Không rõ nguyên nhân."));
    }
  } catch (err) {
    alert("⚠️ Lỗi kết nối.");
  }
});

function datLich() {
  const id = new URLSearchParams(window.location.search).get("id");
  if (id) window.location.href = `datLich.html?chuyenGiaId=${id}`;
}
function toggleUserDropdown() {
  document.getElementById("userDropdown")?.classList.toggle("show");
}
document.addEventListener("click", (e) => {
  const menu = document.getElementById("userMenu");
  const dropdown = document.getElementById("userDropdown");
  if (menu && !menu.contains(e.target)) dropdown?.classList.remove("show");
});
document.getElementById("menu-toggle")?.addEventListener("click", () => {
  document.querySelector(".nav")?.classList.toggle("open");
});
function logout() {
  localStorage.removeItem("user");
  alert("Đăng xuất thành công!");
  window.location.href = "../index.html";
}

// Đánh giá sao
let selectedRating = 0;
const stars = document.querySelectorAll("#starRating .star");

stars.forEach(star => {
  star.addEventListener("mouseover", () => {
    const val = parseInt(star.getAttribute("data-value"));
    stars.forEach(s => {
      s.classList.toggle("hover", parseInt(s.getAttribute("data-value")) <= val);
    });
  });

  star.addEventListener("mouseout", () => {
    stars.forEach(s => s.classList.remove("hover"));
  });

  star.addEventListener("click", () => {
    selectedRating = parseInt(star.getAttribute("data-value"));
    stars.forEach(s => {
      s.classList.toggle("selected", parseInt(s.getAttribute("data-value")) <= selectedRating);
    });
  });
});
