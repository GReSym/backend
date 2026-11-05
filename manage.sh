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
command -v openssl >/dev/null || { echo "openssl не установлен"; exit 1; }

# dotnet нужен только для run/clean build/database migrate|update: setup и dump/restore работают без него (Docker-окружение)
require_dotnet() {
    command -v dotnet >/dev/null || { echo "dotnet не установлен"; exit 1; }
}

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
    # Необязательный: ключ API Openinary (создаётся в его дашборде), без него миграция картинок недоступна
    OPENINARY_API_KEY=$(get_json_value "$CONFIG_FILE" "openinary_api_key")
    if is_placeholder "$OPENINARY_API_KEY"; then
        echo "openinary_api_key не задан: перенос картинок в Openinary работать не будет"
        OPENINARY_API_KEY="###"
    fi

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
       --arg mq_password "$RABBITMQ_PASSWORD" \
       --arg openinary_key "$OPENINARY_API_KEY" '
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
        |
        if (.Openinary? | type) == "object" then
            .Openinary.ApiKey = $openinary_key
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
    require_dotnet
    for d in */ ; do
        echo "Очищаем $d"
        (cd "$d" && dotnet clean)
    done
    echo "Очистка закончена"
}

run_api() {
    require_dotnet
    require_appsettings "GReSym.API"
    cd GReSym.API || exit 1
    dotnet run
}

run_parser() {
    require_dotnet
    require_appsettings "GReSym.Parser.UI"
    cd GReSym.Parser.UI || exit 1
    dotnet run
}

