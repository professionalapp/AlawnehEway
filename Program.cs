using Microsoft.EntityFrameworkCore;
using AlawnehEway.Models;
using System.Globalization;
using System.Security.Cryptography;

// Helper to generate unique reference numbers
string GenerateReference(string prefix, DateTime date)
{
    // Format: PREFIX-ddMMyyyy-HHmmss-milliseconds (e.g., RM-24102025-143059-123)
    return $"{prefix}-{date.ToString("ddMMyyyy-HHmmss-fff")}";
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// إضافة قاعدة البيانات الجديدة لأسعار صرف العملات
builder.Services.AddDbContext<FxDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("FxConnection") ?? builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// إضافة دعم للصفحات الثابتة
app.UseDefaultFiles();
app.UseStaticFiles();

// تطبيق Migrations تلقائياً في Production
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var fxDb = scope.ServiceProvider.GetRequiredService<FxDbContext>();
    if (app.Environment.IsProduction())
    {
        try
        {
            await db.Database.MigrateAsync();
            await fxDb.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration error: {ex.Message}");
        }
    }
}

// تطبيع الدول المخزنة في قاعدة البيانات عند الإقلاع لضمان التوحيد
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        var changed = false;
        // إزالة التكرارات في ExchangeRates عبر اختيار أحدث سجل لكل دولة
        var duplicates = await db.ExchangeRates
            .GroupBy(er => er.Country)
            .SelectMany(g => g.OrderByDescending(x => x.LastModifiedAt ?? x.CreatedAt).Skip(1))
            .ToListAsync();
        if (duplicates.Count > 0)
        {
            db.ExchangeRates.RemoveRange(duplicates);
            changed = true;
        }
        var erList = await db.ExchangeRates.ToListAsync();
        foreach (var er in erList)
        {
            var normalized = NormalizeCountry(er.Country);
            if (!string.Equals(er.Country, normalized, StringComparison.Ordinal))
            {
                er.Country = normalized;
                changed = true;
            }
        }
        var tiersList = await db.FeeTiers.ToListAsync();
        foreach (var t in tiersList)
        {
            var normalized = NormalizeCountry(t.Country);
            if (!string.Equals(t.Country, normalized, StringComparison.Ordinal))
            {
                t.Country = normalized;
                changed = true;
            }
        }
        if (changed)
        {
            await db.SaveChangesAsync();
        }

        // إنشاء مستخدم المدير الافتراضي إذا لم يكن موجوداً
        var adminExists = await db.Users.AnyAsync(u => u.Username == "admin");
        if (!adminExists)
        {
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = HashPassword("123456"),
                Name = "المدير العام",
                Email = "admin@alawneh.com",
                Department = "الإدارة",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                BirthDate = DateTime.UtcNow
            };
            db.Users.Add(adminUser);
            await db.SaveChangesAsync();
        }
    }
    catch
    {
        // تجاهل أي أخطاء في وقت الإقلاع لضمان عدم منع تشغيل التطبيق
    }
}

// إضافة صفحة رئيسية بسيطة
// app.MapGet("/", () => "مرحباً! هذا تطبيق AlawnehEway API. استخدم /swagger لرؤية جميع نقاط النهاية.");

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// توحيد أسماء الدول لاستخدام قيم قياسية (مطابقة لقائمة الواجهة)
string NormalizeCountry(string? input)
{
    if (string.IsNullOrWhiteSpace(input)) return string.Empty;
    var value = input.Trim();
    var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Jordan", "Jordan" }, { "الأردن", "Jordan" },
        { "Saudi Arabia", "Saudi Arabia" }, { "السعودية", "Saudi Arabia" }, { "KSA", "Saudi Arabia" },
        { "UAE", "UAE" }, { "الإمارات", "UAE" }, { "United Arab Emirates", "UAE" },
        { "Turkey", "Turkey" }, { "تركيا", "Turkey" },
        { "Egypt", "Egypt" }, { "مصر", "Egypt" }
    };
    return map.TryGetValue(value, out var canonical) ? canonical : value;
}

// دالة مساعدة للحصول على التسمية العربية للدولة
string? GetArabicLabelForCanonical(string canonical)
{
    return canonical switch
    {
        "Jordan" => "الأردن",
        "Saudi Arabia" => "السعودية",
        "UAE" => "الإمارات",
        "Turkey" => "تركيا",
        "Egypt" => "مصر",
        _ => null
    };
}

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// إنشاء المدير يدوياً (استخدم هذا مرة واحدة فقط)
app.MapPost("/auth/create-admin", async (ApplicationDbContext db) =>
{
    var adminExists = await db.Users.AnyAsync(u => u.Username == "admin");
    if (adminExists)
    {
        return Results.BadRequest("المدير موجود بالفعل");
    }

    var adminUser = new User
    {
        Username = "admin",
        PasswordHash = HashPassword("123456"),
        Name = "المدير العام",
        Email = "admin@alawneh.com",
        Department = "الإدارة",
        PhoneNumber = string.Empty,
        Role = "Admin",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        BirthDate = DateTime.UtcNow
    };

    db.Users.Add(adminUser);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "تم إنشاء حساب المدير بنجاح", username = "admin", password = "123456" });
})
.WithName("CreateAdmin")
.WithOpenApi();

// تسجيل الدخول
app.MapPost("/auth/login", async (LoginRequest request, ApplicationDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
    if (user is null)
    {
        return Results.Json(new { error = "اسم المستخدم أو كلمة المرور غير صحيحة" }, statusCode: 401);
    }

    // التحقق من حالة الحساب
    if (!user.IsActive)
    {
        return Results.Json(new { error = "هذا الصندوق معطل. يرجى التواصل مع المدير" }, statusCode: 403);
    }

    // التحقق من كلمة المرور
    if (!VerifyPassword(request.Password, user.PasswordHash))
    {
        return Results.Json(new { error = "اسم المستخدم أو كلمة المرور غير صحيحة" }, statusCode: 401);
    }

    // تحديث وقت آخر تسجيل دخول
    user.LastLoginAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        userId = user.Id,
        username = user.Username,
        name = user.Name,
        role = user.Role,
        department = user.Department
    });
})
.WithName("Login")
.WithOpenApi();

// إنشاء صندوق جديد (للمدير فقط)
app.MapPost("/admin/create-cashier", async (CreateCashierRequest request, ApplicationDbContext db) =>
{
    // التحقق من عدم وجود اسم المستخدم
    var exists = await db.Users.AnyAsync(u => u.Username == request.CashierId);
    if (exists)
    {
        return Results.BadRequest("معرف الصندوق موجود بالفعل");
    }

    // تحديد الدور المطلوب: Admin / User / Compliance (افتراضي User)
    string role = (request.Role ?? "User").Trim();
    if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(role, "Compliance", StringComparison.OrdinalIgnoreCase))
    {
        role = "User";
    }

    var cashier = new User
    {
        Username = request.CashierId,
        PasswordHash = HashPassword(request.Password),
        Name = request.EmployeeName,
        Email = string.Empty,
        Department = request.Department ?? (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ? "الإدارة" : (string.Equals(role, "Compliance", StringComparison.OrdinalIgnoreCase) ? "العناية الواجبة" : "الصناديق")),
        PhoneNumber = request.PhoneNumber ?? string.Empty,
        BirthDate = DateTime.UtcNow,
        Role = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : (string.Equals(role, "Compliance", StringComparison.OrdinalIgnoreCase) ? "Compliance" : "User"),
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    db.Users.Add(cashier);
    await db.SaveChangesAsync();

    return Results.Created($"/users/{cashier.Id}", new
    {
        userId = cashier.Id,
        cashierId = cashier.Username,
        employeeName = cashier.Name,
        department = cashier.Department,
        role = cashier.Role
    });
})
.WithName("CreateCashier")
.WithOpenApi();

// الحصول على قائمة الصناديق (للمدير فقط)
app.MapGet("/admin/cashiers", async (ApplicationDbContext db) =>
{
    var cashiers = await db.Users
        .Select(u => new
        {
            u.Id,
            u.Username,
            u.Name,
            u.Department,
            u.PhoneNumber,
            u.IsActive,
            u.CreatedAt,
            u.LastLoginAt,
            u.Balance,
            u.InitialBalance,
            u.LastBalanceUpdate,
            u.Role,
            // عدد الحوالات الصادرة (التي تم إرسالها من هذا الصندوق)
            OutgoingRemittances = db.Remittances
                .Where(r => r.SenderUserId == u.Id)
                .Count(),
            // عدد الحوالات الواردة (التي تم سحبها من هذا الصندوق)
            IncomingRemittances = db.Remittances
                .Where(r => r.ReceiverUserId == u.Id)
                .Count()
        })
        .ToListAsync();

    // ترتيب حسب الصلاحية: Admin أولاً، ثم User، ثم Compliance
    var sortedCashiers = cashiers
        .OrderBy(u => u.Role == "Admin" ? 0 : u.Role == "User" ? 1 : 2)
        .ThenByDescending(u => u.CreatedAt)
        .ToList();

    return Results.Ok(sortedCashiers);
})
.WithName("GetCashiers")
.WithOpenApi();

// API للتحقق من بيانات الحوالات في قاعدة البيانات
app.MapGet("/debug/remittances", async (ApplicationDbContext db) =>
{
    var remittances = await db.Remittances
        .OrderByDescending(r => r.Id)
        .Select(r => new
        {
            r.Id,
            r.Reference,
            r.SenderUserId,
            r.ReceiverUserId,
            r.Amount,
            r.Status,
            r.CreatedAt
        })
        .Take(20)
        .ToListAsync();

    return Results.Ok(remittances);
})
.WithName("DebugRemittances")
.WithOpenApi();

// API لحذف جميع الحوالات (للاستخدام في التطوير فقط)
app.MapDelete("/debug/clear-all-remittances", async (ApplicationDbContext db) =>
{
    var remittances = await db.Remittances.ToListAsync();
    var count = remittances.Count;

    db.Remittances.RemoveRange(remittances);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = $"تم حذف {count} حوالة بنجاح" });
})
.WithName("ClearAllRemittances")
.WithOpenApi();

