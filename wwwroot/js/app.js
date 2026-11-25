
const views = ["login", "signup", "forgot", "collections"];

const showView = name => {
  views.forEach(v =>
    document.getElementById(`${v}View`).classList.toggle("active", v === name)
  );

  document.querySelectorAll(".notice").forEach(n => n.textContent = "");

  if (name !== "forgot") {
    document.getElementById("resetBox").hidden = true;
  }
};

document.querySelectorAll("[data-view]").forEach(button =>
  button.addEventListener("click", () => showView(button.dataset.view))
);

const api = async (path, data, token) => {
  const headers = { "Content-Type": "application/json" };

  if (token) headers.Authorization = `Bearer ${token}`;

  const response = await fetch(path, {
    method: "POST",
    headers,
    body: JSON.stringify(data)
  });

  let result = {};

  try {
    result = await response.json();
  } catch {}

  if (!response.ok) {
    throw new Error(
      result.message ||
      (response.status === 401
        ? "Invalid username or password."
        : "Something went wrong.")
    );
  }

  return result;
};

const setNotice = (id, message, success = false) => {
  const el = document.getElementById(id);
  el.textContent = message;
  el.style.color = success ? "#38734a" : "#8a2434";
};

// SIGNUP
document.getElementById("signupView").addEventListener("submit", async e => {
  e.preventDefault();

  const username = document.getElementById("signupUsername").value;
  const email = document.getElementById("signupEmail").value;
  const password = document.getElementById("signupPassword").value;
  const confirmPassword = document.getElementById("confirmPassword").value;

  try {
    const result = await api("/api/auth/signup", {
      username, email, password, confirmPassword
    });

    setNotice("signupNotice", result.message, true);
    e.target.reset();

    setTimeout(() => showView("login"), 1000);
  } catch (err) {
    setNotice("signupNotice", err.message);
  }
});

// LOGIN
document.getElementById("loginView").addEventListener("submit", async e => {
  e.preventDefault();

  try {
    const result = await api("/api/auth/login", {
      username: document.getElementById("loginUsername").value,
      password: document.getElementById("loginPassword").value
    });

    sessionStorage.setItem("aquillaToken", result.token);
    sessionStorage.setItem("aquillaUsername", result.username);

    showView("collections");
  } catch (err) {
    setNotice("loginNotice", err.message);
  }
});

// FORGOT PASSWORD
document.getElementById("forgotView").addEventListener("submit", async e => {
  e.preventDefault();

  try {
    const result = await api("/api/auth/forgot-password", {
      email: document.getElementById("forgotEmail").value
    });

    setNotice("forgotNotice", result.message, true);
    document.getElementById("resetBox").hidden = false;
  } catch (err) {
    setNotice("forgotNotice", err.message);
  }
});

// RESET PASSWORD
document.getElementById("resetButton").addEventListener("click", async () => {
  try {
    const result = await api("/api/auth/reset-password", {
      email: document.getElementById("forgotEmail").value,
      code: document.getElementById("resetCode").value,
      newPassword: document.getElementById("newPassword").value
    });

    setNotice("forgotNotice", result.message, true);
    document.getElementById("forgotView").reset();

    setTimeout(() => showView("login"), 1400);
  } catch (err) {
    setNotice("forgotNotice", err.message);
  }
});

// COLLECTION NAVIGATION
const collectionGrid = document.querySelector(
  "#collectionsView > .collection-grid"
);

const categoryPage = document.getElementById("categoryPage");
const categoryGrid = document.getElementById("categoryGrid");
const categoryTitle = document.getElementById("categoryTitle");
const categorySubtitle = document.getElementById("categorySubtitle");
const backButton = document.getElementById("backToCollections");
const collectionStatus = document.getElementById("collectionStatus");

const collectionHeading = document.querySelector("#collectionsView h1");
const collectionSubheading = document.querySelector(
  "#collectionsView .subheading"
);
const collectionOrnament = document.querySelector(
  "#collectionsView .ornament"
);

