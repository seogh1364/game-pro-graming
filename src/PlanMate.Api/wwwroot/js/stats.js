const statsSummary = document.getElementById("statsSummary");
const statsChart = document.getElementById("statsChart");

const categoryMeta = [
  { key: "studyMinutes", label: "공부", color: "#667eea" },
  { key: "exerciseMinutes", label: "운동", color: "#26c6da" },
  { key: "restMinutes", label: "휴식", color: "#ff7b6b" },
  { key: "generalMinutes", label: "기타", color: "#7c5cff" },
];

function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

function formatMinutes(minutes) {
  if (minutes < 60) return `${minutes}분`;
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return m === 0 ? `${h}시간` : `${h}시간 ${m}분`;
}

function renderSummary(data) {
  statsSummary.innerHTML = categoryMeta.map((cat) => `
    <div class="stat-card">
      <span class="stat-label">${cat.label}</span>
      <strong class="stat-value">${formatMinutes(data[cat.key])}</strong>
    </div>
  `).join("");
}

function renderChart(daily) {
  const max = Math.max(
    1,
    ...daily.flatMap((d) => [d.study, d.exercise, d.rest, d.general])
  );

  statsChart.innerHTML = daily.map((day) => {
    const bars = [
      { value: day.study, color: "#667eea", label: "공부" },
      { value: day.exercise, color: "#26c6da", label: "운동" },
      { value: day.rest, color: "#ff7b6b", label: "휴식" },
      { value: day.general, color: "#7c5cff", label: "기타" },
    ].map((bar) => `
      <div class="chart-bar-wrap" title="${bar.label} ${bar.value}분">
        <div class="chart-bar" style="height:${(bar.value / max) * 100}%; background:${bar.color}"></div>
      </div>
    `).join("");

    return `
      <div class="chart-day">
        <div class="chart-bars">${bars}</div>
        <span class="chart-label">${escapeHtml(day.dayLabel)}</span>
      </div>
    `;
  }).join("");
}

async function loadStats() {
  try {
    const response = await fetch("/api/statistics/week");
    if (!response.ok) throw new Error("통계를 불러오지 못했습니다.");
    const data = await response.json();
    renderSummary(data);
    renderChart(data.daily);
  } catch (err) {
    statsSummary.innerHTML = `<p class="empty">${escapeHtml(err.message)}</p>`;
  }
}

loadStats();
