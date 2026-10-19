using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AsLab.Ui;

/// <summary>
/// Untuk RadioButton absensi: tercentang bila Status sama dengan ConverterParameter,
/// dan saat dicentang mengisi Status dengan ConverterParameter.
/// </summary>
public class StatusSamaKonverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Equals(value as string, parameter as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true && parameter != null ? parameter : Binding.DoNothing;
}

/// <summary>Visible bila nilainya "ada" (bukan null, bukan teks kosong, bukan false, bukan 0). Balik=True membalik hasilnya.</summary>
public class AdaKeVisibilityKonverter : IValueConverter
{
    public bool Balik { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var ada = value switch
        {
            null => false,
            string teks => teks.Length > 0,
            bool b => b,
            int angka => angka != 0,
            _ => true,
        };
        return ada != Balik ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
