#!/bin/bash
#
# Источник истины для структуры конфигурации — *.example.json (хранятся в git).
#   config.example.json                → config.json            (локальные секреты, не в git)
#   <проект>/appsettings.example.json  → <проект>/appsettings.json (генерируется в setup, не в git)

set -o pipefail

cd "$(dirname "$(readlink -f "$0")")" || exit 1

CONFIG_FILE="config.json"
CONFIG_EXAMPLE="config.example.json"
PROJECTS=("GReSym.API" "GReSym.Infrastructure" "GReSym.Parser.UI")
PLACEHOLDERS=("" "null" "xxx" "###")

command -v jq >/dev/null || { echo "jq не установлен"; exit 1; }
command -v dotnet >/dev/null || { echo "dotnet не установлен"; exit 1; }
command -v openssl >/dev/null || { echo "openssl не установлен"; exit 1; }

# Функция для получения значения из JSON по ключу
get_json_value() {
    local json_file="$1"
    local key="$2"
    jq -r --arg k "$key" '.[$k] // empty' "$json_file" 2>/dev/null
}

# Функция для обновления JSON‑файла (установка значения по ключу)
update_json_value() {
    local json_file="$1"
    local key="$2"
    local value="$3"
    local tmp
    tmp=$(mktemp)
    jq --arg k "$key" --arg v "$value" '.[$k] = $v' "$json_file" > "$tmp" && mv "$tmp" "$json_file"
}

is_placeholder() {
    local value="$1"
    for p in "${PLACEHOLDERS[@]}"; do
        [[ "$value" == "$p" ]] && return 0
    done
    return 1
}

# Создаёт config.json из примера, если его нет
check_config() {
    if [[ ! -f "$CONFIG_FILE" ]]; then
        if [[ ! -f "$CONFIG_EXAMPLE" ]]; then
            echo "Ошибка: не найден ни $CONFIG_FILE, ни $CONFIG_EXAMPLE"
            exit 1
        fi
        cp "$CONFIG_EXAMPLE" "$CONFIG_FILE"
        echo "Файл $CONFIG_FILE создан из $CONFIG_EXAMPLE, необходимо его настроить, после запустите скрипт снова."
        exit 1
    fi

    jq empty "$CONFIG_FILE" 2>/dev/null || { echo "Ошибка: $CONFIG_FILE не является корректным JSON"; exit 1; }
}

