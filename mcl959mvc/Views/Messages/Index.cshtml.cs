using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using mcl959mvc.Models;

namespace mcl959mvc.Views.Messages
{
    public class IndexModel : PageModel
    {
        private readonly Mcl959DbContext _context;

        public IndexModel(Mcl959DbContext context)
        {
            _context = context;
        }

        public IList<Message> Message { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Message = await _context.Messages.ToListAsync();
        }
    }
}
