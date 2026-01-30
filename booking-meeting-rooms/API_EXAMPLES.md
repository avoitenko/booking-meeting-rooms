# Приклади використання API

## Авторизація

Всі запити потребують заголовків для авторизації:
- `X-UserId` - ID користувача (обов'язково)
- `X-Role` - Роль користувача: `Employee` або `Admin` (опційно, але потрібно для адмін-операцій)

## Rooms API

### 1. Створити кімнату (Admin)

```bash
curl -X POST "https://localhost:5001/api/rooms" \
  -H "Content-Type: application/json" \
  -H "X-UserId: 00000000-0000-0000-0000-000000000001" \
  -H "X-Role: Admin" \
  -d '{
    "name": "Конференц-зал A",
    "capacity": 20,
    "location": "Поверх 1",
    "isActive": true
  }'
```

### 2. Отримати список кімнат з фільтрацією

```bash
# Всі активні кімнати
curl -X GET "https://localhost:5001/api/rooms?isActive=true" \
  -H "X-UserId: 00000000-0000-0000-0000-000000000001"

# Кімнати на певному поверсі з мінімальною місткістю
curl -X GET "https://localhost:5001/api/rooms?location=Поверх%201&minCapacity=10" \
  -H "X-UserId: 00000000-0000-0000-0000-000000000001"
```

### 3. Отримати кімнату за ID

```bash
curl -X GET "https://localhost:5001/api/rooms/{roomId}" \
  -H "X-UserId: 00000000-0000-0000-0000-000000000001"
```

### 4. Оновити кімнату (Admin)

```bash
curl -X PUT "https://localhost:5001/api/rooms/{roomId}" \
  -H "Content-Type: application/json" \
  -H "X-UserId: 00000000-0000-0000-0000-000000000001" \
  -H "X-Role: Admin" \
  -d '{
    "name": "Конференц-зал A (оновлено)",
    "capacity": 25,
    "location": "Поверх 1",
    "isActive": true
  }'
```

### 5. Деактивувати кімнату (Admin)

```bash
curl -X DELETE "https://localhost:5001/api/rooms/{roomId}" \
  -H "X-UserId: 00000000-0000-0000-0000-000000000001" \
  -H "X-Role: Admin"
```

## Bookings API

### 1. Створити запит на бронювання (Draft)

```bash
curl -X POST "https://localhost:5001/api/bookings" \
  -H "Content-Type: application/json" \
  -H "X-UserId: 00000000-0000-0000-0000-000000000002" \
  -d '{
    "roomId": "00000000-0000-0000-0000-000000000001",
    "startAt": "2024-02-01T10:00:00Z",
    "endAt": "2024-02-01T11:00:00Z",
    "participantEmails": [
      "user1@example.com",
      "user2@example.com"
    ],
    "description": "Планерка команди"
  }'
```

### 2. Відправити запит на бронювання (Draft → Submitted)

```bash
curl -X POST "https://localhost:5001/api/bookings/{bookingId}/submit" \
  -H "X-UserId: 00000000-0000-0000-0000-000000000002"
```

### 3. Підтвердити бронювання (Admin) - Submitted → Confirmed

```bash
curl -X POST "https://localhost:5001/api/bookings/{bookingId}/confirm" \
  -H "X-UserId: 00000000-0000-0000-0000-000000000001" \
  -H "X-Role: Admin"
```

### 4. Відхилити бронювання (Admin) - Submitted → Declined

```bash
curl -X POST "https://localhost:5001/api/bookings/{bookingId}/decline" \
  -H "Content-Type: application/json" \
  -H "X-UserId: 00000000-0000-0000-0000-000000000001" \
  -H "X-Role: Admin" \
  -d '{
    "reason": "Кімната вже зайнята на цей час"
  }'
```

### 5. Скасувати підтверджене бронювання - Confirmed → Cancelled

```bash
curl -X POST "https://localhost:5001/api/bookings/{bookingId}/cancel" \
  -H "Content-Type: application/json" \
  -H "X-UserId: 00000000-0000-0000-0000-000000000002" \
  -d '{
    "reason": "Зустріч перенесена"
  }'
```

### 6. Отримати деталі бронювання з історією

```bash
curl -X GET "https://localhost:5001/api/bookings/{bookingId}" \
  -H "X-UserId: 00000000-0000-0000-0000-000000000002"
```

### 7. Пошук бронювань з фільтрацією

```bash
# Всі бронювання користувача
curl -X GET "https://localhost:5001/api/bookings" \
  -H "X-UserId: 00000000-0000-0000-0000-000000000002"

# Бронювання за період
curl -X GET "https://localhost:5001/api/bookings?from=2024-02-01T00:00:00Z&to=2024-02-28T23:59:59Z" \
  -H "X-UserId: 00000000-0000-0000-0000-000000000002"

# Бронювання конкретної кімнати
curl -X GET "https://localhost:5001/api/bookings?roomId=00000000-0000-0000-0000-000000000001" \
  -H "X-UserId: 00000000-0000-0000-0000-000000000002"

# Тільки підтверджені бронювання
curl -X GET "https://localhost:5001/api/bookings?status=Confirmed" \
  -H "X-UserId: 00000000-0000-0000-0000-000000000002"

# Комбінований пошук (Admin бачить всі бронювання)
curl -X GET "https://localhost:5001/api/bookings?from=2024-02-01T00:00:00Z&to=2024-02-28T23:59:59Z&roomId=00000000-0000-0000-0000-000000000001&status=Confirmed" \
  -H "X-UserId: 00000000-0000-0000-0000-000000000001" \
  -H "X-Role: Admin"
```

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

## Приклади помилок

### Конфлікт бронювань (409 Conflict)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "Booking conflict",
  "status": 409,
  "detail": "Booking conflict detected. Another confirmed booking exists for this room and time slot.",
  "instance": "/api/bookings/..."
}
```

### Невалідний перехід стану (400 Bad Request)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Invalid status transition",
  "status": 400,
  "detail": "Cannot transition from Confirmed to Submitted",
  "instance": "/api/bookings/.../submit"
}
```

### Немає прав доступу (403 Forbidden)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Access denied",
  "status": 403,
  "detail": "You do not have permission to perform this action",
  "instance": "/api/rooms"
}
```
