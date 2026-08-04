document.addEventListener('DOMContentLoaded', () => {
  const input = document.getElementById('apiKey');
  const status = document.getElementById('status');

  chrome.storage.local.get(['openrouterApiKey'], (result) => {
    if (result.openrouterApiKey) {
      input.value = result.openrouterApiKey;
    }
  });

  document.getElementById('saveBtn').addEventListener('click', () => {
    const key = input.value.trim();
    chrome.storage.local.set({ openrouterApiKey: key }, () => {
      status.textContent = 'Сохранено!';
      setTimeout(() => (status.textContent = ''), 2000);
    });
  });
});