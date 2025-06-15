using System.Net.Http.Json;
using System.Text.Json;
using System.Net;

namespace CLTI.Diagnosis.Client.Services
{
    public class CltiApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public CltiApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<ApiResponse<SaveCaseResponse>> SaveCaseAsync(StateServiceDto stateDto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/clticase/save", stateDto, _jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<SaveCaseResponse>(_jsonOptions);
                    return new ApiResponse<SaveCaseResponse>
                    {
                        Success = true,
                        Data = result,
                        Message = "Дані успішно збережено"
                    };
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new ApiResponse<SaveCaseResponse>
                    {
                        Success = false,
                        Error = "Помилка авторизації"
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new ApiResponse<SaveCaseResponse>
                    {
                        Success = false,
                        Error = $"HTTP {response.StatusCode}: {errorContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<SaveCaseResponse>
                {
                    Success = false,
                    Error = $"Помилка збереження: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<StateServiceDto>> GetCaseAsync(int caseId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/clticase/{caseId}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<StateServiceDto>(_jsonOptions);
                    return new ApiResponse<StateServiceDto>
                    {
                        Success = true,
                        Data = result,
                        Message = "Дані успішно отримано"
                    };
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new ApiResponse<StateServiceDto>
                    {
                        Success = false,
                        Error = "Помилка авторизації"
                    };
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new ApiResponse<StateServiceDto>
                    {
                        Success = false,
                        Error = "Case не знайдено"
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new ApiResponse<StateServiceDto>
                    {
                        Success = false,
                        Error = $"HTTP {response.StatusCode}: {errorContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<StateServiceDto>
                {
                    Success = false,
                    Error = $"Помилка отримання: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<SaveCaseResponse>> UpdateCaseAsync(int caseId, StateServiceDto stateDto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/api/clticase/{caseId}", stateDto, _jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<SaveCaseResponse>(_jsonOptions);
                    return new ApiResponse<SaveCaseResponse>
                    {
                        Success = true,
                        Data = result,
                        Message = "Дані успішно оновлено"
                    };
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new ApiResponse<SaveCaseResponse>
                    {
                        Success = false,
                        Error = "Помилка авторизації"
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new ApiResponse<SaveCaseResponse>
                    {
                        Success = false,
                        Error = $"HTTP {response.StatusCode}: {errorContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<SaveCaseResponse>
                {
                    Success = false,
                    Error = $"Помилка оновлення: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<bool>> DeleteCaseAsync(int caseId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/clticase/{caseId}");

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Case успішно видалено"
                    };
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Error = "Помилка авторизації"
                    };
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Error = "Case не знайдено"
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Error = $"HTTP {response.StatusCode}: {errorContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Error = $"Помилка видалення: {ex.Message}"
                };
            }
        }
    }

    #region DTO Classes (shared with server)

    public class StateServiceDto
    {
        public int? CaseId { get; set; }

        // === Гемодинамічні параметри ===
        public double KpiValue { get; set; } = 0;
        public double PpiValue { get; set; } = 0;
        public string? PsatValue { get; set; }
        public double? TcPO2Value { get; set; }
        public bool HasArterialCalcification { get; set; }
        public bool HasDiabetes { get; set; }

        // === Дані про рани ===
        public bool HasNecrosis { get; set; }
        public string? NecrosisType { get; set; }
        public string? GangreneSpread { get; set; }
        public string? UlcerLocation { get; set; }
        public string? UlcerAffectsBone { get; set; }
        public string? UlcerDepth { get; set; }
        public string? UlcerLocation2 { get; set; }

        // === Дані про інфекцію ===
        public bool HasLocalSwelling { get; set; }
        public bool HasErythema { get; set; }
        public bool HasLocalPain { get; set; }
        public bool HasLocalWarmth { get; set; }
        public bool HasPus { get; set; }
        public bool HasTachycardia { get; set; }
        public bool HasTachypnea { get; set; }
        public bool HasTemperatureChange { get; set; }
        public bool HasLeukocytosis { get; set; }
        public string? SirsAbsentType { get; set; }
        public string? HyperemiaSize { get; set; }

        // === CRAB дані ===
        public bool IsOlderThan75 { get; set; }
        public bool HasPreviousAmputationOrRevascularization { get; set; }
        public bool HasPainAndNecrosis { get; set; }
        public bool HasPartialFunctionalDependence { get; set; }
        public bool IsOnHemodialysis { get; set; }
        public bool HasAnginaOrMI { get; set; }
        public bool IsUrgentOperation { get; set; }
        public bool HasCompleteFunctionalDependence { get; set; }

        // === 2YLE дані ===
        public bool IsNonAmbulatory { get; set; }
        public bool HasRutherford5 { get; set; }
        public bool HasRutherford6 { get; set; }
        public bool HasCerebrovascularDisease { get; set; }
        public bool Has2YLEHemodialysis { get; set; }
        public bool HasBMI18to19 { get; set; }
        public bool HasBMILessThan18 { get; set; }
        public bool IsAge65to79 { get; set; }
        public bool IsAge80Plus { get; set; }
        public bool HasEjectionFraction40to49 { get; set; }
        public bool HasEjectionFractionLessThan40 { get; set; }

        // === GLASS дані ===
        public string? GLASSSelectedStage { get; set; }
        public string? GLASSSubStage { get; set; }
        public string? GLASSFemoroPoplitealStage { get; set; }
        public string? GLASSInfrapoplitealStage { get; set; }
        public string? GLASSFinalStage { get; set; }
        public string? SubmalleolarDescriptor { get; set; }

        // === Стани завершення кроків ===
        public bool IsWCompleted { get; set; }
        public bool IsICompleted { get; set; }
        public bool IsfICompleted { get; set; }
        public bool IsWiFIResultsCompleted { get; set; }
        public bool IsCRABCompleted { get; set; }
        public bool Is2YLECompleted { get; set; }
        public bool IsSurgicalRiskCompleted { get; set; }
        public bool IsGLASSCompleted { get; set; }
        public bool IsGLASSFemoroPoplitealCompleted { get; set; }
        public bool IsGLASSInfrapoplitealCompleted { get; set; }
        public bool IsGLASSFinalCompleted { get; set; }
        public bool IsSubmalleolarDiseaseCompleted { get; set; }
        public bool IsRevascularizationAssessmentCompleted { get; set; }
        public bool IsRevascularizationMethodCompleted { get; set; }

        // === Додаткові поля ===
        public bool CannotSaveLimb { get; set; }

        // === Розраховані значення (тільки для читання) ===
        public int? WLevelValue { get; set; }
        public int? ILevelValue { get; set; }
        public int? FILevelValue { get; set; }
        public int ClinicalStage { get; set; }
        public int? CRABTotalScore { get; set; }
        public double? YLETotalScore { get; set; }
    }

    public class SaveCaseResponse
    {
        public int CaseId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTime SavedAt { get; set; }
    }

    public class ApiResponse<T>
    {
        public T? Data { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

#endregion