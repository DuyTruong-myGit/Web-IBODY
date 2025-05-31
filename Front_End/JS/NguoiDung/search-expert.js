const API_BASE_URL = "http://localhost:5221/api/tu-van";

document.addEventListener("DOMContentLoaded", async () => {
  const user = JSON.parse(localStorage.getItem("user"));
  if (!user) {
    alert("Vui lòng đăng nhập để tiếp tục.");
    return (window.location.href = "../index.html");
  }

  const loginLink = document.getElementById("loginLink");
  const userMenu = document.getElementById("userMenu");
  const usernameDisplay = document.getElementById("usernameDisplay");
  const avatarImg = document.querySelector(".user-button img");

  loginLink.style.display = "none";
  userMenu.style.display = "inline-block";
  usernameDisplay.innerText = user.fullName || user.username;

  // ✅ Sửa lỗi: bọc logic avatar trong async function
  if (user && avatarImg) {
    try {
      const res = await fetch(`http://localhost:5221/api/user/profile/${user.taiKhoanId}`);
      const data = await res.json();

      avatarImg.src = data.avatarUrl
        ? `http://localhost:5221${data.avatarUrl}`
        : "/img/default-avatar.png";
    } catch (err) {
      console.error("Lỗi khi tải avatar:", err);
      avatarImg.src = "/img/default-avatar.png";
    }
  }

  // ✅ Tiếp tục các sự kiện khác như bình thường...
  document.querySelector(".user-button")?.addEventListener("click", () => {
    document.getElementById("userDropdown")?.classList.toggle("show");
  });

  document.addEventListener("click", function (e) {
    if (!userMenu.contains(e.target)) {
      document.getElementById("userDropdown")?.classList.remove("show");
    }
  });

  document.querySelectorAll('[onclick="logout()"]').forEach(el =>
    el.addEventListener("click", logout)
  );

  document.getElementById("searchBtn").addEventListener("click", triggerSearch);
  document.getElementById("searchInput").addEventListener("keydown", (e) => {
    if (e.key === "Enter") triggerSearch();
  });

  loadChuyenMonOptions();
  loadExperts(); // Load tất cả ban đầu
});

function logout() {
  localStorage.removeItem("user");
  alert("Đăng xuất thành công!");
  window.location.href = "../index.html";
}

function triggerSearch() {
  const keyword = document.getElementById("searchInput").value;
  const chuyenMon = document.getElementById("specialtyFilter")?.value || "";
  const thu = document.getElementById("availabilityFilter")?.value || "";
  const kinhNghiem = document.getElementById("experienceFilter")?.value || "";

  loadExperts(keyword, chuyenMon, thu, kinhNghiem);
}

async function loadChuyenMonOptions() {
  try {
    const res = await fetch("http://localhost:5221/api/tu-van/danh-sach-chuyen-mon");
    const list = await res.json();
    const select = document.getElementById("specialtyFilter");

    list.forEach(mon => {
      const option = document.createElement("option");
      option.value = mon;
      option.textContent = mon;
      select.appendChild(option);
    });
  } catch (err) {
    console.error("Lỗi tải chuyên môn:", err);
  }
}

async function loadExperts(keyword = "", chuyenMon = "", thu = "", kinhNghiem = "") {
  try {
    const url = new URL(`${API_BASE_URL}/chuyen-gia`);
    if (keyword.trim()) url.searchParams.append("keyword", keyword.trim());
    if (chuyenMon.trim()) url.searchParams.append("chuyenMon", chuyenMon.trim());
    if (thu !== "") url.searchParams.append("thu", thu);
    if (kinhNghiem) url.searchParams.append("soNamKinhNghiem", kinhNghiem);

    const response = await fetch(url);
    const data = await response.json();

    const listContainer = document.getElementById("expertList");
    listContainer.innerHTML = "";

    if (!Array.isArray(data) || data.length === 0) {
      listContainer.innerHTML = "<p>Không tìm thấy chuyên gia nào phù hợp.</p>";
      return;
    }

    for (const expert of data) {
      const card = document.createElement("div");
      card.className = "expert-card";
      card.innerHTML = `
        <img src="http://localhost:5221${expert.avatarUrl || '/img/default-avatar.png'}" alt="Avatar">
        <h3>${expert.hoTen}</h3>
        <p><strong>Chuyên môn:</strong> ${expert.chuyenMon}</p>
        <p><strong>Kinh nghiệm:</strong> ${expert.soNamKinhNghiem} năm</p>
        <button onclick="viewExpertDetail(${expert.id})">Xem hồ sơ</button>
      `;
      listContainer.appendChild(card);
    }
  } catch (error) {
    console.error("Lỗi tải chuyên gia:", error);
    document.getElementById("expertList").innerHTML =
      "<p>Đã xảy ra lỗi khi tải dữ liệu.</p>";
  }
}

function viewExpertDetail(id) {
  window.location.href = `profile-expert.html?id=${id}`;
}
