﻿using ReverseSpectre.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ReverseSpectre.Controllers.CommercialBank
{
    [RoutePrefix("combank/loans")]
    [Route("{action=index}")]
    [Authorize(Roles = "AccountingOfficer")]
    public class ComBankLoanController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index(int? id)
        {
            if (id != null)
            {
                // if client id is present

                Client client = db.Clients.FirstOrDefault(c => c.ClientId == id);

                if (client == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }else
                {
                    var loans = db.LoanApplications.Where(l => l.ClientId == id);
                    return View(loans);
                }

            }else
            {
                // if no client id provided

                var loans = db.LoanApplications.Include("Client").ToList();
                return View(loans);
            }
        }

        [Route("{id}/details")]
        public ActionResult Details(int id)
        {
            LoanApplication loan = db.LoanApplications.FirstOrDefault(l => l.LoanApplicationId == id);

            if(loan == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var documents = db.LoanApplicationDocuments.Include("Files").Where(m => m.LoanApplicationId == loan.LoanApplicationId).ToList();
            loan.LoanApplicationDocuments = documents;

            return View(loan);
        }

        public ActionResult AddRequirement(int id)
        {
            LoanApplication loan = db.LoanApplications.FirstOrDefault(l => l.LoanApplicationId == id);

            if (loan == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            LoanApplicationDocument lad = new LoanApplicationDocument();
            lad.LoanApplicationId = loan.LoanApplicationId;

            return View(lad);
        }

        [HttpPost]
        public ActionResult AddRequirement([Bind(Include = "Name,Comment,LoanApplicationId")] LoanApplicationDocument loanApplicationDocument)
        {
            loanApplicationDocument.Status = LoanDocumentStatusType.Pending;
            loanApplicationDocument.TimestampCreated = DateTime.Now;

            if (ModelState.IsValid)
            {
                db.LoanApplicationDocuments.Add(loanApplicationDocument);
                db.SaveChanges();
                
                LoanApplication loan = db.LoanApplications.FirstOrDefault(l => l.LoanApplicationId == loanApplicationDocument.LoanApplicationId);

                if (loan == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                var documents = db.LoanApplicationDocuments.Include("Files").Where(m => m.LoanApplicationId == loan.LoanApplicationId).ToList();
                loan.LoanApplicationDocuments = documents;

                return PartialView("DocumentList", documents);
            }

            return View(loanApplicationDocument);
        }

        //Loan Documents
        public ActionResult Approve(int id)
        {
            LoanApplicationDocument lad = db.LoanApplicationDocuments.FirstOrDefault(l => l.LoanApplicationDocumentId == id);

            if (lad == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            lad.Comment = string.Empty;
            lad.Status = LoanDocumentStatusType.Approved;
            db.Entry(lad).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Details", new { id = lad.LoanApplicationId });
        }

        public ActionResult Deny(int id)
        {
            LoanApplicationDocument lad = db.LoanApplicationDocuments.FirstOrDefault(l => l.LoanApplicationDocumentId == id);

            if (lad == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            lad.Status = LoanDocumentStatusType.Denied;
            db.Entry(lad).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Details", new { id = lad.LoanApplicationId });
        }

        //[Route("requirement/{id}/revise")]
        public ActionResult ReviseRequirement(int id)
        {
            LoanApplicationDocument lad = db.LoanApplicationDocuments.FirstOrDefault(l => l.LoanApplicationDocumentId == id);

            if (lad == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            LoanDocumentReviseModel ldrm = new LoanDocumentReviseModel(lad);

            return View(ldrm);
        }

        //[Route("requirement/{id}/revise")]
        [HttpPost]
        public ActionResult ReviseRequirement(LoanDocumentReviseModel ldrm)
        {
            if (ModelState.IsValid)
            {
                LoanApplicationDocument lad = db.LoanApplicationDocuments
                    .FirstOrDefault(l => l.LoanApplicationDocumentId == ldrm.LoanApplicationDocumentId);

                if (lad == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                lad.Status = LoanDocumentStatusType.Revision;
                lad.Comment = ldrm.Comment;

                db.Entry(lad).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();


                LoanApplication loan = db.LoanApplications.FirstOrDefault(l => l.LoanApplicationId == lad.LoanApplicationId);

                if (loan == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                var documents = db.LoanApplicationDocuments.Include("Files").Where(m => m.LoanApplicationId == loan.LoanApplicationId).ToList();
                loan.LoanApplicationDocuments = documents;

                return PartialView("DocumentList", documents);
                //return RedirectToAction("Details", new { id = lad.LoanApplicationId });
            }

            return View(ldrm);
        }

        public ActionResult CommentFile(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var file = db.LoanApplicationDocumentFiles.Find(id);
            if (file == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ViewBag.Id = file.LoanApplicationDocumentFileId;
            ViewBag.LoanId = file.LoanApplicationDocumentId;
            return View();
        }

        [HttpPost]
        public ActionResult CommentFile(int? id, string comment)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var file = db.LoanApplicationDocumentFiles.Find(id);
            if (file == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (string.IsNullOrEmpty(comment))
            {
                ViewBag.Id = id;
                ViewBag.LoanId = file.LoanApplicationDocumentId;
                return View();
            }

            // Edit entry
            file.Comment = comment;
            db.Entry(file).State = System.Data.Entity.EntityState.Modified;

            // Save entry
            db.SaveChanges();

            LoanApplication loan = db.LoanApplications.FirstOrDefault(l => l.LoanApplicationId == file.LoanApplicationDocument.LoanApplicationId);

            if (loan == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var documents = db.LoanApplicationDocuments.Include("Files").Where(m => m.LoanApplicationId == loan.LoanApplicationId).ToList();
            loan.LoanApplicationDocuments = documents;

            return PartialView("DocumentList", documents);

            //return RedirectToAction("Details", new { id = file.LoanApplicationDocument.LoanApplicationId });
        }

        [HttpPost]
        public ActionResult ChangeStatus(int id, int statusType)
        {
            // Validation
            var loan = db.LoanApplications.Find(id);
            if (loan == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var officer = db.AccountingOfficers.FirstOrDefault(m => m.User.UserName == User.Identity.Name);
            if (loan.Client.AccountingOfficerId != officer.AccountingOfficerId)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            try
            {
                // Edit entry
                loan.LoanStatus = (LoanStatusType)statusType;
                db.Entry(loan).State = System.Data.Entity.EntityState.Modified;

                // Save entry
                db.SaveChanges();
            }
            catch
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            return new HttpStatusCodeResult(HttpStatusCode.Accepted);
        }

        public ActionResult EditFileComment(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var file = db.LoanApplicationDocumentFiles.Find(id);
            if (file == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ViewBag.Id = id;
            ViewBag.LoanId = file.LoanApplicationDocumentId;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditFileComment(int? id, string comment)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var file = db.LoanApplicationDocumentFiles.Find(id);
            if (file == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (string.IsNullOrEmpty(comment))
            {
                ViewBag.Id = id;
                ViewBag.LoanId = file.LoanApplicationDocumentId;
                return View();
            }

            // Edit entry
            file.Comment = comment;
            db.Entry(file).State = System.Data.Entity.EntityState.Modified;

            // Save entry
            await db.SaveChangesAsync();

            LoanApplication loan = db.LoanApplications.FirstOrDefault(l => l.LoanApplicationId == file.LoanApplicationDocument.LoanApplicationId);

            if (loan == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var documents = db.LoanApplicationDocuments.Include("Files").Where(m => m.LoanApplicationId == loan.LoanApplicationId).ToList();
            loan.LoanApplicationDocuments = documents;

            return PartialView("DocumentList",loan);
        }

        public ActionResult EditInterest(int id, double interest)
        {
            var loan = db.LoanApplications.Find(id);
            if (loan != null && interest >= 0 && interest <= 1)
            {
                loan.Interest = interest;
                db.Entry(loan).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }

            return RedirectToAction("Details", new { id = id });
        }

    }
}
