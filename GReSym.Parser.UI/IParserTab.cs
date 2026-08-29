using Gtk;

public interface IParserTab
{
    Widget GetWidget(); // Возвращает контейнер вкладки
    void Initialize();  // Инициализация, привязка событий
}