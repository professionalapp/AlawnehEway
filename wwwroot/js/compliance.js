const API_BASE_URL = window.location.origin;

async function loadComplianceHolds() {
    const container = document.getElementById('holdsContainer');
    container.innerHTML = `<div class="loading"><i class="fas fa-spinner fa-spin"></i> جاري تحميل الحوالات الموقوفة...</div>`;
    try {
        const res = await fetch(`${API_BASE_URL}/compliance/holds`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const holds = await res.json();
        if (holds.length === 0) {
            container.innerHTML = `<div class="no-users"><i class="fas fa-check-circle"></i><h3>لا توجد حوالات موقوفة</h3></div>`;
            document.getElementById('thresholdInfo').textContent = '';
            return;
        }

        // إظهار حدود الالتزام من أول عنصر (نفس القيم للجميع)
        const thr = holds[0].thresholds;
        if (thr) {
            document.getElementById('thresholdInfo').textContent = `حد الإرسال: ${Number(thr.outgoing).toLocaleString('en-US')} ، حد الاستلام: ${Number(thr.incoming).toLocaleString('en-US')}`;
        }

        const cards = holds.map(h => renderHoldCard(h)).join('');
        container.innerHTML = cards;
    } catch (e) {
        container.innerHTML = `<div class="message error"><i class="fas fa-exclamation-circle"></i> حدث خطأ في تحميل القائمة</div>`;
        console.error(e);
    }
}

function renderHoldCard(h) {
    const reasons = (h.reasons && h.reasons.length) ? h.reasons.map(r => `<span style="background: #dc3545; color: white; padding: 4px 10px; border-radius: 6px; font-size: 13px; display: inline-block; margin-left: 6px;"><i class="fas fa-exclamation-triangle"></i> ${r}</span>`).join('') : '<span style="background: #dc3545; color: white; padding: 4px 10px; border-radius: 6px; font-size: 13px;"><i class="fas fa-exclamation-triangle"></i> تجاوز الحدود</span>';
    
    const createdDate = h.createdAt ? formatDate(h.createdAt) : '-';
    
    return `
    <div style="background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%); border: 1px solid #dee2e6; border-radius: 12px; padding: 20px; margin-bottom: 16px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); position: relative;">
        <!-- Header Section -->
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; padding-bottom: 12px; border-bottom: 2px solid #ced4da;">
            <div>
                <h3 style="margin: 0; color: #495057; font-size: 18px; font-weight: 600;">
                    <i class="fas fa-file-invoice-dollar" style="color: #6c757d; margin-left: 8px;"></i>
                    حوالة رقم: <span style="color: #007bff; font-weight: 700;">${h.reference}</span>
                </h3>
                <div style="color: #6c757d; font-size: 13px; margin-top: 4px;">
                    <i class="fas fa-calendar"></i> تاريخ الإنشاء: ${createdDate}
                </div>
            </div>
            <button class="btn btn-primary" onclick="releaseHold(${h.id})" style="background: linear-gradient(135deg, #28a745 0%, #20c997 100%); border: none; padding: 10px 20px; border-radius: 8px; font-weight: 600; box-shadow: 0 4px 12px rgba(40, 167, 69, 0.3); transition: all 0.3s ease;">
                <i class="fas fa-unlock"></i> فك الإيقاف
            </button>
        </div>

        <!-- Financial Details -->
        <div style="display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; margin-bottom: 16px;">
            <!-- Amount Card -->
            <div style="background: white; padding: 14px; border-radius: 8px; border-right: 4px solid #007bff; box-shadow: 0 1px 4px rgba(0,0,0,0.05);">
                <div style="color: #6c757d; font-size: 13px; margin-bottom: 4px; font-weight: 500;">
                    <i class="fas fa-money-bill-wave"></i> المبلغ
                </div>
                <div style="color: #212529; font-size: 22px; font-weight: 700;">
                    ${Number(h.amount).toLocaleString('en-US', {minimumFractionDigits:2})} <span style="font-size: 16px; color: #6c757d;">د.أ</span>
                </div>
            </div>

            <!-- Fee Card -->
            <div style="background: white; padding: 14px; border-radius: 8px; border-right: 4px solid #6f42c1; box-shadow: 0 1px 4px rgba(0,0,0,0.05);">
                <div style="color: #6c757d; font-size: 13px; margin-bottom: 4px; font-weight: 500;">
                    <i class="fas fa-percent"></i> العمولة
                </div>
                <div style="color: #212529; font-size: 22px; font-weight: 700;">
                    ${Number(h.fee || 0).toLocaleString('en-US', {minimumFractionDigits:2})} <span style="font-size: 16px; color: #6c757d;">د.أ</span>
                </div>
            </div>

            <!-- Country Card -->
            <div style="background: white; padding: 14px; border-radius: 8px; border-right: 4px solid #17a2b8; box-shadow: 0 1px 4px rgba(0,0,0,0.05);">
                <div style="color: #6c757d; font-size: 13px; margin-bottom: 4px; font-weight: 500;">
                    <i class="fas fa-globe"></i> الدولة
                </div>
                <div style="color: #212529; font-size: 18px; font-weight: 600;">
                    ${escapeHtml(h.country || '-')}
                </div>
            </div>
        </div>

        <!-- Parties Section -->
        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 16px;">
            <!-- Sender Card -->
            <div style="background: white; padding: 16px; border-radius: 8px; border-right: 4px solid #28a745; box-shadow: 0 1px 4px rgba(0,0,0,0.05);">
                <div style="color: #6c757d; font-size: 13px; margin-bottom: 8px; font-weight: 600; border-bottom: 1px solid #e9ecef; padding-bottom: 6px;">
                    <i class="fas fa-user-check"></i> بيانات المرسل
                </div>
                <div style="margin-bottom: 8px;">
                    <div style="color: #6c757d; font-size: 12px;">الاسم بالعربية</div>
                    <div style="color: #212529; font-size: 15px; font-weight: 600;">
                        ${escapeHtml(h.sender?.nameAr || '-')}
                    </div>
                </div>
                <div style="margin-bottom: 8px;">
                    <div style="color: #6c757d; font-size: 12px;">الاسم بالإنجليزية</div>
                    <div style="color: #212529; font-size: 14px; font-weight: 500;">
                        ${escapeHtml(h.sender?.nameEn || '-')}
                    </div>
                </div>
                <div style="margin-bottom: 8px;">
                    <div style="color: #6c757d; font-size: 12px;"><i class="fas fa-id-card"></i> الرقم الوطني</div>
                    <div style="color: #212529; font-size: 14px; font-weight: 500;">
                        ${escapeHtml(h.sender?.nationalId || '-')}
                    </div>
                </div>
                <div>
                    <div style="color: #6c757d; font-size: 12px;"><i class="fas fa-phone"></i> رقم الهاتف</div>
                    <div style="color: #212529; font-size: 14px; font-weight: 500; direction: ltr; text-align: right;">
                        ${escapeHtml(h.sender?.phoneNumber || '-')}
                    </div>
                </div>
            </div>

            <!-- Beneficiary Card -->
            <div style="background: white; padding: 16px; border-radius: 8px; border-right: 4px solid #ffc107; box-shadow: 0 1px 4px rgba(0,0,0,0.05);">
                <div style="color: #6c757d; font-size: 13px; margin-bottom: 8px; font-weight: 600; border-bottom: 1px solid #e9ecef; padding-bottom: 6px;">
                    <i class="fas fa-user-tag"></i> بيانات المستفيد
                </div>
                <div style="margin-bottom: 8px;">
                    <div style="color: #6c757d; font-size: 12px;">الاسم بالعربية</div>
                    <div style="color: #212529; font-size: 15px; font-weight: 600;">
                        ${escapeHtml(h.beneficiary?.nameAr || '-')}
                    </div>
                </div>
                <div style="margin-bottom: 8px;">
                    <div style="color: #6c757d; font-size: 12px;">الاسم بالإنجليزية</div>
                    <div style="color: #212529; font-size: 14px; font-weight: 500;">
                        ${escapeHtml(h.beneficiary?.nameEn || '-')}
                    </div>
                </div>
                <div style="margin-bottom: 8px;">
                    <div style="color: #6c757d; font-size: 12px;"><i class="fas fa-id-card"></i> الرقم الوطني</div>
                    <div style="color: #212529; font-size: 14px; font-weight: 500;">
                        ${escapeHtml(h.beneficiary?.nationalId || '-')}
                    </div>
                </div>
                <div>
                    <div style="color: #6c757d; font-size: 12px;"><i class="fas fa-phone"></i> رقم الهاتف</div>
                    <div style="color: #212529; font-size: 14px; font-weight: 500; direction: ltr; text-align: right;">
                        ${escapeHtml(h.beneficiary?.phoneNumber || '-')}
                    </div>
                </div>
            </div>
        </div>

        <!-- Additional Details -->
        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 16px;">
            <!-- Reason & Purpose -->
            <div style="background: white; padding: 14px; border-radius: 8px; border-right: 4px solid #fd7e14; box-shadow: 0 1px 4px rgba(0,0,0,0.05);">
                <div style="color: #6c757d; font-size: 13px; margin-bottom: 6px; font-weight: 500;">
                    <i class="fas fa-comment-dots"></i> سبب الحوالة
                </div>
                <div style="color: #212529; font-size: 14px; font-weight: 500;">
                    ${escapeHtml(h.reason || '-')}
                </div>
            </div>

            <div style="background: white; padding: 14px; border-radius: 8px; border-right: 4px solid #e83e8c; box-shadow: 0 1px 4px rgba(0,0,0,0.05);">
                <div style="color: #6c757d; font-size: 13px; margin-bottom: 6px; font-weight: 500;">
                    <i class="fas fa-bullseye"></i> غاية الحوالة
                </div>
                <div style="color: #212529; font-size: 14px; font-weight: 500;">
                    ${escapeHtml(h.purpose || '-')}
                </div>
            </div>
        </div>

        <!-- Status Info -->
        <div style="background: white; padding: 14px; border-radius: 8px; border-right: 4px solid #6c757d; box-shadow: 0 1px 4px rgba(0,0,0,0.05); margin-bottom: 16px;">
            <div style="display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px;">
                <div>
                    <div style="color: #6c757d; font-size: 12px; margin-bottom: 4px;">
                        <i class="fas fa-info-circle"></i> الحالة
                    </div>
                    <div style="color: #dc3545; font-size: 14px; font-weight: 600;">
                        ${escapeHtml(h.status || '-')}
                    </div>
                </div>
                <div>
                    <div style="color: #6c757d; font-size: 12px; margin-bottom: 4px;">
                        <i class="fas fa-hashtag"></i> رقم الحوالة (ID)
                    </div>
                    <div style="color: #212529; font-size: 14px; font-weight: 600;">
                        ${h.id}
                    </div>
                </div>
                <div>
                    <div style="color: #6c757d; font-size: 12px; margin-bottom: 4px;">
                        <i class="fas fa-coins"></i> الإجمالي
                    </div>
                    <div style="color: #212529; font-size: 16px; font-weight: 700;">
                        ${Number((h.amount || 0) + (h.fee || 0)).toLocaleString('en-US', {minimumFractionDigits:2})} <span style="font-size: 13px; color: #6c757d;">د.أ</span>
                    </div>
                </div>
            </div>
        </div>

        <!-- Compliance Reasons -->
        <div style="background: white; padding: 14px; border-radius: 8px; border: 2px solid #dc3545; box-shadow: 0 1px 4px rgba(0,0,0,0.05);">
            <div style="color: #212529; font-size: 14px; font-weight: 600; margin-bottom: 10px;">
                <i class="fas fa-shield-halved" style="color: #dc3545; margin-left: 6px;"></i> أسباب الإيقاف
            </div>
            <div style="display: flex; flex-wrap: wrap; gap: 8px;">
                ${reasons}
            </div>
        </div>
    </div>`;
}

function formatDate(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    if (isNaN(date.getTime())) return dateString;
    
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

async function releaseHold(id) {
    if (!confirm('هل تريد فك إيقاف هذه الحوالة؟')) return;
    try {
        const releasedByUserId = localStorage.getItem('userId') || sessionStorage.getItem('userId') || '';
        const releasedByUsername = localStorage.getItem('username') || sessionStorage.getItem('username') || '';
        const url = new URL(`${API_BASE_URL}/compliance/release/${id}`);
        if (releasedByUserId) url.searchParams.set('releasedByUserId', releasedByUserId);
        if (releasedByUsername) url.searchParams.set('releasedByUsername', releasedByUsername);
        const res = await fetch(url.toString(), { method: 'POST' });
        if (!res.ok) throw new Error(await res.text());
        await loadComplianceHolds();
        alert('تم فك الإيقاف وإعادة الحالة إلى بانتظار الدفع');
    } catch (e) {
        console.error(e);
        alert('تعذر فك الإيقاف');
    }
}

function promptForcePay(reference) {
    const receiverUserId = prompt('أدخل معرف الصندوق المستقبل (UserId):');
    if (!receiverUserId) return;
    forcePay(reference, parseInt(receiverUserId, 10));
}

async function forcePay(reference, receiverUserId) {
    try {
        const url = new URL(`${API_BASE_URL}/compliance/force-pay`);
        url.searchParams.set('reference', reference);
        url.searchParams.set('receiverUserId', String(receiverUserId));
        const res = await fetch(url.toString(), { method: 'POST' });
        if (!res.ok) throw new Error(await res.text());
        await loadComplianceHolds();
        alert('تم التسليم بنجاح');
    } catch (e) {
        console.error(e);
        alert('تعذر تنفيذ التسليم');
    }
}

function escapeHtml(text) {
    if (text == null) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}