// تحديث حالة الصندوق (تفعيل/تعطيل)
app.MapPut("/admin/cashiers/{id:int}/toggle", async (int id, ApplicationDbContext db) =>
{
    var cashier = await db.Users.FindAsync(id);
    if (cashier is null) return Results.NotFound("الصندوق غير موجود");
    if (cashier.Role == "Admin") return Results.BadRequest("لا يمكن تعديل حساب المدير");

    cashier.IsActive = !cashier.IsActive;
    await db.SaveChangesAsync();
    return Results.Ok(new { userId = cashier.Id, isActive = cashier.IsActive });
})
.WithName("ToggleCashier")
.WithOpenApi();

// حذف صندوق
app.MapDelete("/admin/cashiers/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var cashier = await db.Users.FindAsync(id);
    if (cashier is null) return Results.NotFound("الصندوق غير موجود");
    if (cashier.Role == "Admin") return Results.BadRequest("لا يمكن حذف حساب المدير");

    db.Users.Remove(cashier);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "تم حذف الصندوق بنجاح" });
})
.WithName("DeleteCashier")
.WithOpenApi();

// إعادة تعيين كلمة مرور صندوق
app.MapPost("/admin/cashiers/{id:int}/reset-password", async (int id, ResetPasswordRequest request, ApplicationDbContext db) =>
{
    var cashier = await db.Users.FindAsync(id);
    if (cashier is null) return Results.NotFound("الصندوق غير موجود");
    if (cashier.Role == "Admin") return Results.BadRequest("لا يمكن تعديل حساب المدير");

    cashier.PasswordHash = HashPassword(request.NewPassword);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "تم إعادة تعيين كلمة المرور بنجاح" });
})
.WithName("ResetCashierPassword")
.WithOpenApi();

// تغيير صلاحية صندوق
app.MapPut("/admin/cashiers/{id:int}/change-role", async (int id, ChangeRoleRequest request, ApplicationDbContext db) =>
{
    var cashier = await db.Users.FindAsync(id);
    if (cashier is null) return Results.NotFound("الصندوق غير موجود");

    string newRole = (request.NewRole ?? "User").Trim();
    if (!string.Equals(newRole, "Admin", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(newRole, "Compliance", StringComparison.OrdinalIgnoreCase))
    {
        newRole = "User";
    }

    // تحديث الدور والقسم
    cashier.Role = string.Equals(newRole, "Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" :
                   (string.Equals(newRole, "Compliance", StringComparison.OrdinalIgnoreCase) ? "Compliance" : "User");
    cashier.Department = cashier.Role == "Admin" ? "الإدارة" :
                         (cashier.Role == "Compliance" ? "العناية الواجبة" : "الصناديق");

    await db.SaveChangesAsync();
    return Results.Ok(new { message = "تم تغيير الصلاحية بنجاح", userId = cashier.Id, role = cashier.Role, department = cashier.Department });
})
.WithName("ChangeCashierRole")
.WithOpenApi();

// تغيير كلمة المرور
app.MapPost("/auth/change-password", async (ChangePasswordRequest request, ApplicationDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
    if (user is null)
    {
        return Results.NotFound("المستخدم غير موجود");
    }

    // التحقق من كلمة المرور القديمة
    if (!VerifyPassword(request.OldPassword, user.PasswordHash))
    {
        return Results.BadRequest("كلمة المرور القديمة غير صحيحة");
    }

    user.PasswordHash = HashPassword(request.NewPassword);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "تم تغيير كلمة المرور بنجاح" });
})
.WithName("ChangePassword")
.WithOpenApi();

app.MapPost("/users", async (User user, ApplicationDbContext db) =>
{
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{user.Id}", user);
})
.WithName("CreateUser")
.WithOpenApi();

app.MapGet("/users", async (ApplicationDbContext db) =>
{
    return Results.Ok(await db.Users.ToListAsync());
})
.WithName("GetAllUsers")
.WithOpenApi();

// API لفحص المستخدمين الحاليين مع تفاصيلهم
app.MapGet("/debug/users", async (ApplicationDbContext db) =>
{
    var users = await db.Users
        .Select(u => new
        {
            u.Id,
            u.Username,
            u.Name,
            u.Email,
            u.Department,
            u.Role,
            u.IsActive,
            u.CreatedAt,
            u.LastLoginAt
        })
        .OrderBy(u => u.Id)
        .ToListAsync();

    return Results.Ok(users);
})
.WithName("DebugUsers")
.WithOpenApi();

// تحديث بيانات مستخدم معين
app.MapPut("/users/{id:int}", async (int id, User user, ApplicationDbContext db) =>
{
    var existingUser = await db.Users.FindAsync(id);
    if (existingUser is null) return Results.NotFound("المستخدم غير موجود");

    // تحديث البيانات
    existingUser.Name = user.Name;
    existingUser.Email = user.Email;
    existingUser.Department = user.Department;
    existingUser.BirthDate = user.BirthDate;
    existingUser.PhoneNumber = user.PhoneNumber;

    await db.SaveChangesAsync();
    return Results.Ok(existingUser);
})
.WithName("UpdateUser")
.WithOpenApi();

// API لتحديث اسم مستخدم معين
app.MapPut("/users/{id:int}/name", async (int id, string newName, ApplicationDbContext db) =>
{
    var existingUser = await db.Users.FindAsync(id);
    if (existingUser is null) return Results.NotFound("المستخدم غير موجود");

    existingUser.Name = newName;
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        message = "تم تحديث الاسم بنجاح",
        userId = existingUser.Id,
        oldName = existingUser.Name,
        newName = newName
    });
})
.WithName("UpdateUserName")
.WithOpenApi();

// حذف مستخدم معين
app.MapDelete("/users/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound("المستخدم غير موجود");

    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "تم حذف المستخدم بنجاح" });
})
.WithName("DeleteUser")
.WithOpenApi();

// Parties (Senders/Beneficiaries)
app.MapPost("/parties", async (Party party, ApplicationDbContext db) =>
{
    db.Parties.Add(party);
    await db.SaveChangesAsync();
    return Results.Created($"/parties/{party.Id}", party);
})
.WithName("CreateParty")
.WithOpenApi();

app.MapGet("/parties/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var party = await db.Parties.FindAsync(id);
    return party is null ? Results.NotFound() : Results.Ok(party);
})
.WithName("GetPartyById")
.WithOpenApi();

// بحث عن مرسل/مستفيد بالاسم العربي أو الإنجليزي أو رقم المعرف
app.MapGet("/parties/search", async (string q, ApplicationDbContext db) =>
{
    q = q.Trim();
    var lowered = q.ToLower(CultureInfo.InvariantCulture);
    var results = await db.Parties
        .Where(p => p.NationalId.Contains(q) ||
                    p.NameAr.ToLower().Contains(lowered) ||
                    p.NameEn.ToLower().Contains(lowered))
        .OrderBy(p => p.NameAr)
        .Take(50)
        .ToListAsync();
    return Results.Ok(results);
})
.WithName("SearchParties")
.WithOpenApi();

// تحديث بيانات طرف معين
app.MapPut("/parties/{id:int}", async (int id, Party party, ApplicationDbContext db) =>
{
    var existingParty = await db.Parties.FindAsync(id);
    if (existingParty is null) return Results.NotFound("الطرف غير موجود");

    // تحديث البيانات
    existingParty.NationalId = party.NationalId;
    existingParty.NameAr = party.NameAr;
    existingParty.NameEn = party.NameEn;
    existingParty.PhoneNumber = party.PhoneNumber;
    existingParty.BirthDate = party.BirthDate;
    existingParty.Address = party.Address;
    existingParty.Type = party.Type;
    existingParty.LastModifiedAt = DateTime.UtcNow; // تحديث تاريخ آخر تعديل

    await db.SaveChangesAsync();
    return Results.Ok(existingParty);
})
.WithName("UpdateParty")
.WithOpenApi();

// حذف طرف معين
app.MapDelete("/parties/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var party = await db.Parties.FindAsync(id);
    if (party is null) return Results.NotFound("الطرف غير موجود");

    // التحقق من وجود حوالات مرتبطة بهذا الطرف
    var hasRemittances = await db.Remittances
        .AnyAsync(r => r.SenderId == id || r.BeneficiaryId == id);

    if (hasRemittances)
    {
        return Results.BadRequest("لا يمكن حذف هذا الطرف لأنه مرتبط بحوالات موجودة");
    }

    db.Parties.Remove(party);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "تم حذف الطرف بنجاح" });
})
.WithName("DeleteParty")
.WithOpenApi();

// حساب العمولة وفق الشرائح المطلوبة
decimal CalculateFee(decimal amount)
{
    if (amount <= 500m) return 5m; // أقل أو يساوي 500 دينار: 5 دنانير
    if (amount <= 1000m) return 7m; // أكبر من 500 وحتى 1000: 7 دنانير
    return 10m; // أكثر من 1000: 10 دنانير (تعديل إذا لزم)
}

// حساب العمولة وفق الشرائح حسب الدولة
async Task<decimal> CalculateFeeByCountryAsync(ApplicationDbContext db, string country, decimal amount)
{
    if (string.IsNullOrWhiteSpace(country) || amount < 0) return 0m;
    var normalizedCountry = NormalizeCountry(country);
    // جلب الشرائح للدولة مرتبة تصاعدياً
    var tiers = await db.FeeTiers
        .Where(t => t.Country == normalizedCountry)
        .OrderBy(t => t.MinAmount)
        .ToListAsync();
    if (tiers.Count == 0)
    {
        // fallback للمنطق القديم إن لم توجد شرائح
        return CalculateFee(amount);
    }
    foreach (var t in tiers)
    {
        var maxOk = !t.MaxAmount.HasValue || amount <= t.MaxAmount.Value;
        if (amount >= t.MinAmount && maxOk)
        {
            return t.Fee;
        }
    }
    // إن لم تنطبق أي شريحة محددة، استخدم آخر شريحة مفتوحة إن وجدت
    var open = tiers.LastOrDefault(x => x.MaxAmount == null);
    return open?.Fee ?? CalculateFee(amount);
}

