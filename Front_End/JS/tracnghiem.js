function renderQuestions() {
  const form = document.getElementById("dassForm");
  form.innerHTML = "";

  questions.forEach((q, index) => {
    const qDiv = document.createElement("div");
    qDiv.classList.add("question");
    qDiv.innerHTML = `<p><strong>Câu ${index + 1}:</strong> ${q.text}</p>`;

    q.options.forEach((opt, i) => {
      const [label, category] = opt;
      const value = category;

      const inputId = `q${index}_opt${i}`;
      qDiv.innerHTML += `
        <label for="${inputId}">
          <input type="radio" name="q${index}" id="${inputId}" value="${value}" required>
          ${label}
        </label><br>
      `;
    });

    form.appendChild(qDiv);
  });
}

function calculateResult(form) {
  const formData = new FormData(form);
  const score = { anxiety: 0, depression: 0, stress: 0 };

  for (let [_, value] of formData.entries()) {
    if (score[value] !== undefined) {
      score[value]++;
    }
  }

  return score;
}

function interpret(score) {
  function range(levels, value) {
    for (const [level, min, max] of levels) {
      if (value >= min && value <= max) return level;
    }
    return "Không xác định";
  }

  return {
    anxiety: range([
      ["Bình thường", 0, 7],
      ["Nhẹ", 8, 9],
      ["Vừa", 10, 14],
      ["Nặng", 15, 19],
      ["Rất nặng", 20, 40]
    ], score.anxiety),

    depression: range([
      ["Bình thường", 0, 9],
      ["Nhẹ", 10, 13],
      ["Vừa", 14, 20],
      ["Nặng", 21, 27],
      ["Rất nặng", 28, 40]
    ], score.depression),

    stress: range([
      ["Bình thường", 0, 14],
      ["Nhẹ", 15, 18],
      ["Vừa", 19, 25],
      ["Nặng", 26, 33],
      ["Rất nặng", 34, 40]
    ], score.stress)
  };
}

document.addEventListener("DOMContentLoaded", async() => {
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

  renderQuestions();

  const form = document.getElementById("dassForm");
  form.addEventListener("submit", (e) => {
    e.preventDefault();
    const score = calculateResult(form);
    const result = interpret(score);

    // Xác định điểm cao nhất
    const maxScore = Math.max(score.anxiety, score.depression, score.stress);
    const highest = Object.entries(score)
      .filter(([_, v]) => v === maxScore)
      .map(([k]) => k);

    // Bản đồ lời khuyên
    const adviceMap = {
      anxiety: {
        title: "Lo âu",
        advice: {
          "Bình thường": "Bạn đang kiểm soát tốt lo âu. Duy trì thói quen tích cực.",
          "Nhẹ": "Hãy thử thiền, yoga hoặc đi bộ mỗi ngày để giảm lo âu.",
          "Vừa": "Bạn nên trao đổi với chuyên gia tâm lý để cải thiện.",
          "Nặng": "Lo âu đang ảnh hưởng rõ rệt đến cuộc sống. Hãy đặt lịch tư vấn ngay.",
          "Rất nặng": "Khẩn cấp! Bạn cần được hỗ trợ từ chuyên gia càng sớm càng tốt."
        }
      },
      depression: {
        title: "Trầm cảm",
        advice: {
          "Bình thường": "Bạn đang ở trạng thái cảm xúc ổn định.",
          "Nhẹ": "Hãy chia sẻ với người thân, ra ngoài nhiều hơn.",
          "Vừa": "Cân nhắc gặp chuyên gia để hiểu sâu về cảm xúc.",
          "Nặng": "Dấu hiệu trầm cảm rõ ràng. Hãy nhờ chuyên gia đồng hành.",
          "Rất nặng": "Cần hỗ trợ ngay. Đặt lịch tư vấn để được giúp đỡ kịp thời."
        }
      },
      stress: {
        title: "Căng thẳng",
        advice: {
          "Bình thường": "Bạn đang sống cân bằng. Hãy duy trì điều đó.",
          "Nhẹ": "Sắp xếp công việc và nghỉ ngơi hợp lý hơn.",
          "Vừa": "Nên học cách quản lý thời gian và giảm áp lực.",
          "Nặng": "Căng thẳng kéo dài có thể gây hại. Hãy trao đổi với chuyên gia.",
          "Rất nặng": "Tình trạng báo động. Cần nghỉ ngơi và tư vấn ngay lập tức."
        }
      }
    };

    // Hiển thị kết quả
    let html = `
      <h3>Kết quả đánh giá:</h3>
      <ul>
        <li><strong>Trầm cảm:</strong> ${score.depression} điểm – ${result.depression}</li>
        <li><strong>Lo âu:</strong> ${score.anxiety} điểm – ${result.anxiety}</li>
        <li><strong>Căng thẳng:</strong> ${score.stress} điểm – ${result.stress}</li>
      </ul>
      <h4>🔎 Nhận xét & Lời khuyên:</h4>
    `;

    highest.forEach((type) => {
      const level = result[type];
      html += `
        <p><strong>${adviceMap[type].title} (${level}):</strong><br>
        ${adviceMap[type].advice[level]}</p>
      `;
    });

    html += `
      <div class="cta">
        <p>💡 Bạn có thể <a href="../HTML/NguoiDung/search-expert.html" class="link">kết nối chuyên gia tư vấn</a> để được hỗ trợ sớm.</p>
      </div>
    `;

    document.getElementById("result").innerHTML = html;
  });
});

  // ✅ Hiển thị avatar và tên người dùng
  const user = JSON.parse(localStorage.getItem("user"));
  
  const loginLink = document.getElementById("loginLink");
  const userMenu = document.getElementById("userMenu");
  const usernameDisplay = document.getElementById("usernameDisplay");
  const avatarImg = document.querySelector(".user-button img");

  if (loginLink && userMenu && usernameDisplay) {
    loginLink.style.display = "none";
    userMenu.style.display = "inline-block";
    usernameDisplay.innerText = user.fullName || user.username;

    if (avatarImg) {
      avatarImg.src = user.avatarUrl
        ? `http://localhost:5221${user.avatarUrl}`
        : "../../img/default-avatar.png";
    }
  } else {
    if (userMenu) userMenu.style.display = "none";
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
  window.location.href = "./index.html";
}


let currentQuestionIndex = 0;

document.addEventListener("keydown", function (e) {
  if (e.key === "ArrowDown") {
    const questions = document.querySelectorAll(".question");

    if (currentQuestionIndex >= questions.length) return;

    const currentQuestion = questions[currentQuestionIndex];
    const qInputs = currentQuestion.querySelectorAll("input[type='radio']");

    // Chọn đáp án cuối cùng
    if (qInputs.length > 0) {
      qInputs[qInputs.length - 1].checked = true;
    }

    // Cuộn đến câu tiếp theo
    currentQuestionIndex++;
    if (currentQuestionIndex < questions.length) {
      questions[currentQuestionIndex].scrollIntoView({ behavior: "smooth", block: "center" });
    }
  }
});
