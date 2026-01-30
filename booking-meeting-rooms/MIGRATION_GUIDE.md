# Як застосувати міграції

## Способи застосування міграцій

### 1. Через EF Core CLI (рекомендовано)

```bash
dotnet ef database update
```

Ця команда застосує всі не застосовані міграції до бази даних.

### 2. Автоматично при старті додатку (Development)

Міграції автоматично застосовуються при запуску додатку в режимі Development (вже налаштовано в `Program.cs`):

```bash
dotnet run
```

Або через Visual Studio / Rider - просто запустіть проект.

### 3. Через Visual Studio Package Manager Console

Якщо використовуєте Visual Studio:

```powershell
Update-Database
```

### 4. Створення нової міграції

Якщо потрібно створити нову міграцію після змін в моделі:

```bash
dotnet ef migrations add НазваМіграції
```

### 5. Відкат міграції

Якщо потрібно відкатити останню міграцію:

```bash
dotnet ef database update НазваПопередньоїМіграції
```

Або видалити останню міграцію:

```bash
dotnet ef migrations remove
```

## Перевірка статусу міграцій

Перевірити які міграції застосовані:

```bash
dotnet ef migrations list
```

## Важливо

- Перед застосуванням міграцій переконайтеся, що PostgreSQL запущений
- Перевірте рядок підключення в `appsettings.json` або `appsettings.Development.json`
- Seed дані (прикладові кімнати) будуть додані автоматично при застосуванні міграції `InitialCreate`
