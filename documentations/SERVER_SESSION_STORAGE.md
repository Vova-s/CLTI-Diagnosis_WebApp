# Server-Side Session Storage - Заміна LocalStorage

## 🎯 Проблема, яку вирішуємо

**Локальне зберігання (LocalStorage)** має проблеми безпеки:
- ❌ Доступне через JavaScript (XSS атаки)
- ❌ Не працює якщо користувач блокує cookies
- ❌ Дані зберігаються на клієнті (можна красти, модифікувати)

**Рішення: Server-Side Session Storage**
- ✅ Дані зберігаються на сервері (безпечніше)
- ✅ Працює навіть якщо cookies заблоковані (через headers)
- ✅ Недоступні для JavaScript (захист від XSS)

## 📋 Як це працює

### 1. Архітектура

```
Client (Browser)                    Server (IIS)
┌─────────────┐                    ┌──────────────┐
│             │                    │              │
│ Login       │─── POST /api/login ──>│ AuthController│
│             │                    │              │
│             │<── Session ID ─────│ Save tokens │
│             │    (in header)     │ in DB Cache │
│             │                    │              │
│ API calls  │─── Header ───────>│ Read from   │
│             │    X-Session-Id     │ Session     │
│             │                    │              │
└─────────────┘                    └──────────────┘
```

### 2. Механізм роботи

**Development (In-Memory Cache):**
- ✅ Швидко
- ❌ Втрачається при перезапуску сервера

**Production (SQL Server Distributed Cache):**
- ✅ Зберігається в базі даних
- ✅ Переживає перезапуски сервера
- ✅ Працює в кластері (колищно кілька серверів)

### 3. Ідентифікація сесії

**Спробує в такому порядку:**
1. Cookie `.AspNetCore.Session` (якщо cookies дозволені)
2. Header `X-Session-Id` (якщо cookies заблоковані)
3. Генерує новий ID (якщо нічого не знайдено)

## 🔧 Налаштування для IIS

### Крок 1: Створити таблицю для Cache (автоматично)

При першому запуску на Production, .NET автоматично створить таблицю `SessionCache` в базі даних.

**АБО створити вручну:**

```sql
-- Запустіть в SQL Server Management Studio
CREATE TABLE [dbo].[SessionCache] (
    [Id] NVARCHAR(449) NOT NULL,
    [Value] VARBINARY(MAX) NOT NULL,
    [ExpiresAtTime] DATETIME2 NOT NULL,
    [SlidingExpirationInSeconds] BIGINT NULL,
    [AbsoluteExpiration] DATETIME2 NULL,
    CONSTRAINT [PK_SessionCache] PRIMARY KEY ([Id])
);

CREATE INDEX [IX_SessionCache_ExpiresAtTime] ON [dbo].[SessionCache]([ExpiresAtTime]);
```

### Крок 2: Налаштування IIS

1. **Переконайтеся, що Application Pool має права на базу даних**

2. **Налаштування Session State Provider (опціонально)**

У `appsettings.Production.json` можна додати:
```json
{
  "DistributedCache": {
    "ConnectionString": "Server=...;Database=...;...",
    "SchemaName": "dbo",
    "TableName": "SessionCache"
  }
}
```

### Крок 3: Перевірка роботи

Після деплою перевірте:
1. Виконайте login
2. Перевірте чи в базі даних з'явилася таблиця `SessionCache`
3. Перевірте логи - має бути "Token stored in server session"

## 🔐 Безпека

### Переваги:
- ✅ Токени не зберігаються на клієнті
- ✅ Недоступні через JavaScript (XSS protection)
- ✅ Автоматичне видалення після expiry
- ✅ Працює навіть якщо cookies заблоковані

### Налаштування безпеки:
- HttpOnly cookies (якщо доступні)
- Secure cookies (в HTTPS)
- SameSite=Lax (CSRF protection)
- Session timeout: 30 хвилин без активності

## 📝 Використання в коді

### Server-side (AuthController):
```csharp
// Збереження токенів
await _sessionStorage.SetTokenAsync(token, rememberMe);
await _sessionStorage.SetRefreshTokenAsync(refreshToken, rememberMe);
await _sessionStorage.SetUserAsync(userDto);

// Отримання токенів
var token = await _sessionStorage.GetTokenAsync();
var user = await _sessionStorage.GetUserAsync<UserDto>();
```

### Client-side (Blazor):
Токени автоматично передаються через сервер, клієнт не має доступу до них безпосередньо.

## ⚠️ Важливо

1. **Сесія має бути увімкнена перед Authentication middleware**
   ```csharp
   app.UseSession(); // Першим
   app.UseAuthentication();
   ```

2. **Для Production обов'язково використовувати SQL Server Cache** (не In-Memory)

3. **Session timeout**: За замовчуванням 30 хвилин, можна налаштувати в `AddSession()`**

4. **Моніторинг**: Стежте за розміром таблиці `SessionCache`, робіть cleanup старих записів

## 🚀 Деплой на IIS

1. Опублікуйте додаток
2. Переконайтеся що connection string правильний
3. Перевірте що Application Pool має права на БД
4. Створіть таблицю SessionCache (запустіть `Scripts/CreateSessionCacheTable.sql`)

## ⚠️ Важливо: Сесії після рестарту сервера

### Як це працює:

1. **Data Protection Keys** зберігаються в `Keys/` папці - **не видаляйте їх!**
2. **Session Cache** зберігається в SQL Server - дані **переживають рестарт**
3. **Session Cookie** зашифрована Data Protection ключами - працює якщо ключі не змінилися

### Якщо користувачі втрачають сесію після рестарту:

**Причина**: Data Protection ключі змінилися або були видалені.

**Рішення**:
1. Переконайтеся що папка `Keys/` існує і не видаляється
2. Додайте `Keys/` до `.gitignore` (не комітьте ключі!)
3. У production: використовуйте Azure Key Vault або shared key storage для кількох серверів

### Налаштування для production з кількома серверами:

```csharp
// У production для кількох серверів використовуйте shared key storage
builder.Services.AddDataProtection()
    .PersistKeysToAzureBlobStorage(...) // Або інше shared сховище
    .SetApplicationName("CLTI.Diagnosis")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
```

**Результат**: Токени зберігаються безпечно на сервері, **сесії переживають рестарти**! 🎉

