const API = '';
let token = localStorage.getItem('token');
let currentUser = null;
let allReadingList = [];
let currentPage = 1;
const booksPerPage = 10;
let allBooks = [];
let previousPage = 'books';

function showMessage(text, type) {
    if (!type) type = 'success';
    var msg = document.getElementById('message');
    msg.textContent = text;
    msg.className = 'message ' + type;
    msg.style.display = 'block';
    setTimeout(function () { msg.style.display = 'none'; }, 3000);
}

function showPage(name) {
    previousPage = localStorage.getItem('lastPage') || 'books';
    document.querySelectorAll('.page').forEach(function (p) { p.style.display = 'none'; });
    var pageEl = document.getElementById('page-' + name);
    if (!pageEl) return;
    pageEl.style.display = 'block';
    localStorage.setItem('lastPage', name);
    var heroSection = document.getElementById('heroSection');
    if (heroSection) heroSection.style.display = name === 'books' ? 'block' : 'none';
    if (name === 'books') { loadBooks(); loadCategoryFilter(); }
    if (name === 'profile') loadProfile();
    if (name === 'recommendations') loadRecommendations();
    if (name === 'forum') loadForum();
    if (name === 'favorites') loadFavorites();
    if (name === 'newBookNotifications') loadNewBookNotifications();
    if (name === 'readingList') loadReadingList();
    if (name === 'admin') { showAdminTab('books'); loadAdminBooks(); }
    history.pushState({ page: name }, '', window.location.pathname);
}

function updateNav() {
    var loggedIn = !!token;
    var isAdmin = currentUser && currentUser.role === 'admin';
    document.getElementById('loginLink').style.display = loggedIn ? 'none' : 'inline';
    document.getElementById('registerLink').style.display = loggedIn ? 'none' : 'inline';
    document.getElementById('logoutLink').style.display = loggedIn ? 'inline' : 'none';
    document.getElementById('profileLink').style.display = loggedIn ? 'inline' : 'none';
    document.getElementById('recommendationsLink').style.display = loggedIn ? 'inline' : 'none';
    document.getElementById('adminLink').style.display = isAdmin ? 'inline' : 'none';
    document.getElementById('readingListLink').style.display = loggedIn ? 'inline' : 'none';
    document.getElementById('forumLink').style.display = loggedIn ? 'inline' : 'none';
    document.getElementById('favoritesLink').style.display = loggedIn ? 'inline' : 'none';
    document.getElementById('newBookNotifLink').style.display = loggedIn ? 'inline' : 'none';
}

async function apiFetch(url, method, body) {
    if (!method) method = 'GET';
    var headers = { 'Content-Type': 'application/json' };
    if (token) headers['Authorization'] = 'Bearer ' + token;
    var options = { method: method, headers: headers };
    if (body) options.body = JSON.stringify(body);
    var res = await fetch(API + url, options);
    var data = await res.json();
    return { ok: res.ok, data: data };
}

async function register() {
    var name = document.getElementById('regName').value.trim();
    var email = document.getElementById('regEmail').value.trim();
    var password = document.getElementById('regPassword').value;
    var confirmPassword = document.getElementById('regConfirmPassword').value;
    if (!name || !email || !password || !confirmPassword) { showMessage('Моля попълнете всички полета.', 'error'); return; }
    if (password !== confirmPassword) { showMessage('Паролите не съвпадат.', 'error'); return; }
    if (password.length < 8) { showMessage('Паролата трябва да е поне 8 символа.', 'error'); return; }
    if (!/[A-Z]/.test(password)) { showMessage('Паролата трябва да съдържа поне една главна буква.', 'error'); return; }
    if (!/[a-z]/.test(password)) { showMessage('Паролата трябва да съдържа поне една малка буква.', 'error'); return; }
    if (!/[0-9]/.test(password)) { showMessage('Паролата трябва да съдържа поне една цифра.', 'error'); return; }
    if (!/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password)) {
        showMessage('Паролата трябва да съдържа поне един специален символ.', 'error');
        return;
    } var result = await apiFetch('/api/auth/register', 'POST', { name: name, email: email, password: password });
    if (result.ok) {
        showMessage('Регистрацията е успешна!');
        showPage('login');
    } else showMessage(result.data.message || 'Грешка.', 'error');
}

async function login() {
    var email = document.getElementById('loginEmail').value.trim();
    var password = document.getElementById('loginPassword').value;
    if (!email || !password) { showMessage('Моля попълнете всички полета.', 'error'); return; }
    var result = await apiFetch('/api/auth/login', 'POST', { email: email, password: password });
    if (result.ok) {
        token = result.data.token;
        currentUser = { name: result.data.name, email: result.data.email, role: result.data.role };
        localStorage.setItem('token', token);
        localStorage.setItem('user', JSON.stringify(currentUser));
        updateNav();
        showMessage('Добре дошъл, ' + result.data.name + '!');
        showPage('books');
    } else showMessage(result.data.message || 'Грешка при вход.', 'error');
}

function logout() {
    token = null; currentUser = null;
    localStorage.removeItem('token'); localStorage.removeItem('user');
    updateNav(); showPage('books');
    showMessage('Излязохте успешно.');
}

async function loadBooks() {
    var search = document.getElementById('searchInput') ? document.getElementById('searchInput').value : '';
    var categoryId = document.getElementById('categoryFilter') ? document.getElementById('categoryFilter').value : '';
    var authorId = document.getElementById('authorFilter') ? document.getElementById('authorFilter').value : '';
    var url = '/api/books?';
    if (search) url += 'search=' + encodeURIComponent(search) + '&';
    if (categoryId) url += 'categoryId=' + categoryId + '&';
    if (authorId) url += 'authorId=' + authorId;
    var result = await apiFetch(url);
    if (!result.ok) return;
    allBooks = result.data;
    currentPage = 1;
    renderBooks();
}

