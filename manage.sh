#!/bin/bash

CONFIG_FILE="config.json"

command -v jq >/dev/null || { echo "jq не установлен"; exit 1; }
command -v dotnet >/dev/null || { echo "dotnet не установлен"; exit 1; }
command -v openssl >/dev/null || { echo "openssl не установлен"; exit 1; }

# Функция для получения значения из JSON по ключу
get_json_value() {
    local json_file="$1"
    local key="$2"
    jq -r ".\"$key\"" "$json_file" 2>/dev/null || echo ""
}

# Функция для обновления JSON‑файла (установка значения по ключу)
update_json_value() {
    local json_file="$1"
    local key="$2"
    local value="$3"
    jq --arg k "$key" --arg v "$value" \
       '.[$k] = $v' "$json_file" > temp.json && mv temp.json "$json_file"
}

# Получаем параметры из конфигурационного файла
SERVER=$(get_json_value "$CONFIG_FILE" "server")
PORT=$(get_json_value "$CONFIG_FILE" "port")
DATABASE=$(get_json_value "$CONFIG_FILE" "database")
USER=$(get_json_value "$CONFIG_FILE" "user")
PASSWORD=$(get_json_value "$CONFIG_FILE" "password")
JWT_KEY=$(get_json_value "$CONFIG_FILE" "jwt_key")
JWT_EXPIRE=$(get_json_value "$CONFIG_FILE" "jwt_expire_minutes")

# Проверка обязательных параметров
if [[ -z "$SERVER" || -z "$PORT" || -z "$DATABASE" || -z "$USER" || -z "$PASSWORD" ]]; then
    echo "Ошибка: не все обязательные параметры найдены в $CONFIG_FILE"
    exit 1
fi

