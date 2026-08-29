using Gtk;

namespace GReSym.Parser.UI;

public class CheckpointsTab : IParserTab
{
    private readonly Builder _builder;

    public CheckpointsTab(Builder builder) => _builder = builder;

    public Widget GetWidget() => (Widget)_builder.GetObject("checkpoints_tab");

    public void Initialize()
    {
        // Логика выбора и отображения чекпоинтов
    }
}