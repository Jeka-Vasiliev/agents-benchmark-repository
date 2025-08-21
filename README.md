# LibraryLending - Library Management API

Минимально работающий API для библиотеки с базовыми юзкейсами выдачи книг, построенный на ASP.NET Core (.NET 9) по принципам Clean Architecture.

## Структура проекта

```
LibraryLending/
├── src/
│   ├── LibraryLending.Domain/           # Доменные сущности, value objects, интерфейсы
│   │   ├── Entities/                    # Book, Patron, Loan
│   │   ├── ValueObjects/                # Email, Isbn
│   │   ├── Exceptions/                  # Доменные исключения
│   │   └── Repositories/                # Интерфейсы репозиториев
│   ├── LibraryLending.Application/      # Use cases, DTO, валидации
│   │   ├── DTOs/                        # Data Transfer Objects
│   │   └── UseCases/                    # CQRS команды и запросы через MediatR
│   ├── LibraryLending.Infrastructure/   # EF Core, репозитории, миграции
│   │   ├── Data/                        # DbContext
│   │   └── Repositories/                # Реализации репозиториев
│   └── LibraryLending.WebApi/           # Контроллеры, middleware, DI
│       ├── Controllers/                 # API контроллеры
│       └── Middleware/                  # Обработка исключений
├── tests/
│   └── LibraryLending.Application.Tests/ # Unit тесты
└── Directory.Packages.props             # Центральное управление пакетами
```

## Ключевые компоненты

### Доменные сущности
- **Book**: Книга с ISBN, названием, автором, общим и доступным количеством копий
- **Patron**: Читатель с именем, email (уникальный) и статусом активности
- **Loan**: Выдача книги с датами выдачи, возврата и сроком

### Value Objects
- **Email**: Валидация email адресов
- **Isbn**: Валидация ISBN номеров

### Use Cases
- **RegisterPatron**: Регистрация нового читателя
- **AddBook**: Добавление книги в библиотеку
- **LoanBook**: Выдача книги читателю
- **ReturnBook**: Возврат книги
- **GetBooks**: Получение списка книг с пагинацией и поиском
- **GetPatron**: Получение информации о читателе с активными выдачами

## API Endpoints

### Читатели
- `POST /api/patrons` - Регистрация читателя
- `GET /api/patrons/{id}` - Получение информации о читателе

### Книги
- `POST /api/books` - Добавление книги
- `GET /api/books` - Получение списка книг с пагинацией и поиском

### Выдачи
- `POST /api/loans` - Выдача книги
- `POST /api/loans/{loanId}/return` - Возврат книги

## Запуск проекта

### Предварительные требования
- .NET 9 SDK
- Visual Studio 2022 или VS Code

### Команды для запуска

1. **Клонирование и переход в директорию:**
```bash
cd /path/to/agents-benchmark-repository
```

2. **Восстановление пакетов:**
```bash
dotnet restore
```

3. **Сборка проекта:**
```bash
dotnet build
```

4. **Запуск тестов:**
```bash
dotnet test
```

5. **Запуск API:**
```bash
dotnet run --project src/LibraryLending.WebApi
```

API будет доступен по адресу: `http://localhost:5000`
Swagger UI: `http://localhost:5000/swagger`

## Примеры запросов

### Регистрация читателя
```bash
curl -X POST "http://localhost:5000/api/patrons" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Jane Smith",
    "email": "jane.smith@example.com"
  }'
```

### Добавление книги
```bash
curl -X POST "http://localhost:5000/api/books" \
  -H "Content-Type: application/json" \
  -d '{
    "isbn": "9780134685991",
    "title": "Effective Java",
    "author": "Joshua Bloch",
    "totalCopies": 3
  }'
```

### Получение списка книг
```bash
curl "http://localhost:5000/api/books?page=1&pageSize=10&query=java"
```

### Выдача книги
```bash
curl -X POST "http://localhost:5000/api/loans" \
  -H "Content-Type: application/json" \
  -d '{
    "bookId": "book-guid-here",
    "patronId": "patron-guid-here"
  }'
```

### Возврат книги
```bash
curl -X POST "http://localhost:5000/api/loans/{loan-id}/return"
```

## Технические особенности

- **Clean Architecture**: Четкое разделение слоев без нарушения зависимостей
- **CQRS**: Разделение команд и запросов через MediatR
- **Валидация**: FluentValidation для проверки входных данных
- **Обработка ошибок**: Глобальный middleware с ProblemDetails (RFC 7807)
- **Конкурентность**: Оптимистическая блокировка для Book через RowVersion
- **In-Memory Database**: Для простоты развертывания в v1
- **Seed данные**: Автоматическое создание тестовых данных в Dev режиме

## Коды ошибок

- `400 Bad Request` - Ошибки валидации
- `409 Conflict` - Доменные ошибки:
  - BookUnavailable - Книга недоступна для выдачи
  - LoanAlreadyReturned - Книга уже возвращена
  - PatronEmailAlreadyExists - Email уже используется

## Что дальше (roadmap для сравнения агентов)

### v1.1: Система бронирования
- ReserveBook + очередь брони (FIFO)
- Автосписание при возврате
- Ошибка 409 если книга доступна и резерв не нужен

### v1.2: Ограничение активных выдач
- Запрет выдачи при превышении лимита активных выдач (например, 5)

### v1.3: Продление выдач
- ExtendLoan (1 раз, +7 дней)
- Запрет если есть активная бронь другого читателя

### v1.4: Расширенная пагинация
- Сортировка по полям
- Фильтры по автору/ISBN

### v1.5: Доменные события
- BookLoanedDomainEvent и обработчики
- Система уведомлений (заглушка в v1)

### v1.6: Идемпотентность
- LoanBook по IdempotencyKey заголовку

### v1.7: Аналитика
- Отчет "самые востребованные книги" за период

## Критерии оценки архитектуры

- ✅ Чистота слоев (нет утечек EF в Application)
- ✅ Корректность доменных инвариантов
- ✅ Обработка конфликтов и ошибок
- ✅ Тестируемость
- ✅ Качество API документации