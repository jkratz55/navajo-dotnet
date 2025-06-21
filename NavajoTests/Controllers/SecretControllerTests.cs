using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using navajo_dotnet.Controllers;
using navajo_dotnet.Data;
using navajo_dotnet.Service;
using Moq;
using navajo_dotnet.Domain;
using Xunit;
using navajo_dotnet.Models;

namespace NavajoTests.Controllers;

public class SecretControllerTests
{
    private static readonly string LinkRegex = @"^https://navajo.com/secret/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$";
    
    private readonly Mock<ILogger<SecretController>> _loggerMock;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;
    private readonly DbContextOptions<AppDbContext> _contextOptions;

    public SecretControllerTests()
    {
        _loggerMock = new Mock<ILogger<SecretController>>();
        _encryptionServiceMock = new Mock<IEncryptionService>();
        
        _contextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task Post_CreateSecret_ReturnsCreated()
    {
        var request = new CreateSecretRequest ( Value: "test-secret" );
        _encryptionServiceMock.Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns(("encrypted", "iv"));
        
        SecretController controller = CreateController();
        IActionResult result = await controller.Post(request);
        CreatedResult createdResult = Assert.IsType<CreatedResult>(result);
        CreateSecretResponse response = Assert.IsType<CreateSecretResponse>(createdResult.Value);
        
        // Assert we received a CreatedResult with Http 201 status code and the response contains
        // a valid link and expiration time
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.NotEmpty(response.Link);
        Assert.True(response.ExpiresAt <= DateTimeOffset.UtcNow.AddHours(1));

        var guidMatch = Regex.Match(response.Link, LinkRegex);
        Assert.True(guidMatch.Success, "Link should contain a valid GUID");
        
        Assert.NotNull(createdResult.Location);
        var locationMatch = Regex.Match(createdResult.Location, LinkRegex);
        Assert.True(locationMatch.Success, "Response Header Location must contain a valid Link");
    }
    
    [Fact]
    public async Task Post_CreateSecret_InvalidRequest_ReturnsBadRequest()
    {
        var request = new CreateSecretRequest(Value: String.Empty);
        
        SecretController controller = CreateController();
        controller.ModelState.AddModelError("Value", "Value is required");
        
        IActionResult result = await controller.Post(request);
        
        BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task Get_GetSecret_ReturnsOk()
    {
        // Setup data in database
        using var dbContext = new AppDbContext(_contextOptions);
        var secret = new Secret("encrypted-value", "iv-value");
        dbContext.Secrets.Add(secret);
        await dbContext.SaveChangesAsync();

        // Configure mocks
        _encryptionServiceMock.Setup(x => x.Decrypt(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns("decrypted-secret");

        // Execute the controller method
        var controller = CreateController();
        var result = await controller.Get(secret.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RetrieveSecretResponse>(okResult.Value);
        Assert.Equal("decrypted-secret", response.Value);
    }
    
    [Fact]
    public async Task Get_GetSecret_InvalidId_ReturnsNotFound()
    {
        Guid randomId = Guid.NewGuid();
        
        // Configure mocks
        _encryptionServiceMock.Setup(x => x.Decrypt(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns("decrypted-secret");
        
        // Execute the controller method
        var controller = CreateController();
        var result = await controller.Get(randomId);
        
        // Assert
        var resultObject = Assert.IsType<ObjectResult>(result);
        var response = Assert.IsType<ProblemDetails>(resultObject.Value);
        Assert.Equal(StatusCodes.Status404NotFound, resultObject.StatusCode);
    }
    
    [Fact]
    public async Task Get_GetSecret_ExpiredSecret_ReturnsGone()
    {
        Guid randomId = Guid.NewGuid();
        
        using var dbContext = new AppDbContext(_contextOptions);
        var secret = new Secret("expired-encrypted-value", "expired-iv-value")
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            Id = randomId
        };
        dbContext.Secrets.Add(secret);
        await dbContext.SaveChangesAsync();
        
        // Configure mocks
        _encryptionServiceMock.Setup(x => x.Decrypt(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns("decrypted-secret");
        
        // Execute the controller method
        var controller = CreateController();
        var result = await controller.Get(randomId);
        
        var resultObject = Assert.IsType<ObjectResult>(result);
        var response = Assert.IsType<ProblemDetails>(resultObject.Value);
        Assert.Equal(StatusCodes.Status410Gone, resultObject.StatusCode);
    }
    
    [Fact]
    public async Task Get_GetSecret_ClaimedSecret_ReturnsConflict()
    {
        // Setup data in database
        using var dbContext = new AppDbContext(_contextOptions);
        var secret = new Secret("encrypted-value", "iv-value");
        secret.MarkAsClaimed();
        dbContext.Secrets.Add(secret);
        await dbContext.SaveChangesAsync();
        
        // Configure mocks
        _encryptionServiceMock.Setup(x => x.Decrypt(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns("decrypted-secret");

        // Execute the controller method
        var controller = CreateController();
        var result = await controller.Get(secret.Id);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        var response = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
    }
    
    private SecretController CreateController()
    {
        var dbContext = new AppDbContext(_contextOptions);
        
        SecretController controller = new SecretController(
            _loggerMock.Object,
            _encryptionServiceMock.Object,
            dbContext);
        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("navajo.com");

        // Assign the HttpContext to the controller
        controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext
        };

        return controller;
    }
}