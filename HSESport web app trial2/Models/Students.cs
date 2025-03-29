using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSESport_web_app_trial2.Models
{
    public class Students
    {
        [Key]
        [Display(Name = "Номер зачетной книжки студента")]
        /// <summary>
        /// номер зачетки студента
        /// </summary>
        public int StudentIdentificator
        {
            get; set;
        }

        [Display(Name = "Имя студента")]
        /// <summary>
        /// имя студента
        /// </summary>
        public string? StudentName
        {
            get; set;
        }

        [Display(Name = "Фамилия студента")]
        /// <summary>
        /// фамилия студента
        /// </summary>
        public string? StudentSurname
        {
            get; set;
        }

        [Display(Name = "Отчество студента")]
        /// <summary>
        /// отчество студента
        /// </summary>
        public string? StudentSecondName
        {
            get; set;
        }

        [Display(Name = "Корпоративная почта студента")]
        /// <summary>
        /// корпоративная электронная почта студента
        /// </summary>
        public string? StudentEmail
        {
            get; set;
        }

        [Display(Name = "Число посещений студентом спортивных секций")]
        /// <summary>
        /// число посещений студентом спортивных секций
        /// </summary>
        public int StudentAttendanceOnSportActivities
        {
            get; set;
        }
    }
}
