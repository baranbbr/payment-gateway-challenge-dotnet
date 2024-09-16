using System.Net;
using System.Text.Json;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly ILogger<PaymentRepository> _logger;
    private readonly HttpClient _httpClient;

    public PaymentRepository(ILogger<PaymentRepository> logger, IHttpClientFactory httpClientFactory, string bankUrl)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(bankUrl);
    }

    public async Task<ServiceResult<PostToBankResponse>> PostAsync(PostPaymentRequestDto request)
    {
        ServiceResult<PostToBankResponse> responseObject;
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/payments", request);
            var content = await response.Content.ReadFromJsonAsync<PostToBankResponse>();
            if (response.IsSuccessStatusCode && content != null)
            {
                responseObject = new(response)
                {
                    Content = content
                };
            }
            else if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                responseObject = new(response)
                {
                    ErrorMessage = $"Error while processing payment, bank returned error, Error:{await response?.Content?.ReadAsStringAsync()}",
                };
            }
            else
            {
                var tryGetError = await response.Content.ReadAsStringAsync();
                responseObject = new(response) { ErrorMessage = tryGetError };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while processing payment");
            responseObject = new()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                IsSuccess = false,
                ErrorMessage = $"An unexpected error occurred - Error:{ex.Message}"
            };
        }
        return responseObject;
    }

    public async Task<ServiceResult<GetPaymentResponse>> GetAsync(Guid id)
    {
        // imagining that we're interacting with a db that provides a rest api
        ServiceResult<GetPaymentResponse> responseObject;
        try
        {
            var response = await _httpClient.GetAsync($"/get/payment/{id}");
            var content = await response.Content.ReadFromJsonAsync<GetPaymentResponse>();

            if (response.IsSuccessStatusCode)
            {
                return responseObject = new ServiceResult<GetPaymentResponse>(response)
                {
                    Content = content,
                };
            }

            _logger.LogWarning("Failed to get payment details. PaymentId:{PaymentId}, StatusCode:{StatusCode}, Response:{Response}",
                id, response.StatusCode, content);

            responseObject = new ServiceResult<GetPaymentResponse>(response)
            {
                ErrorMessage = $"Failed to get payment details. StatusCode:{response.StatusCode}",
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting the details for PaymentId:{PaymentId}", id);
            responseObject = new ServiceResult<GetPaymentResponse>
            {
                IsSuccess = false,
                StatusCode = HttpStatusCode.InternalServerError,
                ErrorMessage = $"An error occurred while getting the details for PaymentId:{id}"
            };
        }
        return responseObject;
    }
}