# Приклади використання API

## Авторизація

Всі запити потребують заголовків для авторизації:
- `X-UserId` - ID користувача (обов'язково, числовий ID, наприклад: 1, 2, 3)
- `X-Role` - Роль користувача: `Employee` або `Admin` (обов'язково)

**Примітка:** Після застосування міграцій створюються такі користувачі:
- ID: 1 - Адміністратор (Admin, admin@company.com)
- ID: 2 - Співробітник 1 (Employee, employee1@company.com)
- ID: 3 - Співробітник 2 (Employee, employee2@company.com)

## Базовий URL

За замовчуванням додаток працює на: `http://localhost:5000`

## Rooms API

### 1. Створити кімнату (Admin)

```bash
curl -X POST "http://localhost:5000/api/rooms" \
  -H "Content-Type: application/json" \
  -H "X-UserId: 1" \
  -H "X-Role: Admin" \
  -d '{
    "name": "Конференц-зал A",
    "capacity": 20,
    "location": "Поверх 1",
    "isActive": true
  }'
```

**Відповідь (201 Created):**
```json
{
  "id": 1,
  "name": "Конференц-зал A",
  "capacity": 20,
  "location": "Поверх 1",
  "isActive": true,
  "createdAt": "2024-02-01T10:00:00Z",
  "updatedAt": null
}
```

### 2. Отримати список кімнат з фільтрацією

```bash
# Всі активні кімнати
curl -X GET "http://localhost:5000/api/rooms?isActive=true" \
  -H "X-UserId: 1" \
  -H "X-Role: Admin"

# Кімнати на певному поверсі з мінімальною місткістю
curl -X GET "http://localhost:5000/api/rooms?location=Поверх%201&minCapacity=10" \
  -H "X-UserId: 2" \
  -H "X-Role: Employee"

# Всі кімнати (включно з неактивними)
curl -X GET "http://localhost:5000/api/rooms" \
  -H "X-UserId: 1" \
  -H "X-Role: Admin"
```

**Параметри фільтрації:**
- `location` (string, опціонально) - фільтр по локації
- `minCapacity` (int, опціонально) - мінімальна місткість
- `isActive` (bool, опціонально) - фільтр по активності

### 3. Отримати кімнату за ID

```bash
curl -X GET "http://localhost:5000/api/rooms/1" \
  -H "X-UserId: 1" \
  -H "X-Role: Admin"
```

**Відповідь (200 OK):**
```json
{
  "id": 1,
  "name": "Конференц-зал A",
  "capacity": 20,
  "location": "Поверх 1",
  "isActive": true,
  "createdAt": "2024-02-01T10:00:00Z",
  "updatedAt": null
}
```

### 4. Оновити кімнату (Admin) - часткове оновлення

```bash
# Оновити тільки назву та місткість
curl -X PUT "http://localhost:5000/api/rooms/1" \
  -H "Content-Type: application/json" \
  -H "X-UserId: 1" \
  -H "X-Role: Admin" \
  -d '{
    "name": "Конференц-зал A (оновлено)",
    "capacity": 25
  }'

# Оновити тільки статус активності
curl -X PUT "http://localhost:5000/api/rooms/1" \
  -H "Content-Type: application/json" \
  -H "X-UserId: 1" \
  -H "X-Role: Admin" \
  -d '{
    "isActive": false
  }'

# Повне оновлення всіх полів
curl -X PUT "http://localhost:5000/api/rooms/1" \
  -H "Content-Type: application/json" \
  -H "X-UserId: 1" \
  -H "X-Role: Admin" \
  -d '{
    "name": "Конференц-зал A (оновлено)",
    "capacity": 25,
    "location": "Поверх 1",
    "isActive": true
  }'
```

**Примітка:** Всі поля в `UpdateRoomDto` опціональні. Оновлюються тільки передані поля.

### 5. Видалити кімнату (Admin)

```bash
curl -X DELETE "http://localhost:5000/api/rooms/1" \
  -H "X-UserId: 1" \
  -H "X-Role: Admin"
```

**Відповідь (200 OK):**
```json
{
  "message": "Room with id 1 has been deleted successfully"
}
```

**Примітка:** Якщо кімната має пов'язані бронювання, повертається помилка 409 Conflict.

## Bookings API

### 1. Створити запит на бронювання (Draft)

```bash
curl -X POST "http://localhost:5000/api/bookings" \
  -H "Content-Type: application/json" \
  -H "X-UserId: 2" \
  -H "X-Role: Employee" \
  -d '{
    "roomId": 1,
    "startAt": "2024-02-15T10:00:00Z",
    "endAt": "2024-02-15T11:00:00Z",
    "participantEmails": [
      "user1@example.com",
      "user2@example.com"
    ],
    "description": "Планерка команди"
  }'
```

**Відповідь (201 Created):**
```json
{
  "id": 1,
  "roomId": 1,
  "roomName": "Конференц-зал A",
  "startAt": "2024-02-15T10:00:00Z",
  "endAt": "2024-02-15T11:00:00Z",
  "participantEmails": ["user1@example.com", "user2@example.com"],
  "description": "Планерка команди",
  "status": "Draft",
  "createdByUserId": 2,
  "createdAt": "2024-02-01T10:00:00Z",
  "statusTransitions": []
}
```

**Валідація:**
- `roomId` - обов'язкове, має існувати
- `startAt` - обов'язкове, має бути раніше `endAt`
- `endAt` - обов'язкове
- `participantEmails` - обов'язкове, мінімум 1 email, валідний формат
- `description` - обов'язкове, максимум 1000 символів

### 2. Відправити запит на бронювання (Draft → Submitted)

```bash
curl -X POST "http://localhost:5000/api/bookings/1/submit" \
  -H "X-UserId: 2" \
  -H "X-Role: Employee"
```

**Примітка:** Можуть відправляти тільки Employee і тільки свої власні бронювання.

**Відповідь (200 OK):**
```json
{
  "id": 1,
  "status": "Submitted",
  "statusTransitions": [
    {
      "id": 1,
      "fromStatus": "Draft",
      "toStatus": "Submitted",
      "transitionedAt": "2024-02-01T10:05:00Z",
      "transitionedByUserId": 2
    }
  ]
}
```

### 3. Підтвердити бронювання (Admin) - Submitted → Confirmed

```bash
curl -X POST "http://localhost:5000/api/bookings/1/confirm" \
  -H "X-UserId: 1" \
  -H "X-Role: Admin"
```

**Примітка:** Можуть підтверджувати тільки Admin.

**Відповідь (200 OK):**
```json
{
  "id": 1,
  "status": "Confirmed",
  "statusTransitions": [
    {
      "id": 1,
      "fromStatus": "Draft",
      "toStatus": "Submitted",
      "transitionedAt": "2024-02-01T10:05:00Z",
      "transitionedByUserId": 2
    },
    {
      "id": 2,
      "fromStatus": "Submitted",
      "toStatus": "Confirmed",
      "transitionedAt": "2024-02-01T10:10:00Z",
      "transitionedByUserId": 1
    }
  ]
}
```

### 4. Відхилити бронювання (Admin) - Submitted → Declined

```bash
curl -X POST "http://localhost:5000/api/bookings/1/decline" \
  -H "Content-Type: application/json" \
  -H "X-UserId: 1" \
  -H "X-Role: Admin" \
  -d '{
    "reason": "Кімната вже зайнята на цей час"
  }'
```

**Примітка:** Можуть відхиляти тільки Admin.

**Відповідь (200 OK):**
```json
{
  "id": 1,
  "status": "Declined",
  "statusTransitions": [
    {
      "id": 1,
      "fromStatus": "Draft",
      "toStatus": "Submitted",
      "transitionedAt": "2024-02-01T10:05:00Z",
      "transitionedByUserId": 2
    },
    {
      "id": 2,
      "fromStatus": "Submitted",
      "toStatus": "Declined",
      "transitionedAt": "2024-02-01T10:10:00Z",
      "transitionedByUserId": 1,
      "reason": "Кімната вже зайнята на цей час"
    }
  ]
}
```

### 5. Скасувати підтверджене бронювання - Confirmed → Cancelled

```bash
curl -X POST "http://localhost:5000/api/bookings/1/cancel" \
  -H "Content-Type: application/json" \
  -H "X-UserId: 2" \
  -H "X-Role: Employee" \
  -d '{
    "reason": "Зустріч перенесена"
  }'
```

**Примітка:** Можуть скасовувати тільки Employee і тільки свої власні підтверджені бронювання.

**Відповідь (200 OK):**
```json
{
  "id": 1,
  "status": "Cancelled",
  "statusTransitions": [
    {
      "id": 1,
      "fromStatus": "Draft",
      "toStatus": "Submitted",
      "transitionedAt": "2024-02-01T10:05:00Z",
      "transitionedByUserId": 2
    },
    {
      "id": 2,
      "fromStatus": "Submitted",
      "toStatus": "Confirmed",
      "transitionedAt": "2024-02-01T10:10:00Z",
      "transitionedByUserId": 1
    },
    {
      "id": 3,
      "fromStatus": "Confirmed",
      "toStatus": "Cancelled",
      "transitionedAt": "2024-02-01T11:00:00Z",
      "transitionedByUserId": 2,
      "reason": "Зустріч перенесена"
    }
  ]
}
```

### 6. Отримати деталі бронювання з історією

```bash
curl -X GET "http://localhost:5000/api/bookings/1" \
  -H "X-UserId: 2" \
  -H "X-Role: Employee"
```

**Відповідь (200 OK):**
```json
{
  "id": 1,
  "roomId": 1,
  "roomName": "Конференц-зал A",
  "startAt": "2024-02-15T10:00:00Z",
  "endAt": "2024-02-15T11:00:00Z",
  "participantEmails": ["user1@example.com", "user2@example.com"],
  "description": "Планерка команди",
  "status": "Confirmed",
  "createdByUserId": 2,
  "createdAt": "2024-02-01T10:00:00Z",
  "statusTransitions": [
    {
      "id": 1,
      "fromStatus": "Draft",
      "toStatus": "Submitted",
      "transitionedAt": "2024-02-01T10:05:00Z",
      "transitionedByUserId": 2
    },
    {
      "id": 2,
      "fromStatus": "Submitted",
      "toStatus": "Confirmed",
      "transitionedAt": "2024-02-01T10:10:00Z",
      "transitionedByUserId": 1
    }
  ]
}
```

### 7. Видалити бронювання за ID

```bash
# Admin може видаляти будь-які бронювання
curl -X DELETE "http://localhost:5000/api/bookings/1" \
  -H "X-UserId: 1" \
  -H "X-Role: Admin"

# Employee може видаляти тільки свої чернетки (Draft)
curl -X DELETE "http://localhost:5000/api/bookings/1" \
  -H "X-UserId: 2" \
  -H "X-Role: Employee"
```

**Відповідь (200 OK):**
```json
{
  "message": "Booking request with id 1 has been deleted successfully"
}
```

**Права доступу:**
- **Admin**: може видаляти будь-які бронювання незалежно від статусу
- **Employee**: може видаляти тільки свої чернетки (Draft). Спроба видалити чужі або не-Draft бронювання поверне помилку 403 або 400

**Помилки:**
- `400 Bad Request` - Employee намагається видалити не-Draft бронювання
- `403 Forbidden` - Employee намагається видалити чужі бронювання
- `404 Not Found` - Бронювання не знайдено
- `409 Conflict` - Неможливо видалити через залежності в базі даних

### 8. Пошук бронювань з фільтрацією

```bash
# Всі бронювання користувача (Employee бачить тільки свої)
curl -X GET "http://localhost:5000/api/bookings" \
  -H "X-UserId: 2" \
  -H "X-Role: Employee"

# Бронювання за період
curl -X GET "http://localhost:5000/api/bookings?from=2024-02-01T00:00:00Z&to=2024-02-28T23:59:59Z" \
  -H "X-UserId: 2" \
  -H "X-Role: Employee"

# Бронювання конкретної кімнати
curl -X GET "http://localhost:5000/api/bookings?roomId=1" \
  -H "X-UserId: 2" \
  -H "X-Role: Employee"

# Тільки підтверджені бронювання
curl -X GET "http://localhost:5000/api/bookings?status=Confirmed" \
  -H "X-UserId: 2" \
  -H "X-Role: Employee"

# Комбінований пошук (Admin бачить всі бронювання)
curl -X GET "http://localhost:5000/api/bookings?from=2024-02-01T00:00:00Z&to=2024-02-28T23:59:59Z&roomId=1&status=Confirmed" \
  -H "X-UserId: 1" \
  -H "X-Role: Admin"
```

**Параметри фільтрації:**
- `from` (DateTime, опціонально) - початок періоду
- `to` (DateTime, опціонально) - кінець періоду
- `roomId` (int, опціонально) - ID кімнати
- `status` (string, опціонально) - статус бронювання (Draft, Submitted, Confirmed, Declined, Cancelled)

**Примітка:** 
- Employee бачить тільки свої бронювання
- Admin бачить всі бронювання

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

## Приклади помилок

### Помилка валідації (400 Bad Request)

```json
{
  "error": "ValidationError",
  "message": "One or more validation errors occurred",
  "details": null,
  "statusCode": 400,
  "validationErrors": {
    "RoomId": ["Room ID is required"],
    "StartAt": ["Start time must be before end time"],
    "ParticipantEmails[0]": ["Invalid email format"]
  }
}
```

### Конфлікт бронювань (409 Conflict)

```json
{
  "error": "BookingConflict",
  "message": "Booking conflict detected. Another confirmed booking exists for this room and time slot.",
  "details": null,
  "statusCode": 409,
  "validationErrors": null
}
```

### Невалідний перехід стану (400 Bad Request)

```json
{
  "error": "InvalidStatusTransition",
  "message": "Cannot transition from Confirmed to Submitted",
  "details": null,
  "statusCode": 400,
  "validationErrors": null
}
```

### Немає прав доступу (403 Forbidden)

```json
{
  "error": "Forbidden",
  "message": "You can only submit your own booking requests",
  "details": "Booking was created by user 2, but you are user 3",
  "statusCode": 403,
  "validationErrors": null
}
```

### Ресурс не знайдено (404 Not Found)

```json
{
  "error": "RoomNotFound",
  "message": "Room with id 999 not found",
  "details": null,
  "statusCode": 404,
  "validationErrors": null
}
```

### Не авторизовано (401 Unauthorized)

```json
{
  "error": "Unauthorized",
  "message": "User ID is required",
  "details": "Please provide X-UserId header",
  "statusCode": 401,
  "validationErrors": null
}
```

## Приклади використання з реальними даними

### Повний цикл бронювання

```bash
# 1. Створити бронювання (Employee)
curl -X POST "http://localhost:5000/api/bookings" \
  -H "Content-Type: application/json" \
  -H "X-UserId: 2" \
  -H "X-Role: Employee" \
  -d '{
    "roomId": 1,
    "startAt": "2024-02-15T10:00:00Z",
    "endAt": "2024-02-15T11:00:00Z",
    "participantEmails": ["employee1@company.com"],
    "description": "Щоденна планерка"
  }'

# 2. Відправити на розгляд (Employee)
curl -X POST "http://localhost:5000/api/bookings/1/submit" \
  -H "X-UserId: 2" \
  -H "X-Role: Employee"

# 3. Підтвердити (Admin)
curl -X POST "http://localhost:5000/api/bookings/1/confirm" \
  -H "X-UserId: 1" \
  -H "X-Role: Admin"

# 4. Переглянути деталі
curl -X GET "http://localhost:5000/api/bookings/1" \
  -H "X-UserId: 2" \
  -H "X-Role: Employee"

# 5. Скасувати (Employee)
curl -X POST "http://localhost:5000/api/bookings/1/cancel" \
  -H "Content-Type: application/json" \
  -H "X-UserId: 2" \
  -H "X-Role: Employee" \
  -d '{
    "reason": "Зустріч перенесена на інший час"
  }'
```
