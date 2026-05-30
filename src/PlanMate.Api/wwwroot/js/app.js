const chatEl = document.getElementById("chat");
const taskForm = document.getElementById("taskForm");
const taskTitleInput = document.getElementById("taskTitleInput");
const submitBtn = document.getElementById("submitBtn");
const hintEl = document.getElementById("hint");
const isImportantEl = document.getElementById("isImportant");
const isUrgentEl = document.getElementById("isUrgent");
const durationHint = document.getElementById("durationHint");
const durationChips = document.getElementById("durationChips");
const scheduleDateBtn = document.getElementById("scheduleDateBtn");
const deadlineDateBtn = document.getElementById("deadlineDateBtn");
const timePickerRoot = document.getElementById("timePickerRoot");
const taskListEl = document.getElementById("taskList");
const emptyStateEl = document.getElementById("emptyState");
const taskCountEl = document.getElementById("taskCount");
const todayDateEl = document.getElementById("todayDate");
const heroLine1 = document.getElementById("heroLine1");
const heroLine2 = document.getElementById("heroLine2");
const progressLabel = document.getElementById("progressLabel");
const progressFill = document.getElementById("progressFill");
const progressSub = document.getElementById("progressSub");
const focusAvg = document.getElementById("focusAvg");
const completedCount = document.getElementById("completedCount");
const deadlineAlertsEl = document.getElementById("deadlineAlerts");
const optimizeBtn = document.getElementById("optimizeBtn");
const pomodoroTime = document.getElementById("pomodoroTime");
const pomodoroMode = document.getElementById("pomodoroMode");
const pomodoroStartBtn = document.getElementById("pomodoroStartBtn");
const pomodoroResetBtn = document.getElementById("pomodoroResetBtn");
const agendaListEl = document.getElementById("agendaList");
const focusTaskEl = document.getElementById("focusTask");
const intelDescEl = document.getElementById("intelDesc");
const startFocusBtn = document.getElementById("startFocusBtn");
const conflictPanel = document.getElementById("conflictPanel");
const conflictText = document.getElementById("conflictText");
const conflictOptimizeBtn = document.getElementById("conflictOptimizeBtn");
const conflictIgnoreBtn = document.getElementById("conflictIgnoreBtn");
const pomodoroPanel = document.getElementById("pomodoroPanel");
const pomodoroSettings = document.getElementById("pomodoroSettings");
const focusMinutesInput = document.getElementById("focusMinutesInput");
const breakMinutesInput = document.getElementById("breakMinutesInput");
const focusDurationChips = document.getElementById("focusDurationChips");
const breakDurationChips = document.getElementById("breakDurationChips");
const addTaskDrawer = document.getElementById("addTaskDrawer");

const categoryLabels = { study: "공부", exercise: "운동", rest: "휴식", general: "일반" };

let pendingOptions = { isImportant: false, isUrgent: false, deadline: null, durationMinutes: 60 };
let latestTasks = [];
let scheduleDatePicker = null;
let deadlineDatePicker = null;
let timePicker = null;

let pomodoroSeconds = 25 * 60;
let pomodoroRunning = false;
let pomodoroInterval = null;
let pomodoroPhase = "focus";
let focusDurationMinutes = 25;
let breakDurationMinutes = 5;

function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

function formatTodayDateTime() {
  const now = new Date();
  const datePart = new Intl.DateTimeFormat("ko-KR", {
    year: "numeric", month: "long", day: "numeric", weekday: "long",
  }).format(now);
  const timePart = new Intl.DateTimeFormat("ko-KR", {
    hour: "numeric", minute: "2-digit", hour12: true,
  }).format(now);
  return { datePart, timePart };
}

function updateTodayDate() {
  if (!todayDateEl) return;
  const { datePart, timePart } = formatTodayDateTime();
  todayDateEl.textContent = `${datePart} · ${timePart}`;
}

function startTodayDateClock() {
  updateTodayDate();
  setInterval(updateTodayDate, 1000);
}

