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

document.querySelectorAll('[data-infinite-list]').forEach(function (list) {
  var rows = list.querySelector('tbody[data-load-more-url]');
  var status = list.querySelector('[data-infinite-status]');
  var button = list.querySelector('[data-load-more]');
  var filterForm = document.getElementById(rows && rows.dataset.filterFormId);
  if (!rows || !status || !button || !filterForm) return;

  var loading = false;
  var autoLoadPaused = false;
  var finished = rows.dataset.hasMore !== 'true';
  button.hidden = finished;

  function showLoading() {
    status.replaceChildren();
    var spinner = document.createElement('span');
    spinner.className = 'spinner-border spinner-border-sm me-2';
    spinner.setAttribute('role', 'status');
    spinner.setAttribute('aria-hidden', 'true');
    status.appendChild(spinner);

    var message = document.createElement('span');
    message.textContent = 'Loading more records…';
    status.appendChild(message);
  }

  async function loadNextPage() {
    if (loading || finished) return;
    loading = true;
    list.setAttribute('aria-busy', 'true');
    button.disabled = true;
    button.textContent = 'Loading…';
    showLoading();

    try {
      var query = new URLSearchParams(new FormData(filterForm));
      query.set('page', rows.dataset.nextPage);
      var url = new URL(rows.dataset.loadMoreUrl, window.location.origin);
      url.search = query.toString();
      var response = await fetch(url, {
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        credentials: 'same-origin'
      });
      if (!response.ok) {
        throw new Error('The next page request failed with status ' + response.status);
      }

      var template = document.createElement('template');
      template.innerHTML = await response.text();
      var metadata = template.content.querySelector('[data-list-page-metadata]');
      if (!metadata) {
        throw new Error('The next page response did not contain pagination metadata.');
      }

      var hasMore = metadata.dataset.hasMore === 'true';
      metadata.remove();
      rows.appendChild(template.content);
      rows.dataset.nextPage = String(Number(rows.dataset.nextPage) + 1);
      rows.dataset.hasMore = String(hasMore);
      finished = !hasMore;
      status.replaceChildren();
      button.hidden = finished;
      button.textContent = 'Load more';
    } catch (error) {
      console.error('Unable to load more list records.', error);
      autoLoadPaused = true;
      status.textContent = 'Could not load more records. Try again.';
      button.textContent = 'Retry';
      button.hidden = false;
    } finally {
      loading = false;
      list.removeAttribute('aria-busy');
      button.disabled = false;
    }
  }

  button.addEventListener('click', function () {
    autoLoadPaused = false;
    loadNextPage();
  });
  if ('IntersectionObserver' in window && !finished) {
    var observer = new IntersectionObserver(function (entries) {
      if (!autoLoadPaused && entries.some(function (entry) { return entry.isIntersecting; })) {
        loadNextPage();
      }
    }, { rootMargin: '250px 0px' });
    observer.observe(status);
  }
});
