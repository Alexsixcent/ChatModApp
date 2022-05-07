using Avalonia.Media.Imaging;

namespace ChatModApp.Tools;

public interface IBitmapSource
{
    void SetImageSource(IBitmap? bitmap);
}