using System.Collections.ObjectModel;
using AsLab.Auth;
using AsLab.Services;
using AsLab.Services.Laporan;
using AsLab.Services.Master;
using AsLab.Services.Penilaian;
using AsLab.Ui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsLab.Views.Akademik;

/// <summary>Rekap Nilai (FR-08, FR-13, FR-14): nilai akhir per kelas, export, finalisasi.</summary>
public partial class RekapViewModel : HalamanViewModel
{
    public override string Judul => "Rekap Nilai";

    public List<OpsiKelas> DaftarKelas { get; } = [];
    public ObservableCollection<BarisRekap> Baris { get; } = [];
    public bool IsKoordinator { get; } = Sesi.Wajib.IsKoordinator;

    [ObservableProperty] private OpsiKelas? kelasTerpilih;
    [ObservableProperty] private BarisRekap? barisTerpilih;

    /// <summary>Rekap yang sedang tampil. View membuat kolom M1..Mn dari DaftarModul-nya.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Terkunci), nameof(Keterangan), nameof(BisaKunci), nameof(BisaBukaKunci))]
    private RekapKelas? rekap;

    public bool Terkunci => Rekap?.Terkunci == true;
    public bool BisaKunci => IsKoordinator && Rekap != null && !Rekap.Terkunci;
    public bool BisaBukaKunci => IsKoordinator && Terkunci;

    public string Keterangan => Rekap == null ? "" :
        $"{Rekap.NamaPraktikum} · Dosen: {Rekap.NamaDosen} · Asisten: " +
        (Rekap.NamaAsisten.Count == 0 ? "–" : string.Join(", ", Rekap.NamaAsisten)) +
        (Rekap.Selesai ? "" : " · Praktikum masih berjalan: huruf memakai nilai sementara");

    public RekapViewModel()
    {
        Coba(() => DaftarKelas.AddRange(KelasService.UntukPengguna()));
        KelasTerpilih = DaftarKelas.FirstOrDefault();
    }

    partial void OnKelasTerpilihChanged(OpsiKelas? value) => MuatRekap();

    private void MuatRekap()
    {
        Baris.Clear();
        Rekap = null;
        if (KelasTerpilih == null) return;
        Muat(() =>
        {
            Rekap = RekapService.Hitung(KelasTerpilih.Id);
            foreach (var b in Rekap.Baris) Baris.Add(b);
        });
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (Rekap == null) return;
        var file = Dialog.PilihFileSimpan("Excel (*.xlsx)|*.xlsx", $"Rekap-{Rekap.KodePraktikum}-Kelas-{Rekap.NamaKelas}.xlsx");
        if (file != null)
            Coba(() => ExportService.KeExcel(Rekap, file), $"Rekap diekspor ke {file}");
    }

    [RelayCommand]
    private void ExportPdf()
    {
        if (Rekap == null) return;
        var file = Dialog.PilihFileSimpan("PDF (*.pdf)|*.pdf", $"Rekap-{Rekap.KodePraktikum}-Kelas-{Rekap.NamaKelas}.pdf");
        if (file != null)
            Coba(() => ExportService.KePdf(Rekap, file), $"Rekap diekspor ke {file}");
    }

    [RelayCommand]
    private void Kunci()
    {
        if (Rekap == null) return;
        if (!Dialog.Konfirmasi($"Finalisasi dan kunci {Rekap.Label}?\nSetelah dikunci, absensi dan nilai kelas ini tidak dapat diubah sampai kunci dibuka.")) return;
        if (Coba(() => KunciService.Kunci(Rekap.KelasId), "Kelas difinalisasi dan dikunci."))
            MuatRekap();
    }

    [RelayCommand]
    private void BukaKunci()
    {
        if (Rekap == null) return;
        if (!Dialog.Konfirmasi($"Buka kunci {Rekap.Label}? Asisten akan dapat mengubah absensi dan nilai lagi.")) return;
        if (Coba(() => KunciService.BukaKunci(Rekap.KelasId), "Kunci kelas dibuka."))
            MuatRekap();
    }

    [RelayCommand]
    private void BukaDetail()
    {
        if (BarisTerpilih != null)
            Navigasi.BukaDetailPraktikan(BarisTerpilih.PraktikanId);
    }
}
