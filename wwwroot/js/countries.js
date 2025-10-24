// قائمة الدول المدعومة في النظام
// هذا الملف هو المصدر الوحيد لقائمة الدول (Single Source of Truth)
const COUNTRIES = [
    { value: "Jordan", label: "الأردن", currency: "JOD" },
    { value: "Saudi Arabia", label: "السعودية", currency: "SAR" },
    { value: "UAE", label: "الإمارات", currency: "AED" },
    { value: "Turkey", label: "تركيا", currency: "TRY" },
    { value: "Egypt", label: "مصر", currency: "EGP" },
    { value: "Lebanon", label: "لبنان", currency: "LBP" },
    { value: "Syria", label: "سوريا", currency: "SYP" },
    { value: "Iraq", label: "العراق", currency: "IQD" },
    { value: "Palestine", label: "فلسطين", currency: "ILS" },
    { value: "Kuwait", label: "الكويت", currency: "KWD" },
    { value: "Qatar", label: "قطر", currency: "QAR" },
    { value: "Bahrain", label: "البحرين", currency: "BHD" },
    { value: "Oman", label: "عمان", currency: "OMR" },
    { value: "Yemen", label: "اليمن", currency: "YER" },
    { value: "Sudan", label: "السودان", currency: "SDG" },
    { value: "Morocco", label: "المغرب", currency: "MAD" },
    { value: "Algeria", label: "الجزائر", currency: "DZD" },
    { value: "Tunisia", label: "تونس", currency: "TND" },
    { value: "Libya", label: "ليبيا", currency: "LYD" },
    { value: "Pakistan", label: "باكستان", currency: "PKR" },
    { value: "India", label: "الهند", currency: "INR" },
    { value: "Bangladesh", label: "بنغلاديش", currency: "BDT" },
    { value: "Philippines", label: "الفلبين", currency: "PHP" },
    { value: "Sri Lanka", label: "سريلانكا", currency: "LKR" },
    { value: "Nepal", label: "نيبال", currency: "NPR" },
    { value: "Ethiopia", label: "إثيوبيا", currency: "ETB" },
    { value: "Somalia", label: "الصومال", currency: "SOS" },
    { value: "Kenya", label: "كينيا", currency: "KES" },
    { value: "Ghana", label: "غانا", currency: "GHS" },
    { value: "Nigeria", label: "نيجيريا", currency: "NGN" }
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


