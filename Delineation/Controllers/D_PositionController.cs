using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Delineation.Models;
using Microsoft.AspNetCore.Authorization;
using Delineation.Data;

namespace Delineation.Controllers
{
    [Authorize(Roles = "admin")]
    public class D_PositionController : Controller
    {
        private readonly DelineationContext _context;

        public D_PositionController(DelineationContext context)
        {
            _context = context;
        }

        // GET: D_Position
        public async Task<IActionResult> Index()
        {
            return View(await _context.D_Position.ToListAsync());
        }

        // GET: D_Position/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var d_Position = await _context.D_Position
                .FirstOrDefaultAsync(m => m.Id == id);
            if (d_Position == null)
            {
                return NotFound();
            }

            return View(d_Position);
        }

        // GET: D_Position/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: D_Position/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] D_Position d_Position)
        {
            if (ModelState.IsValid)
            {
                _context.Add(d_Position);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(d_Position);
        }

        // GET: D_Position/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var d_Position = await _context.D_Position.FindAsync(id);
            if (d_Position == null)
            {
                return NotFound();
            }
            return View(d_Position);
        }

        // POST: D_Position/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] D_Position d_Position)
        {
            if (id != d_Position.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(d_Position);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!D_PositionExists(d_Position.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(d_Position);
        }

        // GET: D_Position/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var d_Position = await _context.D_Position
                .FirstOrDefaultAsync(m => m.Id == id);
            if (d_Position == null)
            {
                return NotFound();
            }

            return View(d_Position);
        }

        // POST: D_Position/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var d_Position = await _context.D_Position.FindAsync(id);
            _context.D_Position.Remove(d_Position);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool D_PositionExists(int id)
        {
            return _context.D_Position.Any(e => e.Id == id);
        }
    }
}
