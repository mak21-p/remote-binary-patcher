using System;
using System.IO;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PatcherServer.Models;
using FastRsync.Signature;
using FastRsync.Delta;
using FastRsync.Diagnostics;
using FastRsync.Core;
using Microsoft.AspNetCore.StaticFiles;
using System.Security.Cryptography;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Newtonsoft.Json.Linq;

namespace PatcherServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PatchersController : Controller
    {
        public readonly APIContext _context;

        public PatchersController(APIContext context)
        {
            _context = context;
        }

        // GET: Patchers
        [NonAction]
        public async Task<IActionResult> Index()
        {
              return _context.patchers != null ? 
                          View(await _context.patchers.ToListAsync()) :
                          Problem("Entity set 'Context.patchers'  is null.");
        }

        // GET: Patchers/Details/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.patchers == null)
            {
                return NotFound();
            }

            var patcher = await _context.patchers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (patcher == null)
            {
                return NotFound();
            }

            return View(patcher);
        }

        [HttpGet("sha/{id}")]
        public ActionResult<Patcher> ShaGet([FromRoute] string id)
        {
            var patcher = _context.patchers.FirstOrDefault(p => p.Sha256 == id);

            if (patcher == null)
            {
                return NotFound();
            }

            return patcher;
        }


        [HttpPost]
        [Route("api/upload")]
        public async Task<IActionResult> Upload()
        {
            Console.WriteLine("Started Upload");
            var file = Request.Form.Files[0];
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty or missing");
            }

            // Validate file type
            var allowedExtensions = new[] { ".sig" };
            var fileExtension = Path.GetExtension(file.FileName);
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest("Invalid file type");
            }

            // Sanitize input
            var fileName = Path.GetFileNameWithoutExtension(file.FileName);
            fileName = string.Join("", fileName.Split(Path.GetInvalidFileNameChars()));

            var path = Path.Combine(Directory.GetCurrentDirectory(), "caddy_data", "PatchFiles", "sigfiles", $"sigfile_{DateTime.Now:yyyyMMddHHmmss}_{fileName}.sig");
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "caddy_data", "PatchFiles", "sigfiles")))
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "caddy_data", "PatchFiles", "sigfiles"));
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "caddy_data", "PatchFiles", "deltas"));
            }

            string deltafilepath = Path.Combine(Directory.GetCurrentDirectory(), "caddy_data", "PatchFiles", "deltas", $"delta_{DateTime.Now:yyyyMMddHHmmss}_{fileName}.rdiff");

            using (var stream = new FileStream(path, FileMode.Create))
            {
                string newfilepath = Path.Combine(Directory.GetCurrentDirectory(), "caddy_data", "PatchFiles", "patch-Z.mpq");
                await file.CopyToAsync(stream);
                var delta = new DeltaBuilder();
                using (var newFileStream = new FileStream(newfilepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var signatureStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var deltaStream = new FileStream(deltafilepath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    Console.WriteLine("Started Delta");
                    delta.BuildDelta(newFileStream, new SignatureReader(signatureStream, delta.ProgressReport), new AggregateCopyOperationsDecorator(new BinaryDeltaWriter(deltaStream)));

                }
            }
            string deltaurl = Url.Content(deltafilepath);
            string trimmedPath = deltaurl.Substring("/app/caddy_data/PatchFiles/deltas/".Length);
            string deltafilename = Path.GetFileName(trimmedPath);
            string finalDLURL = "http://65.109.128.248:8080/PatchFiles/deltas/" + deltafilename;

            // Create a new Patcher entity and set its properties
            var patcher = new Patcher
            {
                Sha256 = GetSha256Hash(path),
                DeltaLink = finalDLURL
            };

            // Add the new entity to the database
            _context.patchers.Add(patcher);
            await _context.SaveChangesAsync();

            return Ok(finalDLURL);
        }


        [NonAction]
        private string GetSha256Hash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }



        [NonAction]
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var contentTypeProvider = new FileExtensionContentTypeProvider();
            contentTypeProvider.Mappings[".sig"] = "application/pgp-signature";
            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = contentTypeProvider
            });

            // ...
        }





        // GET: Patchers/Create
        [NonAction]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Patchers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // [HttpPost]
        // [ValidateAntiForgeryToken]
        [NonAction]
        public async Task<IActionResult> Create([Bind("Id,Sha256,DeltaLink")] Patcher patcher)
        {
            if (ModelState.IsValid)
            {
                _context.Add(patcher);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(patcher);
        }

        // GET: Patchers/Edit/5
        [NonAction]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.patchers == null)
            {
                return NotFound();
            }

            var patcher = await _context.patchers.FindAsync(id);
            if (patcher == null)
            {
                return NotFound();
            }
            return View(patcher);
        }

        // POST: Patchers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // [HttpPost]
        // [ValidateAntiForgeryToken]
        [NonAction]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Sha256,DeltaLink")] Patcher patcher)
        {
            if (id != patcher.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(patcher);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatcherExists(patcher.Id))
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
            return View(patcher);
        }

        // GET: Patchers/Delete/5
        [NonAction]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.patchers == null)
            {
                return NotFound();
            }

            var patcher = await _context.patchers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (patcher == null)
            {
                return NotFound();
            }

            return View(patcher);
        }

        // POST: Patchers/Delete/5
        // [HttpPost, ActionName("Delete")]
        // [ValidateAntiForgeryToken]
        [NonAction]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.patchers == null)
            {
                return Problem("Entity set 'Context.patchers'  is null.");
            }
            var patcher = await _context.patchers.FindAsync(id);
            if (patcher != null)
            {
                _context.patchers.Remove(patcher);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> CreateNew([FromBody] Patcher patcher)
        {
            if (ModelState.IsValid)
            {
                await _context.patchers.AddAsync(patcher);
                await _context.SaveChangesAsync();
                return Ok(patcher);
            }
            return BadRequest(ModelState);
        }


        // GET: api/Patchers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Patcher>>> GetPatchers()
        {
            return await _context.patchers.ToListAsync();
        }

        // GET: api/Patchers
        [HttpPost]
        [Route("api/delete/{safeword}")]
        public async Task<ActionResult<IEnumerable<Patcher>>> Reset([FromRoute] string safeword)
        {
            var jsonString = System.IO.File.ReadAllText(@"serviceaccount.json");
            var firebuilder = new FirestoreClientBuilder { JsonCredentials = jsonString };
            FirestoreDb db = FirestoreDb.Create("duskhaven-patcher", firebuilder.Build());

            DocumentSnapshot snapshot = await db.Collection("admin").Document("superadmin").GetSnapshotAsync();

            if (safeword != snapshot.GetValue<string>("safeword"))
            {
                return BadRequest("Incorrect Requeust");
            }

            // Commence reset
            
            var sigDir = Path.Combine(Directory.GetCurrentDirectory(), "caddy_data", "PatchFiles", "sigfiles");
            foreach (var file in Directory.GetFiles(sigDir))
            {
                System.IO.File.Delete(file);
            }

            var deltaDir = Path.Combine(Directory.GetCurrentDirectory(), "caddy_data", "PatchFiles", "deltas");
            foreach (var file in Directory.GetFiles(deltaDir))
            {
                System.IO.File.Delete(file);
            }

            var patchers = _context.patchers.ToList();
            _context.patchers.RemoveRange(patchers);
            await _context.SaveChangesAsync();

            // Create a new Patcher entity and set its properties
            var patcher = new Patcher
            {
                Sha256 = GetSha256Hash(Path.Combine(Directory.GetCurrentDirectory(), "caddy_data", "PatchFiles", "patch-Z.mpq")),
                DeltaLink = "http://65.109.128.248:8080",
                isPatchZ = true,
            };

            var signatureBuilder = new SignatureBuilder();
            using (var basisStream = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "caddy_data", "PatchFiles", "patch-Z.mpq"), FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var signatureStream = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "caddy_data", "PatchFiles", "patch-Z.sig"), FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                signatureBuilder.Build(basisStream, new SignatureWriter(signatureStream));
            }

            // Add the new entity to the database
            _context.patchers.Add(patcher);

            return Ok(snapshot.GetValue<string>("safeword"));
        }

        [NonAction]
        private bool PatcherExists(int id)
        {
          return (_context.patchers?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