const categories = {
  men: [
    "Shirts",
    "T-Shirts",
    "Formal Wear",
    "Casual Wear",
    "Kurtas",
    "Veshti / Dhoti",
    "Wedding Wear"
  ],

  women: [
    "Silk Sarees",
    "Cotton Sarees",
    "Designer Sarees",
    "Salwar Suits",
    "Kurtis",
    "Lehengas",
    "Wedding Wear"
  ],

  kids: [
    "Boys' Wear",
    "Girls' Wear",
    "Traditional Wear",
    "Party Wear",
    "Casual Wear",
    "Festive Wear"
  ]
};

const collectionNames = {
  men: "Men's Collection",
  women: "Women's Collection",
  kids: "Kids' Collection"
};

let currentCategories = [];

function showCategories(type) {
  if (!categories[type]) return;

  currentCategories = categories[type];

  categoryPage.dataset.collectionName = collectionNames[type];

  categoryTitle.textContent = collectionNames[type];
  categorySubtitle.textContent = "Choose a category to explore";

  categoryGrid.innerHTML = "";

  currentCategories.forEach(category => {
    const button = document.createElement("button");

    button.type = "button";
    button.className = "collection-card";
    button.textContent = category;

    button.addEventListener("click", () => {
      showCategoryDetail(category);
    });

    categoryGrid.appendChild(button);
  });

  collectionGrid.hidden = true;
  collectionStatus.hidden = true;
  collectionHeading.hidden = true;
  collectionSubheading.hidden = true;
  collectionOrnament.hidden = true;

  categoryPage.hidden = false;

  backButton.textContent = "← BACK TO COLLECTIONS";
}

function showCategoryDetail(category) {
  categoryTitle.textContent = category;
  categorySubtitle.textContent = "AQUILLA TEXTILES";

  categoryGrid.innerHTML = "";

  const message = document.createElement("p");

  message.textContent =
    "Explore our " + category +
    " collection. New styles and designs coming soon.";

  categoryGrid.appendChild(message);

  backButton.textContent = "← BACK TO CATEGORIES";
}

// MAIN COLLECTION BUTTONS
document.querySelectorAll(
  "#collectionsView .collection-card[data-category]"
).forEach(card => {
  card.addEventListener("click", () => {
    showCategories(card.dataset.category);
  });
});

// BACK BUTTON
backButton.addEventListener("click", () => {

  // Return from category detail to category list
  if (backButton.textContent.includes("BACK TO CATEGORIES")) {

    categoryTitle.textContent =
      categoryPage.dataset.collectionName || "Collection";

    categorySubtitle.textContent = "Choose a category to explore";

    categoryGrid.innerHTML = "";

    currentCategories.forEach(category => {
      const button = document.createElement("button");

      button.type = "button";
      button.className = "collection-card";
      button.textContent = category;

      button.addEventListener("click", () => {
        showCategoryDetail(category);
      });

      categoryGrid.appendChild(button);
    });

    backButton.textContent = "← BACK TO COLLECTIONS";

  } else {

    // Return to main collections
    categoryPage.hidden = true;

    collectionGrid.hidden = false;
    collectionStatus.hidden = false;
    collectionHeading.hidden = false;
    collectionSubheading.hidden = false;
    collectionOrnament.hidden = false;
  }
});

// LOGOUT
document.getElementById("logoutButton").addEventListener("click", () => {
  sessionStorage.removeItem("aquillaToken");
  sessionStorage.removeItem("aquillaUsername");

  categoryPage.hidden = true;
  collectionGrid.hidden = false;
  collectionStatus.hidden = false;
  collectionHeading.hidden = false;
  collectionSubheading.hidden = false;
  collectionOrnament.hidden = false;

  showView("login");
});

// SHOW / HIDE PASSWORD
document.querySelectorAll("[data-toggle]").forEach(button =>
  button.addEventListener("click", () => {
    const field = document.getElementById(button.dataset.toggle);

    const reveal = field.type === "password";

    field.type = reveal ? "text" : "password";

    button.setAttribute(
      "aria-label",
      reveal ? "Hide password" : "Show password"
    );
  })
);

