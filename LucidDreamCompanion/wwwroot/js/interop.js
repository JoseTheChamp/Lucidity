window.ldc = {
  focus: function (selector) {
    const el = document.querySelector(selector);
    if (el) el.focus();
  },
  autosize: function (textareaEl) {
    if (!textareaEl) return;
    textareaEl.style.height = 'auto';
    textareaEl.style.height = (textareaEl.scrollHeight + 2) + 'px';
  }
};