// ==================== سياسات الالتزام (Compliance) ====================
const decimal OUTGOING_THRESHOLD = 15000m; // حد الإرسال
const decimal INCOMING_THRESHOLD = 20000m; // حد السحب/الاستلام

async Task<(bool isHold, string reason, decimal totalWithCurrent)> EvaluateComplianceOnSendAsync(ApplicationDbContext db, int senderPartyId, decimal currentAmount)
{
    // مجموع كل المبالغ المرسلة لهذا الطرف (مدى الحياة)
    var total = await db.Remittances
        .Where(r => r.SenderId == senderPartyId)
        .SumAsync(r => (decimal?)r.Amount) ?? 0m;
    var totalWith = total + currentAmount;
    if (totalWith > OUTGOING_THRESHOLD)
    {
        return (true, $"تجاوز حد الإرسال {OUTGOING_THRESHOLD:N0}", totalWith);
    }
    return (false, string.Empty, totalWith);
}

async Task<(bool isHold, string reason, decimal totalWithCurrent)> EvaluateComplianceOnReceiveAsync(ApplicationDbContext db, int beneficiaryPartyId, decimal currentAmount)
{
    // مجموع المبالغ التي تم تسديدها بالفعل لهذا الطرف (مدى الحياة)
    var totalPaid = await db.Remittances
        .Where(r => r.BeneficiaryId == beneficiaryPartyId && r.Status == "Paid")
        .SumAsync(r => (decimal?)r.Amount) ?? 0m;
    var totalWith = totalPaid + currentAmount;
    if (totalWith > INCOMING_THRESHOLD)
    {
        return (true, $"تجاوز حد الاستلام {INCOMING_THRESHOLD:N0}", totalWith);
    }
    return (false, string.Empty, totalWith);
}

app.MapGet("/fees", async (ApplicationDbContext db, string country, decimal amount) =>
{
    if (amount < 0) return Results.BadRequest("المبلغ غير صالح");
    if (string.IsNullOrWhiteSpace(country)) return Results.BadRequest("الدولة مطلوبة");
    var normalized = NormalizeCountry(country);
    var fee = await CalculateFeeByCountryAsync(db, normalized, amount);
    return Results.Ok(new { country = normalized, amount, fee });
})
.WithName("GetFee")
.WithOpenApi();

// Remittances
app.MapPost("/remittances", async (Remittance remittance, ApplicationDbContext db) =>
{
    // حساب العمولة تلقائياً بناءً على الدولة والمبلغ
    remittance.Fee = await CalculateFeeByCountryAsync(db, remittance.Country, remittance.Amount);
    remittance.CreatedAt = DateTime.UtcNow;
    remittance.Status = "Payment pending";

    // توليد رقم مرجعي: ddMMyyyy + 5 أرقام
    string GenerateReference(string prefix, DateTime dt)
    {
        // Format: PREFIX-ddMMyyyy-HHmmss-milliseconds (e.g., RM-24102025-143059-123)
        return $"{prefix}-{dt.ToString("ddMMyyyy-HHmmss-fff")}";
    }

    remittance.Reference = GenerateReference("RM", remittance.CreatedAt);

    // التحقق من وجود المرسل والمستفيد
    var senderExists = await db.Parties.AnyAsync(p => p.Id == remittance.SenderId);
    var beneficiaryExists = await db.Parties.AnyAsync(p => p.Id == remittance.BeneficiaryId);
    if (!senderExists || !beneficiaryExists)
    {
        return Results.BadRequest("المرسل أو المستفيد غير موجود");
    }

    // إضافة المبلغ والعمولة إلى رصيد الصندوق المرسل (العميل دفع للصندوق)
    if (remittance.SenderUserId.HasValue)
    {
        var senderCashier = await db.Users.FindAsync(remittance.SenderUserId.Value);
        if (senderCashier != null)
        {
            var totalReceived = remittance.Amount + remittance.Fee;
            senderCashier.Balance -= totalReceived; // خصم المبلغ الإجمالي من رصيد الصراف المرسل
            senderCashier.LastBalanceUpdate = DateTime.UtcNow;
        }
    }

    // التحقق من الالتزام قبل الإنشاء (إيقاف إرسال عند تجاوز الحد)
    var sendCheck = await EvaluateComplianceOnSendAsync(db, remittance.SenderId, remittance.Amount);
    if (sendCheck.isHold)
    {
        remittance.Status = "Compliance Hold";
    }

    db.Remittances.Add(remittance);
    await db.SaveChangesAsync();

    // جلب الحوالة مع بيانات المرسل والمستفيد والصناديق الكاملة
    var createdRemittance = await db.Remittances
        .Include(r => r.Sender)
        .Include(r => r.Beneficiary)
        .Include(r => r.SenderUser)
        .Include(r => r.ReceiverUser)
        .Where(r => r.Id == remittance.Id)
        .Select(r => new
        {
            r.Id,
            r.Reference,
            r.Country,
            r.Amount,
            r.Fee,
            r.Reason,
            r.Purpose,
            r.Status,
            r.CreatedAt,
            r.PaidAt,
            Sender = new
            {
                r.Sender!.Id,
                r.Sender.NameAr,
                r.Sender.NameEn,
                r.Sender.NationalId,
                r.Sender.PhoneNumber
            },
            Beneficiary = new
            {
                r.Beneficiary!.Id,
                r.Beneficiary.NameAr,
                r.Beneficiary.NameEn,
                r.Beneficiary.NationalId,
                r.Beneficiary.PhoneNumber
            },
            SenderCashier = r.SenderUser != null ? new
            {
                r.SenderUser.Id,
                r.SenderUser.Name,
                r.SenderUser.Username
            } : null,
            ReceiverCashier = r.ReceiverUser != null ? new
            {
                r.ReceiverUser.Id,
                r.ReceiverUser.Name,
                r.ReceiverUser.Username
            } : null
        })
        .FirstOrDefaultAsync();

    return Results.Created($"/remittances/{createdRemittance?.Id}", createdRemittance);
})
.WithName("CreateRemittance")
.WithOpenApi();

app.MapGet("/remittances", async (ApplicationDbContext db) =>
{
    var list = await db.Remittances
        .Include(r => r.Sender)
        .Include(r => r.Beneficiary)
        .Include(r => r.SenderUser)
        .Include(r => r.ReceiverUser)
        .OrderByDescending(r => r.CreatedAt)
        .Take(100)
        .Select(r => new
        {
            r.Id,
            r.Reference,
            r.Country,
            r.Amount,
            r.Fee,
            r.Reason,
            r.Purpose,
            r.Status,
            r.CreatedAt,
            r.PaidAt,
            Sender = new
            {
                r.Sender!.Id,
                r.Sender.NameAr,
                r.Sender.NameEn,
                r.Sender.NationalId,
                r.Sender.PhoneNumber
            },
            Beneficiary = new
            {
                r.Beneficiary!.Id,
                r.Beneficiary.NameAr,
                r.Beneficiary.NameEn,
                r.Beneficiary.NationalId,
                r.Beneficiary.PhoneNumber
            },
            SenderCashier = r.SenderUser != null ? new
            {
                r.SenderUser.Id,
                r.SenderUser.Name,
                r.SenderUser.Username
            } : null,
            ReceiverCashier = r.ReceiverUser != null ? new
            {
                r.ReceiverUser.Id,
                r.ReceiverUser.Name,
                r.ReceiverUser.Username
            } : null
        })
        .ToListAsync();
    return Results.Ok(list);
})
.WithName("GetRemittances")
.WithOpenApi();

// بحث في الحوالات بالاسم العربي/الإنجليزي أو الرقم الوطني (مرسل/مستفيد) أو الرقم المرجعي
app.MapGet("/remittances/search", async (string? q, string? fromDate, string? toDate, ApplicationDbContext db) =>
{
    var query = db.Remittances
        .Include(r => r.Sender)
        .Include(r => r.Beneficiary)
        .Include(r => r.SenderUser)
        .Include(r => r.ReceiverUser)
        .AsQueryable();

    // فلتر البحث النصي
    if (!string.IsNullOrWhiteSpace(q))
    {
        q = q.Trim();
        var lowered = q.ToLower(CultureInfo.InvariantCulture);
        query = query.Where(r =>
            r.Reference.Contains(q) ||
            r.Sender!.NationalId.Contains(q) || r.Beneficiary!.NationalId.Contains(q) ||
            r.Sender!.NameAr.ToLower().Contains(lowered) || r.Beneficiary!.NameAr.ToLower().Contains(lowered) ||
            r.Sender!.NameEn.ToLower().Contains(lowered) || r.Beneficiary!.NameEn.ToLower().Contains(lowered)
        );
    }

    // فلتر التاريخ (من)
    if (!string.IsNullOrWhiteSpace(fromDate))
    {
        var formats = new[] { "yyyy-MM-dd", "yyyy/MM/dd", "MM/dd/yyyy" };
        if (DateTime.TryParseExact(fromDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var from))
        {
            query = query.Where(r => r.CreatedAt >= from);
        }
    }

    // فلتر التاريخ (إلى)
    if (!string.IsNullOrWhiteSpace(toDate))
    {
        var formats = new[] { "yyyy-MM-dd", "yyyy/MM/dd", "MM/dd/yyyy" };
        if (DateTime.TryParseExact(toDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var to))
        {
            // إضافة يوم كامل للتاريخ النهائي لتضمين كل اليوم
            var toEndOfDay = to.AddDays(1);
            query = query.Where(r => r.CreatedAt < toEndOfDay);
        }
    }

    var results = await query
        .OrderByDescending(r => r.CreatedAt)
        .Take(100)
        .Select(r => new
        {
            r.Id,
            r.Reference,
            r.Country,
            r.Amount,
            r.Fee,
            r.Reason,
            r.Purpose,
            r.Status,
            r.CreatedAt,
            r.PaidAt,
            // معلومات فك الإيقاف (إن وجدت)
            ReleasedNote = db.RemittanceChangeRequests
                .Where(cr => cr.RemittanceId == r.Id && cr.Notes != null && cr.Notes.StartsWith("[Release]"))
                .OrderByDescending(cr => cr.ApprovedAt ?? cr.CreatedAt)
                .Select(cr => cr.Notes)
                .FirstOrDefault(),
            ReleasedAt = db.RemittanceChangeRequests
                .Where(cr => cr.RemittanceId == r.Id && cr.Notes != null && cr.Notes.StartsWith("[Release]"))
                .OrderByDescending(cr => cr.ApprovedAt ?? cr.CreatedAt)
                .Select(cr => cr.ApprovedAt ?? cr.CreatedAt)
                .FirstOrDefault(),
            Sender = new
            {
                r.Sender!.Id,
                r.Sender.NameAr,
                r.Sender.NameEn,
                r.Sender.NationalId,
                r.Sender.PhoneNumber
            },
            Beneficiary = new
            {
                r.Beneficiary!.Id,
                r.Beneficiary.NameAr,
                r.Beneficiary.NameEn,
                r.Beneficiary.NationalId,
                r.Beneficiary.PhoneNumber
            },
            SenderCashier = r.SenderUser != null ? new
            {
                r.SenderUser.Id,
                r.SenderUser.Name,
                r.SenderUser.Username
            } : null,
            ReceiverCashier = r.ReceiverUser != null ? new
            {
                r.ReceiverUser.Id,
                r.ReceiverUser.Name,
                r.ReceiverUser.Username
            } : null
        })
        .ToListAsync();
    return Results.Ok(results);
})
.WithName("SearchRemittances")
.WithOpenApi();

