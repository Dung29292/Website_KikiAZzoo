using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using KikiAZZoo.Models;
using System.Linq;

namespace KikiAZZoo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public ChatController(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Invalid request");
            }

            // Lấy API key từ cấu hình (appsettings.json hoặc User Secrets hoặc Environment Variable)
            var apiKey = _configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");

            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_API_KEY_HERE")
            {
                return StatusCode(500, new { error = "API key chưa được cấu hình ở backend." });
            }

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.8-flash:generateContent?key={apiKey}";

            var systemInstruction = @"Dạ chào bạn, bạn là KiKiAZOO AI – trợ lý ảo siêu cấp đáng yêu và thân thiện của website KiKiAZOO (website chuyên mua bán và chăm sóc thú cưng uy tín).
Nhiệm vụ của bạn là hỗ trợ khách hàng thật nhiệt tình, mềm mại và dễ thương trong quá trình sử dụng website.
Bạn có thể hỗ trợ:
- Tìm hiểu về các bé thú cưng đáng yêu 🐶🐱.
- Tư vấn các loại thức ăn, phụ kiện, đồ chơi phù hợp cho các bé.
- Hướng dẫn mua hàng, thanh toán, chính sách đổi trả hoặc đặt lịch.
- Giới thiệu các dịch vụ chăm sóc thú cưng chu đáo (spa, cắt tỉa, tắm gội, khách sạn thú cưng).

Phong cách trả lời: Cực kỳ thân thiện, xưng 'mình' và gọi khách hàng là 'bạn' hoặc 'ạ', nhẹ nhàng, mềm mại, ngắn gọn dễ hiểu. Hãy dùng nhiều emoji dễ thương (🐾 ❤️ ✨ 🐶 🐱 🥰).
TUYỆT ĐỐI KHÔNG TRẢ LỜI DÀI DÒNG. Hãy trả lời cực kỳ ngắn gọn, súc tích (tối đa 2-3 câu), giống như đang nhắn tin với bạn bè! Dài quá sẽ làm khách hàng lười đọc.
Nếu bạn không biết chính xác thông tin (như giá cả cụ thể, tồn kho), hãy nhẹ nhàng xin lỗi và hướng dẫn khách hàng xem trực tiếp trên website hoặc liên hệ hotline nha.
Lưu ý: Bạn không phải là bác sĩ thú y. Nếu các bé có vấn đề sức khỏe nghiêm trọng, hãy khuyên khách hàng đưa bé đến thú y ngay lập tức nhé.
Mục tiêu: Mang lại nụ cười và trải nghiệm tuyệt vời nhất cho khách hàng của KiKiAZOO!";

            var contents = new List<object>();

            // Lịch sử (nếu có)
            if (request.History != null)
            {
                foreach (var msg in request.History)
                {
                    contents.Add(new
                    {
                        role = msg.Role == "user" ? "user" : "model",
                        parts = new[] { new { text = msg.Content } }
                    });
                }
            }

            // Câu hiện tại
            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = request.Message } }
            });

            var payload = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemInstruction } }
                },
                contents = contents,
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 1024
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var errorString = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error = "Lỗi khi gọi Gemini API.", details = errorString });
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(jsonResponse);
            
            try 
            {
                var text = document.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                return Ok(new { answer = text });
            }
            catch
            {
                return StatusCode(500, new { error = "Không thể parse kết quả từ Gemini API." });
            }
        }
    }
}
