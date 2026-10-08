/**
 * ระบบจัดการ E-Book Store ฝั่ง Client
 * เชื่อมต่อกับ ASP.NET Core 8 Web API
 */

// กำหนด Base URL ของ API (ใช้ Relative Path สำหรับ Same-origin)
const API_BASE = '/api';

// สถานะการทำงานของหน้าเว็บ (Application State)
let currentPage = 1;
const pageSize = 10;
let currentSearch = '';
let currentCategoryId = '';
let bookModalInstance = null;
let toastInstance = null;

// ========================================================
// 1. ฟังก์ชันป้องกัน XSS และฟังก์ชันแปลงรูปแบบข้อมูล
// ========================================================

/**
 * ป้องกันช่องโหว่ Cross-Site Scripting (XSS) โดยแทนที่ตัวอักษรพิเศษของ HTML
 * @param {any} str 
 * @returns {string}
 */
function escapeHtml(str) {
    if (str === null || str === undefined) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

/**
 * แปลงตัวเลขราคาเป็นสกุลเงินบาท (เช่น 1,250.00)
 * @param {number} amount 
 * @returns {string}
 */
function formatCurrency(amount) {
    if (amount === null || amount === undefined || isNaN(amount)) return '0.00';
    return Number(amount).toLocaleString('th-TH', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
}

/**
 * ตัดและจัดรูปแบบวันที่ให้อยู่ในรูป YYYY-MM-DD
 * @param {string} dateString 
 * @returns {string}
 */
function formatDate(dateString) {
    if (!dateString) return '-';
    const date = new Date(dateString);
    if (isNaN(date.getTime())) return dateString;
    return date.toISOString().split('T')[0];
}

/**
 * แสดง Toast แจ้งเตือนผลการดำเนินงาน
 * @param {string} message 
 * @param {boolean} isSuccess 
 */
function showToast(message, isSuccess = true) {
    const toastEl = document.getElementById('actionToast');
    const toastBody = document.getElementById('toastMessage');

    toastEl.classList.remove('bg-success', 'bg-danger');
    toastEl.classList.add(isSuccess ? 'bg-success' : 'bg-danger');
    toastBody.textContent = message;

    if (!toastInstance) {
        toastInstance = new bootstrap.Toast(toastEl, { delay: 3500 });
    }
    toastInstance.show();
}

// ========================================================
// 2. โหลดข้อมูล Lookups (Categories & Authors)
// ========================================================

async function loadCategories() {
    try {
        const response = await fetch(`${API_BASE}/Categories`);
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        const categories = await response.json();

        // 1. นำไปใส่ใน Dropdown ตัวกรองของตาราง
        const filterSelect = document.getElementById('categoryFilter');
        filterSelect.innerHTML = '<option value="">-- ทุกหมวดหมู่ --</option>';
        categories.forEach(cat => {
            const opt = document.createElement('option');
            opt.value = cat.id;
            opt.textContent = cat.name;
            filterSelect.appendChild(opt);
        });

        // 2. นำไปใส่ใน Dropdown ฟอร์ม Modal
        const modalCatSelect = document.getElementById('bookCategoryId');
        modalCatSelect.innerHTML = '<option value="">-- เลือกหมวดหมู่ --</option>';
        categories.forEach(cat => {
            const opt = document.createElement('option');
            opt.value = cat.id;
            opt.textContent = cat.name;
            modalCatSelect.appendChild(opt);
        });
    } catch (err) {
        console.error('Error loading categories:', err);
    }
}

async function loadAuthors() {
    try {
        const response = await fetch(`${API_BASE}/Authors`);
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        const authors = await response.json();

        const modalAuthorSelect = document.getElementById('bookAuthorId');
        modalAuthorSelect.innerHTML = '<option value="">-- เลือกผู้แต่ง --</option>';
        authors.forEach(author => {
            const opt = document.createElement('option');
            opt.value = author.id;
            opt.textContent = author.name;
            modalAuthorSelect.appendChild(opt);
        });
    } catch (err) {
        console.error('Error loading authors:', err);
    }
}

// ========================================================
// 3. โหลดและแสดงผลตารางหนังสือ (Search, Filter, Pagination)
// ========================================================

async function loadBooks(page = 1) {
    currentPage = page;
    const tableBody = document.getElementById('booksTableBody');
    const errorAlert = document.getElementById('globalErrorAlert');
    errorAlert.classList.add('d-none');

    tableBody.innerHTML = `
        <tr>
            <td colspan="8" class="text-center py-4 text-muted">
                <div class="spinner-border spinner-border-sm text-primary me-2" role="status"></div>
                กำลังโหลดข้อมูล...
            </td>
        </tr>`;

    try {
        // สร้าง Query Parameters ให้ตรงกับ BookQueryParameters ใน C#
        const params = new URLSearchParams({
            pageNumber: currentPage,
            pageSize: pageSize
        });
        if (currentSearch) params.append('search', currentSearch);
        if (currentCategoryId) params.append('categoryId', currentCategoryId);

        const response = await fetch(`${API_BASE}/Books/search?${params.toString()}`);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        // อ่านค่า PagedResult<BookResponseDto> (camelCase)
        const data = await response.json();
        renderTable(data.items);
        renderPagination(data);
    } catch (err) {
        console.error('Fetch books failed:', err);
        tableBody.innerHTML = `
            <tr>
                <td colspan="8" class="text-center py-4 text-danger">
                    <i class="bi bi-exclamation-triangle me-2"></i>ไม่สามารถดึงข้อมูลได้: ${escapeHtml(err.message)}
                </td>
            </tr>`;
        errorAlert.textContent = `เกิดข้อผิดพลาดในการเชื่อมต่อ Web API (${err.message}) กรุณาตรวจสอบสถานะ Backend`;
        errorAlert.classList.remove('d-none');
    }
}

function renderTable(books) {
    const tableBody = document.getElementById('booksTableBody');

    if (!books || books.length === 0) {
        tableBody.innerHTML = `
            <tr>
                <td colspan="8" class="text-center py-5 text-muted">
                    <i class="bi bi-inbox fs-1 d-block mb-2 text-secondary"></i>
                    ไม่พบข้อมูลหนังสือตามเงื่อนไขที่ระบุ
                </td>
            </tr>`;
        return;
    }

    let rowsHtml = '';
    books.forEach(b => {
        // ตรวจสอบว่ามี FileUrl หรือไม่ เพื่อสร้างลิงก์ดาวน์โหลด
        const fileIcon = b.fileUrl 
            ? `<a href="${escapeHtml(b.fileUrl)}" target="_blank" rel="noopener noreferrer" class="ms-1 text-primary" title="เปิดลิงก์ไฟล์"><i class="bi bi-box-arrow-up-right"></i></a>` 
            : '';

        rowsHtml += `
            <tr>
                <td class="text-muted fw-bold">${b.id}</td>
                <td>
                    <span class="fw-semibold text-dark">${escapeHtml(b.title)}</span>
                    ${fileIcon}
                </td>
                <td><code>${escapeHtml(b.isbn)}</code></td>
                <td><span class="badge badge-category px-2 py-1">${escapeHtml(b.categoryName || '-')}</span></td>
                <td>${escapeHtml(b.authorName || '-')}</td>
                <td class="text-end fw-semibold text-primary">${formatCurrency(b.price)}</td>
                <td class="text-center text-muted small">${formatDate(b.publishedDate)}</td>
                <td class="text-center action-btns">
                    <button class="btn btn-outline-warning me-1" onclick="openEditModal(${b.id})" title="แก้ไข">
                        <i class="bi bi-pencil-square"></i>
                    </button>
                    <button class="btn btn-outline-danger" onclick="deleteBook(${b.id}, '${escapeHtml(b.title)}')" title="ลบ">
                        <i class="bi bi-trash"></i>
                    </button>
                </td>
            </tr>`;
    });

    tableBody.innerHTML = rowsHtml;
}

function renderPagination(data) {
    const infoEl = document.getElementById('paginationInfo');
    const paginationUl = document.getElementById('paginationControls');

    const totalCount = data.totalCount;
    const pageNum = data.pageNumber;
    const totalPages = data.totalPages;

    if (totalCount === 0) {
        infoEl.textContent = 'ไม่พบรายการข้อมูล';
        paginationUl.innerHTML = '';
        return;
    }

    const startIdx = (pageNum - 1) * pageSize + 1;
    const endIdx = Math.min(pageNum * pageSize, totalCount);
    infoEl.innerHTML = `แสดง <strong>${startIdx} - ${endIdx}</strong> จากทั้งหมด <strong>${totalCount}</strong> รายการ (หน้า <strong>${pageNum}</strong> / <strong>${totalPages}</strong>)`;

    let pagHtml = '';

    // ปุ่มย้อนกลับ (Previous)
    pagHtml += `
        <li class="page-item ${!data.hasPreviousPage ? 'disabled' : ''}">
            <a class="page-link" href="#" onclick="changePage(${pageNum - 1}); return false;" aria-label="Previous">
                <span aria-hidden="true">&laquo;</span>
            </a>
        </li>`;

    // วนลูปสร้างปุ่มตัวเลขหน้า
    for (let p = 1; p <= totalPages; p++) {
        if (p === 1 || p === totalPages || (p >= pageNum - 2 && p <= pageNum + 2)) {
            pagHtml += `
                <li class="page-item ${p === pageNum ? 'active' : ''}">
                    <a class="page-link" href="#" onclick="changePage(${p}); return false;">${p}</a>
                </li>`;
        } else if (p === pageNum - 3 || p === pageNum + 3) {
            pagHtml += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
        }
    }

    // ปุ่มหน้าถัดไป (Next)
    pagHtml += `
        <li class="page-item ${!data.hasNextPage ? 'disabled' : ''}">
            <a class="page-link" href="#" onclick="changePage(${pageNum + 1}); return false;" aria-label="Next">
                <span aria-hidden="true">&raquo;</span>
            </a>
        </li>`;

    paginationUl.innerHTML = pagHtml;
}

function changePage(page) {
    if (page < 1) return;
    loadBooks(page);
}

// ========================================================
// 4. การจัดการ Modal (เปิดฟอร์ม เพิ่ม / แก้ไข)
// ========================================================

function openCreateModal() {
    clearModalErrors();
    document.getElementById('bookForm').reset();
    document.getElementById('bookId').value = '';
    document.getElementById('bookModalLabel').innerHTML = '<i class="bi bi-plus-circle me-2"></i>เพิ่มหนังสือใหม่';
    bookModalInstance.show();
}

async function openEditModal(id) {
    clearModalErrors();
    document.getElementById('bookForm').reset();
    document.getElementById('bookId').value = id;
    document.getElementById('bookModalLabel').innerHTML = '<i class="bi bi-pencil-square me-2"></i>แก้ไขข้อมูลหนังสือ';

    try {
        const response = await fetch(`${API_BASE}/Books/${id}`);
        if (!response.ok) throw new Error('ไม่พบข้อมูลหนังสือเล่มที่ต้องการแก้ไข');

        const book = await response.json();
        document.getElementById('bookTitle').value = book.title || '';
        document.getElementById('bookIsbn').value = book.isbn || '';
        document.getElementById('bookPrice').value = book.price ?? '';
        document.getElementById('bookPublishedDate').value = formatDate(book.publishedDate);
        document.getElementById('bookFileUrl').value = book.fileUrl || '';
        document.getElementById('bookAuthorId').value = book.authorId || '';
        document.getElementById('bookCategoryId').value = book.categoryId || '';

        bookModalInstance.show();
    } catch (err) {
        alert(`เกิดข้อผิดพลาด: ${err.message}`);
    }
}

function clearModalErrors() {
    const alertBox = document.getElementById('modalAlert');
    alertBox.innerHTML = '';
    alertBox.classList.add('d-none');
}

/**
 * ดักจับและแยกแยะข้อผิดพลาดจาก ASP.NET Core Validation (ProblemDetails)
 * @param {object} errData 
 */
function displayModalErrors(errData) {
    const alertBox = document.getElementById('modalAlert');
    let errorMessages = [];

    // กรณี ValidationProblemDetails (เช่น Data Annotations ไม่ผ่าน)
    if (errData && errData.errors) {
        for (const [key, messages] of Object.entries(errData.errors)) {
            if (Array.isArray(messages)) {
                messages.forEach(msg => errorMessages.push(msg));
            } else {
                errorMessages.push(messages);
            }
        }
    } else if (errData && errData.message) {
        // กรณีข้อความ Conflict หรือ BadRequest ที่ส่งมาจาก Controller
        errorMessages.push(errData.message);
    } else if (errData && errData.title) {
        errorMessages.push(errData.title);
    } else {
        errorMessages.push('เกิดข้อผิดพลาดในการบันทึกข้อมูล กรุณาตรวจสอบความถูกต้อง');
    }

    alertBox.innerHTML = `
        <div class="fw-bold mb-1"><i class="bi bi-exclamation-octagon me-1"></i>พบข้อผิดพลาด:</div>
        <ul class="mb-0 ps-3">
            ${errorMessages.map(msg => `<li>${escapeHtml(msg)}</li>`).join('')}
        </ul>`;
    alertBox.classList.remove('d-none');
}

// ========================================================
// 5. บันทึกข้อมูลไปยัง API (POST สำหรับเพิ่ม / PUT สำหรับแก้ไข)
// ========================================================

async function handleSaveBook(e) {
    e.preventDefault();
    clearModalErrors();

    const bookId = document.getElementById('bookId').value;
    const isEdit = Boolean(bookId);

    // ดึงค่าฟิลด์ต่างๆ ให้ตรงตาม CreateBookDto / UpdateBookDto
    const publishedDateVal = document.getElementById('bookPublishedDate').value;
    const payload = {
        title: document.getElementById('bookTitle').value.trim(),
        isbn: document.getElementById('bookIsbn').value.trim(),
        price: parseFloat(document.getElementById('bookPrice').value),
        publishedDate: publishedDateVal ? new Date(publishedDateVal).toISOString() : null,
        fileUrl: document.getElementById('bookFileUrl').value.trim() || null,
        authorId: parseInt(document.getElementById('bookAuthorId').value, 10),
        categoryId: parseInt(document.getElementById('bookCategoryId').value, 10)
    };

    const url = isEdit ? `${API_BASE}/Books/${bookId}` : `${API_BASE}/Books`;
    const method = isEdit ? 'PUT' : 'POST';

    const saveBtn = document.getElementById('btnSaveBook');
    const originalText = saveBtn.innerHTML;
    saveBtn.disabled = true;
    saveBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>กำลังบันทึก...';

    try {
        const response = await fetch(url, {
            method: method,
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            bookModalInstance.hide();
            showToast(isEdit ? 'แก้ไขข้อมูลหนังสือสำเร็จเรียบร้อย' : 'เพิ่มหนังสือใหม่สำเร็จเรียบร้อย');
            loadBooks(isEdit ? currentPage : 1);
        } else {
            const errData = await response.json().catch(() => null);
            displayModalErrors(errData);
        }
    } catch (err) {
        displayModalErrors({ message: `การเชื่อมต่อเซิร์ฟเวอร์ล้มเหลว: ${err.message}` });
    } finally {
        saveBtn.disabled = false;
        saveBtn.innerHTML = originalText;
    }
}

// ========================================================
// 6. ลบข้อมูลหนังสือ (DELETE) พร้อมข้อความยืนยัน
// ========================================================

async function deleteBook(id, title) {
    const isConfirmed = confirm(`คุณต้องการลบหนังสือ:\n"${title}" (ID: ${id}) ใช่หรือไม่?`);
    if (!isConfirmed) return;

    try {
        const response = await fetch(`${API_BASE}/Books/${id}`, {
            method: 'DELETE'
        });

        if (response.ok) {
            showToast('ลบข้อมูลหนังสือสำเร็จเรียบร้อย');
            loadBooks(currentPage);
        } else {
            const errData = await response.json().catch(() => null);
            alert(`ไม่สามารถลบข้อมูลได้: ${errData?.message || 'เกิดข้อผิดพลาดจากเซิร์ฟเวอร์'}`);
        }
    } catch (err) {
        alert(`เกิดข้อผิดพลาดในการเชื่อมต่อ: ${err.message}`);
    }
}

// ========================================================
// 7. ผูก Event Listeners เมื่อ DOM โหลดเสร็จ
// ========================================================

document.addEventListener('DOMContentLoaded', () => {
    // กำหนด Bootstrap Modal Instance
    const modalEl = document.getElementById('bookModal');
    bookModalInstance = new bootstrap.Modal(modalEl);

    // ผูก Event ค้นหาและปุ่มรีเซ็ต
    document.getElementById('btnSearch').addEventListener('click', () => {
        currentSearch = document.getElementById('searchInput').value.trim();
        currentCategoryId = document.getElementById('categoryFilter').value;
        loadBooks(1);
    });

    document.getElementById('searchInput').addEventListener('keyup', (e) => {
        if (e.key === 'Enter') {
            currentSearch = e.target.value.trim();
            loadBooks(1);
        }
    });

    document.getElementById('categoryFilter').addEventListener('change', (e) => {
        currentCategoryId = e.target.value;
        loadBooks(1);
    });

    document.getElementById('btnReset').addEventListener('click', () => {
        document.getElementById('searchInput').value = '';
        document.getElementById('categoryFilter').value = '';
        currentSearch = '';
        currentCategoryId = '';
        loadBooks(1);
    });

    // ผูก Event ปุ่มเปิด Modal เพิ่มหนังสือใหม่
    document.getElementById('btnOpenCreate').addEventListener('click', openCreateModal);

    // ผูก Event การส่งฟอร์ม (Submit)
    document.getElementById('bookForm').addEventListener('submit', handleSaveBook);

    // โหลดข้อมูลเริ่มต้น
    loadCategories();
    loadAuthors();
    loadBooks(1);
});
