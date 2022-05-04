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
    public class D_ResController : Controller
    {
        private readonly DelineationContext _context;

        public D_ResController(DelineationContext context)
        {
            _context = context;
        }

        // GET: D_Res
        public async Task<IActionResult> Index()
        {
            var delineationContext = _context.D_Reses.Include(d => d.Buh).Include(d => d.GlInzh).Include(d => d.Nach).Include(d => d.ZamNach);
            return View(await delineationContext.ToListAsync());
        }

        // GET: D_Res/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var d_Res = await _context.D_Reses
                .Include(d => d.Buh)
                .Include(d => d.GlInzh)
                .Include(d => d.Nach)
                .Include(d => d.ZamNach)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (d_Res == null)
            {
                return NotFound();
            }

            return View(d_Res);
        }

        // GET: D_Res/Create
        public IActionResult Create()
        {
            var person_list = _context.D_Persons.ToList().Select(p => new { Id = p.Id, FIO = p.Surname + " " + p.Name + " " + p.Patronymic });
            ViewData["BuhId"] = new SelectList(person_list, "Id", "FIO");
            ViewData["GlInzhId"] = new SelectList(person_list, "Id", "FIO");
            ViewData["NachId"] = new SelectList(person_list, "Id", "FIO");
            ViewData["ZamNachId"] = new SelectList(person_list, "Id", "FIO");
            return View();
        }

        // POST: D_Res/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,NachId,ZamNachId,GlInzhId,BuhId,City,RESa,RESom,FIOnachRod,Dover")] D_Res d_Res)
        {
            if (ModelState.IsValid)
            {
                _context.Add(d_Res);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            var person_list = _context.D_Persons.ToList().Select(p => new { Id = p.Id, FIO = p.Surname + " " + p.Name + " " + p.Patronymic });
            ViewData["BuhId"] = new SelectList(person_list, "Id", "FIO", d_Res.BuhId);
            ViewData["GlInzhId"] = new SelectList(person_list, "Id", "FIO", d_Res.GlInzhId);
            ViewData["NachId"] = new SelectList(person_list, "Id", "FIO", d_Res.NachId);
            ViewData["ZamNachId"] = new SelectList(person_list, "Id", "FIO", d_Res.ZamNachId);
            return View(d_Res);
        }

        // GET: D_Res/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var d_Res = await _context.D_Reses.FindAsync(id);
            if (d_Res == null)
            {
                return NotFound();
            }
            var person_list = _context.D_Persons.ToList().Select(p => new { Id = p.Id, FIO = p.Surname + " " + p.Name + " " + p.Patronymic });
            ViewData["BuhId"] = new SelectList(person_list, "Id", "FIO", d_Res.BuhId);
            ViewData["GlInzhId"] = new SelectList(person_list, "Id", "FIO", d_Res.GlInzhId);
            ViewData["NachId"] = new SelectList(person_list, "Id", "FIO", d_Res.NachId);
            ViewData["ZamNachId"] = new SelectList(person_list, "Id", "FIO", d_Res.ZamNachId);
            return View(d_Res);
        }

        // POST: D_Res/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,NachId,ZamNachId,GlInzhId,BuhId,City,RESa,RESom,FIOnachRod,Dover")] D_Res d_Res)
        {
            if (id != d_Res.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(d_Res);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!D_ResExists(d_Res.Id))
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
            var person_list = _context.D_Persons.ToList().Select(p => new { Id = p.Id, FIO = p.Surname + " " + p.Name + " " + p.Patronymic });
            ViewData["BuhId"] = new SelectList(person_list, "Id", "FIO", d_Res.BuhId);
            ViewData["GlInzhId"] = new SelectList(person_list, "Id", "FIO", d_Res.GlInzhId);
            ViewData["NachId"] = new SelectList(person_list, "Id", "FIO", d_Res.NachId);
            ViewData["ZamNachId"] = new SelectList(person_list, "Id", "FIO", d_Res.ZamNachId);
            return View(d_Res);
        }

        // GET: D_Res/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var d_Res = await _context.D_Reses
                .Include(d => d.Buh)
                .Include(d => d.GlInzh)
                .Include(d => d.Nach)
                .Include(d => d.ZamNach)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (d_Res == null)
            {
                return NotFound();
            }

            return View(d_Res);
        }

        // POST: D_Res/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var d_Res = await _context.D_Reses.FindAsync(id);
            _context.D_Reses.Remove(d_Res);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool D_ResExists(int id)
        {
            return _context.D_Reses.Any(e => e.Id == id);
        }
    }
}
