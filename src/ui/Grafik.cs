using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace AsLab.Ui;

/// <summary>Warna dan sumbu grafik LiveCharts yang menyesuaikan tema terang/gelap.</summary>
public static class Grafik
{
    public static readonly SKColor WarnaUtama = SKColor.Parse("#5C7CFA");

    public static SolidColorPaint WarnaTeks =>
        new(TemaManager.Gelap ? SKColor.Parse("#9AA5B4") : SKColor.Parse("#5F6B7A"));

    private static SolidColorPaint WarnaGaris =>
        new(TemaManager.Gelap ? SKColor.Parse("#2F3846") : SKColor.Parse("#E3E7EE")) { StrokeThickness = 1 };

    public static Axis[] SumbuLabel(IList<string> label) =>
    [
        new Axis { Labels = label, LabelsPaint = WarnaTeks, TextSize = 12 },
    ];

    /// <summary>Sumbu nilai 0–100.</summary>
    public static Axis[] SumbuNilai() =>
    [
        new Axis { MinLimit = 0, MaxLimit = 100, LabelsPaint = WarnaTeks, SeparatorsPaint = WarnaGaris, TextSize = 12 },
    ];

    public static SKColor WarnaHuruf(string huruf) => huruf switch
    {
        "A" => SKColor.Parse("#2E9E5B"),
        "AB" => SKColor.Parse("#6CC08B"),
        "B" => SKColor.Parse("#3B5BDB"),
        "BC" => SKColor.Parse("#7B93F7"),
        "C" => SKColor.Parse("#E0A100"),
        "D" => SKColor.Parse("#E8590C"),
        _ => SKColor.Parse("#D63A3A"),
    };
}
