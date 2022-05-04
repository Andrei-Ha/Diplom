using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Delineation.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Delineation.Data;

namespace Delineation.Controllers
{
    [Authorize(Roles = "admin")]
    public class D_PersonController : Controller
    {
        private readonly DelineationContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _defaultConnection;
        public D_PersonController(DelineationContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _defaultConnection = _configuration.GetConnectionString("DefaultConnection");
        }

        // GET: D_Person
        public async Task<IActionResult> Index()
        {
            return View(await _context.D_Persons.Include(p=>p.Position).ToListAsync());
        }

        // GET: D_Person/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var d_Person = await _context.D_Persons.Include(o=>o.Position)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (d_Person == null)
            {
                return NotFound();
            }

            return View(d_Person);
        }

        // GET: D_Person/Create
        public IActionResult Create()
        {
            List<SelList> myList = new List<SelList>();
            using (SqliteConnection con = new SqliteConnection(_defaultConnection))
            {
                using (SqliteCommand cmd = con.CreateCommand())
                {
                    con.Open();
                    cmd.CommandText = "select FIO, NAME1, NAME2, (cex || cex1) as KOD, LINOM from delo_s_fio order by fio,name1,name2";
                    SqliteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        myList.Add(new SelList() { Id = reader["FIO"].ToString() + ";" + reader["NAME1"].ToString() + ";" + reader["NAME2"].ToString() + ";" + reader["KOD"].ToString() + ";" + reader["LINOM"].ToString(), Text = reader["FIO"].ToString() + " " + reader["NAME1"].ToString() + " " + reader["NAME2"].ToString() + " " + reader["KOD"].ToString() });
                    }
                    reader.Dispose();
                }
            }
            ViewData["FIO"] = new SelectList(myList, "Id", "Text");
            var positions = _context.Positions.ToList();
            ViewData["Positions"] = new SelectList(positions, "Id", "Name");
            return View();
        }

        // POST: D_Person/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string strFIO, int PositionId)
        {
            if(string.IsNullOrEmpty(strFIO))
            {
                return RedirectToAction(nameof(Create));
            }
            D_Person person = new D_Person()
            {
                Surname = strFIO.Split(';')[0],
                Name = strFIO.Split(';')[1],
                Patronymic = strFIO.Split(';')[2],
                Kod_long = strFIO.Split(';')[3],
                Linom = strFIO.Split(';')[4],
                PositionId = PositionId
            };
            _context.Add(person);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }

        // GET: D_Person/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var d_Person = await _context.D_Persons.FindAsync(id);
            if (d_Person == null)
            {
                return NotFound();
            }
            var positions = _context.Positions.ToList();
            ViewData["Positions"] = new SelectList(positions, "Id", "Name",d_Person.PositionId);
            return View(d_Person);
        }

        // POST: D_Person/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Surname,Name,Patronymic,PositionId")] D_Person d_Person)
        {
            if (id != d_Person.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(d_Person);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!D_PersonExists(d_Person.Id))
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
            return View(d_Person);
        }

        // GET: D_Person/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var d_Person = await _context.D_Persons
                .FirstOrDefaultAsync(m => m.Id == id);
            if (d_Person == null)
            {
                return NotFound();
            }

            return View(d_Person);
        }

        // POST: D_Person/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var d_Person = await _context.D_Persons.FindAsync(id);
            var reses = await _context.D_Reses.Where(r => r.NachId == id || r.ZamNachId == id || r.GlInzhId == id || r.BuhId == id).ToListAsync();
            
            if (d_Person != null)
            {
                _context.D_Persons.Remove(d_Person);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool D_PersonExists(int id)
        {
            return _context.D_Persons.Any(e => e.Id == id);
        }
    }
}
