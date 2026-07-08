using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TraineeTracker.Web.Data;
using TraineeTracker.Web.Models;

namespace TraineeTracker.Web.Repositories
{
    public class DatabaseUserRepository : IUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UniversalDbContext _context;

        public DatabaseUserRepository(UserManager<ApplicationUser> userManager, UniversalDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // Checkt, ob es einen User schon gibt (entweder über die ID oder die E-Mail)
        // wichtig für die Validierung im AdminController, bevor wir Duplikate anlegen
        public async Task<bool> ExistsAsync(ApplicationUser user)
        {
            if (user == null) return false;
            return await _userManager.Users.AnyAsync(u => u.Id == user.Id || u.Email == user.Email);
        }

        // Legt einen neuen User im System an
        // Wir geben einfach ein 'true' zurück, wenn Microsoft Identity das Erstellen erlaubt
        public async Task<bool> CreateAsync(ApplicationUser user)
        {
            var result = await _userManager.CreateAsync(user);
            return result.Succeeded;
        }

        // Aktualisiert die Benutzerdaten (wird z.B. gefeuert, wenn wir den Account auf 'IstAktiv = false' setzen)
        public async Task<bool> UpdateAsync(ApplicationUser user)
        {
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        // Falls wir doch mal einen User komplett löschen müssen
        // Werden wir, glaube ich, nicht brauchen, denn wir wollen alle User archivieren
        public async Task<bool> DeleteAsync(ApplicationUser user)
        {
            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        // eigentlich unnötig, denn CreateAsync() usw. rufen automatisch SaveChangesAsync() auf
        // aber ich lasse das, damit es mit dem Designklassendiagramm konsistent ist
        public async Task<bool> SaveAsync(ApplicationUser user)
        {
            var tracking = _context.ChangeTracker.Entries<ApplicationUser>();
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
    }
}