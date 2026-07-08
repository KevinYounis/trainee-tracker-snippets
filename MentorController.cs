using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TraineeTracker.Web.Data;
using TraineeTracker.Web.Models;
using TraineeTracker.Web.Repositories;

namespace TraineeTracker.Web.Controllers
{
    [Authorize(Roles = "Mentor,Admin")]
    public class MentorController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly UniversalDbContext _context;
        private readonly ITraineeLektionRepository traineeLektionRepository;

        public MentorController(IUserRepository userRepository, UniversalDbContext context, ITraineeLektionRepository traineeLektionRepository)
        {
            _userRepository = userRepository;
            _context = context;
            this.traineeLektionRepository = traineeLektionRepository;
        }

        public IActionResult Index(int traineeId = 1)
        {
            ViewBag.Trainees = WorkflowDemoData.Trainees;
            ViewBag.SelectedTraineeId = traineeId;

            List<TraineeLektion> traineeLektionen = WorkflowDemoData.TraineeLektionen
                .Where(traineeLektion => traineeLektion.TraineeId == traineeId)
                .OrderBy(traineeLektion => traineeLektion.Lektion.StandardPosition)
                .ToList();

            return View(traineeLektionen);
        }

        public IActionResult Review(int id)
        {
            TraineeLektion? traineeLektion = traineeLektionRepository.GetById(id);

            if (traineeLektion == null)
            {
                return NotFound();
            }

            return View(traineeLektion);
        }

        [HttpPost]
        public IActionResult Accept(int id)
        {
            TraineeLektion? traineeLektion = traineeLektionRepository.GetById(id);

            if (traineeLektion == null)
            {
                return NotFound();
            }

            evaluateLektion(id, string.Empty);
            return RedirectToAction(nameof(Index), new { traineeId = traineeLektion.TraineeId });
        }

        public IActionResult RejectForm(int id)
        {
            TraineeLektion? traineeLektion = traineeLektionRepository.GetById(id);

            if (traineeLektion == null)
            {
                return NotFound();
            }

            return View(traineeLektion);
        }

        [HttpPost]
        public IActionResult Reject(int id, string reason)
        {
            TraineeLektion? traineeLektion = traineeLektionRepository.GetById(id);

            if (traineeLektion == null)
            {
                return NotFound();
            }

            if (!proofRejectionReason(reason))
            {
                ModelState.AddModelError(nameof(reason), "Bitte gib eine nachvollziehbare Ablehnungsbegründung ein.");
                return View("RejectForm", traineeLektion);
            }

            rejectLektion(id, reason);
            return RedirectToAction(nameof(Index), new { traineeId = traineeLektion.TraineeId });
        }

        [NonAction]
        public bool evaluateLektion(int traineeLektionId, string reason)
        {
            TraineeLektion? traineeLektion = traineeLektionRepository.GetById(traineeLektionId);

            if (traineeLektion == null || traineeLektion.State != EditStatus.finished)
            {
                return false;
            }

            traineeLektion.State = EditStatus.accepted;
            traineeLektion.Reason = string.Empty;
            return traineeLektionRepository.update(traineeLektion);
        }

        [NonAction]
        public bool proofRejectionReason(string reason)
        {
            return !string.IsNullOrWhiteSpace(reason) && reason.Trim().Length >= 10;
        }

        [NonAction]
        public bool rejectLektion(int traineeLektionId, string reason)
        {
            TraineeLektion? traineeLektion = traineeLektionRepository.GetById(traineeLektionId);

            if (traineeLektion == null || traineeLektion.State != EditStatus.finished || !proofRejectionReason(reason))
            {
                return false;
            }

            traineeLektion.State = EditStatus.rejected;
            traineeLektion.Reason = reason.Trim();
            return traineeLektionRepository.update(traineeLektion);
        }

        [NonAction]
        public bool ProofRejectionReason(string reason)
        {
            return proofRejectionReason(reason);
        }

        [HttpPost]
        public async Task<IActionResult> RejectLektion(int lektionId, string reason)
        {
            if (!ProofRejectionReason(reason))
            {
                ModelState.AddModelError("Reason", "Die Ablehnungsbegründung ist zu kurz.");
                return BadRequest("Die Ablehnungsbegründung ist zu kurz.");
            }

            var traineeLektion = await _context.TraineeProfiles
                .SelectMany(t => t.Trainee_Lektionen)
                .FirstOrDefaultAsync(l => l.Id == lektionId);

            if (traineeLektion != null)
            {
                traineeLektion.Status = "rejected";
                traineeLektion.Reason = reason;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> EvaluateLektion(int lektionId, string feedbackText)
        {
            var traineeLektion = await _context.TraineeProfiles
                .SelectMany(t => t.Trainee_Lektionen)
                .FirstOrDefaultAsync(l => l.Id == lektionId);

            if (traineeLektion != null)
            {
                traineeLektion.Status = "accepted";
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ReleaseFeedbackSubmission(int lektionId)
        {
            var traineeLektion = await _context.TraineeProfiles
                .SelectMany(t => t.Trainee_Lektionen)
                .FirstOrDefaultAsync(l => l.Id == lektionId);

            if (traineeLektion != null)
            {
                traineeLektion.Status = "rated";
                await _context.SaveChangesAsync();
                return Json(true);
            }

            return Json(false);
        }
    }
}
