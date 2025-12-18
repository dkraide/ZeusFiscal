using NetBarcode;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;


namespace MDFe.Damdfe.Html.Utils
{
    public class Helpers
    {

      

        public static string MontarCodigoBarras(string nFeId)
        {
            return "data:image/png;base64, " + new Barcode(nFeId, NetBarcode.Type.Code128, false, 600, 30).GetBase64Image();
        }
        public static string ReadEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string fullName = $"{assembly.GetName().Name}.Resources.{resourceName}";

            using (var stream = assembly.GetManifestResourceStream(fullName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

    }
}
