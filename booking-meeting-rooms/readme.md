# Booking Meeting Rooms API

API для сервісу бронювання переговорних кімнат в компанії.

## Архітектура

Проект реалізовано з використанням **Clean Architecture** з наступною структурою:

```
src/
├── Api/                    # Web API слой (Controllers, Middleware)
├── Application/            # Application слой (DTOs, Validators, Mappings, Interfaces)
│   ├── Common/
│   │   └── Interfaces/    # Інтерфейси для Application слою
│   └── Features/          # Feature-based організація
│       ├── Rooms/         # Rooms feature (DTOs, Validators, Mappings)
│       └── Bookings/      # Bookings feature (DTOs, Validators, Mappings)
├── Domain/                # Domain слой (Entities, Value Objects, Domain Events, Specifications)
│   ├── Common/           # Базові класи (Entity, ValueObject, DomainEvent)
│   ├── Entities/         # Доменні сутності (Room, BookingRequest, BookingStatusTransition)
│   ├── ValueObjects/     # Value Objects (TimeSlot, Email)
│   ├── Enums/            # Перерахування (BookingStatus)
│   ├── Events/           # Domain Events
│   ├── Exceptions/       # Доменні винятки
│   └── Specifications/  # Specifications для бізнес-логіки
└── Infrastructure/       # Infrastructure слой (EF Core, Services, Middleware)
    ├── Data/            # DbContext, Configurations, Settings
    ├── Services/        # Infrastructure сервіси (BookingConflictChecker, TimeSlotValidator)
    └── Middleware/      # Middleware (Authentication, Exception Handling)
```

### Принципи архітектури:

- **Domain-Driven Design**: Вся бізнес-логіка та правила переходів станів знаходяться в Domain шарі
- **Dependency Inversion**: Application та Infrastructure залежать від Domain через інтерфейси
- **Separation of Concerns**: Кожен шар має чітко визначені відповідальності
- **SOLID принципи**: Використання інтерфейсів, dependency injection, single responsibility

### Патерни:

- **State Pattern**: Реалізовано через методи переходів станів у `BookingRequest`
- **Domain Events**: Події для всіх переходів станів (`BookingSubmittedEvent`, `BookingConfirmedEvent`, тощо)
- **Specification Pattern**: `BookingConflictSpecification` для перевірки конфліктів
- **Value Objects**: `TimeSlot`, `Email` з валідацією та інваріантами
- **Repository Pattern**: Використання `IApplicationDbContext` як абстракції над EF Core

## Запуск проекту

### Вимоги

- .NET 10.0 SDK
- PostgreSQL 12+ (або Docker для запуску PostgreSQL)

### Крок 1: Налаштування бази даних

Створіть базу даних PostgreSQL:

```sql
CREATE DATABASE booking_meeting_rooms;
CREATE DATABASE booking_meeting_rooms_dev;
```

Або використайте Docker:

```bash
docker run --name postgres-booking -e POSTGRES_PASSWORD=postgres -e POSTGRES_USER=postgres -p 5432:5432 -d postgres:15
```

### Крок 2: Налаштування конфігурації

Оновіть `appsettings.json` або `appsettings.Development.json` з правильними даними підключення:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=booking_meeting_rooms;Username=postgres;Password=postgres"
  },
  "BookingSettings": {
    "MaxTimeSlotHours": 4,
    "CheckSubmittedForConflicts": false
  }
}
```

**Параметри конфігурації:**
- `MaxTimeSlotHours` - максимальна тривалість бронювання (за замовчуванням 4 години)
- `CheckSubmittedForConflicts` - чи перевіряти конфлікти з `Submitted` бронюваннями (за замовчуванням `false`, перевіряються тільки `Confirmed`)

### Крок 3: Застосування міграцій

```bash
dotnet ef database update
```

Або міграції застосуються автоматично при запуску в Development режимі.

### Крок 4: Запуск додатку

```bash
dotnet run
```

Або через Visual Studio / Rider.

Додаток буде доступний за адресою:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

## Авторизація

API використовує спрощену авторизацію через HTTP заголовки:

- **X-UserId** (обов'язково) - ID користувача (Guid)
- **X-Role** (опційно) - Роль користувача: `Employee` або `Admin`

**Приклад:**
```bash
curl -H "X-UserId: 00000000-0000-0000-0000-000000000001" \
     -H "X-Role: Admin" \
     https://localhost:5001/api/rooms
```

## API Endpoints

### Rooms

- `POST /api/rooms` - Створити кімнату (Admin)
- `GET /api/rooms` - Список кімнат з фільтрацією
- `GET /api/rooms/{id}` - Отримати кімнату за ID
- `PUT /api/rooms/{id}` - Оновити кімнату (Admin)
- `DELETE /api/rooms/{id}` - Деактивувати кімнату (Admin)

### Bookings

- `POST /api/bookings` - Створити запит на бронювання (Draft)
- `POST /api/bookings/{id}/submit` - Відправити запит (Draft → Submitted)
- `POST /api/bookings/{id}/confirm` - Підтвердити бронювання (Admin, Submitted → Confirmed)
- `POST /api/bookings/{id}/decline` - Відхилити бронювання (Admin, Submitted → Declined)
- `POST /api/bookings/{id}/cancel` - Скасувати бронювання (Confirmed → Cancelled)
- `GET /api/bookings/{id}` - Отримати деталі бронювання з історією
- `GET /api/bookings` - Пошук бронювань з фільтрацією

## Приклади API запитів

Детальні приклади використання API дивіться у файлі [API_EXAMPLES.md](API_EXAMPLES.md).

## Конкурентність та конфлікти

### Підхід до конкурентності:

1. **Optimistic Concurrency**: Використання `RowVersion` (timestamp) у `BookingRequest` для виявлення одночасних змін
2. **Перевірка конфліктів**: Ефективна перевірка через SQL запити з індексами, без завантаження всіх бронювань в пам'ять
3. **Транзакції**: Використання транзакцій EF Core для атомарності операцій

### Правила конфліктів:

- За замовчуванням перевіряються тільки `Confirmed` бронювання
- Можна увімкнути перевірку `Submitted` через параметр `CheckSubmittedForConflicts` в конфігурації
- Перевірка виконується перед `Submit` та `Confirm` операціями

## Логування

Використовується **Serilog** для логування:
- Консольний вивід
- Файли логів у папці `logs/` (ротація щодня)
- Логування всіх ключових операцій (створення, переходи станів, помилки)

## Технології

- **.NET 10.0** - платформа
- **ASP.NET Core** - Web API фреймворк
- **Entity Framework Core 10.0** - ORM
- **PostgreSQL** - база даних
- **FluentValidation** - валідація
- **Serilog** - логування
- **Swashbuckle (Swagger)** - документація API

## Структура комітів

Проект розвивався поетапно з логічними комітами:

1. Налаштування проекту: структура папок Clean Architecture, базові пакети, DbContext, конфігурація
2. Domain модель: Room, BookingRequest, Value Objects (TimeSlot), State pattern, Domain Events
3. EF Core міграції та Infrastructure реалізація (репозиторії, конфігурація)
4. API для Rooms: CRUD операції з фільтрацією
5. API для BookingRequest: створення та workflow переходи (Submit, Confirm, Decline, Cancel)
6. Пошук/фільтрація бронювань, обробка помилок, документація, приклади API
