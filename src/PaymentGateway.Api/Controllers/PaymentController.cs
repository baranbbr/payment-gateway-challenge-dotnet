using System.Net;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Constants.Enums;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentController(IPaymentService paymentService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PostPaymentAsync([FromBody] PostPaymentRequest postPaymentRequest)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.SelectMany(x => x.Value.Errors)
                           .Select(x => x.ErrorMessage)
                           .ToList();

            return BadRequest(new PostPaymentResponse()
            {
                Status = PaymentStatus.Rejected.ToString(),
                ErrorMessage = string.Join("; ", errors)
            });
        }
        var response = await paymentService.PostPaymentAsync(postPaymentRequest);
        return response.Status == PaymentStatus.Rejected.ToString()
            ? BadRequest(response)
            : Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPaymentAsync(Guid id)
    {
        var response = await paymentService.GetPaymentByIdAsync(id);
        if (!response.IsSuccess)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    return NotFound($"Payment with Id:{id} could not be found");
                case HttpStatusCode.InternalServerError:
                    return StatusCode((int)HttpStatusCode.InternalServerError, response?.ErrorMessage);
                default:
                    return BadRequest(response?.ErrorMessage);
            }
        }
        return Ok(response.Content);
    }
}