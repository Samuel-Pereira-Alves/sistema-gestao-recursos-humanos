using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto.Engines;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _mailer;
    public EmailController(IEmailService mailer) => _mailer = mailer;

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] EmailRequestDTO req)
    {
        if (string.IsNullOrWhiteSpace(req.To) || string.IsNullOrWhiteSpace(req.Subject))
            return BadRequest(new { ok = false, error = "Campos 'to' e 'subject' são obrigatórios." });

        try
        {



var html = $@"
<div style='max-width:680px;margin:0 auto;padding:0;font-family:Segoe UI,Arial,Helvetica,sans-serif;background:#f4f6f9;color:#333;'>
  <div style='margin:20px;background:#fff;border:1px solid #e0e6ed;border-radius:12px;box-shadow:0 4px 16px rgba(0,0,0,0.08);overflow:hidden;'>

    <!-- Cabeçalho -->
    <div style='background:#0A6ED1;color:#fff;padding:24px;border-bottom:4px solid #095db7;text-align:center;'>
      <h1 style='margin:0;font-size:22px;font-weight:600;'>Sistema de Recursos Humanos</h1>
    </div>

    <!-- Conteúdo -->
    <div style='padding:28px;text-align:center;'>
      <p style='font-size:16px;line-height:1.6;color:#444;margin-bottom:24px;'>
        {req.Text}
      </p>

    </div>

    <!-- Separador -->
    <hr style='border:none;border-top:1px solid #e0e6ed;margin:24px 0;'>

    <!-- Rodapé -->
    <p style='margin:0;font-size:12px;color:#6b7280;text-align:center;padding:12px;'>
      Este é um email automático. Por favor, não responda.
    </p>
  </div>
</div>";




            
var result = await _mailer.SendAsync(
    to: req.To,
    subject: req.Subject,
    textBody: html
);


            return Ok(new { ok = true, result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, error = ex.Message });
        }
    }

}
