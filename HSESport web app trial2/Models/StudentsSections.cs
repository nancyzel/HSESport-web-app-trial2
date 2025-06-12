namespace HSESport_web_app_trial2.Models
{
    public class StudentsSections
    {
        // Эти свойства будут частью составного первичного ключа
        public int StudentId { get; set; }
        public int SectionId { get; set; }

        // Навигационные свойства для доступа к связанным Student и Section
        public Students Student { get; set; }
        public Sections Section { get; set; }
    }
}
