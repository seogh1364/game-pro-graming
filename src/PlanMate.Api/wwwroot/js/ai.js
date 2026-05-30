const aiChatPanel = document.getElementById("aiChatPanel");
const aiInput = document.getElementById("aiInput");
const aiSendBtn = document.getElementById("aiSendBtn");
const aiHint = document.getElementById("aiHint");
const studyTopicInput = document.getElementById("studyTopicInput");
const studyPlanBtn = document.getElementById("studyPlanBtn");
const studyPlanResult = document.getElementById("studyPlanResult");
const threadList = document.getElementById("threadList");
const newThreadBtn = document.getElementById("newThreadBtn");
const quickChips = document.getElementById("quickChips");

const STORAGE_KEY = "planmate-threads";
let threads = [];
let currentThreadId = null;

function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

function loadThreads() {
  try {
    threads = JSON.parse(localStorage.getItem(STORAGE_KEY) || "[]");
  } catch {
    threads = [];
  }
  if (!threads.length) {
    createThread("새 대화", true);
    renderThreadList();
    renderMessages();
  } else {
    currentThreadId = threads[0].id;
    renderThreadList();
    renderMessages();
  }
}

function saveThreads() {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(threads));
}

function getCurrentThread() {
  return threads.find((t) => t.id === currentThreadId);
}

function createThread(title, skipRender) {
  const thread = {
    id: crypto.randomUUID(),
    title: title || "새 대화",
    messages: [],
    updatedAt: Date.now(),
  };
  threads.unshift(thread);
  currentThreadId = thread.id;
  saveThreads();
  if (!skipRender) {
    renderThreadList();
    renderMessages();
  }
  return thread;
}

function renderThreadList() {
  if (!threadList) return;
  threadList.innerHTML = threads.map((t) => `
    <li class="thread-item${t.id === currentThreadId ? " active" : ""}" data-id="${t.id}">
      ${escapeHtml(t.title)}
    </li>
  `).join("");

  threadList.querySelectorAll(".thread-item").forEach((el) => {
    el.addEventListener("click", () => {
      currentThreadId = el.dataset.id;
      renderThreadList();
      renderMessages();
    });
  });
}

function renderMessages() {
  if (!aiChatPanel) return;
  aiChatPanel.innerHTML = "";
  const thread = getCurrentThread();
  if (!thread || !thread.messages.length) {
    addAiBubble(
      "안녕하세요! 저는 Plan mate AI예요.\n일정 밀도, 우선순위, 공부 계획 등 무엇이든 물어보세요.",
      "assistant",
      false
    );
    return;
  }
  thread.messages.forEach((m) => addAiBubble(m.content, m.role, false));
}

function addAiBubble(text, role, persist = true) {
  const row = document.createElement("div");
  row.className = `msg-row ${role}`;
  row.innerHTML = `
    <div class="msg-avatar">${role === "assistant" ? "🤖" : "👤"}</div>
    <div class="bubble ${role}">${escapeHtml(text)}</div>
  `;
  aiChatPanel.appendChild(row);
  aiChatPanel.scrollTop = aiChatPanel.scrollHeight;

  if (persist) {
    const thread = getCurrentThread();
    if (thread) {
      thread.messages.push({ role, content: text });
      if (role === "user" && thread.title === "새 대화") {
        thread.title = text.slice(0, 24) + (text.length > 24 ? "…" : "");
      }
      thread.updatedAt = Date.now();
      saveThreads();
      renderThreadList();
    }
  }
}

function showAiTyping() {
  const el = document.createElement("div");
  el.className = "ai-typing";
  el.id = "aiTyping";
  el.textContent = "Plan mate가 생각 중";
  aiChatPanel.appendChild(el);
  aiChatPanel.scrollTop = aiChatPanel.scrollHeight;
}

function hideAiTyping() {
  document.getElementById("aiTyping")?.remove();
}

async function sendAiMessage(messageOverride) {
  const message = (messageOverride || aiInput.value).trim();
  if (!message) {
    aiHint.textContent = "질문을 입력해 주세요.";
    return;
  }

  aiHint.textContent = "";
  aiInput.value = "";
  addAiBubble(message, "user");

  const thread = getCurrentThread();
  const history = thread ? thread.messages.slice(0, -1) : [];

  showAiTyping();
  aiSendBtn.disabled = true;

  try {
    const response = await fetch("/api/ai/chat", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ message, history }),
    });

    hideAiTyping();

    if (!response.ok) throw new Error("AI 응답을 받지 못했습니다.");

    const data = await response.json();
    const reply = data.reply || "답변을 생성하지 못했어요.";
    addAiBubble(reply, "assistant");

    if (data.usedGemini) {
      aiHint.textContent = "Google Gemini로 답변 중";
    }
  } catch (err) {
    hideAiTyping();
    addAiBubble("잠시 문제가 생겼어요. 서버를 재시작한 뒤 다시 시도해 주세요.", "assistant");
    aiHint.textContent = err.message;
  } finally {
    aiSendBtn.disabled = false;
    aiInput.focus();
  }
}

function renderStudyPlan(plan) {
  studyPlanResult.innerHTML = `
    <p class="study-summary">${escapeHtml(plan.summary)}${plan.usedGemini ? " (Gemini)" : ""}</p>
    <div class="study-days">
      ${plan.days.map((day) => `
        <article class="study-day-card">
          <span class="study-day-num">Day ${day.day}</span>
          <h4>${escapeHtml(day.title)}</h4>
          <p>${escapeHtml(day.focus)}</p>
          <span class="badge badge-cyan">${escapeHtml(day.duration)}</span>
        </article>
      `).join("")}
    </div>
  `;
}

async function createStudyPlan() {
  const topic = studyTopicInput.value.trim();
  if (!topic) {
    aiHint.textContent = "학습 주제를 입력해 주세요.";
    return;
  }

  studyPlanBtn.disabled = true;
  studyPlanBtn.textContent = "생성 중...";
  studyPlanResult.innerHTML = "<p class='hint'>AI가 학습 계획을 만들고 있어요...</p>";

  try {
    const response = await fetch("/api/ai/study-plan", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ topic, days: 5 }),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.message || "학습 계획 생성에 실패했습니다.");
    }

    const plan = await response.json();
    renderStudyPlan(plan);
    addAiBubble(`「${topic}」 ${plan.days.length}일 학습 계획을 만들었어요.`, "assistant");
  } catch (err) {
    studyPlanResult.innerHTML = `<p class="hint">${escapeHtml(err.message)}</p>`;
    aiHint.textContent = err.message;
  } finally {
    studyPlanBtn.disabled = false;
    studyPlanBtn.textContent = "생성";
  }
}

aiSendBtn.addEventListener("click", () => sendAiMessage());
aiInput.addEventListener("keydown", (event) => {
  if (event.key === "Enter") sendAiMessage();
});
studyPlanBtn.addEventListener("click", createStudyPlan);
newThreadBtn.addEventListener("click", () => createThread("새 대화"));

quickChips?.querySelectorAll(".chip").forEach((chip) => {
  chip.addEventListener("click", () => sendAiMessage(chip.dataset.msg));
});

loadThreads();
aiInput.focus();
