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
    public class DeleteModel : PageModel
    {
        private readonly Mcl959DbContext _context;

        public DeleteModel(Mcl959DbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MessagesModel Message { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var message = await _context.Messages.FirstOrDefaultAsync(m => m.Id == id);
            if (message == null)
            {
                return NotFound();
            }
            else
            {
                Message = message;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var message = await _context.Messages.FindAsync(id);
            if (message != null)
            {
                Message = message;
                _context.Messages.Remove(Message);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Index");
        }
    }
}
