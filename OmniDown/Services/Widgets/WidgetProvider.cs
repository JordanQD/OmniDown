using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Providers;
using OmniDown.Services.Storage;
using System;

namespace OmniDown.Services.Widgets;

[System.Runtime.InteropServices.Guid("E3D8F5A1-2B4C-4E6F-8A9D-1C2B3E4F5A6B")]
public sealed class OmniDownWidgetProvider : IWidgetProvider
{
    private readonly WidgetSnapshotStore _snapshotStore = new();
    private readonly WidgetUpdateService _updateService = new();

    public void CreateWidget(WidgetContext widgetContext)
    {
        UpdateWidgetContent(widgetContext.Id, widgetContext.Size);
    }

    public void DeleteWidget(string widgetId, string customState) { }

    public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        if (string.Equals(actionInvokedArgs.Verb, "open", StringComparison.OrdinalIgnoreCase))
        {
            _ = Windows.System.Launcher.LaunchUriAsync(new Uri("omnidown://open"));
        }
    }

    public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        UpdateWidgetContent(
            contextChangedArgs.WidgetContext.Id,
            contextChangedArgs.WidgetContext.Size);
    }

    public void Activate(WidgetContext widgetContext)
    {
        UpdateWidgetContent(widgetContext.Id, widgetContext.Size);
    }

    public void Deactivate(string widgetId) { }

    private void UpdateWidgetContent(string widgetId, WidgetSize size)
    {
        WidgetSnapshot? snapshot = _snapshotStore.Load();
        _updateService.UpdateWidget(widgetId, size, snapshot);
    }
}