# dotnet ef может отсутствовать в PATH (глобальные инструменты лежат в ~/.dotnet/tools)
dotnet_ef() {
    require_dotnet
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

DEPLOY_COMPOSE="../deploy/compose.yaml"

# Находит клиент MariaDB (имена mariadb-* или старые mysql*)
find_db_tool() {
    local name
    for name in "$@"; do
        command -v "$name" >/dev/null && { echo "$name"; return 0; }
    done
    return 1
}

# Команда клиента в DB_CMD: на хосте (учётные данные из config.json) или, если клиента на хосте нет,
# внутри контейнера mariadb из ../deploy (как root, пароль берётся из окружения контейнера и не попадает в командную строку)
# $1 — имя инструмента в контейнере (mariadb | mariadb-dump), дальше — имена на хосте
resolve_db_cmd() {
    local container_tool="$1"
    shift
    local host_tool
    if host_tool=$(find_db_tool "$@"); then
        create_db_defaults_file
        DB_CMD=("$host_tool" --defaults-file="$DB_DEFAULTS_FILE")
    elif [[ -f "$DEPLOY_COMPOSE" ]] && command -v docker >/dev/null; then
        echo "Клиент MariaDB на хосте не найден, используем контейнер mariadb ($DEPLOY_COMPOSE)" >&2
        DB_CMD=(docker compose -f "$DEPLOY_COMPOSE" exec -T mariadb
                sh -c 'tool="$1"; shift; exec "$tool" -uroot -p"$MARIADB_ROOT_PASSWORD" --default-character-set=utf8mb4 "$@"'
                sh "$container_tool")
    else
        echo "Ошибка: не найден ни один из: $* (и нет $DEPLOY_COMPOSE)" >&2
        return 1
    fi
}

# Временный option-файл с учётными данными из config.json (пароль не попадает в командную строку)
# Передаётся через --defaults-file, чтобы ~/.my.cnf и /etc/my.cnf не переопределяли пользователя/хост
# protocol=tcp: иначе host=localhost означает unix-сокет, а у MariaDB в Docker его на хосте нет
# Удаляется при выходе из скрипта
create_db_defaults_file() {
    [[ -n "${DB_DEFAULTS_FILE:-}" && -f "$DB_DEFAULTS_FILE" ]] && return 0  # второй resolve_db_cmd в той же команде
    DB_DEFAULTS_FILE=$(mktemp)
    chmod 600 "$DB_DEFAULTS_FILE"
    trap 'rm -f "$DB_DEFAULTS_FILE"' EXIT
    local password="${DB_PASSWORD//\\/\\\\}"
    password="${password//\"/\\\"}"
    {
        echo "[client]"
        echo "host=$SERVER"
        echo "port=$PORT"
        echo "protocol=tcp"
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
    local compressor
    check_config
    load_config

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

    resolve_db_cmd mariadb-dump mariadb-dump mysqldump || exit 1
    echo "Создаём дамп $DATABASE ($SERVER:$PORT) → $target"
    # Без --databases: в дампе нет CREATE DATABASE/USE, его можно восстановить в базу с любым именем
    local compress_cmd=("$compressor")
    case "$compressor" in
        zstd) compress_cmd=(zstd -q -T0 -19) ;;
        gzip) compress_cmd=(gzip -9) ;;
    esac
    if "${DB_CMD[@]}" \
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
    local decompressor table_count
    if [[ -z "$source" ]]; then
        echo "Использование: ./manage.sh database restore <файл.sql[.gz|.zst]> [--yes]"
        exit 1
    fi
    [[ -f "$source" ]] || { echo "Ошибка: файл $source не найден"; exit 1; }
    decompressor=$(compression_for "$source") || exit 1
    check_config
    load_config
    resolve_db_cmd mariadb mariadb mysql || exit 1

    if ! "${DB_CMD[@]}" -e \
            "CREATE DATABASE IF NOT EXISTS \`$DATABASE\` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;"; then
        echo "Ошибка: не удалось подключиться или создать базу $DATABASE (нужны права CREATE у пользователя $DB_USER)"
        exit 1
    fi

    table_count=$("${DB_CMD[@]}" -N -B -e \
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
    if "${decompress_cmd[@]}" | "${DB_CMD[@]}" "$DATABASE"; then
        echo "Готово. Новые миграции (если есть) применяются через ./manage.sh database update"
    else
        echo "Ошибка при восстановлении"
        exit 1
    fi
}

# ---------------- Частичный перенос игр (dump-games / restore-games) ----------------
# Только данные игр: games, steam_source, game_tags, screenshots, reviews (Steam) по диапазону games.id + вся tags.
# Пользовательские таблицы не трогаются. Подробности — docs/docker.md

GAME_TABLES=(games steam_source game_tags screenshots reviews tags)
GAMES_DUMP_HEADER="-- gresym-games-dump"

# --from N --to M → RANGE_FROM/RANGE_TO, остальные аргументы → RANGE_REST
parse_range_args() {
    RANGE_FROM=1
    RANGE_TO=2147483647
    RANGE_REST=()
    while (( $# )); do
        case "$1" in
            --from) RANGE_FROM="${2:-}"; shift; shift ;;
            --to)   RANGE_TO="${2:-}"; shift; shift ;;
            *)      RANGE_REST+=("$1"); shift ;;
        esac
    done
    if ! [[ "$RANGE_FROM" =~ ^[0-9]+$ && "$RANGE_TO" =~ ^[0-9]+$ ]] || (( RANGE_FROM > RANGE_TO )); then
        echo "Ошибка: --from/--to должны быть числами, from ≤ to (получено: ${RANGE_FROM:-?}..${RANGE_TO:-?})"
        return 1
    fi
}

database_dump_games() {
    parse_range_args "$@" || exit 1
    local target="${RANGE_REST[0]:-}"
    local compressor
    check_config
    load_config

    if [[ -z "$target" ]]; then
        local ext="sql.gz"
        command -v zstd >/dev/null && ext="sql.zst"
        mkdir -p "$DUMPS_DIR"
        target="$DUMPS_DIR/games-${RANGE_FROM}-${RANGE_TO}-$(date +%Y%m%dT%H%M).$ext"
    fi
    compressor=$(compression_for "$target") || exit 1
    if [[ -e "$target" ]]; then
        echo "Ошибка: $target уже существует"
        exit 1
    fi

    resolve_db_cmd mariadb mariadb mysql || exit 1
    local count
    count=$("${DB_CMD[@]}" -N -B -e \
        "SELECT COUNT(*) FROM \`$DATABASE\`.games WHERE id BETWEEN $RANGE_FROM AND $RANGE_TO;") || exit 1
    if (( count == 0 )); then
        echo "В $DATABASE нет игр с id в диапазоне $RANGE_FROM..$RANGE_TO"
        exit 1
    fi

    resolve_db_cmd mariadb-dump mariadb-dump mysqldump || exit 1
    local compress_cmd=("$compressor")
    case "$compressor" in
        zstd) compress_cmd=(zstd -q -T0 -19) ;;
        gzip) compress_cmd=(gzip -9) ;;
    esac
    # Без CREATE TABLE: при восстановлении используется схема целевой БД. --complete-insert: у каждого INSERT
    # явный список колонок, так что расхождение схем (не применена миграция) даёт ошибку, а не тихую потерю данных
    local opts=(--no-create-info --complete-insert --single-transaction --quick --skip-triggers)
    echo "Дамп игр $RANGE_FROM..$RANGE_TO ($count шт.) из $DATABASE ($SERVER:$PORT) → $target"
    if {
        echo "$GAMES_DUMP_HEADER from=$RANGE_FROM to=$RANGE_TO games=$count created=$(date -Iseconds)"
        "${DB_CMD[@]}" "${opts[@]}" --where="id BETWEEN $RANGE_FROM AND $RANGE_TO" "$DATABASE" games &&
        "${DB_CMD[@]}" "${opts[@]}" --where="game_id BETWEEN $RANGE_FROM AND $RANGE_TO" \
            "$DATABASE" steam_source game_tags screenshots reviews &&
        "${DB_CMD[@]}" "${opts[@]}" "$DATABASE" tags
    } | "${compress_cmd[@]}" > "$target"; then
        echo "Готово: $target ($(du -h "$target" | cut -f1))"
    else
        rm -f "$target"
        echo "Ошибка при создании дампа"
        exit 1
    fi
}

