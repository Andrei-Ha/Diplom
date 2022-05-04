using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Delineation.Models;
using GemBox.Document;
using GemBox.Document.Tables;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using System.Net;
using NPetrovich;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using CustomIdentity.Models;
using Delineation.ViewModels;
using Delineation.Services;
using Delineation.Data;

namespace Delineation.Controllers
{
    public class D_ActController : Controller
    {
        private readonly DelineationContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly string _defaultConnection;
        private readonly EmailService _emailService;

        public D_ActController(DelineationContext context, IWebHostEnvironment webHostEnvironment, IConfiguration configuration, EmailService emailService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _defaultConnection = _configuration.GetConnectionString("DefaultConnection");
            _emailService = emailService;
        }

        //[Authorize(Roles = "D_accepter")]
        public async Task<IActionResult> Agreement(int? id)
        {
            if (id != null)
            {
                var d_Act = await _context.D_Acts.Include(p => p.Tc).FirstOrDefaultAsync(p => p.Id == id);
                ViewBag.Agreements = _context.D_Agreements.Include(p=>p.Person).ThenInclude(o=>o.Position).Where(p => p.ActId == id).ToList();
                ViewBag.LinomUser = _context.Users.FirstOrDefault(p=>p.UserName==User.Identity.Name)?.Linom.ToString();
                return View(d_Act);
            }
            else
            {
                return NotFound();
            }
        }

        [Authorize(Roles = "D_accepter")]
        [HttpPost]
        public async Task<IActionResult> Agreement(int Act_id, int agree, int Agr_id, string Note)
        {
            D_Agreement d_Agreement = await _context.D_Agreements.FirstOrDefaultAsync(o => o.Id == Agr_id);
            d_Agreement.Note = Note;
            d_Agreement.Info = DateTime.Now.ToString("HH:mm dd.MM.yyyy") + "/" + Request.HttpContext.Connection.RemoteIpAddress.ToString();
            if (agree == 0)
                d_Agreement.Accept = false;
            else if (agree == 1)
                d_Agreement.Accept = true;
            _context.SaveChanges();
            D_Act d_Act = await _context.D_Acts.Include(p=>p.Agreements).FirstOrDefaultAsync(p => p.Id == Act_id);
            if (d_Act.Agreements.All(p => p.Info != null))
            {
                if (d_Act.Agreements.All(p => p.Accept == true))
                {
                    d_Act.State = (int)Stat.Accepted;
                    await _context.SaveChangesAsync();
                    // вытягивание всех данных Акта для формирования печатной версии
                    _context.D_Persons.Load();
                    D_Act act = await _context.D_Acts.Include(p => p.Tc).ThenInclude(o => o.Res).FirstOrDefaultAsync(i => i.Id == Act_id);
                    CreateDoc(act, "completed");
                }
                else
                    d_Act.State = (int)Stat.Rework;
                    await _context.SaveChangesAsync();
            }
            return RedirectToAction("Ind_agree");
        }

