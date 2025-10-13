
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
        private readonly ILogger<CltiCaseController> _logger;

        public CltiCaseController(
            CLTI.Diagnosis.Services.CltiCaseService cltiCaseService,
            ILogger<CltiCaseController> logger)
        {
            _cltiCaseService = cltiCaseService;
            _logger = logger;
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveCase([FromBody] StateServiceDto stateData)
        {
            try
            {
                _logger.LogInformation("SaveCase called by user {UserId} from {RemoteIp}", 
                    User.Identity?.Name, HttpContext.Connection.RemoteIpAddress);
                
                if (stateData == null)
                {
                    _logger.LogWarning("SaveCase called with null stateData");
                    return BadRequest(new { Error = "Дані не можуть бути порожніми", Success = false });
                }

                _logger.LogDebug("Saving case with data: KPI={KPI}, PPI={PPI}, WLevel={WLevel}", 
                    stateData.KpiValue, stateData.PpiValue, stateData.WLevelValue);

                var caseId = await _cltiCaseService.SaveCaseAsync(stateData);
                
                _logger.LogInformation("Case saved successfully with ID {CaseId} by user {UserId}", 
                    caseId, User.Identity?.Name);
                
                return Ok(new { CaseId = caseId, Message = "Дані успішно збережено", Success = true, SavedAt = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving case for user {UserId}. Error: {ErrorMessage}", 
                    User.Identity?.Name, ex.Message);
                return StatusCode(500, new { Error = "Помилка збереження даних", Details = ex.Message, Success = false });
            }
        }

        [HttpGet("{caseId}")]
        public async Task<IActionResult> GetCase(int caseId)
        {
            try
            {
                _logger.LogInformation("GetCase called for CaseId {CaseId} by user {UserId}", 
                    caseId, User.Identity?.Name);

                var caseData = await _cltiCaseService.GetCaseAsync(caseId);
                if (caseData == null)
                {
                    _logger.LogWarning("Case {CaseId} not found for user {UserId}", 
                        caseId, User.Identity?.Name);
                    return NotFound(new { Error = "Case не знайдено", CaseId = caseId });
                }

                _logger.LogInformation("Case {CaseId} retrieved successfully", caseId);
                return Ok(caseData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving case {CaseId} for user {UserId}. Error: {ErrorMessage}", 
                    caseId, User.Identity?.Name, ex.Message);
                return StatusCode(500, new { Error = "Помилка отримання даних", Details = ex.Message });
            }
        }

        [HttpPut("{caseId}")]
        public async Task<IActionResult> UpdateCase(int caseId, [FromBody] StateServiceDto stateData)
        {
            try
            {
                _logger.LogInformation("UpdateCase called for CaseId {CaseId} by user {UserId}", 
                    caseId, User.Identity?.Name);

                if (stateData == null)
                {
                    _logger.LogWarning("UpdateCase called with null stateData for CaseId {CaseId}", caseId);
                    return BadRequest(new { Error = "Дані не можуть бути порожніми", Success = false });
                }

                stateData.CaseId = caseId;
                
                _logger.LogDebug("Updating case {CaseId} with data: KPI={KPI}, PPI={PPI}", 
                    caseId, stateData.KpiValue, stateData.PpiValue);

                var updatedCaseId = await _cltiCaseService.SaveCaseAsync(stateData);
                
                _logger.LogInformation("Case {CaseId} updated successfully by user {UserId}", 
                    caseId, User.Identity?.Name);
                
                return Ok(new { CaseId = updatedCaseId, Message = "Дані успішно оновлено", Success = true, SavedAt = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating case {CaseId} for user {UserId}. Error: {ErrorMessage}", 
                    caseId, User.Identity?.Name, ex.Message);
                return StatusCode(500, new { Error = "Помилка оновлення даних", Details = ex.Message });
            }
        }

        [HttpDelete("{caseId}")]
        public async Task<IActionResult> DeleteCase(int caseId)
        {
            try
            {
                _logger.LogInformation("DeleteCase called for CaseId {CaseId} by user {UserId}", 
                    caseId, User.Identity?.Name);

                var success = await _cltiCaseService.DeleteCaseAsync(caseId);
                if (!success)
                {
                    _logger.LogWarning("Case {CaseId} not found for deletion by user {UserId}", 
                        caseId, User.Identity?.Name);
                    return NotFound(new { Error = "Case не знайдено", CaseId = caseId });
                }

                _logger.LogInformation("Case {CaseId} deleted successfully by user {UserId}", 
                    caseId, User.Identity?.Name);
                
                return Ok(new { Message = "Case успішно видалено", Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting case {CaseId} for user {UserId}. Error: {ErrorMessage}", 
                    caseId, User.Identity?.Name, ex.Message);
                return StatusCode(500, new { Error = "Помилка видалення даних", Details = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCases()
        {
            try
            {
                _logger.LogInformation("GetAllCases called by user {UserId}", User.Identity?.Name);

                var cases = await _cltiCaseService.GetAllCasesAsync();
                
                _logger.LogInformation("Retrieved {CaseCount} cases for user {UserId}", 
                    cases?.Count() ?? 0, User.Identity?.Name);
                
                return Ok(cases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all cases for user {UserId}. Error: {ErrorMessage}", 
                    User.Identity?.Name, ex.Message);
                return StatusCode(500, new { Error = "Помилка отримання списку cases", Details = ex.Message });
            }
        }
    }
}
