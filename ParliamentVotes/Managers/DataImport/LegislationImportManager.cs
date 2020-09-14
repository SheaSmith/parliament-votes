using ParliamentVotes.Data;
using ParliamentVotes.Models.Organisational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Managers.DataImport
{
    public class LegislationImportManager
    {
        private readonly ApplicationDbContext db;

        public LegislationImportManager(ApplicationDbContext db)
        {
            this.db = db;
        }


        public async Task ImportBills(Parliament parliament)
        {

        }
    }
}
