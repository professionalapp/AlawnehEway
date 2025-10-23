// متغيرات عامة
const API_BASE_URL = 'http://localhost:5216';
let users = [];

// تهيئة التطبيق عند تحميل الصفحة
document.addEventListener('DOMContentLoaded', function() {
    initializeApp();
});

// تهيئة التطبيق
function initializeApp() {
    // تحميل المستخدمين عند فتح الصفحة
    loadUsers();
    
    // ربط نموذج إضافة المستخدم
    const userForm = document.getElementById('userForm');
    if (userForm) {
        userForm.addEventListener('submit', handleAddUser);
    }
}

// تحميل جميع المستخدمين من API
async function loadUsers() {
    try {
        showLoading();
        const response = await fetch(`${API_BASE_URL}/users`);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        users = await response.json();
        displayUsers(users);
        hideLoading();
    } catch (error) {
        console.error('خطأ في تحميل المستخدمين:', error);
        showMessage('خطأ في تحميل المستخدمين. تأكد من أن الخادم يعمل.', 'error');
        hideLoading();
    }
}

// عرض المستخدمين في الصفحة
function displayUsers(usersList) {
    const usersListContainer = document.getElementById('usersList');
    
    if (!usersList || usersList.length === 0) {
        usersListContainer.innerHTML = `
            <div class="no-users">
                <i class="fas fa-users-slash"></i>
                <h3>لا يوجد مستخدمين</h3>
                <p>لم يتم إضافة أي مستخدمين بعد. استخدم النموذج أعلاه لإضافة مستخدم جديد.</p>
            </div>
        `;
        return;
    }
    
    const usersHTML = usersList.map(user => `
        <div class="user-card" data-user-id="${user.id}">
            <div class="user-info">
                <div class="user-avatar">
                    ${getUserInitials(user.name)}
                </div>
                <div class="user-details">
                    <h3>${escapeHtml(user.name)}</h3>
                    <p><i class="fas fa-envelope"></i> ${escapeHtml(user.email)}</p>
                    <p><i class="fas fa-id-badge"></i> رقم المستخدم: ${user.id}</p>
                    ${user.department ? `<p><i class="fas fa-building"></i> ${escapeHtml(user.department)}</p>` : ''}
                    ${user.birthDate ? `<p><i class="fas fa-calendar"></i> ${formatDate(user.birthDate)}</p>` : ''}
                    ${user.phoneNumber ? `<p><i class="fas fa-phone"></i> ${escapeHtml(user.phoneNumber)}</p>` : ''}
                </div>
            </div>
        </div>
    `).join('');
    
    usersListContainer.innerHTML = usersHTML;
}

