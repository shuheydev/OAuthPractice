using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OAuthPractice.ViewModels
{
    public class LoginViewModel
    {
        //[Required]
        //[EmailAddress]
        //public string Email { get; set; }

        //[Required]
        //[DataType(DataType.Password)]
        //public string Password { get; set; }  

        //[Display(Name ="Remember me")]
        //public bool RememberMe { get; set; }

        /// <summary>
        /// ユーザーがログイン前にアクセスしたURLを入れる
        /// ログイン完了後にそのURLを開くため.
        /// </summary>
        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }
    }
}
