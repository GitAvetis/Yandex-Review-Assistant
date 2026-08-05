(function () {
  'use strict';

  const TEXTAREA_SELECTOR = '.ya-business-ui-textarea__control';
  const REVIEW_SELECTOR = '.Review';
  const REVIEW_TEXT_SELECTOR = '.Review-Text';
  const DEFAULT_SERVER_URL = 'http://localhost:5005';

  let activeTextarea = null;
  let currentGeneratedText = '';

  // ---------- Адрес сервера (настраивается на странице options.html) ----------

  function getServerUrl() {
    return new Promise((resolve) => {
      chrome.storage.local.get(['serverUrl'], (result) => {
        const raw = (result.serverUrl || DEFAULT_SERVER_URL).trim();
        resolve(raw.replace(/\/+$/, ''));
      });
    });
  }

  // ---------- Кнопка "Сгенерировать ответ" ----------

  function createGenerateButton() {
    const btn = document.createElement('button');
    btn.id = 'ai-response-generate-btn';
    btn.textContent = '✨ Сгенерировать ответ';
    Object.assign(btn.style, {
      position: 'fixed',
      zIndex: '999999',
      padding: '6px 14px',
      background: '#ffcc00',
      color: '#000',
      border: 'none',
      borderRadius: '8px',
      cursor: 'pointer',
      display: 'none',
      fontSize: '13px',
      fontFamily: 'Arial, sans-serif',
      boxShadow: '0 2px 8px rgba(0,0,0,0.25)',
    });
    document.body.appendChild(btn);
    return btn;
  }

  // ---------- Превью-панель ----------

  function createPreviewPanel() {
    const panel = document.createElement('div');
    panel.id = 'ai-response-preview-panel';
    Object.assign(panel.style, {
      position: 'fixed',
      zIndex: '999999',
      display: 'none',
      background: '#fff',
      border: '1px solid #ddd',
      borderRadius: '10px',
      boxShadow: '0 4px 16px rgba(0,0,0,0.2)',
      padding: '12px',
      width: '360px',
      fontFamily: 'Arial, sans-serif',
    });

    const label = document.createElement('div');
    label.textContent = 'Сгенерированный ответ (можно отредактировать):';
    label.style.fontSize = '12px';
    label.style.color = '#666';
    label.style.marginBottom = '6px';

    const textField = document.createElement('textarea');
    textField.id = 'ai-preview-textfield';
    Object.assign(textField.style, {
      width: '100%',
      minHeight: '80px',
      fontSize: '13px',
      padding: '6px',
      border: '1px solid #ccc',
      borderRadius: '6px',
      resize: 'vertical',
      fontFamily: 'inherit',
      boxSizing: 'border-box',
    });

    const buttonRow = document.createElement('div');
    Object.assign(buttonRow.style, {
      display: 'flex',
      gap: '8px',
      marginTop: '10px',
    });

    const insertBtn = makeSmallButton('Вставить', '#4caf50', '#fff');
    insertBtn.id = 'ai-preview-insert-btn';

    const regenerateBtn = makeSmallButton('Сгенерировать заново', '#eeeeee', '#000');
    regenerateBtn.id = 'ai-preview-regenerate-btn';

    const cancelBtn = makeSmallButton('Отмена', 'transparent', '#999');
    cancelBtn.id = 'ai-preview-cancel-btn';
    cancelBtn.style.marginLeft = 'auto';

    buttonRow.appendChild(insertBtn);
    buttonRow.appendChild(regenerateBtn);
    buttonRow.appendChild(cancelBtn);

    panel.appendChild(label);
    panel.appendChild(textField);
    panel.appendChild(buttonRow);
    document.body.appendChild(panel);

    return panel;
  }

  function makeSmallButton(text, bg, color) {
    const btn = document.createElement('button');
    btn.textContent = text;
    Object.assign(btn.style, {
      padding: '5px 10px',
      background: bg,
      color: color,
      border: bg === 'transparent' ? 'none' : '1px solid transparent',
      borderRadius: '6px',
      cursor: 'pointer',
      fontSize: '12px',
    });
    return btn;
  }

  const generateButton = createGenerateButton();
  const previewPanel = createPreviewPanel();
  const previewTextField = previewPanel.querySelector('#ai-preview-textfield');
  const insertBtn = previewPanel.querySelector('#ai-preview-insert-btn');
  const regenerateBtn = previewPanel.querySelector('#ai-preview-regenerate-btn');
  const cancelBtn = previewPanel.querySelector('#ai-preview-cancel-btn');

  // ---------- Позиционирование ----------

  function positionElementNear(el, textarea, offsetTop = -40) {
    const rect = textarea.getBoundingClientRect();
    el.style.top = `${rect.top + offsetTop}px`;
    el.style.left = `${rect.left}px`;
  }

  function showGenerateButton(textarea) {
    positionElementNear(generateButton, textarea, -40);
    generateButton.style.display = 'block';
  }

  function hideGenerateButton() {
    generateButton.style.display = 'none';
  }

  function showPreviewPanel(textarea) {
    positionElementNear(previewPanel, textarea, -180);
    previewPanel.style.display = 'block';
  }

  function hidePreviewPanel() {
    previewPanel.style.display = 'none';
  }

  function isTextareaEmpty(textarea) {
    return textarea.value.trim().length === 0;
  }

  // ---------- Фокус на textarea Яндекса ----------

  document.addEventListener('focusin', (e) => {
    const textarea = e.target.closest(TEXTAREA_SELECTOR);
    if (!textarea) return;

    if (isTextareaEmpty(textarea)) {
      activeTextarea = textarea;
      showGenerateButton(textarea);
    } else {
      hideGenerateButton();
      activeTextarea = null;
    }
  });

  document.addEventListener('focusout', () => {
    setTimeout(() => {
      const activeEl = document.activeElement;
      const focusOnOurUI =
        activeEl &&
        (activeEl.closest(TEXTAREA_SELECTOR) ||
          activeEl === generateButton ||
          activeEl.closest('#ai-response-preview-panel'));

      if (!focusOnOurUI) {
        hideGenerateButton();
        hidePreviewPanel();
        activeTextarea = null;
      }
    }, 150);
  });

  [generateButton, insertBtn, regenerateBtn, cancelBtn].forEach((btn) => {
    btn.addEventListener('mousedown', (e) => e.preventDefault());
  });

  // ---------- Скролл/ресайз ----------

  window.addEventListener(
    'scroll',
    () => {
      if (!activeTextarea) return;
      if (generateButton.style.display !== 'none') {
        positionElementNear(generateButton, activeTextarea, -40);
      }
      if (previewPanel.style.display !== 'none') {
        positionElementNear(previewPanel, activeTextarea, -180);
      }
    },
    true
  );

  window.addEventListener('resize', () => {
    if (!activeTextarea) return;
    if (generateButton.style.display !== 'none') {
      positionElementNear(generateButton, activeTextarea, -40);
    }
    if (previewPanel.style.display !== 'none') {
      positionElementNear(previewPanel, activeTextarea, -180);
    }
  });

  // ---------- Клик "Сгенерировать ответ" ----------

  generateButton.addEventListener('click', async () => {
    if (!activeTextarea) return;
    hideGenerateButton();
    showPreviewPanel(activeTextarea);
    await runGeneration(activeTextarea);
  });

  regenerateBtn.addEventListener('click', async () => {
    if (!activeTextarea) return;
    await runGeneration(activeTextarea);
  });

  insertBtn.addEventListener('click', () => {
    if (!activeTextarea) return;
    const finalText = previewTextField.value;
    insertText(activeTextarea, finalText);
    hidePreviewPanel();
  });

  cancelBtn.addEventListener('click', () => {
    hidePreviewPanel();
    if (activeTextarea && isTextareaEmpty(activeTextarea)) {
      showGenerateButton(activeTextarea);
    }
  });

  // ---------- Логика генерации ----------

  async function runGeneration(textarea) {
    const review = textarea.closest(REVIEW_SELECTOR);
    const reviewTextEl = review ? review.querySelector(REVIEW_TEXT_SELECTOR) : null;
    const reviewText = reviewTextEl ? reviewTextEl.textContent.trim() : '';

    if (!reviewText) {
      previewTextField.value = 'Не удалось найти текст отзыва на странице.';
      return;
    }

    setPreviewLoadingState(true);

    try {
      const generatedText = await generateReplyFromServer(reviewText);
      currentGeneratedText = generatedText;
      previewTextField.value = generatedText;
    } catch (err) {
      console.error('Ошибка генерации:', err);
      const serverUrl = await getServerUrl();
      previewTextField.value =
        `Не удалось получить ответ от сервера (${serverUrl}). ` +
        'Убедитесь, что приложение GigaChatReplyServer запущено (иконка в трее), ' +
        'а адрес в настройках расширения указан верно.';
    } finally {
      setPreviewLoadingState(false);
    }
  }

  function setPreviewLoadingState(isLoading) {
    insertBtn.disabled = isLoading;
    regenerateBtn.disabled = isLoading;
    regenerateBtn.textContent = isLoading ? 'Генерирую...' : 'Сгенерировать заново';
    if (isLoading) previewTextField.value = 'Генерирую ответ...';
  }

  // ---------- Реальный вызов GigaChat-сервера ----------

  async function generateReplyFromServer(reviewText) {
    const serverUrl = await getServerUrl();

    const response = await fetch(`${serverUrl}/reply`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ text: reviewText }),
    });

    if (!response.ok) {
      throw new Error(`Сервер вернул код ${response.status}`);
    }

    const data = await response.json();
    if (!data || typeof data.reply !== 'string') {
      throw new Error('Сервер вернул ответ в неожиданном формате.');
    }

    return data.reply;
  }

  // ---------- Вставка текста ----------

  function insertText(textarea, text) {
    const nativeInputValueSetter = Object.getOwnPropertyDescriptor(
      window.HTMLTextAreaElement.prototype,
      'value'
    ).set;
    nativeInputValueSetter.call(textarea, text);
    textarea.dispatchEvent(new Event('input', { bubbles: true }));
    textarea.dispatchEvent(new Event('change', { bubbles: true }));
  }

  console.log('[Review Assistant] Content script загружен и активен');
})();