// جلب حوالة عبر الرقم المرجعي
app.MapGet("/remittances/by-ref/{reference}", async (string reference, ApplicationDbContext db) =>
{
    var r = await db.Remittances
        .Include(x => x.Sender)
        .Include(x => x.Beneficiary)
        .Include(x => x.SenderUser)
        .Include(x => x.ReceiverUser)
        .FirstOrDefaultAsync(x => x.Reference == reference);

    if (r is null) return Results.NotFound();

    var result = new
    {
        r.Id,
        r.Reference,
        r.Country,
        r.Amount,
        r.Fee,
        r.Status,
        r.CreatedAt,
        r.PaidAt,
        ReleasedNote = db.RemittanceChangeRequests
            .Where(cr => cr.RemittanceId == r.Id && cr.Notes != null && cr.Notes.StartsWith("[Release]"))
            .OrderByDescending(cr => cr.ApprovedAt ?? cr.CreatedAt)
            .Select(cr => cr.Notes)
            .FirstOrDefault(),
        ReleasedAt = db.RemittanceChangeRequests
            .Where(cr => cr.RemittanceId == r.Id && cr.Notes != null && cr.Notes.StartsWith("[Release]"))
            .OrderByDescending(cr => cr.ApprovedAt ?? cr.CreatedAt)
            .Select(cr => cr.ApprovedAt ?? cr.CreatedAt)
            .FirstOrDefault(),
        Sender = new
        {
            r.Sender!.Id,
            r.Sender.NameAr,
            r.Sender.NameEn,
            r.Sender.NationalId,
            r.Sender.PhoneNumber
        },
        Beneficiary = new
        {
            r.Beneficiary!.Id,
            r.Beneficiary.NameAr,
            r.Beneficiary.NameEn,
            r.Beneficiary.NationalId,
            r.Beneficiary.PhoneNumber
        },
        SenderCashier = r.SenderUser != null ? new
        {
            r.SenderUser.Id,
            r.SenderUser.Name,
            r.SenderUser.Username
        } : null,
        ReceiverCashier = r.ReceiverUser != null ? new
        {
            r.ReceiverUser.Id,
            r.ReceiverUser.Name,
            r.ReceiverUser.Username
        } : null
    };

    return Results.Ok(result);
})
.WithName("GetRemittanceByReference")
.WithOpenApi();

// تعليم الحوالة كمسددة (سحب) عبر الرقم المرجعي
app.MapPost("/remittances/mark-paid", async (string reference, int? receiverUserId, ApplicationDbContext db) =>
{
    var r = await db.Remittances.FirstOrDefaultAsync(x => x.Reference == reference);
    if (r is null) return Results.NotFound("الحوالة غير موجودة");

    // التحقق من الالتزام قبل السداد (إيقاف السحب عند تجاوز الحد)
    var receiveCheck = await EvaluateComplianceOnReceiveAsync(db, r.BeneficiaryId, r.Amount);
    if (receiveCheck.isHold)
    {
        r.Status = "Compliance Hold";
        await db.SaveChangesAsync();
        return Results.BadRequest($"تم إيقاف الحوالة تلقائياً بسبب الالتزام: {receiveCheck.reason}");
    }

    // إذا لم يكن للحوالة صندوق مستقبل، أضفه الآن
    if (!r.ReceiverUserId.HasValue && receiverUserId.HasValue)
    {
        r.ReceiverUserId = receiverUserId.Value;
    }

    // التحقق من رصيد الصندوق المستقبل وخصم المبلغ (الصندوق يسلم المبلغ للعميل)
    if (r.ReceiverUserId.HasValue)
    {
        var receiverCashier = await db.Users.FindAsync(r.ReceiverUserId.Value);
        if (receiverCashier != null)
        {
            // التحقق من كفاية الرصيد
            if (receiverCashier.Balance < r.Amount)
            {
                return Results.BadRequest($"رصيد الصندوق غير كافٍ لتسليم الحوالة. الرصيد الحالي: {receiverCashier.Balance} دينار، المطلوب: {r.Amount} دينار");
            }

            receiverCashier.Balance -= r.Amount; // خصم المبلغ (الصندوق يسلم المال)
            receiverCashier.LastBalanceUpdate = DateTime.UtcNow;
        }
    }

    r.Status = "Paid";
    r.PaidAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(r);
})
.WithName("MarkRemittancePaid")
.WithOpenApi();

// === واجهات عرض/إدارة إيقافات الالتزام ===
app.MapGet("/compliance/holds", async (ApplicationDbContext db) =>
{
    var holds = await db.Remittances
        .Include(r => r.Sender)
        .Include(r => r.Beneficiary)
        .Where(r => r.Status == "Compliance Hold")
        .OrderByDescending(r => r.CreatedAt)
        .Take(200)
        .ToListAsync();

    var result = new List<object>();
    foreach (var r in holds)
    {
        var sendCheck = await EvaluateComplianceOnSendAsync(db, r.SenderId, 0m);
        var recvCheck = await EvaluateComplianceOnReceiveAsync(db, r.BeneficiaryId, r.Amount);
        var reasons = new List<string>();
        if (!string.IsNullOrWhiteSpace(sendCheck.reason) && (sendCheck.totalWithCurrent > OUTGOING_THRESHOLD))
            reasons.Add($"إرسال: {sendCheck.reason}");
        if (!string.IsNullOrWhiteSpace(recvCheck.reason) && (recvCheck.totalWithCurrent > INCOMING_THRESHOLD))
            reasons.Add($"استلام: {recvCheck.reason}");

        result.Add(new
        {
            r.Id,
            r.Reference,
            r.Amount,
            r.Fee,
            r.Country,
            r.Status,
            r.CreatedAt,
            r.Reason,
            r.Purpose,
            Sender = new { r.Sender!.Id, r.Sender.NameAr, r.Sender.NameEn, r.Sender.NationalId, r.Sender.PhoneNumber },
            Beneficiary = new { r.Beneficiary!.Id, r.Beneficiary.NameAr, r.Beneficiary.NameEn, r.Beneficiary.NationalId, r.Beneficiary.PhoneNumber },
            Reasons = reasons,
            Thresholds = new { Outgoing = OUTGOING_THRESHOLD, Incoming = INCOMING_THRESHOLD }
        });
    }
    return Results.Ok(result);
})
.WithName("GetComplianceHolds")
.WithOpenApi();

// فك إيقاف الالتزام: تحويل الحالة إلى Payment pending
app.MapPost("/compliance/release/{id:int}", async (int id, ApplicationDbContext db, int? releasedByUserId, string? releasedByUsername) =>
{
    var r = await db.Remittances.FindAsync(id);
    if (r is null) return Results.NotFound("الحوالة غير موجودة");
    if (r.Status != "Compliance Hold") return Results.BadRequest("الحوالة ليست موقوفة بسبب الالتزام");

    r.Status = "Payment pending";
    // تسجيل عملية فك الإيقاف كملاحظة خاصة في RemittanceChangeRequests
    var cr = new RemittanceChangeRequest
    {
        RemittanceId = r.Id,
        RequestType = ChangeRequestType.ReturnToPending,
        Notes = "[Release] " + (string.IsNullOrWhiteSpace(releasedByUsername) ? "Unknown" : releasedByUsername),
        Status = ChangeRequestStatus.Approved,
        CreatedAt = DateTime.UtcNow,
        ApprovedAt = DateTime.UtcNow
    };
    db.RemittanceChangeRequests.Add(cr);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "تم فك إيقاف الالتزام وإعادة الحالة إلى بانتظار الدفع", r.Id, r.Reference, r.Status });
})
.WithName("ReleaseComplianceHold")
.WithOpenApi();

// دفع إجباري عبر صفحة الالتزام (تجاوز الفحص)
app.MapPost("/compliance/force-pay", async (string reference, int receiverUserId, ApplicationDbContext db) =>
{
    var r = await db.Remittances.FirstOrDefaultAsync(x => x.Reference == reference);
    if (r is null) return Results.NotFound("الحوالة غير موجودة");

    // تعيين الصندوق المستقبل إن لم يكن موجوداً
    if (!r.ReceiverUserId.HasValue)
    {
        r.ReceiverUserId = receiverUserId;
    }

    // التحقق من رصيد الصندوق المستقبل وخصم المبلغ
    if (r.ReceiverUserId.HasValue)
    {
        var receiverCashier = await db.Users.FindAsync(r.ReceiverUserId.Value);
        if (receiverCashier != null)
        {
            if (receiverCashier.Balance < r.Amount)
            {
                return Results.BadRequest($"رصيد الصندوق غير كافٍ لتسليم الحوالة. الرصيد الحالي: {receiverCashier.Balance} دينار، المطلوب: {r.Amount} دينار");
            }
            receiverCashier.Balance -= r.Amount;
            receiverCashier.LastBalanceUpdate = DateTime.UtcNow;
        }
    }

    r.Status = "Paid";
    r.PaidAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "تم فك الحوالة و سحبها", r.Id, r.Reference, r.Status });
})
.WithName("ForcePayComplianceHold")
.WithOpenApi();

