// Search UI
(function() {
    let miniSearch = null;
    let isSearchOpen = false;

    // Load search index
    async function loadSearchIndex() {
        if (miniSearch) return;
        try {
            const response = await fetch('/search.json');
            const data = await response.json();
            miniSearch = new MiniSearch({
                fields: ['title', 'content'], // fields to index for full-text search
                storeFields: ['title', 'id'], // fields to return with search results
                searchOptions: {
                    boost: { title: 2 },
                    fuzzy: 0.2
                }
            });
            miniSearch.addAll(data);
        } catch (error) {
            console.error('Failed to load search index', error);
        }
    }

    function createSearchModal() {
        let selectedIndex = -1;
        const modal = document.createElement('div');
        modal.id = 'search-modal';
        modal.className = 'fixed inset-0 z-50 flex items-start justify-center pt-24 hidden';
        modal.innerHTML = `
            <div class="fixed inset-0 bg-gray-900/50 backdrop-blur-sm" id="search-backdrop"></div>
            <div class="relative w-full max-w-lg bg-white dark:bg-gray-800 rounded-xl shadow-2xl ring-1 ring-black/5 overflow-hidden">
                <div class="flex items-center px-4 py-3 border-b border-gray-200 dark:border-gray-700">
                    <i class="fi fi-rr-search text-gray-500 mr-3"></i>
                    <input type="text" id="search-input" class="w-full bg-transparent border-none focus:ring-0 text-gray-900 dark:text-gray-100 placeholder-gray-500" placeholder="Search documentation..." autocomplete="off">
                    <button id="search-close" class="text-xs bg-gray-100 dark:bg-gray-700 px-2 py-1 rounded text-gray-500">ESC</button>
                </div>
                <div id="search-results" class="max-h-96 overflow-y-auto py-2"></div>
                <div id="search-footer" class="px-4 py-2 border-t border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50 text-xs text-gray-500 flex justify-between">
                    <span>Search by MiniSearch</span>
                </div>
            </div>
        `;
        document.body.appendChild(modal);

        // Event listeners
        const input = modal.querySelector('#search-input');
        const backdrop = modal.querySelector('#search-backdrop');
        const closeBtn = modal.querySelector('#search-close');
        const resultsContainer = modal.querySelector('#search-results');

        backdrop.addEventListener('click', closeSearch);
        closeBtn.addEventListener('click', closeSearch);

        input.addEventListener('input', (e) => {
            selectedIndex = -1;
            const query = e.target.value;
            if (!query) {
                resultsContainer.innerHTML = '';
                return;
            }
            if (miniSearch) {
                const results = miniSearch.search(query);
                renderResults(results, resultsContainer);
            }
        });

        function updateSelection() {
            const items = resultsContainer.querySelectorAll('a');
            items.forEach((item, index) => {
                if (index === selectedIndex) {
                    item.classList.add('bg-gray-100', 'dark:bg-gray-700/50');
                    item.scrollIntoView({ block: 'nearest' });
                } else {
                    item.classList.remove('bg-gray-100', 'dark:bg-gray-700/50');
                }
            });
        }

        input.addEventListener('keydown', (e) => {
            const items = resultsContainer.querySelectorAll('a');
            if (items.length === 0) return;

            if (e.key === 'ArrowDown') {
                e.preventDefault();
                selectedIndex = (selectedIndex + 1) % items.length;
                updateSelection();
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                selectedIndex = (selectedIndex <= 0) ? items.length - 1 : selectedIndex - 1;
                updateSelection();
            } else if (e.key === 'Enter') {
                e.preventDefault();
                if (selectedIndex >= 0 && selectedIndex < items.length) {
                    items[selectedIndex].click();
                }
            }
        });
    }

    function renderResults(results, container) {
        if (results.length === 0) {
            container.innerHTML = '<div class="px-4 py-3 text-sm text-gray-500">No results found.</div>';
            return;
        }

        container.innerHTML = results.map((result, index) => {
            // Ensure path is absolute and uses forward slashes
            let href = result.id.replace(/\\/g, '/');
            if (!href.startsWith('/')) href = '/' + href;

            return `
            <a href="${href}" class="block px-4 py-3 hover:bg-gray-100 dark:hover:bg-gray-700/50 group">
                <div class="text-sm font-medium text-gray-900 dark:text-gray-100 group-hover:text-blue-600 dark:group-hover:text-blue-400">
                    ${result.title}
                </div>
                <div class="text-xs text-gray-500 truncate">
                    ${result.id}
                </div>
            </a>
        `}).join('');
    }

    function openSearch() {
        if (!document.getElementById('search-modal')) {
            createSearchModal();
        }
        const modal = document.getElementById('search-modal');
        modal.classList.remove('hidden');
        document.getElementById('search-input').focus();
        isSearchOpen = true;
        loadSearchIndex();
    }

    function closeSearch() {
        const modal = document.getElementById('search-modal');
        if (modal) {
            modal.classList.add('hidden');
        }
        isSearchOpen = false;
    }

    // Global keyboard shortcut (Ctrl+K or Cmd+K)
    document.addEventListener('keydown', (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            if (isSearchOpen) closeSearch();
            else openSearch();
        }
        if (e.key === 'Escape' && isSearchOpen) {
            closeSearch();
        }
    });

    // Expose openSearch to button clicks
    window.openSearch = openSearch;

})();
