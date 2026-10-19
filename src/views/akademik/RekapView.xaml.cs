using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace AsLab.Views.Akademik;

public partial class RekapView : UserControl
{
    private readonly List<DataGridColumn> kolomModul = [];

    public RekapView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is RekapViewModel vm)
            {
                vm.PropertyChanged += ViewModel_PropertyChanged;
                BuatKolomModul(vm);
            }
        };
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RekapViewModel.Rekap) && sender is RekapViewModel vm)
            BuatKolomModul(vm);
    }

    /// <summary>Membuat kolom M1, M2, ... sesuai jumlah modul, disisipkan setelah kolom Nama.</summary>
    private void BuatKolomModul(RekapViewModel vm)
    {
        foreach (var kolom in kolomModul) Tabel.Columns.Remove(kolom);
        kolomModul.Clear();
        if (vm.Rekap == null) return;

        for (var i = 0; i < vm.Rekap.DaftarModul.Count; i++)
        {
            var modul = vm.Rekap.DaftarModul[i];
            var kolom = new DataGridTextColumn
            {
                Header = "M" + modul.Nomor,
                Binding = new Binding($"NilaiPerModul[{i}]") { StringFormat = "0.00" },
                ElementStyle = (Style)FindResource("SelTeks"),
                Width = 68,
            };
            Tabel.Columns.Insert(2 + i, kolom);
            kolomModul.Add(kolom);
        }
    }
}
