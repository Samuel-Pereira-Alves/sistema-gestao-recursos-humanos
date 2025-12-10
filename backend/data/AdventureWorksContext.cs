namespace sistema_gestao_recursos_humanos.backend.data
{
    using Microsoft.EntityFrameworkCore;

    public class AdventureWorksContext  : DbContext
    {
        public AdventureWorksContext (DbContextOptions<AdventureWorksContext > options) : base(options)
        {
        }

    }
}