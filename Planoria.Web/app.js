const API_URL = "http://localhost:5225";

const $ = (id) => document.getElementById(id);
const statusEl = $("status");
const loginView = $("login-view");
const appView = $("app-view");
const loginForm = $("login-form");
const loginError = $("login-error");
const logoutBtn = $("logout-btn");
const newTaskBtn = $("new-task-btn");
const taskForm = $("task-form");
const taskTitleInput = $("task-title");
const taskDueInput = $("task-due");
const taskError = $("task-error");
const viewTitle = $("view-title");
const viewSubtitle = $("view-subtitle");
const tasksMessage = $("tasks-message");
const taskList = $("task-list");
const confirmDialog = $("confirm-dialog");
const confirmText = $("confirm-text");
const registerForm = $("register-form");
const registerError = $("register-error");
const switchBtn = $("switch-btn");
const switchText = $("switch-text");

let allTasks = [];

/* ---------- Date helpers ---------- */
function startOfDay(date) {
    const d = new Date(date);
    d.setHours(0, 0, 0, 0);
    return d;
}

function addDays(date, days) {
    const d = new Date(date);
    d.setDate(d.getDate() + days);
    return d;
}

function startOfWeek(date) {
    const d = startOfDay(date);
    const daysSinceMonday = (d.getDay() + 6) % 7;
    return addDays(d, -daysSinceMonday);
}

function toInputDate(date) {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, "0");
    const d = String(date.getDate()).padStart(2, "0");
    return `${y}-${m}-${d}`;
}

function formatDate(date) {
    return date.toLocaleDateString("en-GB", { weekday: "short", day: "numeric", month: "short" });
}

/* ---------- Views (Today / Week / Month / All) ---------- */
const VIEWS = {
    today: {
        title: "Today",
        empty: "Nothing due today.",
        range: () => {
            const start = startOfDay(new Date());
            return [start, addDays(start, 1)];
        }
    },
    week: {
        title: "This week",
        empty: "Nothing due this week.",
        range: () => {
            const start = startOfWeek(new Date());
            return [start, addDays(start, 7)];
        }
    },
    month: {
        title: "This month",
        empty: "Nothing due this month.",
        range: () => {
            const now = new Date();
            return [new Date(now.getFullYear(), now.getMonth(), 1), new Date(now.getFullYear(), now.getMonth() + 1, 1)];
        }
    },
    all: {
        title: "All tasks",
        empty: "No tasks yet. Add your first one!",
        range: null
    }
};

function currentView() {
    const name = location.hash.replace("#", "");
    return VIEWS[name] ? name : "today";
}

function tasksForView(name) {
    const config = VIEWS[name];
    let tasks = allTasks;

    if (config.range) {
        const [start, end] = config.range();
        const todayStart = startOfDay(new Date());
        tasks = allTasks.filter((t) => {
            const due = new Date(t.dueDate);
            const inRange = due >= start && due < end;
            const overdue = !t.isCompleted && due < todayStart;
            return inRange || overdue;
        });
    }

    return [...tasks].sort((a, b) => new Date(a.dueDate) - new Date(b.dueDate));
}

/* ---------- Auth state ---------- */
function getToken() {
    return sessionStorage.getItem("token");
}

function showScreen() {
    const loggedIn = Boolean(getToken());
    loginView.hidden = loggedIn;
    appView.hidden = !loggedIn;
    if (loggedIn) loadTasks();
}

function logout() {
    sessionStorage.removeItem("token");
    allTasks = [];
    showScreen();
}

/* ---------- API calls ---------- */
async function checkApi() {
    try {
        const response = await fetch(`${API_URL}/health`);
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        const data = await response.json();
        statusEl.textContent = `API status: ${data.status}`;
    } catch (err) {
        statusEl.textContent = "API is not reachable.";
    }
}

async function loadTasks() {
    if (allTasks.length === 0) tasksMessage.textContent = "Loading...";

    try {
        const response = await fetch(`${API_URL}/api/tasks`, {
            headers: { Authorization: `Bearer ${getToken()}` }
        });
        if (response.status === 401) {
            logout();
            return;
        }
        if (!response.ok) throw new Error(`HTTP ${response.status}`);

        allTasks = await response.json();
        render();
    } catch (err) {
        tasksMessage.textContent = "Could not load tasks.";
    }
}

async function callTaskApi(path, method) {
    try {
        const response = await fetch(`${API_URL}/api/tasks/${path}`, {
            method,
            headers: { Authorization: `Bearer ${getToken()}` }
        });

        if (response.status === 401) {
            logout();
            return;
        }
        if (!response.ok) {
            tasksMessage.textContent = "Action failed.";
            return;
        }

        loadTasks();
    } catch (err) {
        tasksMessage.textContent = "Cannot reach the server.";
    }
}

/* ---------- Delete confirmation dialog ---------- */
function confirmDelete(taskTitle) {
    confirmText.textContent = `"${taskTitle}" will be permanently deleted.`;
    confirmDialog.returnValue = "";

    return new Promise((resolve) => {
        confirmDialog.addEventListener(
            "close",
            () => resolve(confirmDialog.returnValue === "confirm"),
            { once: true }
        );
        confirmDialog.showModal();
    });
}

