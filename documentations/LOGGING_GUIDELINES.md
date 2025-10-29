# Рекомендації щодо логування - Що дійсно потрібно логувати

## ❌ Що ЗАРАЗ логується занадто багато

### 1. RequestLoggingMiddleware - логує КОЖЕН HTTP запит
**Проблема**: Логує початок і кінець КОЖНОГО запиту (включаючи статичні файли, CSS, JS, зображення)

**Рішення**: 
- ❌ Прибрати з production або логувати тільки помилки
- ✅ Залишити тільки для API endpoints (/api/*)
- ✅ Виключити статичні файли (css, js, images, fonts)

### 2. AuthenticationDiagnosticMiddleware - логує ДВІЧІ
**Проблема**: Логує BEFORE і AFTER для кожного API/Account запиту з детальною інформацією про cookies та claims

**Рішення**:
- ❌ Прибрати з production (тільки для development діагностики)
- ✅ Або логувати тільки Warning/Error рівні

### 3. AuthController - надто детальне логування
**Проблема**: Логує всі спроби входу (включаючи успішні), всі refresh token операції

**Що залишити**:
- ✅ Failed login attempts (LogWarning) - ВАЖЛИВО для безпеки
- ✅ Security events (password reset, token revocation) - ВАЖЛИВО
- ❌ Успішні логини (LogInformation) - прибрати або перевести на Debug
- ❌ Використання існуючих refresh tokens - прибрати

### 4. UserService - занадто багато LogInformation
**Проблема**: Логує всі успішні операції (update, change password, delete)

**Що залишити**:
- ✅ LogWarning - всі помилки та невалідні спроби - ЗАЛИШИТИ
- ✅ LogError - всі винятки - ЗАЛИШИТИ
- ❌ LogInformation - успішні операції - прибрати або Debug

## ✅ Що ПОВИННО логуватися (Best Practices)

### 1. Security Events (LogWarning/LogError)
**ЗАВЖДИ логувати**:
- ✅ Failed authentication attempts (з IP, email, timestamp)
- ✅ Failed authorization attempts (доступ до захищених ресурсів)
- ✅ Password reset requests
- ✅ Password change attempts (успішні та невдалі)
- ✅ Token revocation
- ✅ Suspicious activity (багато невдалих спроб)

### 2. Errors (LogError)
**ЗАВЖДИ логувати**:
- ✅ Всі винятки (exceptions) з повним stack trace
- ✅ Database errors
- ✅ API errors
- ✅ Authentication/Authorization errors

### 3. Business Events (LogInformation - тільки важливі)
**Логувати тільки ключові події**:
- ✅ User registration (тільки успішна)
- ✅ Critical business operations
- ❌ Звичайні CRUD операції - НЕ логувати

### 4. Performance Monitoring (LogWarning/Debug)
**Для моніторингу продуктивності**:
- ✅ Повільні запити (> 1 секунда)
- ✅ Database queries що тривають довго
- ❌ Всі запити - НЕ логувати

## 📋 Рівні логування за середовищем

### Development
```
MinimumLevel: Debug (для діагностики)
- LogDebug: OK (детальна діагностика)
- LogInformation: OK (всі події)
- RequestLoggingMiddleware: OK
- AuthenticationDiagnosticMiddleware: OK
```

### Production
```
MinimumLevel: Warning (тільки проблеми)
- LogDebug: НЕ логувати
- LogInformation: Тільки критичні події
- LogWarning: Всі безпекові події та проблеми
- LogError: Всі помилки
- RequestLoggingMiddleware: Тільки помилки або виключити
- AuthenticationDiagnosticMiddleware: Виключити
```

## 🔧 Рекомендовані зміни

### 1. Оновити appsettings.json для Production
```json
"MinimumLevel": {
  "Default": "Warning",  // Змінити з Information
  "Override": {
    "Microsoft": "Warning",
    "Microsoft.AspNetCore": "Warning",
    "Microsoft.EntityFrameworkCore": "Warning",
    "CLTI.Diagnosis": "Warning",  // Змінити з Information
    "System": "Warning"
  }
}
```

### 2. Видалити або змінити Middleware
- RequestLoggingMiddleware: Логувати тільки API endpoints або тільки помилки
- AuthenticationDiagnosticMiddleware: Видалити з production, залишити тільки для development

### 3. Оновити AuthController
- Успішні логини: LogDebug замість LogInformation
- Refresh token операції: LogDebug або прибрати
- Залишити LogWarning для всіх failed attempts

### 4. Оновити UserService
- Успішні операції: LogDebug замість LogInformation
- Залишити LogWarning/LogError для помилок

## 🎯 Правило логування

**Логуй тільки те, що:**
1. ✅ Потрібно для діагностики проблем
2. ✅ Важливо для безпеки
3. ✅ Потрібно для аудиту (security events)
4. ✅ Вказує на помилки або проблеми

**НЕ логуй:**
1. ❌ Звичайні успішні операції
2. ❌ Кожен HTTP запит
3. ❌ Debug інформацію в production
4. ❌ Деталі автентифікації для кожного запиту

## 📊 Очікуваний результат після оптимізації

**Зараз**: ~1000+ записів на день (занадто багато)
**Після оптимізації**: ~50-100 записів на день (тільки важливі події)

**Переваги**:
- ✅ Менше навантаження на диск
- ✅ Швидший пошук у логах
- ✅ Менше обсяг лог-файлів
- ✅ Легше знаходити проблеми
- ✅ Краща продуктивність

---

*Рекомендації базуються на OWASP Logging Guide та .NET Logging Best Practices*

