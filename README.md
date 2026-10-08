# GReSym Backend

Backend-часть дипломного проекта **GReSym (Game Recommendation System)** — рекомендательной системы видеоигр.

Система предназначена для формирования персонализированных рекомендаций видеоигр на основе их текстовых описаний и игровой истории пользователя. В основе рекомендательной части проекта лежит сравнение **embedding-векторов видеоигр**, полученных из их текстовых описаний. Backend отвечает за работу с пользователями, играми и пользовательскими оценками, хранение данных и предоставление API для остальных компонентов системы.

> Этот репозиторий содержит только backend-часть GReSym. В рамках проекта также планируются отдельные компоненты для ML-сервиса, frontend и мобильных приложений.

## Возможности

Backend предоставляет базовую инфраструктуру для работы рекомендательной системы:

- регистрация и авторизация пользователей;
- JWT-аутентификация;
- управление профилем пользователя;
- получение и управление информацией об играх;
- работа с тегами игр;
- пользовательские оценки игр;
- отзывы пользователей;
- хранение исходных данных об играх;
- работа с изображениями и скриншотами;
- административные операции;
- взаимодействие с базой данных через Entity Framework Core;
- миграции базы данных;
- парсинг данных Steam;
- отдельный UI для запуска и контроля процесса парсинга.

Рекомендательная логика и создание embedding-векторов являются отдельной частью общей архитектуры GReSym и не входят непосредственно в данный backend-репозиторий.

## Архитектура

Проект построен с разделением на несколько слоёв:

```text
GReSym.API
    │
    ▼
GReSym.Application
    │
    ▼
GReSym.Core
    ▲
    │
GReSym.Infrastructure
```

### GReSym.API

Web API на ASP.NET.

Содержит:

- Controllers;
- конфигурацию приложения;
- JWT-аутентификацию;
- регистрацию зависимостей;
- HTTP endpoints.

Основные контроллеры:

- `AuthController` — регистрация и авторизация;
- `UsersController` — работа с пользователями;
- `GamesController` — работа с играми;
- `AdminController` — административные операции.

### GReSym.Application

Слой прикладной логики.

Содержит:

- сервисы приложения;
- DTO;
- интерфейсы сервисов;
- JWT token service;
- mapping;
- настройки JWT.

Основные сервисы:

```text
AuthService
GamesService
JwtTokenService
ReviewsService
UsersService
```

### GReSym.Core

Ядро приложения и доменная модель.

Содержит:

- сущности;
- value objects;
- enum'ы;
- доменные исключения;
- интерфейсы репозиториев;
- общие вспомогательные классы.

Основные сущности:

```text
Game
Tag
GameTag
Screenshot
User
UserGameRate
Review
SteamSource
MetacriticSource
```

### GReSym.Infrastructure

Инфраструктурный слой.

Отвечает за:

- подключение к базе данных;
- Entity Framework Core;
- `DbContext`;
- конфигурацию сущностей;
- репозитории;
- Unit of Work;
- миграции.

Используется `ApplicationDbContext` и набор специализированных репозиториев для работы с основными сущностями.

### GReSym.Parser

Отдельный модуль для получения и загрузки данных.

Включает ETL-процесс:

```text
Steam
  │
  ▼
Extractor
  │
  ▼
Parser
  │
  ▼
Loader
  │
  ▼
Database
```

Parser предназначен прежде всего для первоначального наполнения и последующего обновления базы данных информацией об играх и пользовательских отзывах.

### GReSym.Parser.UI

Графический интерфейс для управления парсером.

Позволяет запускать различные операции парсинга и отслеживать их состояние.

## Структура репозитория

```text
.
├── GReSym.API/              # ASP.NET Web API
├── GReSym.Application/      # Прикладная логика и DTO
├── GReSym.Core/             # Доменная модель и интерфейсы
├── GReSym.Infrastructure/   # БД, EF Core и репозитории
├── GReSym.Parser/           # Парсер и ETL
├── GReSym.Parser.UI/        # UI для управления парсером
├── GReSym.sln               # Solution
├── config.example.json      # Пример локальной конфигурации (в git)
├── config.json              # Локальная конфигурация с секретами (не в git, создаётся из примера)
└── manage.sh                # Скрипт управления проектом

Каждый из проектов GReSym.API, GReSym.Infrastructure и GReSym.Parser.UI содержит
appsettings.example.json (в git) и генерируемый из него appsettings.json (не в git).
```

