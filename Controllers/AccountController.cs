using System.Security.Claims;
using ImageCompress.AccountSQL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static ImageCompress.AccountSQL.AccountService;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private const string PROJECT_ID = "imagecompress-393703";
    private const string LOCATION_ID = "asia-east1";
    private const string KEY_RING_ID = "account-password-key-ring";
    private readonly ILogger<AccountController> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly AccountServiceClient _accountServiceClient;
    private readonly KmsHelper _kmsService;
    private readonly JwtHelper _jwtHelper;

    public AccountController(ILogger<AccountController> logger,
        IWebHostEnvironment webHostEnvironment,
        AccountServiceClient accountServiceClient,
        KmsHelper kmsHelper,
        JwtHelper jwtHelper
        )
    {
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;
        _accountServiceClient = accountServiceClient;
        _kmsService = kmsHelper;
        _jwtHelper = jwtHelper;
    }
    [HttpPost("SignUp")]
    public async Task<ActionResult> SignUp(SignUpViewModel signUpViewModel)
    {
        try
        {
            var searchEmail = await _accountServiceClient.SelectAccountByEmailAsync(
                new SelectAccountByEmailRequest()
                {
                    Email = signUpViewModel.Email
                });
            if (!string.IsNullOrEmpty(searchEmail.Account.Id))
            {
                return BadRequest("Email already used.");
            }
            var accountID = Guid.NewGuid().ToString();
            _kmsService.CreateKeySymmetricEncryptDecrypt(PROJECT_ID, LOCATION_ID, KEY_RING_ID,
                accountID);
            var encryptPassword = _kmsService.EncryptText(PROJECT_ID, LOCATION_ID, KEY_RING_ID, accountID, signUpViewModel.Password);
            await _accountServiceClient.InsertAccountAsync(
                new InsertAccountRequest()
                {
                    Account = new AccountItem()
                    {
                        Id = accountID,
                        Email = signUpViewModel.Email,
                        Password = encryptPassword,
                        State = 0,
                        CreateDate = DateTime.Now.ToString(),
                        CreateBy = accountID,
                    }
                });
            return Ok();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "");
            throw;
        }
    }
    [HttpPost("SignIn")]
    public async Task<ActionResult> SignIn(SignInViewModel signInViewModel)
    {
        try
        {
            var account = (await _accountServiceClient.SelectAccountByEmailAsync(
                new SelectAccountByEmailRequest()
                {
                    Email = signInViewModel.Email
                }
            )).Account;
            if (string.IsNullOrEmpty(account.Id))
            {
                return BadRequest("Email not yet sigup.");
            }
            var decryptPassword = _kmsService.DecryptText(PROJECT_ID, LOCATION_ID, KEY_RING_ID, account.Id, account.Password);
            if (decryptPassword != signInViewModel.Password)
            {
                return BadRequest("Email or Password incorrect.");
            }
            var accessToken = _jwtHelper.GenerateToken(account);
            return Ok(accessToken);

        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "");
            throw;
        }
    }
    [Authorize]
    [HttpGet("GoogleOAuthCallBack")]
    public async Task<ActionResult> GoogleOAuthCallBack()
    {
        try
        {
            var googleIdentifyId = this.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var email = this.User.FindFirst(ClaimTypes.Email)!.Value;
            var accountSearchReply = await _accountServiceClient.SelectAccountByEmailAsync(new SelectAccountByEmailRequest()
            {
                Email = this.User.FindFirst(ClaimTypes.Email)!.Value
            });
            if (string.IsNullOrEmpty(accountSearchReply.Account.Id))
            {
                var account = new AccountItem();
                account.Id = Guid.NewGuid().ToString();
                account.Email = email;
                account.GoogleId = googleIdentifyId;
                account.State = 0;
                account.CreateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                account.CreateBy = account.Id;
            }
            var redirectUri = new Uri(_webHostEnvironment.IsDevelopment() ? "http://127.0.0.1:4200" : $"{Request.Scheme}://{Request.Host}");

            string successRedirectPath = "/home";
            redirectUri = new Uri(redirectUri, successRedirectPath);

            return Redirect(redirectUri.ToString());
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "");
            throw;
        }
    }
}
