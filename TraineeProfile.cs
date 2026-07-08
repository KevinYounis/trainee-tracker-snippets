using System;

namespace TraineeTracker.Web.Models
{
    // Diese Klasse speichert alles, was rein ein Trainee wissen oder haben muss.
    // Die allgemeinen Daten (Name, E-Mail) sind ja schon im ApplicationUser.
    public class TraineeProfile
    {
        // Primärschlüssel für die Datenbank, damit EF die Profile auseinanderhalten kann.
        public int Id { get; set; }

        // Start und Ende der Ausbildung
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Start und Ende einer möglichen Pause
        public DateTime? BreakStartDate { get; set; }
        public DateTime? BreakEndDate { get; set; }

        // Hier sind alle Lektionen drin, die speziell diesem Trainee zugeordnet sind
        public List<TraineeLektion> Trainee_Lektionen { get; set; } = new();
        
        // Ich weiß nicht, ob dass wichtig ist (vielleicht für Lehrplan-Import wichtig?)
        public string LehrplanTyp { get; set; } = string.Empty; 
        
        // Hier speichern wir die ID des dazugehörigen ApplicationUsers ab (als Fremdschlüssel)
        public string ApplicationUserId { get; set; } = string.Empty;

        // Das eigentliche User-Objekt
        public ApplicationUser ApplicationUser { get; set; } = null!;
        
        // Ein Trainee kann von mehreren Mentoren betreut werden (und umgekehrt).
        // Das ist genau die Liste, die wir mit unserer 'AssignMentor'-View befüllen
        public List<MentorProfile> Mentoren { get; set; } = new();
    }
}