function addBubble(text, role) {
  if (!chatEl) return;
  const row = document.createElement("div");
  row.className = `msg-row ${role}`;
  row.innerHTML = `
    <div class="msg-avatar">${role === "assistant" ? "🤖" : "👤"}</div>
    <div class="bubble ${role}">${escapeHtml(text)}</div>
  `;
  chatEl.appendChild(row);
  chatEl.scrollTop = chatEl.scrollHeight;
}

function formatTimeDisplay(time) {
  const [hour, minute] = time.split(":").map(Number);
  return `${String(hour).padStart(2, "0")}:${String(minute).padStart(2, "0")}`;
}

function formatDuration(minutes) {
  if (minutes < 60) return `${minutes}분`;
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return m === 0 ? `${h}시간` : `${h}시간 ${m}분`;
}

function getNowMinutes() {
  const now = new Date();
  return now.getHours() * 60 + now.getMinutes();
}

function isCurrentTask(task) {
  if (task.isCompleted) return false;
  const now = getNowMinutes();
  return now >= task.sortMinutes && now < task.endSortMinutes;
}

function resetForm() {
  pendingOptions = { isImportant: false, isUrgent: false, deadline: null, durationMinutes: 60 };
  if (taskTitleInput) taskTitleInput.value = "";
  if (isImportantEl) isImportantEl.checked = false;
  if (isUrgentEl) isUrgentEl.checked = false;
  if (durationHint) durationHint.textContent = "AI 예상: -";
  if (hintEl) hintEl.textContent = "";
  scheduleDatePicker?.setValue(PlanMatePickers.toDateKey(new Date()));
  deadlineDatePicker?.setValue(null);
  timePicker?.setValue(getDefaultTime());
  durationChips?.querySelectorAll(".duration-chip").forEach((chip) => {
    chip.classList.toggle("active", chip.dataset.min === "60");
  });
}

function getDefaultTime() {
  const now = new Date();
  now.setMinutes(now.getMinutes() + 30);
  return `${String(now.getHours()).padStart(2, "0")}:${String(now.getMinutes()).padStart(2, "0")}`;
}

function initPickers() {
  if (scheduleDateBtn) {
    scheduleDatePicker = PlanMatePickers.attachDatePicker({
      trigger: scheduleDateBtn,
      value: PlanMatePickers.toDateKey(new Date()),
      placeholder: "일정 날짜 선택",
      onChange: () => {},
    });
  }

  if (deadlineDateBtn) {
    deadlineDatePicker = PlanMatePickers.attachDatePicker({
      trigger: deadlineDateBtn,
      value: null,
      placeholder: "선택 안 함",
      allowClear: true,
      onChange: (key) => {
        pendingOptions.deadline = key;
      },
    });
  }

  if (timePickerRoot) {
    timePicker = PlanMatePickers.attachTimePicker(timePickerRoot, {
      value: getDefaultTime(),
    });
  }

  durationChips?.querySelectorAll(".duration-chip").forEach((chip) => {
    chip.addEventListener("click", () => {
      durationChips.querySelectorAll(".duration-chip").forEach((c) => c.classList.remove("active"));
      chip.classList.add("active");
      pendingOptions.durationMinutes = Number(chip.dataset.min);
    });
  });
}

function resolveDeadline() {
  const scheduleKey = scheduleDatePicker?.getValue();
  const deadlineKey = deadlineDatePicker?.getValue();
  const todayKey = PlanMatePickers.toDateKey(new Date());

  if (deadlineKey) return deadlineKey;
  if (scheduleKey && scheduleKey !== todayKey) return scheduleKey;
  return null;
}

