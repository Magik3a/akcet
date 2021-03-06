﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using akcetDB;
using akcet_fakturi.Areas.InvoiceTemplates.Models;
using akcet_fakturi.Models;
using HtmlAgilityPack;
using iTextSharp.text;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using Kendo.Mvc.Extensions;
using Microsoft.AspNet.Identity;
using WebGrease.Css.Extensions;
using Tools;
using System.Text;
using System.Configuration;
using akcet_fakturi.Areas.EmailTemplates.Models;

namespace akcet_fakturi.Controllers
{
    public class HomeController : BaseController
    {
        private AkcetModel db = new AkcetModel();
        public ActionResult Index()
        {

            return View();
        }

        #region Testing

        public ActionResult TestPage()
        {
            var userId = User.Identity.GetUserId();

            ViewBag.Html = db.Fakturis.OrderByDescending(o => o.DateCreated).Where(u => u.UserID == userId).FirstOrDefault().FakturaHtml;

            return View();
        }
        #endregion

        [HttpPost]
        public ActionResult Index(ContactFormModel Model)
        {

            if (!ModelState.IsValid)
                return View(Model);

            var messageBody = new StringBuilder();

            messageBody.AppendLine("######################################## <br /> <br />");
            messageBody.AppendLine("Ново съобщение от: " + Model.FirstName + " " + Model.LastName);
            messageBody.AppendLine("<br /> <br />");
            messageBody.AppendLine("######################################## <br /> <br />");
            messageBody.AppendLine("Телефон: " + Model.Phone);
            messageBody.AppendLine("<br /> <br />");
            messageBody.AppendLine("######################################## <br /> <br />");
            messageBody.AppendLine("Имейл: " + Model.Email);
            messageBody.AppendLine("<br /> <br />");
            messageBody.AppendLine("######################################## <br /> <br />");
            messageBody.AppendLine("Съобщение: " + Model.Message);
            messageBody.AppendLine("<br /> <br />");
            messageBody.AppendLine("######################################## <br /> <br />");
            EmailFunctions.SendEmail(ConfigurationManager.AppSettings["AdminEmail"], "Ново запитване от сайта fakturi.nl", messageBody.ToString());
            TempData["MessageIsSent"] = "Съобщението е изпратено успешно.";
            return View(Model);
        }


        public ActionResult AdminIndex()
        {
            return View();
        }
        #region AddInvoice
        [Authorize]
        public ActionResult Invoices()
        {
            ViewBag.formNumber = 1;
            var userId = User.Identity.GetUserId();
            string error = "";

            if(!CheckUserDetails(userId, out error))
                TempData["ResultError"] = error;

            ViewBag.IdAddress = new SelectList(db.Addresses.Where(a => a.UserName == User.Identity.Name), "IdAddress", "StreetName");
            ViewBag.Dds = new SelectList(db.DDS, "DdsId", "DdsName");
            ViewBag.Companies = new SelectList(db.Companies.Where(m => m.UserId == userId && m.IsDeleted == false), "CompanyID", "CompanyName");
            ViewBag.Projects = new SelectList(db.Projects.Where(m => m.UserID == userId && m.IsDeleted == false), "ProjectID", "ProjectName");
            ViewBag.Products = new SelectList(db.Products.Where(p => p.UserId == userId && p.IsDeleted == false), "ProductID", "ProductName");
            return View();
        }

        public ActionResult CreateCompanyAjax([Bind(Exclude = "UserId", Include = "CompanyID,UserId,IdAddress,CompanyName,CompanyMol,DdsNumber,CompanyDescription,CompanyPhone,IsPrimary,DateCreated,DateModified")] Company company)
        {

            ModelState.Remove("UserId");

            if (!Request.IsAjaxRequest())
            {
                return Json(false);
            }
            if (!ModelState.IsValid)
            {
                return Json(false);
            }
            if (company.IdAddress == 0)
            {
                TempData["ResultError"] = "Не сте избрали адрес на фирмата!";
                return Json(false);
            }
            using (var context = new AkcetModel())
            {
                company.DateCreated = DateTime.Now;
                company.DateModified = DateTime.Now;
                company.UserId = User.Identity.GetUserId();
                company.IsDeleted = false;
                db.Companies.Add(company);
                db.SaveChanges();


                return Json(new { id = company.CompanyID, value = company.CompanyName });
            }
        }

