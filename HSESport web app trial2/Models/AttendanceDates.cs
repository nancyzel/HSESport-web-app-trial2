using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSESport_web_app_trial2.Models
{
    public class AttendanceDates
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Указываем автоинкремент
        [Display(Name = "Id посещения")]
        /// <summary>
        /// Id посещения
        /// </summary>
        public int AttendanceId
        {
            get; set;
        }

        [Display(Name = "Id студента")]
        /// <summary>
        /// Id студента
        /// </summary>
        public int StudentId
        {
            get; set;
        }

        [Display(Name = "Id секции")]
        /// <summary>
        /// Id секции
        /// </summary>
        public int SectionId
        {
            get; set;
        }

        [Display(Name = "Дата посещения")]
        /// <summary>
        /// дата посещения
        /// </summary>
        public DateOnly? Date
        {
            get; set;
        }
    }
}
