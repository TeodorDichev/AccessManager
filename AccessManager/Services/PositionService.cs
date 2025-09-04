using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Utills;
using AccessManager.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AccessManager.Services
{
    public class PositionService
    {
        private readonly Context _context;
        public PositionService(Context context)
        {
            _context = context;
        }

        internal Position? GetPosition(Guid? id)
        {
            return _context.Positions.FirstOrDefault(d => d.Id == id);
        }

        internal bool ExistsPositionWithDescription(string name)
        {
            return _context.Positions.Any(d => d.Description == name);
        }

        internal void UpdatePositionDescription(Position position, string name)
        {
            position.Description = name;
            _context.SaveChanges();
        }

        internal Position CreatePosition(string name)
        {
            Position directive = new Position
            {
                Id = Guid.NewGuid(),
                Description = name
            };

            _context.Positions.Add(directive);
            _context.SaveChanges();

            return directive;
        }

        internal int GetPositionsCount(int page)
        {
            return _context.Positions.Count();
        }

        internal List<Position> GetPositions()
        {
            return _context.Positions
                .OrderBy(d => d.Description)
                .ToList();
        }

        internal PagedResult<Position> GetPositionsPaged(int page)
        {
            if (page < 1) page = 1;

            return new PagedResult<Position>
            {
                Items = _context.Positions
                .OrderBy(d => d.Id)
                .Skip((page - 1) * Constants.ItemsPerPage)
                .Take(Constants.ItemsPerPage)
                .ToList(),
                TotalCount = _context.Positions.Count(),
                Page = page
            };
        }

        internal void DeletePosition(Position directive)
        {
            var timestamp = DateTime.Now;

            _context.Positions
                .Where(d => d.Id == directive.Id)
                .ExecuteDelete();
        }
    }
}
