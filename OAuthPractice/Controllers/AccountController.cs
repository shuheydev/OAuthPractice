using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OAuthPractice.ViewModels;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace OAuthPractice.Controllers
{
    [Route("[controller]")]
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly SignInManager<IdentityUser> signInManager;

        public AccountController(
            UserManager<IdentityUser> userManager
            , SignInManager<IdentityUser> signInManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
        }

        [HttpGet]
        [Route("[action]")]
        // GET: /<controller>/
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            //check comming model object is valid?
            if (ModelState.IsValid)
            {
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email
                };

                var result = await userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("register", "Account");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("[action]")]
        public async Task<IActionResult> Login()
        {
            LoginViewModel model = new LoginViewModel
            {
                ReturnUrl = "",
                ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList()
            };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("[action]")]
        public async Task<IActionResult> Login(string returnUrl)
        {
            LoginViewModel model = new LoginViewModel
            {
                ReturnUrl = returnUrl,
                ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList()
            };
      
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("[action]")]
        public IActionResult ExternalLogin(LoginViewModel viewModel, string provider, string returnUrl)
        {
            //Google側の認証が終わったらどのURLに遷移させるかを指定している
            var redirectUrl = Url.Action("ExternalLoginCallbac", "Account",
                new { ReturnUrl = returnUrl });

            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

            return new ChallengeResult(provider, properties);//Googleの認証ページが開く
        }

        /// <summary>
        /// Googleの認証が終わったらこちら.
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <param name="remoteError"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [Route("[action]")]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            LoginViewModel loginViewModel = new LoginViewModel
            {
                ReturnUrl = returnUrl,
                ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList()
            };

            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider:{remoteError}");

                return View("Login", loginViewModel);
            }

            //これはなにをやるんだ?
            //signInManagerがOAuthの認証結果を持っている?
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ModelState.AddModelError(string.Empty, "Error loading external login information.");

                return View("Login", loginViewModel);
            }

            //これはこのアプリケーション側のAspNetUserLoginsテーブルにログインユーザーとして登録するためか?
            var signInResult = await signInManager.ExternalLoginSignInAsync(info.LoginProvider,
                info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            //わかった.
            // AspNetUserLoginsテーブルは外部認証によるログイン情報が格納されるんだ.
            //初回は記録がないからfalse

            if (signInResult.Succeeded)
            {
                //以前にその外部認証でログインしたことがある.
                //問題なし
                return LocalRedirect(returnUrl);
            }
            else
            {
                //signInResultがfalseってどういうとき?
                //初めて外部認証でログインしたユーザーの場合だ.こっちは.

                //メールアドレスを取得する.
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);

                // このアプリにアカウントを持っているかどうかをEmailで検索する.
                //内部のユーザー情報を検索する
                if (email != null)
                {

                    var user = await userManager.FindByEmailAsync(email);

                    //そんなユーザーはいない...の場合
                    //つまり,このアプリを初めて使うユーザーで,
                    //外部認証を使った人.
                    if (user == null)
                    {
                        user = new IdentityUser
                        {
                            UserName = info.Principal.FindFirstValue(ClaimTypes.Email),
                            Email = info.Principal.FindFirstValue(ClaimTypes.Email)
                        };

                        //ユーザー作成
                        await userManager.CreateAsync(user);
                    }

                    //UserLoginsテーブルに追加.
                    await userManager.AddLoginAsync(user, info);
                    //このアプリにおけるサインイン状態にする
                    await signInManager.SignInAsync(user, isPersistent: false);

                    return LocalRedirect(returnUrl);
                }
            }

            return View("Login", loginViewModel);
        }
    }
}
