using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TraineeTracker.Web.Data;
using TraineeTracker.Web.Models;
using TraineeTracker.Web.Repositories;

namespace TraineeTracker.Web.Controllers
{
    // Hilfsmodell für die Tabelle, damit Name, Rolle und Account-Status zusammenbleiben
    public class UserWithRoleViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public string Rolle { get; set; } = "Keine";
    }

    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {

        
        // Wir nutzen hier drei verschiedene Helfer, weil Microsoft Identity (für Logins)
        // und unser normales Entity Framework (für Profile) zusammenarbeiten müssen.
        private readonly IUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UniversalDbContext _context; 

        public AdminController(IUserRepository userRepository, UserManager<ApplicationUser> userManager, UniversalDbContext context)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> UserList(string search)
        {
            // Alle User aus der Datenbank abrufen
            var usersQuery = _userManager.Users;

            // Optionale Suchfunktion anwenden, falls etwas eingegeben wurde
            if (!string.IsNullOrWhiteSpace(search))
            {
                usersQuery = usersQuery.Where(u => 
                    u.Vorname.Contains(search) || 
                    u.Nachname.Contains(search) || 
                    u.Email.Contains(search));
            }

            var users = await usersQuery.ToListAsync();
            var userListWithRoles = new List<UserWithRoleViewModel>();

            // Rollen für jeden Benutzer ermitteln
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userListWithRoles.Add(new UserWithRoleViewModel
                {
                    User = user,
                    Rolle = roles.FirstOrDefault() ?? "Keine"
                });
            }

            return View("UserList", userListWithRoles);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        // ACCOUNT-ERSTELLUNG (MENTOR)
        [HttpPost]
        public async Task<IActionResult> CreateMentor(string email, string vorname, string nachname, string password)
        {
            // Als erstes den User im Arbeitsspeicher erstellen
            var user = new ApplicationUser { UserName = email, Email = email, Vorname = vorname, Nachname = nachname };
            
            // Prüfen, ob der User schon existiert
            if (await _userRepository.ExistsAsync(user)) 
            {
                TempData["AlertMessage"] = "Fehler: Ein User mit dieser E-Mail-Adresse existiert bereits!";
                return RedirectToAction("CreateUser"); 
            }

            // Wenn frei, schmeißen wir den User in den Identity-Topf. Das Passwort wird dabei automatisch gehasht
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                // "role" ist aus Identity. Brauchen wir für Zugriffsrechte
                await _userManager.AddToRoleAsync(user, "Mentor");

                //nur ID angeben, denn Entity Framework füllt ApplicationUser-Variable automatisch aus
                _context.MentorProfiles.Add(new MentorProfile { ApplicationUserId = user.Id });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("UserList");
        }

        // ACCOUNT-ERSTELLUNG (TRAINEE)
        // ähnlich wie beim Mentor
        [HttpPost]
        public async Task<IActionResult> CreateTrainee(string email, string vorname, string nachname, string password, DateTime start, DateTime ende)
        {
            var user = new ApplicationUser { UserName = email, Email = email, Vorname = vorname, Nachname = nachname };

            if (await _userRepository.ExistsAsync(user)) 
            {
                TempData["AlertMessage"] = "Fehler: Ein User mit dieser E-Mail-Adresse existiert bereits!";
                return RedirectToAction("CreateUser"); 
            }

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                // "role" ist aus Identity. Brauchen wir für Zugriffsrechte
                await _userManager.AddToRoleAsync(user, "Trainee");
                
                //nur ID angeben, denn Entity Framework füllt ApplicationUser-Variable automatisch aus
                _context.TraineeProfiles.Add(new TraineeProfile { ApplicationUserId = user.Id, StartDate = start, EndDate = ende });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("UserList");
        }

        [HttpPost]
        public async Task<IActionResult> CloseUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Wir löschen den User nicht hart aus der DB
            // Wir setzen einfach 'IstAktiv' auf false
            // Dadurch sperren wir den Login, behalten aber alle Daten 
            user.IstAktiv = false; 
            await _userRepository.UpdateAsync(user);
            
            return RedirectToAction("UserList");
        }

        // Reaktivierungs-Funktion
        [HttpPost]
        public async Task<IActionResult> ReactivateUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.IstAktiv = true; 
            await _userRepository.UpdateAsync(user);
            return RedirectToAction("UserList");
        }

        // Diese Methode lädt das Formular im Browser 
        [HttpGet]
        public async Task<IActionResult> AsignMentorToTrainee(string traineeProfileId)
        {
            var traineeProfile = await _context.TraineeProfiles
                .Include(t => t.ApplicationUser)
                .Include(t => t.Mentoren)
                .ThenInclude(m => m.ApplicationUser)
                .FirstOrDefaultAsync(t => t.ApplicationUserId == traineeProfileId);

            if (traineeProfile == null) return NotFound("Trainee-Profil wurde nicht gefunden.");

            var availableMentors = await _context.MentorProfiles
                .Include(m => m.ApplicationUser)
                .ToListAsync();

            ViewData["AvailableMentors"] = availableMentors;

            return View("AssignMentor", traineeProfile);
        }

        [HttpPost]
        public async Task<IActionResult> AsignMentorToTrainee(string traineeProfileId, string mentorProfileId)
        {
            // Wir laden das Trainee-Profile und sagen EF mit .Include(), dass er die Liste der 
            // bereits zugewiesenen Mentoren direkt mitladen soll
            var traineeProfile = await _context.TraineeProfiles
                .Include(t => t.Mentoren)
                .FirstOrDefaultAsync(t => t.ApplicationUserId == traineeProfileId);
            var mentorProfile = await _context.MentorProfiles
                .FirstOrDefaultAsync(m => m.ApplicationUserId == mentorProfileId);

            if (traineeProfile != null && mentorProfile != null)
            {
                // Da es eine n:m Beziehung ist, fügen wir einfach beide jeweils der Liste des anderen hinzu.
                traineeProfile.Mentoren.Add(mentorProfile);
                mentorProfile.Trainees.Add(traineeProfile);
                await _context.SaveChangesAsync(); 
            }
            return RedirectToAction("UserList");
        }

        // Mentor entfernen
        [HttpPost]
        public async Task<IActionResult> RemoveMentorFromTrainee(string traineeProfileId, string mentorProfileId)
        {
            var traineeProfile = await _context.TraineeProfiles
                .Include(t => t.Mentoren)
                .FirstOrDefaultAsync(t => t.ApplicationUserId == traineeProfileId);
            var mentorProfile = await _context.MentorProfiles
                .FirstOrDefaultAsync(m => m.ApplicationUserId == mentorProfileId);

            if (traineeProfile != null && mentorProfile != null)
            {
                traineeProfile.Mentoren.Remove(mentorProfile);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("AsignMentorToTrainee", new { traineeProfileId = traineeProfileId });
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Benutzer nicht gefunden.");

            // Rolle ermitteln
            var roles = await _userManager.GetRolesAsync(user);
            var rolle = roles.FirstOrDefault() ?? "Keine";

            // Zusatzdaten laden, falls es ein Trainee oder Mentor ist
            if (rolle == "Trainee")
            {
                var traineeProfile = await _context.TraineeProfiles
                    .FirstOrDefaultAsync(t => t.ApplicationUserId == userId);
                if (traineeProfile != null)
                {
                    ViewData["StartDate"] = traineeProfile.StartDate.ToString("yyyy-MM-dd");
                    ViewData["EndDate"] = traineeProfile.EndDate.ToString("yyyy-MM-dd");
                }
            }

            ViewData["Rolle"] = rolle;
            return View("EditUser", user);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(string id, string vorname, string nachname, string email, string newPassword, DateTime? startDate, DateTime? endDate)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // 1. Stammdaten aktualisieren
            user.Vorname = vorname;
            user.Nachname = nachname;
            user.Email = email;
            user.UserName = email; // Identity nutzt Email oft als Username

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return BadRequest("Fehler beim Aktualisieren der Stammdaten.");
            }

            // Passwort optional ändern (nur wenn etwas eingegeben wurde)
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                // Altes Passwort entfernen ohne Validierung des alten Passworts (da Admin-Recht)
                await _userManager.RemovePasswordAsync(user);
                await _userManager.AddPasswordAsync(user, newPassword);
            }

            // Rolle prüfen und Trainee-Daten aktualisieren
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Trainee") && startDate.HasValue && endDate.HasValue)
            {
                var traineeProfile = await _context.TraineeProfiles
                    .FirstOrDefaultAsync(t => t.ApplicationUserId == id);
                if (traineeProfile != null)
                {
                    traineeProfile.StartDate = startDate.Value;
                    traineeProfile.EndDate = endDate.Value;
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("UserList");
        }
    }
}