using System.Threading.Tasks;
using TraineeTracker.Web.Models;

namespace TraineeTracker.Web.Repositories
{
    public interface IUserRepository
    {
        Task<bool> ExistsAsync(ApplicationUser user);
        Task<bool> UpdateAsync(ApplicationUser user);
        Task<bool> DeleteAsync(ApplicationUser user);
        Task<bool> CreateAsync(ApplicationUser user);

        // eigentlich unnötig, denn CreateAsync() usw. rufen automatisch SaveChangesAsync() auf
        // aber ich lasse das, damit es mit dem Designklassendiagramm konsistent ist
        Task<bool> SaveAsync(ApplicationUser user); 
    }
}