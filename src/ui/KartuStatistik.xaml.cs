using System.Windows;
using System.Windows.Controls;

namespace AsLab.Ui;

/// <summary>Komponen bersama: kartu statistik dengan judul, angka, keterangan, dan ikon.</summary>
public partial class KartuStatistik : UserControl
{
    public static readonly DependencyProperty JudulProperty =
        DependencyProperty.Register(nameof(Judul), typeof(string), typeof(KartuStatistik));

    public static readonly DependencyProperty NilaiProperty =
        DependencyProperty.Register(nameof(Nilai), typeof(string), typeof(KartuStatistik));

    public static readonly DependencyProperty KeteranganProperty =
        DependencyProperty.Register(nameof(Keterangan), typeof(string), typeof(KartuStatistik));

    public static readonly DependencyProperty IkonProperty =
        DependencyProperty.Register(nameof(Ikon), typeof(string), typeof(KartuStatistik));

    public string Judul
    {
        get => (string)GetValue(JudulProperty);
        set => SetValue(JudulProperty, value);
    }

    public string Nilai
    {
        get => (string)GetValue(NilaiProperty);
        set => SetValue(NilaiProperty, value);
    }

    public string Keterangan
    {
        get => (string)GetValue(KeteranganProperty);
        set => SetValue(KeteranganProperty, value);
    }

    public string Ikon
    {
        get => (string)GetValue(IkonProperty);
        set => SetValue(IkonProperty, value);
    }

    public KartuStatistik()
    {
        InitializeComponent();
    }
}