        [Authorize(Roles = "D_operator")]
        public async Task<IActionResult> Drawing(int? id )
        {
            List<string> list_svg = new List<string>();
            if (id != null && id != 0)
            {
                string path_svg = _webHostEnvironment.WebRootPath + "\\Output\\svg\\" + id.ToString() + ".svg";
                FileInfo MySvg = new FileInfo(path_svg);
                if (MySvg.Exists)
                {
                    using StreamReader sr = new StreamReader(path_svg, System.Text.Encoding.Default);
                    string line;
                    bool content = false;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.IndexOf("konec") > -1)
                        {
                            content = false;
                            list_svg.Add(line.Substring(0, line.IndexOf("<rect id=\"konec\"")));
                        }
                        if (content) list_svg.Add(line);
                        if (line.IndexOf("nachalo") > -1) content = true;
                    }
                }
                ViewBag.list_svg = list_svg;
                string path = _webHostEnvironment.WebRootPath + "\\Output\\images\\";
                DirectoryInfo dir_image = new DirectoryInfo(path);
                List<FileInfo> list_files = dir_image.EnumerateFiles().Where(p => p.Name.Split('.')[0] == id.ToString()).ToList();
                foreach (FileInfo fileInfo in list_files)
                {
                    if (fileInfo.Exists) ViewBag.FileName = fileInfo.Name;
                }
                D_Act d_Act = await _context.D_Acts.Include(p => p.Tc).FirstOrDefaultAsync(o => o.Id == id);
                string path_vsd = _webHostEnvironment.WebRootPath + "\\Output\\vsd\\" + d_Act.Tc.TPnum + ".vsd",
                    path_lvsd = _webHostEnvironment.WebRootPath + "\\Output\\vsd038\\l" + d_Act.Tc.TPnum + ".vsd";
                FileInfo vsd = new FileInfo(path_vsd);
                FileInfo lvsd = new FileInfo(path_lvsd);
                if (vsd.Exists) ViewBag.vsd = d_Act.Tc.TPnum + ".vsd";
                if (lvsd.Exists) ViewBag.lvsd = "l" + d_Act.Tc.TPnum + ".vsd";
                return View(d_Act);
            }
            else
            {
                return NotFound();
            }
        }

        private void DeleteAllImageById(int id,string ext)
        {
            string path = _webHostEnvironment.WebRootPath + "\\Output\\images\\";
            DirectoryInfo dir_image = new DirectoryInfo(path);
            List<FileInfo> list_files = dir_image.EnumerateFiles().Where(p => p.Name.Split('.')[0] == id.ToString() && p.Name.Split('.')[1]!= ext).ToList();
            foreach (FileInfo fileInfo in list_files)
            {
                if (fileInfo.Exists) fileInfo.Delete();
            }
            //List<string> list_ext0 = new List<string>( new string[] { "png", "jpg", "jpeg" });
            //List<string> list_ext = new List<string> { ".png", ".jpg", ".jpeg" };
        }

        [Authorize(Roles = "D_operator")]
        [HttpPost]
        //[RequestSizeLimit(40000000)]
        public IActionResult SavePNG(string png, string svg, string raw, int id)
        {
            var decodeURL_svg = WebUtility.UrlDecode(svg);
            var base64Data_svg = decodeURL_svg.Split(',');
            string path_svg = _webHostEnvironment.WebRootPath + "\\Output\\svg\\"+id.ToString()+".svg";
            using (FileStream fs = new FileStream(path_svg, FileMode.Create))
            {
                using BinaryWriter bw = new BinaryWriter(fs);
                byte[] data = Convert.FromBase64String(base64Data_svg[1]);
                bw.Write(data);
            }
            //---
            var decodeURL_png = WebUtility.UrlDecode(png);
            var base64Data_png = decodeURL_png.Split(',');
            string path_png = _webHostEnvironment.WebRootPath + "\\Output\\png\\" + id.ToString() + ".png";
            using (FileStream fs = new FileStream(path_png, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    byte[] data = Convert.FromBase64String(base64Data_png[1]);
                    bw.Write(data);
                }
            }
            //---
            if (raw != "0" && raw != "1") // 0 - удаление; 1- без изменений(картинка существует); иначе - пишем в поток
            {
                var decodeURL_raw = WebUtility.UrlDecode(raw);
                var base64Data_raw = decodeURL_raw.Split(',');
                string ext = base64Data_raw[0].Split('/')[1].Split(';')[0];
                string path_raw = _webHostEnvironment.WebRootPath + "\\Output\\images\\" + id.ToString() + "." + ext;
                var allowedExt = new[] { "png", "jpg", "jpeg" };
                if (allowedExt.Any(p => p == ext.ToLower())) // проверка расширения
                {
                    using (FileStream fs = new FileStream(path_raw, FileMode.Create))
                    {
                        using (BinaryWriter bw = new BinaryWriter(fs))
                        {
                            DeleteAllImageById(id, ext);
                            byte[] data = Convert.FromBase64String(base64Data_raw[1]);
                            bw.Write(data);
                        }
                    }
                }
            }
            else { if(raw == "0") DeleteAllImageById(id, "0"); }
            var d_Act = _context.D_Acts.Include(p => p.Tc).ThenInclude(p => p.Res).FirstOrDefault(p => p.Id == id);
            return RedirectToAction(nameof(Details),new { id=id});
        }

        // GET: D_Act
        public async Task<IActionResult> Index( int? res, string fio, int page=1, ASortState sortOrder = ASortState.DateAsc)
        {
            ViewBag.sprpodrs = _context.Units.ToList();
            int pageSize = 5;
            //фильтрация
            IQueryable<D_Act> acts = _context.D_Acts.Include(d => d.Tc).ThenInclude(p => p.Res).ThenInclude(p => p.Nach).Where(p => p.State == (int)Stat.Completed);

            if (res != null && res != 0)
            {
                acts = acts.Where(p => p.Tc.ResId == res);
            }
            if (!String.IsNullOrEmpty(fio))
            {
                acts = acts.Where(p => p.Tc.FIO.Contains(fio));
            }
            // сортировка
            switch (sortOrder)
            {
                case ASortState.DateDesc:
                    acts = acts.OrderByDescending(s => s.Date);
                    break;
                case ASortState.ResAsc:
                    acts = acts.OrderBy(s => s.Tc.Res.Name);
                    break;
                case ASortState.ResDesc:
                    acts = acts.OrderByDescending(s => s.Tc.Res.Name);
                    break;
                case ASortState.FioAsc:
                    acts = acts.OrderBy(s => s.Tc.FIO);
                    break;
                case ASortState.FioDesc:
                    acts = acts.OrderByDescending(s => s.Tc.FIO);
                    break;
                default:
                    acts = acts.OrderBy(s => s.Date);
                    break;
            }

            // пагинация
            var count = await acts.CountAsync();
            var items = await acts.Skip((page - 1)*pageSize).Take(pageSize).ToListAsync();

            // формируем модель представления
            ActsIndexViewModel viewModel = new ActsIndexViewModel
            {
                ActsPageViewModel = new ActsPageViewModel(count, page, pageSize),
                ActsSortViewModel = new ActsSortViewModel(sortOrder),
                ActsFilterViewModel = new ActsFilterViewModel(_context.D_Reses.Select(p=>new SelList { Id = p.Id.ToString(), Text = p.Name }).ToList(), res, fio),
                Acts = items
            };
            return View(viewModel);
            /*string path_dir_pdf = _webHostEnvironment.WebRootPath + "\\Output\\pdf_signed";
            DirectoryInfo directory = new DirectoryInfo(path_dir_pdf);
            List<string> fileNames = directory.GetFiles().Select(p=>p.Name.Split('.')[0]).ToList();
            ViewBag.fileNames = fileNames;*/
        }

        //[Authorize(Roles = "D_accepter")]
        public async Task<IActionResult> Ind_agree()
        {
            var delineationContext = _context.D_Acts.Include(d => d.Tc).ThenInclude(p => p.Res).ThenInclude(p => p.Nach).Where(p => p.State == (int)Stat.InAgreement);
            return View(await delineationContext.ToListAsync());
        }

        public async Task<IActionResult> Ind_edit()
        {
            string path_dir_pdf = _webHostEnvironment.WebRootPath + "\\Output\\pdf";
            DirectoryInfo directory = new DirectoryInfo(path_dir_pdf);
            List<string> fileNames = directory.GetFiles().Select(p => p.Name.Split('.')[0]).ToList();
            ViewBag.fileNames = fileNames;
            var delineationContext = _context.D_Acts.Include(d => d.Tc).ThenInclude(p => p.Res).ThenInclude(p => p.Nach).Where(p=>(p.State==(int)Stat.Edit)|| (p.State == (int)Stat.InAgreement)|| (p.State == (int)Stat.Rework) || (p.State == (int)Stat.Accepted)).OrderBy(o=>o.State);
            return View(await delineationContext.ToListAsync());
        }

        // GET: D_Act/Details/5
        [Authorize(Roles = "D_operator")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            else
            {
                string path_svg = _webHostEnvironment.WebRootPath + "\\Output\\png\\" + id.ToString() + ".png";
                FileInfo MyPng = new FileInfo(path_svg);
                if (MyPng.Exists) { ViewBag.fileName = MyPng.Name; }
            }
            var d_Act = await _context.D_Acts
                .Include(d => d.Tc).ThenInclude(d => d.Res).Include(o=>o.Agreements).ThenInclude(p=>p.Person).ThenInclude(o=>o.Position)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (d_Act == null)
            {
                return NotFound();
            }
            // !!!!!!!!!!!!!!!!!!!!!!!!!
            ViewBag.listPerson = _context.D_Persons.Include(p=>p.Position).Where(p => p.Kod_long == d_Act.Tc.ResId.ToString()).ToList();
                return View(d_Act);
        }

        //GET: D_Act/Create
        [Authorize(Roles = "D_operator")]
        public IActionResult Create(string Kluch)
        {
            List<SelList> myList = new List<SelList>();
            using (SqliteConnection con = new SqliteConnection(_defaultConnection))
            {
                using SqliteCommand cmd = con.CreateCommand();
                con.Open();
                // filter of TU list by podr
                string filterByPodr = string.Empty;
                //string podr = User.FindFirst("Podr")?.Value;
                //if (!string.IsNullOrEmpty(podr))
                //{
                //    filterByPodr = " and kod_podr = '" + podr +"'";
                //}
                //cmd.CommandText = "select tu_all.kluch,n_tu,CONVERT(VARCHAR(10),d_tu,104) s2,fio,adress_ob,naim from tu_all,sprpodr Where del=3 and n_akt = 0 and n_tu is not null and d_tu is not null and  CAST(kod AS NVARCHAR)+CAST(KOD_DOP AS NVARCHAR)=kod_podr and kod_podr='542000'";
                cmd.CommandText = "select tu_all.kluch as kluch,n_tu,strftime('%d.%m.%Y',d_tu) as s2,fio,adress_ob,sprpodr.naim as naim from tu_all,sprpodr Where n_akt = 0 and n_tu is not null and d_tu is not null and typ_tu=1 and CAST(kod AS NVARCHAR)||CAST(KOD_DOP AS NVARCHAR)=kod_podr" + filterByPodr;
                SqliteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    myList.Add(new SelList() { Id = reader["kluch"].ToString(), Text = "№" + reader["n_tu"].ToString() + " от " + reader["s2"].ToString() + "; " + reader["fio"].ToString() + "; " + reader["adress_ob"].ToString() + "; " + reader["naim"].ToString() });
                }
                reader.Dispose();
            }
            ViewData["TcId"] = new SelectList(myList, "Id", "Text",Kluch);
            //ViewData["TcId"] = new SelectList(_context.D_Tces.OrderBy(p => p.Date).Select(p => new { Id = p.Id, text = "№" + p.Num + " от " + p.Date.ToString("dd.MM.yyyy") + "; " + p.FIO + "; " + p.Address }), "Id", "text");
            return View();
        }

        [Authorize(Roles = "D_operator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int TcId)
        {
            if (await _context.D_Tces.FindAsync(TcId) != null)
                return RedirectToAction(nameof(Create));
            string str_numTP = "",str="";
            D_Tc tc = new D_Tc();
            using (SqliteConnection con = new SqliteConnection(_defaultConnection))
            {
                using (SqliteCommand cmd = con.CreateCommand())
                {
                    con.Open();
                    cmd.CommandText = "select kluch,n_tu, d_tu,kod_podr,fio,num_abonent,name_ob,adress_ob,p_all,name_ps,n_tp,typ_tp,n_vl,typ_vl,n_op,p_kat1,p_kat2,p_kat3 from tu_all where kluch=" + TcId.ToString();
                    SqliteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        tc.Id =Convert.ToInt32(reader["kluch"]);
                        tc.Num = reader["n_tu"].ToString();
                        if(reader["d_tu"].ToString().Length >= 10) tc.Date = DateTime.Parse(reader["d_tu"].ToString());
                        tc.ResId = Convert.ToInt32(reader["kod_podr"]);
                        tc.FIO = reader["fio"].ToString();
                        tc.AbonNum = reader["num_abonent"].ToString();
                        tc.ObjName = reader["name_ob"].ToString();
                        tc.Address = reader["adress_ob"].ToString();
                        tc.Pow = reader["p_all"].ToString();
                        tc.PS = reader["name_ps"].ToString();
                        tc.TPnum = Convert.ToInt32(reader["n_tp"]);
                        tc.TP = reader["typ_tp"].ToString() + "-" + reader["n_tp"].ToString();
                        tc.Line04 = reader["typ_vl"].ToString() + " " + reader["n_vl"].ToString();
                        tc.Pillar = reader["n_op"].ToString();
                        str_numTP = reader["n_tp"].ToString();
                        string cat = "";
                        for(int i=1;i<4;i++) // если значение в одной из категорий >0, номер соответствующей категории записывается в tc.Category через запятую
                            if (Convert.ToDecimal(reader["p_kat" + i.ToString()]) > 0) cat += i.ToString() + ",";
                        int ilength = cat.Length - 1;
                        tc.Category = cat.Substring(0, ilength);
                    }
                    reader.Dispose();
                }
            }
            //if (str_numTP != "") // обращение к базе Диполя
            //{
            //    string path_vsd = _webHostEnvironment.WebRootPath + "\\Output\\vsd\\" + str_numTP + ".vsd";
            //    string path_vsd038 = _webHostEnvironment.WebRootPath + "\\Output\\vsd038\\l" + str_numTP + ".vsd";
            //    using OracleConnection con = new OracleConnection(_configuration.GetConnectionString("Oracle" + tc.ResId));
            //    string doc_code = "";
            //    using OracleCommand cmd = con.CreateCommand();
            //    con.Open();
            //    cmd.CommandText = "select TP_NUM, doc_code, type_txt from TP,TP_SYS_TYPES where substation_type_id=id and substr(doc_code,TO_NUMBER(INSTR(doc_code,'-',1,2))+1)=" + str_numTP;
            //    OracleDataReader MyReader = cmd.ExecuteReader();
            //    using (MyReader)
            //    {
            //        while (MyReader.Read())
            //        {
            //            //tc.TP += ":" + MyReader["type_txt"].ToString().Replace("ЗТП","ТП")+ "-" +str_numTP + " (" + MyReader["TP_NUM"].ToString() +")";
            //            doc_code = MyReader["doc_code"].ToString();
            //        }
            //    }
            //    cmd.CommandText = "select INV_NUMB from TP_INV_NUMB where doc_code='" + doc_code + "'";
            //    MyReader = cmd.ExecuteReader();
            //    using (MyReader)
            //    {
            //        while (MyReader.Read())
            //        {
            //            tc.TPInvNum = Convert.ToInt32(MyReader["inv_numb"]);
            //        }
            //    }
            //    cmd.CommandText = "select line_doc_code, substation_id, vl10.substation_id as psid, p_name, p_voltage from TP_VL_10, vl10, psubstations where p_code=substation_id and vl10.doc_code=line_doc_code and substr(TP_VL_10.doc_code,TO_NUMBER(INSTR(TP_VL_10.doc_code,'-',1,2))+1)=" + str_numTP;
            //    MyReader = cmd.ExecuteReader();
            //    using (MyReader)
            //    {
            //        while (MyReader.Read())
            //        {
            //            str += "ВЛ 10кВ №" + MyReader["line_doc_code"].ToString().Split('-')[2] + ";" + "ПС " + MyReader["p_name"].ToString() + "-" + MyReader["p_voltage"].ToString() + "/";
            //        }
            //    }
            //    FileInfo file_vsd = new FileInfo(path_vsd);
            //    if (!file_vsd.Exists)
            //    //if(true)
            //    {
            //        cmd.CommandText = "select PFILE, DOC_CODE from TP_SHEM WHERE DOC_CODE='" + doc_code + "'";
            //        OracleDataReader MyReaderB = cmd.ExecuteReader();
            //        using (MyReaderB)
            //        {
            //            while (MyReaderB.Read())
            //            {
            //                OracleBinary oracleBinary = MyReaderB.GetOracleBinary(0);
            //                using (FileStream fstream = new FileStream(path_vsd, FileMode.OpenOrCreate))
            //                {
            //                    byte[] array = (byte[])oracleBinary;
            //                    fstream.Write(array, 0, array.Length);
            //                }
            //            }
            //        }
            //        string doc_code_vl0038 = doc_code.Split('-')[0] + "-CL038-" + doc_code.Split('-')[2];
            //        //не будет работать если номер отходящей линии двузначный
            //        cmd.CommandText = "select PFILE, DOC_CODE from CL038_VISIO WHERE SUBSTR(DOC_CODE,0,LENGTH(doc_code)-2)='" + doc_code_vl0038 + "'";
            //        MyReaderB = cmd.ExecuteReader();
            //        using (MyReaderB)
            //        {
            //            while (MyReaderB.Read())
            //            {
            //                OracleBinary oracleBinary = MyReaderB.GetOracleBinary(0);
            //                using (FileStream fstream = new FileStream(path_vsd038, FileMode.OpenOrCreate))
            //                {
            //                    byte[] array = (byte[])oracleBinary;
            //                    fstream.Write(array, 0, array.Length);
            //                }
            //            }
            //        }
            //    }
            //}
            _context.D_Tces.Add(tc);
            str = "ВЛ 10кВ №224; ПС ПИНСК-35";
            _context.D_Acts.Add(new D_Act { Tc=tc , StrPSline10=str, Info=FuncSetInfo("Create")});
            _context.SaveChanges();
            D_Act act = _context.D_Acts.ToList().LastOrDefault(p => p.TcId == TcId);
            return RedirectToAction(nameof(Edit), act);
        }

        // GET: D_Act/Edit/5
        [Authorize(Roles = "D_operator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var d_Act = await _context.D_Acts.Include(p=>p.Tc).ThenInclude(p=>p.Res).FirstOrDefaultAsync(p=>p.Id==id);
            if (d_Act == null)
            {
                return NotFound();
            }
            /*ViewData["TcId"] = new SelectList(_context.D_Tces.OrderBy(p=>p.Date).Select(p=>new { Id = p.Id, text = p.Num + " от " + p.Date.ToString("dd.MM.yyyy") + "; " + p.FIO + "; " + p.Address }), "Id", "text", d_Act.TcId);*/
            return View(d_Act);
        }

        // POST: D_Act/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "D_operator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string FIOcons, [Bind("Id,Date,TcId,IsEntity,EntityDoc,ConsBalance,DevBalance,ConsExpl,DevExpl,IsTransit,FIOtrans,Validity,Temp,StrPSline10")] D_Act d_Act)
        {
            if (id != d_Act.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    D_Tc d_Tc = _context.D_Tces.FirstOrDefault(p => p.Id == d_Act.TcId);
                    d_Tc.Line10 = d_Act.Temp.Split(';')[0];
                    d_Tc.PS = d_Act.Temp.Split(';')[1];
                    d_Tc.FIO = FIOcons;
                    d_Act.Info = _context.D_Acts.AsNoTracking().FirstOrDefault(o => o.Id == d_Act.Id).Info + FuncSetInfo("Edit");
                    _context.Update(d_Act);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!D_ActExists(d_Act.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                var d_Act2 = _context.D_Acts.Include(p => p.Tc).ThenInclude(p => p.Res).FirstOrDefault(p => p.Id == id);
                return RedirectToAction(nameof(Drawing),d_Act2);
            }
            /*ViewData["TcId"] = new SelectList(_context.D_Tces.OrderBy(p=>p.Date).Select(p=>new { Id = p.Id, text = p.Num + " от " + p.Date.ToString("dd.MM.yyyy") + "; " + p.FIO + "; " + p.Address }), "Id", "text", d_Act.TcId);*/
            return View(d_Act);
        }

        // GET: D_Act/Delete/5
        [Authorize(Roles = "D_operator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var d_Act = await _context.D_Acts
                .Include(d => d.Tc)
                .ThenInclude(l=>l.Res)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (d_Act == null)
            {
                return NotFound();
            }

            return View(d_Act);
        }

        // POST: D_Act/Delete/5
        [Authorize(Roles = "D_operator")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            D_Act d_Act = await _context.D_Acts.FindAsync(id);
            D_Tc d_Tc = await _context.D_Tces.FindAsync(d_Act.TcId);
            // удаление отметки о выдаче акта на основании выданного ТУ в TU_ALL
            using (SqliteConnection con = new SqliteConnection(_defaultConnection))
            {
                using SqliteCommand cmd = con.CreateCommand();
                con.Open();
                //cmd.CommandText = "UPDATE TU_ALL SET del=1, n_akt=0, del_date=SYSDATETIME() WHERE kluch=" + d_Tc.Id.ToString();
                cmd.CommandText = "UPDATE TU_ALL SET n_akt=0, del_date=date('now') WHERE kluch=" + d_Tc.Id.ToString();
                var Result = cmd.ExecuteNonQuery();
            }
            _context.D_Acts.Remove(d_Act);
            _context.D_Tces.Remove(d_Tc);
            await _context.SaveChangesAsync();
            //
            string webRootPath = _webHostEnvironment.WebRootPath;
            string path_docx = webRootPath + "\\Output\\docx\\" + id + ".docx";
            string path_html = webRootPath + "\\Output\\html\\" + id + ".html";
            string path_html_folder = webRootPath + "\\Output\\html\\" + id + "_files";
            string path_pdf = webRootPath + "\\Output\\pdf\\" + id + ".pdf";
            string path_pdf_signed = webRootPath + "\\Output\\pdf_signed\\" + id + "_sign.pdf";
            string path_png = webRootPath + "\\Output\\png\\" + id + ".png";
            string path_svg = webRootPath + "\\Output\\svg\\" + id + ".svg";
            FileInfo docxToDel = new FileInfo(path_docx);
            if (docxToDel.Exists) docxToDel.Delete();
            FileInfo htmlToDel = new FileInfo(path_html);
            if (htmlToDel.Exists) htmlToDel.Delete();
            DirectoryInfo DirHtmlDel = new DirectoryInfo(path_html_folder);
            if (DirHtmlDel.Exists) DirHtmlDel.Delete(true);
            FileInfo pdfToDel = new FileInfo(path_pdf);
            if (pdfToDel.Exists) pdfToDel.Delete();
            FileInfo signed_pdfToDel = new FileInfo(path_pdf_signed);
            if (signed_pdfToDel.Exists) signed_pdfToDel.Delete();
            FileInfo pngToDel = new FileInfo(path_png);
            if (pngToDel.Exists) pngToDel.Delete();
            FileInfo svgToDel = new FileInfo(path_svg);
            if (svgToDel.Exists) svgToDel.Delete();
            DeleteAllImageById(id, "0");
            return RedirectToAction(nameof(Ind_edit));
        }

        private bool D_ActExists(int id)
        {
            return _context.D_Acts.Any(e => e.Id == id);
        }

        [HttpPost]
        [Authorize(Roles = "D_operator")]
        public async Task<IActionResult> CreateAct(int id, string type, List<string> personidList)
        {
            _context.D_Persons.Load();
            D_Act act = await _context.D_Acts.Include(p => p.Tc).ThenInclude(o => o.Res).Where(i => i.Id == id).FirstOrDefaultAsync();
            await CreateDoc(act,type);
            if (type == "agreement")
            {
                if (personidList.Count < 1) return RedirectToAction(nameof(Details), new { id = id });
                // согласовать повторно
                _context.D_Agreements.RemoveRange(_context.D_Agreements.Where(p => p.ActId == id));
                //
                List<D_Person> persons = _context.D_Persons.ToList();
                string UrlAgreementLink = Url.Action("Agreement", "D_Act", new { id = id }, protocol: HttpContext.Request.Scheme);
                string text_mail = string.Empty;
                User user = new User();
                bool notice = false;
                foreach (string personid in personidList)
                {
                    notice = false;
                    // формирование и отправка письма согласующему
                    //string linom = persons.FirstOrDefault(p => p.Id.ToString() == personid).Linom;
                    //user = _context.Users.FirstOrDefault(o => o.Linom.ToString() == linom);
                    //if (user != null) // рассылка только для зарегистрированных пользователей!!!
                    //{
                    //    text_mail = $"{user.UserName}, перейдите пожалуйста по ссылке {UrlAgreementLink} для согласования АКТа разграничения балансовой принадлежности электросетей и эксплуатационной ответственности сторон. Составитель акта {User.Identity.Name}";
                    //    // ! Заменить адрес asgoreglyad@brestenergo.by на user.Email
                    //    await _emailService.SendEmailAsync(user.Email, "Важно! Согласование АКТа разграничения", text_mail);
                    //    notice = true;
                    //}

                        _context.D_Agreements.Add(new D_Agreement() { ActId = id, PersonId = Convert.ToInt32(personid), Notice=notice });
                }
                _context.SaveChanges();
                return RedirectToAction(nameof(Ind_edit));
            }
            else if (type == "completed")
                return RedirectToAction(nameof(Index));
            else return NotFound();
        }

        private async Task CreateDoc(D_Act act,string type)
        {
            bool IsEntity = act.IsEntity, IsTransit = act.IsTransit;
            string str_b = "", str_e = "";
            string str_Entity = IsEntity ? "Юридическое" : "Физическое";
            string str_ConsDover = IsEntity ? "действующ. на основании " + act.EntityDoc + " ": "";
            string str_Id = act.Id.ToString(),
                    str_City = str_b + act.Tc.Res.City + str_e,
                    str_RESa = str_b + act.Tc.Res.RESa + str_e,
                    str_RESom = str_b + act.Tc.Res.RESom + str_e,
                    str_FIOnachRod = str_b + act.Tc.Res.FIOnachRod + str_e,
                    str_Dover = str_b + act.Tc.Res.Dover + str_e,
                    str_DateAct = str_b + act.Date.ToString("dd.MM.yyyy") + str_e,
                    str_EntityDoc = str_b + act.EntityDoc + str_e,
                    str_NumTc = str_b + act.Tc.Num + str_e,
                    str_DateTc = str_b + act.Tc.Date.ToString("dd.MM.yyyy") + str_e,
                    str_RES = str_b + act.Tc.Res.Name + str_e,
                    str_Company = str_b + act.Tc.Company + str_e,
                    str_FIOcons = str_b + act.Tc.FIO + str_e,
                    str_ObjName = str_b + act.Tc.ObjName + str_e,
                    str_Address = str_b + act.Tc.Address + str_e,
                    str_Pow = str_b + act.Tc.Pow + str_e,
                    str_Category = str_b + act.Tc.Category.ToString() + str_e,
                    str_TP = str_b + act.Tc.TP + str_e,
                    str_Line04 = str_b + act.Tc.Line04 + str_e,
                    str_Pillar = str_b + act.Tc.Pillar + str_e,
                    str_InvNum = str_b + act.Tc.TP + str_e,
                    str_ConsBalance = str_b + act.ConsBalance + str_e,
                    str_DevBalance = str_b + act.DevBalance + str_e,
                    str_ConsExpl = str_b + act.ConsExpl + str_e,
                    str_DevExpl = str_b + act.DevExpl + str_e,
                    str_FIOtrans = str_b + act.FIOtrans + str_e,
                    str_Validity = str_b + act.Validity + str_e,
                    str_Nach = str_b + act.Tc.Res.Nach.Surname + " " + act.Tc.Res.Nach.Name.Substring(0, 1) + "." + act.Tc.Res.Nach.Patronymic.Substring(0, 1) + "." + str_e,
                    str_ZamNach = str_b + act.Tc.Res.ZamNach.Surname + " " + act.Tc.Res.ZamNach.Name.Substring(0, 1) + "." + act.Tc.Res.ZamNach.Patronymic.Substring(0, 1) + "." + str_e,
                    str_GlInzh = str_b + act.Tc.Res.GlInzh.Surname + " " + act.Tc.Res.GlInzh.Name.Substring(0, 1) + "." + act.Tc.Res.GlInzh.Patronymic.Substring(0, 1) + "." + str_e,
                    str_Buh = str_b + act.Tc.Res.Buh.Surname + " " + act.Tc.Res.Buh.Name.Substring(0, 1) + "." + act.Tc.Res.Buh.Patronymic.Substring(0, 1) + "." + str_e;
            string[] arrCons = str_FIOcons.Split(' ');
            string str_Cons = "";
            if (arrCons.Length >= 3)
            {
                str_Cons = str_b + arrCons[0] + " " + arrCons[1]?.Substring(0, 1) + "." + arrCons[2]?.Substring(0, 1) + "." + str_e;
                Petrovich FIOcons = new Petrovich() { AutoDetectGender = true, LastName = str_FIOcons.Split(' ')[0], FirstName = str_FIOcons.Split(' ')[1], MiddleName = str_FIOcons.Split(' ')[2] };
                var inflected = FIOcons.InflectTo(Case.Genitive);
                str_FIOcons = inflected.LastName + " " + inflected.FirstName + " " + inflected.MiddleName;
            }
            ComponentInfo.SetLicense("FREE-LIMITED-KEY");
            string contentRootPath = _webHostEnvironment.ContentRootPath;
            string webRootPath = _webHostEnvironment.WebRootPath;
            ViewBag.pathContent = contentRootPath;
            ViewBag.pathWeb = webRootPath;
            string path_docx = webRootPath + "\\Output\\docx\\" + str_Id + ".docx";
            string path_html = webRootPath + "\\Output\\html\\" + str_Id + ".html";
            string path_pdf = webRootPath + "\\Output\\pdf\\" + str_Id + ".pdf";
            ViewBag.path_docx = path_docx;
            var doc = new DocumentModel();
            var ActFont = new CharacterStyle("ActFont") { CharacterFormat = { Spacing = 1, Bold = true } };
            doc.Styles.Add(ActFont);
            doc.DefaultParagraphFormat.LineSpacing = 1;
            doc.DefaultCharacterFormat.FontName = "Times New Roman";
            doc.DefaultCharacterFormat.Size = 12;
            //---section1---//
            Section section1 = new Section(doc);
            doc.Sections.Add(section1);
            Paragraph paragraph = new Paragraph(doc) { ParagraphFormat = { Alignment = HorizontalAlignment.Center } };
            section1.Blocks.Add(paragraph);
            Run run1 = new Run(doc, "АКТ");
            Run run2 = new Run(doc, "разграничения балансовой принадлежности электросетей") { CharacterFormat = { Style = ActFont } };
            Run run3 = new Run(doc, "и эксплуатационной ответственности сторон") { CharacterFormat = { Style = ActFont } };
            paragraph.Inlines.Add(run1);
            paragraph.Inlines.Add(new SpecialCharacter(doc, SpecialCharacterType.LineBreak));
            paragraph.Inlines.Add(run2);
            paragraph.Inlines.Add(new SpecialCharacter(doc, SpecialCharacterType.LineBreak));
            paragraph.Inlines.Add(run3);
            //---table---//
            var table = new Table(doc) { TableFormat = { PreferredWidth = new TableWidth(100, TableWidthUnit.Percentage) } };
            table.TableFormat.Borders.SetBorders(MultipleBorderTypes.All, BorderStyle.None, Color.Black, 1);
            var row = new TableRow(doc);
            table.Rows.Add(row);
            var cell_left = new TableCell(doc, new Paragraph(doc, new Run(doc, "г. " + str_City)) { ParagraphFormat = { Alignment = HorizontalAlignment.Left } });
            var cell_right = new TableCell(doc, new Paragraph(doc, new Run(doc, str_DateAct)) { ParagraphFormat = { Alignment = HorizontalAlignment.Right } });
            row.Cells.Add(cell_left);
            row.Cells.Add(cell_right);
            section1.Blocks.Add(table);
            //---
            string str_act = "\nРУП «Брестэнерго» именуемое в дальнейшем «Энергоснабжающая организация», в лице начальника " + str_RESa + " РЭС филиала «Пинские электрические сети» РУП «Брестэнерго» "+str_FIOnachRod+" действующего на основании доверенности "+str_Dover+" с одной стороны, и "+str_Entity+" лицо "+str_Company+" именуемое в дальнейшем «Потребитель», в лице "+str_FIOcons+" " + str_ConsDover + " с другой стороны составили настоящий АКТ о нижеследующем.\t";
            section1.Blocks.Add(new Paragraph(doc, str_act) { ParagraphFormat = { Alignment = HorizontalAlignment.Justify } });
            //---
            string str_ty = "На день составления Акта технические условия "+str_NumTc+" от "+str_DateTc+" \n " +
                "на внешнее электроснабжение объекта";
            section1.Blocks.Add(new Paragraph(doc, str_ty) { ParagraphFormat = { Alignment = HorizontalAlignment.Center } });
            //---
            string str_building = str_ObjName +
                ", находящегося по адресу " + str_Address + " выполнены:\t";
            section1.Blocks.Add(new Paragraph(doc, str_building) { ParagraphFormat = { Alignment = HorizontalAlignment.Justify } });
            //---
            string str_par = "\tРазрешенная к использованию мощность "+str_Pow+" кВт.\t\n" +
                "\tЭлектроустановки потребителя относятся к "+str_Category+" категории " +
                "по надежности электроснабжения.\t\n" +
                "\tСхема внешнего электроснабжения относится к "+str_Category+" категории по надежности электроснабжения.\t\n" +
                "\tЭнергоснабжающая организация не несет ответственности перед Потребителем за перерывы в электроснабжении при несоответствии схемы электроснабжения категории электроприемников Потребителя и повреждении оборудования, не находящегося у нее на балансе.\t\n" +
                "\tВ соответствии с главой 3 Правил электроснабжения границы раздела устанавливаются следующими:\t\n";
            Paragraph paragraph2 = new Paragraph(doc, str_par) { ParagraphFormat = { Alignment = HorizontalAlignment.Justify } };
            section1.Blocks.Add(paragraph2);
            //---
            section1.Blocks.Add(new Paragraph(doc,
                new SpecialCharacter(doc, SpecialCharacterType.Tab),
                new Run(doc, "I. По балансовой принадлежности:") { CharacterFormat = { Style = ActFont } },
                new SpecialCharacter(doc, SpecialCharacterType.Tab),
                new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                new Run(doc,str_Line04 +" от " + str_TP + " оп. №"+str_Pillar+" на балансе "+str_RESa+" РЭС."),
                new SpecialCharacter(doc, SpecialCharacterType.Tab),
                new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                new Run(doc, str_ConsBalance + " от оп. №" + str_Pillar + " " + str_Line04 + " от " + str_TP + " и внутреннее эл. оборудование расположенное по адресу " + str_Address + " находится на балансе Потребителя"),
                new SpecialCharacter(doc, SpecialCharacterType.Tab),
                new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                new Run(doc, "Граница раздела между "+str_RESom+" РЭС и Потребителем - "+str_DevBalance+ " №" + str_Pillar +" " + str_Line04 + " от " + str_TP),
                new SpecialCharacter(doc, SpecialCharacterType.Tab),
                new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                new SpecialCharacter(doc, SpecialCharacterType.Tab),
                new Run(doc, "II. По Эксплутационной ответственности:") { CharacterFormat = { Style = ActFont } },
                new SpecialCharacter(doc, SpecialCharacterType.Tab),
                new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                new Run(doc, str_Line04 + " от " + str_TP + " оп. №" + str_Pillar + " на балансе " + str_RESa + " РЭС."),
                new SpecialCharacter(doc, SpecialCharacterType.Tab),
                new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                new Run(doc, str_ConsExpl + " от оп. №" + str_Pillar + " " + str_Line04 + " от " + str_TP + " и внутреннее эл. оборудование расположенное по адресу " + str_Address + " находится на балансе Потребителя"),
                new SpecialCharacter(doc, SpecialCharacterType.Tab),
                new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                new Run(doc, "Граница раздела между " + str_RESom + " РЭС и Потребителем - " + str_DevExpl + " №" + str_Pillar + " " + str_Line04 + " от " + str_TP),
                new SpecialCharacter(doc, SpecialCharacterType.Tab),
                new SpecialCharacter(doc, SpecialCharacterType.LineBreak)
                )
            { ParagraphFormat = { Alignment = HorizontalAlignment.Justify } });
            ///////////////////////////////////---sectiion2---
            if (type != "agreement")
            {
                Section section2 = new Section(doc);
                doc.Sections.Add(section2);
                ///---
                Paragraph paragraph21 = new Paragraph(doc) { ParagraphFormat = { Alignment = HorizontalAlignment.Center } };
                section2.Blocks.Add(paragraph21);
                paragraph21.Inlines.Add(new Run(doc, "Схема питания электроустановки:"));
                paragraph21.Inlines.Add(new SpecialCharacter(doc, SpecialCharacterType.LineBreak));
                // find picture to insert
                string path_pict = (_webHostEnvironment.WebRootPath + "\\Output\\png\\");
                DirectoryInfo dirPNG = new DirectoryInfo(path_pict);
                FileInfo pictFile = dirPNG.EnumerateFiles().Where(p => p.Name.Split('.')[0] == act.Id.ToString()).FirstOrDefault();
                if (pictFile != null)
                {
                    path_pict += pictFile.Name;
                    Picture pict = new Picture(doc, path_pict, 159, 106, LengthUnit.Millimeter);
                    paragraph21.Inlines.Add(pict);
                }
                //---
                section2.Blocks.Add(new Paragraph(doc,
                    new SpecialCharacter(doc, SpecialCharacterType.Tab),
                    new Run(doc, "ПРИМЕЧАНИЕ"),
                    new SpecialCharacter(doc, SpecialCharacterType.Tab),
                    new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                    new SpecialCharacter(doc, SpecialCharacterType.Tab),
                    new Run(doc, "1.Границы по схеме обозначаются: балансовой принадлежности - красной линией; эксплуатационной ответственности - синей."),
                    new SpecialCharacter(doc, SpecialCharacterType.Tab),
                    new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                    new SpecialCharacter(doc, SpecialCharacterType.Tab),
                    new Run(doc, "2.При изменении срока действия Акта, присоединенных мощностей, схемы внешнего электроснабжения, категории надежности электроснабжения, границ балансовой принадлежности и эксплуатационной ответственности Акт подлежит замене."),
                    new SpecialCharacter(doc, SpecialCharacterType.Tab),
                    new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                    new SpecialCharacter(doc, SpecialCharacterType.Tab),
                    new Run(doc, "3.Доверенность потребителя на подписание акта разграничения хранится в энергоснабжающей организации."),
                    new SpecialCharacter(doc, SpecialCharacterType.Tab),
                    new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                    new SpecialCharacter(doc, SpecialCharacterType.Tab),
                    new Run(doc, "4.На схеме питания электроустановки указываются места установки приборов учета, параметры силовых и измерительных трансформаторов и ЛЭП."),
                    new SpecialCharacter(doc, SpecialCharacterType.Tab),
                    new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                    new SpecialCharacter(doc, SpecialCharacterType.Tab),
                    new Run(doc, "5.Потребителю запрещается без согласования с диспетчером энергоснабжающей организации самовольно производить переключения и изменять схему внешнего электроснабжения."),
                    new SpecialCharacter(doc, SpecialCharacterType.Tab),
                    new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                    new SpecialCharacter(doc, SpecialCharacterType.Tab),
                    new Run(doc, "6.Потребителю запрещается без согласования с энергоснабжающей организацией подключать к своим электроустановкам сторонних потребителей."),
                    new SpecialCharacter(doc, SpecialCharacterType.Tab),
                    new SpecialCharacter(doc, SpecialCharacterType.LineBreak)
                    )
                { ParagraphFormat = { Alignment = HorizontalAlignment.Justify } }
                );
                //---
                var table2 = new Table(doc) { TableFormat = { PreferredWidth = new TableWidth(100, TableWidthUnit.Percentage) } };
                table2.TableFormat.Borders.SetBorders(MultipleBorderTypes.All, BorderStyle.None, Color.Black, 1);
                var row21 = new TableRow(doc);
                table2.Rows.Add(row21);
                var cell2_left = new TableCell(doc, new Paragraph(doc,
                    new Run(doc, "Представитель энергоснабжающей организации"),
                    new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                    new Run(doc, "Представитель Потребителя"),
                    new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                    new Run(doc, "Представитель владельца"),
                    new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                    new Run(doc, "транзитных электрических сетей"),
                    new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                    new Run(doc, "Срок действия акта")
                    ));
                var cell2_center = new TableCell(doc, new Paragraph(doc,
                    new Run(doc, "_____"),
                    new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                    new Run(doc, "_____"),
                    new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                    new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                    new Run(doc, "_____"),
                    new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                    new Run(doc, str_Validity)
                    ));
                var cell2_right = new TableCell(doc, new Paragraph(doc,
                    new Run(doc, str_Nach),
                    new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                    new Run(doc, str_Cons),
                    new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                    new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                    new Run(doc, str_FIOtrans),
                    new SpecialCharacter(doc, SpecialCharacterType.LineBreak),
                    new Run(doc, "")
                    ));
                row21.Cells.Add(cell2_left);
                row21.Cells.Add(cell2_center);
                row21.Cells.Add(cell2_right);
                section2.Blocks.Add(table2);
                //---save---//
                doc.Save(path_docx);
                doc.Save(path_pdf);
            }
            else
            {
                doc.Save(path_html, SaveOptions.HtmlDefault);
                _context.D_Acts.FirstOrDefault(p => p.Id == act.Id).State = (int)Stat.InAgreement;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<IActionResult> DownloadAct(List<IFormFile> postedFiles, int id)
        {
            if (postedFiles.Count > 0)
            {
                string path = _webHostEnvironment.WebRootPath + "\\Output\\pdf_signed\\";
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                foreach (IFormFile postedFile in postedFiles)
                {
                    string fileName_origin = Path.GetFileName(postedFile.FileName);
                    FileInfo fileInfo = new FileInfo(fileName_origin);
                    string ext = fileInfo.Extension;
                    //string ext = fileName_origin.Split('.')[1];
                    string fileName = id + "_sign" + ext;
                    if (".pdf" == ext.ToLower()) // проверка расширения
                    {
                        using (FileStream stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                        {
                            await postedFile.CopyToAsync(stream);
                            ViewBag.FileName = fileName;
                            ViewBag.Message += string.Format("<b>{0}</b> загружен.<br />", fileName_origin);
                        }
                    }
                }
                //блок, который выполнится после закачки подписанного акта
                D_Act act = _context.D_Acts.FirstOrDefault(p => p.Id == id);
                act.State = (int)Stat.Completed;
                _context.SaveChanges();
                // отметка о выдаче акта на основании выданного ТУ в TU_ALL
                using (SqliteConnection con = new SqliteConnection(_defaultConnection))
                {
                    using (SqliteCommand cmd = con.CreateCommand())
                    {
                        con.Open();
                        cmd.CommandText = "UPDATE TU_ALL SET n_akt=" + id.ToString() + ", del_date=date('now') WHERE kluch=" + act.TcId.ToString();
                        var Result = cmd.ExecuteNonQuery();
                    }
                }
                ///-V удаление уже не нужных файлов
                string webRootPath = _webHostEnvironment.WebRootPath;
                string path_docx = webRootPath + "\\Output\\docx\\" + id + ".docx";
                string path_html = webRootPath + "\\Output\\html\\" + id + ".html";
                string path_png = webRootPath + "\\Output\\png\\" + id + ".png";
                string path_svg = webRootPath + "\\Output\\svg\\" + id + ".svg";
                FileInfo docxToDel = new FileInfo(path_docx);
                if (docxToDel.Exists) docxToDel.Delete();
                FileInfo htmlToDel = new FileInfo(path_html);
                FileInfo pngToDel = new FileInfo(path_png);
                if (pngToDel.Exists) pngToDel.Delete();
                FileInfo svgToDel = new FileInfo(path_svg);
                if (svgToDel.Exists) svgToDel.Delete();
                DeleteAllImageById(id, "0");
                ///-A удаление уже не нужных файлов
                return RedirectToAction("Index", "D_Act");
            }
            else return RedirectToAction("Details", "D_Act", new { id = id });
        }

        private string FuncSetInfo(string operation)
        {

            return DateTime.Now.ToString("HH:mm dd.MM.yyyy") + "/" + Request.HttpContext.Connection.RemoteIpAddress.ToString() +"/" + User.Identity.Name+  "/" + operation + ";";
        }
    }
}
