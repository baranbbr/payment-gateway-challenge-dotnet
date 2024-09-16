using System.Net;

namespace PaymentGateway.Api.Models.Responses
{
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public T Content { get; set; }
        public string ErrorMessage { get; set; }

        public ServiceResult(HttpResponseMessage response)
        {
            StatusCode = response.StatusCode;
            IsSuccess = response.IsSuccessStatusCode;
        }
        public ServiceResult() { }
    }
}
