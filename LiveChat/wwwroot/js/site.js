// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// 標記 data-auto-submit 的欄位，值改變時直接送出表單
document.addEventListener('change', (event) => {
    if (event.target.matches('[data-auto-submit]')) {
        event.target.form.requestSubmit();
    }
});

// 標記 data-confirm 的表單，送出前先確認
document.addEventListener('submit', (event) => {
    const message = event.target.dataset.confirm;
    if (message && !window.confirm(message)) {
        event.preventDefault();
    }
});

// 標記 data-scroll-bottom 的區塊，載入時捲到最下方（最新訊息）
document.querySelectorAll('[data-scroll-bottom]').forEach((element) => {
    element.scrollTop = element.scrollHeight;
});
