# Booking Meeting Rooms API

API для сервісу бронювання переговорних кімнат в компанії.

## Архітектура

Проект реалізовано з використанням **Clean Architecture** з наступною структурою:

```
src/
├── Api/                    # Web API слой (Controllers, Middleware, Filters, DTOs)
│   ├── Controllers/       # API контроллери (RoomsController, BookingsController)
│   ├── Dtos/              # DTO для API відповідей (ErrorResponseDto)
│   ├── Filters/           # Swagger фільтри (SwaggerHeaderOperationFilter)
│   └── Middleware/        # Middleware (GlobalExceptionHandlerMiddleware)
├── Application/            # Application слой (DTOs, Mappings, Interfaces)
│   ├── Common/
│   │   └── Interfaces/    # Інтерфейси для Application слою
│   └── Features/          # Feature-based організація
│       ├── Rooms/         # Rooms feature (DTOs, Mappings)
│       └── Bookings/      # Bookings feature (DTOs, Mappings)
├── Domain/                # Domain слой (Entities, Value Objects, Enums, Exceptions)
│   ├── Common/           # Базові класи (Entity, ValueObject)
│   ├── Entities/         # Доменні сутності (Room, BookingRequest, BookingStatusTransition, User)
│   ├── ValueObjects/     # Value Objects (TimeSlot)
│   ├── Enums/            # Перерахування (BookingStatus, UserRole)
│   └── Exceptions/       # Доменні винятки (DomainException)
├── Infrastructure/       # Infrastructure слой (EF Core, Services, Middleware)
│   ├── Data/            # DbContext, Configurations, Settings, PostgreSqlConnection
│   ├── Services/        # Infrastructure сервіси (BookingConflictChecker, TimeSlotValidator)
│   └── Middleware/      # Middleware (HeaderAuthenticationHandler)
├── MigrationRunner.cs   # Утиліта для застосування міграцій
└── Program.cs            # Точка входу додатку
```

### Принципи архітектури:

- **Domain-Driven Design**: Вся бізнес-логіка та правила переходів станів знаходяться в Domain шарі
- **Dependency Inversion**: Application та Infrastructure залежать від Domain через інтерфейси
- **Separation of Concerns**: Кожен шар має чітко визначені відповідальності
- **SOLID принципи**: Використання інтерфейсів, dependency injection, single responsibility

### Патерни:

- **State Pattern**: Реалізовано через методи переходів станів у `BookingRequest`
- **Value Objects**: `TimeSlot` з валідацією та інваріантами
- **Repository Pattern**: Використання `IApplicationDbContext` як абстракції над EF Core
- **Header-based Authentication**: Спрощена автентифікація через HTTP заголовки `X-UserId` та `X-Role`

## Запуск проекту

### Вимоги

- .NET 10.0 SDK
- PostgreSQL 12+ (або Docker для запуску PostgreSQL)

### Крок 1: Налаштування PostgreSQL

Запустіть PostgreSQL сервер. Можна використати Docker:

```bash
docker run --name postgres-booking -e POSTGRES_PASSWORD=postgres -e POSTGRES_USER=postgres -p 5432:5432 -d postgres:15
```

**Примітка:** База даних буде створена автоматично під час застосування міграцій, якщо її не існує.

### Крок 2: Налаштування конфігурації

Оновіть `appsettings.json` з правильними даними підключення:

```json
{
  "PostgreSqlConnection": {
    "Host": "localhost",
    "Port": 5432,
    "Database": "booking_meeting_rooms",
    "Username": "postgres",
    "Password": "postgres"
  },
  "BookingSettings": {
    "MaxTimeSlotHours": 4,
    "CheckSubmittedForConflicts": false
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  }
}
```

**Параметри конфігурації:**
- `PostgreSqlConnection` - параметри підключення до PostgreSQL
  - `Host` - адреса сервера
  - `Port` - порт (за замовчуванням 5432)
  - `Database` - назва бази даних
  - `Username` - ім'я користувача
  - `Password` - пароль
- `BookingSettings` - налаштування бронювань
  - `MaxTimeSlotHours` - максимальна тривалість бронювання (за замовчуванням 4 години)
  - `CheckSubmittedForConflicts` - чи перевіряти конфлікти з `Submitted` бронюваннями (за замовчуванням `false`, перевіряються тільки `Confirmed`)

### Крок 3: Застосування міграцій

Міграції застосовуються через окрему утиліту з ключем `migrate`:

```bash
# Після збірки проекту
dotnet build

# Застосування міграцій (створює базу даних, якщо її немає, застосовує міграції та додає seed дані)
dotnet run -- migrate

# Або зі скомпільованого додатку
cd bin/Debug/net10.0
dotnet booking-meeting-rooms.dll migrate
# або
booking-meeting-rooms.exe migrate
```

