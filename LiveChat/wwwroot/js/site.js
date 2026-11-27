// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// 標記 data-auto-submit 的欄位，值改變時直接送出表單
document.addEventListener('change', (event) => {
    if (event.target.matches('[data-auto-submit]')) {
        event.target.form.requestSubmit();
    }
});
