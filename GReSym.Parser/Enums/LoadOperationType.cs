using GReSym.Parser.Enums;

public enum LoadOperationType
{
    Insert,     // Вставка новой записи
    Update,     // Обновление существующей записи
    Skip,       // Пропуск (данные уже актуальны)
    Delete      // Удаление записи
}