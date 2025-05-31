

const BASE_API = "http://localhost:5221";

document.addEventListener("DOMContentLoaded", async () => {
  const user = JSON.parse(localStorage.getItem("user"));
  const chuyenGiaId = new URLSearchParams(window.location.search).get("chuyenGiaId");

  if (!user || !chuyenGiaId) {
    alert("Thiếu thông tin đăng nhập hoặc chuyên gia.");
    window.location.href = "search-expert.html";
    return;
  }

  // Hiển thị thông tin người dùng
  const loginLink = document.getElementById("loginLink");
  const userMenu = document.getElementById("userMenu");
  const usernameDisplay = document.getElementById("usernameDisplay");
  const avatarImg = document.querySelector(".user-button img");

  if (loginLink && userMenu && usernameDisplay) {
    loginLink.style.display = "none";
    userMenu.style.display = "inline-block";
    usernameDisplay.innerText = user.fullName || user.username;
    
  }

  // 1. Load thông tin người dùng
  const userRes = await fetch(`${BASE_API}/api/user/profile/${user.taiKhoanId}`);
  const userData = await userRes.json();
  if (avatarImg) {
  avatarImg.src = userData.avatarUrl
    ? `${BASE_API}${userData.avatarUrl}`
    : "/img/default-avatar.png";
}

  document.getElementById("fullName").value = userData.hoTen;
  document.getElementById("email").value = userData.email;
  document.getElementById("phone").value = "Không có";

  // 2. Load chuyên gia
  const expertRes = await fetch(`${BASE_API}/api/tu-van/chuyen-gia/${chuyenGiaId}`);
  const expert = await expertRes.json();
  const ratingRes = await fetch(`${BASE_API}/api/tu-van/avgDanhGia/${chuyenGiaId}`);
  const rating = await ratingRes.json();

  document.getElementById("expertName").innerHTML = `<strong>Họ tên:</strong> ${expert.hoTen}`;
  document.getElementById("expertChuyenMon").innerHTML = `<strong>Chuyên môn:</strong> ${expert.chuyenMon}`;
  document.getElementById("expertDanhGia").innerHTML = `<strong>Đánh giá:</strong> ⭐ ${rating.diemTrungBinh ?? 0} (${rating.soLuongDanhGia ?? 0} đánh giá)`;
  document.getElementById("expertGioiThieu").innerHTML = `<strong>Giới thiệu:</strong> ${expert.gioiThieu}`;

  // 3. Load hình thức tư vấn
  const htRes = await fetch(`${BASE_API}/api/dat-lich/hinh-thuc-tu-van`);
  const htList = await htRes.json();
  const hinhThucId = document.getElementById("hinhThucId");
  htList.forEach(ht => {
    const opt = document.createElement("option");
    opt.value = ht.id;
    opt.textContent = `${ht.ten} - ${ht.thoiLuongPhut} phút - ${ht.giaCoBan.toLocaleString()}₫`;
    hinhThucId.appendChild(opt);
  });

  // 4. Load slot khi chọn ngày
  document.getElementById("ngay").addEventListener("change", async () => {
    const date = document.getElementById("ngay").value;
    if (!date) return;

    const res = await fetch(`${BASE_API}/api/dat-lich/khoang-trong?chuyenGiaId=${chuyenGiaId}&ngay=${date}`);
    const slots = await res.json();

    const slotContainer = document.getElementById("slotContainer");
    slotContainer.innerHTML = "";

    if (slots.length === 0) {
      slotContainer.innerHTML = "❌ Không còn khung giờ trống.";
      return;
    }

    slots.forEach((s, i) => {
      const btn = document.createElement("button");
      btn.textContent = `${s.start} - ${s.end}`;
      btn.onclick = () => {
        document.querySelectorAll("#slotContainer button").forEach(b => b.classList.remove("selected"));
        btn.classList.add("selected");
        sessionStorage.setItem("slotIndex", i);
      };
      btn.dataset.index = i;
      slotContainer.appendChild(btn);
    });
  });

  // 5. Gửi form đặt lịch
  document.getElementById("bookingForm").addEventListener("submit", async (e) => {
    e.preventDefault();

    const index = sessionStorage.getItem("slotIndex");
    const date = document.getElementById("ngay").value;
    const soCa = Number(document.getElementById("soCa").value);
    const hinhThuc = Number(document.getElementById("hinhThucId").value);
    const tomTat = document.getElementById("notes").value.trim();

    if (index === null || !date || !hinhThuc || !tomTat) {
      return alert("❗ Vui lòng điền đầy đủ thông tin và chọn khung giờ.");
    }

    const resSlots = await fetch(`${BASE_API}/api/dat-lich/khoang-trong?chuyenGiaId=${chuyenGiaId}&ngay=${date}`);
    const slots = await resSlots.json();
    const slot = slots[index];
    if (!slot) return alert("❗ Khung giờ đã bị thay đổi. Vui lòng chọn lại.");

    const batDau = `${date}T${slot.start}:00`;

    const body = {
      nguoiDungId: userData.id,
      chuyenGiaId: parseInt(chuyenGiaId),
      hinhThucId: hinhThuc,
      thoiGianBatDau: batDau,
      soCa,
      tomTat
    };

    try {
      const res = await fetch(`${BASE_API}/api/dat-lich/tao-lich`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body)
      });

      const text = await res.text();
      let result;
      try {
        result = JSON.parse(text);
      } catch {
        result = { message: text };
      }

      if (!res.ok) {
        return alert("❌ Lỗi: " + (result.message || "Không xác định."));
      }

      alert(result.message || "✅ Đặt lịch thành công!");
      window.location.href = "lich-hen-user.html";
    } catch (err) {
      alert("❌ Lỗi kết nối đến server.");
      console.error(err);
    }
  });
});

// Dropdown menu
function toggleUserDropdown() {
  const dropdown = document.getElementById("userDropdown");
  dropdown?.classList.toggle("show");
}

document.addEventListener("click", function (e) {
  const menu = document.getElementById("userMenu");
  const dropdown = document.getElementById("userDropdown");
  if (menu && !menu.contains(e.target)) {
    dropdown?.classList.remove("show");
  }
});

function logout() {
  localStorage.removeItem("user");
  alert("Đăng xuất thành công!");
  window.location.href = "../index.html";
}
