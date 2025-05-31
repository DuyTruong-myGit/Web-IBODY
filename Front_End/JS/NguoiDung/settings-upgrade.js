const user = JSON.parse(localStorage.getItem("user"));

document.addEventListener("DOMContentLoaded", async() => {
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
    console.error("❌ Lỗi khi tải avatar người dùng:", err);
    avatarImg.src = "/img/default-avatar.png";
  }
}
  }

  // Xử lý form gửi yêu cầu nâng cấp
  const form = document.getElementById("upgradeForm");
  const responseDiv = document.getElementById("responseMessage");

  form.addEventListener("submit", async (e) => {
    e.preventDefault();

    const payload = {
      taiKhoanId: user.taiKhoanId,
      hoTen: document.getElementById("fullName").value,
      soNamKinhNghiem: parseInt(document.getElementById("experienceYears").value),
      soChungChi: document.getElementById("certificates").value,
      chuyenMon: document.getElementById("specialty").value,
      gioiThieu: document.getElementById("introduction").value,
      anhChungChi: "" // placeholder, thực tế backend đang upload riêng
    };

    try {
      const res = await fetch("http://localhost:5221/api/profile/request-upgrade", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      });

      const data = await res.json();

      if (!res.ok) throw new Error(data.message || "Gửi yêu cầu thất bại.");

      responseDiv.innerText = data.message;
      responseDiv.style.color = "green";

      // 🔽 Upload ảnh chứng chỉ nếu có
      const fileInput = document.getElementById("certificateImage");
      if (fileInput && fileInput.files.length > 0) {
        const file = fileInput.files[0];
        const formData = new FormData();
        formData.append("file", file);

        try {
          const uploadRes = await fetch(`http://localhost:5221/api/profile/upload-certificate/${user.taiKhoanId}`, {
            method: "POST",
            body: formData
          });

          const uploadData = await uploadRes.json();
          console.log("Upload ảnh chứng chỉ thành công:", uploadData);
        } catch (uploadErr) {
          console.error("Lỗi upload ảnh chứng chỉ:", uploadErr);
        }
      }

      form.reset();
    } catch (err) {
      responseDiv.innerText = err.message;
      responseDiv.style.color = "red";
    }
  });
});
