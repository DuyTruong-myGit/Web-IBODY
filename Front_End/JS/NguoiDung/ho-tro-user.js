const API_BASE_URL = "http://localhost:5221/api";



document.addEventListener("DOMContentLoaded", async () => {
  const user = JSON.parse(localStorage.getItem("user"));
  const loginLink = document.getElementById("loginLink");
  const userMenu = document.getElementById("userMenu");
  const usernameDisplay = document.getElementById("usernameDisplay");
  const avatarImg = document.querySelector(".user-button img");

  if (user && loginLink && userMenu && usernameDisplay) {
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
  } else {
    if (userMenu) userMenu.style.display = "none";
  }
});


// ✅ Gửi yêu cầu hỗ trợ
const form = document.getElementById("supportForm");
form.addEventListener("submit", async function (e) {
  e.preventDefault();

  const topic = document.getElementById("supportTopic").value.trim();
  const content = document.getElementById("supportContent").value.trim();

  if (!topic || !content) {
    alert("Vui lòng nhập đầy đủ thông tin!");
    return;
  }

  const payload = {
    nguoiBaoCaoId: user.taiKhoanId,
    chuyenGiaTaiKhoanId: 0,
    lyDo: `[HỖ TRỢ] ${topic}: ${content}`
  };

  try {
    const res = await fetch(`${API_BASE_URL}/bao-cao/gui`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    if (res.ok) {
      alert("Yêu cầu hỗ trợ đã được gửi thành công!");
      form.reset();
    } else {
      const data = await res.json();
      alert(data.message || "Gửi yêu cầu thất bại.");
    }
  } catch (error) {
    console.error("Lỗi gửi hỗ trợ:", error);
    alert("Đã xảy ra lỗi trong quá trình gửi yêu cầu.");
  }
});

// ✅ Xử lý dropdown menu người dùng
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


// Dropdown
function toggleUserDropdown() {
  const dropdown = document.getElementById("userDropdown");
  dropdown?.classList.toggle("show");
}

document.addEventListener("click", (e) => {
  const menu = document.getElementById("userMenu");
  const dropdown = document.getElementById("userDropdown");
  if (menu && !menu.contains(e.target)) {
    dropdown?.classList.remove("show");
  }
});

document.getElementById("menu-toggle")?.addEventListener("click", () => {
  document.querySelector(".nav")?.classList.toggle("open");
});


function logout() {
  localStorage.removeItem("user");
  alert("Đăng xuất thành công!");
  window.location.href = "../index.html";
}
