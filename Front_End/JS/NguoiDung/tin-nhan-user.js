document.addEventListener("DOMContentLoaded", () => {
  const user = JSON.parse(localStorage.getItem("user"));
  const loginLink = document.getElementById("loginLink");
  const userMenu = document.getElementById("userMenu");
  const usernameDisplay = document.getElementById("usernameDisplay");

  if (user) {
    loginLink.style.display = "none";
    userMenu.style.display = "inline-block";
    usernameDisplay.innerText = user.fullName || user.username;
  } else {
    userMenu.style.display = "none";
    alert("Bạn cần đăng nhập.");
    window.location.href = "../index.html";
    return;
  }
// if (user) {
//     // 👇 Tự động mở trang nhận cuộc gọi
//     if (!window.name || window.name !== "receiverWindow") {
//       window.open("../video-receiver.html", "_blank", "width=1,height=1,left=-1000,top=-1000");
//     }
//   }
  // Toggle menu responsive
  const toggleBtn = document.getElementById("menu-toggle");
  const nav = document.querySelector(".nav");
  toggleBtn?.addEventListener("click", () => {
    nav.classList.toggle("open");
  });

  // Toggle dropdown user
  const userButton = document.querySelector(".user-button");
  const userDropdown = document.getElementById("userDropdown");
  userButton?.addEventListener("click", () => {
    userDropdown.classList.toggle("show");
  });

  // Ẩn dropdown khi click ra ngoài
  document.addEventListener("click", function (e) {
    if (!userMenu.contains(e.target)) {
      userDropdown?.classList.remove("show");
    }
  });

  // Logout
  const logoutLinks = document.querySelectorAll('[onclick="logout()"]');
  logoutLinks.forEach(el => el.addEventListener("click", logout));

  initChat(); // Khởi tạo chat sau khi xác thực xong
});

function logout() {
  localStorage.removeItem("user");
  alert("Đăng xuất thành công!");
  window.location.href = "../index.html";
}

// =================== CHỨC NĂNG CHAT ===================
let selectedExpert = null;
let refreshInterval = null;

async function initChat() {
  const user = JSON.parse(localStorage.getItem("user"));
  await loadExpertList(user);

  document.getElementById("messageForm").addEventListener("submit", async (e) => {
    e.preventDefault();
    const content = document.getElementById("messageInput").value.trim();

    if (!selectedExpert || !selectedExpert.taiKhoanId || !content) {
      alert("Vui lòng chọn chuyên gia và nhập nội dung.");
      return;
    }

    try {
      const res = await fetch("http://localhost:5221/api/user/guiTinNhan", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          nguoiGuiId: user.taiKhoanId,
          nguoiNhanId: selectedExpert.taiKhoanId,
          noiDung: content
        })
      });

      if (!res.ok) {
        const err = await res.text();
        console.error("Gửi thất bại:", err);
        alert("Không thể gửi tin nhắn.");
        return;
      }

      document.getElementById("messageInput").value = "";
      await loadMessages(user, selectedExpert);
    } catch (err) {
      console.error("Lỗi gửi tin nhắn:", err);
    }
  });
}
async function loadExpertList(user) {
  try {
    const res = await fetch(`http://localhost:5221/api/user/danhSachChuyenGiaDangTuVan/${user.taiKhoanId}`);
    const experts = await res.json();
    const ul = document.getElementById("expertList");
    ul.innerHTML = "";

    // ✅ Lọc trùng theo taiKhoanId
    const uniqueExperts = [];
    const seenIds = new Set();
    experts.forEach(ex => {
      if (!seenIds.has(ex.taiKhoanId)) {
        seenIds.add(ex.taiKhoanId);
        uniqueExperts.push(ex);
      }
    });

    console.log("Danh sách chuyên gia (đã lọc):", uniqueExperts);

    uniqueExperts.forEach(expert => {
      const li = document.createElement("li");
      li.textContent = expert.hoTen;
      li.addEventListener("click", () => selectExpert(user, expert));
      ul.appendChild(li);
    });
  } catch (err) {
    console.error("Lỗi tải danh sách chuyên gia:", err);
  }
}


async function selectExpert(user, expert) {
  if (!expert || !expert.taiKhoanId) {
    alert("Không tìm thấy chuyên gia.");
    return;
  }

  selectedExpert = expert;
  document.getElementById("expertName").innerText = expert.hoTen;
  await loadMessages(user, expert);

  // Auto refresh mỗi 3 giây
  if (refreshInterval) clearInterval(refreshInterval);
  refreshInterval = setInterval(() => {
    loadMessages(user, expert);
  }, 3000);

  const callBtn = document.getElementById("callButton");
  callBtn.style.display = "inline-block";
  callBtn.onclick = () => {
    window.open(`../video-caller.html?to=${expert.taiKhoanId}`, "_blank");
  };
}


async function loadMessages(user, expert) {
  try {
    const res = await fetch(`http://localhost:5221/api/user/lichSuTinNhan?taiKhoan1=${user.taiKhoanId}&taiKhoan2=${expert.taiKhoanId}`);
    const messages = await res.json();

    if (!Array.isArray(messages)) {
      console.error("Phản hồi không hợp lệ:", messages);
      return;
    }

    const list = document.getElementById("messageList");
    list.innerHTML = messages.map(msg => `
      <div class="${msg.fromUserId === user.taiKhoanId ? 'sent' : 'received'}">
        <span>${msg.content}</span>
        <time>${new Date(msg.timestamp).toLocaleTimeString()}</time>
      </div>

    `).join("");

    list.scrollTop = list.scrollHeight;
  } catch (err) {
    console.error("Lỗi khi tải tin nhắn:", err);
  }
}


document.addEventListener("DOMContentLoaded", async () => {
  const user = JSON.parse(localStorage.getItem("user"));
  const avatarImg = document.querySelector(".user-button img");

  if (user && avatarImg) {
    try {
      const res = await fetch(`http://localhost:5221/api/user/profile/${user.taiKhoanId}`);
      const data = await res.json();

      avatarImg.src = data.avatarUrl
        ? `http://localhost:5221${data.avatarUrl}`
        : "/img/default-avatar.png"; // dùng / thay vì ../
    } catch (err) {
      console.error("Lỗi khi tải avatar:", err);
      avatarImg.src = "/img/default-avatar.png";
    }
  }
});
