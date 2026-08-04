using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace EnterpriseBillingSystem.Wpf.Converters;

/// <summary>
/// Converts a relative image path (e.g. "/uploads/products/abc.jpg") into
/// a full BitmapImage using the ApiImageBaseUrl set at application startup.
/// If the path is already an absolute URL it is used as-is.
/// Returns null if the path is null or empty (shows the placeholder icon).
/// </summary>
public class RelativeImageUrlConverter : IValueConverter
{
    /// <summary>
    /// Base URL of the media server, e.g. "http://167.99.13.177:8081".
    /// Set once from App.xaml.cs before any binding resolves.
    /// </summary>
    public static string ApiImageBaseUrl { get; set; } = string.Empty;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            // If it's already an absolute URI use it directly
            if (Uri.TryCreate(path, UriKind.Absolute, out var absoluteUri))
                return new BitmapImage(absoluteUri);

            // It's a relative path — prepend the configured media base
            var baseUrl = ApiImageBaseUrl.TrimEnd('/');
            var fullUrl = $"{baseUrl}/{path.TrimStart('/')}";

            if (Uri.TryCreate(fullUrl, UriKind.Absolute, out var fullUri))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = fullUri;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bmp.EndInit();
                return bmp;
            }
        }
        catch
        {
            // Any failure (network, bad URL, etc.) → return null to show placeholder
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