## Требования

Для работы backend необходимы:

- Linux/Unix-подобная система;
- .NET SDK;
- MariaDB/MySQL;
- `jq`;
- `openssl`.

Проверить наличие необходимых утилит можно командами:

```bash
dotnet --version
jq --version
openssl version
```

Также для работы миграций Entity Framework Core должен быть доступен `dotnet ef`
(`manage.sh` находит его и в `~/.dotnet/tools`, даже если этот каталог не добавлен в `PATH`).

При необходимости:

```bash
dotnet tool install --global dotnet-ef
```

## Настройка

Конфигурация построена на файлах-примерах, которые хранятся в git и являются источником истины:

| Пример (в git) | Генерируемый файл (не в git) |
|---|---|
| `config.example.json` | `config.json` — учётные данные БД и JWT |
| `GReSym.API/appsettings.example.json` | `GReSym.API/appsettings.json` |
| `GReSym.Infrastructure/appsettings.example.json` | `GReSym.Infrastructure/appsettings.json` |
| `GReSym.Parser.UI/appsettings.example.json` | `GReSym.Parser.UI/appsettings.json` |

Перед первым запуском необходимо создать и заполнить `config.json`. Если файл отсутствует,
`./manage.sh setup` скопирует его из `config.example.json` и завершится, чтобы его можно было заполнить.

Содержимое `config.example.json`:

```json
{
  "server": "localhost",
  "port": 3306,
  "database": "GamesRecommend",
  "user": "xxx",
  "password": "xxx",
  "jwt_expire_minutes": 60,
  "jwt_key": "xxx"
}
```

Параметры:

| Параметр | Описание |
|---|---|
| `server` | Адрес сервера базы данных |
| `port` | Порт базы данных |
| `database` | Название базы данных |
| `user` | Пользователь базы данных |
| `password` | Пароль пользователя базы данных |
| `jwt_expire_minutes` | Время жизни JWT-токена в минутах |
| `jwt_key` | Секретный ключ для подписи JWT |

Значения `""`, `null`, `xxx` и `###` считаются незаполненными: `setup` завершится с ошибкой и перечислит,
какие параметры нужно указать (кроме `jwt_key` и `jwt_expire_minutes`).

`jwt_key` должен иметь длину не менее **32 байт**. Если ключ отсутствует, является заглушкой или слишком короткий,
`manage.sh` сгенерирует новый ключ с помощью `openssl` и сохранит его в `config.json` (ключ в консоль не выводится).
Если `jwt_expire_minutes` не задан, используется 60.

Если пользователь или пароль БД содержат символы `;`, `"`, `'` или `=`, значение автоматически заключается в кавычки
в строке подключения.

### Важно

`config.json` и `appsettings.json` содержат учётные данные базы данных и секретный JWT-ключ. Они перечислены в `.gitignore`
и не должны попадать в репозиторий. В git хранятся только `*.example.json` с заглушками.

Несекретные настройки (логирование, `Jwt.Issuer`, `Jwt.Audience` и т.п.) меняются в соответствующем
`appsettings.example.json`, после чего нужно повторно выполнить `./manage.sh setup`. Ручные правки в `appsettings.json`
перезаписываются при каждом `setup`.

Не используйте реальные production credentials в примерах или тестовой конфигурации.

## Первоначальная настройка

После создания `config.json` необходимо выполнить:

```bash
chmod +x manage.sh
./manage.sh setup
```

Команда `setup`:

1. создаёт `config.json` из `config.example.json`, если его нет (и завершается для заполнения);
2. проверяет, что все параметры БД заполнены;
3. проверяет JWT-ключ и при необходимости генерирует новый;
4. формирует строку подключения к БД;
5. для `GReSym.API`, `GReSym.Infrastructure` и `GReSym.Parser.UI` заново создаёт `appsettings.json` из
   `appsettings.example.json`, подставляя строку подключения (и JWT-ключ/время жизни для API). Права на файл: `600`.

## База данных

Backend использует Entity Framework Core для работы с базой данных.

