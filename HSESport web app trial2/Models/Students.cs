using System.ComponentModel.DataAnnotations;

namespace HSESport_web_app_trial2.Models
{
    public class Students
    {
        [Key]
        /// <summary>
        /// номер зачетки студента
        /// </summary>
        public int StudentIdentificator
        {
            get; set;
        }

        /// <summary>
        /// имя студента
        /// </summary>
        public string? StudentName
        {
            get; set;
        }

        /// <summary>
        /// фамилия студента
        /// </summary>
        public string? StudentSurname
        {
            get; set;
        }

        /// <summary>
        /// отчество студента
        /// </summary>
        public string? studentSecondName
        {
            get; set;
        }

        /// <summary>
        /// корпоративная электронная почта студента
        /// </summary>
        public string? StudentEmail
        {
            get; set;
        }

        /// <summary>
        /// число посещений студентом спортивных секций
        /// </summary>
        public int StudentAttendanceOnSportActivities
        {
            get; set;
        }
    }
}
