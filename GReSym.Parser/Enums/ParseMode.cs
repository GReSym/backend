namespace GReSym.Parser.Enums;

public enum ParserMode
{
    Full,           // Полный парсинг
    Incremental,    // Только новые/обновленные
    Update          // Обновление существующих
}