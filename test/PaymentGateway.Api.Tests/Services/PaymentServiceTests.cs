using Microsoft.Extensions.Logging;

using Moq;

using PaymentGateway.Api.Constants.Enums;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Repositories;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.Services
{
    public class PaymentServiceTests
    {
        private readonly PaymentService _paymentService;
        private readonly Mock<IPaymentRepository> _paymentRepository;
        private readonly Mock<ILogger<PaymentService>> _logger;
        private readonly PostPaymentRequestDto _postPaymentRequestDto;
        private readonly PostPaymentRequest _postPaymentRequest;
        private readonly ServiceResult<PostToBankResponse> _postToBankResponse;
        private const string AuthorizationCode = "1231231";
        private const bool Authorized = true;

        public PaymentServiceTests()
        {
            _postPaymentRequest = new()
            {
                Amount = 1000,
                CardNumber = "123124144231",
                Currency = Currencies.GBP.ToString(),
                Cvv = "123",
                ExpiryMonth = 5,
                ExpiryYear = DateTime.Now.Year + 100
            };

            _postToBankResponse = new ServiceResult<PostToBankResponse>()
            {
                IsSuccess = true,
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new PostToBankResponse() { AuthorizationCode = AuthorizationCode, Authorized = Authorized }
            };

            _postPaymentRequestDto = new PostPaymentRequestDto(_postPaymentRequest);
            _logger = new Mock<ILogger<PaymentService>>();

            _paymentRepository = new Mock<IPaymentRepository>();
            _paymentRepository.Setup(x => x.PostAsync(It.IsAny<PostPaymentRequestDto>()))
                .ReturnsAsync(() => _postToBankResponse);
            _paymentService = new PaymentService(_logger.Object, _paymentRepository.Object);
        }

        [Fact]
        public async Task ProcessPaymentAsync_ReturnsSuccess()
        {
            // Arrange
            var expectedCardNumberLastFour = int.Parse(_postPaymentRequest.CardNumber[^4..]);
            var expectedAmount = _postPaymentRequest.Amount;

            // Act
            var result = await _paymentService.PostPaymentAsync(_postPaymentRequest);

            // Assert
            Assert.Equal(PaymentStatus.Authorized.ToString(), result.Status);
            Assert.Equal(expectedCardNumberLastFour, result.CardNumberLastFour);
            Assert.Equal(expectedAmount, result.Amount);
        }

        [Fact]
        public async Task ProcessPaymentAsync_Should_ReturnFailure_WhenInvalidExpiryDate()
        {
            // Arrange
            _postPaymentRequest.ExpiryMonth = 1;
            _postPaymentRequest.ExpiryYear = DateTime.Now.Year - 2;
            var expectedErrorMessage = $"Expiry date of {_postPaymentRequest.ExpiryMonth}/{_postPaymentRequest.ExpiryYear} is not valid";

            // Act
            var result = await _paymentService.PostPaymentAsync(_postPaymentRequest);

            // Assert
            Assert.Contains(expectedErrorMessage, result.ErrorMessage);
            Assert.Equal(PaymentStatus.Rejected.ToString(), result.Status);
        }

        [Fact]
        public async Task ProcessPaymentAsync_Should_ReturnDeclined_WhenPaymentNotAuthorized()
        {
            // Arrange
            _postToBankResponse.Content.Authorized = false;
            _postToBankResponse.Content.AuthorizationCode = string.Empty;

            _paymentRepository.Setup(x => x.PostAsync(It.IsAny<PostPaymentRequestDto>()))
                .ReturnsAsync(() => _postToBankResponse);

            // Act
            var result = await _paymentService.PostPaymentAsync(_postPaymentRequest);

            // Assert
            Assert.True(result.Status == PaymentStatus.Declined.ToString());
        }
    }
}
