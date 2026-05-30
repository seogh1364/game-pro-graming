const calGrid = document.getElementById("calGrid");
const calMonthLabel = document.getElementById("calMonthLabel");
const calPrev = document.getElementById("calPrev");
const calNext = document.getElementById("calNext");
const calToday = document.getElementById("calToday");
const detailDateFull = document.getElementById("detailDateFull");
const detailSchedule = document.getElementById("detailSchedule");
const aiSuggestText = document.getElementById("aiSuggestText");
const suggestFocusBtn = document.getElementById("suggestFocusBtn");
const energyBars = document.getElementById("energyBars");
const energyNote = document.getElementById("energyNote");

const DOW = ["일", "월", "화", "수", "목", "금", "토"];
const eventColors = ["cal-event-purple", "cal-event-cyan", "cal-event-coral"];

let viewDate = new Date();
let selectedDate = new Date();
let allTasks = [];
let dayEventsModalEl = null;

function ensureDayEventsModal() {
  if (dayEventsModalEl) return dayEventsModalEl;

  dayEventsModalEl = document.createElement("div");
  dayEventsModalEl.id = "calDayEventsModal";
  dayEventsModalEl.className = "cal-day-modal hidden";
  dayEventsModalEl.setAttribute("role", "dialog");
  dayEventsModalEl.setAttribute("aria-modal", "true");
  dayEventsModalEl.innerHTML = `
    <div class="cal-day-modal-backdrop" data-close-cal-day></div>
    <div class="cal-day-modal-panel">
      <button type="button" class="cal-day-modal-close" data-close-cal-day aria-label="닫기">×</button>
      <p class="cal-day-modal-kicker">일정 목록</p>
      <h3 class="cal-day-modal-title" id="calDayModalTitle"></h3>
      <div class="cal-day-modal-list" id="calDayModalList"></div>
    </div>
  `;
  document.body.appendChild(dayEventsModalEl);

  dayEventsModalEl.querySelectorAll("[data-close-cal-day]").forEach((el) => {
    el.addEventListener("click", closeDayEventsModal);
  });

  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape" && dayEventsModalEl && !dayEventsModalEl.classList.contains("hidden")) {
      closeDayEventsModal();
    }
  });

  return dayEventsModalEl;
}

function closeDayEventsModal() {
  if (!dayEventsModalEl) return;
  dayEventsModalEl.classList.add("hidden");
  document.body.style.overflow = "";
}

function showDayEventsModal(date, tasks) {
  ensureDayEventsModal();
  const fmt = new Intl.DateTimeFormat("ko-KR", {
    weekday: "long", month: "long", day: "numeric",
  });
  dayEventsModalEl.querySelector("#calDayModalTitle").textContent = fmt.format(date);
  dayEventsModalEl.querySelector("#calDayModalList").innerHTML = tasks.length
    ? tasks.map((t, idx) => `
        <div class="cal-day-modal-item${t.advice?.trim() ? " cal-day-modal-item-clickable" : ""}" ${t.advice?.trim() ? `data-task-idx="${idx}" tabindex="0" role="button"` : ""}>
          <div class="cal-day-modal-item-time">${formatTimeDisplay(t.time)} · ${t.durationMinutes}분</div>
          <h4>${escapeHtml(t.title)}</h4>
          ${t.advice?.trim() ? `<p class="cal-day-modal-item-advice">${escapeHtml(AdviceUI.summarizeAdvice(t.advice.trim()).short)}</p>` : ""}
        </div>
      `).join("")
    : `<p class="empty" style="padding:12px">등록된 일정이 없습니다.</p>`;

  dayEventsModalEl.querySelectorAll(".cal-day-modal-item-clickable").forEach((el) => {
    const t = tasks[Number(el.dataset.taskIdx)];
    if (!t?.advice?.trim()) return;
    AdviceUI.bindAgendaAdviceClick(el, {
      title: t.title,
      time: `${formatTimeDisplay(t.time)} · ${t.durationMinutes}분`,
      advice: t.advice.trim(),
    });
  });

  dayEventsModalEl.classList.remove("hidden");
  document.body.style.overflow = "hidden";
  dayEventsModalEl.querySelector(".cal-day-modal-close").focus();
}

