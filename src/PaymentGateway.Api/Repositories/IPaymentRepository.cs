using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Repositories
{
    public interface IPaymentRepository
    {
        Task<ServiceResult<PostToBankResponse>> PostAsync(PostPaymentRequestDto request);
        Task<ServiceResult<GetPaymentResponse>> GetAsync(Guid id);
    }
}
