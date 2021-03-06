﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using akcetDB;
using akcet_fakturi.Models;
using Microsoft.AspNet.Identity;
using Data;

namespace akcet_fakturi.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private AppDbContext db = new AppDbContext();

        // GET: Products
        public ActionResult Index()
        {
            var model = new List<Product>();
            var userId = User.Identity.GetUserId();
            model = db.Products.Where(p => p.UserId == userId && p.IsDeleted == false).ToList();

            return View(model);
        }

        // GET: Products/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // GET: Products/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Exclude = "UserId", Include = "ProductID,UserId,ProductName,DateCreated,DateModified")] Product product)
        {
            ModelState.Remove("UserId");
            if (ModelState.IsValid)
            {
                product.DateCreated = DateTime.Now;
                product.DateModified = DateTime.Now;
                product.UserId = User.Identity.GetUserId();

                db.Products.Add(product);
                db.SaveChanges();
                TempData["ResultSuccess"] = "Успешно добавихте продукт!";
                return RedirectToAction("Index");
            }

            return View(product);
        }

        // GET: Products/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ProductID,UserId,ProductName,DateCreated,DateModified")] Product product)
        {
            ModelState.Remove("UserId");
            if (ModelState.IsValid)
            {
                var productDb = db.Products.Find(product.ProductID);

                productDb.DateModified = DateTime.Now;
                productDb.ProductName = product.ProductName;
                
                db.SaveChanges();
                TempData["ResultSuccess"] = "Успешно редактирахте продукт!";
                return RedirectToAction("Index");
            }
            return View(product);
        }

        // GET: Products/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Products.Find(id);
            product.IsDeleted = true;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
