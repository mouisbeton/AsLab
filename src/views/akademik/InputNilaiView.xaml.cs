using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace AsLab.Views.Akademik;

public partial class InputNilaiView : UserControl
{
    private readonly List<DataGridColumn> kolomSkor = [];

    public InputNilaiView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is InputNilaiViewModel vm)
            {
                vm.PropertyChanged += ViewModel_PropertyChanged;
                BuatKolomSkor(vm);
            }
        };
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InputNilaiViewModel.Komponen) && sender is InputNilaiViewModel vm)
            BuatKolomSkor(vm);
    }

    /// <summary>
    /// Membuat satu kolom per komponen nilai (misalnya Pretest, Praktik, Laporan), disisipkan setelah kolom Nama.
    /// Setiap sel menampilkan TemplateSel dengan DataContext Sel[i] milik baris tersebut.
    /// </summary>
    private void BuatKolomSkor(InputNilaiViewModel vm)
    {
        foreach (var kolom in kolomSkor) Tabel.Columns.Remove(kolom);
        kolomSkor.Clear();

        for (var i = 0; i < vm.Komponen.Count; i++)
        {
            var isiSel = new FrameworkElementFactory(typeof(ContentPresenter));
            isiSel.SetBinding(ContentPresenter.ContentProperty, new Binding($"Sel[{i}]"));
            isiSel.SetValue(ContentPresenter.ContentTemplateProperty, FindResource("TemplateSel"));

            var kolom = new DataGridTemplateColumn
            {
                Header = vm.Komponen[i].Teks, // contoh: "Pretest 20%"
                CellTemplate = new DataTemplate { VisualTree = isiSel },
                Width = 130,
            };
            Tabel.Columns.Insert(2 + i, kolom);
            kolomSkor.Add(kolom);
        }
    }
}