// إرجاع الحوالة إلى Payment pending عبر الرقم المرجعي مع ملاحظات
app.MapPost("/remittances/return", async (ReturnRequest request, ApplicationDbContext db) =>
{
    var r = await db.Remittances.FirstOrDefaultAsync(x => x.Reference == request.Reference);
    if (r is null) return Results.NotFound("الحوالة غير موجودة");

    // إعادة الرصيد إذا كانت الحوالة مسددة
    if (r.Status == "Paid")
    {
        // إضافة المبلغ إلى رصيد الصندوق المستقبل (إلغاء الخصم الذي حصل عند التسليم)
        if (r.ReceiverUserId.HasValue)
        {
            var receiverCashier = await db.Users.FindAsync(r.ReceiverUserId.Value);
            if (receiverCashier != null)
            {
                receiverCashier.Balance += r.Amount;
                receiverCashier.LastBalanceUpdate = DateTime.UtcNow;
            }
        }

        // خصم المبلغ والعمولة من رصيد الصندوق المرسل (إلغاء الإضافة التي حصلت عند الإرسال)
        if (r.SenderUserId.HasValue)
        {
            var senderCashier = await db.Users.FindAsync(r.SenderUserId.Value);
            if (senderCashier != null)
            {
                senderCashier.Balance -= (r.Amount + r.Fee);
                senderCashier.LastBalanceUpdate = DateTime.UtcNow;
            }
        }
    }

    r.Status = "Payment pending";
    r.PaidAt = null;

    // إنشاء طلب تغيير لتسجيل الملاحظات
    if (!string.IsNullOrWhiteSpace(request.Notes))
    {
        var changeRequest = new RemittanceChangeRequest
        {
            RemittanceId = r.Id,
            RequestType = ChangeRequestType.ReturnToPending,
            Notes = request.Notes,
            Status = ChangeRequestStatus.Approved, // معتمد مباشرة
            ApprovedAt = DateTime.UtcNow
        };
        db.RemittanceChangeRequests.Add(changeRequest);
    }

    await db.SaveChangesAsync();
    return Results.Ok(r);
})
.WithName("ReturnRemittanceToPending")
.WithOpenApi();

// تحديث بيانات حوالة معينة
app.MapPut("/remittances/{id:int}", async (int id, Remittance remittance, ApplicationDbContext db) =>
{
    var existingRemittance = await db.Remittances.FindAsync(id);
    if (existingRemittance is null) return Results.NotFound("الحوالة غير موجودة");

    // التحقق من وجود المرسل والمستفيد الجديدين
    var senderExists = await db.Parties.AnyAsync(p => p.Id == remittance.SenderId);
    var beneficiaryExists = await db.Parties.AnyAsync(p => p.Id == remittance.BeneficiaryId);
    if (!senderExists || !beneficiaryExists)
    {
        return Results.BadRequest("المرسل أو المستفيد غير موجود");
    }

    // تحديث البيانات
    existingRemittance.SenderId = remittance.SenderId;
    existingRemittance.BeneficiaryId = remittance.BeneficiaryId;
    existingRemittance.Country = remittance.Country;
    existingRemittance.Amount = remittance.Amount;
    existingRemittance.Fee = await CalculateFeeByCountryAsync(db, existingRemittance.Country, existingRemittance.Amount); // إعادة حساب العمولة
    existingRemittance.Reason = remittance.Reason;
    existingRemittance.Purpose = remittance.Purpose;

    await db.SaveChangesAsync();
    return Results.Ok(existingRemittance);
})
.WithName("UpdateRemittance")
.WithOpenApi();

// حذف حوالة معينة
app.MapDelete("/remittances/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var remittance = await db.Remittances.FindAsync(id);
    if (remittance is null) return Results.NotFound("الحوالة غير موجودة");

    // التحقق من وجود طلبات تعديل مرتبطة
    var hasChangeRequests = await db.RemittanceChangeRequests
        .AnyAsync(cr => cr.RemittanceId == id);

    if (hasChangeRequests)
    {
        return Results.BadRequest("لا يمكن حذف هذه الحوالة لأنها مرتبطة بطلبات تعديل");
    }

    db.Remittances.Remove(remittance);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "تم حذف الحوالة بنجاح" });
})
.WithName("DeleteRemittance")
.WithOpenApi();

// حوالات لطرف معين
app.MapGet("/parties/{id:int}/remittances", async (int id, ApplicationDbContext db) =>
{
    var list = await db.Remittances
        .Include(r => r.Sender)
        .Include(r => r.Beneficiary)
        .Include(r => r.SenderUser)
        .Include(r => r.ReceiverUser)
        .Where(r => r.SenderId == id || r.BeneficiaryId == id)
        .OrderByDescending(r => r.CreatedAt)
        .Take(20)
        .Select(r => new
        {
            r.Id,
            r.Reference,
            r.Country,
            r.Amount,
            r.Fee,
            r.Reason,
            r.Purpose,
            r.Status,
            r.CreatedAt,
            r.PaidAt,
            Sender = new
            {
                r.Sender!.Id,
                r.Sender.NameAr,
                r.Sender.NameEn,
                r.Sender.NationalId,
                r.Sender.PhoneNumber
            },
            Beneficiary = new
            {
                r.Beneficiary!.Id,
                r.Beneficiary.NameAr,
                r.Beneficiary.NameEn,
                r.Beneficiary.NationalId,
                r.Beneficiary.PhoneNumber
            },
            SenderCashier = r.SenderUser != null ? new
            {
                r.SenderUser.Id,
                r.SenderUser.Name,
                r.SenderUser.Username
            } : null,
            ReceiverCashier = r.ReceiverUser != null ? new
            {
                r.ReceiverUser.Id,
                r.ReceiverUser.Name,
                r.ReceiverUser.Username
            } : null
        })
        .ToListAsync();
    return Results.Ok(list);
})
.WithName("GetPartyRemittances")
.WithOpenApi();

app.MapGet("/parties/{id:int}/latest-remittance", async (int id, ApplicationDbContext db) =>
{
    var r = await db.Remittances
        .Where(x => x.SenderId == id || x.BeneficiaryId == id)
        .OrderByDescending(x => x.CreatedAt)
        .FirstOrDefaultAsync();
    return r is null ? Results.NoContent() : Results.Ok(r);
})
.WithName("GetPartyLatestRemittance")
.WithOpenApi();

// إنشاء طلب تغيير على الحوالة (إرجاع أو تحديث)
app.MapPost("/remittances/change-requests", async (ApplicationDbContext db, RemittanceChangeRequest req, string reference) =>
{
    var r = await db.Remittances.FirstOrDefaultAsync(x => x.Reference == reference);
    if (r is null) return Results.NotFound("الحوالة غير موجودة");

    req.RemittanceId = r.Id;
    req.Status = ChangeRequestStatus.Pending;
    req.CreatedAt = DateTime.UtcNow;

    // تغيير حالة الحوالة إلى "قيد الموافقة" عند إرسال طلب الإرجاع أو التعديل
    r.Status = "Pending Approval";

    db.RemittanceChangeRequests.Add(req);
    await db.SaveChangesAsync();
    return Results.Created($"/remittances/change-requests/{req.Id}", req);
})
.WithName("CreateRemittanceChangeRequest")
.WithOpenApi();

// قائمة طلبات التغيير بالحالة
app.MapGet("/remittances/change-requests", async (ApplicationDbContext db, ChangeRequestStatus? status) =>
{
    var q = db.RemittanceChangeRequests
        .Include(c => c.Remittance!)
            .ThenInclude(r => r.Sender)
        .Include(c => c.Remittance!)
            .ThenInclude(r => r.Beneficiary)
        .AsQueryable();
    if (status.HasValue) q = q.Where(c => c.Status == status);
    var list = await q.OrderByDescending(c => c.CreatedAt).Take(100).ToListAsync();
    return Results.Ok(list);
})
.WithName("GetRemittanceChangeRequests")
.WithOpenApi();

// اعتماد طلب تغيير
app.MapPost("/remittances/change-requests/{id:int}/approve", async (int id, ApplicationDbContext db) =>
{
    var cr = await db.RemittanceChangeRequests.Include(c => c.Remittance).FirstOrDefaultAsync(c => c.Id == id);
    if (cr is null) return Results.NotFound();
    if (cr.Status != ChangeRequestStatus.Pending) return Results.BadRequest("الحالة غير صحيحة");

    var r = cr.Remittance!;
    if (cr.RequestType == ChangeRequestType.ReturnToPending)
    {
        // إعادة الرصيد إذا كانت الحوالة مسددة
        if (r.Status == "Paid")
        {
            // إضافة المبلغ إلى رصيد الصندوق المستقبل (إلغاء الخصم الذي حصل عند التسليم)
            if (r.ReceiverUserId.HasValue)
            {
                var receiverCashier = await db.Users.FindAsync(r.ReceiverUserId.Value);
                if (receiverCashier != null)
                {
                    receiverCashier.Balance += r.Amount;
                    receiverCashier.LastBalanceUpdate = DateTime.UtcNow;
                }
            }

            // خصم المبلغ والعمولة من رصيد الصندوق المرسل (إلغاء الإضافة التي حصلت عند الإرسال)
            if (r.SenderUserId.HasValue)
            {
                var senderCashier = await db.Users.FindAsync(r.SenderUserId.Value);
                if (senderCashier != null)
                {
                    senderCashier.Balance -= (r.Amount + r.Fee);
                    senderCashier.LastBalanceUpdate = DateTime.UtcNow;
                }
            }
        }

        r.Status = "Payment pending";
        r.PaidAt = null;
    }
    else if (cr.RequestType == ChangeRequestType.UpdateDetails)
    {
        if (!string.IsNullOrWhiteSpace(cr.ProposedCountry)) r.Country = cr.ProposedCountry!;
        if (cr.ProposedAmount.HasValue) r.Amount = cr.ProposedAmount.Value;
        if (!string.IsNullOrWhiteSpace(cr.ProposedReason)) r.Reason = cr.ProposedReason!;
        if (!string.IsNullOrWhiteSpace(cr.ProposedPurpose)) r.Purpose = cr.ProposedPurpose!;
        r.Fee = await CalculateFeeByCountryAsync(db, r.Country, r.Amount);
    }

    cr.Status = ChangeRequestStatus.Approved;
    cr.ApprovedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(new { request = cr, remittance = r });
})
.WithName("ApproveRemittanceChangeRequest")
.WithOpenApi();

