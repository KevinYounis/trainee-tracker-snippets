namespace TraineeTracker.Web.Models
{
    // Diese Klasse speichert alles, was rein ein Mentor wissen oder haben muss.
    // Die allgemeinen Daten (Name, E-Mail) sind ja schon im ApplicationUser.
    public class MentorProfile
    {
        // Primärschlüssel für die Datenbank, damit EF die Profile auseinanderhalten kann.
        public int Id { get; set; }

        // eigentlich nicht wichtig, kann man rausnehmen
        public string LehrplanTyp { get; set; } = string.Empty; 

        // Hier speichern wir die ID des dazugehörigen ApplicationUsers ab (als Fremdschlüssel)
        public string ApplicationUserId { get; set; } = string.Empty;

        // Das eigentliche User-Objekt
        public ApplicationUser ApplicationUser { get; set; } = null!;

        // Ein Mentor kann ja mehrere Trainees betreuen. Durch diese Liste weiß das System,
        // wer wem zugewiesen ist (wichtig für Fortschrittskontrolle und Feedback-Views).
        public List<TraineeProfile> Trainees { get; set; } = new();
    }
}