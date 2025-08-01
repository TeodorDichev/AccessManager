using AccessManager.Data;
using AccessManager.Data.Entities;

namespace AccessManager.Services
{
    public class InformationSystemsService
    {
        private readonly Context _context;
        public InformationSystemsService(Context context)
        {
            _context = context;
        }

        public List<InformationSystem> GetAllInformationSystems()
        {
            return _context.InformationSystems.Where(s => s.DeletedOn == null).ToList();
        }
    }
}
