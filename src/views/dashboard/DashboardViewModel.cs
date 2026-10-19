using System.Collections.ObjectModel;
using AsLab.Services;
using AsLab.Services.Dashboard;
using AsLab.Services.Master;
using AsLab.Ui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

namespace AsLab.Views.Dashboard;

/// <summary>Dashboard (FR-10): kartu statistik, 2 grafik, praktikan berisiko, aktivitas terbaru.</summary>
public partial class DashboardViewModel : HalamanViewModel
{
    public override string Judul => "Dashboard";

    [ObservableProperty] private string totalPraktikan = "–";
    [ObservableProperty] private string kelasAktif = "–";
    [ObservableProperty] private string modulBerjalan = "–";
    [ObservableProperty] private string rataKehadiran = "–";

    [ObservableProperty] private ISeries[] grafikBatang = [];
    [ObservableProperty] private Axis[] sumbuX = [];
    [ObservableProperty] private Axis[] sumbuY = [];
    [ObservableProperty] private ISeries[] grafikDonat = [];
    [ObservableProperty] private SolidColorPaint warnaLegenda = Grafik.WarnaTeks;

    public ObservableCollection<BarisRisiko> Berisiko { get; } = [];
    public ObservableCollection<BarisLog> Aktivitas { get; } = [];

    [ObservableProperty] private BarisRisiko? risikoTerpilih;

    public DashboardViewModel()
    {
        Muat();
    }

    [RelayCommand]
    private void Muat()
    {
        Coba(() =>
        {
            var data = DashboardService.Ambil();

            TotalPraktikan = data.TotalPraktikan.ToString();
            KelasAktif = data.KelasAktif.ToString();
            ModulBerjalan = $"{data.ModulBerjalan} / {data.TotalModul}";
            RataKehadiran = Format.Persen(data.RataKehadiran);

            GrafikBatang =
            [
                new ColumnSeries<double>
                {
                    Name = "Rata-rata nilai",
                    Values = data.RataPerModul.Select(r => (double)r.Rata).ToArray(),
                    Fill = new SolidColorPaint(Grafik.WarnaUtama),
                    MaxBarWidth = 48,
                },
            ];
            SumbuX = Grafik.SumbuLabel(data.RataPerModul.Select(r => r.Label).ToArray());
            SumbuY = Grafik.SumbuNilai();

            GrafikDonat = data.DistribusiHuruf
                .Select(d => (ISeries)new PieSeries<int>
                {
                    Name = $"{d.Huruf} ({d.Jumlah})",
                    Values = [d.Jumlah],
                    InnerRadius = 55,
                    Fill = new SolidColorPaint(Grafik.WarnaHuruf(d.Huruf)),
                })
                .ToArray();
            WarnaLegenda = Grafik.WarnaTeks;

            Berisiko.Clear();
            foreach (var b in data.Berisiko) Berisiko.Add(b);
            Aktivitas.Clear();
            foreach (var a in data.Aktivitas) Aktivitas.Add(a);
        });
    }

    [RelayCommand]
    private void BukaDetail()
    {
        if (RisikoTerpilih != null)
            Navigasi.BukaDetailPraktikan(RisikoTerpilih.PraktikanId);
    }
}
