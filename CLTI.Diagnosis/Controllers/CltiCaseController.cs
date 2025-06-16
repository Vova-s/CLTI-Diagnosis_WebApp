
// ✅ CltiCaseController.cs - Тепер використовує JWT
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CLTI.Diagnosis.Services;

namespace CLTI.Diagnosis.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // ✅ Тепер використовує JWT за замовчуванням
    public class CltiCaseController : ControllerBase
    {
        private readonly CLTI.Diagnosis.Services.CltiCaseService _cltiCaseService;

        public CltiCaseController(CLTI.Diagnosis.Services.CltiCaseService cltiCaseService)
        {
            _cltiCaseService = cltiCaseService;
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveCase([FromBody] StateServiceDto stateData)
        {
            try
            {
                var caseId = await _cltiCaseService.SaveCaseAsync(stateData);
                return Ok(new { CaseId = caseId, Message = "Дані успішно збережено", Success = true, SavedAt = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Помилка збереження даних", Details = ex.Message });
            }
        }

        [HttpGet("{caseId}")]
        public async Task<IActionResult> GetCase(int caseId)
        {
            try
            {
                var caseData = await _cltiCaseService.GetCaseAsync(caseId);
                if (caseData == null)
                {
                    return NotFound(new { Error = "Case не знайдено" });
                }

                return Ok(caseData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Помилка отримання даних", Details = ex.Message });
            }
        }

        [HttpPut("{caseId}")]
        public async Task<IActionResult> UpdateCase(int caseId, [FromBody] StateServiceDto stateData)
        {
            try
            {
                stateData.CaseId = caseId;
                var updatedCaseId = await _cltiCaseService.SaveCaseAsync(stateData);
                return Ok(new { CaseId = updatedCaseId, Message = "Дані успішно оновлено", Success = true, SavedAt = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Помилка оновлення даних", Details = ex.Message });
            }
        }

        [HttpDelete("{caseId}")]
        public async Task<IActionResult> DeleteCase(int caseId)
        {
            try
            {
                var success = await _cltiCaseService.DeleteCaseAsync(caseId);
                if (!success)
                {
                    return NotFound(new { Error = "Case не знайдено" });
                }

                return Ok(new { Message = "Case успішно видалено" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Помилка видалення даних", Details = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCases()
        {
            try
            {
                var cases = await _cltiCaseService.GetAllCasesAsync();
                return Ok(cases);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Помилка отримання списку cases", Details = ex.Message });
            }
        }
    }
}