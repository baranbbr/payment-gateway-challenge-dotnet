using System.Net;

using Microsoft.AspNetCore.Mvc;

using Moq;

using PaymentGateway.Api.Constants.Enums;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.Controllers;

public class PaymentControllerTests
{
    private readonly PaymentController _paymentController;
    private readonly Mock<IPaymentService> _mockPaymentService;
    private readonly PostPaymentRequest _postPaymentRequest;
    private readonly PostToBankResponse _postToBankResponse;
    private readonly PostPaymentResponse _postPaymentResponse;
    private readonly ServiceResult<GetPaymentResponse> _getPaymentResponse;

    public PaymentControllerTests()
    {
        _postPaymentRequest = new PostPaymentRequest()
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = 2028,
            Amount = 100,
            Currency = "GBP",
            Cvv = "123"
        };

        _postToBankResponse = new PostToBankResponse()
        {
            AuthorizationCode = "1231231",
            Authorized = true
        };

        _getPaymentResponse = new ServiceResult<GetPaymentResponse>()
        {
            Content = new GetPaymentResponse()
            {

                CardNumber = "1234567890123456",
                ExpiryMonth = 12,
                ExpiryYear = 2028,
                Amount = 100,
                Currency = "GBP",
                Status = PaymentStatus.Authorized,
                Id = Guid.NewGuid()
            },
            IsSuccess = true,
            StatusCode = HttpStatusCode.OK
        };

        _postPaymentResponse = new PostPaymentResponse()
        {
            Amount = _postPaymentRequest.Amount,
            CardNumberLastFour = int.Parse(_postPaymentRequest.CardNumber[^4..]),
            Currency = _postPaymentRequest.Currency,
            Status = PaymentStatus.Authorized.ToString()
        };
        _mockPaymentService = new Mock<IPaymentService>();
        _mockPaymentService.Setup(p => p.PostPaymentAsync(It.IsAny<PostPaymentRequest>()))
            .ReturnsAsync(() => _postPaymentResponse);
        _mockPaymentService.Setup(p => p.GetPaymentByIdAsync(It.IsAny<Guid>())).ReturnsAsync(() => _getPaymentResponse);
        _paymentController = new PaymentController(_mockPaymentService.Object);
    }

    [Fact]
    public async Task PostPaymentAsync_Should_Generate_Successful_Response()
    {
        // Arrange
        var postPaymentRequest = new PostPaymentRequest()
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = 2028,
            Amount = 100,
            Currency = "GBP",
            Cvv = "123"
        };

        // Act
        var result = await _paymentController.PostPaymentAsync(postPaymentRequest);

        // Assert
        var requestResult = result as OkObjectResult;
        var resultObject = requestResult.Value as PostPaymentResponse;

        Assert.Equal(_postPaymentResponse.Status, resultObject.Status);
        _mockPaymentService.Verify(x => x.PostPaymentAsync(postPaymentRequest), Times.Once());
    }

    [Fact]
    public async Task PostPaymentAsync_Should_ReturnBadRequest_WhenFails()
    {
        // Arrange
        _mockPaymentService.Setup(p => p.PostPaymentAsync(It.IsAny<PostPaymentRequest>()))
            .ReturnsAsync(() => new PostPaymentResponse()
            {
                Status = PaymentStatus.Rejected.ToString(),
                ErrorMessage = "Error",
            });

        // Act
        var result = await _paymentController.PostPaymentAsync(new PostPaymentRequest());

        // Assert
        var requestResult = result as BadRequestObjectResult;
        Assert.Equal((int)HttpStatusCode.BadRequest, requestResult.StatusCode);
    }

    [Fact]
    public async Task GetPaymentAsync_Should_Generate_Successful_Response()
    {
        // Arrange
        var id = Guid.NewGuid();
        _getPaymentResponse.Content.Id = id;

        // Act
        var result = await _paymentController.GetPaymentAsync(id);

        // Assert
        var okResult = result as OkObjectResult;
        var resultObject = okResult.Value as GetPaymentResponse;
        Assert.Equal(_getPaymentResponse.Content.Id.ToString(), resultObject.Id.ToString());
        _mockPaymentService.Verify(x => x.GetPaymentByIdAsync(id), Times.Once());
    }

    [Fact]
    public async Task GetPaymentAsync_Should_ReturnNotFound_WhenIdNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockPaymentService.Setup(p => p.GetPaymentByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => new ServiceResult<GetPaymentResponse>()
            {
                StatusCode = HttpStatusCode.NotFound,
                ErrorMessage = "not found"
            });

        // Act
        var result = await _paymentController.GetPaymentAsync(id);

        // Assert
        var requestResult = result as NotFoundObjectResult;
        Assert.Equal((int)HttpStatusCode.NotFound, requestResult.StatusCode);
    }

    [Fact]
    public async Task GetPaymentAsync_Should_ReturnBadRequest_WhenServiceFails()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mockPaymentService.Setup(p => p.GetPaymentByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => new ServiceResult<GetPaymentResponse>
            {
                ErrorMessage = "An unexpected error occurred",
                StatusCode = HttpStatusCode.BadRequest,
                IsSuccess = false
            });

        // Act
        var result = await _paymentController.GetPaymentAsync(id);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.NotNull(badRequestResult);
        //Assert.Contains("An unexpected error occurred", badRequestResult.);
    }
}