function renderBooks() {
    var grid = document.getElementById('booksList');
    if (allBooks.length === 0) {
        grid.innerHTML = '<p>Няма намерени книги.</p>';
        document.getElementById('pagination').innerHTML = '';
        return;
    }
    var totalPages = Math.ceil(allBooks.length / booksPerPage);
    var start = (currentPage - 1) * booksPerPage;
    var end = start + booksPerPage;
    var paginated = allBooks.slice(start, end);
    var html = '';
    paginated.forEach(function (book) {
        var btn = '';
        if (token) {
            btn = '<button onclick="addToReadingList(' + book.id + ',\'want_to_read\')">Добави в дневника</button>';
        } else {
            btn = '<button disabled>Влезте в акаунта си</button>';
        }
        html += '<div class="book-card">' +
            '<img src="' + (book.coverUrl || '') + '" alt="' + book.title + '" onerror="this.src=\'https://placehold.co/200x280/1a1a2e/e0c97f?text=Knjiga\'" onclick="showBookDetail(' + book.id + ')" style="cursor:pointer;">' +
            '<div class="book-card-body">' +
            '<h3 onclick="showBookDetail(' + book.id + ')" style="cursor:pointer;">' + book.title + '</h3>' +
            '<p>' + (book.authorName || 'Неизвестен автор') + '</p>' +
            '<p>' + (book.categoryName || 'Без категория') + '</p>' +
            '<p>' + (book.publishedYear || '') + '</p>' +
            btn + '</div></div>';
    });
    grid.innerHTML = html;
    var paginationHtml = '';
    if (totalPages > 1) {
        paginationHtml += '<div class="pagination">';
        if (currentPage > 1) paginationHtml += '<button onclick="changePage(' + (currentPage - 1) + ')">← Назад</button>';
        for (var i = 1; i <= totalPages; i++) {
            paginationHtml += '<button onclick="changePage(' + i + ')" class="' + (i === currentPage ? 'active' : '') + '">' + i + '</button>';
        }
        if (currentPage < totalPages) paginationHtml += '<button onclick="changePage(' + (currentPage + 1) + ')">Напред →</button>';
        paginationHtml += '</div>';
    }
    document.getElementById('pagination').innerHTML = paginationHtml;
}

