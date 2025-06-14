using CLTI.Diagnosis.Client.Algoritm.Services;
using CLTI.Diagnosis.Client.Extensions;

namespace CLTI.Diagnosis.Client.Services
{
    /// <summary>
    /// Client-side service for CLTI case operations
    /// </summary>
    public class CltiCaseService
    {
        private readonly CltiApiClient _apiClient;

        public CltiCaseService(CltiApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// Saves the current StateService data to the server
        /// </summary>
        /// <param name="stateService">StateService with current data</param>
        /// <returns>API response with case ID</returns>
        public async Task<bool> SaveCaseAsync(StateService stateService)
        {
            try
            {
                // Convert StateService to DTO
                var dto = stateService.ToDto();

                // Call the API
                var response = await _apiClient.SaveCaseAsync(dto);

                if (response.Success && response.Data != null)
                {
                    // Update the StateService with the returned case ID
                    stateService.CaseId = response.Data.CaseId;
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                // Log error if needed
                return false;
            }
        }

        /// <summary>
        /// Loads a case from the server and updates the StateService
        /// </summary>
        /// <param name="caseId">Case ID to load</param>
        /// <param name="stateService">StateService to update</param>
        /// <returns>True if successful</returns>
        public async Task<bool> LoadCaseAsync(int caseId, StateService stateService)
        {
            try
            {
                var response = await _apiClient.GetCaseAsync(caseId);

                if (response.Success && response.Data != null)
                {
                    // Update StateService from DTO
                    stateService.UpdateFromDto(response.Data);
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                // Log error if needed
                return false;
            }
        }

        /// <summary>
        /// Deletes a case from the server
        /// </summary>
        /// <param name="caseId">Case ID to delete</param>
        /// <returns>True if successful</returns>
        public async Task<bool> DeleteCaseAsync(int caseId)
        {
            try
            {
                var response = await _apiClient.DeleteCaseAsync(caseId);
                return response.Success;
            }
            catch (Exception)
            {
                // Log error if needed
                return false;
            }
        }
    }
}