async function predictDuration(title) {
  try {
    const response = await fetch("/api/tasks/predict-duration", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ title }),
    });
    if (!response.ok) return;
    const data = await response.json();
    pendingOptions.durationMinutes = data.durationMinutes;
    if (durationHint) {
      durationHint.textContent = `AI 예상: ${data.label} (${categoryLabels[data.category] || data.category})`;
    }
    durationChips?.querySelectorAll(".duration-chip").forEach((chip) => {
      chip.classList.toggle("active", Number(chip.dataset.min) === data.durationMinutes);
    });
  } catch {
    if (durationHint) durationHint.textContent = "AI 예상: 약 1시간";
  }
}

function renderProgress(progress) {
  if (progressLabel) progressLabel.textContent = `${progress.percent}%`;
  if (progressFill) progressFill.style.width = `${progress.percent}%`;
  if (progressSub) progressSub.textContent = `${progress.total}개 중 ${progress.completed}개 완료`;
  if (focusAvg) focusAvg.textContent = `${progress.percent}%`;
  if (completedCount) completedCount.textContent = `${progress.completed}개`;
}

function renderDeadlineAlerts(alerts) {
  if (!deadlineAlertsEl) return;
  deadlineAlertsEl.innerHTML = "";
  alerts.forEach((alert) => {
    const el = document.createElement("div");
    el.className = "alert-banner";
    el.textContent = `🔔 ${alert.title}: ${alert.message} (마감 ${alert.deadlineLabel})`;
    deadlineAlertsEl.appendChild(el);
    if (alert.daysLeft <= 1 && Notification.permission === "granted") {
      new Notification("Plan mate 마감 알림", { body: `${alert.title} — ${alert.message}` });
    }
  });
}

function priorityBadge(task) {
  if (task.isImportant && task.isUrgent) return { text: "긴급·중요", cls: "badge-coral" };
  if (task.isImportant) return { text: "중요", cls: "badge-purple" };
  if (task.isUrgent) return { text: "긴급", cls: "badge-coral" };
  return { text: categoryLabels[task.category] || "일반", cls: "badge-cyan" };
}

function findConflicts(tasks) {
  const pairs = [];
  const active = tasks.filter((t) => !t.isCompleted);
  for (let i = 0; i < active.length; i++) {
    for (let j = i + 1; j < active.length; j++) {
      const a = active[i];
      const b = active[j];
      if (a.sortMinutes < b.endSortMinutes && b.sortMinutes < a.endSortMinutes) {
        pairs.push([a, b]);
      }
    }
  }
  return pairs;
}

function renderConflicts(tasks) {
  if (!conflictPanel) return;
  const pairs = findConflicts(tasks);
  if (!pairs.length) {
    conflictPanel.classList.add("hidden");
    return;
  }
  const [a, b] = pairs[0];
  conflictPanel.classList.remove("hidden");
  if (conflictText) {
    conflictText.textContent = `「${a.title}」(${formatTimeDisplay(a.time)})와 「${b.title}」(${formatTimeDisplay(b.time)})가 겹칩니다. AI 재배치를 추천합니다.`;
  }
}

function renderFocusHero(tasks) {
  if (!focusTaskEl) return;
  const incomplete = tasks.filter((t) => !t.isCompleted);
  if (!incomplete.length) {
    focusTaskEl.textContent = "모든 일정 완료! 🎉";
    if (intelDescEl) intelDescEl.textContent = "오늘 할 일을 모두 마쳤어요. 휴식하거나 내일을 준비해 보세요.";
    if (startFocusBtn) startFocusBtn.disabled = true;
    return;
  }

  if (startFocusBtn) startFocusBtn.disabled = false;
  const current = incomplete.find(isCurrentTask);
  const top = current || incomplete[0];
  focusTaskEl.textContent = top.title;

  const meetingCount = incomplete.length;

  if (intelDescEl) {
    if (current) {
      intelDescEl.textContent = `지금 진행 중인 일정입니다. ${formatDuration(current.durationMinutes)} 집중 블록을 추천해요.`;
    } else {
      intelDescEl.textContent = `오늘 ${meetingCount}개 일정이 있어요. ${formatTimeDisplay(top.time)}부터 집중하면 좋아요.`;
    }
  }
}