После настройки подключения необходимо применить существующие миграции:

```bash
./manage.sh database update
```

Команда выполняет:

```bash
dotnet ef database update
```

в проекте `GReSym.Infrastructure`.

### Создание миграции

Для создания новой миграции:

```bash
./manage.sh database migrate
```

Название миграции можно передать вручную:

```bash
./manage.sh database migrate AddNewField
```

Если название не указано, будет автоматически создано имя вида:

```text
migration_YYYYMMDDTHHMM
```

После создания миграции её необходимо применить:

```bash
./manage.sh database update
```

## Запуск

### API

Для запуска ASP.NET API:

```bash
./manage.sh run api
```

Команда проверяет, что `GReSym.API/appsettings.json` сгенерирован (иначе просит выполнить `setup`), переходит в `GReSym.API` и выполняет:

```bash
dotnet run
```

### Parser UI

Для запуска графического интерфейса парсера:

```bash
./manage.sh run parser
```

## manage.sh

`manage.sh` — основной вспомогательный скрипт репозитория для настройки и управления backend-компонентами.

Доступные команды:

```text
./manage.sh setup
./manage.sh clean credentials
./manage.sh clean build
./manage.sh run api
./manage.sh run parser
./manage.sh database migrate
./manage.sh database migrate <name>
./manage.sh database update
```

### Настройка

```bash
./manage.sh setup
```

Генерирует `appsettings.json` всех проектов из примеров и `config.json`. Подробнее в разделе «Первоначальная настройка».

Скрипт можно запускать из любого каталога. Файл `config.json` нужен только для `setup`.
Команды `run` и `database` проверяют, что соответствующий `appsettings.json` настроен.

### Очистка credentials

```bash
./manage.sh clean credentials
```

Восстанавливает каждый `appsettings.json` из соответствующего `appsettings.example.json`, то есть заменяет секреты заглушками `###`.

При этом `config.json` не очищается — это необходимо сделать самостоятельно.

### Очистка сборки

```bash
./manage.sh clean build
```

Выполняет `dotnet clean` для проектов верхнего уровня репозитория.

### Запуск API

```bash
./manage.sh run api
```

### Запуск Parser UI

```bash
./manage.sh run parser
```

### Миграции

Создать миграцию:

```bash
./manage.sh database migrate MyMigration
```

Применить миграции:

```bash
./manage.sh database update
```

## Типичный сценарий запуска

После клонирования репозитория последовательность действий выглядит следующим образом:

```bash
git clone <repository-url>
cd <repository-directory>

chmod +x manage.sh

./manage.sh setup
./manage.sh database update
./manage.sh run api
```

Первый запуск `setup` создаст `config.json` из `config.example.json` и завершится. После заполнения параметров базы данных необходимо повторно выполнить:

```bash
./manage.sh setup
```

## Аутентификация

Для авторизации API использует JWT.

Секретный ключ и время жизни токена задаются в `config.json`:

```json
{
  "jwt_expire_minutes": 60,
  "jwt_key": "..."
}
```

Во время `setup` эти значения переносятся в конфигурацию `GReSym.API`.

JWT используется API для идентификации авторизованных пользователей и разграничения доступа к защищённым endpoints.

## Работа с данными игр

В базе данных хранятся данные, необходимые для последующей работы рекомендательной системы.

В частности, модель содержит информацию о:

- играх;
- жанрах и тегах;
- системных требованиях;
- ценах;
- скриншотах;
- данных Steam;
- данных Metacritic;
- пользовательских оценках;
- пользовательских отзывах.

Эти данные формируют основу для последующего ML-пайплайна GReSym.

## Технологии

Основной стек backend:

- **C# / .NET**
- **ASP.NET Core**
- **Entity Framework Core**
- **MySQL / MariaDB**
- **JWT**
- **LINQ**
- **Bash**
- **jq**
- **OpenSSL**

Архитектура приложения использует разделение на API, Application, Core и Infrastructure слои, что позволяет отделить HTTP-часть приложения, прикладную логику, доменную модель и инфраструктурные зависимости.

Backend является центральным серверным компонентом системы: он предоставляет API, управляет пользователями и данными, а также обеспечивает взаимодействие остальных компонентов с основной базой данных.
