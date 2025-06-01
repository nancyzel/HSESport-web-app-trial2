using System.ComponentModel.DataAnnotations;

namespace HSESport_web_app_trial2.Models
{
    public class Authorization
    {

        [Key]
        [Display(Name = "Адрес электронной почты")]
        /// <summary>
        /// номер зачетки студента
        /// </summary>
        public string? UserEmail
        {
            get; set;
        }

        [Display(Name = "Пароль")]
        /// <summary>
        /// имя студента
        /// </summary>
        public string? UserPassword
        {
            get; set;
        }
    }
}
