using UglyToad.PdfPig;
using sistema_gestao_recursos_humanos.backend.models.tools;

namespace sistema_gestao_recursos_humanos.backend.models.tools
{
    public static class PdfTextExtractor
    {
        public static string ExtractAllText(Stream pdfStream)
        {
            var tmpFile = Path.GetTempFileName();
            using (var fs = File.Create(tmpFile))
            {
                pdfStream.CopyTo(fs);
            }

            var sb = new System.Text.StringBuilder();
            using (var doc = PdfDocument.Open(tmpFile))
            {
                foreach (var page in doc.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
            }

            File.Delete(tmpFile);
            return sb.ToString();
        }
    }
}
