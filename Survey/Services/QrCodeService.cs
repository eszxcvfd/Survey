using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace Survey.Services
{
    public class QrCodeService : IQrCodeService
    {
        private readonly string _qrCodeFolder = Path.Combine("wwwroot", "qrcodes");

        public async Task<string> GenerateAndSaveAsync(string url)
        {
            // Ensure folder exists
            if (!Directory.Exists(_qrCodeFolder))
            {
                Directory.CreateDirectory(_qrCodeFolder);
            }

            // Generate QR code
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrCodeData);
            using var qrCodeImage = qrCode.GetGraphic(20);

            // Save to file
            var fileName = $"{Guid.NewGuid()}.png";
            var filePath = Path.Combine(_qrCodeFolder, fileName);
            
            await Task.Run(() => qrCodeImage.Save(filePath, ImageFormat.Png));

            // Return relative path
            return $"/qrcodes/{fileName}";
        }
    }
}