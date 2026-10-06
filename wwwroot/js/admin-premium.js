(() => {
    const sidebar = document.getElementById('sidebar');
    const content = document.getElementById('content');
    const topbar = document.getElementById('topbar');
    const overlay = document.getElementById('overlay');
    const mobileButton = document.getElementById('mobileBtn');
    const desktopButton = document.getElementById('toggleBtn');
    const media = window.matchMedia('(max-width: 991.98px)');
    let returnFocus;
    function closeMenu(restoreFocus = true) {
        sidebar.classList.remove('mobile-show');
        overlay.classList.remove('show');
        mobileButton.setAttribute('aria-expanded', 'false');
        document.body.style.overflow = '';
        content.inert = false;
        topbar.inert = false;
        sidebar.inert = media.matches;
        if (restoreFocus && returnFocus) returnFocus.focus();
    }
    mobileButton.addEventListener('click', () => {
        returnFocus = document.activeElement;
        sidebar.inert = false;
        sidebar.classList.add('mobile-show');
        overlay.classList.add('show');
        mobileButton.setAttribute('aria-expanded', 'true');
        document.body.style.overflow = 'hidden';
        sidebar.querySelector('.admin-mobile-close').focus();
        content.inert = true;
        topbar.inert = true;
    });
    desktopButton.addEventListener('click', () => {
        const collapsed = sidebar.classList.toggle('collapsed');
        content.classList.toggle('full', collapsed);
        topbar.classList.toggle('full', collapsed);
        desktopButton.setAttribute('aria-expanded', String(!collapsed));
    });
    overlay.addEventListener('click', () => closeMenu());
    sidebar.querySelector('.admin-mobile-close').addEventListener('click', () => closeMenu());
    document.addEventListener('keydown', event => {
        if (!sidebar.classList.contains('mobile-show')) return;
        if (event.key === 'Escape') closeMenu();
        if (event.key === 'Tab') {
            const nodes = [...sidebar.querySelectorAll('a[href],button')].filter(el => el.getClientRects().length);
            const first = nodes[0], last = nodes[nodes.length - 1];
            if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
            else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
        }
    });
    media.addEventListener('change', () => closeMenu(false));
    sidebar.querySelectorAll('.nav-link').forEach(link => {
        link.title = link.textContent.trim();
        if (link.classList.contains('active')) link.setAttribute('aria-current', 'page');
    });
    closeMenu(false);
})();
