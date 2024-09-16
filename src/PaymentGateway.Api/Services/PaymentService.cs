using PaymentGateway.Api.Constants.Enums;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Repositories;

namespace PaymentGateway.Api.Services
{
    public class PaymentService(ILogger<PaymentService> logger, IPaymentRepository paymentsRepository) : IPaymentService
    {
        public async Task<PostPaymentResponse> PostPaymentAsync(PostPaymentRequest postPaymentRequest)
        {
            var validExpiryDate = IsValidExpiryDate(postPaymentRequest.ExpiryMonth, postPaymentRequest.ExpiryYear);
            if (!validExpiryDate)
            {
                var errorMessageTemplate = $"Expiry date of {postPaymentRequest.ExpiryMonth}/{postPaymentRequest.ExpiryYear} is not valid";
                logger.LogError(errorMessageTemplate);
                return CreatePostPaymentResponse(postPaymentRequest, PaymentStatus.Rejected, errorMessageTemplate);
            }

            var paymentRequestDto = new PostPaymentRequestDto(postPaymentRequest);
            var response = await paymentsRepository.PostAsync(paymentRequestDto);
            return response.IsSuccess && string.IsNullOrEmpty(response.ErrorMessage)
                ? CreatePostPaymentResponse(postPaymentRequest, response.Content.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined)
                : CreatePostPaymentResponse(postPaymentRequest, PaymentStatus.Rejected, response?.ErrorMessage);
        }

        public async Task<ServiceResult<GetPaymentResponse>> GetPaymentByIdAsync(Guid id)
        {
            var response = await paymentsRepository.GetAsync(id);
            if (!response.IsSuccess || !string.IsNullOrEmpty(response.ErrorMessage))
            {
                logger.LogError(response.ErrorMessage);
            }
            return response;
        }

        private static PostPaymentResponse CreatePostPaymentResponse(PostPaymentRequest postPaymentRequest, PaymentStatus status, string errorMessage = null)
        {
            var response = new PostPaymentResponse()
            {
                Amount = postPaymentRequest.Amount,
                CardNumberLastFour = int.TryParse(postPaymentRequest.CardNumber[^4..], out var lastFour) ? lastFour : 0,
                Currency = postPaymentRequest.Currency,
                ExpiryMonth = postPaymentRequest.ExpiryMonth,
                ExpiryYear = postPaymentRequest.ExpiryYear,
                Id = Guid.NewGuid(),
                Status = status.ToString(),
                ErrorMessage = errorMessage ?? string.Empty
            };
            return response;
        }

        private static bool IsValidExpiryDate(int expiryMonth, int expiryYear)
        {
            return expiryYear == DateTime.Now.Year ? expiryMonth > DateTime.Now.Month : expiryYear > DateTime.Now.Year;
        }
    }
}
