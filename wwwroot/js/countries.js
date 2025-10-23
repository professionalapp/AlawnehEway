// قائمة الدول المدعومة في النظام
// هذا الملف هو المصدر الوحيد لقائمة الدول (Single Source of Truth)
const COUNTRIES = [
    { value: "Jordan", label: "الأردن", currency: "JOD" },
    { value: "Saudi Arabia", label: "السعودية", currency: "SAR" },
    { value: "UAE", label: "الإمارات", currency: "AED" },
    { value: "Turkey", label: "تركيا", currency: "TRY" },
    { value: "Egypt", label: "مصر", currency: "EGP" }
];

// دالة لملء dropdown الدول
function populateCountryDropdown(selectElementId, includeEmptyOption = true) {
    const selectElement = document.getElementById(selectElementId);
    if (!selectElement) {
        console.error(`Element with id "${selectElementId}" not found`);
        return;
    }

    // مسح الخيارات الموجودة
    selectElement.innerHTML = '';

    // إضافة خيار فارغ إذا كان مطلوباً
    if (includeEmptyOption) {
        const emptyOption = document.createElement('option');
        emptyOption.value = '';
        emptyOption.textContent = 'اختر الدولة';
        selectElement.appendChild(emptyOption);
    }

    // إضافة جميع الدول
    COUNTRIES.forEach(country => {
        const option = document.createElement('option');
        option.value = country.value;
        option.textContent = country.label;
        option.setAttribute('data-currency', country.currency);
        selectElement.appendChild(option);
    });
}

// دالة للحصول على معلومات دولة معينة
function getCountryInfo(countryValue) {
    return COUNTRIES.find(c => c.value === countryValue);
}

// دالة للحصول على اسم الدولة بالعربية
function getCountryLabel(countryValue) {
    const country = getCountryInfo(countryValue);
    return country ? country.label : countryValue;
}

// دالة للحصول على العملة
function getCountryCurrency(countryValue) {
    const country = getCountryInfo(countryValue);
    return country ? country.currency : '';
}


