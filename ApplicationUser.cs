using Microsoft.AspNetCore.Identity;

namespace TraineeTracker.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Eigene Felder, die Microsoft Identity nicht eingebaut hat, 
        // wir aber für das makandra-Design und die persönliche Ansprache brauchen
        public string Vorname { get; set; } = string.Empty;
        public string Nachname { get; set; } = string.Empty;
        
        // für unsere Statusverwaltung (Konto schließen / reaktivieren)
        public bool IstAktiv { get; set; } = true;
        
        // Verknüpfung zu den Trainee-Spezifischen Daten (falls der User ein Trainee ist)
        public TraineeProfile? TraineeProfile { get; set; }

        // Verknüpfung zu den Mentor-Spezifischen Daten (falls der User ein Mentor ist)
        public MentorProfile? MentorProfile { get; set; }

        // HINWEIS FÜR DIE ANDEREN: 
        // Felder wie 'Email', 'PasswordHash' oder 'Id' bitte hier NICHT manuell hinzufügen!
        // Die kommen automatisch im Hintergrund durch die IdentityUser-Vererbung oben mit.
    }
}