function renderAgenda(tasks) {
  if (!agendaListEl) return;
  agendaListEl.innerHTML = "";

  if (!tasks.length) {
    if (emptyStateEl) emptyStateEl.classList.remove("hidden");
    return;
  }
  if (emptyStateEl) emptyStateEl.classList.add("hidden");

  tasks.forEach((task) => {
    const current = isCurrentTask(task);
    const badge = priorityBadge(task);
    const endTime = formatTimeDisplay(
      `${String(Math.floor(task.endSortMinutes / 60)).padStart(2, "0")}:${String(task.endSortMinutes % 60).padStart(2, "0")}`
    );
    const adviceFull = (task.advice && task.advice.trim()) || "차근차근 진행해 보세요.";
    const summary = AdviceUI.summarizeAdvice(adviceFull);
    const showMoreHint = summary.truncated || adviceFull.length > 70;

    const item = document.createElement("article");
    item.className = `agenda-item has-advice${current ? " current" : ""}${task.isCompleted ? " completed" : ""}`;
    item.setAttribute("tabindex", "0");
    item.setAttribute("role", "button");
    item.setAttribute("aria-label", `${task.title} AI 조언 보기`);
    item.innerHTML = `
      <div class="agenda-time">${formatTimeDisplay(task.time)}</div>
      <div class="agenda-body">
        <h4>${escapeHtml(task.title)}</h4>
        <p class="agenda-advice-label">💡 AI 조언</p>
        <p class="agenda-advice-preview">${escapeHtml(summary.short)}</p>
        ${showMoreHint ? '<span class="advice-more-hint">탭하여 전체 보기 →</span>' : ""}
        <div class="agenda-badges">
          <span class="badge ${badge.cls}">${badge.text}</span>
          <span class="badge badge-cyan">${formatDuration(task.durationMinutes)} · ~${endTime}</span>
          ${current ? '<span class="badge badge-current">진행 중</span>' : ""}
        </div>
      </div>
      <div class="agenda-actions">
        <button type="button" title="완료" data-complete="${task.id}">${task.isCompleted ? "✓" : "○"}</button>
        <button type="button" class="delete" title="삭제" data-delete="${task.id}">×</button>
      </div>
    `;

    AdviceUI.bindAgendaAdviceClick(item, {
      title: task.title,
      time: formatTimeDisplay(task.time),
      durationLabel: `${formatDuration(task.durationMinutes)} · ~${endTime}`,
      advice: adviceFull,
    });

    item.querySelector("[data-delete]").addEventListener("click", async (e) => {
      e.stopPropagation();
      try { await deleteTask(task.id); } catch (err) { if (hintEl) hintEl.textContent = err.message; }
    });
    item.querySelector("[data-complete]").addEventListener("click", async (e) => {
      e.stopPropagation();
      try { await toggleComplete(task.id); } catch (err) { if (hintEl) hintEl.textContent = err.message; }
    });
    agendaListEl.appendChild(item);
  });
}

function renderTasks(tasks) {
  latestTasks = tasks;
  if (taskCountEl) taskCountEl.textContent = `${tasks.length}개`;
  renderAgenda(tasks);
  renderFocusHero(tasks);
  renderConflicts(tasks);
}

async function loadTasks() {
  const [tasksRes, progressRes, alertsRes] = await Promise.all([
    fetch("/api/tasks"),
    fetch("/api/tasks/progress"),
    fetch("/api/tasks/deadline-alerts"),
  ]);
  if (tasksRes.ok) renderTasks(await tasksRes.json());
  if (progressRes.ok) renderProgress(await progressRes.json());
  if (alertsRes.ok) renderDeadlineAlerts(await alertsRes.json());
}

async function deleteTask(id) {
  const response = await fetch(`/api/tasks/${id}`, { method: "DELETE" });
  if (!response.ok) {
    const error = await response.json().catch(() => ({}));
    throw new Error(error.message || "일정을 삭제하지 못했습니다.");
  }
  renderTasks(await response.json());
  addBubble("일정을 삭제했어요.", "assistant");
  await refreshProgress();
}

