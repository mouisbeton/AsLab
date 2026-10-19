using System.Collections.ObjectModel;
using AsLab.Services;
using AsLab.Services.Master;
using AsLab.Services.Penilaian;
using AsLab.Services.Pengaturan;
using AsLab.Ui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsLab.Views.Dashboard;

/// <summary>Satu baris di daftar komponen nilai yang bisa diedit.</summary>
public partial class KomponenVm : ObservableObject
{
    public int? Id { get; init; } // null = komponen baru, belum tersimpan

    [ObservableProperty] private string nama = "";
    [ObservableProperty] private string bobot = "";
}

/// <summary>Pengaturan (koordinator): komponen nilai, tema, backup database, log aktivitas.</summary>
public partial class PengaturanViewModel : HalamanViewModel
{
    public override string Judul => "Pengaturan";

    public ObservableCollection<BarisMataPraktikum> DaftarPraktikum { get; } = [];
    public ObservableCollection<KomponenVm> Komponen { get; } = [];
    public ObservableCollection<BarisLog> Log { get; } = [];

    [ObservableProperty] private BarisMataPraktikum? praktikumTerpilih;

    /// <summary>True bila sudah ada nilai: komponen hanya boleh diganti nama/bobotnya (BR-13).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BolehTambahHapus))]
    [NotifyCanExecuteChangedFor(nameof(TambahKomponenCommand), nameof(HapusKomponenCommand))]
    private bool sudahAdaNilai;

    [ObservableProperty] private bool temaGelap = TemaManager.Gelap;

    public bool BolehTambahHapus => !SudahAdaNilai;

    public string TotalBobot
    {
        get
        {
            decimal total = 0;
            foreach (var k in Komponen)
                if (Format.CobaBacaAngka(k.Bobot, out var b)) total += b;
            return $"Total: {total:0.##}%";
        }
    }

    public PengaturanViewModel()
    {
        Coba(() =>
        {
            foreach (var p in PraktikumService.Semua()) DaftarPraktikum.Add(p);
            PraktikumTerpilih = DaftarPraktikum.FirstOrDefault();
            MuatLog();
        });
    }

    partial void OnPraktikumTerpilihChanged(BarisMataPraktikum? value) => MuatKomponen();

    private void MuatKomponen()
    {
        Komponen.Clear();
        if (PraktikumTerpilih == null) return;
        SudahAdaNilai = PengaturanService.SudahAdaNilai(PraktikumTerpilih.Id);
        foreach (var k in PengaturanService.AmbilKomponen(PraktikumTerpilih.Id))
            TambahBaris(new KomponenVm { Id = k.Id, Nama = k.Nama, Bobot = k.Bobot.ToString("0.##") });
        OnPropertyChanged(nameof(TotalBobot));
    }

    private void TambahBaris(KomponenVm baris)
    {
        // Total bobot dihitung ulang setiap kali sebuah bobot diketik.
        baris.PropertyChanged += (_, _) => OnPropertyChanged(nameof(TotalBobot));
        Komponen.Add(baris);
    }

    // CanExecute: tombol otomatis nonaktif bila BolehTambahHapus bernilai false.
    [RelayCommand(CanExecute = nameof(BolehTambahHapus))]
    private void TambahKomponen()
    {
        TambahBaris(new KomponenVm { Nama = "", Bobot = "0" });
        OnPropertyChanged(nameof(TotalBobot));
    }

    [RelayCommand(CanExecute = nameof(BolehTambahHapus))]
    private void HapusKomponen(KomponenVm baris)
    {
        Komponen.Remove(baris);
        OnPropertyChanged(nameof(TotalBobot));
    }

    [RelayCommand]
    private void SimpanKomponen()
    {
        if (PraktikumTerpilih == null) return;

        var daftar = new List<InputKomponen>();
        foreach (var k in Komponen)
        {
            if (!Format.CobaBacaAngka(k.Bobot, out var bobot))
            {
                PesanSukses = null;
                PesanError = $"Bobot komponen \"{k.Nama}\" harus berupa angka, contoh 20.";
                return;
            }
            daftar.Add(new InputKomponen(k.Id, k.Nama, bobot));
        }

        var jumlah = 0;
        if (Coba(() => jumlah = PengaturanService.SimpanKomponen(PraktikumTerpilih.Id, daftar)))
        {
            PesanSukses = $"Komponen nilai disimpan. {jumlah} nilai modul dihitung ulang.";
            MuatKomponen();
            MuatLog();
        }
    }

    partial void OnTemaGelapChanged(bool value)
    {
        if (value != TemaManager.Gelap)
            Navigasi.Utama?.GantiTemaCommand.Execute(null);
    }

    [RelayCommand]
    private void Backup()
    {
        var file = Dialog.PilihFileSimpan("Backup SQL Server (*.bak)|*.bak", $"aslab-backup-{DateTime.Now:yyyyMMdd-HHmm}.bak");
        if (file == null) return;
        if (Coba(() => PengaturanService.Backup(file), $"Backup tersimpan di {file}"))
            MuatLog();
    }

    private void MuatLog()
    {
        Log.Clear();
        foreach (var l in LogService.Terbaru(100)) Log.Add(l);
    }
}
