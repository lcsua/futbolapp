(() => {
  const form = document.querySelector("[data-contact-form]");
  if (!form) return;

  const submit = form.querySelector("[data-contact-submit]");
  const errorEl = form.querySelector("[data-contact-error]");
  const expected = Number(form.getAttribute("data-captcha-sum"));
  const thanksUrl = form.getAttribute("data-thanks-url") || "/gracias";

  function showError(message) {
    if (!errorEl) return;
    errorEl.textContent = message;
    errorEl.hidden = false;
  }

  function hideError() {
    if (!errorEl) return;
    errorEl.hidden = true;
    errorEl.textContent = "";
  }

  function goThanks() {
    window.location.assign(thanksUrl);
  }

  function requiredValue(selector) {
    const el = form.querySelector(selector);
    return (el && el.value ? el.value : "").trim();
  }

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    hideError();

    const honeypot = form.querySelector('input[name="botcheck"]');
    const decoy = form.querySelector('input[name="website"]');
    if ((honeypot && honeypot.checked) || (decoy && decoy.value.trim())) {
      goThanks();
      return;
    }

    const captcha = form.querySelector('input[name="captcha"]');
    const answer = Number((captcha && captcha.value ? captcha.value : "").trim());
    if (!Number.isFinite(answer) || answer !== expected) {
      showError("Revisá la suma para confirmar que no sos un robot.");
      captcha?.focus();
      return;
    }

    const equipos = Number(requiredValue('input[name="Equipos"]'));
    const divisiones = Number(requiredValue('input[name="Divisiones"]'));
    if (
      !requiredValue('input[name="name"]') ||
      !requiredValue('input[name="email"]') ||
      !requiredValue('input[name="Localidad"]') ||
      !requiredValue('input[name="Liga"]') ||
      !Number.isFinite(equipos) || equipos < 1 ||
      !Number.isFinite(divisiones) || divisiones < 1
    ) {
      showError("Completá contacto, correo, localidad, liga, equipos y divisiones.");
      return;
    }

    if (submit) {
      submit.disabled = true;
      submit.textContent = "Enviando…";
    }

    const body = new FormData(form);
    body.delete("captcha");
    body.delete("website");

    try {
      const res = await fetch("https://api.web3forms.com/submit", {
        method: "POST",
        body,
        headers: { Accept: "application/json" }
      });
      const data = await res.json().catch(() => ({}));
      if (!res.ok || data.success === false) {
        throw new Error(data.message || "No se pudo enviar.");
      }
      goThanks();
    } catch (_) {
      showError("No se pudo enviar. Probá de nuevo en un rato.");
      if (submit) {
        submit.disabled = false;
        submit.textContent = "Enviar";
      }
    }
  });
})();
