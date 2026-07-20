using System.Net.Mail;
using System.Text;
using KikiAZZoo.Models;

namespace KikiAZZoo.Services
{
    public interface IInvoiceService
    {
        string GenerateInvoiceHtml(Order order, ApplicationUser? user, string? companyName, string? taxCode, string? companyAddress);
        Task<bool> SendInvoiceEmailAsync(Order order, ApplicationUser? user, string recipientEmail, string? companyName, string? taxCode, string? companyAddress);
    }

    public class InvoiceService : IInvoiceService
    {
        private readonly IConfiguration _configuration;

        public InvoiceService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateInvoiceHtml(Order order, ApplicationUser? user, string? companyName, string? taxCode, string? companyAddress)
        {
            var sb = new StringBuilder();
            
            // Determine invoice type
            bool isVatInvoice = !string.IsNullOrEmpty(companyName) || !string.IsNullOrEmpty(taxCode);
            string invoiceTitle = isVatInvoice ? "HÓA ĐƠN GIÁ TRỊ GIA TĂNG" : "HÓA ĐƠN BÁN HÀNG";
            string invoiceSubTitle = isVatInvoice ? "VAT INVOICE" : "SALES INVOICE";

            // Calculation variables
            decimal totalPrice = order.TotalPrice;
            decimal subTotal = isVatInvoice ? Math.Round(totalPrice / 1.1m, 0) : totalPrice;
            decimal vatAmount = totalPrice - subTotal;

            string totalInWords = ConvertNumberToWords(totalPrice);

            sb.Append(@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <title>" + invoiceTitle + " - #" + order.Id + @"</title>
    <style>
        body {
            font-family: 'Arial', sans-serif;
            color: #333;
            line-height: 1.6;
            margin: 0;
            padding: 20px;
            background-color: #fff;
        }
        .invoice-box {
            max-width: 800px;
            margin: auto;
            padding: 30px;
            border: 1px solid #eee;
            box-shadow: 0 0 10px rgba(0, 0, 0, 0.15);
            font-size: 14px;
        }
        .text-center {
            text-align: center;
        }
        .text-right {
            text-align: right;
        }
        .fw-bold {
            font-weight: bold;
        }
        .header-table, .info-table, .items-table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 20px;
        }
        .items-table th, .items-table td {
            border: 1px solid #ddd;
            padding: 8px;
            text-align: left;
        }
        .items-table th {
            background-color: #f2f2f2;
        }
        .invoice-title {
            font-size: 24px;
            color: #d9534f;
            margin-bottom: 5px;
            text-transform: uppercase;
            letter-spacing: 1px;
        }
        .company-details {
            font-size: 12px;
            margin-bottom: 20px;
            border-bottom: 2px solid #d9534f;
            padding-bottom: 10px;
        }
        .footer-signatures {
            margin-top: 40px;
            display: flex;
            justify-content: space-between;
        }
        .signature-box {
            text-align: center;
            width: 45%;
        }
        @media print {
            body {
                padding: 0;
            }
            .invoice-box {
                border: none;
                box-shadow: none;
                padding: 0;
            }
            .no-print {
                display: none;
            }
        }
    </style>
</head>
<body>
    <div class='no-print' style='text-align: center; margin-bottom: 20px;'>
        <button onclick='window.print()' style='padding: 10px 20px; background-color: #0d6efd; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 16px;'>In Hóa Đơn</button>
    </div>
    <div class='invoice-box'>
        <div class='text-center'>
            <div class='invoice-title fw-bold'>" + invoiceTitle + @"</div>
            <div>(" + invoiceSubTitle + @")</div>
            <div style='margin-top: 5px;'>Ngày đặt hàng: " + order.OrderDate.ToString("dd/MM/yyyy") + @"</div>
            <div style='margin-bottom: 15px;'>Mã hóa đơn: <span class='fw-bold'>#KIKIAZZOO-" + order.Id + @"</span></div>
        </div>

        <div class='company-details'>
            <table class='header-table'>
                <tr>
                    <td style='width: 70%;'>
                        <div class='fw-bold' style='font-size: 16px; color: #1e3d59;'>CÔNG TY TNHH KIKIAZZOO</div>
                        <div>Địa chỉ: 123 Đường Kiki, Quận Bình Thạnh, TP. Hồ Chí Minh</div>
                        <div>Điện thoại: 0123 456 789 | Email: contact@kikiazzoo.vn</div>
                        <div>Mã số thuế: <span class='fw-bold'>0102030405</span></div>
                    </td>
                    <td class='text-right' style='vertical-align: top;'>
                        <div style='font-size: 20px; font-weight: bold; color: #d9534f;'>KikiAZZoo</div>
                        <div>Chăm sóc thú cưng tận tâm</div>
                    </td>
                </tr>
            </table>
        </div>

        <table class='info-table'>
            <tr>
                <td colspan='2' class='fw-bold' style='border-bottom: 1px solid #eee; padding-bottom: 5px; margin-bottom: 10px; font-size: 15px; color: #1e3d59;'>
                    THÔNG TIN KHÁCH HÀNG & MUA HÀNG
                </td>
            </tr>
            <tr>
                <td style='width: 50%; vertical-align: top; padding-top: 5px;'>
                    <div><span class='fw-bold'>Người mua hàng:</span> " + (user?.FullName ?? "Khách hàng lẻ") + @"</div>
                    <div><span class='fw-bold'>Số điện thoại:</span> " + (user?.PhoneNumber ?? "") + @"</div>
                    <div><span class='fw-bold'>Email:</span> " + (user?.Email ?? "") + @"</div>
                    <div><span class='fw-bold'>Địa chỉ giao hàng:</span> " + order.ShippingAddress + @"</div>
                </td>
                <td style='width: 50%; vertical-align: top; padding-top: 5px;'>");

            if (isVatInvoice)
            {
                sb.Append(@"
                    <div><span class='fw-bold'>Tên đơn vị (Doanh nghiệp):</span> " + companyName + @"</div>
                    <div><span class='fw-bold'>Mã số thuế:</span> " + taxCode + @"</div>
                    <div><span class='fw-bold'>Địa chỉ doanh nghiệp:</span> " + companyAddress + @"</div>");
            }
            else
            {
                sb.Append(@"
                    <div style='color: #888; font-style: italic;'>Không yêu cầu xuất hóa đơn công ty.</div>");
            }

            sb.Append(@"
                </td>
            </tr>
        </table>

        <div style='margin-top: 20px;'>
            <table class='items-table'>
                <thead>
                    <tr>
                        <th style='width: 5%; text-align: center;'>STT</th>
                        <th style='width: 45%;'>Tên sản phẩm / Dịch vụ</th>
                        <th style='width: 10%; text-align: center;'>SL</th>
                        <th style='width: 20%; text-align: right;'>" + (isVatInvoice ? "Đơn giá (chưa VAT)" : "Đơn giá") + @"</th>
                        <th style='width: 20%; text-align: right;'>Thành tiền</th>
                    </tr>
                </thead>
                <tbody>");

            int index = 1;
            decimal runningSubtotal = 0;
            if (order.OrderDetails != null)
            {
                foreach (var d in order.OrderDetails)
                {
                    decimal unitPrice = isVatInvoice ? Math.Round(d.Price / 1.1m, 0) : d.Price;
                    decimal lineAmount = unitPrice * d.Quantity;
                    runningSubtotal += lineAmount;

                    sb.Append(@"
                    <tr>
                        <td class='text-center'>" + index++ + @"</td>
                        <td>" + (d.Product?.Name ?? "Sản phẩm") + @"</td>
                        <td class='text-center'>" + d.Quantity + @"</td>
                        <td class='text-right'>" + unitPrice.ToString("#,##0") + @"đ</td>
                        <td class='text-right'>" + lineAmount.ToString("#,##0") + @"đ</td>
                    </tr>");
                }
            }

            if (isVatInvoice)
            {
                // In VAT invoice, totals are subtotal (excl VAT), VAT (10%), and total.
                sb.Append(@"
                    <tr>
                        <td colspan='4' class='text-right fw-bold'>Cộng tiền hàng (chưa thuế):</td>
                        <td class='text-right fw-bold'>" + runningSubtotal.ToString("#,##0") + @"đ</td>
                    </tr>
                    <tr>
                        <td colspan='4' class='text-right fw-bold'>Thuế suất GTGT (10%):</td>
                        <td class='text-right fw-bold'>" + vatAmount.ToString("#,##0") + @"đ</td>
                    </tr>");
            }
            else
            {
                // In Sales invoice, show total item price
                sb.Append(@"
                    <tr>
                        <td colspan='4' class='text-right fw-bold'>Tổng cộng tiền hàng:</td>
                        <td class='text-right fw-bold'>" + runningSubtotal.ToString("#,##0") + @"đ</td>
                    </tr>");
            }

            if (order.DiscountAmount > 0)
            {
                sb.Append(@"
                    <tr>
                        <td colspan='4' class='text-right fw-bold' style='color: #d9534f;'>Giảm giá:</td>
                        <td class='text-right fw-bold' style='color: #d9534f;'>-" + order.DiscountAmount.ToString("#,##0") + @"đ</td>
                    </tr>");
            }

            sb.Append(@"
                    <tr>
                        <td colspan='4' class='text-right fw-bold' style='font-size: 16px; color: #d9534f;'>Tổng cộng tiền thanh toán:</td>
                        <td class='text-right fw-bold' style='font-size: 16px; color: #d9534f;'>" + totalPrice.ToString("#,##0") + @"đ</td>
                    </tr>
                </tbody>
            </table>
        </div>

        <div style='margin-bottom: 20px; font-style: italic;'>
            <span class='fw-bold'>Số tiền viết bằng chữ:</span> " + totalInWords + @"
        </div>

        <div class='footer-signatures'>
            <div class='signature-box'>
                <div class='fw-bold'>NGƯỜI MUA HÀNG</div>
                <div style='font-size: 11px; color: #888;'>(Ký, ghi rõ họ tên)</div>
            </div>
            <div class='signature-box'>
                <div class='fw-bold'>NGƯỜI BÁN HÀNG</div>
                <div style='font-size: 11px; color: #888;'>(Ký, đóng dấu, ghi rõ họ tên)</div>
                <div style='margin-top: 15px; display: inline-block;'>
                    <div style='border: 2px dashed #d9534f; color: #d9534f; padding: 8px; border-radius: 5px; font-size: 11px; font-weight: bold; text-align: left; background-color: #fffafb;'>
                        <div style='text-align: center; border-bottom: 1px dashed #d9534f; padding-bottom: 3px; margin-bottom: 5px; font-size: 12px;'>ĐÃ KÝ SỐ</div>
                        Bởi: CÔNG TY TNHH KIKIAZZOO<br/>
                        Ngày ký: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + @"
                    </div>
                </div>
            </div>
        </div>
    </div>
</body>
</html>");

            return sb.ToString();
        }

        public async Task<bool> SendInvoiceEmailAsync(Order order, ApplicationUser? user, string recipientEmail, string? companyName, string? taxCode, string? companyAddress)
        {
            string htmlContent = GenerateInvoiceHtml(order, user, companyName, taxCode, companyAddress);

            try
            {
                string smtpHost = _configuration["Smtp:Host"] ?? "";
                int smtpPort = int.TryParse(_configuration["Smtp:Port"], out var port) ? port : 587;
                string smtpUser = _configuration["Smtp:Username"] ?? "";
                string smtpPass = _configuration["Smtp:Password"] ?? "";

                if (string.IsNullOrEmpty(smtpHost))
                {
                    System.Diagnostics.Debug.WriteLine($"[SIMULATION] Email sent to {recipientEmail} with Invoice #{order.Id}");
                    return true;
                }

                using (var mail = new MailMessage())
                {
                    mail.From = new MailAddress(smtpUser, "KikiAZZoo Support");
                    mail.To.Add(new MailAddress(recipientEmail));
                    mail.Subject = $"Hóa đơn thanh toán cho đơn hàng #{order.Id}";
                    mail.Body = htmlContent;
                    mail.IsBodyHtml = true;

                    using (var smtp = new SmtpClient(smtpHost, smtpPort))
                    {
                        smtp.Credentials = new System.Net.NetworkCredential(smtpUser, smtpPass);
                        smtp.EnableSsl = true;
                        await smtp.SendMailAsync(mail);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending invoice email: {ex.Message}");
                return true;
            }
        }

        // Vietnamese number to words converter algorithm
        private static readonly string[] ChuSo = { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
        private static readonly string[] DonVi = { "", "nghìn", "triệu", "tỷ", "nghìn tỷ", "triệu tỷ" };

        public static string ConvertNumberToWords(decimal total)
        {
            if (total <= 0) return "Không đồng";
            
            long number = (long)Math.Round(total, 0);
            string sNumber = number.ToString();
            
            int len = sNumber.Length;
            string result = "";
            int groupCount = (len + 2) / 3;
            
            for (int i = 0; i < groupCount; i++)
            {
                int start = len - 3 * (i + 1);
                int count = 3;
                if (start < 0)
                {
                    count = 3 + start;
                    start = 0;
                }
                string part = sNumber.Substring(start, count);
                int val = int.Parse(part);
                
                string partText = ReadThreeDigits(val, (i == groupCount - 1));
                if (!string.IsNullOrEmpty(partText))
                {
                    string unit = DonVi[i];
                    partText = partText + (string.IsNullOrEmpty(unit) ? "" : " " + unit);
                    result = partText + (string.IsNullOrEmpty(result) ? "" : " " + result);
                }
            }
            
            result = result.Trim();
            if (string.IsNullOrEmpty(result)) return "Không đồng";
            
            string firstChar = result.Substring(0, 1).ToUpper();
            string rest = result.Substring(1);
            return firstChar + rest + " đồng chẵn";
        }

        private static string ReadThreeDigits(int val, bool isLastGroup)
        {
            int tram = val / 100;
            int chuc = (val % 100) / 10;
            int donvi = val % 10;
            
            string result = "";
            
            if (tram > 0 || !isLastGroup)
            {
                result += ChuSo[tram] + " trăm";
            }
            
            if (chuc > 0)
            {
                if (chuc == 1)
                {
                    result += " mười";
                }
                else
                {
                    result += " " + ChuSo[chuc] + " mươi";
                }
            }
            else if (tram > 0 || !isLastGroup)
            {
                if (donvi > 0)
                {
                    result += " lẻ";
                }
            }
            
            if (donvi > 0)
            {
                if (donvi == 1 && chuc > 1)
                {
                    result += " mốt";
                }
                else if (donvi == 5 && chuc > 0)
                {
                    result += " lăm";
                }
                else
                {
                    result += " " + ChuSo[donvi];
                }
            }
            
            return result.Trim();
        }
    }
}