        public JsonResult CreateAddressAjax(Address modelAddress)
        {
            if (!Request.IsAjaxRequest())
            {
                TempData["ResultError"] = "Грешка в добавяне на адрес!";
                return Json(false);
            }
            if (!ModelState.IsValid)
            {
                TempData["ResultError"] = "Грешка в добавяне на адрес!";
                return Json(false);
            }

            using (var context = new AkcetModel())
            {
                var address = new Address();
                address = modelAddress;
                address.DateCreated = DateTime.Now;
                address.DateModified = DateTime.Now;
                address.UserName = User.Identity.Name;
                address.UserID = User.Identity.GetUserId();

                context.Addresses.Add(address);

                context.SaveChanges();

                TempData["ResultSuccess"] = "Успешно добавихте адрес!";

                return Json(new { id = address.IdAddress, value = address.StreetName });

            }
        }

        public JsonResult CreateProductAjax(Product modelProduct)
        {
            ModelState.Remove("UserId");

            if (!Request.IsAjaxRequest())
            {
                TempData["ResultError"] = "Грешка в добавяне на адрес!";
                return Json(false);
            }
            if (!ModelState.IsValid)
            {
                TempData["ResultError"] = "Грешка в добавяне на адрес!";
                return Json(false);
            }

            using (var context = new AkcetModel())
            {
                modelProduct.DateCreated = DateTime.Now;
                modelProduct.DateModified = DateTime.Now;
                modelProduct.UserId = User.Identity.GetUserId();
                modelProduct.IsDeleted = false;
                db.Products.Add(modelProduct);
                db.SaveChanges();


                return Json(new { id = modelProduct.ProductID, value = modelProduct.ProductName });
            }
        }

        public JsonResult CreateInvoiceAjax(string Companies, FakturiTemp Fakturi)
        {
            if (Companies == "0")
                return Json(false);

            if (String.IsNullOrWhiteSpace(Fakturi.InvoiceDate))
                return Json(false);

            if (String.IsNullOrWhiteSpace(Fakturi.InvoiceEndDate))
                return Json(false);

            Fakturi.CompanyID = Int32.Parse(Companies);

            var userId = User.Identity.GetUserId();

            using (var context = new AkcetModel())
            {
                var temp = new List<ProductInvoiceTemp>();
                Fakturi.ProductInvoiceTemps = temp;
                Fakturi.UserId = userId;
                Fakturi.DateCreated = DateTime.Now;
                Fakturi.DateModified = DateTime.Now;
                Fakturi.UserName = User.Identity.Name;
                context.FakturiTemps.Add(Fakturi);
                context.SaveChanges();

            }
            return Json(Fakturi);
        }

