using System.ComponentModel.DataAnnotations;
namespace HSESport_web_app_trial2.Models
{
    public class Students: BaseUserModel
    {
        [Key]
        [Display(Name = "Номер зачетной книжки студента")]
        /// <summary>
        /// номер зачетки студента
        /// </summary>
        public int StudentId
        {
            get; set;
        }

        [Display(Name = "Число посещений студентом спортивных секций")]
        /// <summary>
        /// число посещений студентом спортивных секций
        /// </summary>
        public int AttendanceRate
        {
            get; set;
        }

        public ICollection<StudentsSections> StudentsSections { get; set; } = new List<StudentsSections>();
    }
}