# Валидация и генерация JWT‑ключа
validate_and_generate_jwt_key() {
    local generate_key=0
    if [[ -n "$JWT_KEY" ]]; then
        # Если ключ есть — проверяем длину
        local key_length=${#JWT_KEY}
        if (( key_length < 32 )); then
            echo "JWT‑ключ слишком короткий (меньше 32 байт) либо отсутствует. "
            generate_key=1
        else
            echo "Используем существующий JWT‑ключ (длина: $key_length байт)"
        fi
    fi
    if [[ generate_key -eq 1 ]]; then
        # Генерируем новый ключ (64 символа hex = 32 байта)
        echo "Генерируем новый JWT‑ключ..."
        JWT_KEY=$(openssl rand -hex 32)
        update_json_value "$CONFIG_FILE" "jwt_key" "$JWT_KEY"
        echo "Новый JWT‑ключ сгенерирован и сохранён в $CONFIG_FILE"
        echo "Ключ: $JWT_KEY"
    fi
}

# Строка подключения к БД
DB_CONNECTION="Server=$SERVER;Port=$PORT;Database=$DATABASE;User=$USER;Password=$PASSWORD;"

# Функции для обновления файлов в подпроектах (остаются без изменений)
update_api_project() {
    local project_path="$1"
    if [[ -f "$project_path/appsettings.json" ]]; then
        echo "Обновляем GReSym.API: $project_path"
        jq --arg conn "$DB_CONNECTION" \
           '.ConnectionStrings.DefaultConnection = $conn' \
           "$project_path/appsettings.json" > temp.json && mv temp.json "$project_path/appsettings.json"
        if [[ -n "$JWT_KEY" && -n "$JWT_EXPIRE" ]]; then
            jq --arg key "$JWT_KEY" --argjson expire "$JWT_EXPIRE" \
               '.Jwt.Key = $key | .Jwt.ExpireMinutes = $expire' \
               "$project_path/appsettings.json" > temp.json && mv temp.json "$project_path/appsettings.json"
        fi
    else
        echo "Файл appsettings.json не найден в $project_path"
    fi
}

update_infrastructure_project() {
    local project_path="$1"
    if [[ -f "$project_path/appsettings.json" ]]; then
        echo "Обновляем GReSym.Infrastructure: $project_path"
        jq --arg conn "$DB_CONNECTION" \
           '.ConnectionStrings.DefaultConnection = $conn' \
           "$project_path/appsettings.json" > temp.json && mv temp.json "$project_path/appsettings.json"
    else
        echo "Файл appsettings.json не найден в $project_path"
    fi
}

update_parser_ui_project() {
    local project_path="$1"
    if [[ -f "$project_path/appsettings.json" ]]; then
        echo "Обновляем GReSym.Parser.UI: $project_path"
        jq --arg conn "$DB_CONNECTION" \
           '.ConnectionStrings.DefaultConnection = $conn' \
           "$project_path/appsettings.json" > temp.json && mv temp.json "$project_path/appsettings.json"
    else
        echo "Файл appsettings.json не найден в $project_path"
    fi
}

clean_passwords() {
    local project_path="$1"
    local file="$project_path/appsettings.json"

    if [[ -f "$file" ]]; then
        echo "Очищаем пароли в: $project_path"

        local CLEANED_DB_CONNECTION="Server=###;Port=###;Database=###;User=###;Password=###;"
        local tmp

        tmp=$(mktemp)

        jq --arg clean_conn "$CLEANED_DB_CONNECTION" '
            if (.ConnectionStrings? | type) == "object" then
                .ConnectionStrings.DefaultConnection = $clean_conn
            else
                .
            end
            |
            if (.Jwt? | type) == "object" then
                .Jwt.Key = "###"
            else
                .
            end
        ' "$file" > "$tmp" && mv "$tmp" "$file"

    else
        echo "Файл appsettings.json не найден в $project_path"
    fi
}

clean_build() {
    for d in */ ; do
        echo "Очищаем $d"
        cd $d
        dotnet clean
        cd ..
    done
    echo "Очистка закончена"
}

check_config() {
    # Проверяем существование конфигурационного файла
    if [[ ! -f "$CONFIG_FILE" ]]; then
        echo "Создаём конфигурационный файл $CONFIG_FILE"
        cat > "$CONFIG_FILE" << EOF
{
"server": "localhost",
"port": 3306,
"database": "exampleDBname",
"user": "dbuser",
"password": "securepassword123",
"jwt_key": "",
"jwt_expire_minutes": 60
}
EOF
        echo "Файл $CONFIG_FILE создан, необходимо его настроить, после запустите скрипт снова."
        exit 1
    fi
}

run_api() {
    cd GReSym.API || exit 1
    dotnet run
}

run_parser() {
    cd GReSym.Parser.UI || exit 1
    dotnet run
}

database_migrate() {
      local migration_name="$1"
      if [[ ! -n $migration_name ]]; then
          migration_name="migration_$(date -Iminutes)"
      fi
      cd GReSym.Infrastructure || exit 1
      dotnet ef migrations add "$migration_name"
      echo "Миграция $migration_name создана!"
}

database_update() {
    cd GReSym.Infrastructure || exit 1
    dotnet ef database update
}

# Основная логика скрипта
case "${1:-}" in
    "setup")
        check_config
        echo "Настраиваем проекты с параметрами из $CONFIG_FILE"
        validate_and_generate_jwt_key
        update_api_project "GReSym.API"
        update_infrastructure_project "GReSym.Infrastructure"
        update_parser_ui_project "GReSym.Parser.UI"
        ;;
    "clean")
        case "${2:-}" in
            "credentials")
                echo "Очищаем пароли во всех проектах"
                clean_passwords "GReSym.API"
                clean_passwords "GReSym.Infrastructure"
                clean_passwords "GReSym.Parser.UI"
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
                database_migrate $3
                ;;
            "update")
                database_update
                ;;
            *)
                echo "Использование: ./manage.sh database {migrate|update}"
                exit 1
                ;;
        esac
        ;;
    *)
        echo "Использование: $0 {setup|clean|run|database}"
        echo "  setup             — настроить пароли и ключи (в соответствии с файлом config.json)"
        echo "  clean credentials — очистить пароли (из config.json нужно убирать самостоятельно)"
        echo "  clean build       — очистить файлы сборки"
        echo "  run api           — запустить API"
        echo "  run parser        — запустить Parser"
        echo "  database migrate  — создать миграцию, опционально можно задать название миграции"
        echo "  database update   — применить миграции"
        exit 1
        ;;
esac
