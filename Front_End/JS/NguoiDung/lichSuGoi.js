const user = JSON.parse(localStorage.getItem("user"));

document.addEventListener("DOMContentLoaded", async () => {
  if (!user || !user.taiKhoanId) {
    alert("Vui lòng đăng nhập lại.");
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

  const tableBody = document.querySelector("#historyTable tbody");
  const apiUrl = `http://localhost:5221/api/user/lich-su-goi/${user.taiKhoanId}`;

  try {
    const res = await fetch(apiUrl);
    const data = await res.json();

    if (!Array.isArray(data) || data.length === 0) {
      tableBody.innerHTML = "<tr><td colspan='7'>Không có lịch sử đăng ký gói nào.</td></tr>";
      return;
    }

data.forEach(row => {
  const tr = document.createElement("tr");
  tr.innerHTML = `
    <td><a href="bill-details.html?id=${row.id}" class="link">${row.tenGoi}</a></td>
    <td>${row.gia.toLocaleString()}₫</td>
    <td>${new Date(row.ngayBatDau).toLocaleDateString()}</td>
    <td>${new Date(row.ngayKetThuc).toLocaleDateString()}</td>
    <td>${row.soLuot}</td>
    <td>${row.soLuotConLai}</td>
    <td>${row.trangThai === "con_hieu_luc" ? "Đang hoạt động" : "Hết hiệu lực"}</td>
  `;
  tableBody.appendChild(tr);
});

  } catch (err) {
    console.error("Lỗi tải lịch sử gói:", err);
    tableBody.innerHTML = "<tr><td colspan='7'>Không thể tải dữ liệu. Vui lòng thử lại sau.</td></tr>";
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
