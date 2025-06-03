using System.ComponentModel.DataAnnotations;

namespace HSESport_web_app_trial2.Models
{
    public class BaseUserModel
    {
        [Display(Name = "Имя студента")]
        /// <summary>
        /// имя студента
        /// </summary>
        public string? Name
        {
            get; set;
        }

        [Display(Name = "Фамилия студента")]
        /// <summary>
        /// фамилия студента
        /// </summary>
        public string? Surname
        {
            get; set;
        }

        [Display(Name = "Отчество студента")]
        /// <summary>
        /// отчество студента
        /// </summary>
        public string? SecondName
        {
            get; set;
        }

        [Display(Name = "Адрес корпоративной почты")]
        /// <summary>
        /// корпоративная электронная почта студента
        /// </summary>
        public string? Email
        {
            get; set;
        }

        [Display(Name = "Пароль")]
        /// <summary>
        /// пароль от личного кабинета в приложении HSESport
        /// </summary>
        public string? Password
        {
            get; set;
        }
    }
}