// رفض طلب تغيير
app.MapPost("/remittances/change-requests/{id:int}/reject", async (int id, ApplicationDbContext db) =>
{
    var cr = await db.RemittanceChangeRequests.Include(c => c.Remittance).FirstOrDefaultAsync(c => c.Id == id);
    if (cr is null) return Results.NotFound();
    if (cr.Status != ChangeRequestStatus.Pending) return Results.BadRequest("الحالة غير صحيحة");

    // رفض الطلب
    cr.Status = ChangeRequestStatus.Rejected;

    // إذا الحوالة حالتها حالياً قيد الموافقة
    if (cr.Remittance != null && cr.Remittance.Status == "Pending Approval")
    {
        // تحقق هل يوجد أي طلب آخر معلق (قيد الموافقة) لنفس الحوالة
        var otherPendingRequests = await db.RemittanceChangeRequests
            .Where(x => x.RemittanceId == cr.Remittance.Id && x.Status == ChangeRequestStatus.Pending && x.Id != cr.Id)
            .AnyAsync();

        if (!otherPendingRequests)
        {
            cr.Remittance.Status = "Payment pending";
        }
        // إذا في طلب آخر معلق لنفس الحوالة تظل الحالة كما هي
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { request = cr, remittance = cr.Remittance });
})
.WithName("RejectRemittanceChangeRequest")
.WithOpenApi();

// === أسعار الصرف ===

// الحصول على جميع أسعار الصرف (مجموعة بدون تكرار على مستوى الدولة)
app.MapGet("/exchange-rates", async (ApplicationDbContext db, ExchangeRateScope? scope) =>
{
    try
    {
        var query = db.ExchangeRates.AsQueryable();

        if (scope.HasValue)
        {
            var s = scope.Value;
            // فصل كامل بين أسعار الحوالات وتبديل العملات
            query = query.Where(er => er.Scope == s);
        }

        var all = await query
            .OrderBy(er => er.Country)
            .ThenByDescending(er => er.LastModifiedAt ?? er.CreatedAt)
            .ToListAsync();

        // تصفية البيانات غير الصحيحة
        var validRates = all.Where(er =>
            !string.IsNullOrWhiteSpace(er.Country) &&
            er.Rate > 0 &&
            !string.IsNullOrWhiteSpace(er.Currency)
        ).ToList();

        var distinct = validRates
            .GroupBy(er => er.Country)
            .Select(g => g.First())
            .OrderBy(er => er.Country)
            .ToList();

        Console.WriteLine($"تم جلب {distinct.Count} سعر صرف صحيح من أصل {all.Count} سعر");
        return Results.Ok(distinct);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"خطأ في جلب أسعار الصرف: {ex.Message}");
        return Results.Problem($"خطأ في جلب أسعار الصرف: {ex.Message}");
    }
})
.WithName("GetExchangeRates")
.WithOpenApi();

// الحصول على سعر صرف دولة معينة
app.MapGet("/exchange-rates/{country}", async (string country, ApplicationDbContext db, ExchangeRateScope? scope) =>
{
    var normalized = NormalizeCountry(country);
    var query = db.ExchangeRates.Where(er => er.Country == normalized);
    if (scope.HasValue)
    {
        var s = scope.Value;
        // فصل كامل بين أسعار الحوالات وتبديل العملات
        query = query.Where(er => er.Scope == s);
    }
    var rate = await query.FirstOrDefaultAsync();
    return rate is null ? Results.NotFound("سعر الصرف غير موجود") : Results.Ok(rate);
})
.WithName("GetExchangeRate")
.WithOpenApi();

// إضافة سعر صرف جديد
app.MapPost("/exchange-rates", async (ExchangeRate exchangeRate, ApplicationDbContext db) =>
{
    try
    {
        // التحقق من عدم وجود دولة بنفس الاسم ضمن نفس النطاق
        exchangeRate.Country = NormalizeCountry(exchangeRate.Country);
        var existing = await db.ExchangeRates.FirstOrDefaultAsync(er => er.Country == exchangeRate.Country && er.Scope == exchangeRate.Scope);
        if (existing != null)
        {
            return Results.BadRequest($"يوجد سعر صرف لهذه الدولة ({exchangeRate.Country}) بالفعل لهذا النطاق. يرجى تحديث السعر الموجود بدلاً من إضافة جديد.");
        }

        exchangeRate.CreatedAt = DateTime.Now; // استخدام التوقيت المحلي
        db.ExchangeRates.Add(exchangeRate);
        await db.SaveChangesAsync();
        return Results.Created($"/exchange-rates/{exchangeRate.Id}", exchangeRate);
    }
    catch (Exception ex)
    {
        // معالجة أخطاء قاعدة البيانات بشكل أفضل
        if (ex.InnerException?.Message?.Contains("duplicate key") == true)
        {
            return Results.BadRequest($"يوجد سعر صرف لهذه الدولة ({exchangeRate.Country}) بالفعل. يرجى تحديث السعر الموجود بدلاً من إضافة جديد.");
        }
        return Results.Problem($"خطأ في قاعدة البيانات: {ex.Message}");
    }
})
.WithName("CreateExchangeRate")
.WithOpenApi();

// تحديث سعر صرف
app.MapPut("/exchange-rates/{id:int}", async (int id, ExchangeRate exchangeRate, ApplicationDbContext db) =>
{
    try
    {
        var existing = await db.ExchangeRates.FindAsync(id);
        if (existing is null) return Results.NotFound("سعر الصرف غير موجود");

        // التحقق من صحة البيانات
        if (string.IsNullOrWhiteSpace(exchangeRate.Country))
            return Results.BadRequest("اسم الدولة مطلوب");

        if (exchangeRate.Rate <= 0)
            return Results.BadRequest("سعر الصرف يجب أن يكون أكبر من صفر");

        // التحقق من عدم وجود دولة أخرى بنفس الاسم في نفس النطاق
        exchangeRate.Country = NormalizeCountry(exchangeRate.Country);
        var duplicate = await db.ExchangeRates.AnyAsync(er => er.Country == exchangeRate.Country && er.Scope == exchangeRate.Scope && er.Id != id);
        if (duplicate)
        {
            return Results.BadRequest("يوجد سعر صرف لهذه الدولة بالفعل في نفس النطاق");
        }

        // تحديث الحقول
        existing.Country = exchangeRate.Country;
        existing.Rate = exchangeRate.Rate;
        existing.Currency = exchangeRate.Currency;
        existing.Notes = exchangeRate.Notes;
        existing.Scope = exchangeRate.Scope;
        existing.LastModifiedAt = DateTime.Now; // استخدام التوقيت المحلي بدلاً من UTC

        // التأكد من تتبع التغييرات وحفظها
        db.ExchangeRates.Update(existing);
        var affected = await db.SaveChangesAsync();
        if (affected == 0)
        {
            return Results.Problem("لم يتم حفظ أي تغييرات على سعر الصرف");
        }
        return Results.Ok(existing);
    }
    catch (Exception ex)
    {
        return Results.Problem($"خطأ في تحديث سعر الصرف: {ex.Message}");
    }
})
.WithName("UpdateExchangeRate")
.WithOpenApi();

// جلب سعر صرف عبر المعرّف للتحقق السريع بعد التحديث
app.MapGet("/exchange-rates/by-id/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var er = await db.ExchangeRates.FindAsync(id);
    return er is null ? Results.NotFound("سعر الصرف غير موجود") : Results.Ok(er);
})
.WithOpenApi();

// حذف سعر صرف
app.MapDelete("/exchange-rates/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var exchangeRate = await db.ExchangeRates.FindAsync(id);
    if (exchangeRate is null) return Results.NotFound("سعر الصرف غير موجود");

    db.ExchangeRates.Remove(exchangeRate);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "تم حذف سعر الصرف بنجاح" });
})
.WithName("DeleteExchangeRate")
.WithOpenApi();

// === Fee Tiers ===
// التحقق من التداخل بين الشرائح لنفس الدولة
bool IsOverlapping(FeeTier a, FeeTier b)
{
    if (!string.Equals(a.Country, b.Country, StringComparison.Ordinal)) return false;
    var aMin = a.MinAmount;
    var aMax = a.MaxAmount ?? decimal.MaxValue;
    var bMin = b.MinAmount;
    var bMax = b.MaxAmount ?? decimal.MaxValue;
    return aMin <= bMax && bMin <= aMax;
}

app.MapGet("/fee-tiers", async (ApplicationDbContext db, string? country) =>
{
    var q = db.FeeTiers.AsQueryable();
    if (!string.IsNullOrWhiteSpace(country))
    {
        var normalized = NormalizeCountry(country);
        q = q.Where(t => t.Country == normalized);
    }
    var list = await q.OrderBy(t => t.Country).ThenBy(t => t.MinAmount).ToListAsync();
    return Results.Ok(list);
})
.WithName("GetFeeTiers")
.WithOpenApi();

