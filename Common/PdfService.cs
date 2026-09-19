using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace SkillHive.Common
{
    public class CertificatePdfData
    {
        public string StudentName { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public string AcademyName { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;
        public string VerificationUrl { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
    }

    public class PdfService
    {
        public byte[] GenerateCertificate(CertificatePdfData data)
        {
            var qrBytes = GenerateQrCode(data.VerificationUrl);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                  page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Lato"));

                    page.Content().Border(3).BorderColor(Colors.Grey.Darken2).Padding(30).Column(col =>
                    {
                        col.Spacing(15);

                        // Header
                        col.Item().AlignCenter().Text(data.AcademyName)
                            .FontSize(22).SemiBold().FontColor(Colors.Grey.Darken3);

                        col.Item().AlignCenter().Text("Certificate of Completion")
                            .FontSize(36).Bold().FontColor(Colors.Grey.Darken4);

                        col.Item().PaddingTop(20).AlignCenter().Text("This certificate is proudly presented to")
                            .FontSize(14).FontColor(Colors.Grey.Darken1);

                        // Student name
                        col.Item().AlignCenter().Text(data.StudentName)
                            .FontSize(32).Bold().FontColor(Colors.Black);

                        col.Item().AlignCenter().Text("for successfully completing")
                            .FontSize(14).FontColor(Colors.Grey.Darken1);

                        // Course
                        col.Item().AlignCenter().Text(data.CourseTitle)
                            .FontSize(22).SemiBold().FontColor(Colors.Grey.Darken3);

                        // Footer row: date on left, QR on right
                        col.Item().PaddingTop(30).Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text($"Issued: {data.IssuedAt:MMMM dd, yyyy}")
                                    .FontSize(12).FontColor(Colors.Grey.Darken1);
                                left.Item().Text($"Verification: {data.VerificationCode}")
                                    .FontSize(12).FontColor(Colors.Grey.Darken1);
                                left.Item().PaddingTop(10).Text("Verify at:")
                                    .FontSize(10).FontColor(Colors.Grey.Darken1);
                                left.Item().Text(data.VerificationUrl)
                                    .FontSize(10).FontColor(Colors.Blue.Medium);
                            });

                            row.ConstantItem(120).AlignRight().Image(qrBytes).FitWidth();
                        });
                    });
                });
            }).GeneratePdf();
        }

        private byte[] GenerateQrCode(string url)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);
            return qrCode.GetGraphic(10);
        }
    }
}