        public ActionResult SaveProductAjax(string Products, string Dds, string Projects, string ProductPrice, string Quanity)
        {
            decimal productPrice;
            decimal quantity;

            if (String.IsNullOrWhiteSpace(Products))
                return Json(false);
            if (String.IsNullOrWhiteSpace(Dds))
                return Json(false);
            if (String.IsNullOrWhiteSpace(Projects))
                return Json(false);
            if (String.IsNullOrWhiteSpace(ProductPrice) && !decimal.TryParse(ProductPrice, out productPrice))
                return Json(false);
            if (String.IsNullOrWhiteSpace(Quanity) && !decimal.TryParse(Quanity, out quantity))
                return Json(false);

            //TODO: If ProductPrice has ',' do something
            //TODO: If Quantity has ',' do something

            var userId = User.Identity.GetUserId();
            ViewBag.Dds = new SelectList(db.DDS, "DdsId", "DdsName");
            ViewBag.Products = new SelectList(db.Products.Where(p => p.UserId == userId && p.IsDeleted == false), "ProductID", "ProductName");
            ViewBag.Projects = new SelectList(db.Projects.Where(m => m.UserID == userId && m.IsDeleted == false), "ProjectID", "ProjectName");
            ViewBag.IsInsertedProduct = true;


            var model = new ProductInvoiceTemp();
            var firstOrDefault = db.FakturiTemps.Where(s => s.UserId == userId).OrderByDescending(x => x.DateCreated).FirstOrDefault();
            if (firstOrDefault != null)
            {
               // var orDefault = db.DDS.FirstOrDefault(s => s.Value == Dds);
               // if (orDefault != null)
             //   {
                    var tbl = new ProductInvoiceTemp()
                    {
                        InvoiceIDTemp = firstOrDefault.InvoiceIDTemp,
                        DdsID = Int32.Parse(Dds),
                        ProjectID = Int32.Parse(Projects),
                        ProductID = Int32.Parse(Products),
                        ProductPrice = Decimal.Parse(ProductPrice, CultureInfo.InvariantCulture),
                        Quanity = Decimal.Parse(Quanity, CultureInfo.InvariantCulture)
                    };
                    using (var context = new AkcetModel())
                    {
                        db.ProductInvoiceTemps.Add(tbl);
                        db.SaveChanges();
                    }

                    model.ProductInvoiceID = tbl.ProductInvoiceID;
             //   }
            }

            //  model.ProductInvoiceID = Int32.Parse(Products);
            return PartialView("~/Views/Shared/InvoicesPartials/_TabProductsPartial.cshtml", model);
        }

