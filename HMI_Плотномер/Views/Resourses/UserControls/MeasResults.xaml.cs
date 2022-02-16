using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IDensity.Views.Resourses.UserControls
{
    /// <summary>
    /// Логика взаимодействия для MeasResults.xaml
    /// </summary>
    public partial class MeasResults : UserControl
    {


        public int SelIndex
        {
            get { return (int)GetValue(SelIndexProperty); }
            set { SetValue(SelIndexProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelIndex.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelIndexProperty =
            DependencyProperty.Register(nameof(SelIndex), typeof(int), typeof(MeasResults), new PropertyMetadata(0));


        public MeasResults()
        {

            InitializeComponent();
        }
    }
}
