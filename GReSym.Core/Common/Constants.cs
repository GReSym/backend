namespace GReSym.Core.Common;

public static class Constants
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
        public const string Moderator = "Moderator";
    }
    
    public static class Validation
    {
        public const int MaxEmailLength = 100;
        public const int MaxPasswordHashLength = 100;
        public const int MaxDisplayNameLength = 64;
        public const int MaxGameTitleLength = 255;
    }
    
    public static class Api
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
    }

    public static class Tags
    {
        /// <summary>Steam store features and categories, not gameplay. Skipped when picking a user's favorite tag.</summary>
        public static readonly HashSet<string> StoreFeatures = new(StringComparer.OrdinalIgnoreCase)
        {
            "Семейный доступ", "Для одного игрока", "Достижения Steam", "Steam Cloud", "Полная поддержка контроллеров",
            "Частичная поддержка контроллеров", "Расширенное управление громкостью", "Можно играть без QTE",
            "Сохранение в любое время", "Альтернативные цвета", "Управление только клавиатурой", "Стереозвук",
            "Включён античит Valve", "Remote Play Together", "Настройки движения камеры", "Remote Play на телефоне",
            "Remote Play на планшете", "Remote Play на телевизоре", "Поддержка модификаций", "Коллекционные карточки Steam",
            "Скрытые субтитры", "Мастерская Steam", "Настройки субтитров", "Объёмный звук", "Поддержка комментариев",
            "Временная шкала Steam", "Внутриигровые покупки", "Таблицы лидеров Steam", "Предметы для SteamVR",
            "Регулируемый размер текста", "Поддержка отслеживания контроллеров", "Поддержка HDR",
            "Преобразование речи в текст в чате", "Преобразование текста в речь в чате", "Steam Machine",
            "Поддержка контроллера DualShock", "Поддержка контроллера DualSense", "Поддержка API системы ввода Steam",
            "Ранний доступ",
        };
    }
}
