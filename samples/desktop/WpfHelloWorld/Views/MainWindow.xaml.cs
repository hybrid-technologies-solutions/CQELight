using CQELight.Abstractions.IoC.Interfaces;
using CQELight.IoC;
using CQELight.MVVM.Interfaces;
using CQELight.MVVM.MahApps;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfHelloWorld.ViewModels;

namespace WpfHelloWorld
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : CQEMetroWindow, IAutoRegisterType
    {
        public MainWindow(IScope scope)
        {
            InitializeComponent();
            DataContext = scope.Resolve<MainWindowViewModel>(new TypeResolverParameter(typeof(IView), this));
        }
    }
}
