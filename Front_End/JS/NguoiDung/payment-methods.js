const BASE_API = "http://localhost:5221";

document.addEventListener("DOMContentLoaded", async () => {
  const user = JSON.parse(localStorage.getItem("user"));
  const goiId = new URLSearchParams(window.location.search).get("goiId");

  if (!user || !goiId) {
    alert("Thiếu thông tin người dùng hoặc gói dịch vụ.");
    return;
  }
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
    const res = await fetch(`${BASE_API}/api/user/profile/${user.taiKhoanId}`);
    const data = await res.json();

    avatarImg.src = data.avatarUrl
      ? `${BASE_API}${data.avatarUrl}`
      : "/img/default-avatar.png";
  } catch (err) {
    console.error("Lỗi tải avatar:", err);
    avatarImg.src = "/img/default-avatar.png";
  }
}
  } 




  try {
    const res = await fetch(`${BASE_API}/api/goi-dich-vu/chi-tiet/${goiId}`);
    const text = await res.text();
    if (!res.ok) throw new Error("Không tìm thấy gói dịch vụ.");
    const goi = JSON.parse(text);

    // Hiển thị thông tin gói
    document.getElementById("goiTen").textContent = goi.ten;
    document.getElementById("goiGia").textContent = `${goi.gia.toLocaleString()}₫`;
    document.getElementById("goiThoiHan").textContent = `${goi.thoiHanNgay} ngày`;
    document.getElementById("goiSoLuot").textContent = `${goi.soLuot} lượt tư vấn`;
    document.getElementById("chuyenKhoanGhiChu").textContent = `user_${user.taiKhoanId}_goi_${goi.id}`;

    // Khi bấm "Tôi đã chuyển khoản"
    document.querySelector(".pay-btn").addEventListener("click", async () => {
      try {
        const res2 = await fetch(`${BASE_API}/api/user/yeu-cau-chuyen-khoan`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            taiKhoanId: user.taiKhoanId,
            goiDichVuId: goi.id
          })
        });

        const data = await res2.json();
        if (!res2.ok) throw new Error(data.message || "Gửi yêu cầu thất bại");

        alert("✅ Yêu cầu xác nhận đã được gửi. Vui lòng chờ admin duyệt!");
        window.location.href = "./lichSuGoi.html";
      } catch (err) {
        alert("❌ " + err.message);
      }
    });
  } catch (err) {
    console.error("Lỗi tải gói:", err);
    alert("Không thể tải thông tin gói dịch vụ.");
  }
});

// Dropdown user
function toggleUserDropdown() {
  const dropdown = document.getElementById("userDropdown");
  dropdown.classList.toggle("show");
}

document.addEventListener("click", function (e) {
  const menu = document.getElementById("userMenu");
  const dropdown = document.getElementById("userDropdown");
  if (!menu.contains(e.target)) {
    dropdown.classList.remove("show");
  }
});

document.addEventListener("DOMContentLoaded", () => {
  const toggleBtn = document.getElementById("menu-toggle");
  const nav = document.querySelector(".nav");
  toggleBtn?.addEventListener("click", () => {
    nav.classList.toggle("open");
  });
});

function logout() {
  localStorage.removeItem("user");
  alert("Đăng xuất thành công!");
  window.location.href = "../index.html";
}