app.MapPost("/fee-tiers", async (ApplicationDbContext db, FeeTier tier) =>
{
    if (tier.MinAmount < 0 || (tier.MaxAmount.HasValue && tier.MaxAmount.Value < tier.MinAmount))
        return Results.BadRequest("قيمة المدى غير صحيحة");

    // منع التداخل
    tier.Country = NormalizeCountry(tier.Country);
    var existing = await db.FeeTiers.Where(t => t.Country == tier.Country).ToListAsync();
    if (existing.Any(t => IsOverlapping(t, tier)))
        return Results.BadRequest("هناك تداخل مع شريحة موجودة لنفس الدولة");

    tier.CreatedAt = DateTime.UtcNow;
    db.FeeTiers.Add(tier);
    await db.SaveChangesAsync();
    return Results.Created($"/fee-tiers/{tier.Id}", tier);
})
.WithName("CreateFeeTier")
.WithOpenApi();

app.MapPut("/fee-tiers/{id:int}", async (ApplicationDbContext db, int id, FeeTier tier) =>
{
    var existingTier = await db.FeeTiers.FindAsync(id);
    if (existingTier is null) return Results.NotFound("الشريحة غير موجودة");

    if (tier.MinAmount < 0 || (tier.MaxAmount.HasValue && tier.MaxAmount.Value < tier.MinAmount))
        return Results.BadRequest("قيمة المدى غير صحيحة");

    // تطبيع الدولة ومنع التداخل مع غيرها
    tier.Country = NormalizeCountry(tier.Country);
    var siblings = await db.FeeTiers.Where(t => t.Country == tier.Country && t.Id != id).ToListAsync();
    if (siblings.Any(t => IsOverlapping(t, tier)))
        return Results.BadRequest("هناك تداخل مع شريحة موجودة لنفس الدولة");

    existingTier.Country = tier.Country;
    existingTier.MinAmount = tier.MinAmount;
    existingTier.MaxAmount = tier.MaxAmount;
    existingTier.Fee = tier.Fee;
    existingTier.LastModifiedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(existingTier);
})
.WithName("UpdateFeeTier")
.WithOpenApi();

app.MapDelete("/fee-tiers/{id:int}", async (ApplicationDbContext db, int id) =>
{
    var existingTier = await db.FeeTiers.FindAsync(id);
    if (existingTier is null) return Results.NotFound("الشريحة غير موجودة");
    db.FeeTiers.Remove(existingTier);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "تم حذف الشريحة" });
})
.WithName("DeleteFeeTier")
.WithOpenApi();

// ==================== إدارة أرصدة الصناديق ====================

// جلب رصيد صندوق محدد
app.MapGet("/cashiers/{id:int}/balance", async (ApplicationDbContext db, int id) =>
{
    var cashier = await db.Users.FindAsync(id);
    if (cashier is null) return Results.NotFound("الصندوق غير موجود");

    return Results.Ok(new
    {
        cashier.Id,
        cashier.Name,
        cashier.Username,
        cashier.Balance,
        cashier.InitialBalance,
        cashier.LastBalanceUpdate
    });
})
.WithName("GetCashierBalance")
.WithOpenApi();

// جلب جميع أرصدة الصناديق
app.MapGet("/cashiers/balances", async (ApplicationDbContext db) =>
{
    var cashiers = await db.Users
        .Where(u => u.IsActive)
        .Select(u => new
        {
            u.Id,
            u.Name,
            u.Username,
            u.Department,
            u.Balance,
            u.InitialBalance,
            u.LastBalanceUpdate,
            u.Role
        })
        .ToListAsync();

    return Results.Ok(cashiers);
})
.WithName("GetAllCashierBalances")
.WithOpenApi();

// تحديث/تغذية رصيد صندوق (للأدمن فقط)
app.MapPost("/cashiers/{id:int}/add-balance", async (ApplicationDbContext db, int id, AddBalanceRequest request) =>
{
    var cashier = await db.Users.FindAsync(id);
    if (cashier is null) return Results.NotFound("الصندوق غير موجود");

    if (request.Amount <= 0)
        return Results.BadRequest("المبلغ يجب أن يكون أكبر من صفر");

    cashier.Balance += request.Amount;
    cashier.LastBalanceUpdate = DateTime.UtcNow;

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        cashier.Id,
        cashier.Name,
        cashier.Balance,
        AddedAmount = request.Amount,
        cashier.LastBalanceUpdate,
        Message = $"تم إضافة {request.Amount} دينار إلى رصيد الصندوق"
    });
})
.WithName("AddCashierBalance")
.WithOpenApi();

// تحديث الرصيد الأولي للصندوق
app.MapPut("/cashiers/{id:int}/initial-balance", async (ApplicationDbContext db, int id, SetInitialBalanceRequest request) =>
{
    var cashier = await db.Users.FindAsync(id);
    if (cashier is null) return Results.NotFound("الصندوق غير موجود");

    if (request.InitialBalance < 0)
        return Results.BadRequest("الرصيد الأولي لا يمكن أن يكون سالباً");

    cashier.InitialBalance = request.InitialBalance;
    cashier.Balance = request.InitialBalance;
    cashier.LastBalanceUpdate = DateTime.UtcNow;

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        cashier.Id,
        cashier.Name,
        cashier.InitialBalance,
        cashier.Balance,
        cashier.LastBalanceUpdate,
        Message = "تم تعيين الرصيد الأولي بنجاح"
    });
})
.WithName("SetCashierInitialBalance")
.WithOpenApi();

// ==================== أسعار صرف العملات (FX Exchange Rates) ====================

// الحصول على جميع أسعار صرف العملات
app.MapGet("/fx-exchange-rates", async (FxDbContext fxDb) =>
{
    try
    {
        var rates = await fxDb.FxExchangeRates
            .Include(fx => fx.Cashier)
            .OrderBy(fx => fx.Currency)
            .ToListAsync();

        return Results.Ok(rates);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"خطأ في جلب أسعار صرف العملات: {ex.Message}");
        return Results.Problem($"خطأ في جلب أسعار صرف العملات: {ex.Message}");
    }
})
.WithName("GetFxExchangeRates")
.WithOpenApi();

// الحصول على سعر صرف عملة معينة
app.MapGet("/fx-exchange-rates/{currency}", async (string currency, FxDbContext fxDb) =>
{
    try
    {
        var rate = await fxDb.FxExchangeRates
            .Include(fx => fx.Cashier)
            .FirstOrDefaultAsync(fx => fx.Currency == currency.ToUpper());

        return rate is null ? Results.NotFound("سعر الصرف غير موجود") : Results.Ok(rate);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"خطأ في جلب سعر صرف العملة {currency}: {ex.Message}");
        return Results.Problem($"خطأ في جلب سعر صرف العملة: {ex.Message}");
    }
})
.WithName("GetFxExchangeRate")
.WithOpenApi();

// إضافة سعر صرف عملة جديد
app.MapPost("/fx-exchange-rates", async (FxExchangeRate fxRate, FxDbContext fxDb) =>
{
    try
    {
        // التحقق من عدم وجود عملة بنفس الاسم
        fxRate.Currency = fxRate.Currency.ToUpper();
        var existing = await fxDb.FxExchangeRates.FirstOrDefaultAsync(fx => fx.Currency == fxRate.Currency);
        if (existing != null)
        {
            return Results.BadRequest($"يوجد سعر صرف لهذه العملة ({fxRate.Currency}) بالفعل. يرجى تحديث السعر الموجود بدلاً من إضافة جديد.");
        }

        // التحقق من صحة البيانات
        if (fxRate.BuyRate <= 0 || fxRate.SellRate <= 0)
        {
            return Results.BadRequest("أسعار الشراء والبيع يجب أن تكون أكبر من صفر");
        }

        if (fxRate.BuyRate >= fxRate.SellRate)
        {
            return Results.BadRequest("سعر الشراء يجب أن يكون أقل من سعر البيع");
        }

        fxRate.CreatedAt = DateTime.Now;
        fxDb.FxExchangeRates.Add(fxRate);
        await fxDb.SaveChangesAsync();

        return Results.Created($"/fx-exchange-rates/{fxRate.Id}", fxRate);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"خطأ في إضافة سعر صرف العملة: {ex.Message}");
        return Results.Problem($"خطأ في إضافة سعر صرف العملة: {ex.Message}");
    }
})
.WithName("CreateFxExchangeRate")
.WithOpenApi();

// تحديث سعر صرف عملة
app.MapPut("/fx-exchange-rates/{id:int}", async (int id, FxExchangeRate fxRate, FxDbContext fxDb) =>
{
    try
    {
        var existing = await fxDb.FxExchangeRates.FindAsync(id);
        if (existing is null) return Results.NotFound("سعر الصرف غير موجود");

        // التحقق من صحة البيانات
        if (fxRate.BuyRate <= 0 || fxRate.SellRate <= 0)
        {
            return Results.BadRequest("أسعار الشراء والبيع يجب أن تكون أكبر من صفر");
        }

        if (fxRate.BuyRate >= fxRate.SellRate)
        {
            return Results.BadRequest("سعر الشراء يجب أن يكون أقل من سعر البيع");
        }

        // تحديث الحقول
        existing.Currency = fxRate.Currency.ToUpper();
        existing.BuyRate = fxRate.BuyRate;
        existing.SellRate = fxRate.SellRate;
        existing.Notes = fxRate.Notes;
        existing.CashierId = fxRate.CashierId;
        existing.LastModifiedAt = DateTime.Now;

        await fxDb.SaveChangesAsync();
        return Results.Ok(existing);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"خطأ في تحديث سعر صرف العملة: {ex.Message}");
        return Results.Problem($"خطأ في تحديث سعر صرف العملة: {ex.Message}");
    }
})
.WithName("UpdateFxExchangeRate")
.WithOpenApi();

