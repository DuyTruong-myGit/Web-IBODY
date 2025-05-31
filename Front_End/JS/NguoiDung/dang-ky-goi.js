
document.addEventListener("DOMContentLoaded", async () => {
  const user = JSON.parse(localStorage.getItem("user"));

  if (!user) {
    alert("Vui lòng đăng nhập để tiếp tục.");
    return window.location.href = "../index.html";
  }

  // ✅ Hiển thị avatar và tên người dùng
  const loginLink = document.getElementById("loginLink");
  const userMenu = document.getElementById("userMenu");
  const usernameDisplay = document.getElementById("usernameDisplay");
  const avatarImg = document.querySelector(".user-button img");

  if (loginLink && userMenu && usernameDisplay) {
    loginLink.style.display = "none";
    userMenu.style.display = "inline-block";
    usernameDisplay.innerText = user.fullName || user.username;

    if (avatarImg) {
  try {
    const res = await fetch(`http://localhost:5221/api/user/profile/${user.taiKhoanId}`);
    const data = await res.json();

    avatarImg.src = data.avatarUrl
      ? `http://localhost:5221${data.avatarUrl}`
      : "/img/default-avatar.png";
  } catch (err) {
    console.error("❌ Lỗi khi tải avatar:", err);
    avatarImg.src = "/img/default-avatar.png";
  }
}
  }

  const goiListEl = document.getElementById("goiList");
  const goiHienTaiEl = document.getElementById("goiHienTai");

  try {
    // ✅ Tải gói hiện tại
    const goiRes = await fetch(`http://localhost:5221/api/goi-dich-vu/dang-su-dung/${user.taiKhoanId}`);
    const goiText = await goiRes.text();
    let goiDangSuDung = null;

    if (goiText && goiText !== "null") {
      const parsed = JSON.parse(goiText);
      if (parsed && parsed.goiDichVuId && parsed.tenGoi) {
        goiDangSuDung = parsed;
      }
    }

    if (goiDangSuDung) {
      const conLai = goiDangSuDung.soLuotConLai;
      const ngayHetHan = new Date(goiDangSuDung.ngayKetThuc).toLocaleDateString();
      goiHienTaiEl.innerHTML = `
        <div class="goi-hientai">
          <h3>Gói hiện tại của bạn</h3>
          <p><strong>Gói:</strong> ${goiDangSuDung.tenGoi}</p>
          <p><strong>Còn lại:</strong> ${conLai} lượt</p>
          <p><strong>Hết hạn:</strong> ${ngayHetHan}</p>
          ${conLai <= 0 ? `<button onclick="giaHanGoi(${goiDangSuDung.goiDichVuId})">Gia hạn gói</button>` : ""}
          <button onclick="huyGoi()">Hủy gói</button>
        </div>
      `;
    } else {
      goiHienTaiEl.innerHTML = "<p>Hiện chưa có gói dịch vụ nào đang sử dụng.</p>";
    }

    // ✅ Tải danh sách gói
    const res = await fetch("http://localhost:5221/api/goi-dich-vu/danh-sach");
    const list = await res.json();

    if (!Array.isArray(list) || list.length === 0) {
      goiListEl.innerHTML = "<p>Không có gói dịch vụ khả dụng.</p>";
      return;
    }

    goiListEl.innerHTML = list.map(goi => `
      <div class="goi-item">
        <h4>${goi.ten}</h4>
        <p>${goi.moTa}</p>
        <p><strong>Giá:</strong> ${Number(goi.gia).toLocaleString()}₫</p>
        <p><strong>Thời hạn:</strong> ${goi.thoiHanNgay} ngày</p>
        <button onclick="chuyenTrangThanhToan(${goi.id})">Đăng ký</button>
      </div>
    `).join("");
  } catch (err) {
    console.error("❌ Lỗi tải dữ liệu:", err);
    goiListEl.innerHTML = "<p>Không thể tải dữ liệu.</p>";
  }
});

// ✅ Xử lý điều hướng và thao tác gói
async function chuyenTrangThanhToan(goiId) {
  const user = JSON.parse(localStorage.getItem("user"));
  const goiRes = await fetch(`http://localhost:5221/api/goi-dich-vu/dang-su-dung/${user.taiKhoanId}`);
  const goiText = await goiRes.text();
  let goiDangSuDung = null;

  if (goiText && goiText !== "null") {
    try {
      const parsed = JSON.parse(goiText);
      if (parsed && parsed.soLuotConLai > 0) {
        const confirmMsg = `⚠️ Bạn vẫn còn ${parsed.soLuotConLai} lượt trong gói "${parsed.tenGoi}".\n`
          + `Nếu bạn đăng ký gói mới, số lượt còn lại sẽ bị mất.\n\n`
          + `Bạn có chắc chắn muốn tiếp tục không?`;

        const ok = confirm(confirmMsg);
        if (!ok) return;
      }
    } catch {
      // không cần xử lý nếu không parse được
    }
  }

  // Tiếp tục điều hướng
  window.location.href = `payment-methods.html?goiId=${goiId}`;
}


async function giaHanGoi(goiDichVuId) {
  chuyenTrangThanhToan(goiDichVuId);
}

async function huyGoi() {
  const user = JSON.parse(localStorage.getItem("user"));
  if (!confirm("Bạn chắc chắn muốn hủy gói hiện tại?")) return;
  try {
    const res = await fetch(`http://localhost:5221/api/goi-dich-vu/huy-goi/${user.taiKhoanId}`, {
      method: "POST"
    });
    const text = await res.text();
    try {
      const data = JSON.parse(text);
      alert(data.message || "Đã hủy gói.");
    } catch {
      alert("Đã hủy gói.");
    }
    window.location.reload();
  } catch (err) {
    alert("❌ Lỗi khi hủy gói: " + err.message);
  }
}

// ✅ Dropdown menu
function toggleUserDropdown() {
  const dropdown = document.getElementById("userDropdown");
  dropdown.classList.toggle("show");
}

document.addEventListener("click", function (e) {
  const menu = document.getElementById("userMenu");
  const dropdown = document.getElementById("userDropdown");
  if (menu && !menu.contains(e.target)) {
    dropdown.classList.remove("show");
  }
});

// ✅ Logout
function logout() {
  localStorage.removeItem("user");
  alert("Đăng xuất thành công!");
  window.location.href = "../index.html";
}
