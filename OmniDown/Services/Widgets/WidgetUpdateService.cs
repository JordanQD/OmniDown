using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Providers;

namespace OmniDown.Services.Widgets;

public sealed class WidgetUpdateService
{
    public const string DefinitionId = "OmniDown_Downloads";

    private readonly WidgetCardBuilder _cardBuilder = new();

    public void UpdateWidget(string widgetId, WidgetSize size, WidgetSnapshot? snapshot)
    {
        string cardJson = _cardBuilder.BuildCard(snapshot, size);

        var options = new WidgetUpdateRequestOptions(widgetId)
        {
            Template = cardJson
        };

        WidgetManager.GetDefault().UpdateWidget(options);
    }

    public void UpdateAll(WidgetSnapshot? snapshot)
    {
        foreach (WidgetInfo widgetInfo in WidgetManager.GetDefault().GetWidgetInfos())
        {
            WidgetContext context = widgetInfo.WidgetContext;
            if (context.DefinitionId == DefinitionId)
            {
                UpdateWidget(context.Id, context.Size, snapshot);
            }
        }
    }
}
