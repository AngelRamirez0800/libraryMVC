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
