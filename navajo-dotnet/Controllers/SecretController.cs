using Microsoft.AspNetCore.Mvc;
using navajo_dotnet.Data;
using navajo_dotnet.Domain;
using navajo_dotnet.Models;
using navajo_dotnet.Service;

namespace navajo_dotnet.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class SecretController : ControllerBase
{
    private readonly ILogger<SecretController> _logger;
    private readonly IEncryptionService _encryptionService;
    private readonly AppDbContext _dbContext;

    public SecretController(ILogger<SecretController> logger, IEncryptionService encryptionService,
        AppDbContext dbContext)
    {
        _logger = logger;
        _encryptionService = encryptionService;
        _dbContext = dbContext;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateSecretResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] CreateSecretRequest req)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var (encrypted, iv) = _encryptionService.Encrypt(req.Value);
        Secret secret = new Secret(encrypted, iv);
        
        _dbContext.Secrets.Add(secret);
        await _dbContext.SaveChangesAsync();

        var resp = new CreateSecretResponse
        {
            Link = $"{Request.Scheme}://{Request.Host}/secret/{secret.Id}",
            ExpiresAt = secret.ExpiresAt
        };
        
        return Created(new Uri($"{Request.Scheme}://{Request.Host}/secret/{secret.Id}"), resp);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RetrieveSecretResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public async Task<IActionResult> Get(Guid id)
    {
        var secret = await _dbContext.Secrets.FindAsync(id);
        if (secret == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Secret not found",
                detail: $"Secret with id {id.ToString()} not found"
            );
        }

        if (secret.IsExpired())
        {
            return Problem(
                statusCode: StatusCodes.Status410Gone,
                title: "Secret expired",
                detail: $"Secret with id {id.ToString()} has expired"
            );
        }

        if (secret.IsClaimed())
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Secret already claimed",
                detail: $"Secret with id {id.ToString()} has already been claimed"
            );
        }

        secret.MarkAsClaimed();
        await _dbContext.SaveChangesAsync();

        var value = System.Text.Encoding.UTF8.GetString(secret.Value);
        var nonce = System.Text.Encoding.UTF8.GetString(secret.Nonce);
        
        var decryptedValue = _encryptionService.Decrypt(value, nonce);

        return Ok(new RetrieveSecretResponse
        {
            Value = decryptedValue
        });
    }
}