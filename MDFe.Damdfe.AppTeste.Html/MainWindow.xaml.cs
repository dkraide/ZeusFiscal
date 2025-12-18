using DFe.Utils;
using MDFe.Classes.Retorno;
using MDFe.Damdfe.Base;
using MDFe.Damdfe.Html;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
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

namespace MDFe.Damdfe.AppTeste.Html
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private MDFeProcMDFe GetMDFeProcMDFe()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Selecione o arquivo MDF-e (procMDFe)",
                Filter = "Arquivos XML (*.xml)|*.xml",
                Multiselect = false
            };

            bool? result = dialog.ShowDialog();

            if (result != true)
                return null;

            string xml = File.ReadAllText(dialog.FileName);

            MDFeProcMDFe proc = FuncoesXml.XmlStringParaClasse<MDFeProcMDFe>(xml);

            return proc;
        }
        private async void btnImprimir_Click(object sender, RoutedEventArgs e)
        {
            var mdfe = GetMDFeProcMDFe();
            if (mdfe == null)
                return;

            ConfiguracaoDamdfe config = new ConfiguracaoDamdfe
            {
                DocumentoCancelado = chbCancelado.IsChecked == true,
                QuebrarLinhasObservacao = chbQuebrarLinhaObservacao.IsChecked == true,
                DocumentoEncerrado = chbEncerrado.IsChecked == true,
                Logomarca = imgLogoPreview.Source != null ? ImageToBytes(imgLogoPreview.Source) : null
            };

            DamdfeHtml damdfeHtml = new DamdfeHtml(config);

            var html = await damdfeHtml.ObterDocHtmlAsync(mdfe);
            string pasta = AppDomain.CurrentDomain.BaseDirectory;
            string arquivo = System.IO.Path.Combine(pasta, "damdfe_gerado.html");

            File.WriteAllText(arquivo, html, Encoding.UTF8);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = arquivo,
                UseShellExecute = true
            });

            MessageBox.Show("DAMDFE gerado com sucesso!", "OK");
        }

        private void btnSelecionarLogo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Selecione um logotipo",
                Filter = "Imagens (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (dlg.ShowDialog() == true)
            {
                BitmapImage bitmap = new BitmapImage(new Uri(dlg.FileName));
                imgLogoPreview.Source = bitmap;
            }
        }

        private byte[] ImageToBytes(ImageSource imgSrc)
        {
            if (imgSrc == null) return null;

            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = 100;

            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)imgSrc));

            using MemoryStream ms = new MemoryStream();
            encoder.Save(ms);
            return ms.ToArray();
        }
    }
}