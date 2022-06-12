using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace ChatModApp.Controls;

public class AnimatedImage : Image
{
    public override void Render(DrawingContext context)
    {
        //Useless
        //Dispatcher.UIThread.Post(InvalidateMeasure, DispatcherPriority.Background);
        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
        
        base.Render(context);
    }
}