using Microsoft.AspNetCore.Mvc;
using CLTI.Diagnosis.Services;
using CLTI.Diagnosis.Client.Algoritm.Services;

namespace CLTI.Diagnosis.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CltiCaseController : ControllerBase
    {
        private readonly CltiCaseService _cltiCaseService;

        public CltiCaseController(CltiCaseService cltiCaseService)
        {
            _cltiCaseService = cltiCaseService;
        }


        [HttpPost("save")]
        public async Task<IActionResult> SaveCase([FromBody] StateServiceDto stateData)
        {
            try
            {
                var caseId = await _cltiCaseService.SaveCaseAsync(stateData);
                return Ok(new { CaseId = caseId, Message = "Дані успішно збережено" });
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
                return Ok(new { CaseId = updatedCaseId, Message = "Дані успішно оновлено" });
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
    }
}