using System.Windows;
using AsLab.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;

namespace AsLab.Ui;

/// <summary>
/// Kelas dasar semua ViewModel halaman. Menyediakan judul halaman
/// serta pesan error/sukses yang tampil di bagian atas halaman.
/// </summary>
public abstract partial class HalamanViewModel : ObservableObject
{
    public abstract string Judul { get; }

    [ObservableProperty]
    private string? pesanError;

    [ObservableProperty]
    private string? pesanSukses;

    /// <summary>
    /// Menjalankan aksi dan menampilkan pesan error bila gagal.
    /// Mengembalikan true bila berhasil.
    /// </summary>
    protected bool Coba(Action aksi, string? pesanBerhasil = null)
    {
        PesanError = null;
        PesanSukses = null;
        try
        {
            aksi();
            PesanSukses = pesanBerhasil;
            return true;
        }
        catch (AturanException ex)
        {
            PesanError = ex.Message;
        }
        catch (Exception ex)
        {
            PesanError = "Terjadi kesalahan: " + ex.Message;
        }
        return false;
    }

    /// <summary>
    /// Untuk memuat ulang data setelah aksi. Berbeda dari <see cref="Coba"/>,
    /// pesan sukses yang sedang tampil tidak dihapus.
    /// </summary>
    protected void Muat(Action aksi)
    {
        try
        {
            aksi();
        }
        catch (Exception ex)
        {
            PesanError = ex is AturanException ? ex.Message : "Terjadi kesalahan: " + ex.Message;
        }
    }
}

/// <summary>Pilihan sederhana untuk dropdown filter (Id null = "Semua").</summary>
public record Opsi(int? Id, string Label)
{
    public override string ToString() => Label;
}

/// <summary>Dialog standar Windows.</summary>
public static class Dialog
{
    public static bool Konfirmasi(string pesan) =>
        MessageBox.Show(pesan, "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No)
        == MessageBoxResult.Yes;

    public static void Info(string pesan, string judul = "AsLab") =>
        MessageBox.Show(pesan, judul, MessageBoxButton.OK, MessageBoxImage.Information);

    public static string? PilihFileSimpan(string filter, string namaAwal)
    {
        var dialog = new SaveFileDialog { Filter = filter, FileName = namaAwal, OverwritePrompt = true };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public static string? PilihFileBuka(string filter)
    {
        var dialog = new OpenFileDialog { Filter = filter };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