# Колонки таблицы в целевой БД через запятую, кроме перечисленных: table_columns <таблица> [исключить...]
table_columns() {
    local table="$1"
    shift
    local exclude=" $* " column result=()
    while read -r column; do
        [[ "$exclude" == *" $column "* ]] || result+=("\`$column\`")
    done < <("${DB_CMD[@]}" -N -B -e "SELECT column_name FROM information_schema.columns
        WHERE table_schema = '$DATABASE' AND table_name = '$table' ORDER BY ordinal_position;")
    local IFS=,
    echo "${result[*]}"
}

# "`a` = VALUES(`a`), ..." для ON DUPLICATE KEY UPDATE
values_assignments() {
    local column result=() columns
    IFS=, read -r -a columns <<< "$1"
    for column in "${columns[@]}"; do
        result+=("$column = VALUES($column)")
    done
    local IFS=,
    echo "${result[*]}"
}

# EXIT-trap restore-games: убрать промежуточную БД и временный option-файл
drop_games_import() {
    "${DB_CMD[@]}" -N -B -e "DROP DATABASE IF EXISTS \`$1\`;" >/dev/null 2>&1
    rm -f "${DB_DEFAULTS_FILE:-}"
}

database_restore_games() {
    local source="$1"
    local assume_yes="$2"
    local decompressor
    if [[ -z "$source" ]]; then
        echo "Использование: ./manage.sh database restore-games <файл.sql[.gz|.zst]> [--yes]"
        exit 1
    fi
    [[ -f "$source" ]] || { echo "Ошибка: файл $source не найден"; exit 1; }
    decompressor=$(compression_for "$source") || exit 1
    local decompress_cmd=(cat "$source")
    case "$decompressor" in
        zstd) decompress_cmd=(zstd -q -dc "$source") ;;
        gzip) decompress_cmd=(gzip -dc "$source") ;;
    esac
    local header
    header=$("${decompress_cmd[@]}" 2>/dev/null | head -n 1)
    if [[ "$header" != "$GAMES_DUMP_HEADER"* ]]; then
        echo "Ошибка: $source — не дамп dump-games (полный дамп восстанавливается через database restore)"
        exit 1
    fi
    echo "Дамп: ${header#-- }"

    check_config
    load_config
    resolve_db_cmd mariadb mariadb mysql || exit 1
    local main="\`$DATABASE\`"
    local stg_name="${DATABASE}_games_import"
    local stg="\`$stg_name\`"
    q() { "${DB_CMD[@]}" -N -B -e "$1"; }

    local has_games
    has_games=$(q "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '$DATABASE' AND table_name = 'games';") || exit 1
    if [[ "$has_games" != "1" ]]; then
        echo "Ошибка: в $DATABASE нет таблицы games (сначала полный restore или ./manage.sh database update)"
        exit 1
    fi

    # Промежуточная БД: таблицы той же структуры (CREATE TABLE LIKE копирует индексы, но не внешние ключи)
    trap "drop_games_import '$stg_name'" EXIT
    local create="DROP DATABASE IF EXISTS $stg; CREATE DATABASE $stg CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;"
    local table
    for table in "${GAME_TABLES[@]}"; do
        create+=" CREATE TABLE $stg.\`$table\` LIKE $main.\`$table\`;"
    done
    if ! q "$create"; then
        echo "Ошибка: не удалось создать промежуточную базу $stg_name (нужны права CREATE/DROP у пользователя $DB_USER)"
        exit 1
    fi
    echo "Загружаем дамп во временную базу $stg_name..."
    if ! "${decompress_cmd[@]}" | "${DB_CMD[@]}" "$stg_name"; then
        echo "Ошибка при загрузке дампа (схема целевой БД отличается? примените миграции: ./manage.sh database update)"
        exit 1
    fi

    # Конфликты: в этой БД под тем же id другая игра / чужой steam_app_id / чужие id скриншотов и отзывов
    local imported="SELECT id FROM $stg.games"
    local conflicts
    conflicts=$(q "
        SELECT CONCAT('game ', g.id, ': здесь «', m.title, '» (appid ', IFNULL(ms.steam_app_id, '-'), '), в дампе «', g.title,
                      '» (appid ', IFNULL(ss.steam_app_id, '-'), ')')
          FROM $stg.games g JOIN $main.games m ON m.id = g.id
          LEFT JOIN $stg.steam_source ss ON ss.game_id = g.id LEFT JOIN $main.steam_source ms ON ms.game_id = g.id
         WHERE m.title <> g.title AND NOT (ms.steam_app_id <=> ss.steam_app_id)
        UNION ALL
        SELECT CONCAT('steam_app_id ', ss.steam_app_id, ': в дампе у game ', ss.game_id, ', здесь у game ', ms.game_id)
          FROM $stg.steam_source ss JOIN $main.steam_source ms ON ms.steam_app_id = ss.steam_app_id
         WHERE ms.game_id <> ss.game_id AND ms.game_id NOT IN ($imported)
        UNION ALL
        SELECT CONCAT('screenshot ', s.id, ': в дампе у game ', s.game_id, ', здесь у game ', m.game_id)
          FROM $stg.screenshots s JOIN $main.screenshots m ON m.id = s.id
         WHERE m.game_id <> s.game_id AND m.game_id NOT IN ($imported)
        UNION ALL
        SELECT CONCAT('review ', r.id, ' (', IFNULL(r.external_id, '-'), '): в дампе у game ', r.game_id, ', здесь у game ', IFNULL(m.game_id, 'NULL'))
          FROM $stg.reviews r JOIN $main.reviews m ON m.id = r.id
         WHERE NOT (m.game_id <=> r.game_id) AND (m.game_id IS NULL OR m.game_id NOT IN ($imported))
        UNION ALL
        SELECT CONCAT('review ', r.id, ' (', r.external_id, '): в дампе у game ', r.game_id, ', здесь у game ', IFNULL(m.game_id, 'NULL'))
          FROM $stg.reviews r JOIN $main.reviews m ON m.external_id = r.external_id
         WHERE NOT (m.game_id <=> r.game_id) AND (m.game_id IS NULL OR m.game_id NOT IN ($imported))
        LIMIT 50;") || exit 1
    if [[ -n "$conflicts" ]]; then
        echo "Ошибка: дамп конфликтует с данными $DATABASE (база не от того же источника?). Ничего не изменено:"
        echo "$conflicts" | sed 's/^/  /'
        exit 1
    fi

    local summary
    summary=$(q "
        SELECT COUNT(*), IFNULL(MIN(id), 0), IFNULL(MAX(id), 0),
               IFNULL(SUM(id IN (SELECT id FROM $main.games)), 0),
               IFNULL(SUM(header_image_url NOT LIKE 'http%' AND header_image_url <> ''), 0),
               (SELECT COUNT(*) FROM $stg.screenshots), (SELECT COUNT(*) FROM $stg.reviews),
               (SELECT COUNT(*) FROM $stg.tags s WHERE NOT EXISTS (SELECT 1 FROM $main.tags t WHERE t.name = s.name))
          FROM $stg.games;") || exit 1
    local total min_id max_id existing media_paths screenshots reviews new_tags
    read -r total min_id max_id existing media_paths screenshots reviews new_tags <<< "$summary"
    if (( total == 0 )); then
        echo "В дампе нет игр, восстанавливать нечего"
        exit 1
    fi
    echo "Игр в дампе: $total (id $min_id..$max_id): новых $((total - existing)), обновится $existing"
    echo "Скриншотов: $screenshots, отзывов: $reviews, новых тегов: $new_tags. Пользовательские таблицы не меняются"
    if (( media_paths > 0 )); then
        echo "Внимание: у $media_paths игр в дампе обложка — путь Openinary, а не URL Steam: здесь эти файлы могут отсутствовать"
    fi
    if [[ "$assume_yes" != "--yes" ]]; then
        read -r -p "Применить к $DATABASE ($SERVER:$PORT)? [y/N] " answer
        [[ "$answer" =~ ^[YyДд]$ ]] || { echo "Отменено"; exit 1; }
    fi

    local games_cols games_update ss_cols shot_cols shot_update review_cols
    games_cols=$(table_columns games)
    # Путь Openinary (уже перенесённая обложка) остаётся, как в GameLoader; created_at — как был
    games_update="$main.games.header_image_url = IF($main.games.header_image_url <> '' AND $main.games.header_image_url NOT LIKE 'http%',
        $main.games.header_image_url, VALUES(header_image_url)), $(values_assignments "$(table_columns games id created_at header_image_url)")"
    ss_cols=$(table_columns steam_source id)
    shot_cols=$(table_columns screenshots)
    shot_update="$main.screenshots.url = IF($main.screenshots.url NOT LIKE 'http%', $main.screenshots.url, VALUES(url)),
        $(values_assignments "$(table_columns screenshots id url)")"
    review_cols=$(table_columns reviews)

    # Одна транзакция: клиент останавливается на первой ошибке, соединение закрывается, и всё откатывается.
    # games — только upsert (DELETE/REPLACE каскадом удалил бы оценки, списки и рекомендации пользователей)
    echo "Применяем..."
    if ! "${DB_CMD[@]}" "$DATABASE" <<SQL
START TRANSACTION;
INSERT INTO $main.tags (name)
    SELECT s.name FROM $stg.tags s WHERE NOT EXISTS (SELECT 1 FROM $main.tags t WHERE t.name = s.name);
INSERT INTO $main.games ($games_cols) SELECT $games_cols FROM $stg.games
    ON DUPLICATE KEY UPDATE $games_update;
DELETE m FROM $main.steam_source m JOIN $stg.games g ON g.id = m.game_id;
INSERT INTO $main.steam_source ($ss_cols) SELECT $ss_cols FROM $stg.steam_source;
DELETE m FROM $main.game_tags m JOIN $stg.games g ON g.id = m.game_id;
INSERT INTO $main.game_tags (game_id, tag_id)
    SELECT DISTINCT gt.game_id, t.id FROM $stg.game_tags gt
      JOIN $stg.tags st ON st.id = gt.tag_id JOIN $main.tags t ON t.name = st.name;
INSERT INTO $main.screenshots ($shot_cols) SELECT $shot_cols FROM $stg.screenshots
    ON DUPLICATE KEY UPDATE $shot_update;
DELETE m FROM $main.screenshots m JOIN $stg.games g ON g.id = m.game_id
    LEFT JOIN $stg.screenshots s ON s.id = m.id WHERE s.id IS NULL;
DELETE m FROM $main.reviews m JOIN $stg.games g ON g.id = m.game_id;
INSERT INTO $main.reviews ($review_cols) SELECT $review_cols FROM $stg.reviews;
COMMIT;
SQL
    then
        echo "Ошибка при применении, изменения откатены"
        exit 1
    fi
    echo "Готово: игры $min_id..$max_id в $DATABASE"
    echo "Дальше: векторы — uv run ml import <файл.npz>, поиск — uv run ml index-text --from $min_id --to $max_id"
    echo "(deploy/manage.sh restore games <папка> делает всё сразу)"
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
            "dump-games")
                database_dump_games "${@:3}"
                ;;
            "restore-games")
                database_restore_games "${3:-}" "${4:-}"
                ;;
            *)
                echo "Использование: ./manage.sh database {migrate|update|dump|restore|dump-games|restore-games}"
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
        echo "  database dump-games    — дамп только игр (games, steam_source, game_tags, screenshots, reviews + все tags):"
        echo "                           [--from N] [--to M] [путь] — диапазон games.id; не запускать во время парсинга"
        echo "  database restore-games — влить такой дамп: <файл> [--yes]; игры обновляются/добавляются, пользователи не трогаются"
        exit 1
        ;;
esac