        public ActionResult DeleteProductInvoiceTemp(int id)
        {
            //var userId = User.Identity.GetUserId();
            //var invoiceId = db.FakturiTemps.Where(s => s.UserId == userId).OrderByDescending(x => x.DateCreated).FirstOrDefault().InvoiceIDTemp;

            var product = db.ProductInvoiceTemps.Find(id);

            db.ProductInvoiceTemps.Remove(product);
            db.SaveChanges();

            return Json(id, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CreateProjectAjax(Project modelProject)
        {
            ModelState.Remove("UserID");

            if (!Request.IsAjaxRequest())
            {
                TempData["ResultError"] = "Грешка в добавяне на адрес!";
                return Json(false);
            }
            if (!ModelState.IsValid)
            {
                TempData["ResultError"] = "Грешка в добавяне на адрес!";
                return Json(false);
            }

            using (var context = new AkcetModel())
            {
                modelProject.DateCreated = DateTime.Now;
                modelProject.DateModified = DateTime.Now;
                modelProject.UserID = User.Identity.GetUserId();
                modelProject.UserName = User.Identity.Name;
                modelProject.IsDeleted = false;
                db.Projects.Add(modelProject);
                db.SaveChanges();


                return Json(new { id = modelProject.ProjectID, value = modelProject.ProjectName });
            }
        }

        public string RenderViewToString(string templateName, object model)
        {
            templateName = "~/Areas/InvoiceTemplates/Views/InvoiceTemplate/" + templateName + ".cshtml";

            //TODO: Make enumeration for variable templateName.

            ViewData.Model = model;

            try
            {
                using (StringWriter sw = new StringWriter())
                {
                    ViewEngineResult viewResult = ViewEngines.Engines.FindView(ControllerContext, templateName, null);
                    ViewContext viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                    viewResult.View.Render(viewContext, sw);
                    viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);

                    return sw.ToString();
                }
            }
            catch (Exception ex)
            {
                EmailFunctions.SendExceptionToAdmin(ex);
                TempData["ResultErrors"] = "There was a problem with rendering template for email!";
                return "Error in register form! Email with the problem was send to aministrator.";
            }
        }

        public JsonResult SaveInvoiceConfirmed(bool value)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var model = GetInvoiceTempModel(userId);

                // TODO: Set Counter for each user for every year

                var tempCounter =
                    db.Counters
                        .FirstOrDefault(c => c.UserID == userId && c.Year == DateTime.Now.Year.ToString());
                if (tempCounter != null) tempCounter.CounterValue++;


                //var counter = db.Counters.OrderByDescending(s => s.CounterValue).FirstOrDefault(c => c.Year == DateTime.Now.Year.ToString());
                //counter.CounterValue++;
                //db.Counters.Add(counter);
                // TODO: Set Counter for each user for every year


                var faktura = new Fakturi();
                faktura.CompanyID = model.CompanyID;
                faktura.InvoiceDate = DateTime.Parse(model.InvoiceDate);
                faktura.InvoiceEndDate = DateTime.Parse(model.InvoiceEndDate);

                faktura.TotalPrice = model.TotalWithDDS;
                faktura.Period = model.Period ?? " ";
                faktura.FakturaNumber = model.InvoiceNumber;
                faktura.FakturaHtml = RenderViewToString("Index", model);
                faktura.UserID = userId;
                faktura.UserName = User.Identity.Name;
                faktura.DateCreated = DateTime.Now;
                faktura.DateModified = DateTime.Now;
                db.Fakturis.Add(faktura);

                var products = new List<ProductInvoice>();
                foreach (var productTemp in model.ProductsListTemp)
                {
                    products.Add(new ProductInvoice()
                    {
                        InvoiceID = faktura.InvoiceID,
                        DdsID = productTemp.DdsID,
                        ProductID = productTemp.ProductID,
                        ProjectID = productTemp.ProjectID,
                        Quantity = productTemp.Quanity,
                        TotalPrice = productTemp.ProductTotalPrice
                    });
                }
                products.ForEach(s => db.ProductInvoices.Add(s));
                db.SaveChanges();

                byte[] bytes = GeneratePDF(faktura.FakturaHtml.Replace("\r\n", string.Empty));
                string strEmailResult = "<img src=\"" + ConfigurationManager.AppSettings["SocialLogoPath"] + "\">";
                EmailFunctions.SendEmail(ConfigurationManager.AppSettings["AdminEmail"], "New invoice from user "+ User.Identity.Name, strEmailResult, bytes, DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");

                return Json(faktura.FakturaHtml, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                EmailFunctions.SendExceptionToAdmin(ex);
                return Json(false, JsonRequestBehavior.AllowGet);

            }

        }


        public ActionResult SendIvoiceToEmail(string EmailReciever)
        {
            try
            {


                var userId = User.Identity.GetUserId();

                var faktura = db.Fakturis.OrderByDescending(o => o.DateCreated).Where(u => u.UserID == userId).FirstOrDefault();

                var strResult = faktura.FakturaHtml.Replace("\r\n", string.Empty);

                var modelForEmailBody = new InvoiceEmailBodyViewModel()
                {
                    Total = faktura.TotalPrice.ToString(),
                    FakturaDate = faktura.InvoiceDate.ToString("dd.MM.yyyy"),
                    FakturaEndDate = faktura.InvoiceEndDate.ToString("dd.MM.yyyy"),
                    FakturaNumber = faktura.FakturaNumber
                };
                string strEmailResult = RenderEmailBodyView("InvoiceEmailBody", modelForEmailBody);

                byte[] bytes = GeneratePDF(strResult);

                EmailFunctions.SendEmail(EmailReciever, "Faktuur van: " + faktura.Company.CompanyName + " - Periode: " + faktura.Period, strEmailResult, bytes, DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");

                return Json(true, JsonRequestBehavior.AllowGet);

            }
            catch
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult DownloadInvoice(int? idInvoice)
        {
            try
            {
                var userId = User.Identity.GetUserId();

                var strResult = db.Fakturis.OrderByDescending(o => o.DateCreated).Where(u => u.UserID == userId).FirstOrDefault().FakturaHtml;

                byte[] bytes = GeneratePDF(strResult);

                Response.Buffer = false;
                Response.Clear();
                Response.ClearContent();
                Response.ClearHeaders();
                //Set the appropriate ContentType.
                Response.ContentType = "Application/pdf";
                //Write the file content directly to the HTTP content output stream.
                Response.BinaryWrite(bytes);
                Response.Flush();
                Response.End();
                return File(bytes, "application/pdf", DateTime.Now.ToString(CultureInfo.InvariantCulture));

            }
            catch (Exception ex)
            {
                EmailFunctions.SendExceptionToAdmin(ex);
                return Json(false);
            }
        }

        #endregion

    }
}