// معالجة إضافة مستخدم جديد
async function handleAddUser(event) {
    event.preventDefault();
    
    const formData = new FormData(event.target);
    const userData = {
        name: formData.get('name').trim(),
        email: formData.get('email').trim(),
        department: formData.get('department').trim(),
        birthDate: formData.get('birthDate'),
        phoneNumber: formData.get('phoneNumber').trim()
    };
    
    // التحقق من صحة البيانات
    if (!userData.name || !userData.email || !userData.department || !userData.birthDate) {
        showMessage('يرجى ملء جميع الحقول المطلوبة.', 'error');
        return;
    }
    
    if (!isValidEmail(userData.email)) {
        showMessage('يرجى إدخال بريد إلكتروني صحيح.', 'error');
        return;
    }
    
    try {
        showMessage('جاري إضافة المستخدم...', 'success');
        
        const response = await fetch(`${API_BASE_URL}/users`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(userData)
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const newUser = await response.json();
        
        // إضافة المستخدم الجديد إلى القائمة
        users.unshift(newUser);
        displayUsers(users);
        
        // مسح النموذج
        event.target.reset();
        
        showMessage(`تم إضافة المستخدم "${newUser.name}" بنجاح!`, 'success');
        
        // التمرير إلى قائمة المستخدمين
        setTimeout(() => {
            document.getElementById('usersList').scrollIntoView({ 
                behavior: 'smooth',
                block: 'start'
            });
        }, 1000);
        
    } catch (error) {
        console.error('خطأ في إضافة المستخدم:', error);
        showMessage('خطأ في إضافة المستخدم. تأكد من أن الخادم يعمل.', 'error');
    }
}

// عرض رسالة للمستخدم
function showMessage(message, type = 'success') {
    const messageContainer = document.getElementById('messageContainer');
    const messageElement = document.createElement('div');
    messageElement.className = `message ${type}`;
    messageElement.innerHTML = `
        <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'}"></i>
        ${message}
    `;
    
    messageContainer.innerHTML = '';
    messageContainer.appendChild(messageElement);
    
    // إخفاء الرسالة بعد 5 ثوان
    setTimeout(() => {
        messageElement.remove();
    }, 5000);
}

// عرض مؤشر التحميل
function showLoading() {
    const usersListContainer = document.getElementById('usersList');
    usersListContainer.innerHTML = `
        <div class="loading">
            <i class="fas fa-spinner fa-spin"></i>
            جاري تحميل المستخدمين...
        </div>
    `;
}

// إخفاء مؤشر التحميل
function hideLoading() {
    // سيتم استبدال هذا في displayUsers
}

// الحصول على الأحرف الأولى من الاسم
function getUserInitials(name) {
    if (!name) return '?';
    const words = name.trim().split(' ');
    if (words.length >= 2) {
        return (words[0][0] + words[1][0]).toUpperCase();
    }
    return name[0].toUpperCase();
}

// التحقق من صحة البريد الإلكتروني
function isValidEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

// تنظيف النص من HTML
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// تنسيق التاريخ
function formatDate(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    if (isNaN(date.getTime())) return dateString;
    
    // إضافة 3 ساعات لتصحيح المنطقة الزمنية (UTC+3)
    date.setHours(date.getHours() + 3);
    
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    
    let hours = date.getHours();
    const minutes = date.getMinutes().toString().padStart(2, '0');
            const ampm = hours >= 12 ? 'PM' : 'AM';
    
    hours = hours % 12;
    hours = hours ? hours : 12;
    const strHours = hours.toString().padStart(2, '0');
    
    return `${month}/${day}/${year} ${strHours}:${minutes} ${ampm}`;
}

// تحويل حالة الحوالة إلى نص عربي موحد عبر جميع الصفحات
function mapStatusToArabic(status) {
    switch (status) {
        case 'Payment pending': return 'بانتظار الدفع';
        case 'Paid': return 'مدفوعة';
        default: return status;
    }
}

function printReceipt(remittance, type) {
    const logoSrc = 'images/alawneh.png';
    // استخدام التاريخ الميلادي بدلاً من الهجري
    const receiptDate = new Date().toLocaleDateString('ar-SA', { 
        year: 'numeric', 
        month: 'long', 
        day: 'numeric', 
        hour: '2-digit', 
        minute: '2-digit',
        calendar: 'gregory' // استخدام التقويم الميلادي
    });
    const voucherType = type === 'send' ? 'مستند قبض' : 'مستند صرف';

    const receiptContent = `
        <div style="font-family: 'Arial', sans-serif; direction: rtl; text-align: right; padding: 25px; border: 2px solid #007bff; width: 90mm; margin: 0 auto; background: #ffffff;">
            <div style="text-align: center; margin-bottom: 25px;">
                <img src="${logoSrc}" alt="Alawneh Logo" style="width: 180px; height: auto; display: block; margin: 0 auto;">
                <h2 style="color: #007bff; margin-top: 15px; font-size: 24px; font-weight: bold;">شركة العلاونة للصرافة</h2>
                <p style="font-size: 18px; color: #007bff; font-weight: bold; margin: 10px 0;">${voucherType}</p>
            </div>
            
            <div style="background: #ffffff; padding: 15px; border-radius: 8px; margin-bottom: 20px; border: 1px solid #dee2e6;">
                <div style="margin-bottom: 12px; font-size: 16px;">
                    <strong style="color: #6c757d; font-size: 18px;">التاريخ:</strong> 
                    <span style="font-size: 18px; color: #6c757d; font-weight: bold;">${receiptDate}</span>
                </div>
                <div style="margin-bottom: 12px; font-size: 16px;">
                    <strong style="color: #6c757d; font-size: 18px;">الرقم المرجعي:</strong> 
                    <span style="font-size: 18px; color: #6c757d; font-weight: bold;">${remittance.reference}</span>
                </div>
                <div style="font-size: 16px;">
                    <strong style="color: #6c757d; font-size: 18px;">الحالة:</strong> 
                    <span style="font-size: 18px; color: #6c757d; font-weight: bold;">${mapStatusToArabic(remittance.status)}</span>
                </div>
            </div>
            
            <hr style="border: none; border-top: 2px solid #007bff; margin: 20px 0;">
            
            <div style="background: #ffffff; padding: 15px; border-radius: 8px; margin-bottom: 20px; border: 1px solid #dee2e6;">
                <div style="margin-bottom: 15px; font-size: 16px;">
                    <strong style="color: #6c757d; font-size: 18px;">المرسل:</strong> 
                    <div style="font-size: 18px; color: #6c757d; font-weight: bold; margin-top: 5px;">
                        ${remittance.sender?.nameAr ?? 'غير محدد'} (${remittance.sender?.nameEn ?? 'غير محدد'})
                    </div>
                </div>
                <div style="margin-bottom: 15px; font-size: 16px;">
                    <strong style="color: #6c757d; font-size: 18px;">المستفيد:</strong> 
                    <div style="font-size: 18px; color: #6c757d; font-weight: bold; margin-top: 5px;">
                        ${remittance.beneficiary?.nameAr ?? 'غير محدد'} (${remittance.beneficiary?.nameEn ?? 'غير محدد'})
                    </div>
                </div>
            </div>
            
            <hr style="border: none; border-top: 2px solid #007bff; margin: 20px 0;">
            
            <div style="background: #ffffff; padding: 15px; border-radius: 8px; margin-bottom: 20px; border: 1px solid #dee2e6;">
                <div style="margin-bottom: 12px; font-size: 16px;">
                    <strong style="color: #6c757d; font-size: 18px;">المبلغ:</strong> 
                    <span style="font-size: 20px; color: #6c757d; font-weight: bold;">${remittance.amount.toFixed(2)} د.أ</span>
                </div>
                <div style="margin-bottom: 12px; font-size: 16px;">
                    <strong style="color: #6c757d; font-size: 18px;">العمولة:</strong> 
                    <span style="font-size: 20px; color: #6c757d; font-weight: bold;">${remittance.fee?.toFixed(2) ?? '0.00'} د.أ</span>
                </div>
                <div style="font-size: 18px; background: #6c757d; color: white; padding: 10px; border-radius: 6px; text-align: center;">
                    <strong style="font-size: 22px;">الإجمالي: ${((remittance.amount || 0) + (remittance.fee || 0)).toFixed(2)} د.أ</strong>
                </div>
            </div>
            
            <div style="text-align: center; margin-top: 30px; font-size: 16px; color: #6c757d; font-weight: bold; background: #ffffff; padding: 15px; border-radius: 8px; border: 1px solid #dee2e6;">
                شكراً لاختياركم شركة العلاونة للصرافة
            </div>
        </div>
    `;

    const printWindow = window.open('', '_blank');
    printWindow.document.write(receiptContent);
    printWindow.document.close();
    printWindow.print();
}

function onChange(callback) {
    clearTimeout(window.timer);
    window.timer = setTimeout(callback, 400);
}

// تحديث الصفحة كل 30 ثانية لضمان الحصول على أحدث البيانات
setInterval(() => {
    if (document.visibilityState === 'visible') {
        loadUsers();
    }
}, 30000);
