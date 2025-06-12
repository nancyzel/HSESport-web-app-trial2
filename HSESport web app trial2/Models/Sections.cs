using System.ComponentModel.DataAnnotations;

namespace HSESport_web_app_trial2.Models
{
    public class Sections
    {
        [Key]
        [Display(Name = "Id секции")]
        /// <summary>
        /// Id секции
        /// </summary>
        public int SectionId
        {
            get; set;
        }

        [Display(Name = "Название секции")]
        /// <summary>
        /// название секции
        /// </summary>
        public string? Name
        {
            get; set;
        }

        [Display(Name = "Адрес секции")]
        /// <summary>
        /// адрес секции
        /// </summary>
        public string? Address
        {
            get; set;
        }

        [Display(Name = "Время начала секции")]
        /// <summary>
        /// время начала секции
        /// </summary>
        public TimeSpan? StartTime
        {
            get; set;
        }

        [Display(Name = "Время конца секции")]
        /// <summary>
        /// время конца секции
        /// </summary>
        public TimeSpan? EndTime
        {
            get; set;
        }

        public ICollection<TeacherSection> TeacherSections { get; set; } = new List<TeacherSection>();
        public ICollection<StudentsSections> StudentsSections { get; set; } = new List<StudentsSections>();
    }
}