**Що робить команда `migrate`:**
1. Перевіряє існування бази даних, створює її якщо не існує
2. Застосовує всі міграції до бази даних
3. Додає seed дані (якщо таблиці порожні):
   - **Користувачі:**
     - ID: 1 - Адміністратор (Admin, admin@company.com)
     - ID: 2 - Співробітник 1 (Employee, employee1@company.com)
     - ID: 3 - Співробітник 2 (Employee, employee2@company.com)
   - **Кімнати:** 5 прикладних кімнат (Конференц-зал A, Конференц-зал B, Мала переговорна, Велика переговорна, Кімната для переговорів)

**Примітка:** Міграції **не** застосовуються автоматично при запуску додатку. Вони застосовуються тільки через команду `migrate`.

### Крок 4: Запуск додатку

```bash
dotnet run
```

Або через Visual Studio / Rider.

Додаток буде доступний за адресою:
- HTTP: `http://localhost:5000`
- Swagger UI: `http://localhost:5000` (доступний завжди, не тільки в Development)

## Авторизація

API використовує спрощену авторизацію через HTTP заголовки:

- **X-UserId** (обов'язково) - ID користувача (числовий ID, наприклад: 1, 2, 3)
- **X-Role** (обов'язково) - Роль користувача: `Employee` або `Admin`

**Перевірка авторизації:**
- `X-UserId` перевіряється на формат (має бути числом) та існування користувача в базі даних
- `X-Role` перевіряється на валідність (`Employee` або `Admin`) та відповідність ролі користувача з бази даних
- Якщо роль з заголовка не відповідає ролі користувача в базі, повертається помилка авторизації

**Приклад:**
```bash
curl -H "X-UserId: 1" \
     -H "X-Role: Admin" \
     http://localhost:5000/api/rooms
```

## Права доступу

### Admin (ID: 1)
- ✅ Створювати, оновлювати, видаляти кімнати
- ✅ Підтверджувати та відхиляти бронювання
- ✅ Бачити всі бронювання

### Employee (ID: 2, 3)
- ✅ Створювати бронювання
- ✅ Відправляти свої бронювання (Draft → Submitted)
- ✅ Скасовувати свої підтверджені бронювання (Confirmed → Cancelled)
- ✅ Бачити тільки свої бронювання
- ❌ Не може підтверджувати/відхиляти бронювання
- ❌ Не може керувати кімнатами

## API Endpoints

### Rooms

- `POST /api/rooms` - Створити кімнату (Admin)
- `GET /api/rooms` - Список кімнат з фільтрацією (location, minCapacity, isActive)
- `GET /api/rooms/{id}` - Отримати кімнату за ID
- `PUT /api/rooms/{id}` - Оновити кімнату (Admin, часткове оновлення - всі поля опціональні)
- `DELETE /api/rooms/{id}` - Видалити кімнату (Admin, фізичне видалення)

### Bookings

- `POST /api/bookings` - Створити запит на бронювання (Draft)
- `POST /api/bookings/{id}/submit` - Відправити запит (Draft → Submitted, тільки свої для Employee)
- `POST /api/bookings/{id}/confirm` - Підтвердити бронювання (Admin, Submitted → Confirmed)
- `POST /api/bookings/{id}/decline` - Відхилити бронювання (Admin, Submitted → Declined)
- `POST /api/bookings/{id}/cancel` - Скасувати бронювання (Confirmed → Cancelled, тільки свої для Employee)
- `DELETE /api/bookings/{id}` - Видалити бронювання (Admin - будь-які, Employee - тільки свої Draft)
- `GET /api/bookings/{id}` - Отримати деталі бронювання з історією переходів
- `GET /api/bookings` - Пошук бронювань з фільтрацією (from, to, roomId, status)

## Статуси бронювань

- `Draft` - Чернетка (тільки створена)
- `Submitted` - Відправлено на розгляд
- `Confirmed` - Підтверджено
- `Declined` - Відхилено
- `Cancelled` - Скасовано

## Workflow переходів

```
Draft → Submitted → Confirmed → Cancelled
              ↓
          Declined
```

## Приклади API запитів

- [postman_collection.json](postman_collection.json) - Колекція запитів для Postman

### Базові приклади

**Отримати список кімнат:**
```bash
curl -H "X-UserId: 1" \
     -H "X-Role: Admin" \
     http://localhost:5000/api/rooms
```

**Створити бронювання (Employee):**
```bash
curl -X POST "http://localhost:5000/api/bookings" \
  -H "Content-Type: application/json" \
  -H "X-UserId: 2" \
  -H "X-Role: Employee" \
  -d '{
    "roomId": 1,
    "startAt": "2026-02-10T10:00:00Z",
    "endAt": "2026-02-10T12:00:00Z",
    "participantEmails": ["user1@example.com", "user2@example.com"],
    "description": "Встреча по проекту"
  }'
```

**Підтвердити бронювання (Admin):**
```bash
curl -X POST "http://localhost:5000/api/bookings/1/confirm" \
  -H "X-UserId: 1" \
  -H "X-Role: Admin"
```

**Детальні приклади:** Повний список прикладів з CURL командами дивіться у файлі [CURL_EXAMPLES.md](docs/CURL_EXAMPLES.md).

## Конкурентність та конфлікти

### Підхід до конкурентності:

1. **Optimistic Concurrency**: Використання `RowVersion` (timestamp) у `BookingRequest` для виявлення одночасних змін
2. **Перевірка конфліктів**: Ефективна перевірка через SQL запити з індексами, без завантаження всіх бронювань в пам'ять
3. **Транзакції**: Використання транзакцій EF Core для атомарності операцій

### Правила конфліктів:

- За замовчуванням перевіряються тільки `Confirmed` бронювання
- Можна увімкнути перевірку `Submitted` через параметр `CheckSubmittedForConflicts` в конфігурації
- Перевірка виконується перед `Submit` та `Confirm` операціями

## Обробка помилок

Всі помилки повертаються в уніфікованому форматі `ErrorResponseDto`:

```json
{
  "error": "ErrorCode",
  "message": "Human-readable message",
  "details": "Additional details",
  "statusCode": 400,
  "validationErrors": {
    "FieldName": ["Error message 1", "Error message 2"]
  }
}
```

Типи помилок:
- `400 Bad Request` - помилки валідації або невалідні переходи станів
- `401 Unauthorized` - відсутні або невалідні заголовки авторизації
- `403 Forbidden` - недостатньо прав для виконання операції
- `404 Not Found` - ресурс не знайдено
- `409 Conflict` - конфлікт бронювань або обмеження цілісності даних
- `500 Internal Server Error` - внутрішні помилки сервера

## Логування

Використовується **Serilog** для логування:
- Консольний вивід з кольоровим форматуванням
- Файли логів у папці `logs/` (ротація щодня)
- Логування всіх ключових операцій (створення, переходи станів, помилки)
- Логи міграцій зберігаються окремо в `logs/migration-*.log`

## Технології

- **.NET 10.0** - платформа
- **ASP.NET Core** - Web API фреймворк
- **Entity Framework Core 10.0** - ORM
- **PostgreSQL** - база даних
- **Npgsql** - провайдер PostgreSQL для EF Core
- **Serilog** - логування
- **Swashbuckle (Swagger)** - документація API

## Міграції бази даних

### Створення нової міграції

```bash
dotnet ef migrations add НазваМіграції
```

### Застосування міграцій

```bash
# Через утиліту migrate
dotnet run -- migrate

# Або зі скомпільованого додатку
booking-meeting-rooms.exe migrate
```

### Перевірка статусу міграцій

```bash
dotnet ef migrations list
```

**Важливо:**
- Міграції застосовуються тільки через команду `migrate`
- База даних створюється автоматично, якщо її не існує
- Seed дані додаються автоматично після застосування міграцій (якщо таблиці порожні)

## Розробка

### Структура комітів

Проект розвивався поетапно з логічними комітами:

1. Налаштування проекту: структура папок Clean Architecture, базові пакети, DbContext, конфігурація
2. Domain модель: Room, BookingRequest, User, Value Objects (TimeSlot), State pattern
3. EF Core міграції та Infrastructure реалізація (конфігурація, сервіси)
4. API для Rooms: CRUD операції з фільтрацією
5. API для BookingRequest: створення та workflow переходи (Submit, Confirm, Decline, Cancel)
6. Авторизація через заголовки з перевіркою в базі даних
7. Утиліта для міграцій з автоматичним створенням бази даних та seed даними
8. Пошук/фільтрація бронювань, обробка помилок, документація, приклади API

### Запуск для розробки

```bash
# 1. Застосувати міграції
dotnet run -- migrate

# 2. Запустити додаток
dotnet run

# 3. Відкрити Swagger UI
# http://localhost:5000
```

## Додаткова документація

- [CURL_EXAMPLES.md](docs/CURL_EXAMPLES.md) - детальні приклади використання API з CURL командами
- [MIGRATION_GUIDE.md](docs/MIGRATION_GUIDE.md) - детальний гайд по роботі з міграціями
