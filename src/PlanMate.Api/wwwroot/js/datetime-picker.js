/**
 * Plan mate — 캘린더·시간 선택 UI
 */
const PlanMatePickers = (() => {
  const DOW = ["일", "월", "화", "수", "목", "금", "토"];

  function pad(n) {
    return String(n).padStart(2, "0");
  }

  function toDateKey(d) {
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  }

  function parseDateKey(key) {
    if (!key) return null;
    const [y, m, d] = key.split("-").map(Number);
    return new Date(y, m - 1, d);
  }

  function formatDateLabel(key) {
    if (!key) return null;
    const d = parseDateKey(key);
    const today = new Date();
    const isToday =
      d.getFullYear() === today.getFullYear() &&
      d.getMonth() === today.getMonth() &&
      d.getDate() === today.getDate();
    const fmt = new Intl.DateTimeFormat("ko-KR", {
      month: "long",
      day: "numeric",
      weekday: "short",
    });
    return isToday ? `오늘 · ${fmt.format(d)}` : fmt.format(d);
  }

  function formatTimeLabel(time) {
    if (!time) return "시간 선택";
    const [h, m] = time.split(":").map(Number);
    return `${pad(h)}:${pad(m)}`;
  }

  function closeAllPopovers(except) {
    document.querySelectorAll(".picker-popover.open").forEach((el) => {
      if (el !== except) el.classList.remove("open");
    });
  }

  document.addEventListener("click", (e) => {
    if (!e.target.closest(".picker-wrap")) {
      closeAllPopovers();
    }
  });

  /**
   * @param {object} opts
   * @param {HTMLElement} opts.trigger - 버튼 요소
   * @param {string|null} opts.value - yyyy-MM-dd
   * @param {string} opts.placeholder
   * @param {boolean} opts.allowClear
   * @param {(key: string|null) => void} opts.onChange
   */
  function attachDatePicker(opts) {
    const { trigger, placeholder = "날짜 선택", allowClear = false, onChange } = opts;
    let value = opts.value || null;
    let viewDate = parseDateKey(value) || new Date();

    const wrap = document.createElement("div");
    wrap.className = "picker-wrap";
    trigger.parentNode.insertBefore(wrap, trigger);
    wrap.appendChild(trigger);
    trigger.type = "button";
    trigger.classList.add("picker-trigger");

    const popover = document.createElement("div");
    popover.className = "picker-popover picker-calendar";
    popover.innerHTML = `
      <div class="picker-cal-head">
        <button type="button" class="cal-nav-btn" data-cal-prev aria-label="이전 달">‹</button>
        <span class="picker-cal-title"></span>
        <button type="button" class="cal-nav-btn" data-cal-next aria-label="다음 달">›</button>
      </div>
      <div class="picker-cal-grid"></div>
      <div class="picker-cal-foot">
        <button type="button" class="picker-foot-btn" data-cal-today>오늘</button>
        ${allowClear ? '<button type="button" class="picker-foot-btn muted" data-cal-clear>선택 안 함</button>' : ""}
      </div>
    `;
    wrap.appendChild(popover);

    const titleEl = popover.querySelector(".picker-cal-title");
    const gridEl = popover.querySelector(".picker-cal-grid");

    function updateTrigger() {
      trigger.textContent = value ? formatDateLabel(value) : placeholder;
      trigger.classList.toggle("has-value", !!value);
    }

    function renderGrid() {
      const year = viewDate.getFullYear();
      const month = viewDate.getMonth();
      titleEl.textContent = `${year}년 ${month + 1}월`;

      const first = new Date(year, month, 1);
      const start = first.getDay();
      const daysInMonth = new Date(year, month + 1, 0).getDate();
      const todayKey = toDateKey(new Date());
      const total = Math.ceil((start + daysInMonth) / 7) * 7;

      gridEl.innerHTML =
        DOW.map((d) => `<div class="picker-cal-dow">${d}</div>`).join("") +
        Array.from({ length: total }, (_, i) => {
          const dayNum = i - start + 1;
          const inMonth = dayNum >= 1 && dayNum <= daysInMonth;
          const cellDate = inMonth ? new Date(year, month, dayNum) : null;
          const key = cellDate ? toDateKey(cellDate) : "";
          const classes = [
            "picker-cal-day",
            !inMonth ? "other" : "",
            key === todayKey ? "today" : "",
            key === value ? "selected" : "",
          ]
            .filter(Boolean)
            .join(" ");
          return `<button type="button" class="${classes}" data-key="${key}" ${inMonth ? "" : "disabled"}>${inMonth ? dayNum : ""}</button>`;
        }).join("");
    }

    function setValue(key) {
      value = key;
      updateTrigger();
      onChange?.(value);
      popover.classList.remove("open");
    }

    trigger.addEventListener("click", (e) => {
      e.stopPropagation();
      const open = popover.classList.toggle("open");
      if (open) {
        closeAllPopovers(popover);
        viewDate = parseDateKey(value) || new Date();
        renderGrid();
      }
    });

    popover.addEventListener("click", (e) => {
      e.stopPropagation();
      const dayBtn = e.target.closest("[data-key]");
      if (dayBtn && !dayBtn.disabled) {
        setValue(dayBtn.dataset.key);
        return;
      }
      if (e.target.closest("[data-cal-prev]")) {
        viewDate = new Date(viewDate.getFullYear(), viewDate.getMonth() - 1, 1);
        renderGrid();
      }
      if (e.target.closest("[data-cal-next]")) {
        viewDate = new Date(viewDate.getFullYear(), viewDate.getMonth() + 1, 1);
        renderGrid();
      }
      if (e.target.closest("[data-cal-today]")) {
        setValue(toDateKey(new Date()));
      }
      if (e.target.closest("[data-cal-clear]")) {
        setValue(null);
      }
    });

    updateTrigger();

    return {
      getValue: () => value,
      setValue: (key) => {
        value = key;
        updateTrigger();
      },
    };
  }

  /**
   * @param {HTMLElement} container
   * @param {{ value?: string, onChange?: (time: string) => void }} opts
   */
  function attachTimePicker(container, opts = {}) {
    let value = opts.value || "";
    const onChange = opts.onChange;

    container.classList.add("time-picker");
    container.innerHTML = `
      <div class="time-picker-display">
        <span class="time-picker-label">${value ? formatTimeLabel(value) : "시간을 선택하세요"}</span>
      </div>
      <div class="time-quick-row">
        <button type="button" class="time-quick-btn" data-offset="0">지금</button>
        <button type="button" class="time-quick-btn" data-offset="30">30분 후</button>
        <button type="button" class="time-quick-btn" data-offset="60">1시간 후</button>
        <button type="button" class="time-quick-btn" data-round="next-hour">다음 정각</button>
      </div>
      <div class="time-custom-row">
        <label class="input-label">24시간 · 시 : 분</label>
        <div class="time-select-row">
          <select class="time-select time-hour" aria-label="시 (0–23)"></select>
          <span class="time-colon">:</span>
          <select class="time-select time-minute" aria-label="분 (0–59)"></select>
        </div>
      </div>
    `;

    const labelEl = container.querySelector(".time-picker-label");
    const hourSelect = container.querySelector(".time-hour");
    const minuteSelect = container.querySelector(".time-minute");

    for (let h = 0; h < 24; h++) {
      const opt = document.createElement("option");
      opt.value = pad(h);
      opt.textContent = pad(h);
      hourSelect.appendChild(opt);
    }
    for (let m = 0; m < 60; m++) {
      const opt = document.createElement("option");
      opt.value = pad(m);
      opt.textContent = pad(m);
      minuteSelect.appendChild(opt);
    }

    function setValue(time) {
      value = time;
      labelEl.textContent = formatTimeLabel(time);
      if (time) {
        const [h, m] = time.split(":");
        hourSelect.value = pad(Number(h));
        minuteSelect.value = pad(Number(m));
      }
      onChange?.(time);
    }

    function offsetMinutes(minutes) {
      const now = new Date();
      now.setMinutes(now.getMinutes() + minutes);
      setValue(`${pad(now.getHours())}:${pad(now.getMinutes())}`);
    }

    container.querySelectorAll(".time-quick-btn").forEach((btn) => {
      btn.addEventListener("click", () => {
        if (btn.dataset.offset !== undefined) {
          offsetMinutes(Number(btn.dataset.offset));
        } else if (btn.dataset.round === "next-hour") {
          const now = new Date();
          now.setHours(now.getHours() + 1, 0, 0, 0);
          setValue(`${pad(now.getHours())}:00`);
        }
      });
    });

    function syncFromSelects() {
      setValue(`${hourSelect.value}:${minuteSelect.value}`);
    }
    hourSelect.addEventListener("change", syncFromSelects);
    minuteSelect.addEventListener("change", syncFromSelects);

    if (!value) {
      offsetMinutes(30);
    } else {
      setValue(value);
    }

    return {
      getValue: () => value,
      setValue,
    };
  }

  return { attachDatePicker, attachTimePicker, toDateKey, formatDateLabel, formatTimeLabel };
})();
