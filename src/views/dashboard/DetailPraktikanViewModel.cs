using AsLab.Services;
using AsLab.Services.Dashboard;
using AsLab.Ui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

namespace AsLab.Views.Dashboard;

/// <summary>Detail praktikan (FR-16): profil, grafik progres nilai per modul, riwayat absensi.</summary>
public partial class DetailPraktikanViewModel : HalamanViewModel
{
    public override string Judul => "Detail Praktikan";

    public int PraktikanId { get; }

    [ObservableProperty] private DataDetailPraktikan? data;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NilaiSementara), nameof(NilaiAkhir))]
    private DetailKelas? kelasTerpilih;
    [ObservableProperty] private ISeries[] grafikProgres = [];
    [ObservableProperty] private Axis[] sumbuX = [];
    [ObservableProperty] private Axis[] sumbuY = [];

    public DetailPraktikanViewModel(int praktikanId)
    {
        PraktikanId = praktikanId;
        Coba(() =>
        {
            Data = DetailPraktikanService.Ambil(praktikanId);
            KelasTerpilih = Data.DaftarKelas.FirstOrDefault();
        });
    }

    // Dipanggil otomatis oleh CommunityToolkit setiap KelasTerpilih berubah.
    partial void OnKelasTerpilihChanged(DetailKelas? value)
    {
        if (value == null) return;

        // Hanya modul yang sudah dinilai di kelas; modul tanpa nilai bernilai 0 (BR-02).
        var label = new List<string>();
        var nilai = new List<double>();
        for (var i = 0; i < value.Rekap.DaftarModul.Count; i++)
        {
            var nilaiModul = value.Baris.NilaiPerModul[i];
            var adaNilaiDiKelas = value.Rekap.Baris.Any(b => b.NilaiPerModul[i] != null);
            if (!adaNilaiDiKelas) continue;
            label.Add("M" + value.Rekap.DaftarModul[i].Nomor);
            nilai.Add((double)(nilaiModul ?? 0));
        }

        GrafikProgres =
        [
            new LineSeries<double>
            {
                Name = "Nilai modul",
                Values = nilai.ToArray(),
                Fill = null,
                GeometrySize = 10,
                LineSmoothness = 0,
                Stroke = new SolidColorPaint(Grafik.WarnaUtama) { StrokeThickness = 3 },
                GeometryStroke = new SolidColorPaint(Grafik.WarnaUtama) { StrokeThickness = 3 },
            },
        ];
        SumbuX = Grafik.SumbuLabel(label);
        SumbuY = Grafik.SumbuNilai();
    }

    public string NilaiSementara => Format.Angka(KelasTerpilih?.Baris.NilaiSementara);
    public string NilaiAkhir => Format.Angka(KelasTerpilih?.Baris.NilaiAkhir);

    [RelayCommand]
    private void Kembali() => Navigasi.Kembali();
}
