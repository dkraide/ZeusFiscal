using MDFe.Classes.Informacoes;
using MDFe.Classes.Retorno;
using MDFe.Damdfe.Base;
using MDFe.Damdfe.Html.Utils;
using QRCoder;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MDFe.Damdfe.Html
{
    public class DamdfeHtml
    {
        private readonly ConfiguracaoDamdfe configuracaoDamdfe;

        public DamdfeHtml(ConfiguracaoDamdfe configuracao)
        {
            this.configuracaoDamdfe = configuracao;
        }

        public Task<string> ObterDocHtmlAsync(MDFeProcMDFe mdfeProc)
        {
            return Task.Run(() => MontarDamdfe(mdfeProc));
        }



        private string MontarDamdfe(MDFeProcMDFe proc)
        {
            // 🔥 Carrega o template
            string html = Helpers.ReadEmbeddedResource("damdfe_template.html");
            var mdfe = proc.MDFe;
            var inf = mdfe.InfMDFe;
            var emit = inf.Emit;
            var ide = inf.Ide;
            var tot = mdfe.InfMDFe.Tot;
            var rodo = (MDFeRodo)inf.InfModal.Modal;

            // 🔥 Formatar Modal
            string modalDescricao = "Rodoviário de Carga";
            if (ide.Modal == Classes.Flags.MDFeModal.Aereo) modalDescricao = "Aéreo";
            else if (ide.Modal == Classes.Flags.MDFeModal.Aquaviario) modalDescricao = "Aquaviário";
            else if (ide.Modal == Classes.Flags.MDFeModal.Ferroviari) modalDescricao = "Ferroviário";

            // 🔥 Formatar chave com espaçamento
            string chaveFormatada = FormatarChaveAcesso(proc.ProtMDFe?.InfProt?.ChMDFe ?? "");

            // 🔥 Substituir placeholders básicos
            html = html.Replace("#NOMEFANTASIA#", emit.XNome ?? "")
                       .Replace("#CNPJ#", FormatarCNPJ(emit.CNPJ ?? ""))
                       .Replace("#IE#", emit.IE ?? "")
                       .Replace("#ENDERECO#", emit.EnderEmit?.XLgr ?? "")
                       .Replace("#NUMERO#", emit.EnderEmit?.Nro ?? "")
                       .Replace("#BAIRRO#", emit.EnderEmit?.XBairro ?? "")
                       .Replace("#MUNICIPIO#", emit.EnderEmit?.XMun ?? "")
                       .Replace("#UF#", emit.EnderEmit?.UF.ToString() ?? "")
                       .Replace("#CEP#", FormatarCEP(emit.EnderEmit?.CEP.ToString() ?? ""))
                       .Replace("#CHAVE#", chaveFormatada)
                       .Replace("#PROTOCOLO#", proc.ProtMDFe?.InfProt?.NProt ?? "")
                       .Replace("#PROTOCOLO_DATA#", proc.ProtMDFe?.InfProt?.DhRecbto.ToString("dd/MM/yyyy HH:mm:ss") ?? "")
                       .Replace("#MODELO#", ide.Mod.ToString())
                       .Replace("#SERIE#", ide.Serie.ToString())
                       .Replace("#NUMERO_MDFe#", ide.NMDF.ToString("000.000.000"))
                       .Replace("#FL#", "1/1")
                       .Replace("#DATAEMISSAO#", ide.DhEmi.ToString("dd/MM/yyyy - HH:mm:sszzz"))
                       .Replace("#UF_CARREG#", ide.UFIni.ToString() ?? "")
                       .Replace("#UF_DESCARG#", ide.UFFim.ToString() ?? "")
                       .Replace("#MODAL#", modalDescricao)
                       .Replace("#CIOT#", rodo.CIOT ?? "000")
                       .Replace("#QTD_CTE#", (tot.QCTe ?? 0).ToString("000"))
                       .Replace("#QTD_CTRC#", "000")
                       .Replace("#QTD_NFE#", (tot.QNFe ?? 0).ToString("000"))
                       .Replace("#PESO_TOTAL#", (tot.QCarga).ToString("N4"))
                       .Replace("#PLACA#", rodo.VeicTracao?.Placa ?? "")
                       .Replace("#RNTRC#", rodo.RNTRC ?? "")
                       .Replace("#CONDUTOR_CPF#", FormatarCPF(rodo.VeicTracao?.Condutor?[0]?.CPF ?? ""))
                       .Replace("#CONDUTOR_NOME#", rodo.VeicTracao?.Condutor?[0]?.XNome ?? "")
                       .Replace("#DATA_IMPRESSAO#", DateTime.Now.ToString("dd/MM/yyyy"))
                       .Replace("#HORA_IMPRESSAO#", DateTime.Now.ToString("HH:mm:ss"))
                       .Replace("#VERSAO#", "1.0.0");

            // 🔥 Vale Pedágio
            string valeRespCnpj = "";
            string valeFornCnpj = "";
            string valeComprovante = "";

            html = html.Replace("#VALE_RESP_CNPJ#", valeRespCnpj)
                       .Replace("#VALE_FORN_CNPJ#", valeFornCnpj)
                       .Replace("#VALE_COMPROVANTE#", valeComprovante);

            // 🔥 Observações
            html = html.Replace("#OBS#", inf.InfAdic?.InfCpl ?? "");

            // 🔥 Código de Barras
            string chave = proc.ProtMDFe?.InfProt?.ChMDFe ?? "";
            if (!string.IsNullOrEmpty(chave))
            {
                string base64Barras = GerarCodigoBarrasBase64(chave);
                html = html.Replace("#COD_BARRAS#", base64Barras);
            }
            else
            {
                html = html.Replace("#COD_BARRAS#", "");
            }

            // 🔥 QR Code
            string qrCode = proc?.MDFe?.infMDFeSupl?.qrCodMDFe ?? "";
            if (!string.IsNullOrEmpty(qrCode))
            {
                string base64Qr = GerarQrCodeBase64(qrCode);
                html = html.Replace("#QRCODE#", base64Qr);
            }
            else
            {
                html = html.Replace("#QRCODE#", "");
            }

            // 🔥 LOGO
            if (configuracaoDamdfe.Logomarca != null && configuracaoDamdfe.Logomarca.Length > 0)
            {
                string base64 = Convert.ToBase64String(configuracaoDamdfe.Logomarca);
                html = html.Replace("#LOGO#", "data:image/png;base64," + base64);
                html = html.Replace("#LOGO_HIDDEN#", "");
            }
            else
            {
                html = html.Replace("#LOGO#", "");
                html = html.Replace("#LOGO_HIDDEN#", "hidden");
            }
            html = html.Replace("#ENCERRADO#", configuracaoDamdfe.DocumentoEncerrado ? "" : "hidden");
            return html;
        }

        // 🔥 Função para formatar chave de acesso (com espaços)
        private string FormatarChaveAcesso(string chave)
        {
            if (string.IsNullOrEmpty(chave) || chave.Length != 44)
                return chave;

            // Formato: 9999 9999 9999 9999 9999 9999 9999 9999 9999 9999 9999
            return string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10}",
                chave.Substring(0, 4),
                chave.Substring(4, 4),
                chave.Substring(8, 4),
                chave.Substring(12, 4),
                chave.Substring(16, 4),
                chave.Substring(20, 4),
                chave.Substring(24, 4),
                chave.Substring(28, 4),
                chave.Substring(32, 4),
                chave.Substring(36, 4),
                chave.Substring(40, 4));
        }

        // 🔥 Função para formatar CNPJ
        private string FormatarCNPJ(string cnpj)
        {
            if (string.IsNullOrEmpty(cnpj))
                return "";

            cnpj = new string(cnpj.Where(char.IsDigit).ToArray());

            if (cnpj.Length != 14)
                return cnpj;

            return string.Format("{0}.{1}.{2}/{3}-{4}",
                cnpj.Substring(0, 2),
                cnpj.Substring(2, 3),
                cnpj.Substring(5, 3),
                cnpj.Substring(8, 4),
                cnpj.Substring(12, 2));
        }

        // 🔥 Função para formatar CPF
        private string FormatarCPF(string cpf)
        {
            if (string.IsNullOrEmpty(cpf))
                return "";

            cpf = new string(cpf.Where(char.IsDigit).ToArray());

            if (cpf.Length != 11)
                return cpf;

            return string.Format("{0}.{1}.{2}-{3}",
                cpf.Substring(0, 3),
                cpf.Substring(3, 3),
                cpf.Substring(6, 3),
                cpf.Substring(9, 2));
        }

        // 🔥 Função para formatar CEP
        private string FormatarCEP(string cep)
        {
            if (string.IsNullOrEmpty(cep))
                return "";

            cep = new string(cep.Where(char.IsDigit).ToArray());

            if (cep.Length != 8)
                return cep;

            return string.Format("{0}.{1}-{2}",
                cep.Substring(0, 2),
                cep.Substring(2, 3),
                cep.Substring(5, 3));
        }

        // 🔥 Gerar QR Code em Base64
        private string GerarQrCodeBase64(string texto)
        {
            try
            {
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(texto, QRCodeGenerator.ECCLevel.M))
                using (QRCode qrCode = new QRCode(qrCodeData))
                using (Bitmap qrCodeImage = qrCode.GetGraphic(5, Color.Black, Color.White, true))
                using (MemoryStream ms = new MemoryStream())
                {
                    qrCodeImage.Save(ms, ImageFormat.Png);
                    byte[] imageBytes = ms.ToArray();
                    return "data:image/png;base64," + Convert.ToBase64String(imageBytes);
                }
            }
            catch
            {
                return "";
            }
        }

        // 🔥 Gerar Código de Barras em Base64 (Code 128)
        private string GerarCodigoBarrasBase64(string chave)
        {
            try
            {
                // Remove qualquer formatação da chave
                chave = new string(chave.Where(char.IsDigit).ToArray());
                return Helpers.MontarCodigoBarras(chave);
            }
            catch
            {
                return "";
            }
        }
    }
}
