const API_URL = "http://localhost:5225";

const statusEl = document.getElementById("status");
const loginView = document.getElementById("login-view");
const tasksView = document.getElementById("tasks-view");
const loginForm = document.getElementById("login-form");
const loginError = document.getElementById("login-error");
const logoutBtn = document.getElementById("logout-btn");
const taskList = document.getElementById("task-list");
const tasksMessage = document.getElementById("tasks-message");
const taskForm = document.getElementById("task-form");
const taskError = document.getElementById("task-error");
function getToken() {
    return sessionStorage.getItem("token");
}

function showView() {
    const loggedIn = Boolean(getToken());
    loginView.hidden = loggedIn;
    tasksView.hidden = !loggedIn;
    if (loggedIn) loadTasks();
}

function logout() {
    sessionStorage.removeItem("token");
    showView();
}

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
    tasksMessage.textContent = "Loading...";
    taskList.replaceChildren();

    try {
        const response = await fetch(`${API_URL}/api/tasks`, {
            headers: { Authorization: `Bearer ${getToken()}` }
        });
        if (response.status === 401) {
            logout();
            return;
        }
        if (!response.ok) throw new Error(`HTTP ${response.status}`);

        const tasks = await response.json();
        renderTasks(tasks);
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

function renderTasks(tasks) {
    taskList.replaceChildren();
    tasksMessage.textContent = tasks.length === 0 ? "No tasks yet." : "";

    for (const task of tasks) {
        const li = document.createElement("li");
        li.className = task.isCompleted ? "task done" : "task";

        const info = document.createElement("div");
        info.className = "task-info";

        const title = document.createElement("span");
        title.className = "task-title";
        title.textContent = task.title;

        const due = document.createElement("span");
        due.className = "task-due";
        due.textContent = new Date(task.dueDate).toLocaleDateString("en-GB");

        info.append(title, due);

        const actions = document.createElement("div");
        actions.className = "task-actions";

        const toggleBtn = document.createElement("button");
        toggleBtn.type = "button";
        toggleBtn.className = "secondary";
        toggleBtn.textContent = task.isCompleted ? "Reopen" : "Done";
        toggleBtn.addEventListener("click", () => {
            const action = task.isCompleted ? "reopen" : "complete";
            callTaskApi(`${task.id}/${action}`, "POST");
        });

        const deleteBtn = document.createElement("button");
        deleteBtn.type = "button";
        deleteBtn.className = "danger";
        deleteBtn.textContent = "Delete";
        deleteBtn.addEventListener("click", () => {
            if (confirm("Delete this task?")) {
                callTaskApi(`${task.id}`, "DELETE");
            }
        });

        actions.append(toggleBtn, deleteBtn);
        li.append(info, actions);
        taskList.append(li);
    }
}

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
        showView();
    } catch (err) {
        loginError.textContent = "Cannot reach the server.";
    }
});
taskForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    taskError.textContent = "";

    const body = {
        title: taskForm.title.value.trim(),
        description: null,
        recurrenceType: 0,
        dueDate: new Date(taskForm.dueDate.value).toISOString()
    };

    if (!body.title) {
        taskError.textContent = "Title is required.";
        return;
    }

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
        loadTasks();
    } catch (err) {
        taskError.textContent = "Cannot reach the server.";
    }
});
logoutBtn.addEventListener("click", logout);

checkApi();
showView();