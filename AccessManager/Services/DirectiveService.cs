using AccessManager.Data;
using AccessManager.Data.Entities;

namespace AccessManager.Services
{
    public class DirectiveService
    {
        private readonly Context _context;
        public DirectiveService(Context context)
        {
            _context = context;
        }


        internal Directive? GetDirective(Guid id)
        {
            return _context.Directives.FirstOrDefault(d => d.Id == id);
        }

        internal void UpdateDirectiveName(Directive directive, string name)
        {
            if (directive != null)
            {
                directive.Name = name;
                _context.SaveChanges();
            }
        }

        internal void CreateDirective(string name)
        {
            if (!_context.Directives.Any(d => d.Name == name))
            {
                _context.Directives.Add(new Directive
                {
                    Id = Guid.NewGuid(),
                    Name = name
                });
                _context.SaveChanges();
            }

        }

        internal void DeleteDirective(Directive directive)
        {
            _context.Directives.Remove(directive);
            _context.SaveChanges();
        }
        internal void UpdateAccessDirective(UserAccess access, Guid directiveId)
        {
            if (_context.Directives.Any(d => d.Id == directiveId))
                access.GrantedByDirectiveId = directiveId;
        }

        internal List<Directive> GetDirectives()
        {
            return _context.Directives.ToList();
        }
    }
}
