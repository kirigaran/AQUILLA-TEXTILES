const views = ["login", "signup", "forgot", "collections"];
const showView = name => {
  views.forEach(v => document.getElementById(`${v}View`).classList.toggle("active", v === name));
  document.querySelectorAll(".notice").forEach(n => n.textContent = "");
  if (name !== "forgot") document.getElementById("resetBox").hidden = true;
};
document.querySelectorAll("[data-view]").forEach(button =>
  button.addEventListener("click", () => showView(button.dataset.view)));

const api = async (path, data, token) => {
  const headers = {"Content-Type":"application/json"};
  if (token) headers.Authorization = `Bearer ${token}`;
  const response = await fetch(path, {method:"POST", headers, body:JSON.stringify(data)});
  let result = {};
  try { result = await response.json(); } catch {}
  if (!response.ok) throw new Error(result.message || (response.status === 401 ? "Invalid username or password." : "Something went wrong."));
  return result;
};
const setNotice = (id, message, success=false) => {
  const el = document.getElementById(id); el.textContent = message;
  el.style.color = success ? "#38734a" : "#8a2434";
};

document.getElementById("signupView").addEventListener("submit", async e => {
  e.preventDefault();
  const username = document.getElementById("signupUsername").value;
  const email = document.getElementById("signupEmail").value;
  const password = document.getElementById("signupPassword").value;
  const confirmPassword = document.getElementById("confirmPassword").value;
  try {
    const result = await api("/api/auth/signup", {username,email,password,confirmPassword});
    setNotice("signupNotice", result.message, true);
    e.target.reset();
    setTimeout(() => showView("login"), 1000);
  } catch(err) { setNotice("signupNotice", err.message); }
});

document.getElementById("loginView").addEventListener("submit", async e => {
  e.preventDefault();
  try {
    const result = await api("/api/auth/login", {
      username:document.getElementById("loginUsername").value,
      password:document.getElementById("loginPassword").value
    });
    sessionStorage.setItem("aquillaToken", result.token);
    sessionStorage.setItem("aquillaUsername", result.username);
    showView("collections");
  } catch(err) { setNotice("loginNotice", err.message); }
});

document.getElementById("forgotView").addEventListener("submit", async e => {
  e.preventDefault();
  try {
    const result = await api("/api/auth/forgot-password", {
      email:document.getElementById("forgotEmail").value
    });
    setNotice("forgotNotice", result.message, true);
    document.getElementById("resetBox").hidden = false;
  } catch(err) { setNotice("forgotNotice", err.message); }
});
document.getElementById("resetButton").addEventListener("click", async () => {
  try {
    const result = await api("/api/auth/reset-password", {
      email:document.getElementById("forgotEmail").value,
      code:document.getElementById("resetCode").value,
      newPassword:document.getElementById("newPassword").value
    });
    setNotice("forgotNotice", result.message, true);
    document.getElementById("forgotView").reset();
    setTimeout(() => showView("login"), 1400);
  } catch(err) { setNotice("forgotNotice", err.message); }
});

document.querySelectorAll(".collection-card").forEach(card => card.addEventListener("click", () => {
  document.querySelectorAll(".collection-card").forEach(c => c.classList.remove("selected"));
  card.classList.add("selected");
  document.getElementById("collectionStatus").textContent =
    `${card.dataset.category[0].toUpperCase()+card.dataset.category.slice(1)} collection selected.`;
}));
document.getElementById("logoutButton").addEventListener("click", () => {
  sessionStorage.removeItem("aquillaToken");
  sessionStorage.removeItem("aquillaUsername");
  showView("login");
});

document.querySelectorAll("[data-toggle]").forEach(button => button.addEventListener("click", () => {
  const field = document.getElementById(button.dataset.toggle);
  const reveal = field.type === "password";
  field.type = reveal ? "text" : "password";
  button.setAttribute("aria-label", reveal ? "Hide password" : "Show password");
}));
