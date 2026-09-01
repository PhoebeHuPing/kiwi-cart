using System.Security.Claims;
using KiwiCart.Api.Controllers;
using KiwiCart.Core.DTOs;
using KiwiCart.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace KiwiCart.Tests.Controllers;

public class FeedbackControllerTests
{
    private readonly Mock<IFeedbackService> _service = new();
    private readonly FeedbackController _sut;

    public FeedbackControllerTests()
    {
        _sut = new FeedbackController(_service.Object);
    }

    private void SetUser(string sub, string? name)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, sub) };
        if (name is not null) claims.Add(new Claim("name", name));
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async Task GetAll_Returns200WithData()
    {
        _service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeedbackResponse>
            {
                new() { Id = 1, UserName = "Phoebe", Message = "hi", Category = "general" }
            });

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsAssignableFrom<IReadOnlyList<FeedbackResponse>>(ok.Value);
        Assert.Single(data);
    }

    [Fact]
    public async Task Submit_EmptyMessage_Returns400()
    {
        SetUser("auth0|u1", "Phoebe");

        var result = await _sut.Submit(new FeedbackRequest { Message = "  " }, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(400, problem.StatusCode);
        _service.Verify(s => s.CreateAsync(
            It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<FeedbackRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Submit_ValidMessage_Returns201AndUsesJwtClaims()
    {
        SetUser("auth0|user123", "Phoebe");
        _service.Setup(s => s.CreateAsync(
                "auth0|user123", "Phoebe",
                It.IsAny<FeedbackRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeedbackResponse
            {
                Id = 42, UserName = "Phoebe", Message = "Great!", Category = "praise"
            });

        var result = await _sut.Submit(
            new FeedbackRequest { Message = "Great!", Category = "praise" }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, created.StatusCode);
        var body = Assert.IsType<FeedbackResponse>(created.Value);
        Assert.Equal(42, body.Id);
        // Verify controller took userId/userName from JWT claims, not the request body.
        _service.Verify(s => s.CreateAsync(
            "auth0|user123", "Phoebe",
            It.Is<FeedbackRequest>(r => r.Message == "Great!"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
