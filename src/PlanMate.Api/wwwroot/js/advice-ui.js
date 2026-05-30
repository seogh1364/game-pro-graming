/**
 * AI 조언 카드 요약 + 상세 모달
 */
const AdviceUI = (() => {
  let modalEl = null;

  function escapeHtml(text) {
    const div = document.createElement("div");
    div.textContent = text;
    return div.innerHTML;
  }

  function summarizeAdvice(text, maxLen = 90) {
    const full = (text || "").trim() || "차근차근 진행해 보세요.";
    if (full.length <= maxLen) {
      return { short: full, full, truncated: false };
    }

    const chunk = full.slice(0, maxLen);
    const lastStop = Math.max(
      chunk.lastIndexOf("."),
      chunk.lastIndexOf("!"),
      chunk.lastIndexOf("?"),
      chunk.lastIndexOf("。")
    );

    if (lastStop > 35) {
      return { short: full.slice(0, lastStop + 1), full, truncated: true };
    }

    const lastSpace = chunk.lastIndexOf(" ");
    const short =
      (lastSpace > 30 ? chunk.slice(0, lastSpace) : chunk.trim()) + "…";
    return { short, full, truncated: true };
  }

  function ensureModal() {
    if (modalEl) return modalEl;

    modalEl = document.createElement("div");
    modalEl.id = "adviceModal";
    modalEl.className = "advice-modal hidden";
    modalEl.setAttribute("role", "dialog");
    modalEl.setAttribute("aria-modal", "true");
    modalEl.innerHTML = `
      <div class="advice-modal-backdrop" data-close-advice></div>
      <div class="advice-modal-panel">
        <button type="button" class="advice-modal-close" data-close-advice aria-label="닫기">×</button>
        <p class="advice-modal-kicker">💡 AI 조언</p>
        <h3 class="advice-modal-title" id="adviceModalTitle"></h3>
        <p class="advice-modal-meta" id="adviceModalMeta"></p>
        <div class="advice-modal-body" id="adviceModalBody"></div>
      </div>
    `;
    document.body.appendChild(modalEl);

    modalEl.querySelectorAll("[data-close-advice]").forEach((el) => {
      el.addEventListener("click", close);
    });

    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape" && modalEl && !modalEl.classList.contains("hidden")) {
        close();
      }
    });

    return modalEl;
  }

  function show({ title, time, advice, durationLabel }) {
    ensureModal();
    modalEl.querySelector("#adviceModalTitle").textContent = title;
    modalEl.querySelector("#adviceModalMeta").textContent = [time, durationLabel]
      .filter(Boolean)
      .join(" · ");
    modalEl.querySelector("#adviceModalBody").textContent =
      (advice && advice.trim()) || "차근차근 진행해 보세요.";
    modalEl.classList.remove("hidden");
    document.body.style.overflow = "hidden";
    modalEl.querySelector(".advice-modal-close").focus();
  }

  function close() {
    if (!modalEl) return;
    modalEl.classList.add("hidden");
    document.body.style.overflow = "";
  }

  function bindAgendaAdviceClick(element, payload) {
    element.addEventListener("click", (e) => {
      if (e.target.closest(".agenda-actions")) return;
      show(payload);
    });
    element.addEventListener("keydown", (e) => {
      if (e.key === "Enter" || e.key === " ") {
        e.preventDefault();
        show(payload);
      }
    });
  }

  return { summarizeAdvice, show, close, bindAgendaAdviceClick, escapeHtml };
})();
