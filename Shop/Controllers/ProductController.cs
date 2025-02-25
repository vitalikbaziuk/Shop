﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Shop.Data;
using Shop.Models;
using Shop.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Shop.Controllers
{
    public class ProductController : Controller
    {

        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> productList = _db.Products.Include(u => u.Category);
            return View(productList);
        }

        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new ProductVM()
            {
                Product = new Product(),
                CategorySelectList = _db.Categories.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
            };

            if (id == null)
            {
                return View(productVM);
            }
            else
            {
                productVM.Product = _db.Products.Find(id);
                if (productVM.Product == null)
                    return NotFound();
                return View(productVM);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductVM productVM)
        {
            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                string webRootPath = _webHostEnvironment.WebRootPath;

                if (productVM.Product.Id == 0)
                {
                    string upload = webRootPath + ENV.ImagePath;
                    string filename = Guid.NewGuid().ToString();
                    string extentions = Path.GetExtension(files[0].FileName);

                    using (var fileStream = new FileStream(Path.Combine(upload, filename + extentions), FileMode.Create))
                        files[0].CopyTo(fileStream);

                    productVM.Product.Image = filename + extentions;

                    _db.Products.Add(productVM.Product);
                }
                else
                {
                    var formObject = _db.Products.AsNoTracking().FirstOrDefault(u => u.Id == productVM.Product.Id);
                    if (files.Count() > 0)
                    {
                        string upload = webRootPath + ENV.ImagePath;
                        string filename = Guid.NewGuid().ToString();
                        string extentions = Path.GetExtension(files[0].FileName);

                        var oldFile = Path.Combine(upload, formObject.Image);

                        if (System.IO.File.Exists(oldFile))
                            System.IO.File.Delete(oldFile);

                        using (var fileStream = new FileStream(Path.Combine(upload, filename + extentions), FileMode.Create))
                            files[0].CopyTo(fileStream);

                        productVM.Product.Image = filename + extentions;
                    }
                    else
                    {
                        productVM.Product.Image = formObject.Image;
                    }
                    _db.Products.Update(productVM.Product);
                }
                _db.SaveChanges();

                return RedirectToAction("Index");
            }
            productVM.CategorySelectList = _db.Categories.Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Id.ToString()
            });

            return View(productVM);
        }

        //GET
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
                return NotFound();

            Product product = _db.Products.Include(u => u.Category).FirstOrDefault(u => u.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        //POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? id)
        {
            var obj = _db.Products.Find(id);

            if (obj == null)
                return NotFound();

            string upload = _webHostEnvironment.WebRootPath + ENV.ImagePath;
            var oldFile = Path.Combine(upload, obj.Image);

            if (System.IO.File.Exists(oldFile))
                System.IO.File.Delete(oldFile);

            _db.Products.Remove(obj);
            _db.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}