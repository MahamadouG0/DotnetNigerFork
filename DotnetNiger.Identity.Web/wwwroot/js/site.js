document.addEventListener('DOMContentLoaded', function() {
    // Copy to clipboard
    document.querySelectorAll('.copy-btn').forEach(function(btn) {
        btn.addEventListener('click', function() {
            var code = this.closest('pre').querySelector('code').textContent;
            navigator.clipboard.writeText(code).then(function() {
                var original = btn.textContent;
                btn.textContent = 'Copi\u00e9 !';
                btn.classList.add('btn-success');
                setTimeout(function() {
                    btn.textContent = original;
                    btn.classList.remove('btn-success');
                }, 2000);
            });
        });
    });

    // Table search/filter
    document.querySelectorAll('[data-search]').forEach(function(input) {
        input.addEventListener('keyup', function() {
            var searchTerm = this.value.toLowerCase();
            var tableId = this.dataset.search;
            var table = document.getElementById(tableId);
            if (!table) return;
            var rows = table.querySelectorAll('tbody tr');
            rows.forEach(function(row) {
                var text = row.textContent.toLowerCase();
                row.style.display = text.includes(searchTerm) ? '' : 'none';
            });
        });
    });

    // Auto-refresh status
    if (document.querySelector('[data-auto-refresh]')) {
        var interval = parseInt(document.querySelector('[data-auto-refresh]').dataset.autoRefresh) || 30000;
        setTimeout(function() { location.reload(); }, interval);
    }

    // Dark mode toggle
    var darkModeToggle = document.getElementById('darkModeToggle');
    if (darkModeToggle) {
        var theme = localStorage.getItem('theme') || 'light';
        if (theme === 'dark') {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            darkModeToggle.checked = true;
        }
        darkModeToggle.addEventListener('change', function() {
            if (this.checked) {
                document.documentElement.setAttribute('data-bs-theme', 'dark');
                localStorage.setItem('theme', 'dark');
            } else {
                document.documentElement.removeAttribute('data-bs-theme');
                localStorage.setItem('theme', 'light');
            }
        });
    }

    // Password strength meter (improved)
    document.querySelectorAll('[data-password-strength]').forEach(function(input) {
        function updateMeter() {
            var meter = document.getElementById(input.dataset.passwordStrength);
            if (!meter) return;
            var val = input.value;
            var score = 0;
            if (val.length >= 8) score++;
            if (val.length >= 12) score++;
            if (val.match(/[a-z]/) && val.match(/[A-Z]/)) score++;
            if (val.match(/\d/)) score++;
            if (val.match(/[^a-zA-Z\d]/)) score++;
            if (val.length >= 16) score++;
            var levels = ['', 'Faible', 'Moyen', 'Fort', 'Tr\u00e8s fort', 'Excellent'];
            var colors = ['', '#dc3545', '#ffc107', '#0d6efd', '#198754', '#146c43'];
            var pct = Math.min(score * 20, 100);
            meter.style.width = pct + '%';
            meter.textContent = levels[score] || '';
            meter.className = 'progress-bar';
            if (score > 0) meter.style.backgroundColor = colors[score];
        }
        input.addEventListener('input', updateMeter);
        updateMeter();
    });

    // Modal delete confirmation
    document.querySelectorAll('[data-modal-delete]').forEach(function(btn) {
        btn.addEventListener('click', function(e) {
            e.preventDefault();
            var form = document.getElementById(this.dataset.modalDelete);
            var modalEl = document.getElementById('deleteConfirmModal');
            if (!modalEl || !form) return;
            var label = modalEl.querySelector('.modal-body p');
            if (label) label.textContent = this.dataset.modalLabel || 'Confirmer la suppression ?';
            var confirmBtn = modalEl.querySelector('.btn-danger');
            if (confirmBtn) {
                var newBtn = confirmBtn.cloneNode(true);
                confirmBtn.parentNode.replaceChild(newBtn, confirmBtn);
                newBtn.addEventListener('click', function() { form.submit(); });
            }
            var modal = new bootstrap.Modal(modalEl);
            modal.show();
        });
    });

    // Pagination: page size selector auto-submit
    document.querySelectorAll('[data-pagesize]').forEach(function(select) {
        select.addEventListener('change', function() {
            var url = new URL(window.location.href);
            url.searchParams.set('pageSize', this.value);
            url.searchParams.set('page', '1');
            window.location.href = url.toString();
        });
    });
});
