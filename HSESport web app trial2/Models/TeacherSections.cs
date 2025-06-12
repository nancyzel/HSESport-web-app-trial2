namespace HSESport_web_app_trial2.Models
{
    public class TeacherSection
    {
        public int TeacherId { get; set; }
        public int SectionId { get; set; }

        // Навигационные свойства для доступа к связанным Teacher и Section
        public Teachers Teacher { get; set; } 
        public Sections Section { get; set; } 
    }
}
