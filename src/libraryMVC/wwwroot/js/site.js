// Global helper function for summary text expansion ("Read more / Read less")
function toggleSummaryText(btn) {
  var wrapper = btn.closest('.summary-wrapper');
  if (!wrapper) return;
  var content = wrapper.querySelector('.summary-content');
  if (!content) return;

  if (content.classList.contains('line-clamp-3')) {
    content.classList.remove('line-clamp-3');
    btn.innerHTML = '<i class="bi bi-chevron-up me-1"></i>Read less';
  } else {
    content.classList.add('line-clamp-3');
    btn.innerHTML = '<i class="bi bi-chevron-down me-1"></i>Read more';
  }
}

document.querySelectorAll('.toggle-password').forEach(function (button) {
  button.addEventListener('click', function () {
    var passwordInput = document.getElementById(this.dataset.passwordTarget);
    var icon = this.querySelector('i');
    var isPassword = passwordInput.type === 'password';

    passwordInput.type = isPassword ? 'text' : 'password';
    icon.className = isPassword ? 'bi bi-eye-slash' : 'bi bi-eye';
    this.setAttribute('aria-label', isPassword ? 'Hide password' : 'Show password');
    this.setAttribute('title', isPassword ? 'Hide password' : 'Show password');
  });
});