function changePage(page) {
    currentPage = page;
    renderBooks();
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

async function showBookDetail(bookId) {
    var r1 = await apiFetch('/api/books/' + bookId);
    if (!r1.ok) return;
    var data = r1.data;
    var r2 = await apiFetch('/api/reviews/book/' + bookId);
    var reviews = r2.ok ? r2.data : [];
    previousPage = localStorage.getItem('lastPage') || 'books';
    document.querySelectorAll('.page').forEach(function (p) { p.style.display = 'none'; });
    document.getElementById('heroSection').style.display = 'none';
    document.getElementById('page-bookDetail').style.display = 'block';
    localStorage.setItem('lastPage', 'bookDetail');
    if (token) {
        var favResult = await apiFetch('/api/favorites/check/' + data.id);
        var isFav = favResult.ok && favResult.data.isFavorite;
        var favBtn = isFav
            ? '<button class="detail-btn" style="background:linear-gradient(135deg,#e74c3c,#c0392b);" onclick="toggleFavorite(' + data.id + ', true)">♥ Премахни от любими</button>'
            : '<button class="detail-btn" style="background:linear-gradient(135deg,#e74c3c,#c0392b);" onclick="toggleFavorite(' + data.id + ', false)">♡ Добави в любими</button>';
        buttonsHtml = '<div style="display:flex; gap:10px; flex-wrap:wrap; margin-top:16px;">' +
            favBtn +
            '<button class="detail-btn" style="background:linear-gradient(135deg,#27ae60,#219a52);" onclick="addToReadingList(' + data.id + ',\'want_to_read\')">Искам да прочета</button>' +
            '<button class="detail-btn" style="background:linear-gradient(135deg,#e67e22,#d35400);" onclick="addToReadingList(' + data.id + ',\'reading\')">Чета сега</button>' +
            '<button class="detail-btn" style="background:linear-gradient(135deg,#8e44ad,#7d3c98);" onclick="addToReadingList(' + data.id + ',\'read\')">Прочетена</button>' +
            '</div>';
    } else {
        buttonsHtml = '<button class="detail-btn" disabled>Влезте за да продължите</button>';
    }
    var reviewsHtml = '';
    if (reviews.length > 0) {
        reviews.forEach(function (r) {
            var stars = '';
            for (var i = 0; i < r.rating; i++) stars += '★';
            for (var j = r.rating; j < 5; j++) stars += '☆';
            reviewsHtml += '<div class="review-card"><div class="review-header">' +
                '<span class="review-author">' + r.userName + '</span>' +
                '<span class="review-rating">' + stars + '</span>' +
                '<span class="review-date">' + new Date(r.createdAt).toLocaleDateString('bg-BG') + '</span>' +
                '</div><p>' + (r.comment || '') + '</p></div>';
        });
    } else {
        reviewsHtml = '<p style="color:#777;">Няма рецензии още.</p>';
    }
    var starInputs = '';
    for (var i = 1; i <= 5; i++) {
        starInputs += '<span onclick="setRating(' + i + ')" style="font-size:28px; cursor:pointer; color:#ddd;">★</span>';
    }
    var reviewFormHtml = token ? '<div class="review-form"><h4>Напиши рецензия</h4>' +
        '<div class="star-rating" id="starRating">' + starInputs + '</div>' +
        '<textarea id="reviewComment" placeholder="Твоята рецензия..." style="width:100%;padding:12px;border:2px solid #eee;border-radius:10px;font-size:14px;margin:10px 0;height:100px;resize:none;font-family:inherit;"></textarea>' +
        '<button onclick="submitReview(' + data.id + ')" class="detail-btn">Изпрати рецензия</button></div>' : '';
    document.getElementById('bookDetailContent').innerHTML =
        '<div class="book-detail"><div class="book-detail-top">' +
        '<img src="' + (data.coverUrl || '') + '" alt="' + data.title + '" onerror="this.src=\'https://placehold.co/200x280/1a1a2e/e0c97f?text=Knjiga\'">' +
        '<div class="book-detail-info">' +
        '<h1>' + data.title + '</h1>' +
        '<p class="book-detail-author">' + (data.authorName || 'Неизвестен автор') + '</p>' +
        '<p>Категория: ' + (data.categoryName || 'Без категория') + '</p>' +
        '<p>Година: ' + (data.publishedYear || 'Неизвестна') + '</p>' +
        '<p>ISBN: ' + (data.isbn || '-') + '</p>' +
        buttonsHtml + '</div></div>' +
    '<div class="book-description"><h3>Описание</h3><p>' + (data.description || 'Няма описание.') + '</p></div>' +
    (data.authorBio ? '<div class="book-description" style="margin-top:16px;"><h3>За автора</h3><p>' + data.authorBio + '</p></div>' : '') +
        (data.pdfUrl ? '<div class="book-description" style="margin-top:16px;"><h3>Четене на книгата</h3><iframe src="' + data.pdfUrl + '" style="width:100%; height:600px; border:none; border-radius:12px;"></iframe></div>' : '') +
        '<div class="book-reviews"><h3>Рецензии (' + reviews.length + ')</h3>' +
        reviewFormHtml + '<div id="reviewsList">' + reviewsHtml + '</div></div></div>';
    window._currentRating = 0;
}

function setRating(rating) {
    window._currentRating = rating;
    var stars = document.querySelectorAll('#starRating span');
    stars.forEach(function (s, i) { s.style.color = i < rating ? '#e0c97f' : '#ddd'; });
}

async function submitReview(bookId) {
    if (!window._currentRating) { showMessage('Моля изберете оценка.', 'error'); return; }
    var comment = document.getElementById('reviewComment').value.trim();
    var result = await apiFetch('/api/reviews', 'POST', { bookId: bookId, rating: window._currentRating, comment: comment || null });
    if (result.ok) { showMessage('Рецензията е добавена!'); showBookDetail(bookId); }
    else showMessage(result.data.message || 'Грешка.', 'error');
}


async function loadProfile() {
    var result = await apiFetch('/api/users/me');
    if (!result.ok) return;
    var data = result.data;
    document.getElementById('profileInfo').innerHTML =
        '<div class="profile-card"><div class="profile-header">' +
        '<div class="profile-avatar">&#128100;</div>' +
        '<h2>' + data.name + '</h2>' +
        '<p>' + (data.role === 'admin' ? 'Администратор' : 'Член') + '</p>' +
        '</div><div class="profile-body">' +
        '<div class="info-row"><span class="info-label">Имейл</span><span class="info-value">' + data.email + '</span></div>' +
        '<div class="info-row"><span class="info-label">Регистриран на</span><span class="info-value">' + new Date(data.createdAt).toLocaleDateString('bg-BG') + '</span></div>' +
    '<div class="info-row"><span class="info-label">Прочетени книги</span><span class="info-value">' + (data.activeLoans || 0) + '</span></div>' +
        '</div></div>';
}

async function loadRecommendations() {
    var result = await apiFetch('/api/recommendations');
    if (!result.ok) return;
    var data = result.data;
    var list = document.getElementById('recommendationsList');
    if (data.length === 0) { list.innerHTML = '<p>Няма налични препоръки в момента.</p>'; return; }
    var html = '';
    data.forEach(function (book) {
        html += '<div class="book-card">' +
            '<img src="' + (book.coverUrl || '') + '" alt="' + book.title + '" onerror="this.src=\'https://placehold.co/200x280/1a1a2e/e0c97f?text=Knjiga\'" onclick="showBookDetail(' + book.id + ')" style="cursor:pointer;">' +
            '<div class="book-card-body">' +
            '<h3 onclick="showBookDetail(' + book.id + ')" style="cursor:pointer;">' + book.title + '</h3>' +
            '<p>' + (book.authorName || 'Неизвестен автор') + '</p>' +
            '<p>' + (book.categoryName || 'Без категория') + '</p>' +
            '<button onclick="addToReadingList(' + book.id + ',\'want_to_read\')">Добави в дневника</button>' +
            '</div></div>';
    });
    list.innerHTML = html;
}

function showAdminTab(tab) {
    document.querySelectorAll('.admin-tabs button').forEach(function (b) { b.classList.remove('active'); });
    if (event && event.target) event.target.classList.add('active');
    document.getElementById('admin-books').style.display = tab === 'books' ? 'block' : 'none';
    document.getElementById('admin-users').style.display = tab === 'users' ? 'block' : 'none';
    document.getElementById('admin-authors').style.display = tab === 'authors' ? 'block' : 'none';
    document.getElementById('admin-categories').style.display = tab === 'categories' ? 'block' : 'none';
    if (tab === 'books') { loadAdminBooks(); loadAuthorsAndCategories(); }
    if (tab === 'users') loadAdminUsers();
    if (tab === 'authors') loadAdminAuthors();
    if (tab === 'categories') loadAdminCategories();
}

async function adminAddBook() {
    var title = document.getElementById('adminBookTitle').value.trim();
    var isbn = document.getElementById('adminBookIsbn').value.trim();
    var year = document.getElementById('adminBookYear').value;
    var desc = document.getElementById('adminBookDesc').value.trim();
    var coverUrl = document.getElementById('adminBookCover').value.trim() || null;
    var pdfUrl = document.getElementById('adminBookPdf') ? document.getElementById('adminBookPdf').value.trim() || null : null;
    if (!title) { showMessage('Заглавието е задължително.', 'error'); return; }
    var authorEl = document.getElementById('adminBookAuthor');
    var categoryEl = document.getElementById('adminBookCategory');
    var result = await apiFetch('/api/books', 'POST', {
        title: title, isbn: isbn || null,
        publishedYear: year ? parseInt(year) : null,
        coverUrl: coverUrl, description: desc || null,
        pdfUrl: pdfUrl,
        authorId: authorEl.value ? parseInt(authorEl.value) : null,
        categoryId: categoryEl.value ? parseInt(categoryEl.value) : null
    });
    if (result.ok) {
        showMessage('Книгата е добавена успешно!');
        ['adminBookTitle', 'adminBookIsbn', 'adminBookYear', 'adminBookDesc', 'adminBookCover'].forEach(function (id) { document.getElementById(id).value = ''; });
        loadAdminBooks();
    } else showMessage(result.data.message || 'Грешка при добавяне.', 'error');
}

async function loadAdminBooks() {
    var result = await apiFetch('/api/books');
    if (!result.ok) return;
    var html = '<table><thead><tr><th>Заглавие</th><th>Автор</th><th>Действия</th></tr></thead><tbody>';
    result.data.forEach(function (b) {
        html += '<tr><td>' + b.title + '</td><td>' + (b.authorName || '-') + '</td>' +
            '<td><button class="btn-primary" onclick="editBook(' + b.id + ')">Редактирай</button> <button class="btn-danger" onclick="adminDeleteBook(' + b.id + ')">Изтрий</button></td></tr>';
    });
    html += '</tbody></table>';
    document.getElementById('adminBooksList').innerHTML = html;
}

async function adminDeleteBook(id) {
    if (!confirm('Сигурни ли сте?')) return;
    var result = await apiFetch('/api/books/' + id, 'DELETE');
    if (result.ok) { showMessage(result.data.message); loadAdminBooks(); }
    else showMessage(result.data.message || 'Грешка при изтриване.', 'error');
}

async function editBook(id) {
    var result = await apiFetch('/api/books/' + id);
    if (!result.ok) return;
    var b = result.data;
    await loadAuthorsAndCategories();
    document.getElementById('adminBookTitle').value = b.title || '';
    document.getElementById('adminBookIsbn').value = b.isbn || '';
    document.getElementById('adminBookYear').value = b.publishedYear || '';
    document.getElementById('adminBookCover').value = b.coverUrl || '';
    document.getElementById('adminBookDesc').value = b.description || '';
    var authorSel = document.getElementById('adminBookAuthor');
    var categorySel = document.getElementById('adminBookCategory');
    for (var i = 0; i < authorSel.options.length; i++) {
        if (authorSel.options[i].text === b.authorName) { authorSel.selectedIndex = i; break; }
    }
    for (var j = 0; j < categorySel.options.length; j++) {
        if (categorySel.options[j].text === b.categoryName) { categorySel.selectedIndex = j; break; }
    }
    var btn = document.querySelector('#admin-books .form-inline button');
    btn.textContent = 'Обнови книгата';
    btn.onclick = function () { updateBook(id); };
    document.getElementById('admin-books').scrollIntoView({ behavior: 'smooth' });
}

async function updateBook(id) {
    var title = document.getElementById('adminBookTitle').value.trim();
    var isbn = document.getElementById('adminBookIsbn').value.trim();
    var year = document.getElementById('adminBookYear').value;
    var cover = document.getElementById('adminBookCover').value.trim();
    var desc = document.getElementById('adminBookDesc').value.trim();
    var authorEl = document.getElementById('adminBookAuthor');
    var categoryEl = document.getElementById('adminBookCategory');
    if (!title) { showMessage('Заглавието е задължително.', 'error'); return; }
    var result = await apiFetch('/api/books/' + id, 'PUT', {
        title: title, isbn: isbn || null,
        publishedYear: year ? parseInt(year) : null,
        coverUrl: cover || null, description: desc || null,
        authorId: authorEl.value ? parseInt(authorEl.value) : null,
        categoryId: categoryEl.value ? parseInt(categoryEl.value) : null
    });
    if (result.ok) {
        showMessage('Книгата е обновена успешно!');
        var btn = document.querySelector('#admin-books .form-inline button');
        btn.textContent = 'Добави книга';
        btn.onclick = function () { adminAddBook(); };
        ['adminBookTitle', 'adminBookIsbn', 'adminBookYear', 'adminBookDesc', 'adminBookCover'].forEach(function (id) { document.getElementById(id).value = ''; });
        loadAdminBooks();
    } else showMessage(result.data.message || 'Грешка при обновяване.', 'error');
}

async function loadAdminUsers() {
    var result = await apiFetch('/api/users');
    if (!result.ok) return;
    var html = '<table><thead><tr><th>Ime</th><th>Имейл</th><th>Роля</th><th>Действия</th></tr></thead><tbody>';
    result.data.forEach(function (u) {
        html += '<tr><td>' + u.name + '</td><td>' + u.email + '</td><td>' + (u.role === 'admin' ? 'Admin' : 'Member') + '</td>' +
            '<td><button class="btn-primary" onclick="toggleRole(' + u.id + ',\'' + u.role + '\')">' + (u.role === 'admin' ? 'Към Member' : 'Към Admin') + '</button>' +
            '<button class="btn-danger" onclick="adminDeleteUser(' + u.id + ')">Изтрий</button></td></tr>';
    });
    html += '</tbody></table>';
    document.getElementById('adminUsersList').innerHTML = html;
}

async function toggleRole(id, currentRole) {
    var newRole = currentRole === 'admin' ? 'member' : 'admin';
    var result = await apiFetch('/api/users/' + id + '/role', 'PUT', { role: newRole });
    if (result.ok) { showMessage(result.data.message); loadAdminUsers(); }
    else showMessage(result.data.message || 'Грешка.', 'error');
}

async function adminDeleteUser(id) {
    if (!confirm('Сигурни ли сте?')) return;
    var result = await apiFetch('/api/users/' + id, 'DELETE');
    if (result.ok) { showMessage(result.data.message); loadAdminUsers(); }
    else showMessage(result.data.message || 'Грешка при изтриване.', 'error');
}
async function loadAuthorsAndCategories() {
    var r1 = await apiFetch('/api/authors');
    var r2 = await apiFetch('/api/categories');
    if (r1.ok) {
        var sel = document.getElementById('adminBookAuthor');
        sel.innerHTML = '<option value="">-- Избери автор --</option>';
        r1.data.forEach(function (a) { sel.innerHTML += '<option value="' + a.id + '">' + a.name + '</option>'; });
    }
    if (r2.ok) {
        var sel2 = document.getElementById('adminBookCategory');
        sel2.innerHTML = '<option value="">-- Избери категория --</option>';
        r2.data.forEach(function (c) { sel2.innerHTML += '<option value="' + c.id + '">' + c.name + '</option>'; });
    }
}

async function adminAddAuthor() {
    var name = document.getElementById('adminAuthorName').value.trim();
    var bio = document.getElementById('adminAuthorBio').value.trim();
    if (!name) { showMessage('Името е задължително.', 'error'); return; }
    var result = await apiFetch('/api/authors', 'POST', { name: name, bio: bio || null });
    if (result.ok) {
        showMessage('Авторът е добавен успешно!');
        document.getElementById('adminAuthorName').value = '';
        document.getElementById('adminAuthorBio').value = '';
        loadAdminAuthors();
    } else showMessage(result.data.message || 'Грешка при добавяне.', 'error');
}

async function loadAdminAuthors() {
    var result = await apiFetch('/api/authors');
    if (!result.ok) return;
    var html = '<table><thead><tr><th>Ime</th><th>Книги</th><th>Биография</th><th>Действия</th></tr></thead><tbody>';
    result.data.forEach(function (a) {
        html += '<tr><td>' + a.name + '</td><td>' + a.booksCount + '</td><td>' + (a.bio ? a.bio.substring(0, 60) + '...' : '-') + '</td>' +
            '<td><button class="btn-danger" onclick="adminDeleteAuthor(' + a.id + ')">Изтрий</button></td></tr>';
    });
    html += '</tbody></table>';
    document.getElementById('adminAuthorsList').innerHTML = html;
}

async function adminDeleteAuthor(id) {
    if (!confirm('Сигурни ли сте?')) return;
    var result = await apiFetch('/api/authors/' + id, 'DELETE');
    if (result.ok) { showMessage(result.data.message); loadAdminAuthors(); }
    else showMessage(result.data.message || 'Грешка при изтриване.', 'error');
}

async function adminAddCategory() {
    var name = document.getElementById('adminCategoryName').value.trim();
    var slug = document.getElementById('adminCategorySlug').value.trim();
    if (!name) { showMessage('Името е задължително.', 'error'); return; }
    var result = await apiFetch('/api/categories', 'POST', { name: name, slug: slug || null });
    if (result.ok) {
        showMessage('Категорията е добавена успешно!');
        document.getElementById('adminCategoryName').value = '';
        document.getElementById('adminCategorySlug').value = '';
        loadAdminCategories();
    } else showMessage(result.data.message || 'Грешка при добавяне.', 'error');
}

async function loadAdminCategories() {
    var result = await apiFetch('/api/categories');
    if (!result.ok) return;
    var html = '<table><thead><tr><th>Ime</th><th>Slug</th><th>Книги</th><th>Действия</th></tr></thead><tbody>';
    result.data.forEach(function (c) {
        html += '<tr><td>' + c.name + '</td><td>' + c.slug + '</td><td>' + c.booksCount + '</td>' +
            '<td><button class="btn-danger" onclick="adminDeleteCategory(' + c.id + ')">Изтрий</button></td></tr>';
    });
    html += '</tbody></table>';
    document.getElementById('adminCategoriesList').innerHTML = html;
}

async function adminDeleteCategory(id) {
    if (!confirm('Сигурни ли сте?')) return;
    var result = await apiFetch('/api/categories/' + id, 'DELETE');
    if (result.ok) { showMessage(result.data.message); loadAdminCategories(); }
    else showMessage(result.data.message || 'Грешка при изтриване.', 'error');
}

async function loadCategoryFilter() {
    var r1 = await apiFetch('/api/categories');
    var r2 = await apiFetch('/api/authors');
    if (r1.ok) {
        var filter = document.getElementById('categoryFilter');
        if (filter) {
            filter.innerHTML = '<option value="">Всички категории</option>';
            r1.data.forEach(function (c) { filter.innerHTML += '<option value="' + c.id + '">' + c.name + '</option>'; });
        }
    }
    if (r2.ok) {
        var authorFilter = document.getElementById('authorFilter');
        if (authorFilter) {
            authorFilter.innerHTML = '<option value="">Всички автори</option>';
            r2.data.forEach(function (a) { authorFilter.innerHTML += '<option value="' + a.id + '">' + a.name + '</option>'; });
        }
    }
}

async function loadReadingList() {
    var r1 = await apiFetch('/api/readinglist');
    var r2 = await apiFetch('/api/readinglist/stats');
    if (!r1.ok) return;
    allReadingList = r1.data;
    if (r2.ok) {
        var s = r2.data;
        document.getElementById('readingStats').innerHTML =
            '<div class="stat-card"><div class="stat-number">' + s.totalBooks + '</div><div class="stat-label">Общо книги</div></div>' +
            '<div class="stat-card"><div class="stat-number">' + s.wantToRead + '</div><div class="stat-label">Искам да прочета</div></div>' +
            '<div class="stat-card"><div class="stat-number">' + s.currentlyReading + '</div><div class="stat-label">Чета сега</div></div>' +
            '<div class="stat-card"><div class="stat-number">' + s.read + '</div><div class="stat-label">Прочетени</div></div>' +
            '<div class="stat-card"><div class="stat-number">' + s.booksReadThisYear + '</div><div class="stat-label">Тази година</div></div>' +
            '<div class="stat-card"><div class="stat-number" style="font-size:16px;">' + (s.favoriteCategory || '-') + '</div><div class="stat-label">Любим жанр</div></div>';
    }
    filterReadingList('all');
}

function filterReadingList(status) {
    document.querySelectorAll('.admin-tabs button').forEach(function (b) { b.classList.remove('active'); });
    var tabMap = { 'all': 'tab-all', 'want_to_read': 'tab-want', 'reading': 'tab-reading', 'read': 'tab-read' };
    var tabEl = document.getElementById(tabMap[status]);
    if (tabEl) tabEl.classList.add('active');
    var filtered = status === 'all' ? allReadingList : allReadingList.filter(function (r) { return r.status === status; });
    var content = document.getElementById('readingListContent');
    if (filtered.length === 0) { content.innerHTML = '<p style="color:#777;">Няма книги в тази категория.</p>'; return; }
    var html = '';
    filtered.forEach(function (r) {
        html += '<div class="book-card">' +
            '<img src="' + (r.coverUrl || '') + '" alt="' + r.bookTitle + '" onerror="this.src=\'https://placehold.co/200x280/1a1a2e/e0c97f?text=Knjiga\'" onclick="showBookDetail(' + r.bookId + ')" style="cursor:pointer;">' +
            '<div class="book-card-body">' +
            '<h3 onclick="showBookDetail(' + r.bookId + ')" style="cursor:pointer;">' + r.bookTitle + '</h3>' +
            '<p>' + (r.authorName || 'Неизвестен автор') + '</p>' +
            '<p>' + (r.categoryName || 'Без категория') + '</p>' +
            '<div style="margin:8px 0;"><select onchange="updateReadingStatus(' + r.id + ',this.value)" style="width:100%;padding:6px 10px;border:2px solid #eee;border-radius:8px;font-size:13px;outline:none;">' +
            '<option value="want_to_read"' + (r.status === 'want_to_read' ? ' selected' : '') + '>Искам да прочета</option>' +
            '<option value="reading"' + (r.status === 'reading' ? ' selected' : '') + '>Чета сега</option>' +
            '<option value="read"' + (r.status === 'read' ? ' selected' : '') + '>Прочетена</option>' +
            '</select></div>' +
            (r.status !== 'want_to_read' && r.startedAt ? '<p style="font-size:12px;color:#aaa;">Започната: ' + new Date(r.startedAt).toLocaleDateString('bg-BG') + '</p>' : '') +
            (r.status === 'read' && r.finishedAt ? '<p style="font-size:12px;color:#aaa;">Завършена: ' + new Date(r.finishedAt).toLocaleDateString('bg-BG') + '</p>' : '') +
            (r.status === 'reading' && r.pdfUrl ? '<a href="' + r.pdfUrl + '" target="_blank" style="display:block;width:100%;padding:8px;margin-top:8px;background:linear-gradient(135deg,#27ae60,#219a52);color:white;border-radius:8px;font-size:13px;text-align:center;text-decoration:none;">Продължи четенето</a>' : '') +
            '<button onclick="removeFromReadingList(' + r.id + ')" style="width:100%;padding:8px;margin-top:8px;background:#e74c3c;color:white;border:none;border-radius:8px;cursor:pointer;font-size:13px;">Премахни</button>' +            '</div></div>';
    });
    content.innerHTML = html;
}

async function addToReadingList(bookId, status) {
    var result = await apiFetch('/api/readinglist', 'POST', { bookId: bookId, status: status });
    if (result.ok) showMessage(result.data.message);
    else showMessage(result.data.message || 'Грешка.', 'error');
}

async function updateReadingStatus(id, status) {
    var result = await apiFetch('/api/readinglist/' + id, 'PUT', { status: status });
    if (result.ok) { showMessage(result.data.message); loadReadingList(); }
    else showMessage(result.data.message || 'Грешка.', 'error');
}

async function removeFromReadingList(id) {
    if (!confirm('Сигурни ли сте?')) return;
    var result = await apiFetch('/api/readinglist/' + id, 'DELETE');
    if (result.ok) { showMessage(result.data.message); loadReadingList(); }
    else showMessage(result.data.message || 'Грешка.', 'error');
}

async function forgotPassword() {
    var email = document.getElementById('forgotEmail').value.trim();
    if (!email) { showMessage('Моля въведете имейл.', 'error'); return; }
    var result = await apiFetch('/api/auth/forgot-password', 'POST', { email: email });
    if (result.ok) {
        var msg = document.getElementById('forgotMessage');
        msg.style.display = 'block';
        msg.innerHTML = '<p>' + result.data.message + '</p>';
    } else showMessage(result.data.message || 'Грешка.', 'error');
}

function useResetLink(token2, email) {
    document.getElementById('resetEmail').value = email;
    document.getElementById('resetCode').value = token2;
    showPage('resetPassword');
}

async function resetPassword() {
    var email = document.getElementById('resetEmail').value.trim();
    var code = document.getElementById('resetCode').value.trim();
    var newPassword = document.getElementById('resetNewPassword').value;
    var confirmPassword = document.getElementById('resetConfirmPassword').value;
    if (!email || !code || !newPassword || !confirmPassword) { showMessage('Моля попълнете всички полета.', 'error'); return; }
    var result = await apiFetch('/api/auth/reset-password', 'POST', { email: email, code: code, newPassword: newPassword, confirmPassword: confirmPassword });
    if (result.ok) { showMessage(result.data.message); setTimeout(function () { showPage('login'); }, 2000); }
    else showMessage(result.data.message || 'Грешка.', 'error');
}

function toggleTheme() {
    var current = document.documentElement.getAttribute('data-theme');
    var newTheme = current === 'dark' ? 'light' : 'dark';
    document.documentElement.setAttribute('data-theme', newTheme);
    localStorage.setItem('theme', newTheme);
    document.getElementById('themeBtn').textContent = newTheme === 'dark' ? 'Светъл режим' : 'Тъмен режим';
}

window.onpopstate = function (event) {
    if (event.state && event.state.page) {
        showPage(event.state.page);
    }
};
async function changePassword() {
    var currentPassword = document.getElementById('currentPassword').value;
    var newPassword = document.getElementById('newPassword').value;
    var confirmNewPassword = document.getElementById('confirmNewPassword').value;
    if (!currentPassword || !newPassword || !confirmNewPassword) {
        showMessage('Моля попълнете всички полета.', 'error'); return;
    }
    if (newPassword !== confirmNewPassword) {
        showMessage('Новите пароли не съвпадат.', 'error'); return;
    }
    if (newPassword.length < 8) { showMessage('Паролата трябва да е поне 8 символа.', 'error'); return; }
    if (!/[A-Z]/.test(newPassword)) { showMessage('Паролата трябва да съдържа поне една главна буква.', 'error'); return; }
    if (!/[a-z]/.test(newPassword)) { showMessage('Паролата трябва да съдържа поне една малка буква.', 'error'); return; }
    if (!/[0-9]/.test(newPassword)) { showMessage('Паролата трябва да съдържа поне една цифра.', 'error'); return; }
    if (!/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(newPassword)) { showMessage('Паролата трябва да съдържа поне един специален символ.', 'error'); return; }
    var result = await apiFetch('/api/auth/change-password', 'POST', {
        currentPassword: currentPassword,
        newPassword: newPassword,
        confirmPassword: confirmNewPassword
    });
    if (result.ok) {
        showMessage(result.data.message);
        document.getElementById('currentPassword').value = '';
        document.getElementById('newPassword').value = '';
        document.getElementById('confirmNewPassword').value = '';
    } else showMessage(result.data.message || 'Грешка.', 'error');
}

function goBack() {
    showPage(previousPage);
}

async function loadForum() {
    var result = await apiFetch('/api/forum');
    if (!result.ok) return;
    var list = document.getElementById('forumTopicsList');
    var newTopicBtn = document.getElementById('newTopicBtn');
    if (newTopicBtn) newTopicBtn.style.display = token ? 'block' : 'none';
    if (result.data.length === 0) {
        list.innerHTML = '<p style="color:#777;">Няма теми още. Бъди първият!</p>';
        return;
    }
    var html = '';
    result.data.forEach(function (t) {
        html += '<div class="loan-card" style="cursor:pointer;" onclick="loadForumTopic(' + t.id + ')">' +
            '<div class="loan-info">' +
            '<h3>' + t.title + '</h3>' +
            '<p>' + t.userName + ' · ' + new Date(t.createdAt).toLocaleDateString('bg-BG') + '</p>' +
            '<p style="color:#777; font-size:13px;">' + t.postsCount + ' отговора</p>' +
            '</div></div>';
    });
    list.innerHTML = html;
}

async function loadForumTopic(id) {
    var result = await apiFetch('/api/forum/topic/' + id);
    if (!result.ok) return;
    var t = result.data;

    document.querySelectorAll('.page').forEach(function (p) { p.style.display = 'none'; });
    document.getElementById('heroSection').style.display = 'none';
    document.getElementById('page-forumTopic').style.display = 'block';
    localStorage.setItem('lastPage', 'forum');

    var postsHtml = '';
    if (t.posts && t.posts.length > 0) {
        t.posts.forEach(function (p) {
            postsHtml += '<div class="review-card">' +
                '<div class="review-header">' +
                '<span class="review-author">' + p.userName + '</span>' +
                '<span class="review-date">' + new Date(p.createdAt).toLocaleDateString('bg-BG') + '</span>' +
                '</div><p>' + p.content + '</p></div>';
        });
    } else {
        postsHtml = '<p style="color:#777;">Няма отговори още.</p>';
    }
    var replyForm = token ? '<div class="review-form" style="margin-top:24px;"><h4>Напиши отговор</h4>' +
        '<textarea id="forumReplyContent" placeholder="Твоят отговор..." style="width:100%;padding:12px;border:2px solid #eee;border-radius:10px;font-size:14px;margin:10px 0;height:100px;resize:none;font-family:inherit;"></textarea>' +
        '<button onclick="createForumPost(' + t.id + ')" class="detail-btn">Изпрати</button></div>' : '';
    document.getElementById('forumTopicContent').innerHTML =
        '<div class="book-detail">' +
        '<h2>' + t.title + '</h2>' +
        '<p style="color:#777; margin-bottom:8px;">' + t.userName + ' · ' + new Date(t.createdAt).toLocaleDateString('bg-BG') + '</p>' +
        '<div class="book-description"><p>' + t.content + '</p></div>' +
        '<div class="book-reviews" style="margin-top:24px;"><h3>Отговори (' + (t.posts ? t.posts.length : 0) + ')</h3>' +
        postsHtml + replyForm + '</div></div>';
}

async function createForumTopic() {
    var title = document.getElementById('forumTopicTitle').value.trim();
    var content = document.getElementById('forumTopicText').value.trim();
    if (!title || !content) { showMessage('Моля попълнете всички полета.', 'error'); return; }
    var result = await apiFetch('/api/forum', 'POST', { title: title, content: content });
    if (result.ok) {
        showMessage('Темата е създадена успешно!');
        document.getElementById('forumTopicTitle').value = '';
        document.getElementById('forumTopicText').value = '';
        showPage('forum');
    } else showMessage(result.data.message || 'Грешка.', 'error');
}

async function createForumPost(topicId) {
    var content = document.getElementById('forumReplyContent').value.trim();
    if (!content) { showMessage('Моля напишете отговор.', 'error'); return; }
    var result = await apiFetch('/api/forum/post', 'POST', { topicId: topicId, content: content });
    if (result.ok) {
        showMessage('Отговорът е добавен!');
        loadForumTopic(topicId);
    } else showMessage(result.data.message || 'Грешка.', 'error');
}

async function loadFavorites() {
    var result = await apiFetch('/api/favorites');
    if (!result.ok) return;
    var list = document.getElementById('favoritesList');
    if (result.data.length === 0) {
        list.innerHTML = '<p style="color:#777;">Нямаш любими книги още.</p>';
        return;
    }
    var html = '';
    result.data.forEach(function (book) {
        html += '<div class="book-card">' +
            '<img src="' + (book.coverUrl || '') + '" alt="' + book.title + '" onerror="this.src=\'https://placehold.co/200x280/1a1a2e/e0c97f?text=Knjiga\'" onclick="showBookDetail(' + book.id + ')" style="cursor:pointer;">' +
            '<div class="book-card-body">' +
            '<h3 onclick="showBookDetail(' + book.id + ')" style="cursor:pointer;">' + book.title + '</h3>' +
            '<p>' + (book.authorName || 'Неизвестен автор') + '</p>' +
            '<p>' + (book.categoryName || 'Без категория') + '</p>' +
            '<button onclick="removeFavorite(' + book.id + ')" style="background:#e74c3c;">♥ Премахни от любими</button>' +
            '</div></div>';
    });
    list.innerHTML = html;
}

async function toggleFavorite(bookId, isFavorite) {
    if (isFavorite) {
        var result = await apiFetch('/api/favorites/' + bookId, 'DELETE');
        if (result.ok) {
            showMessage('Премахната от любими!');
            showBookDetail(bookId);
        }
    } else {
        var result = await apiFetch('/api/favorites', 'POST', { bookId: bookId });
        if (result.ok) {
            showMessage('Добавена в любими!');
            showBookDetail(bookId);
        } else showMessage(result.data.message || 'Грешка.', 'error');
    }
}

async function removeFavorite(bookId) {
    var result = await apiFetch('/api/favorites/' + bookId, 'DELETE');
    if (result.ok) { showMessage('Премахната от любими!'); loadFavorites(); }
    else showMessage(result.data.message || 'Грешка.', 'error');
}

async function loadNewBookNotifications() {
    var result = await apiFetch('/api/newbooknotifications');
    if (!result.ok) return;
    var list = document.getElementById('newBookNotifsList');
    if (result.data.length === 0) {
        list.innerHTML = '<p style="color:#777;">Няма известия.</p>';
        return;
    }
    var html = '';
    result.data.forEach(function (n) {
        html += '<div class="loan-card" style="opacity:' + (n.isRead ? '0.6' : '1') + ';">' +
            '<div class="loan-info">' +
            '<p>' + n.message + '</p>' +
            '<p style="font-size:12px;color:#aaa;">' + new Date(n.createdAt).toLocaleDateString('bg-BG') + '</p>' +
            '</div>' +
            '<div class="loan-actions">' +
            (!n.isRead ? '<button onclick="markNewBookNotifRead(' + n.id + ')" class="btn-primary">Прочетено</button>' : '<span style="color:#27ae60;">✓ Прочетено</span>') +
            '<button onclick="showBookDetail(' + n.bookId + ')" class="btn-primary" style="margin-left:8px;">Виж книгата</button>' +
            '</div></div>';
    });
    list.innerHTML = html;
    checkNewBookNotifBadge();
}

async function markNewBookNotifRead(id) {
    var result = await apiFetch('/api/newbooknotifications/' + id + '/read', 'PUT');
    if (result.ok) loadNewBookNotifications();
}

async function markAllNewBookNotifsRead() {
    var result = await apiFetch('/api/newbooknotifications/read-all', 'PUT');
    if (result.ok) { showMessage('Всички са маркирани!'); loadNewBookNotifications(); }
}

async function checkNewBookNotifBadge() {
    if (!token) return;
    var result = await apiFetch('/api/newbooknotifications/unread');
    if (!result.ok) return;
    var badge = document.getElementById('newBookBadge');
    if (result.data.count > 0) {
        badge.style.display = 'inline';
        badge.textContent = result.data.count;
    } else {
        badge.style.display = 'none';
    }
}

window.onload = function () {
    // Провери дали има token и email в URL параметрите
    var urlParams = new URLSearchParams(window.location.search);
    var resetToken = urlParams.get('token');
    var resetEmail = urlParams.get('email');
    if (resetToken && resetEmail) {
        document.getElementById('resetEmail').value = resetEmail;
        document.getElementById('resetCode').value = resetToken;
        showPage('resetPassword');
        return;
    }
    var savedUser = localStorage.getItem('user');
    if (savedUser) currentUser = JSON.parse(savedUser);
    updateNav();
    var lastPage = localStorage.getItem('lastPage') || 'books';
    var validPages = ['books', 'profile', 'recommendations', 'readingList', 'admin', 'forum', 'favorites', 'newBookNotifications'];    if (validPages.includes(lastPage) && token) {
        showPage(lastPage);
    } else {
        showPage('books');
    }
    loadCategoryFilter();
    if (token) checkNewBookNotifBadge();
    var savedTheme = localStorage.getItem('theme') || 'light';
    document.documentElement.setAttribute('data-theme', savedTheme);
    document.getElementById('themeBtn').textContent = savedTheme === 'dark' ? 'Светъл режим' : 'Тъмен режим';
};
