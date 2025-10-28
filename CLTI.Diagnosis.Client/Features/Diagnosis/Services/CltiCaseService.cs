using CLTI.Diagnosis.Client.Infrastructure.Auth;
using CLTI.Diagnosis.Client.Infrastructure.Http;
using CLTI.Diagnosis.Client.Infrastructure.State;
using CLTI.Diagnosis.Client.Features.Diagnosis.Services;
using CLTI.Diagnosis.Client.Extensions;

namespace CLTI.Diagnosis.Client.Features.Diagnosis.Services
{
    public class CltiCaseService
    {
        private readonly CltiApiClient _apiClient;

        public CltiCaseService(CltiApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<bool> SaveCaseAsync(StateService stateService)
        {
            try
            {
                var dto = stateService.ToDto();

                var response = await _apiClient.SaveCaseAsync(dto);

                if (response.Success && response.Data != null)
                {
                    stateService.CaseId = response.Data.CaseId;
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> LoadCaseAsync(int caseId, StateService stateService)
        {
            try
            {
                var response = await _apiClient.GetCaseAsync(caseId);

                if (response.Success && response.Data != null)
                {
                    stateService.UpdateFromDto(response.Data);
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteCaseAsync(int caseId)
        {
            try
            {
                var response = await _apiClient.DeleteCaseAsync(caseId);
                return response.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}