// حذف سعر صرف عملة
app.MapDelete("/fx-exchange-rates/{id:int}", async (int id, FxDbContext fxDb) =>
{
    try
    {
        var fxRate = await fxDb.FxExchangeRates.FindAsync(id);
        if (fxRate is null) return Results.NotFound("سعر الصرف غير موجود");

        fxDb.FxExchangeRates.Remove(fxRate);
        await fxDb.SaveChangesAsync();
        return Results.Ok(new { message = "تم حذف سعر الصرف بنجاح" });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"خطأ في حذف سعر صرف العملة: {ex.Message}");
        return Results.Problem($"خطأ في حذف سعر صرف العملة: {ex.Message}");
    }
})
.WithName("DeleteFxExchangeRate")
.WithOpenApi();

// ==================== عمليات تبديل العملات (شراء/بيع) ====================

// إنشاء عملية تبديل عملة جديدة
app.MapPost("/currency-exchanges", async (CurrencyExchange exchange, ApplicationDbContext db) =>
{
    try
    {
        // التحقق من وجود الصندوق
        var cashier = await db.Users.FindAsync(exchange.CashierId);
        if (cashier == null)
        {
            return Results.BadRequest("الصندوق غير موجود");
        }

        // حساب المبلغ بالدينار الأردني
        exchange.JodAmount = exchange.ForeignAmount * exchange.ExchangeRate;
        exchange.CreatedAt = DateTime.UtcNow;

        // تحديث رصيد الصندوق حسب نوع العملية
        if (exchange.Type == ExchangeType.Buy)
        {
            // الصندوق يشتري عملة من العميل: يدفع دنانير للعميل (خصم من الرصيد)
            cashier.Balance -= exchange.JodAmount;
        }
        else if (exchange.Type == ExchangeType.Sell)
        {
            // الصندوق يبيع عملة للعميل: يستلم دنانير من العميل (إضافة للرصيد)
            cashier.Balance += exchange.JodAmount;
        }

        cashier.LastBalanceUpdate = DateTime.UtcNow;

        // توليد رقم مرجعي لعملية الصرف
        exchange.Reference = GenerateReference("CE", exchange.CreatedAt);
        Console.WriteLine($"🔍 Generated reference: {exchange.Reference}");

        db.CurrencyExchanges.Add(exchange);
        await db.SaveChangesAsync();

        // إعادة جلب العملية مع بيانات الصندوق
        var created = await db.CurrencyExchanges
            .Include(ce => ce.Cashier)
            .FirstOrDefaultAsync(ce => ce.Id == exchange.Id);

        Console.WriteLine($"🔍 Returning reference: {created?.Reference}");
        return Results.Created($"/currency-exchanges/{exchange.Id}", created);
    }
    catch (Exception ex)
    {
        return Results.Problem($"خطأ في حفظ العملية: {ex.Message}");
    }
})
.WithName("CreateCurrencyExchange")
.WithOpenApi();

// جلب عمليات تبديل العملات
app.MapGet("/currency-exchanges", async (ApplicationDbContext db) =>
{
    var query = db.CurrencyExchanges
        .Include(ce => ce.Cashier)
        .OrderByDescending(ce => ce.CreatedAt);
    return Results.Ok(await query.ToListAsync());
})
.WithName("GetCurrencyExchanges")
.WithOpenApi();

// جلب عمليات تبديل العملات لصندوق معين
app.MapGet("/currency-exchanges/cashier/{cashierId:int}", async (int cashierId, ApplicationDbContext db) =>
{
    try
    {
        var exchanges = await db.CurrencyExchanges
            .Include(ce => ce.Cashier)
            .Where(ce => ce.CashierId == cashierId)
            .OrderByDescending(ce => ce.CreatedAt)
            .Select(ce => new
            {
                ce.Id,
                ce.Type,
                ce.Currency,
                ce.ForeignAmount,
                ce.ExchangeRate,
                ce.JodAmount,
                ce.Profit,
                ce.CustomerNationalId,
                ce.CustomerName,
                ce.CustomerPhone,
                ce.CreatedAt,
                ce.Notes,
                ce.Country
            })
            .ToListAsync();

        var buyCount = exchanges.Count(e => e.Type == ExchangeType.Buy);
        var sellCount = exchanges.Count(e => e.Type == ExchangeType.Sell);

        Console.WriteLine($"الصندوق {cashierId}: إجمالي العمليات = {exchanges.Count}, عمليات الشراء = {buyCount}, عمليات البيع = {sellCount}");

        return Results.Ok(new
        {
            TotalCount = exchanges.Count,
            BuyCount = buyCount,
            SellCount = sellCount,
            Exchanges = exchanges
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"خطأ في جلب عمليات الصندوق {cashierId}: {ex.Message}");
        return Results.Problem($"خطأ في جلب عمليات الصندوق: {ex.Message}");
    }
})
.WithName("GetCashierCurrencyExchanges")
.WithOpenApi();

// البحث في عمليات تبديل العملات بالاسم أو الرقم الوطني أو الرقم المرجعي
app.MapGet("/currency-exchanges/search", async (string? q, string? fromDate, string? toDate, ApplicationDbContext db) =>
{
    var query = db.CurrencyExchanges
        .Include(ce => ce.Cashier)
        .AsQueryable();

    // فلتر البحث النصي
    if (!string.IsNullOrWhiteSpace(q))
    {
        q = q.Trim();
        var lowered = q.ToLower(CultureInfo.InvariantCulture);
        query = query.Where(ce =>
            ce.Reference.Contains(q) ||
            (ce.CustomerNationalId != null && ce.CustomerNationalId.Contains(q)) ||
            (ce.CustomerName != null && ce.CustomerName.ToLower().Contains(lowered))
        );
    }

    // فلتر التاريخ (من)
    if (!string.IsNullOrWhiteSpace(fromDate))
    {
        var formats = new[] { "yyyy-MM-dd", "yyyy/MM/dd", "MM/dd/yyyy" };
        if (DateTime.TryParseExact(fromDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var from))
        {
            query = query.Where(ce => ce.CreatedAt >= from);
        }
    }

    // فلتر التاريخ (إلى)
    if (!string.IsNullOrWhiteSpace(toDate))
    {
        var formats = new[] { "yyyy-MM-dd", "yyyy/MM/dd", "MM/dd/yyyy" };
        if (DateTime.TryParseExact(toDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var to))
        {
            // إضافة يوم كامل للتاريخ النهائي لتضمين كل اليوم
            var toEndOfDay = to.AddDays(1);
            query = query.Where(ce => ce.CreatedAt < toEndOfDay);
        }
    }

    var results = await query
        .OrderByDescending(ce => ce.CreatedAt)
        .Select(ce => new
        {
            ce.Id,
            ce.Reference,
            ce.Type,
            ce.Currency,
            ce.ForeignAmount,
            ce.ExchangeRate,
            ce.JodAmount,
            ce.Profit,
            ce.CustomerNationalId,
            ce.CustomerName,
            ce.CustomerPhone,
            ce.CreatedAt,
            ce.Notes,
            ce.Country,
            Cashier = ce.Cashier != null ? new
            {
                ce.Cashier.Id,
                ce.Cashier.Name,
                ce.Cashier.Username
            } : null
        })
        .ToListAsync();

    return Results.Ok(results);
})
.WithName("SearchCurrencyExchanges")
.WithOpenApi();

// حذف عملية تبديل عملة
app.MapDelete("/currency-exchanges/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var exchange = await db.CurrencyExchanges
        .Include(ce => ce.Cashier)
        .FirstOrDefaultAsync(ce => ce.Id == id);

    if (exchange == null)
    {
        return Results.NotFound("العملية غير موجودة");
    }

    // إرجاع الرصيد للصندوق
    if (exchange.Cashier != null)
    {
        if (exchange.Type == ExchangeType.Buy)
        {
            // كانت عملية شراء (خصم من الرصيد) -> نضيف الرصيد عند الحذف
            exchange.Cashier.Balance += exchange.JodAmount;
        }
        else if (exchange.Type == ExchangeType.Sell)
        {
            // كانت عملية بيع (إضافة للرصيد) -> نخصم الرصيد عند الحذف
            exchange.Cashier.Balance -= exchange.JodAmount;
        }
        exchange.Cashier.LastBalanceUpdate = DateTime.UtcNow;
    }

    db.CurrencyExchanges.Remove(exchange);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "تم حذف العملية بنجاح" });
})
.WithName("DeleteCurrencyExchange")
.WithOpenApi();

// إحصائيات عمليات تبديل العملات
app.MapGet("/currency-exchanges/statistics", async (ApplicationDbContext db, int? cashierId = null) =>
{
    var query = db.CurrencyExchanges.AsQueryable();

    if (cashierId.HasValue)
    {
        query = query.Where(ce => ce.CashierId == cashierId.Value);
    }

    var stats = new
    {
        TotalOperations = await query.CountAsync(),
        BuyOperations = await query.CountAsync(ce => ce.Type == ExchangeType.Buy),
        SellOperations = await query.CountAsync(ce => ce.Type == ExchangeType.Sell),
        TotalProfit = await query.SumAsync(ce => (decimal?)ce.Profit) ?? 0m,
        TotalJodAmount = await query.SumAsync(ce => (decimal?)ce.JodAmount) ?? 0m
    };

    return Results.Ok(stats);
})
.WithName("GetCurrencyExchangeStatistics")
.WithOpenApi();

app.Run();

// تشفير كلمة المرور
string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var bytes = System.Text.Encoding.UTF8.GetBytes(password);
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
}

// التحقق من كلمة المرور
bool VerifyPassword(string password, string hash)
{
    var passwordHash = HashPassword(password);
    return passwordHash == hash;
}

// نموذج لطلب الإرجاع مع الملاحظات
public record ReturnRequest(string Reference, string? Notes = null);

// نماذج للمصادقة
public record LoginRequest(string Username, string Password);
public record CreateCashierRequest(
    string CashierId,
    string Password,
    string EmployeeName,
    string? Department = null,
    string? PhoneNumber = null,
    string? Role = null
);
public record ChangePasswordRequest(string Username, string OldPassword, string NewPassword);
public record ResetPasswordRequest(string NewPassword);
public record ChangeRoleRequest(string NewRole);

// نماذج لإدارة الأرصدة
public record AddBalanceRequest(decimal Amount, string? Notes = null);
public record SetInitialBalanceRequest(decimal InitialBalance);

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