function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

function formatTimeDisplay(time) {
  const [hour, minute] = time.split(":").map(Number);
  return `${String(hour).padStart(2, "0")}:${String(minute).padStart(2, "0")}`;
}

function sameDay(a, b) {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
}

function dateKey(d) {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function getTasksForDate(d) {
  const today = new Date();
  const key = dateKey(d);
  const todayKey = dateKey(today);

  if (key === todayKey) {
    return allTasks.filter((t) => !t.deadline || t.deadline.slice(0, 10) >= todayKey);
  }

  return allTasks.filter((t) => t.deadline && t.deadline.slice(0, 10) === key);
}

function renderCalendar() {
  const year = viewDate.getFullYear();
  const month = viewDate.getMonth();
  calMonthLabel.textContent = `${year}년 ${month + 1}월`;

  const firstDay = new Date(year, month, 1);
  const startOffset = firstDay.getDay();
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const today = new Date();

  calGrid.innerHTML = DOW.map((d) => `<div class="cal-dow">${d}</div>`).join("");

  const totalCells = Math.ceil((startOffset + daysInMonth) / 7) * 7;

  for (let i = 0; i < totalCells; i++) {
    const dayNum = i - startOffset + 1;
    const cellDate = new Date(year, month, dayNum);
    const otherMonth = dayNum < 1 || dayNum > daysInMonth;
    const displayDay = otherMonth
      ? dayNum < 1
        ? new Date(year, month, dayNum).getDate()
        : dayNum - daysInMonth
      : dayNum;

    const isToday = !otherMonth && sameDay(cellDate, today);
    const isSelected = !otherMonth && sameDay(cellDate, selectedDate);

    const tasks = otherMonth ? [] : getTasksForDate(cellDate);
    const events = tasks.slice(0, 2).map((t, idx) =>
      `<div class="cal-event ${eventColors[idx % 3]}">${escapeHtml(t.title)}</div>`
    ).join("");
    const more = tasks.length > 2
      ? `<button type="button" class="cal-event cal-event-more cal-event-cyan">+${tasks.length - 2}개</button>`
      : "";

    const cell = document.createElement("div");
    cell.className = `cal-day${otherMonth ? " other-month" : ""}${isToday ? " today" : ""}${isSelected ? " selected" : ""}`;
    cell.innerHTML = `<div class="cal-day-num">${displayDay}</div>${events}${more}`;

    if (!otherMonth) {
      cell.addEventListener("click", (e) => {
        if (e.target.closest(".cal-event-more")) return;
        selectedDate = new Date(year, month, dayNum);
        renderCalendar();
        renderDetail();
      });

      const moreBtn = cell.querySelector(".cal-event-more");
      if (moreBtn) {
        moreBtn.addEventListener("click", (e) => {
          e.stopPropagation();
          selectedDate = new Date(year, month, dayNum);
          renderCalendar();
          renderDetail();
          const sorted = [...tasks].sort((a, b) => a.sortMinutes - b.sortMinutes);
          showDayEventsModal(selectedDate, sorted);
        });
      }
    }
    calGrid.appendChild(cell);
  }
}

function renderDetail() {
  const fmt = new Intl.DateTimeFormat("ko-KR", {
    weekday: "long", month: "long", day: "numeric",
  });
  detailDateFull.textContent = fmt.format(selectedDate);

  const tasks = getTasksForDate(selectedDate);
  if (!tasks.length) {
    detailSchedule.innerHTML = `<p class="empty" style="padding:12px">이 날짜에 등록된 일정이 없습니다.</p>`;
    aiSuggestText.textContent = "여유로운 하루예요. 집중 블록이나 휴식 시간을 추가해 보세요.";
  } else {
    detailSchedule.innerHTML = tasks.map((t, idx) => {
      const adviceFull = (t.advice && t.advice.trim()) || "";
      const summary = adviceFull ? AdviceUI.summarizeAdvice(adviceFull) : null;
      return `
      <div class="detail-item${summary ? " detail-item-clickable" : ""}" ${summary ? `tabindex="0" role="button" data-task-idx="${idx}"` : ""}>
        <div class="detail-item-time">${formatTimeDisplay(t.time)} · ${t.durationMinutes}분</div>
        <h4>${escapeHtml(t.title)}</h4>
        ${summary ? `
          <p class="agenda-advice-label">💡 AI 조언</p>
          <p class="agenda-advice-preview">${escapeHtml(summary.short)}</p>
          <span class="advice-more-hint">탭하여 전체 보기 →</span>
        ` : ""}
      </div>
    `;
    }).join("");

    detailSchedule.querySelectorAll(".detail-item-clickable").forEach((el) => {
      const t = tasks[Number(el.dataset.taskIdx)];
      if (!t?.advice?.trim()) return;
      AdviceUI.bindAgendaAdviceClick(el, {
        title: t.title,
        time: `${formatTimeDisplay(t.time)} · ${t.durationMinutes}분`,
        advice: t.advice.trim(),
      });
    });

    const gaps = findGaps(tasks);
    if (gaps.length) {
      aiSuggestText.textContent = `${gaps[0].start}~${gaps[0].end} 사이에 ${gaps[0].minutes}분 집중 블록을 추가하면 좋아요.`;
    } else {
      aiSuggestText.textContent = "일정이 빡빡해요. AI 재배치를 고려해 보세요.";
    }
  }

  renderEnergy(tasks);
}

function findGaps(tasks) {
  const sorted = [...tasks].sort((a, b) => a.sortMinutes - b.sortMinutes);
  const gaps = [];
  for (let i = 0; i < sorted.length - 1; i++) {
    const gap = sorted[i + 1].sortMinutes - sorted[i].endSortMinutes;
    if (gap >= 30) {
      const startH = Math.floor(sorted[i].endSortMinutes / 60);
      const startM = sorted[i].endSortMinutes % 60;
      const endH = Math.floor(sorted[i + 1].sortMinutes / 60);
      const endM = sorted[i + 1].sortMinutes % 60;
      gaps.push({
        minutes: gap,
        start: `${startH}:${String(startM).padStart(2, "0")}`,
        end: `${endH}:${String(endM).padStart(2, "0")}`,
      });
    }
  }
  return gaps;
}

function renderEnergy(tasks) {
  const hours = [8, 10, 12, 14, 16, 18, 20];
  const loads = hours.map((h) => {
    const start = h * 60;
    const end = start + 120;
    return tasks.filter((t) => t.sortMinutes < end && t.endSortMinutes > start).length;
  });
  const max = Math.max(1, ...loads);

  energyBars.innerHTML = loads.map((load, i) => {
    const height = Math.max(15, 100 - (load / max) * 80);
    return `<div class="energy-bar" style="height:${height}%" title="${hours[i]}시"></div>`;
  }).join("");

  const peakIdx = loads.indexOf(Math.min(...loads));
  const peakHour = hours[peakIdx >= 0 ? peakIdx : 1];
  energyNote.textContent = `${String(peakHour).padStart(2, "0")}:00~${String(peakHour + 2).padStart(2, "0")}:00에 집중력이 가장 높을 것으로 예측됩니다.`;
}

async function loadTasks() {
  try {
    const response = await fetch("/api/tasks");
    if (!response.ok) throw new Error("일정을 불러오지 못했습니다.");
    allTasks = await response.json();
    renderCalendar();
    renderDetail();
  } catch (err) {
    calGrid.innerHTML = `<p class="empty">${escapeHtml(err.message)}</p>`;
  }
}

calPrev.addEventListener("click", () => {
  viewDate = new Date(viewDate.getFullYear(), viewDate.getMonth() - 1, 1);
  renderCalendar();
});

calNext.addEventListener("click", () => {
  viewDate = new Date(viewDate.getFullYear(), viewDate.getMonth() + 1, 1);
  renderCalendar();
});

calToday.addEventListener("click", () => {
  viewDate = new Date();
  selectedDate = new Date();
  renderCalendar();
  renderDetail();
});

suggestFocusBtn.addEventListener("click", () => {
  location.href = "/";
});

loadTasks();