# Читает и проверяет параметры из config.json
load_config() {
    SERVER=$(get_json_value "$CONFIG_FILE" "server")
    PORT=$(get_json_value "$CONFIG_FILE" "port")
    DATABASE=$(get_json_value "$CONFIG_FILE" "database")
    DB_USER=$(get_json_value "$CONFIG_FILE" "user")
    DB_PASSWORD=$(get_json_value "$CONFIG_FILE" "password")
    JWT_KEY=$(get_json_value "$CONFIG_FILE" "jwt_key")
    JWT_EXPIRE=$(get_json_value "$CONFIG_FILE" "jwt_expire_minutes")
    RABBITMQ_USER=$(get_json_value "$CONFIG_FILE" "rabbitmq_user")
    RABBITMQ_PASSWORD=$(get_json_value "$CONFIG_FILE" "rabbitmq_password")

    local missing=()
    is_placeholder "$SERVER" && missing+=("server")
    is_placeholder "$PORT" && missing+=("port")
    is_placeholder "$DATABASE" && missing+=("database")
    is_placeholder "$DB_USER" && missing+=("user")
    is_placeholder "$DB_PASSWORD" && missing+=("password")
    is_placeholder "$RABBITMQ_USER" && missing+=("rabbitmq_user")
    is_placeholder "$RABBITMQ_PASSWORD" && missing+=("rabbitmq_password")

    if (( ${#missing[@]} > 0 )); then
        echo "Ошибка: в $CONFIG_FILE не заполнены параметры: ${missing[*]}"
        exit 1
    fi

    if ! [[ "$JWT_EXPIRE" =~ ^[0-9]+$ ]]; then
        echo "jwt_expire_minutes не задан или некорректен, используем 60"
        JWT_EXPIRE=60
    fi

    DB_CONNECTION="Server=$SERVER;Port=$PORT;Database=$DATABASE;User=$(quote_conn_value "$DB_USER");Password=$(quote_conn_value "$DB_PASSWORD");"
}

# Значения с ; " ' = или пробелами по краям оборачиваются в кавычки (внутренние " удваиваются)
quote_conn_value() {
    local value="$1"
    if [[ "$value" =~ [\;\"\'=] || "$value" != "${value#[[:space:]]}" || "$value" != "${value%[[:space:]]}" ]]; then
        printf '"%s"' "${value//\"/\"\"}"
    else
        printf '%s' "$value"
    fi
}

# Валидация и генерация JWT‑ключа
validate_and_generate_jwt_key() {
    if is_placeholder "$JWT_KEY" || (( ${#JWT_KEY} < 32 )); then
        echo "JWT‑ключ отсутствует или слишком короткий (меньше 32 байт)."
        echo "Генерируем новый JWT‑ключ..."
        # 64 символа hex = 32 байта
        JWT_KEY=$(openssl rand -hex 32)
        update_json_value "$CONFIG_FILE" "jwt_key" "$JWT_KEY"
        echo "Новый JWT‑ключ сгенерирован и сохранён в $CONFIG_FILE"
    else
        echo "Используем существующий JWT‑ключ (длина: ${#JWT_KEY} байт)"
    fi
}

# Генерирует <проект>/appsettings.json из <проект>/appsettings.example.json
# Строка подключения, JWT и RabbitMQ подставляются только если соответствующие секции есть в примере
generate_appsettings() {
    local project_path="$1"
    local example="$project_path/appsettings.example.json"
    local target="$project_path/appsettings.json"
    local tmp

    if [[ ! -f "$example" ]]; then
        echo "Файл $example не найден, пропускаем $project_path"
        return 1
    fi

    echo "Генерируем $target из $example"
    tmp=$(mktemp)
    jq --arg conn "$DB_CONNECTION" \
       --arg key "$JWT_KEY" \
       --argjson expire "$JWT_EXPIRE" \
       --arg mq_user "$RABBITMQ_USER" \
       --arg mq_password "$RABBITMQ_PASSWORD" '
        if (.ConnectionStrings? | type) == "object" then
            .ConnectionStrings.DefaultConnection = $conn
        else . end
        |
        if (.Jwt? | type) == "object" then
            .Jwt.Key = $key | .Jwt.ExpireMinutes = $expire
        else . end
        |
        if (.RabbitMq? | type) == "object" then
            .RabbitMq.User = $mq_user | .RabbitMq.Password = $mq_password
        else . end
    ' "$example" > "$tmp" && mv "$tmp" "$target" && chmod 600 "$target"
}

# Возвращает appsettings.json к состоянию примера (без секретов)
clean_passwords() {
    local project_path="$1"
    local example="$project_path/appsettings.example.json"

    if [[ -f "$example" ]]; then
        echo "Восстанавливаем $project_path/appsettings.json из примера"
        cp "$example" "$project_path/appsettings.json"
    else
        echo "Файл $example не найден, пропускаем $project_path"
    fi
}

# Проверяет, что appsettings.json сгенерирован (setup был выполнен)
require_appsettings() {
    local file="$1/appsettings.json"
    if [[ ! -f "$file" ]] || grep -q '###' "$file"; then
        echo "Ошибка: $file отсутствует или не настроен. Выполните ./manage.sh setup"
        exit 1
    fi
}

clean_build() {
    for d in */ ; do
        echo "Очищаем $d"
        (cd "$d" && dotnet clean)
    done
    echo "Очистка закончена"
}

run_api() {
    require_appsettings "GReSym.API"
    cd GReSym.API || exit 1
    dotnet run
}

run_parser() {
    require_appsettings "GReSym.Parser.UI"
    cd GReSym.Parser.UI || exit 1
    dotnet run
}

# dotnet ef может отсутствовать в PATH (глобальные инструменты лежат в ~/.dotnet/tools)
dotnet_ef() {
    if dotnet ef --version >/dev/null 2>&1; then
        dotnet ef "$@"
    elif [[ -x "$HOME/.dotnet/tools/dotnet-ef" ]]; then
        "$HOME/.dotnet/tools/dotnet-ef" "$@"
    else
        echo "dotnet-ef не найден. Установите: dotnet tool install --global dotnet-ef"
        exit 1
    fi
}

database_migrate() {
    local migration_name="$1"
    if [[ -z "$migration_name" ]]; then
        migration_name="migration_$(date +%Y%m%dT%H%M)"
    fi
    require_appsettings "GReSym.Infrastructure"
    cd GReSym.Infrastructure || exit 1
    dotnet_ef migrations add "$migration_name" && echo "Миграция $migration_name создана!"
}

database_update() {
    require_appsettings "GReSym.Infrastructure"
    cd GReSym.Infrastructure || exit 1
    dotnet_ef database update
}

DUMPS_DIR="dumps"

# Находит клиент MariaDB (имена mariadb-* или старые mysql*)
find_db_tool() {
    local name
    for name in "$@"; do
        command -v "$name" >/dev/null && { echo "$name"; return 0; }
    done
    echo "Ошибка: не найден ни один из: $*" >&2
    return 1
}

# Временный option-файл с учётными данными из config.json (пароль не попадает в командную строку)
# Передаётся через --defaults-file, чтобы ~/.my.cnf и /etc/my.cnf не переопределяли пользователя/хост
# Удаляется при выходе из скрипта
create_db_defaults_file() {
    DB_DEFAULTS_FILE=$(mktemp)
    chmod 600 "$DB_DEFAULTS_FILE"
    trap 'rm -f "$DB_DEFAULTS_FILE"' EXIT
    local password="${DB_PASSWORD//\\/\\\\}"
    password="${password//\"/\\\"}"
    {
        echo "[client]"
        echo "host=$SERVER"
        echo "port=$PORT"
        echo "user=$DB_USER"
        echo "password=\"$password\""
        echo "default-character-set=utf8mb4"
    } > "$DB_DEFAULTS_FILE"
}

# Команда (рас)паковки по расширению файла: .sql, .sql.gz, .sql.zst
compression_for() {
    case "$1" in
        *.sql.zst) command -v zstd >/dev/null || { echo "Ошибка: zstd не установлен" >&2; return 1; }; echo "zstd" ;;
        *.sql.gz)  echo "gzip" ;;
        *.sql)     echo "cat" ;;
        *) echo "Ошибка: неизвестное расширение $1 (ожидается .sql, .sql.gz или .sql.zst)" >&2; return 1 ;;
    esac
}

database_dump() {
    local target="$1"
    local dump_tool compressor
    check_config
    load_config
    dump_tool=$(find_db_tool mariadb-dump mysqldump) || exit 1

    if [[ -z "$target" ]]; then
        local ext="sql.gz"
        command -v zstd >/dev/null && ext="sql.zst"
        mkdir -p "$DUMPS_DIR"
        target="$DUMPS_DIR/${DATABASE}-$(date +%Y%m%dT%H%M).$ext"
    fi
    compressor=$(compression_for "$target") || exit 1
    if [[ -e "$target" ]]; then
        echo "Ошибка: $target уже существует"
        exit 1
    fi

    create_db_defaults_file
    echo "Создаём дамп $DATABASE ($SERVER:$PORT) → $target"
    # Без --databases: в дампе нет CREATE DATABASE/USE, его можно восстановить в базу с любым именем
    local compress_cmd=("$compressor")
    case "$compressor" in
        zstd) compress_cmd=(zstd -q -T0 -19) ;;
        gzip) compress_cmd=(gzip -9) ;;
    esac
    if "$dump_tool" --defaults-file="$DB_DEFAULTS_FILE" \
            --single-transaction --quick --triggers \
            "$DATABASE" | "${compress_cmd[@]}" > "$target"; then
        echo "Готово: $target ($(du -h "$target" | cut -f1))"
    else
        rm -f "$target"
        echo "Ошибка при создании дампа"
        exit 1
    fi
}

database_restore() {
    local source="$1"
    local assume_yes="$2"
    local client decompressor table_count
    if [[ -z "$source" ]]; then
        echo "Использование: ./manage.sh database restore <файл.sql[.gz|.zst]> [--yes]"
        exit 1
    fi
    [[ -f "$source" ]] || { echo "Ошибка: файл $source не найден"; exit 1; }
    decompressor=$(compression_for "$source") || exit 1
    check_config
    load_config
    client=$(find_db_tool mariadb mysql) || exit 1
    create_db_defaults_file

    if ! "$client" --defaults-file="$DB_DEFAULTS_FILE" -e \
            "CREATE DATABASE IF NOT EXISTS \`$DATABASE\` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;"; then
        echo "Ошибка: не удалось подключиться или создать базу $DATABASE (нужны права CREATE у пользователя $DB_USER)"
        exit 1
    fi

    table_count=$("$client" --defaults-file="$DB_DEFAULTS_FILE" -N -B -e \
        "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '$DATABASE';") || exit 1
    if (( table_count > 0 )) && [[ "$assume_yes" != "--yes" ]]; then
        echo "В базе $DATABASE уже есть таблицы ($table_count). Таблицы из дампа будут удалены и созданы заново."
        read -r -p "Продолжить? [y/N] " answer
        [[ "$answer" =~ ^[YyДд]$ ]] || { echo "Отменено"; exit 1; }
    fi

    local decompress_cmd=(cat "$source")
    case "$decompressor" in
        zstd) decompress_cmd=(zstd -q -dc "$source") ;;
        gzip) decompress_cmd=(gzip -dc "$source") ;;
    esac
    echo "Восстанавливаем $source → $DATABASE ($SERVER:$PORT)"
    if "${decompress_cmd[@]}" | "$client" --defaults-file="$DB_DEFAULTS_FILE" "$DATABASE"; then
        echo "Готово. Новые миграции (если есть) применяются через ./manage.sh database update"
    else
        echo "Ошибка при восстановлении"
        exit 1
    fi
}

# Основная логика скрипта
case "${1:-}" in
    "setup")
        check_config
        load_config
        echo "Настраиваем проекты с параметрами из $CONFIG_FILE"
        validate_and_generate_jwt_key
        for project in "${PROJECTS[@]}"; do
            generate_appsettings "$project"
        done
        ;;
    "clean")
        case "${2:-}" in
            "credentials")
                echo "Очищаем пароли во всех проектах"
                for project in "${PROJECTS[@]}"; do
                    clean_passwords "$project"
                done
                ;;
            "build")
                clean_build
                ;;
            *)
                echo "Использование: ./manage.sh clean {credentials|build}"
                exit 1
                ;;
        esac
        ;;
    "run")
        case "${2:-}" in
            "api")
                run_api
                ;;
            "parser")
                run_parser
                ;;
            *)
                echo "Использование: ./manage.sh run {api|parser}"
                exit 1
                ;;
        esac
        ;;
    "database")
        case "${2:-}" in
            "migrate")
                database_migrate "${3:-}"
                ;;
            "update")
                database_update
                ;;
            "dump")
                database_dump "${3:-}"
                ;;
            "restore")
                database_restore "${3:-}" "${4:-}"
                ;;
            *)
                echo "Использование: ./manage.sh database {migrate|update|dump|restore}"
                exit 1
                ;;
        esac
        ;;
    *)
        echo "Использование: $0 {setup|clean|run|database}"
        echo "  setup             — создать config.json из примера / сгенерировать appsettings.json из appsettings.example.json"
        echo "  clean credentials — восстановить appsettings.json из примеров (config.json не трогается)"
        echo "  clean build       — очистить файлы сборки"
        echo "  run api           — запустить API"
        echo "  run parser        — запустить Parser UI"
        echo "  database migrate  — создать миграцию, опционально можно задать название миграции"
        echo "  database update   — применить миграции"
        echo "  database dump     — дамп БД (по умолчанию в dumps/, .sql.zst), опционально можно задать путь (.sql/.sql.gz/.sql.zst)"
        echo "  database restore  — восстановить БД из дампа: <файл> [--yes]"
        exit 1
        ;;
esac
