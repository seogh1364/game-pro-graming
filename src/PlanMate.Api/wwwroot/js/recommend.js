const routineTitleEl = document.getElementById("routineTitle");
const routineSummaryEl = document.getElementById("routineSummary");
const routineDayEl = document.getElementById("routineDay");
const routineTimetableEl = document.getElementById("routineTimetable");
const refreshRoutineBtn = document.getElementById("refreshRoutineBtn");
const weatherCard = document.getElementById("weatherCard");

function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

function renderWeather(data) {
  weatherCard.innerHTML = `
    <div class="weather-icon">${escapeHtml(data.icon)}</div>
    <div>
      <p class="weather-title">${escapeHtml(data.condition)} · ${escapeHtml(data.temperature)}</p>
      <p class="weather-desc">${escapeHtml(data.recommendation)}</p>
      <p class="weather-activity">추천: ${escapeHtml(data.activity)}</p>
    </div>
  `;
}

async function loadWeather() {
  try {
    const response = await fetch("/api/weather/recommendation");
    if (!response.ok) throw new Error("날씨 정보를 불러오지 못했습니다.");
    renderWeather(await response.json());
  } catch (err) {
    weatherCard.innerHTML = `<p class="hint">${escapeHtml(err.message)}</p>`;
  }
}

function renderWeekendRoutine(plan) {
  routineTitleEl.textContent = plan.title;
  routineSummaryEl.textContent = plan.summary;
  routineDayEl.textContent = plan.dayLabel;
  routineTimetableEl.innerHTML = "";

  plan.slots.forEach((slot) => {
    const row = document.createElement("div");
    row.className = "timetable-row";
    row.innerHTML = `
      <div class="timetable-time">${escapeHtml(slot.time)}</div>
      <div>
        <p class="timetable-activity">${escapeHtml(slot.activity)}</p>
        <p class="timetable-tip">💡 ${escapeHtml(slot.tip)}</p>
      </div>
    `;
    routineTimetableEl.appendChild(row);
  });
}

async function loadWeekendRoutine() {
  routineTitleEl.textContent = "불러오는 중...";
  routineTimetableEl.innerHTML = "";

  try {
    const response = await fetch("/api/recommendations/weekend-workout");
    if (!response.ok) throw new Error("추천 루틴을 불러오지 못했습니다.");
    renderWeekendRoutine(await response.json());
  } catch (err) {
    routineTitleEl.textContent = "오류";
    routineSummaryEl.textContent = err.message;
  }
}

refreshRoutineBtn.addEventListener("click", loadWeekendRoutine);
loadWeather();
loadWeekendRoutine();
