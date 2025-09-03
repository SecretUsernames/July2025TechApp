using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace July2025Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly HttpClient _http;

        public ChatController(IHttpClientFactory factory)
        {
            // Must match the named client you registered in Program.cs
            _http = factory.CreateClient("OpenAI");
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request, CancellationToken ct)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Question))
                return BadRequest(new { error = "empty_question" });

            try
            {
                var body = new
                {
                    model = "gpt-4o-mini",
                    messages = new[]
                    {
                        new { role = "system", content = "You are MedVault Assistant." },
                        new { role = "user", content = request.Question }
                    }
                };

                var resp = await _http.PostAsJsonAsync("chat/completions", body, ct);
                var raw = await resp.Content.ReadAsStringAsync(ct);

                if (!resp.IsSuccessStatusCode)
                {
                    // Surface details so your front end shows *why* it failed
                    return StatusCode((int)resp.StatusCode, new
                    {
                        error = "openai_request_failed",
                        status = (int)resp.StatusCode,
                        raw
                    });
                }

                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                // choices[0].message.content
                var answer = root.GetProperty("choices")[0]
                                 .GetProperty("message")
                                 .GetProperty("content")
                                 .GetString();

                if (string.IsNullOrWhiteSpace(answer))
                    return StatusCode(502, new { error = "empty_response", raw });

                return Ok(new { Answer = answer });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { error = "http_request_exception", message = ex.Message });
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, new { error = "openai_timeout" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "unexpected", message = ex.Message });
            }
        }
    }

    public class ChatRequest
    {
        public string Question { get; set; } = string.Empty;
    }
}
