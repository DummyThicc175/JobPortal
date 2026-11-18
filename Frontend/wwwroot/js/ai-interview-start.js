(() => {
    "use strict";
    const bieuMau = document.querySelector("[data-ai-start]");
    if (!bieuMau) return;
    const luaChon = bieuMau.querySelector("select");
    const maHoSo = new URLSearchParams(location.search).get("applicationId");
    if ([...luaChon.options].some(muc => muc.value === maHoSo)) luaChon.value = maHoSo;
    bieuMau.addEventListener("submit", () => {
        bieuMau.querySelector("button").disabled = true;
        bieuMau.querySelector("[data-ai-start-status]").textContent = "AI đang chuẩn bị câu hỏi đầu tiên. Bạn đợi một chút nhé…";
    });
})();
