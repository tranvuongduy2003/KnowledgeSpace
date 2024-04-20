using FluentValidation.AspNetCore;
using IdentityModel.Client;
using KnowledgeSpace.ViewModels.Contents;
using KnowledgeSpace.WebPortal.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
if (environment == Environments.Development)
{
    builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
}

builder.Services.AddHttpClient();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Events = new CookieAuthenticationEvents
        {
            // this event is fired everytime the cookie has been validated by the cookie middleware,
            // so basically during every authenticated request
            // the decryption of the cookie has already happened so we have access to the user claims
            // and cookie properties - expiration, etc..
            OnValidatePrincipal = async x =>
            {
                // since our cookie lifetime is based on the access token one,
                // check if we're more than halfway of the cookie lifetime
                var now = DateTimeOffset.UtcNow;
                var timeElapsed = now.Subtract(x.Properties.IssuedUtc.Value);
                var timeRemaining = x.Properties.ExpiresUtc.Value.Subtract(now);
                if (timeElapsed > timeRemaining)
                {
                    var identity = (ClaimsIdentity)x.Principal.Identity;
                    var accessTokenClaim = identity.FindFirst("access_token");
                    var refreshTokenClaim = identity.FindFirst("refresh_token");
                    // if we have to refresh, grab the refresh token from the claims, and request
                    // new access token and refresh token
                    var refreshToken = refreshTokenClaim.Value;
                    var response = await new HttpClient().RequestRefreshTokenAsync(new RefreshTokenRequest
                    {
                        Address = builder.Configuration["Authorization:AuthorityUrl"],
                        ClientId = builder.Configuration["Authorization:ClientId"],
                        ClientSecret = builder.Configuration["Authorization:ClientSecret"],
                        RefreshToken = refreshToken
                    });
                    if (!response.IsError)
                    {
                        // everything went right, remove old tokens and add new ones
                        identity.RemoveClaim(accessTokenClaim);
                        identity.RemoveClaim(refreshTokenClaim);
                        identity.AddClaims(new[]
                        {
                                        new Claim("access_token", response.AccessToken),
                                        new Claim("refresh_token", response.RefreshToken)
                                    });
                        // indicate to the cookie middleware to renew the session cookie
                        // the new lifetime will be the same as the old one, so the alignment
                        // between cookie and access token is preserved
                        x.ShouldRenew = true;
                    }
                }
            }
        };
    })
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = builder.Configuration["Authorization:AuthorityUrl"];
        options.RequireHttpsMetadata = false;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.ClientId = builder.Configuration["Authorization:ClientId"];
        options.ClientSecret = builder.Configuration["Authorization:ClientSecret"];
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("offline_access");
        options.Scope.Add("api.knowledgespace");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
            RoleClaimType = "role"
        };
        options.Events = new OpenIdConnectEvents
        {
            // that event is called after the OIDC middleware received the auhorisation code,
            // redeemed it for an access token and a refresh token,
            // and validated the identity token
            OnTokenValidated = x =>
            {
                // store both access and refresh token in the claims - hence in the cookie
                var identity = (ClaimsIdentity)x.Principal.Identity;
                identity.AddClaims(new[]
                {
                                new Claim("access_token", x.TokenEndpointResponse.AccessToken),
                                new Claim("refresh_token", x.TokenEndpointResponse.RefreshToken)
                            });
                // so that we don't issue a session cookie but one with a fixed expiration
                x.Properties.IsPersistent = true;
                // align expiration of the cookie with expiration of the
                // access token
                var accessToken = new JwtSecurityToken(x.TokenEndpointResponse.AccessToken);
                x.Properties.ExpiresUtc = accessToken.ValidTo;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddControllersWithViews()
                .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<KnowledgeBaseCreateRequestValidator>());

//Declare DI containers
builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddTransient<ICategoryApiClient, CategoryApiClient>();
builder.Services.AddTransient<IKnowledgeBaseApiClient, KnowledgeBaseApiClient>();
builder.Services.AddTransient<ILabelApiClient, LabelApiClient>();
builder.Services.AddTransient<IUserApiClient, UserApiClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseAuthentication();

app.MapControllerRoute(
        name: "My KBs",
        pattern: "/my-kbs",
        new { controller = "Account", action = "MyKnowledgeBases" });

app.MapControllerRoute(
        name: "New KB",
        pattern: "/new-kb",
        new { controller = "Account", action = "CreateNewKnowledgeBase" });

app.MapControllerRoute(
        name: "Edit KB",
        pattern: "/edit-kb/{id}",
        new { controller = "Account", action = "EditKnowledgeBase" });

app.MapControllerRoute(
        name: "List By Tag Id",
        pattern: "/tag/{tagId}",
        new { controller = "KnowledgeBase", action = "ListByTag" });

app.MapControllerRoute(
        name: "Search KB",
        pattern: "/search",
        new { controller = "KnowledgeBase", action = "Search" });

app.MapControllerRoute(
        name: "KnowledgeBaseDetails",
        pattern: "/kb/{seoAlias}-{id}",
        new { controller = "KnowledgeBase", action = "Details" });

app.MapControllerRoute(
        name: "ListByCategoryId",
        pattern: "/cat/{categoryAlias}-{id}",
        new { controller = "KnowledgeBase", action = "ListByCategoryId" });

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();