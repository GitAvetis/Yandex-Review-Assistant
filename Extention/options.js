const DEFAULT_SERVER_URL = 'http://localhost:5005';

document.addEventListener('DOMContentLoaded', () => {
  const input = document.getElementById('serverUrl');
  const status = document.getElementById('status');
  const saveBtn = document.getElementById('saveBtn');

  chrome.storage.local.get(['serverUrl'], (result) => {
    input.value = result.serverUrl || DEFAULT_SERVER_URL;
  });

  saveBtn.addEventListener('click', () => {
    const raw = input.value.trim();
    const normalized = raw.replace(/\/+$/, ''); // убираем завершающий "/"

    const validation = validateServerUrl(normalized);
    if (!validation.ok) {
      showStatus(validation.message, 'err');
      return;
    }

    chrome.storage.local.set({ serverUrl: normalized }, () => {
      input.value = normalized;
      showStatus('Сохранено!', 'ok');
    });
  });

  function showStatus(text, kind) {
    status.textContent = text;
    status.className = kind;
    setTimeout(() => {
      status.textContent = '';
      status.className = '';
    }, 2500);
  }

  function validateServerUrl(value) {
    if (!value) {
      return { ok: false, message: 'Укажите адрес сервера.' };
    }

    let url;
    try {
      url = new URL(value);
    } catch {
      return { ok: false, message: 'Некорректный URL. Пример: http://localhost:5005' };
    }

    if (url.protocol !== 'http:') {
      return { ok: false, message: 'Сервер работает только по http:// (локально).' };
    }

    if (url.hostname !== 'localhost' && url.hostname !== '127.0.0.1') {
      return { ok: false, message: 'Разрешён только адрес localhost или 127.0.0.1.' };
    }

    return { ok: true };
  }
});