async function toggleComplete(id) {
  const response = await fetch(`/api/tasks/${id}/complete`, { method: "PATCH" });
  if (!response.ok) throw new Error("완료 상태를 변경하지 못했습니다.");
  const data = await response.json();
  renderTasks(data.tasks);
  await refreshProgress();
}

async function refreshProgress() {
  const response = await fetch("/api/tasks/progress");
  if (response.ok) renderProgress(await response.json());
}

async function saveTask(title, time, confirmConflict = false) {
  const body = {
    title, time,
    isImportant: pendingOptions.isImportant,
    isUrgent: pendingOptions.isUrgent,
    durationMinutes: pendingOptions.durationMinutes,
    deadline: pendingOptions.deadline || null,
    confirmConflict,
  };
  const response = await fetch("/api/tasks", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  if (response.status === 409) {
    const conflict = await response.json();
    const names = (conflict.conflicts || []).map((c) => `${c.title} (${c.overlapRange})`).join("\n");
    const ok = window.confirm(`⚠️ 겹치는 일정이 있어요:\n${names}\n\n그래도 추가할까요?`);
    if (ok) return saveTask(title, time, true);
    throw new Error("겹치는 일정 때문에 추가를 취소했어요.");
  }

  if (!response.ok) {
    const error = await response.json().catch(() => ({}));
    throw new Error(error.message || "일정을 저장하지 못했습니다.");
  }
  return response.json();
}

async function handleSubmit(event) {
  event?.preventDefault();

  const title = taskTitleInput?.value.trim();
  const time = timePicker?.getValue();

  if (!title) {
    if (hintEl) hintEl.textContent = "할 일을 입력해 주세요.";
    taskTitleInput?.focus();
    return;
  }
  if (!time) {
    if (hintEl) hintEl.textContent = "시작 시간을 선택해 주세요.";
    return;
  }

  pendingOptions.isImportant = isImportantEl?.checked ?? false;
  pendingOptions.isUrgent = isUrgentEl?.checked ?? false;
  pendingOptions.deadline = resolveDeadline();

  const activeChip = durationChips?.querySelector(".duration-chip.active");
  if (activeChip) {
    pendingOptions.durationMinutes = Number(activeChip.dataset.min);
  }

  if (submitBtn) {
    submitBtn.disabled = true;
    submitBtn.textContent = "추가 중...";
  }

  try {
    const result = await saveTask(title, time);
    const dateLabel = PlanMatePickers.formatDateLabel(scheduleDatePicker?.getValue());
    addBubble(`「${title}」 ${dateLabel || "오늘"} ${PlanMatePickers.formatTimeLabel(time)}`, "user");
    if (result.created?.advice?.trim()) {
      addBubble(`💡 ${result.created.advice.trim()}${result.usedGemini ? " (Gemini)" : ""}`, "assistant");
    }
    addBubble(`등록했어요. 예상 ${result.durationLabel}, 우선순위 순으로 정렬됩니다.`, "assistant");
    renderTasks(result.tasks);
    await refreshProgress();
    await loadDeadlineAlerts();
    resetForm();
  } catch (err) {
    if (hintEl) hintEl.textContent = err.message;
    addBubble(err.message, "assistant");
  } finally {
    if (submitBtn) {
      submitBtn.disabled = false;
      submitBtn.textContent = "일정 추가";
    }
  }
}

async function loadDeadlineAlerts() {
  const response = await fetch("/api/tasks/deadline-alerts");
  if (response.ok) renderDeadlineAlerts(await response.json());
}

async function optimizeSchedule() {
  if (optimizeBtn) {
    optimizeBtn.disabled = true;
    optimizeBtn.querySelector("strong").textContent = "재배치 중...";
  }
  try {
    const response = await fetch("/api/tasks/optimize", { method: "POST" });
    if (!response.ok) throw new Error("일정 재배치에 실패했습니다.");
    const data = await response.json();
    renderTasks(data.tasks);
    addBubble(`🤖 ${data.summary}${data.usedGemini ? " (Gemini)" : ""}`, "assistant");
    if (conflictPanel) conflictPanel.classList.add("hidden");
  } catch (err) {
    if (hintEl) hintEl.textContent = err.message;
  } finally {
    if (optimizeBtn) {
      optimizeBtn.disabled = false;
      optimizeBtn.querySelector("strong").textContent = "AI 일정 재배치";
    }
  }
}

function formatPomodoroTime(seconds) {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${String(m).padStart(2, "0")}:${String(s).padStart(2, "0")}`;
}

function clampMinutes(value, min, max) {
  const n = Number.parseInt(String(value), 10);
  if (Number.isNaN(n)) return min;
  return Math.min(max, Math.max(min, n));
}

function syncDurationChipActive(chipsEl, minutes) {
  if (!chipsEl) return;
  chipsEl.querySelectorAll(".duration-chip").forEach((chip) => {
    chip.classList.toggle("active", Number(chip.dataset.min) === minutes);
  });
}

function setFocusDuration(minutes) {
  focusDurationMinutes = clampMinutes(minutes, 1, 180);
  if (focusMinutesInput) focusMinutesInput.value = focusDurationMinutes;
  syncDurationChipActive(focusDurationChips, focusDurationMinutes);
  applyPomodoroDurationToDisplay();
}

function setBreakDuration(minutes) {
  breakDurationMinutes = clampMinutes(minutes, 1, 60);
  if (breakMinutesInput) breakMinutesInput.value = breakDurationMinutes;
  syncDurationChipActive(breakDurationChips, breakDurationMinutes);
  applyPomodoroDurationToDisplay();
}

function applyPomodoroDurationToDisplay() {
  if (pomodoroRunning) return;

  const pausedMidSession = pomodoroStartBtn?.textContent === "재개";
  if (pausedMidSession) {
    if (pomodoroMode) {
      pomodoroMode.textContent = pomodoroPhase === "focus"
        ? `집중 ${focusDurationMinutes}분`
        : `휴식 ${breakDurationMinutes}분`;
    }
    return;
  }

  if (pomodoroPhase === "focus") {
    pomodoroSeconds = focusDurationMinutes * 60;
    if (pomodoroMode) pomodoroMode.textContent = `집중 ${focusDurationMinutes}분`;
  } else {
    pomodoroSeconds = breakDurationMinutes * 60;
    if (pomodoroMode) pomodoroMode.textContent = `휴식 ${breakDurationMinutes}분`;
  }
  if (pomodoroTime) pomodoroTime.textContent = formatPomodoroTime(pomodoroSeconds);
}

function setPomodoroSettingsLocked(locked) {
  if (pomodoroSettings) pomodoroSettings.classList.toggle("is-running", locked);
}

function initPomodoroDurationControls() {
  focusDurationChips?.querySelectorAll(".duration-chip").forEach((chip) => {
    chip.addEventListener("click", () => setFocusDuration(Number(chip.dataset.min)));
  });
  breakDurationChips?.querySelectorAll(".duration-chip").forEach((chip) => {
    chip.addEventListener("click", () => setBreakDuration(Number(chip.dataset.min)));
  });

  focusMinutesInput?.addEventListener("change", () => setFocusDuration(focusMinutesInput.value));
  focusMinutesInput?.addEventListener("input", () => {
    if (focusMinutesInput.value === "") return;
    setFocusDuration(focusMinutesInput.value);
  });

  breakMinutesInput?.addEventListener("change", () => setBreakDuration(breakMinutesInput.value));
  breakMinutesInput?.addEventListener("input", () => {
    if (breakMinutesInput.value === "") return;
    setBreakDuration(breakMinutesInput.value);
  });
}

function resetPomodoro() {
  clearInterval(pomodoroInterval);
  pomodoroRunning = false;
  pomodoroPhase = "focus";
  pomodoroSeconds = focusDurationMinutes * 60;
  setPomodoroSettingsLocked(false);
  if (pomodoroMode) pomodoroMode.textContent = `집중 ${focusDurationMinutes}분`;
  if (pomodoroTime) pomodoroTime.textContent = formatPomodoroTime(pomodoroSeconds);
  if (pomodoroStartBtn) pomodoroStartBtn.textContent = "시작";
}

function startPomodoro() {
  if (pomodoroRunning) {
    clearInterval(pomodoroInterval);
    pomodoroRunning = false;
    setPomodoroSettingsLocked(false);
    if (pomodoroStartBtn) pomodoroStartBtn.textContent = "재개";
    return;
  }

  if (pomodoroStartBtn?.textContent === "시작") {
    pomodoroSeconds = pomodoroPhase === "focus"
      ? focusDurationMinutes * 60
      : breakDurationMinutes * 60;
    if (pomodoroTime) pomodoroTime.textContent = formatPomodoroTime(pomodoroSeconds);
  }

  pomodoroRunning = true;
  setPomodoroSettingsLocked(true);
  if (pomodoroStartBtn) pomodoroStartBtn.textContent = "일시정지";
  pomodoroInterval = setInterval(() => {
    pomodoroSeconds -= 1;
    if (pomodoroTime) pomodoroTime.textContent = formatPomodoroTime(pomodoroSeconds);
    if (pomodoroSeconds <= 0) {
      if (pomodoroPhase === "focus") {
        pomodoroPhase = "break";
        pomodoroSeconds = breakDurationMinutes * 60;
        if (pomodoroMode) pomodoroMode.textContent = `휴식 ${breakDurationMinutes}분`;
        addBubble(`${focusDurationMinutes}분 집중 완료! ${breakDurationMinutes}분 휴식하세요.`, "assistant");
      } else {
        pomodoroPhase = "focus";
        pomodoroSeconds = focusDurationMinutes * 60;
        if (pomodoroMode) pomodoroMode.textContent = `집중 ${focusDurationMinutes}분`;
        addBubble("휴식 끝! 다시 집중해 볼까요?", "assistant");
      }
      if (pomodoroTime) pomodoroTime.textContent = formatPomodoroTime(pomodoroSeconds);
    }
  }, 1000);
}

function scrollToPomodoro() {
  if (pomodoroPanel) {
    pomodoroPanel.scrollIntoView({ behavior: "smooth", block: "center" });
  }
  startPomodoro();
}

if (taskForm) taskForm.addEventListener("submit", handleSubmit);
if (taskTitleInput) {
  taskTitleInput.addEventListener("blur", () => {
    const title = taskTitleInput.value.trim();
    if (title) predictDuration(title);
  });
}
if (optimizeBtn) optimizeBtn.addEventListener("click", optimizeSchedule);
if (conflictOptimizeBtn) conflictOptimizeBtn.addEventListener("click", optimizeSchedule);
if (conflictIgnoreBtn) conflictIgnoreBtn.addEventListener("click", () => conflictPanel?.classList.add("hidden"));
if (pomodoroStartBtn) pomodoroStartBtn.addEventListener("click", startPomodoro);
if (pomodoroResetBtn) pomodoroResetBtn.addEventListener("click", resetPomodoro);
if (startFocusBtn) startFocusBtn.addEventListener("click", scrollToPomodoro);

if ("Notification" in window && Notification.permission === "default") {
  Notification.requestPermission();
}

if (chatEl) addBubble("할 일, 날짜, 시간을 한 번에 설정할 수 있어요.", "assistant");
initPickers();
initPomodoroDurationControls();
startTodayDateClock();
loadTasks();
resetPomodoro();

setInterval(() => {
  if (latestTasks.length) {
    renderAgenda(latestTasks);
    renderFocusHero(latestTasks);
  }
}, 60000);
