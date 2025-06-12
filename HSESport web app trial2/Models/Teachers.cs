using System.ComponentModel.DataAnnotations;

namespace HSESport_web_app_trial2.Models
{
    public class Teachers
    {
        [Key]
        [Display(Name = "Id преподавателя")]
        /// <summary>
        /// Id преподавателя
        /// </summary>
        public int TeacherId
        {
            get; set;
        }

        [Display(Name = "Имя преподавателя")]
        /// <summary>
        /// Имя преподавателя
        /// </summary>
        public string? Name
        {
            get; set;
        }

        [Display(Name = "Фамилия преподавателя")]
        /// <summary>
        /// фамилия преподавателя
        /// </summary>
        public string? Surname
        {
            get; set;
        }

        [Display(Name = "Отчество преподавателя")]
        /// <summary>
        /// отчество преподавателя
        /// </summary>
        public string? SecondName
        {
            get; set;
        }

        [Display(Name = "Адрес корпоративаной почты преподавателя")]
        /// <summary>
        /// Адрес корпоративаной почты преподавателя
        /// </summary>
        public string? Email
        {
            get; set;
        }

        [Display(Name = "Пароль преподавателя")]
        /// <summary>
        /// Пароль преподавателя
        /// </summary>
        public string? Password
        {
            get; set;
        }

        public ICollection<TeacherSection> TeacherSections { get; set; } = new List<TeacherSection>();
    }
}