/* ---------- Rendering ---------- */
function createTaskCard(task) {
    const due = new Date(task.dueDate);
    const overdue = !task.isCompleted && due < startOfDay(new Date());

    const li = document.createElement("li");
    li.className = "task" + (task.isCompleted ? " done" : "") + (overdue ? " overdue" : "");

    const check = document.createElement("button");
    check.type = "button";
    check.className = "check";
    check.textContent = task.isCompleted ? "\u2713" : "";
    check.setAttribute("aria-label", task.isCompleted ? "Reopen task" : "Mark task as done");
    check.addEventListener("click", () => {
        callTaskApi(`${task.id}/${task.isCompleted ? "reopen" : "complete"}`, "POST");
    });

    const info = document.createElement("div");
    info.className = "task-info";

    const title = document.createElement("span");
    title.className = "task-title";
    title.textContent = task.title;

    const dueText = document.createElement("span");
    dueText.className = "task-due";
    dueText.textContent = overdue ? `${formatDate(due)} \u00b7 Overdue` : formatDate(due);

    info.append(title, dueText);

    const deleteBtn = document.createElement("button");
    deleteBtn.type = "button";
    deleteBtn.className = "text-danger";
    deleteBtn.textContent = "Delete";
    deleteBtn.addEventListener("click", async () => {
        if (await confirmDelete(task.title)) {
            callTaskApi(`${task.id}`, "DELETE");
        }
    });

    li.append(check, info, deleteBtn);
    return li;
}

function render() {
    const name = currentView();
    const config = VIEWS[name];
    const tasks = tasksForView(name);

    for (const link of document.querySelectorAll(".tabs a")) {
        if (link.dataset.view === name) link.setAttribute("aria-current", "page");
        else link.removeAttribute("aria-current");
    }

    const done = tasks.filter((t) => t.isCompleted).length;
    viewTitle.textContent = config.title;
    viewSubtitle.textContent = `${tasks.length - done} open \u00b7 ${done} done`;
    tasksMessage.textContent = tasks.length === 0 ? config.empty : "";

    taskList.replaceChildren(...tasks.map(createTaskCard));
}

/* ---------- Events ---------- */
function setAuthMode(mode) {
    const isRegister = mode === "register";
    loginForm.hidden = isRegister;
    registerForm.hidden = !isRegister;
    loginError.textContent = "";
    registerError.textContent = "";
    switchText.textContent = isRegister ? "Already have an account?" : "New here?";
    switchBtn.textContent = isRegister ? "Sign in" : "Create an account";
}

switchBtn.addEventListener("click", () => {
    setAuthMode(registerForm.hidden ? "register" : "login");
});

registerForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    registerError.textContent = "";

    const body = {
        username: $("reg-username").value.trim(),
        email: $("reg-email").value.trim(),
        password: $("reg-password").value
    };

    try {
        const response = await fetch(`${API_URL}/api/auth/register`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body)
        });

        if (response.status === 429) {
            registerError.textContent = "Too many attempts. Please wait a minute.";
            return;
        }
        if (!response.ok) {
            registerError.textContent = "Could not create the account. Check your details; the username or email may already be taken.";
            return;
        }

        const data = await response.json();
        sessionStorage.setItem("token", data.token);
        registerForm.reset();
        setAuthMode("login");
        showScreen();
    } catch (err) {
        registerError.textContent = "Cannot reach the server.";
    }
});
loginForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    loginError.textContent = "";

    const body = {
        usernameOrEmail: loginForm.usernameOrEmail.value.trim(),
        password: loginForm.password.value
    };

    try {
        const response = await fetch(`${API_URL}/api/auth/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body)
        });

        if (response.status === 429) {
            loginError.textContent = "Too many attempts. Please wait a minute.";
            return;
        }
        if (!response.ok) {
            loginError.textContent = "Invalid username or password.";
            return;
        }

        const data = await response.json();
        sessionStorage.setItem("token", data.token);
        loginForm.reset();
        showScreen();
    } catch (err) {
        loginError.textContent = "Cannot reach the server.";
    }
});

newTaskBtn.addEventListener("click", () => {
    taskForm.hidden = !taskForm.hidden;
    if (!taskForm.hidden) {
        const today = toInputDate(new Date());
        taskDueInput.min = today;
        taskDueInput.value = today;
        taskTitleInput.focus();
    }
});

taskForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    taskError.textContent = "";

    const title = taskTitleInput.value.trim();
    if (!title) {
        taskError.textContent = "Title is required.";
        return;
    }

    if (taskDueInput.value < toInputDate(new Date())) {
        taskError.textContent = "Due date cannot be in the past.";
        return;
    }

    const body = {
        title,
        description: null,
        recurrenceType: 0,
        dueDate: new Date(taskDueInput.value).toISOString()
    };

    try {
        const response = await fetch(`${API_URL}/api/tasks`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                Authorization: `Bearer ${getToken()}`
            },
            body: JSON.stringify(body)
        });

        if (response.status === 401) {
            logout();
            return;
        }
        if (!response.ok) {
            taskError.textContent = "Could not add the task.";
            return;
        }

        taskForm.reset();
        taskForm.hidden = true;
        loadTasks();
    } catch (err) {
        taskError.textContent = "Cannot reach the server.";
    }
});

logoutBtn.addEventListener("click", logout);

window.addEventListener("hashchange", () => {
    if (getToken()) render();
});

checkApi();
showScreen();